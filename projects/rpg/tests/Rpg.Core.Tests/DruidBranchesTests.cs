using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Druid's first two branches (sprint 217, docs/classes-druide-2.md): the Nature Warden, whose glyphs last a turn more, its Sylvan who strikes
/// from them and its Grove Keeper who shields, itself with its blow; the Shapeshifter, whose form lasts 3 turns, costs no hit point, ends and comes back. On the real data.
/// </summary>
public class DruidBranchesTests
{
    private static Hero Druid(string spec, string? fork, int level, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "druid", Level: level, Stats: new Characteristics(Earth: 100), Deck: deck.Length == 0 ? null : deck) { Spec = spec, Fork = fork, Final = final };

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    /// <summary>A training fight at the hero's turn, with all the points it needs, an enemy next to it.</summary>
    private static (Fight Fight, Fighter Me, Fighter Foe) Training(Hero hero)
    {
        Fight f = new(Real, "training", 1, hero);
        while (f.Current != f.HeroFighter)
            f.Apply(new EndTurnAction());
        Fighter me = f.HeroFighter!;
        me.Ap = 99;
        Fighter foe = f.Fighters.First(x => x.Team == 1);
        foe.Cell = me.Cell.Neighbours().First(f.IsFree);
        foe.Hp = 999;
        return (f, me, foe);
    }

    /// <summary>Ends turns until the hero's comes back, its points refilled.</summary>
    private static void NextTurn(Fight f)
    {
        do
            f.Apply(new EndTurnAction());
        while (f.Current != f.HeroFighter);
        f.HeroFighter!.Ap = 99;
    }

    [Fact]
    public void TheNatureWarden_ItsLineagesAndFinals()
    {
        Specialisation spec = Real.Specs["nature-warden"];
        Assert.Equal("druid", spec.Class);
        Assert.Equal(["path-hedge", "path-barkskin"], spec.Base.Spells);
        Assert.Equal(["sylvan", "grove-keeper"], spec.Forks.Select(x => x.Id));
        Assert.Contains(new GlyphTurnsRule(1), Specs.RulesOf(Druid("nature-warden", null, 40), Real));
        Assert.Contains(new GlyphTargetRule(20, 0), Specs.RulesOf(Druid("nature-warden", "sylvan", 60), Real));
        Assert.Contains(new ShieldRule(25), Specs.RulesOf(Druid("nature-warden", "grove-keeper", 60), Real));
        Assert.DoesNotContain(Specs.RulesOf(Druid("nature-warden", "grove-keeper", 60), Real), r => r is GlyphTargetRule);
        // The Lord of the Woods' Wrath leaves shared ground; the Elder's comes back sooner.
        Spell wrath = Compiled(Druid("nature-warden", "sylvan", 100, "woods-lord"), "path-woods-wrath");
        Assert.Equal([Affects.Enemies, Affects.Allies], wrath.AllEffects.OfType<GlyphEffect>().Select(g => g.Affects));
        Assert.True(wrath.AllEffects.OfType<GlyphEffect>().Single(g => g.Affects == Affects.Allies).Welcomes);
        Assert.Equal(1, Compiled(Druid("nature-warden", "sylvan", 100, "sylvan-elder"), "path-woods-wrath").Cooldown);
        // The Grove Keeper's Guarding Bough strikes and shields its caster; the Ancestral Warden adds resistance, the Spirit of the Forest brings it back sooner.
        Spell bough = Real.Spells["path-guarding-bough"];
        Assert.True(bough.DamageMax > 0);
        Assert.Contains(bough.AllEffects, e => e is ShieldEffect { Affects: Affects.Caster, Amount: 12 });
        Assert.Contains(Compiled(Druid("nature-warden", "grove-keeper", 100, "ancestral-warden"), "path-guarding-bough").AllEffects, e => e is StatusEffect { Affects: Affects.Caster, Stat: Stat.Resistance, Value: 8 });
        Assert.Equal(1, Compiled(Druid("nature-warden", "grove-keeper", 100, "forest-spirit"), "path-guarding-bough").Cooldown);
        Assert.All(spec.Forks, x => Assert.NotEqual(x.Finals![0].Template, x.Finals[1].Template));
    }

    [Fact]
    public void TheNatureWardensGlyphs_LastATurnMore_TheKitsToo_TheirTwoSides()
    {
        // The Hedge and the kit's Bramble Ring: each glyph a side one turn more than in the data.
        foreach (string id in new[] { "path-hedge", "bramble-ring" })
        {
            int[] bare = [.. Real.Spells[id].AllEffects.OfType<GlyphEffect>().Select(g => g.Turns)];
            int[] warden = [.. Compiled(Druid("nature-warden", null, 40), id).AllEffects.OfType<GlyphEffect>().Select(g => g.Turns)];
            Assert.Equal(2, warden.Length);
            Assert.Equal(bare.Select(t => t + 1), warden);
        }
    }

    [Fact]
    public void TheHedge_HindersTheEnemies_AndSheltersTheAlly_ItIsCastOn()
    {
        (Fight f, Fighter me, Fighter foe) = Training(Druid("nature-warden", null, 40, null, "path-hedge"));
        Cell target = me.Cell.Neighbours().First(c => f.IsFree(c) && c.DistanceTo(foe.Cell) <= 2);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-hedge", target)));
        Assert.Equal([Affects.Enemies, Affects.Allies], f.Glyphs.Select(g => g.Effect.Affects));
        Assert.All(f.Glyphs, g => Assert.Equal(3, g.Effect.Turns));
    }

    [Fact]
    public void TheWildShape_OpensTheFormsBlows_ForThreeTurns_WithoutHp_ThenEnds()
    {
        (Fight f, Fighter me, Fighter foe) = Training(Druid("shapeshifter", null, 40, null, "path-wild-shape", "path-claws"));
        Assert.Equal(ActionError.NoForm, f.Check(new CastAction("path-claws", foe.Cell)));
        int hp = me.Hp;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-wild-shape", me.Cell)));
        Assert.Equal(hp, me.Hp);
        Assert.True(me.Raging);
        Assert.Contains(me.Statuses, s => s is { Stat: Stat.Rage, Value: 20 });
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-claws", foe.Cell)));
        // It passes: three turns, then the form's blows are shut until it is taken again.
        for (int turn = 0; turn < 3 && me.Raging; turn++)
            NextTurn(f);
        Assert.False(me.Raging);
        foe.Cell = me.Cell.Neighbours().First(f.IsFree);
        Assert.Equal(ActionError.NoForm, f.Check(new CastAction("path-claws", foe.Cell)));
        // And it comes back, its cooldown of 4 run out.
        while (me.CooldownLeft(me.Spells.Single(s => s.Id == "path-wild-shape")) > 0)
            NextTurn(f);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-wild-shape", me.Cell)));
        Assert.True(me.Raging);
    }

    [Fact]
    public void TheAI_WeighsThePassingForm_ByTheBlowsItLifts_AndNotTwice()
    {
        (Fight f, Fighter me, _) = Training(Druid("shapeshifter", null, 40, null, "path-wild-shape", "path-claws", "thorn"));
        Spell shape = me.Spells.Single(s => s.Id == "path-wild-shape");
        // Counted as the Berserker's lasting Rage, 3 turns of 20 % were worth 0.05 × 20 × 3 × 4 = 12, less than a blow: the bench saw it cast 0.1 times a fight.
        double worth = Ai.Value(f, me, shape, me.Cell, me.Cell);
        Assert.True(worth > 12, $"{worth}");
        f.Apply(new CastAction("path-wild-shape", me.Cell));
        me.Cooldowns.Clear();
        Assert.Equal(0, Ai.Value(f, me, shape, me.Cell, me.Cell));
    }

    [Fact]
    public void TheShapeshifter_ItsLineagesAndFinals_AndWhatSetsItsFormApart_FromTheBerserkers()
    {
        Specialisation spec = Real.Specs["shapeshifter"];
        Assert.Equal(["path-wild-shape", "path-claws"], spec.Base.Spells);
        Assert.Equal(["changeform", "wild-druid"], spec.Forks.Select(x => x.Id));
        // Reversible and free (docs/classes-conception.md §1.4): 3 turns and no hit point, where the Berserker's Rage lasts the fight and is paid for.
        Spell shape = Real.Spells["path-wild-shape"], rage = Real.Spells["rage"];
        Assert.Equal((0, 3), (shape.HpCost, shape.AllEffects.OfType<StatusEffect>().Single(s => s.Stat == Stat.Rage).Turns));
        Assert.True(rage.HpCost > 0 && rage.AllEffects.OfType<StatusEffect>().Single(s => s.Stat == Stat.Rage).Turns == 99);
        string[] spells = [.. spec.Base.Spells!, .. spec.Forks.SelectMany(x => x.Tiers.SelectMany(t => t.Spells ?? []))];
        Assert.All(spells.Where(id => Real.Spells[id].DamageMax > 0), id => Assert.True(Real.Spells[id].Form, id));
        Assert.All(spells, id => Assert.Equal(0, Real.Spells[id].HpCost));
        Assert.Contains(Compiled(Druid("shapeshifter", "changeform", 100, "beasts-king"), "path-pounce").AllEffects, e => e is StatusEffect { Stat: Stat.Mp, Value: -1 });
        Assert.Equal(1, Compiled(Druid("shapeshifter", "changeform", 100, "primal-predator"), "path-pounce").Cooldown);
        Assert.Contains(Compiled(Druid("shapeshifter", "wild-druid", 100, "animal-avatar"), "path-trample").AllEffects, e => e is StatusEffect { Affects: Affects.Caster, Stat: Stat.Damage, Value: 10 });
        Assert.Equal(1, Compiled(Druid("shapeshifter", "wild-druid", 100, "shape-lord"), "path-trample").Cooldown);
        Assert.All(spec.Forks, x => Assert.NotEqual(x.Finals![0].Template, x.Finals[1].Template));
    }

    [Fact]
    public void TheTrample_SparesItsCaster_AndLeavesItsSharedGround_ThatShelters_TheDruidInIt()
    {
        (Fight f, Fighter me, Fighter foe) = Training(Druid("shapeshifter", "wild-druid", 60, null, "path-wild-shape", "path-trample"));
        f.Apply(new CastAction("path-wild-shape", me.Cell));
        (int mine, int theirs) = (me.Hp, foe.Hp);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-trample", me.Cell)));
        Assert.Equal(mine, me.Hp);
        Assert.True(foe.Hp < theirs);
        Assert.Equal([Affects.Enemies, Affects.Allies], f.Glyphs.Select(g => g.Effect.Affects));
        Assert.Contains(me.Statuses, s => s is { Stat: Stat.Resistance, Value: 15 });
    }
}
