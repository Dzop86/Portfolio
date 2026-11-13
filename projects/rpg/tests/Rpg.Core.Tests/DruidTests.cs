using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Druid (sprint 216, docs/classes-druide.md): its shared glyphs, one for the enemies and one, welcoming, for the allies, on small boards;
/// its kit on the real data, and what sets it apart from the classes whose glyphs strike only enemies.
/// </summary>
public class DruidTests
{
    private static readonly GlyphEffect Hinder = new(Affects.Enemies, 2, Status: Stat.Mp, Value: -1, StatusTurns: 1);
    private static readonly GlyphEffect Shelter = new(Affects.Allies, 2, Status: Stat.Resistance, Value: 15, StatusTurns: 1, Welcomes: true);
    private static readonly Spell Ring = new("ring", Text, 1, 0, 9, false, false, 0, 0, 9, Area: new Area(AreaShape.Circle, 1), Effects: [Hinder, Shelter]);
    // The mossy keepers' Moss Circle: an allies' glyph that does not welcome.
    private static readonly Spell Moss = new("moss", Text, 1, 0, 9, false, false, 0, 0, 9, Area: new Area(AreaShape.Circle, 1),
        Effects: [Shelter with { Welcomes = false }]);

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData([.. Spells, Ring, Moss], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    /// <summary>The druid at the first cell, an ally at the third, an enemy at the fourth: one circle on the third cell covers all three.</summary>
    private static Fight Grove() =>
        Play(["A.AB.."], Spec(0, 0, ap: 99, mp: 0, hp: 500, initiative: 999, spells: ["ring", "moss"]), Spec(0, 1, mp: 0, initiative: 2), Spec(1, 0, hp: 500, mp: 0, initiative: 1));

    [Fact]
    public void ASharedGlyph_HindersTheEnemies_AndShelters_TheAlliesOnIt_AtOnce()
    {
        Fight f = Grove();
        Fighter ally = f.Fighters[1];
        Fighter foe = f.Fighters[2];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("ring", ally.Cell)));
        // Two glyphs on the same cells, one a side.
        Assert.Equal([Affects.Enemies, Affects.Allies], f.Glyphs.Select(g => g.Effect.Affects));
        Assert.Equal(f.Glyphs[0].Cells, f.Glyphs[1].Cells);
        // The ally standing there is sheltered now; the enemy is spared until it can walk off (sprint 152).
        Assert.Contains(ally.Statuses, s => s is { Stat: Stat.Resistance, Value: 15 });
        Assert.DoesNotContain(foe.Statuses, s => s.Stat == Stat.Mp);
        Assert.True(f.Strikes(f.Glyphs[0], foe) && !f.Strikes(f.Glyphs[0], ally) && f.Strikes(f.Glyphs[1], ally) && !f.Strikes(f.Glyphs[1], foe));
    }

    [Fact]
    public void ASharedGlyphCastAgain_ReplacesItsTwoGlyphs_OneASide()
    {
        Fight f = Grove();
        Fighter me = f.Fighters[0];
        f.Apply(new CastAction("ring", new Cell(2, 0)));
        int[] first = [.. f.Glyphs.Select(g => g.Id)];
        f.Apply(new CastAction("ring", new Cell(4, 0)));
        Assert.Equal(2, f.Glyphs.Count);
        Assert.DoesNotContain(f.Glyphs, g => first.Contains(g.Id));
        Assert.All(f.Glyphs, g => Assert.Contains(new Cell(4, 0), g.Cells));
        Assert.Equal(0, me.Team);
    }

    [Fact]
    public void AnAlliesGlyphThatDoesNotWelcome_WaitsForTheAllysTurn_AsBefore()
    {
        // The monsters' Moss Circle keeps its rule: the fights of the Forest do not change.
        Fight f = Grove();
        Fighter ally = f.Fighters[1];
        f.Apply(new CastAction("moss", ally.Cell));
        Assert.DoesNotContain(ally.Statuses, s => s.Stat == Stat.Resistance);
    }

    [Fact]
    public void OnlyAnAlliesGlyph_WithAStatus_MayWelcome()
    {
        Spell bad = Ring with { Id = "bad", Effects = [Hinder with { Welcomes = true }] };
        Assert.Throws<InvalidDataException>(() => new GameData([.. Spells, bad], [new MapSpec("m", Text, ["A.B"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])]));
    }

    [Fact]
    public void TheAI_CountsAWelcomingGlyph_InFullForTheAlliesOnIt()
    {
        Fight f = Grove();
        Fighter me = f.Fighters[0];
        Fighter ally = f.Fighters[1];
        double welcoming = Ai.Value(f, me, Ring with { Effects = [Shelter] }, me.Cell, ally.Cell);
        double waiting = Ai.Value(f, me, Moss, me.Cell, ally.Cell);
        Assert.True(welcoming > waiting && waiting > 0, $"{welcoming} {waiting}");
    }

    private static IEnumerable<GlyphEffect> GlyphsOf(Spell s) => s.AllEffects.OfType<GlyphEffect>();

    [Fact]
    public void TheDruidsSheet_AndItsKit()
    {
        HeroClass d = Real.Class("druid")!;
        Assert.Equal((78, 3, 8, 4, 10), (d.Hp, d.HpPerLevel, d.Ap, d.Mp, d.Initiative));
        Spell[] kit = [.. d.Spells.Select(id => Real.Spells[id])];
        Assert.Equal(24, kit.Length);
        Assert.Equal([1, 1, 1, 1, 1, .. Enumerable.Range(1, 19).Select(i => 5 * i)], kit.Select(k => k.Level));
        // Its engine from level 1: ten spells lay a shared glyph, the neutral ones of levels 1 and 5 among them.
        Spell[] shared = [.. kit.Where(k => GlyphsOf(k).Any(g => g.Affects == Affects.Allies))];
        Assert.Equal(10, shared.Length);
        Assert.Equal(["bramble-ring", "grove"], shared.Where(k => k.Level <= 5).Select(k => k.Id));
        // Each with one allies' glyph that welcomes, and an enemies' one beside it if any.
        Assert.All(shared, k => Assert.Single(GlyphsOf(k), g => g is { Affects: Affects.Allies, Welcomes: true }));
        Assert.All(kit, k => Assert.True(GlyphsOf(k).Count(g => g.Affects == Affects.Enemies) <= 1, k.Id));
        // Blows of 1 to 4 cells at the full budget (0.85 in the design, measured to 1): the Thorn as the Mage's Pebble; a blow that lays a glyph at 0.8 of that.
        Assert.All(kit.Where(k => k.DamageMax > 0), k => Assert.Equal(4, k.MaxRange));
        static double Average(string id) => (Real.Spells[id].DamageMin + Real.Spells[id].DamageMax) / 2.0;
        Assert.InRange(Average("thorn") / Average("pebble"), 0.92, 1.08);
        // The Wildfire against the Necromancer's Tomb Flames, the same level, cost, area and cooldown at 0.85: 0.8 of a blow at 1, 0.94 of it.
        Assert.InRange(Average("wildfire") / Average("tomb-flames"), 0.88, 0.99);
        // Its burning ground does a third of the blow, in the blow's element; in Ash for its Ash twin, one glyph a side still.
        GlyphEffect burn = GlyphsOf(Real.Spells["brushfire"]).Single(g => g.Affects == Affects.Enemies);
        Assert.Equal((Element.Fire, Real.Spells["brushfire"].DamageMax / 3), (burn.Element, burn.DamageMax));
        Assert.Equal([Element.Ash], GlyphsOf(Real.Spells["ash-brushfire"]).Where(g => g.Affects == Affects.Enemies).Select(g => g.Element));
        Assert.Null(d.Rules);
    }

    [Fact]
    public void WhatSetsTheDruidApart_ItsGlyphsHelpTheAlliesToo()
    {
        // docs/classes-conception.md §1.4: the Ranger's glyphs strike enemies only, the Druid's help the allies too.
        string[] druid = [.. Real.Class("druid")!.Spells];
        string[] others = [.. Real.Classes.Where(c => c.Id != "druid").SelectMany(c => c.Spells), .. Real.Specs.Values.Where(s => s.Class != "druid")
            .SelectMany(s => (s.Base.Spells ?? []).Concat(s.Forks.SelectMany(f => f.Tiers.SelectMany(t => t.Spells ?? []))))];
        Assert.All(others.Select(id => Real.Spells[id]), s => Assert.DoesNotContain(GlyphsOf(s), g => g.Welcomes));
        // No spell of the game but the Druid's and its twins welcomes.
        Assert.All(Real.Spells.Values.Where(s => GlyphsOf(s).Any(g => g.Welcomes)), s => Assert.Contains(s.Twin ?? s.Id, druid));
    }

    [Fact]
    public void TheGrove_HealsTheDruidStandingInIt_EveryTurn()
    {
        Fight f = new(Real, "training", 1, new Hero("Ondine", "female-b", "druid", Level: 10, Deck: ["grove"]));
        while (f.Current != f.HeroFighter)
            f.Apply(new EndTurnAction());
        Fighter me = f.HeroFighter!;
        me.Hp = me.MaxHp / 2;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("grove", me.Cell)));
        Assert.Contains(me.Statuses, s => s is { Stat: Stat.Regen, Value: 5 });
    }
}
