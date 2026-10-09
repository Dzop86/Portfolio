using System.Net;
using System.Net.Http.Json;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

public class ServerTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WithoutConfiguration_ThereIsOneServer_AndCharactersLiveOnIt()
    {
        await using var api = new ApiFactory();
        GameServer player = await api.SignedIn();
        Assert.Equal([new ServerInfo("osmeria", "Osméria")], await player.Servers(Cancel));
        CharacterSummary c = await player.CreateCharacter("Élise", "female-c", "mage", 3, cancel: Cancel);
        Assert.Equal(("osmeria", 1), (c.Server, c.Level));
        Assert.Equal(("osmeria", 1), (Assert.Single(await player.Characters(Cancel)).Server, c.Level));
    }

    [Fact]
    public async Task ConfiguredServers_AreListed_AndACharacterGoesOnTheOneChosen()
    {
        await using var api = new ApiFactory
        {
            Settings = new Dictionary<string, string>
            {
                ["Servers:0:Id"] = "osmeria",
                ["Servers:0:Name"] = "Osméria",
                ["Servers:1:Id"] = "brume",
                ["Servers:1:Name"] = "Brume",
            },
        };
        GameServer player = await api.SignedIn();
        Assert.Equal(["Osméria", "Brume"], (await player.Servers(Cancel)).Select(s => s.Name));
        Assert.Equal("brume", (await player.CreateCharacter("Margaux", "female-e", "guard", server: "brume", cancel: Cancel)).Server);
        var e = await Assert.ThrowsAsync<ServerException>(() => player.CreateCharacter("Elsa", "female-a", "guard", server: "atlantis", cancel: Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, e.Status);
        Assert.Contains("server:", e.Message, StringComparison.Ordinal);
        // The list needs no account: a launcher could show it before signing in.
        var anonymous = await api.CreateClient().GetFromJsonAsync<ServerInfo[]>(new Uri("/api/servers", UriKind.Relative), Cancel);
        Assert.Equal(2, anonymous!.Length);
    }
}
