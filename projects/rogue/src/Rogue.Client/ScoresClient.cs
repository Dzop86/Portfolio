using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rogue.Core;

namespace Rogue.Client;

/// <summary>A ranked run handed out by the server: play this seed before the deadline.</summary>
public sealed record RunTicket(Guid Id, string Seed, DateTimeOffset ExpiresAt)
{
    public ulong SeedValue => RunRecord.TryParseSeed(Seed, out ulong seed) ? seed : throw new FormatException($"Bad seed from the server: {Seed}");
}

/// <summary>The outcome the server computed by replaying the run.</summary>
public sealed record RunVerdict(Guid Id, GameState Outcome, int Score, int Depth, int Turns, int Kills);

public sealed record ScoreLine(int Rank, string Player, int Score, GameState Outcome, int Depth, int Turns, DateTimeOffset PlayedAt);

/// <summary>An answer of the API other than success: its status, title and, for a refused run, the reason and the faulty action.</summary>
public sealed class ScoresException : Exception
{
    public ScoresException(HttpStatusCode status, string? title, string? error = null, int? actionIndex = null)
        : base($"{(int)status} {title}")
    {
        Status = status;
        Title = title;
        Error = error;
        ActionIndex = actionIndex;
    }

    public ScoresException()
    {
    }

    public ScoresException(string message)
        : base(message)
    {
    }

    public ScoresException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HttpStatusCode Status { get; }

    public string? Title { get; }

    public string? Error { get; }

    public int? ActionIndex { get; }
}

/// <summary>
/// The score API seen from a game client: sign up, sign in (the token is then sent with every call),
/// start a ranked run, submit it, read the leaderboard.
/// </summary>
public sealed class ScoresClient(HttpClient http)
{
    public const string NameTaken = "NameTaken";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public bool SignedIn => http.DefaultRequestHeaders.Authorization is not null;

    /// <exception cref="ScoresException">With <see cref="ScoresException.Error"/> set to <c>NameTaken</c> when the name exists.</exception>
    public async Task SignUpAsync(string name, string password, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await http.PostAsJsonAsync("api/accounts", new { name, password }, Json, cancel);
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new ScoresException(response.StatusCode, "Name taken", NameTaken);
        await Check(response, cancel);
    }

    public async Task SignInAsync(string name, string password, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await http.PostAsJsonAsync("api/tokens", new { name, password }, Json, cancel);
        await Check(response, cancel);
        AccessToken token = (await response.Content.ReadFromJsonAsync<AccessToken>(Json, cancel))!;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    }

    public async Task<RunTicket> StartRunAsync(CancellationToken cancel = default)
    {
        HttpResponseMessage response = await http.PostAsync("api/runs", null, cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<RunTicket>(Json, cancel))!;
    }

    public async Task<RunVerdict> SubmitAsync(RunTicket ticket, RunRecord run, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(run);
        using var body = new StringContent(run.ToJson(), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await http.PostAsync($"api/runs/{ticket.Id}/submission", body, cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<RunVerdict>(Json, cancel))!;
    }

    public async Task<IReadOnlyList<ScoreLine>> LeaderboardAsync(int limit = 10, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await http.GetAsync($"api/scores?limit={limit}", cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<List<ScoreLine>>(Json, cancel))!;
    }

    private static async Task Check(HttpResponseMessage response, CancellationToken cancel)
    {
        if (response.IsSuccessStatusCode)
            return;
        Problem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<Problem>(Json, cancel);
        }
        catch (JsonException)
        {
            // Not a problem document (a proxy's error page, an empty 429): the status says enough.
        }
        catch (NotSupportedException)
        {
        }
        throw new ScoresException(response.StatusCode, problem?.Title ?? response.ReasonPhrase, problem?.Error, problem?.ActionIndex);
    }

    private sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

    private sealed record Problem(string? Title, string? Error, int? ActionIndex);
}
