using System.Net;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// What the screens before the game can do, without an engine: sign in (or up; the launcher usually
/// does it and hands a token), choose a server (at once when there is only one), list, create and
/// delete the characters, choose one, or play offline. Every refusal becomes a key of
/// <see cref="Texts"/> (<c>lobby.*</c>) in <see cref="Problem"/>, so the screen shows it in the
/// player's language; the checks the server makes are made here first, to answer at once.
/// </summary>
public sealed class Lobby(GameServer server)
{
    public GameServer Server { get; } = server ?? throw new ArgumentNullException(nameof(server));

    /// <summary>All the account's characters, on every server.</summary>
    public IReadOnlyList<CharacterSummary> Characters { get; private set; } = [];

    public IReadOnlyList<ServerInfo> Servers { get; private set; } = [];

    /// <summary>The server chosen; set at once when there is only one.</summary>
    public ServerInfo? ChosenServer { get; private set; }

    /// <summary>The characters of the chosen server, oldest first.</summary>
    public IReadOnlyList<CharacterSummary> Here => [.. Characters.Where(c => c.Server == ChosenServer?.Id)];

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
            await LoadAccount(cancel);
        }, status => status switch
        {
            HttpStatusCode.BadRequest => "lobby.account-invalid",
            HttpStatusCode.Conflict => "lobby.account-taken",
            HttpStatusCode.Unauthorized => "lobby.wrong-password",
            _ => null,
        });
    }

    /// <summary>Signed in by the launcher: its token, then the characters (a token run out sends back to the form).</summary>
    public async Task<bool> UseToken(string token, CancellationToken cancel = default)
    {
        Server.UseToken(token);
        return await Call(() => LoadAccount(cancel), _ => null);
    }

    /// <summary>The servers and the characters; a single server is chosen at once.</summary>
    private async Task LoadAccount(CancellationToken cancel)
    {
        Servers = await Server.Servers(cancel);
        Characters = await Server.Characters(cancel);
        ChosenServer = Servers.Count == 1 ? Servers[0] : null;
    }

    public void ChooseServer(string? id) => ChosenServer = Servers.FirstOrDefault(s => s.Id == id);

    public async Task<bool> Create(string name, string look, string heroClass, int colour = 0, CancellationToken cancel = default)
    {
        if (!Hero.IsValidName(name))
            return Fail("lobby.name-invalid");
        if (new Hero(name, look, heroClass, colour).Problem(GameData.Embedded) is not null || heroClass is null)
            return Fail("lobby.class-invalid");
        if (!CanCreate)
            return Fail("lobby.too-many-characters");
        if (ChosenServer is null)
            return Fail("lobby.choose-server");
        return await Call(async () =>
        {
            await Server.CreateCharacter(name, look, heroClass, colour, ChosenServer?.Id, cancel);
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
        Servers = [];
        ChosenServer = null;
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
