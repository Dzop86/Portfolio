using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Rogue (Voleur, sprint 204, docs/classes-voleur.md): a class's own rules, its engine on an isolated target, Stealth on small boards; then its
/// kit, and what the AI makes of Stealth.
/// </summary>
public class RogueTests
{
    // Fixed numbers where a test counts hit points.
    private static readonly Spell Stab = new("stab", Text, 1, 1, 1, false, false, 20, 20, 9);
    private static readonly Spell Fade = new("fade", Text, 1, 0, 0, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Caster, Stat.Hidden, 1, 2)]);
    private static readonly Spell Taint = new("taint", Text, 1, 1, 1, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Poison, 3, 2)]);
    private static readonly Spell Blast = new("blast", Text, 1, 1, 6, false, false, 5, 5, 9, Element.Neutral, new Area(AreaShape.Circle, 1));
    private static readonly Spell Purge = new("purge", Text, 1, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new PurgeEffect(Affects.Enemies)]);
    private static readonly Spell[] All = [.. Spells, Stab, Fade, Taint, Blast, Purge];

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    private static FighterSpec Rogue(params string[] spells) =>
        Spec(0, ap: 99, mp: 0, initiative: 999, spells: spells) with { SpecRules = [new IsolatedRule(2, 15)] };

    // ---- The class's engine: an isolated target (§2) ----

    [Theory]
    [InlineData("AB.B", 20)]
    [InlineData("ABB.", 20)]
    [InlineData("AB..B", 23)]
    [InlineData("AB", 23)]
    public void TheRogue_StrikesAnIsolatedTarget_FifteenPercentHarder(string row, int dealt)
    {
        // A fighter of the target's side within 2 cells protects it; at 3, or alone, it is isolated.
        Fight f = Play([row], [Rogue("stab"), .. Enumerable.Range(0, row.Count(c => c == 'B')).Select(i => Spec(1, i, hp: 500, initiative: 1 - i))]);
        Fighter target = f.Fighters[1];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("stab", target.Cell)));
        Assert.Equal(500 - dealt, target.Hp);
    }

    [Fact]
    public void WithoutTheRule_AnIsolatedTarget_TakesWhatAnyOtherDoes()
    {
        Fight f = Play(["AB"], Spec(0, ap: 99, mp: 0, initiative: 999, spells: "stab"), Spec(1, hp: 500, initiative: 1));
        f.Apply(new CastAction("stab", f.Fighters[1].Cell));
        Assert.Equal(480, f.Fighters[1].Hp);
    }

    [Fact]
    public void TheRoguesRules_ComeFromItsClass_NoOtherClassHasAny()
    {
        HeroClass rogue = Real.Class("rogue")!;
        Assert.Equal([new IsolatedRule(2, 15), new StackRule(3)], rogue.Rules);
        Assert.All(Real.Classes.Where(c => c.Id is not ("rogue" or "berserker" or "paladin")), c => Assert.Null(c.Rules));
        // A Rogue of level 1, with no path, fights with them; a Warrior of level 1 with nothing.
        Fight f = new(Real, "training", 1, new Hero("Ondine", "female-c", "rogue"));
        Assert.Contains(f.HeroFighter!.Talents.SelectMany(t => t.Rules), r => r is IsolatedRule { Cells: 2, Percent: 15 });
        Assert.Empty(new Fight(Real, "training", 1, new Hero("Ondine", "female-c", "warrior")).HeroFighter!.Talents);
    }

    // ---- Stealth (§3) ----

    [Fact]
    public void AHiddenFighter_IsAimedAtFromTwoCellsOrLess_AnAreaBesideItStillStrikesIt()
    {
        Fight f = Play(["A....B"], Rogue("fade"), Spec(1, hp: 500, initiative: 1, spells: ["bow", "blast"]));
        Fighter me = f.Fighters[0], enemy = f.Fighters[1];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("fade", me.Cell)));
        Assert.True(me.Hidden);
        Assert.Contains(me.Statuses, s => s is { Stat: Stat.Hidden, TurnsLeft: 2 });
        Assert.Equal(ActionError.Hidden, f.CheckCast(enemy, Spells[1], enemy.Cell, me.Cell));
        Assert.Equal(ActionError.None, f.CheckCast(enemy, Spells[1], new Cell(me.Cell.X + 2, me.Cell.Y), me.Cell));
        // Its own side aims at it as ever; an area on the cell beside it is no aim at it.
        Assert.Equal(ActionError.None, f.CheckCast(enemy, Blast, enemy.Cell, new Cell(me.Cell.X + 1, me.Cell.Y)));
    }

    [Theory]
    [InlineData("stab", false)]
    [InlineData("taint", false)]
    [InlineData("fade", true)]
    public void Stealth_EndsWhenItsBearerHurtsAnEnemy(string spell, bool hidden)
    {
        Fight f = Play(["AB"], Rogue("fade", "stab", "taint"), Spec(1, hp: 500, initiative: 1));
        Fighter me = f.Fighters[0];
        f.Apply(new CastAction("fade", me.Cell));
        f.Apply(new CastAction(spell, spell == "fade" ? me.Cell : f.Fighters[1].Cell));
        Assert.Equal(hidden, me.Hidden);
        Assert.Equal(!hidden, f.Events.OfType<StatusEnded>().Any(e => e is { Fighter: 0, Stat: Stat.Hidden }));
    }

    [Fact]
    public void AHitWhileHidden_StillGetsTheEngine_AndTheStealthIsAStatusAnEnemysPurgeTakes()
    {
        Fight f = Play(["AB"], Rogue("fade", "stab"), Spec(1, hp: 500, initiative: 1));
        f.Apply(new CastAction("fade", f.Fighters[0].Cell));
        f.Apply(new CastAction("stab", f.Fighters[1].Cell));
        Assert.Equal(477, f.Fighters[1].Hp);
        Assert.False(Fight.Ailment(new Status(Stat.Hidden, 1, 2, Element.Neutral, 0)));
    }

    [Fact]
    public void AnEnemysPurge_TakesTheStealth()
    {
        Fight f = Play(["A.B"], Rogue("fade"), Spec(1, hp: 500, initiative: 1, spells: ["purge"]));
        f.Apply(new CastAction("fade", f.Fighters[0].Cell));
        f.Apply(new EndTurnAction());
        Assert.Equal(ActionError.None, f.Apply(new CastAction("purge", f.Fighters[0].Cell)));
        Assert.False(f.Fighters[0].Hidden);
    }

    // ---- The kit (§4) ----

    [Fact]
    public void TheRoguesSheet_AndItsTwentyFourSpells_AtTheirLevels()
    {
        HeroClass rogue = Real.Class("rogue")!;
        Assert.Equal((58, 3, 8, 5, 30), (rogue.Hp, rogue.HpPerLevel, rogue.Ap, rogue.Mp, rogue.Initiative));
        Assert.Equal(("Voleur", "Rogue"), (rogue.Name.Fr, rogue.Name.En));
        Spell[] kit = [.. rogue.Spells.Select(id => Real.Spells[id])];
        Assert.Equal(24, kit.Length);
        Assert.Equal([1, 1, 1, 1, 1, .. Enumerable.Range(1, 19).Select(i => 5 * i)], kit.Select(s => s.Level));
        // Four spells an element, and eight neutral ones; reach at most 4, Stealth in two of them.
        Assert.All(new[] { Element.Earth, Element.Fire, Element.Water, Element.Air }, e => Assert.Equal(4, kit.Count(s => s.Element == e)));
        Assert.Equal(8, kit.Count(s => s.Element == Element.Neutral));
        Assert.All(kit, s => Assert.InRange(s.MaxRange, 0, 4));
        Assert.Equal(["fade", "smoke-bomb"], kit.Where(s => s.AllEffects.Any(e => e is StatusEffect { Stat: Stat.Hidden, Affects: Affects.Caster })).Select(s => s.Id));
        // Classes share no spell.
        Assert.Equal(Real.Classes.Sum(c => c.Spells.Count), Real.Classes.SelectMany(c => c.Spells).Distinct().Count());
    }

    [Fact]
    public void TheRogue_StrikesAtOnePointOneFiveOfTheBudget()
    {
        // The Pommel Strike and the first sprints' Strike: 3 action points, from next to the target, at level 1.
        static double Average(string id) => (Real.Spells[id].DamageMin + Real.Spells[id].DamageMax) / 2.0;
        // 1.15 since the first measure (docs/classes-voleur.md §6): at 1.1 it lost the Disjoint alone.
        Assert.InRange(Average("pommel-strike") / Average("strike"), 1.1, 1.2);
        // Its twins at the compound's share of its budget.
        Assert.InRange(Average("lightning-ember-dagger") / Average("ember-dagger"), 0.75, 0.85);
        Assert.Equal(96, Real.Spells.Values.Count(s => s.Twin is string t && Real.Class("rogue")!.Spells.Contains(t)));
    }

    // ---- What the AI makes of Stealth ----

    [Fact]
    public void Stealth_IsWorthTheBlowsOfTheEnemiesTooFarToComeClose()
    {
        // A bowman 5 cells away with 1 movement point reaches the Rogue, but cannot come within 2 cells of it; one next to it can.
        Fight far = Play(["A....B"], Rogue("fade"), Spec(1, hp: 500, mp: 1, initiative: 1, spells: ["bow"]));
        Assert.True(Ai.Unreached(far, far.Fighters[0]) > 0);
        Fight near = Play(["A.B"], Rogue("fade"), Spec(1, hp: 500, mp: 1, initiative: 1, spells: ["bow"]));
        Assert.Equal(0, Ai.Unreached(near, near.Fighters[0]));
    }
}
