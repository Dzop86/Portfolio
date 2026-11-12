using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The Necromancer by the API, on PostgreSQL (sprint 214, docs/classes-necromancien.md): created like any class, its dead fed by its class and raised on a corpse only.</summary>
public class NecromancerTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ANecromancer_IsCreated_ItsDeadFedByItsClass_AndAskingForACorpse()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("ne1");
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "necromancer", cancel: Cancel);
        Assert.Equal("necromancer", c.Class);
        Assert.Null(c.Hero.Problem(GameData.Embedded));
        Assert.Contains("raise-dead", c.Hero.DeckOf(GameData.Embedded));
        // Read back from the server: its dead fed by its class, and no corpse to raise it on before a monster falls.
        CharacterSummary read = (await server.Characters(Cancel)).Single(x => x.Id == c.Id);
        var fight = new Fight(GameData.Embedded, "training", 1, read.Hero);
        while (fight.Current != fight.HeroFighter)
            fight.Apply(new EndTurnAction());
        Fighter me = fight.HeroFighter!;
        Assert.Contains(me.Talents.SelectMany(t => t.Rules), r => r is SummonRule { Summon: "risen", HpPercent: 35, StatsPercent: 45 });
        Assert.Empty(fight.Corpses);
        Cell near = me.Cell.Neighbours().SelectMany(n => n.Neighbours()).First(n => n.DistanceTo(me.Cell) == 2 && fight.IsFree(n));
        Assert.Equal(ActionError.NoCorpse, fight.Check(new CastAction("raise-dead", near)));
    }
}
