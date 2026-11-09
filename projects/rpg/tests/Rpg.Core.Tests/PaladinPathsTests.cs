using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Paladin's second half (sprint 209, docs/classes-paladin-2.md): the Avenger's vengeance on a small board, the Lightbearer's Aegis that raises, then
/// the Templar and the Avenger, their lineages and eight final classes on the real data.
/// </summary>
public class PaladinPathsTests
{
    private static readonly Spell Hit = new("hit", Text, 1, 1, 1, false, false, 20, 20, 9);
    private static readonly Spell[] All = [.. Spells, Hit];
    private static readonly string[] Prefixed = ["paladin-inquisitor", "paladin-fanatic"];

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    [Fact]
    public void TheAvenger_StrikesHarder_TheMoreItsSideHasSuffered_BeyondTheCeiling()
    {
        Fight f = Play(["AB.A"], Spec(0, hp: 100, ap: 99, mp: 0, initiative: 999, spells: "hit") with { SpecRules = [new VengeanceRule(50, 30), new BonusRule(35)] },
            Spec(1, hp: 900, initiative: 1), Spec(0, 1, hp: 100, initiative: 0));
        Fighter target = f.Fighters[1];
        f.Apply(new CastAction("hit", target.Cell));
        Assert.Equal(873, target.Hp);
        // Its ally down to a fifth of its hit points, itself whole: 40 % missing on average, half of it, 20 % more beyond the ceiling.
        f.Fighters[2].Hp = 20;
        f.Apply(new CastAction("hit", target.Cell));
        Assert.Equal(842, target.Hp);
    }

    private static Hero Paladin(string? spec, string? fork = null, int level = 60, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "paladin", Level: level, Stats: new Characteristics(Earth: 90), Spec: spec, Fork: fork, Final: final, Deck: deck.Length == 0 ? null : deck);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    [Fact]
    public void TheTree_IsProtectorTemplarAvenger_TwelveFinals()
    {
        Specialisation[] branches = [.. Real.Specs.Values.Where(s => s.Class == "paladin")];
        Assert.Equal(["protector", "templar", "avenger"], branches.Select(b => b.Id));
        Assert.Equal(["paladin-rampart", "defender", "paladin-inquisitor", "paladin-crusader", "justiciar", "paladin-fanatic"], branches.SelectMany(b => b.Forks).Select(f => f.Id));
        Assert.Equal(12, branches.SelectMany(b => b.Forks).SelectMany(f => f.Finals!).Count());
        Assert.Equal(12, branches.SelectMany(b => b.Forks.SelectMany(f => b.SpellsWith(f.Id))).Distinct().Count());
        Assert.All(branches.SelectMany(b => b.Forks), f => Assert.NotEqual(f.Finals![0].Template, f.Finals[1].Template));
        // The Paladin's ids beside the Priest's (classes-conception.md §8): the same names, never the same ids.
        Assert.All(Prefixed, id => Assert.DoesNotContain(Real.Specs.Values.Where(s => s.Class == "priest").SelectMany(s => s.Forks), f => f.Id == id));
    }

    [Fact]
    public void TheFinals_ChangeWhatTheirLineageHas()
    {
        Assert.Equal(20, Specs.RulesOf(Paladin("templar", "paladin-inquisitor", 100, "holy-judge"), Real).OfType<TargetStateRule>().Single(r => r.States == 2).Percent);
        Assert.Equal(0, Compiled(Paladin("templar", "paladin-inquisitor", 100, "paladin-purifier"), "path-sentence").Cooldown);
        Assert.Equal(2, Specs.RulesOf(Paladin("templar", "paladin-crusader", 100, "divine-crusader"), Real).OfType<PointsRule>().Single().Ap);
        Assert.Contains(Compiled(Paladin("templar", "paladin-crusader", 100, "lightbearer"), "aegis").AllEffects, e => e is ReviveEffect { Percent: 30 });
        Assert.Contains("spec-bounty", Specs.PassivesOf(Paladin("avenger", "justiciar", 100, "divine-executioner"), Real));
        Assert.Equal(1, Compiled(Paladin("avenger", "justiciar", 100, "hand-of-judgement"), "path-verdict-blow").Cooldown);
        Assert.Equal(2, Compiled(Paladin("avenger", "paladin-fanatic", 100, "paladin-zealot"), "path-holy-fury").Cooldown);
        Assert.Contains(Compiled(Paladin("avenger", "paladin-fanatic", 100, "holy-avatar"), "path-holy-fury").AllEffects, e => e is StatusEffect { Stat: Stat.Damage, Value: 20 });
    }

    [Fact]
    public void TheLightbearersAegis_RaisesAFallenAlly_OnItsCell()
    {
        Hero lightbearer = Paladin("templar", "paladin-crusader", 100, "lightbearer", "aegis");
        Hero warrior = Progression.Built(new Hero("Bastien", "male-c", "warrior", Level: 100), Real);
        var data = new GameData([.. Real.Spells.Values], [new MapSpec("m", Text, ["A.A..B"])], [new Scenario("s", Text, "m", [Spec(0, initiative: 9999), Spec(1, hp: 900, initiative: 1)])], Real.Classes,
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);
        Fight f = new(data, "s", 1, lightbearer, allies: [warrior]);
        Fighter friend = f.HeroFighters[1];
        friend.Hp = 0;
        Assert.False(friend.IsAlive);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("aegis", friend.Cell)));
        Assert.True(friend.IsAlive);
    }
}
