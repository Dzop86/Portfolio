using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>End to end: the game's login and character screen, without its drawing, against the real server.</summary>
public class LobbyTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APlayerSignsUp_CreatesACharacter_AndFightsWithIt()
    {
        await using var api = new ApiFactory();
        var lobby = new Lobby(new GameServer(api.CreateClient()));
        Assert.True(await lobby.SignIn("ada_lovelace", "correct horse battery", create: true, Cancel));
        Assert.Empty(lobby.Characters);
        Assert.True(await lobby.Create("Élise", "female-c", "sentinel", cancel: Cancel));
        CharacterSummary elise = Assert.Single(lobby.Characters);

        var fight = new Fight(GameData.Embedded, "training", 3, elise.Hero);
        Assert.Equal("Élise", fight.Fighters.First(f => f.Team == 0).Name.Fr);

        Assert.True(await lobby.Delete(elise.Id, Cancel));
        Assert.Empty(lobby.Characters);
    }

    [Fact]
    public async Task TheServersRefusals_BecomeTheScreensMessages()
    {
        await using var api = new ApiFactory();
        var first = new Lobby(new GameServer(api.CreateClient()));
        Assert.True(await first.SignIn("ada_lovelace", "correct horse battery", create: true, Cancel));
        Assert.True(await first.Create("Élise", "female-c", "sentinel", cancel: Cancel));

        var second = new Lobby(new GameServer(api.CreateClient()));
        Assert.False(await second.SignIn("Ada_Lovelace", "another long password", create: true, Cancel));
        Assert.Equal("lobby.account-taken", second.Problem);
        Assert.False(await second.SignIn("ada_lovelace", "wrong password!!", create: false, Cancel));
        Assert.Equal("lobby.wrong-password", second.Problem);
        Assert.False(await second.SignIn("with space", "correct horse battery", create: true, Cancel));
        Assert.Equal("lobby.account-invalid", second.Problem);

        Assert.True(await second.SignIn("grace_hopper", "correct horse battery", create: true, Cancel));
        Assert.False(await second.Create("ÉLISE", "male-a", "sentinel", cancel: Cancel));
        Assert.Equal("lobby.name-taken", second.Problem);
    }

    [Fact]
    public async Task TheFifthCharacter_IsTheLast()
    {
        await using var api = new ApiFactory();
        var lobby = new Lobby(new GameServer(api.CreateClient()));
        await lobby.SignIn("ada_lovelace", "correct horse battery", create: true, Cancel);
        foreach (string name in new[] { "Anne", "Bruno", "Chloé", "Damien", "Elsa" })
            Assert.True(await lobby.Create(name, "male-d", "sentinel", cancel: Cancel), lobby.Problem);
        Assert.False(lobby.CanCreate);
        Assert.False(await lobby.Create("Fanny", "male-d", "sentinel", cancel: Cancel));
        Assert.Equal("lobby.too-many-characters", lobby.Problem);
    }

    [Fact]
    public async Task AfterTwelveHours_ThePlayerSignsInAgain()
    {
        await using var api = new ApiFactory();
        var lobby = new Lobby(new GameServer(api.CreateClient()));
        await lobby.SignIn("ada_lovelace", "correct horse battery", create: true, Cancel);
        api.Clock.Advance(TimeSpan.FromHours(13));
        Assert.False(await lobby.Create("Élise", "female-c", "sentinel", cancel: Cancel));
        Assert.Equal("lobby.session-expired", lobby.Problem);
        Assert.False(lobby.SignedIn);
    }

    [Fact]
    public async Task TheLaunchersToken_OpensTheCharacters_AndAStaleOneTheForm()
    {
        await using var api = new ApiFactory();
        GameServer launcher = await api.SignedIn();
        await launcher.CreateCharacter("Élise", "female-c", "mage", 3, cancel: Cancel);
        string token = launcher.Http.DefaultRequestHeaders.Authorization!.Parameter!;

        var game = new Lobby(new GameServer(api.CreateClient()));
        Assert.True(await game.UseToken(token, Cancel));
        Assert.True(game.SignedIn);
        Assert.Equal("Élise", Assert.Single(game.Characters).Name);

        var late = new Lobby(new GameServer(api.CreateClient()));
        api.Clock.Advance(TimeSpan.FromHours(13));
        Assert.False(await late.UseToken(token, Cancel));
        Assert.Equal("lobby.session-expired", late.Problem);
        Assert.False(late.SignedIn);
    }
}
