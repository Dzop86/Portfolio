using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The Druid by the API, on PostgreSQL (sprint 216, docs/classes-druide.md): created like any class, its shared glyph helping it at once.</summary>
public class DruidTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ADruid_IsCreated_AndItsSharedGlyph_ShieldsItAtOnce()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("dr1");
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-b", "druid", cancel: Cancel);
        Assert.Equal("druid", c.Class);
        Assert.Null(c.Hero.Problem(GameData.Embedded));
        Assert.Contains("bramble-ring", c.Hero.DeckOf(GameData.Embedded));
        // Read back from the server: its Bramble Ring laid round itself, one glyph a side, its own resistance at once.
        CharacterSummary read = (await server.Characters(Cancel)).Single(x => x.Id == c.Id);
        var fight = new Fight(GameData.Embedded, "training", 1, read.Hero);
        while (fight.Current != fight.HeroFighter)
            fight.Apply(new EndTurnAction());
        Fighter me = fight.HeroFighter!;
        Cell near = me.Cell.Neighbours().First(fight.IsFree);
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("bramble-ring", near)));
        Assert.Equal([Affects.Enemies, Affects.Allies], fight.Glyphs.Select(g => g.Effect.Affects));
        Assert.Contains(me.Statuses, s => s is { Stat: Stat.Resistance, Value: 15 });
    }
}
