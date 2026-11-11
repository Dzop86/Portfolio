using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Summoner's Demonist and Conjurer (sprint 213, docs/classes-invocateur-3.md): the tithe on small boards; the branches, their lineages and finals, the
/// Demon and the Pillar on the real data.
/// </summary>
public class DemonistConjurerTests
{
    private static readonly Spell Call = new("call", Text, 1, 1, 1, false, false, 0, 0, 9, Effects: [new SummonEffect(Affects.All, "pet", Max: 2)]);
    private static readonly SummonSpec Pet = new("pet", Text, "male-d", 50, 6, 0, 50, []);

    private static Fight Play(params FighterSpec[] fighters) =>
        new(new GameData([.. Spells, Call], [new MapSpec("m", Text, ["A.B"])], [new Scenario("s", Text, "m", fighters)], summons: [Pet]), "s", 1);

    private static FighterSpec Caller(params TalentRule[] rules) =>
        Spec(0, ap: 99, mp: 0, hp: 500, initiative: 999, spells: ["call"]) with { SpecRules = rules };

    private static FighterSpec Foe() => Spec(1, hp: 900, mp: 0, initiative: 1);

    /// <summary>Everyone passes until <paramref name="who"/> starts its turn number <paramref name="turn"/>.</summary>
    private static void PassUntil(Fight f, Fighter who, int turn)
    {
        while (!(f.Current == who && who.Turn == turn))
            f.Apply(new EndTurnAction());
    }

    [Fact]
    public void TheTithe_TakesItsPercentAtEachTurnOfTheCreature_TheRulesAddUp_NeverTheLastHitPoint()
    {
        Fight f = Play(Caller(new TitheRule("pet", 4), new TitheRule("pet", 2)), Foe());
        Fighter me = f.Fighters[0];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("call", new Cell(1, 0))));
        Fighter pet = f.Fighters[^1];
        // Called, it has taken nothing yet.
        Assert.Equal(500, me.Hp);
        PassUntil(f, pet, 1);
        // 6 % of 500.
        Assert.Equal(470, me.Hp);
        Assert.Contains(f.Events.OfType<HpPaid>(), e => e.Fighter == me.Id && e.Amount == 30 && e.Hp == 470);
        me.Hp = 10;
        PassUntil(f, pet, 2);
        Assert.Equal(1, me.Hp);
        PassUntil(f, pet, 3);
        Assert.Equal(1, me.Hp);
        Assert.True(me.IsAlive);
    }

    [Fact]
    public void WithoutATitheOfItsKind_ACreatureTakesNothing()
    {
        Fight f = Play(Caller(new TitheRule("other", 10)), Foe());
        Fighter me = f.Fighters[0];
        f.Apply(new CastAction("call", new Cell(1, 0)));
        PassUntil(f, f.Fighters[^1], 3);
        Assert.Equal(500, me.Hp);
        Assert.DoesNotContain(f.Events, e => e is HpPaid);
    }

    [Fact]
    public void ADeadSummoner_PaysNoTithe()
    {
        Fight f = Play(Caller(new TitheRule("pet", 10)), Foe());
        Fighter me = f.Fighters[0];
        f.Apply(new CastAction("call", new Cell(1, 0)));
        Fighter pet = f.Fighters[^1];
        me.Hp = 0;
        int paid = f.Events.OfType<HpPaid>().Count();
        while (f.Current != pet && !f.IsOver)
            f.Apply(new EndTurnAction());
        Assert.Equal(paid, f.Events.OfType<HpPaid>().Count());
    }

    private static Hero Summoner(string spec, string? fork, int level, string? final = null) =>
        new("Ondine", "female-c", "summoner", Level: level, Stats: new Characteristics(Earth: 90), Spec: spec, Fork: fork, Final: final);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    private static Fighter HeroIn(Hero hero, out Fight fight)
    {
        fight = new Fight(Real, "training", 1, hero);
        while (fight.Current != fight.HeroFighter)
            fight.Apply(new EndTurnAction());
        return fight.HeroFighter!;
    }

    [Fact]
    public void TheAI_CountsTheTithe_HalfTheHitPointsTheDemonWouldTakeOverItsTurns()
    {
        Fighter demonist = HeroIn(Summoner("demonist", null, 40) with { Deck = ["path-demon"] }, out _);
        Assert.Equal(Ai.SummonTurns * 0.5 * 3 * demonist.MaxHp / 100.0, Ai.TitheCost(demonist, "demon"), 6);
        Fighter caller = HeroIn(Summoner("demonist", "demon-caller", 60) with { Deck = ["path-demon"] }, out _);
        Assert.Equal(Ai.SummonTurns * 0.5 * 4 * caller.MaxHp / 100.0, Ai.TitheCost(caller, "demon"), 6);
        // Nothing for a creature of another kind, nor for a Summoner without a tithe.
        Assert.Equal(0, Ai.TitheCost(caller, "hound"));
        Assert.Equal(0, Ai.TitheCost(HeroIn(Summoner("conjurer", null, 40), out _), "pillar"));
    }

    [Fact]
    public void TheDemonist_ItsLineagesAndFinals()
    {
        Specialisation spec = Real.Specs["demonist"];
        Assert.Equal("summoner", spec.Class);
        Assert.Equal(["path-demon", "path-blood-pact"], spec.Base.Spells);
        Assert.Equal(["demon-caller", "pactbearer"], spec.Forks.Select(x => x.Id));
        TalentRule[] bare = [.. Specs.RulesOf(Summoner("demonist", null, 40), Real)];
        Assert.Contains(new SummonRule("demon", 25, 40), bare);
        Assert.Contains(new TitheRule("demon", 3), bare);
        // The Demon Caller feeds its Demon more, and pays more for it: 4 % in all.
        TalentRule[] caller = [.. Specs.RulesOf(Summoner("demonist", "demon-caller", 60), Real)];
        Assert.Contains(new SummonRule("demon", 35, 50), caller);
        Assert.Equal(4, caller.OfType<TitheRule>().Sum(r => r.Percent));
        Assert.Contains(Compiled(Summoner("demonist", "demon-caller", 100, "infernal-lord"), "path-unleash").AllEffects, e => e is StatusEffect { Stat: Stat.Ap, Value: 2 });
        Assert.Equal(2, Compiled(Summoner("demonist", "demon-caller", 100, "archdemonist"), "path-demon").Cooldown);
        // The Pactbearer pays for its own blows.
        Assert.True(Real.Spells["path-blood-price"] is { HpCost: > 0, DamageMax: > 0 });
        Assert.Contains(Compiled(Summoner("demonist", "pactbearer", 100, "pact-master"), "path-blood-pact").AllEffects, e => e is StatusEffect { Stat: Stat.Ap, Value: 1 });
        Assert.Contains(Compiled(Summoner("demonist", "pactbearer", 100, "abyss-chosen"), "path-blood-pact").AllEffects, e => e is StatusEffect { Stat: Stat.Rage, Turns: 99 });
        Assert.All(spec.Forks, x => Assert.NotEqual(x.Finals![0].Template, x.Finals[1].Template));
        // Blood Pact: hit points for damage, to the caster and its allies around; Unleash aims at the Demon.
        Assert.True(Real.Spells["path-blood-pact"] is { HpCost: > 0 });
        Assert.Contains(Real.Spells["path-blood-pact"].AllEffects, e => e is StatusEffect { Affects: Affects.Allies, Stat: Stat.Damage });
        Assert.Equal("demon", Real.Spells["path-unleash"].Aims);
    }

    [Fact]
    public void TheConjurer_ItsLineagesAndFinals()
    {
        Specialisation spec = Real.Specs["conjurer"];
        Assert.Equal("summoner", spec.Class);
        Assert.Equal(["path-pillar", "path-anchor"], spec.Base.Spells);
        Assert.Equal(["elemental-caller", "astral-caller"], spec.Forks.Select(x => x.Id));
        Assert.Contains(new SummonRule("pillar", 25, 40), Specs.RulesOf(Summoner("conjurer", null, 40), Real));
        // The Pillar never moves; two at most, three for the Elemental Caller.
        Assert.Equal(0, Real.Summons["pillar"].Mp);
        Assert.Equal(2, Real.Spells["path-pillar"].AllEffects.OfType<SummonEffect>().Single().Max);
        Assert.Equal(3, Compiled(Summoner("conjurer", "elemental-caller", 60), "path-pillar").AllEffects.OfType<SummonEffect>().Single().Max);
        Assert.Equal(1, Compiled(Summoner("conjurer", "elemental-caller", 100, "elemental-lord"), "path-pillar").Cooldown);
        Assert.Contains(Specs.RulesOf(Summoner("conjurer", "elemental-caller", 100, "primordial"), Real).OfType<GrowthRule>(), r => r.Summon == "pillar");
        // The Astral Caller draws on the ground.
        Assert.Contains(Real.Spells["path-constellation"].AllEffects, e => e is GlyphEffect { Affects: Affects.Enemies });
        Assert.Contains(Compiled(Summoner("conjurer", "astral-caller", 100, "cosmic-master"), "path-constellation").AllEffects, e => e is StatusEffect { Stat: Stat.Resistance, Value: -10 });
        Assert.All(spec.Forks, x => Assert.NotEqual(x.Finals![0].Template, x.Finals[1].Template));
        Assert.Equal(("pillar", "pillar"), (Real.Spells["path-anchor"].Aims, Real.Spells["path-overload"].Aims));
    }

    [Fact]
    public void ADemonists_Demon_IsFedAndTakesItsTitheInARealFight()
    {
        Fighter me = HeroIn(Summoner("demonist", "demon-caller", 60) with { Deck = ["path-demon"] }, out Fight fight);
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("path-demon", me.Cell.Neighbours().First(fight.IsFree))));
        Fighter demon = fight.Fighters[^1];
        Assert.Equal(me.MaxHp * 35 / 100, demon.MaxHp);
        int hp = me.Hp;
        fight.Apply(new EndTurnAction());
        Assert.Same(demon, fight.Current);
        Assert.Equal(hp - (me.MaxHp * 4 / 100), me.Hp);
    }

    [Fact]
    public void AConjurers_Pillar_StaysWhereItWasLaid_AndAnchoringShieldsAroundIt()
    {
        Fighter me = HeroIn(Summoner("conjurer", null, 40) with { Deck = ["path-pillar", "path-anchor"] }, out Fight fight);
        Cell laid = me.Cell.Neighbours().First(fight.IsFree);
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("path-pillar", laid)));
        Fighter pillar = fight.Fighters[^1];
        Assert.Equal(me.MaxHp * 25 / 100, pillar.MaxHp);
        // Anchoring aims at the Pillar, and shields it and the caster next to it.
        Assert.Equal(ActionError.NoValidTarget, fight.Apply(new CastAction("path-anchor", me.Cell.Neighbours().First(fight.IsFree))));
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("path-anchor", laid)));
        Assert.True(pillar.Shield > 0 && me.Shield > 0);
        // The AI plays its turn: it does not move.
        fight.Apply(new EndTurnAction());
        Assert.Same(pillar, fight.Current);
        while (fight.Current == pillar && !fight.IsOver)
        {
            FightAction action = Ai.Decide(fight);
            Assert.IsNotType<MoveAction>(action);
            Assert.Equal(ActionError.None, fight.Apply(action));
        }
        Assert.Equal(laid, pillar.Cell);
    }
}
