using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rpg.Api.Data;
using Rpg.Client;

namespace Rpg.Api.Tests;

/// <summary>
/// The API on a fresh PostgreSQL database of its own, dropped afterwards. The server comes from the
/// <c>RPG_TEST_DB</c> environment variable (for instance <c>Host=localhost;Username=postgres</c>).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _database = "rpg_test_" + Guid.NewGuid().ToString("N");

    public FakeClock Clock { get; } = new();

    public int AccountsPerMinute { get; init; } = 10_000;

    /// <summary>More configuration (servers...), as an environment would give it.</summary>
    public IReadOnlyDictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();

    /// <summary>A folder served under /updates, as for the launcher; none by default.</summary>
    public string? UpdatesRoot { get; init; }

    public static string DatabaseServer => Environment.GetEnvironmentVariable("RPG_TEST_DB")
        ?? throw new InvalidOperationException("Set RPG_TEST_DB to a PostgreSQL connection string (Host=...;Username=...) to run the API tests.");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Game", $"{DatabaseServer};Database={_database}");
        builder.UseSetting("RateLimit:AccountsPerMinute", AccountsPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
        foreach ((string key, string value) in Settings)
            builder.UseSetting(key, value);
        if (UpdatesRoot is not null)
            builder.UseSetting("Updates:Root", UpdatesRoot);
        builder.UseSetting("Jwt:Key", Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    public T WithDb<T>(Func<GameDb, T> query)
    {
        using IServiceScope scope = Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<GameDb>());
    }

    public override async ValueTask DisposeAsync()
    {
        using (IServiceScope scope = Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<GameDb>().Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }

    /// <summary>The game's own client, on a new account with a unique name, signed in.</summary>
    public async Task<GameServer> SignedIn(string prefix = "p")
    {
        var server = new GameServer(CreateClient());
        string name = $"{prefix}{Guid.NewGuid():N}"[..20];
        await server.SignUp(name, "correct horse battery");
        await server.SignIn(name, "correct horse battery");
        return server;
    }
}

/// <summary>A clock the tests move forward by hand (token lifetime).</summary>
public sealed class FakeClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
