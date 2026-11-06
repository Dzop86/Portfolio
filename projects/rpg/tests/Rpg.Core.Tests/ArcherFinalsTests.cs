using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Archer's tree of four ranks (sprint 198, D96, docs/classes-archer.md): the Artillerist come in the Trail's place, the Bounty Hunter and its quarry, the
/// Arbalist and its heavy bolt, and the new final classes, each on the arena; the hero converted to its taken-back final fights as before.
/// </summary>
public class ArcherFinalsTests
{
    private static Hero Archer(string spec, string? fork, string? final, int level = 100, params string[] deck) =>
        new("Ondine", "female-a", "archer", Level: level, Stats: new Characteristics(Earth: 90, Fire: 20), Spec: spec, Fork: fork, Final: final, Deck: deck.Length == 0 ? null : deck);

    private static GameData With(string[] rows, FighterSpec[] fighters) =>
        new([.. Real.Spells.Values], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], Real.Classes,
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);

    /// <summary>The hero plays first, against monsters that play after it.</summary>
    private static Fight On(string[] rows, Hero hero, params FighterSpec[] monsters) => new(With(rows, [Spec(0, initiative: 9999), .. monsters]), "s", 1, hero);

    private static FighterSpec Foe(int start = 0, int hp = 900) => Spec(1, start, hp: hp, ap: 9, mp: 3, initiative: 1);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    private static int Percent(Hero hero, HitContext context, int cells = 3) =>
        Talents.Percent([Specs.AsTalent(Specs.RulesOf(hero, Real))], new Cell(0, 0), new Cell(cells, 0), Element.Earth, context);

    [Fact]
    public void EachLineage_EndsInTwoFinals_TheFirstTakenBackFromItsEvolutionOf100()
    {
        SpecFork[] forks = [.. Real.Specs.Values.Where(s => s.Class == "archer").SelectMany(s => s.Forks)];
        Assert.Equal(["marksman", "bounty-hunter", "trapper", "falconer", "barrage", "runner"], forks.Select(f => f.Id));
        Assert.All(forks, f => Assert.Equal(2, f.Finals!.Count));
        // The taken-back finals have the passives, replaces and rules of the evolutions of before, written here since those are gone.
        Assert.Equal(["spec-calm-eye-long"], Specs.PassivesOf(Archer("sightline", "marksman", "dragon-eye"), Real));
        Assert.Equal(new Area(AreaShape.Line, 1), Compiled(Archer("sightline", "marksman", "dragon-eye"), "path-aimed-shot").Zone);
        Assert.Equal(2, Compiled(Archer("ambush", "trapper", "forest-master"), "path-jaws").AllEffects.OfType<GlyphEffect>().Single().Max);
        Assert.Equal(2, Compiled(Archer("ambush", "falconer", "beast-lord"), "hawk").AllEffects.OfType<SummonEffect>().Single().Max);
        Assert.Equal(1, Compiled(Archer("trail", "barrage", "living-ballista"), "path-barrage").Cooldown);
        Assert.Equal(new EchoRule(50, 3), Specs.RulesOf(Archer("trail", "runner", "vaulter"), Real).OfType<EchoRule>().Single());
        Assert.Equal("Exécuteur", Specs.TitleOf(Archer("sightline", "bounty-hunter", "bounty-killer"), Real)!.Fr);
    }

    [Fact]
    public void TheArtillerist_HasTheBarrageAtThirty_AndPushesACellFarther()
    {
        Assert.Equal(30, Real.Spells["path-barrage"].Level);
        Assert.Equal(["path-barrage", "path-heel-kick"], Real.Specs["trail"].Base.Spells);
        Assert.Equal(["spec-quick-heels"], Specs.PassivesOf(Archer("trail", null, null, 40), Real));
        Assert.Equal(3, Compiled(Archer("trail", null, null, 40), "path-heel-kick").AllEffects.OfType<PushEffect>().Single().Cells);
        Fight f = On(["AB........"], Archer("trail", null, null, 40, "path-heel-kick"), Foe());
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-heel-kick", new Cell(1, 0))));
        Assert.Equal(new Cell(4, 0), f.Fighters[1].Cell);
    }

    [Fact]
    public void TheQuarry_CarriesTwoStates_WeakSpotAThird_AndTheBountyHunterHitsHarderFromThree()
    {
        Fight f = On(["A...B......"], Archer("sightline", "bounty-hunter", null, 60, "path-prey", "path-weak-spot"), Foe());
        Fighter foe = f.Fighters[1];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-prey", foe.Cell)));
        Assert.Equal(2, foe.States);
        Assert.Equal(2, f.ContextOf(f.HeroFighter!, null, f.HeroFighter!.Cell, foe).TargetStates);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-weak-spot", foe.Cell)));
        Assert.Equal(3, foe.States);
        Assert.Equal(3, f.ContextOf(f.HeroFighter!, null, f.HeroFighter!.Cell, foe).TargetStates);
        Assert.Equal(15, Percent(Archer("sightline", "bounty-hunter", null, 60), new HitContext(TargetStates: 3)));
        Assert.Equal(0, Percent(Archer("sightline", "bounty-hunter", null, 60), new HitContext(TargetStates: 2)));
        // The quarry: −1 MP and −10 % of damage, three turns.
        Assert.Contains(foe.Statuses, s => s.Stat == Stat.Mp && s.Value == -1 && s.TurnsLeft == 3);
        Assert.Contains(foe.Statuses, s => s.Stat == Stat.Damage && s.Value == -10);
    }

    [Fact]
    public void TheLegendaryStalker_DoublesTheBonus_AndMarksMoreOften()
    {
        Hero hero = Archer("sightline", "bounty-hunter", "legendary-tracker");
        Assert.Equal(30, Percent(hero, new HitContext(TargetStates: 3)));
        Assert.Equal(0, Percent(hero, new HitContext(TargetStates: 2)));
        Assert.Equal(1, Compiled(hero, "path-prey").Cooldown);
        Assert.Equal(2, Compiled(Archer("sightline", "bounty-hunter", null), "path-prey").Cooldown);
    }

    [Fact]
    public void TheExecutioner_CollectsItsBounty_OnAKill()
    {
        Fight f = On(["A...BB....."], Archer("sightline", "bounty-hunter", "bounty-killer", deck: ["stone-arrow"]), Foe(hp: 1), Foe(1));
        Fighter hero = f.HeroFighter!;
        int ap = hero.Ap;
        int cost = hero.Spells.Single(s => s.Id == "stone-arrow").ApCost;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("stone-arrow", f.Fighters[1].Cell)));
        Assert.False(f.Fighters[1].IsAlive);
        Assert.Equal(1, f.Events.OfType<PassiveTriggered>().Count(p => p.Passive == "spec-bounty"));
        Assert.Equal(ap - cost + 2, hero.Ap);
        Assert.Contains(hero.Statuses, s => s.Stat == Stat.Damage && s.Value == 15);
    }

    [Fact]
    public void ThePerfectShot_OpensEachTurnHarder_AndBringsTheBullseyeBackSooner()
    {
        Hero hero = Archer("sightline", "marksman", "perfect-shot");
        Assert.Equal(20, Percent(hero, new HitContext(DamageCasts: 0)));
        Assert.Equal(0, Percent(hero, new HitContext(DamageCasts: 1)));
        Assert.Equal(2, Compiled(hero, "path-bullseye").Cooldown);
        Assert.Equal(3, Compiled(Archer("sightline", "marksman", "dragon-eye"), "path-bullseye").Cooldown);
    }

    [Fact]
    public void ThePathfinder_TrapsFarAhead_AndHitsHarderFromSixCells()
    {
        Hero hero = Archer("ambush", "trapper", "tracker");
        Assert.Equal(8, Compiled(hero, "path-jaws").MaxRange);
        Assert.Equal(7, Compiled(hero, "path-bait").MaxRange);
        Assert.Equal(10, Percent(hero, new HitContext(), cells: 6));
        Assert.Equal(0, Percent(hero, new HitContext(), cells: 5));
        Assert.Equal(1, Compiled(hero, "path-jaws").AllEffects.OfType<GlyphEffect>().Single().Max);
    }

    [Fact]
    public void TheAncestralPredator_HasOneStrongHawk_ItTradesPlacesWithEachTurn()
    {
        Hero hero = Archer("ambush", "falconer", "ancestral-predator", deck: ["hawk", "stone-arrow"]);
        Fight f = On(["A.........B"], hero, Foe());
        Assert.Equal(ActionError.None, f.Apply(new CastAction("hawk", new Cell(1, 0))));
        Fighter hawk = f.Fighters.Single(x => x.SummonKind == "hawk");
        Assert.Equal(f.HeroFighter!.MaxHp * 35 / 100, hawk.MaxHp);
        Assert.Equal(1, Compiled(hero, "hawk").AllEffects.OfType<SummonEffect>().Single().Max);
        Spell changeover = Compiled(hero, "path-changeover");
        Assert.Equal((0, 1), (changeover.Cooldown, changeover.PerTurn));
    }

    [Fact]
    public void TheArbalist_HitsHarderWithoutWalking_AndItsMasterReachesTenAndPushesTwo()
    {
        Hero arbalist = Archer("trail", "barrage", null, 60);
        Assert.Equal(5, Percent(arbalist, new HitContext(Walked: false)));
        Assert.Equal(0, Percent(arbalist, new HitContext(Walked: true)));
        Assert.Equal(["path-barrage", "path-heel-kick", "path-heavy-bolt"], Real.Specs["trail"].SpellsWith("barrage"));
        Spell bolt = Compiled(Archer("trail", "barrage", "crossbow-master"), "path-heavy-bolt");
        Assert.Equal(10, bolt.MaxRange);
        Assert.Equal(2, bolt.AllEffects.OfType<PushEffect>().Single().Cells);
        Assert.Empty(Compiled(Archer("trail", "barrage", "living-ballista"), "path-heavy-bolt").AllEffects.OfType<PushEffect>());
        Fight f = On(["A.....B......"], Archer("trail", "barrage", "crossbow-master", deck: ["path-heavy-bolt"]), Foe());
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-heavy-bolt", new Cell(6, 0))));
        Assert.Equal(new Cell(8, 0), f.Fighters[1].Cell);
    }

    [Fact]
    public void TheWarHawk_ShootsOnTheRunTwiceATurn()
    {
        Assert.Equal(2, Compiled(Archer("trail", "runner", "war-hawk"), "path-running-shot").PerTurn);
        Assert.Equal(1, Compiled(Archer("trail", "runner", "vaulter"), "path-running-shot").PerTurn);
        Fight f = On(["A...B......"], Archer("trail", "runner", "war-hawk", deck: ["path-running-shot"]), Foe());
        f.HeroFighter!.Ap = 30;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-running-shot", new Cell(4, 0))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-running-shot", new Cell(4, 0))));
        Assert.NotEqual(ActionError.None, f.Check(new CastAction("path-running-shot", new Cell(4, 0))));
    }
}
