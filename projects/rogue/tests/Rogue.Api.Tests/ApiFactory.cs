using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rogue.Api.Data;
using Rogue.Api.Endpoints;
using Rogue.Core;

namespace Rogue.Api.Tests;

/// <summary>
/// The API on a fresh PostgreSQL database of its own, dropped afterwards. The server comes from the
/// <c>ROGUE_TEST_DB</c> environment variable (for instance <c>Host=localhost;Username=postgres</c>).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _database = "rogue_test_" + Guid.NewGuid().ToString("N");

    public FakeClock Clock { get; } = new();

    public int AccountsPerMinute { get; init; } = 10_000;

    public static string DatabaseServer => Environment.GetEnvironmentVariable("ROGUE_TEST_DB")
        ?? throw new InvalidOperationException("Set ROGUE_TEST_DB to a PostgreSQL connection string (Host=...;Username=...) to run the API tests.");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Scores", $"{DatabaseServer};Database={_database}");
        builder.UseSetting("RateLimit:AccountsPerMinute", AccountsPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Jwt:Key", Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    public T WithDb<T>(Func<ScoresDb, T> query)
    {
        using IServiceScope scope = Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<ScoresDb>());
    }

    public override async ValueTask DisposeAsync()
    {
        using (IServiceScope scope = Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ScoresDb>().Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }

    /// <summary>A signed-in player with a unique name.</summary>
    public async Task<PlayerClient> SignedInPlayer(string prefix = "p")
    {
        HttpClient http = CreateClient();
        string name = $"{prefix}{Guid.NewGuid():N}"[..20];
        var credentials = new Credentials(name, "correct horse battery");
        (await http.PostAsJsonAsync("/api/accounts", credentials)).EnsureSuccessStatusCode();
        HttpResponseMessage response = await http.PostAsJsonAsync("/api/tokens", credentials);
        response.EnsureSuccessStatusCode();
        AccessToken token = (await response.Content.ReadFromJsonAsync<AccessToken>(Json.Options))!;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return new PlayerClient(name, http);
    }
}

public sealed record PlayerClient(string Name, HttpClient Http)
{
    public async Task<RunTicket> StartRun()
    {
        HttpResponseMessage response = await Http.PostAsync("/api/runs", null);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RunTicket>(Json.Options))!;
    }

    public Task<HttpResponseMessage> Submit(Guid run, object body) => Http.PostAsJsonAsync($"/api/runs/{run}/submission", body);

    public Task<HttpResponseMessage> Submit(Guid run, string json) =>
        Http.PostAsync($"/api/runs/{run}/submission", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
}

/// <summary>A clock the tests move forward by hand (run expiry, token lifetime).</summary>
public sealed class FakeClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

public static class Json
{
    public static readonly System.Text.Json.JsonSerializerOptions Options = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };
}

public static class Runs
{
    /// <summary>Lets the autopilot play the whole run on the seed handed out by the server.</summary>
    public static Game Play(RunTicket ticket)
    {
        var game = new Game(ulong.Parse(ticket.Seed, System.Globalization.CultureInfo.InvariantCulture));
        while (game.State == GameState.Playing)
            game.Apply(Autopilot.Choose(game));
        return game;
    }

    public static string Json(Game game) => RunRecord.From(game).ToJson();
}
