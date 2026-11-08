using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Berserker's second half (sprint 207, docs/classes-berserker-2.md): the Barbarian's clan up close and the Gladiator's execution on small boards,
/// then their lineages and eight final classes on the real data.
/// </summary>
public class BerserkerPathsTests
{
    private static readonly Spell Hit = new("hit", Text, 1, 1, 1, false, false, 20, 20, 9);
    private static readonly Spell[] All = [.. Spells, Hit];

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    private static FighterSpec Caster(params TalentRule[] rules) => Spec(0, ap: 99, mp: 0, initiative: 999, spells: "hit") with { SpecRules = rules };

    [Fact]
    public void TheGladiator_Executes_ATargetBelowThirtyPercent_BeyondTheCeiling()
    {
        Fight f = Play(["AB"], Caster(new ExecuteRule(30, 25), new BonusRule(35)), Spec(1, hp: 100, initiative: 1));
        Fighter target = f.Fighters[1];
        f.Apply(new CastAction("hit", target.Cell));
        // +35 %, the ceiling: 27; the target at 73 of 100 is not yet below 30 %.
        Assert.Equal(73, target.Hp);
        target.Hp = 29;
        f.Apply(new CastAction("hit", target.Cell));
        // +35 % and +25 % beyond: 32, the target falls.
        Assert.False(target.IsAlive);
    }

    [Theory]
    [InlineData("ABA", 78)]
    [InlineData("AB.A", 80)]
    public void TheBarbarian_StrikesHarder_ATargetAnAllyStandsNextTo(string row, int left)
    {
        Fight f = Play([row], Caster(new FlankRule(10)), Spec(1, hp: 100, initiative: 1), Spec(0, 1, initiative: 0));
        f.Apply(new CastAction("hit", f.Fighters[1].Cell));
        Assert.Equal(left, f.Fighters[1].Hp);
    }

    private static Hero Berserker(string? spec, string? fork = null, int level = 60, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "berserker", Level: level, Stats: new Characteristics(Earth: 90), Spec: spec, Fork: fork, Final: final, Deck: deck.Length == 0 ? null : deck);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    [Fact]
    public void TheTree_IsRavagerBarbarianGladiator_TwelveFinals()
    {
        Specialisation[] branches = [.. Real.Specs.Values.Where(s => s.Class == "berserker")];
        Assert.Equal(["skullcracker", "carnivore", "tribal-warrior", "savage", "blood-champion", "executor"], branches.SelectMany(b => b.Forks).Select(f => f.Id));
        Assert.Equal(12, branches.SelectMany(b => b.Forks).SelectMany(f => f.Finals!).Count());
        Assert.Equal(12, branches.SelectMany(b => b.Forks.SelectMany(f => b.SpellsWith(f.Id))).Distinct().Count());
        Assert.All(branches.SelectMany(b => b.Forks), f => Assert.NotEqual(f.Finals![0].Template, f.Finals[1].Template));
        // What sets it apart from the Warrior, its paths too (classes-conception.md §1.4): no shield, no pull, no movement taken.
        Assert.DoesNotContain(branches.SelectMany(b => b.Forks.SelectMany(f => b.SpellsWith(f.Id))).Distinct().SelectMany(id => Real.Spells[id].AllEffects),
            e => e is ShieldEffect or PullEffect || e is StatusEffect { Affects: Affects.Enemies, Stat: Stat.Mp });
    }

    [Fact]
    public void TheFinals_ChangeWhatTheirLineageHas()
    {
        Assert.Equal(2, Compiled(Berserker("barbarian", "tribal-warrior", 100, "warchief"), "path-war-standard").Cooldown);
        Assert.Equal(new Area(AreaShape.Circle, 3), Compiled(Berserker("barbarian", "tribal-warrior", 100, "clan-king"), "path-clan-cry").Area);
        Assert.Equal(20, Specs.RulesOf(Berserker("barbarian", "savage", 100, "predator"), Real).OfType<TargetStateRule>().Single().Percent);
        Assert.Contains(Compiled(Berserker("barbarian", "savage", 100, "primal-avatar"), "rage").AllEffects, e => e is StatusEffect { Stat: Stat.Ap, Value: 2 });
        Assert.Equal(2, Compiled(Berserker("berserker-gladiator", "blood-champion", 100, "arena-legend"), "path-blood-duel").MaxRange);
        Assert.Equal(15, Specs.RulesOf(Berserker("berserker-gladiator", "blood-champion", 100, "blood-hero"), Real).OfType<StillRule>().Single().Still);
        Assert.Equal(40, Specs.RulesOf(Berserker("berserker-gladiator", "executor", 100, "headsman"), Real).OfType<ExecuteRule>().Sum(r => r.Percent));
        Assert.Equal(1, Compiled(Berserker("berserker-gladiator", "executor", 100, "beheader"), "path-behead").Cooldown);
    }
}
