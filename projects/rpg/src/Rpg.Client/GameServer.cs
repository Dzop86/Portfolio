using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>Why the server said no: the HTTP status, and the message to show (its title, then its field errors).</summary>
public sealed class ServerException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>
/// The game's side of the server's API (accounts and characters), used by the Godot client and by the
/// end-to-end tests, so both go through the same calls. Sign in once; the token goes with every call.
/// </summary>
public sealed class GameServer(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public HttpClient Http { get; } = http ?? throw new ArgumentNullException(nameof(http));

    public bool SignedIn => Http.DefaultRequestHeaders.Authorization is not null;

    public async Task SignUp(string name, string password, CancellationToken cancel = default) =>
        await Check(await Http.PostAsJsonAsync("api/accounts", new Credentials(name, password), Json, cancel), cancel);

    public async Task SignIn(string name, string password, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Http.PostAsJsonAsync("api/tokens", new Credentials(name, password), Json, cancel);
        await Check(response, cancel);
        AccessToken token = (await response.Content.ReadFromJsonAsync<AccessToken>(Json, cancel))!;
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    }

    public void SignOut() => Http.DefaultRequestHeaders.Authorization = null;

    public async Task<IReadOnlyList<CharacterSummary>> Characters(CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Http.GetAsync(new Uri("api/characters", UriKind.Relative), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary[]>(Json, cancel))!;
    }

    public async Task<CharacterSummary> CreateCharacter(string name, string look, string heroClass, int colour = 0, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Http.PostAsJsonAsync("api/characters", new NewCharacter(name, look, heroClass, colour), Json, cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>Saves where a character stands in a town.</summary>
    public async Task SavePlace(Guid id, Place place, CancellationToken cancel = default) =>
        await Check(await Http.PutAsJsonAsync(new Uri($"api/characters/{id}/place", UriKind.Relative), place, Json, cancel), cancel);

    public async Task DeleteCharacter(Guid id, CancellationToken cancel = default) =>
        await Check(await Http.DeleteAsync(new Uri($"api/characters/{id}", UriKind.Relative), cancel), cancel);

    private static async Task Check(HttpResponseMessage response, CancellationToken cancel)
    {
        if (response.IsSuccessStatusCode)
            return;
        string message = response.StatusCode.ToString();
        try
        {
            using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancel));
            if (problem.RootElement.TryGetProperty("title", out JsonElement title))
                message = title.GetString() ?? message;
            if (problem.RootElement.TryGetProperty("errors", out JsonElement errors))
                message += " " + string.Join(" ", errors.EnumerateObject().SelectMany(e => e.Value.EnumerateArray().Select(v => $"{e.Name}: {v.GetString()}")));
        }
        catch (JsonException)
        {
            // Not a problem document (a proxy, a crash): the status says enough.
        }
        throw new ServerException(response.StatusCode, message);
    }
}
