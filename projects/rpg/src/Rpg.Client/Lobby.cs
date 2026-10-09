using System.Net;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// What the login and character screen can do, without an engine: sign in (or up), list, create and
/// delete characters, choose one to fight, or play offline. Every refusal becomes a key of
/// <see cref="Texts"/> (<c>lobby.*</c>) in <see cref="Problem"/>, so the screen shows it in the
/// player's language; the checks the server makes are made here first, to answer at once.
/// </summary>
public sealed class Lobby(GameServer server)
{
    public GameServer Server { get; } = server ?? throw new ArgumentNullException(nameof(server));

    public IReadOnlyList<CharacterSummary> Characters { get; private set; } = [];

    /// <summary>The <c>lobby.*</c> key of the last refusal, or null after a success.</summary>
    public string? Problem { get; private set; }

    public bool SignedIn => Server.SignedIn;

    public bool CanCreate => SignedIn && Characters.Count < Accounts.MaxCharacters;

    /// <summary>Signs in, after creating the account if <paramref name="create"/>; then loads the characters.</summary>
    public async Task<bool> SignIn(string name, string password, bool create, CancellationToken cancel = default)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(password))
            return Fail("lobby.fill-in");
        if (create && password.Length < 10)
            return Fail("lobby.password-short");
        return await Call(async () =>
        {
            if (create)
                await Server.SignUp(name, password, cancel);
            await Server.SignIn(name, password, cancel);
            Characters = await Server.Characters(cancel);
        }, status => status switch
        {
            HttpStatusCode.BadRequest => "lobby.account-invalid",
            HttpStatusCode.Conflict => "lobby.account-taken",
            HttpStatusCode.Unauthorized => "lobby.wrong-password",
            _ => null,
        });
    }

    public async Task<bool> Create(string name, string look, CancellationToken cancel = default)
    {
        if (!Hero.IsValidName(name))
            return Fail("lobby.name-invalid");
        if (!CanCreate)
            return Fail("lobby.too-many-characters");
        return await Call(async () =>
        {
            await Server.CreateCharacter(name, look, cancel);
            Characters = await Server.Characters(cancel);
        }, status => status switch
        {
            HttpStatusCode.BadRequest => "lobby.name-invalid",
            HttpStatusCode.Conflict => Characters.Count >= Accounts.MaxCharacters ? "lobby.too-many-characters" : "lobby.name-taken",
            _ => null,
        });
    }

    public async Task<bool> Delete(Guid id, CancellationToken cancel = default) =>
        await Call(async () =>
        {
            await Server.DeleteCharacter(id, cancel);
            Characters = await Server.Characters(cancel);
        }, _ => null);

    public void SignOut()
    {
        Server.SignOut();
        Characters = [];
        Problem = null;
    }

    /// <summary>
    /// Runs the calls; a refusal becomes a key: the screen's own (<paramref name="refusal"/>), else
    /// the ones every call shares (expired session, too many tries, server down).
    /// </summary>
    private async Task<bool> Call(Func<Task> calls, Func<HttpStatusCode, string?> refusal)
    {
        try
        {
            await calls();
            Problem = null;
            return true;
        }
        catch (ServerException e) when (refusal(e.Status) is string key)
        {
            return Fail(key);
        }
        catch (ServerException e) when (e.Status == HttpStatusCode.Unauthorized)
        {
            // The token ran out (12 hours): back to the sign-in form.
            SignOut();
            return Fail("lobby.session-expired");
        }
        catch (ServerException e) when (e.Status == HttpStatusCode.TooManyRequests)
        {
            return Fail("lobby.too-many-tries");
        }
        catch (ServerException)
        {
            return Fail("lobby.server-error");
        }
        catch (HttpRequestException)
        {
            return Fail("lobby.unreachable");
        }
        catch (TaskCanceledException)
        {
            // HttpClient's time-out (the screen's own cancellation never happens: it lives as long as the game).
            return Fail("lobby.unreachable");
        }
    }

    private bool Fail(string key)
    {
        Problem = key;
        return false;
    }
}
