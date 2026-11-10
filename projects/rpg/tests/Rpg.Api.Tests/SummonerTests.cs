using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The Summoner by the API, on PostgreSQL (sprint 211, docs/classes-invocateur.md): created like any class, its creatures fed by its class's rules.</summary>
public class SummonerTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ASummoner_IsCreated_WithTwoHoundsToCall_FedByItsClass()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("su1");
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "summoner", cancel: Cancel);
        Assert.Equal("summoner", c.Class);
        Assert.Null(c.Hero.Problem(GameData.Embedded));
        Assert.Contains("call-hound", c.Hero.DeckOf(GameData.Embedded));
        // Read back from the server, it raises a Hound fed by its class: a fifth of its hit points.
        CharacterSummary read = (await server.Characters(Cancel)).Single(x => x.Id == c.Id);
        var fight = new Fight(GameData.Embedded, "training", 1, read.Hero);
        while (fight.Current != fight.HeroFighter)
            fight.Apply(new EndTurnAction());
        Fighter me = fight.HeroFighter!;
        Cell free = me.Cell.Neighbours().First(fight.IsFree);
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("call-hound", free)));
        Assert.Equal(me.MaxHp * 20 / 100, fight.Fighters[^1].MaxHp);
    }
}
