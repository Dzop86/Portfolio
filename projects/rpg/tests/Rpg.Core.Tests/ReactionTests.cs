using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The six reactions (sprint 140, D80, T106): each pair's state, Electrified's jump and its one target by caster, Blinded's
/// range, a chief's cap; the limit of two elements opened and the price of giving talents back.
/// </summary>
public class ReactionTests
{
    private static readonly Spell Clod = new("clod", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Earth);
    private static readonly Spell Ember = new("ember", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Fire);
    private static readonly Spell Splash = new("splash", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Water);
    private static readonly Spell Gust = new("gust", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Air);
    private static readonly Spell Poke = new("poke", Text, 1, 1, 6, false, false, 10, 10, 9);
    private static readonly HeroClass Tester = new("tester", Text, Text, 500, 99, 6, 999, ["clod", "ember", "splash", "gust", "poke"], HpPerLevel: 0);

    private static readonly string[] All = ["bogging", "chilling", "cracking", "electrifying", "fogging", "blinding"];

    private static GameData Data(string[] rows, params FighterSpec[] fighters) =>
        new([.. Spells, .. TreeSpells, Clod, Ember, Splash, Gust, Poke], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], [Tester],
            summons: TreeSummons, talents: Real.Talents.Values, reactions: Real.Reactions.Values);

    private static Hero Hero(params string[] talents) => new("Ondine", "female-a", "tester", Level: 100, Talents: talents);

    private static FighterSpec Monster(int start = 0, int hp = 300, bool chief = false, params string[] spells) =>
        Spec(1, start, hp: hp, initiative: 1, spells: spells.Length == 0 ? ["strike"] : spells) with { Chief = chief };

    private static Fight Play(string[] rows, Hero hero, params FighterSpec[] monsters) =>
        new(Data(rows, [Spec(0, initiative: 999, spells: ["strike"]), .. monsters.Length == 0 ? [Monster()] : monsters]), "s", 1, hero);

    private static Cell C(int x, int y = 0) => new(x, y);

    [Theory]
    [InlineData("clod", "splash", "bogged", Stat.Mp, -2, 1)]
    [InlineData("splash", "gust", "chilled", Stat.Ap, -2, 1)]
    [InlineData("ember", "clod", "cracked", Stat.Resistance, -15, 2)]
    [InlineData("gust", "ember", "electrified", Stat.Electrified, 35, 1)]
    [InlineData("splash", "ember", "fogged", Stat.Damage, -25, 1)]
    [InlineData("clod", "gust", "blinded", Stat.Range, -2, 1)]
    public void EachPair_SetsOffItsReaction(string first, string second, string reaction, Stat stat, int value, int turns)
    {
        Fight f = Play(["A..B"], Hero(All));
        f.Apply(new CastAction(first, C(3)));
        f.Apply(new CastAction(second, C(3)));
        Assert.Contains(new ReactionTriggered(1, reaction, 0), f.Events);
        Assert.Equal(new Status(stat, value, turns, Element.Neutral, 0, reaction), Assert.Single(f.Fighters[1].Statuses));
    }

    [Fact]
    public void TheRealReactions_AreTheSixPairs_OneTalentEach()
    {
        Assert.Equal(6, Real.Reactions.Count);
        Element[] four = [Element.Earth, Element.Fire, Element.Water, Element.Air];
        foreach (Element a in four)
        {
            foreach (Element b in four.Where(b => b > a))
                Assert.Single(Real.Reactions.Values, r => r.Joins(a, b));
        }
        Assert.All(Real.Reactions.Keys, id => Assert.Single(Real.Talents.Values, t => t.Rules.OfType<ReactionRule>().Any(r => r.Reaction == id)));
    }

    [Fact]
    public void Electrified_MakesEveryHitItTakesJump_OnOneTargetAtATimeByCaster()
    {
        Fight f = Play(["A..BB.B"], Hero(All), Monster(0), Monster(1), Monster(2));
        f.Apply(new CastAction("gust", C(3)));
        f.Apply(new CastAction("ember", C(3)));
        Assert.Contains(f.Fighters[1].Statuses, s => s.Stat == Stat.Electrified);
        int before = f.Fighters[2].Hp;
        // A neutral hit on the electrified one: 35 % of 10 (3) on its neighbour.
        f.Apply(new CastAction("poke", C(3)));
        Assert.Equal(before - 3, f.Fighters[2].Hp);
        // Electrifying the neighbour takes it off the first.
        f.Apply(new CastAction("gust", C(4)));
        f.Apply(new CastAction("ember", C(4)));
        Assert.DoesNotContain(f.Fighters[1].Statuses, s => s.Stat == Stat.Electrified);
        Assert.Contains(f.Fighters[2].Statuses, s => s.Stat == Stat.Electrified);
    }

    [Fact]
    public void Blinded_TakesTwoCellsOfRange_ToSpellsReachingFarther_NeverUnderOne()
    {
        // The monster, blinded, casts its 6-cell spell no farther than 4.
        Fight f = Play(["A....B"], Hero(All), Monster(spells: ["poke", "strike"]));
        f.Apply(new CastAction("clod", C(5)));
        f.Apply(new CastAction("gust", C(5)));
        Fighter monster = f.Fighters[1];
        Assert.Equal(4, monster.MaxRangeOf(Poke));
        Assert.Equal(1, monster.MaxRangeOf(Strike));
        f.Apply(new EndTurnAction());
        Assert.Equal(ActionError.OutOfRange, f.Check(new CastAction("poke", f.HeroFighter!.Cell)));
    }

    [Fact]
    public void AChief_LosesAtMostTwoActionPoints_ToChilledAndBogged_Each()
    {
        Fight f = Play(["A..B"], Hero(All), Monster(chief: true));
        f.Apply(new CastAction("splash", C(3)));
        f.Apply(new CastAction("gust", C(3)));
        f.Apply(new CastAction("clod", C(3)));
        f.Apply(new CastAction("splash", C(3)));
        Assert.Equal([(Stat.Ap, -2), (Stat.Mp, -2)], f.Fighters[1].Statuses.Select(s => (s.Stat, s.Value)).Order());
    }

    [Fact]
    public void AHero_OpensTwoElementsAtMost_AndGivingTalentsBack_CostsAfterForty()
    {
        Assert.Equal(1, Talents.ElementsOpened([Real.Talents["open-lightning"], Real.Talents["convert-lightning"]]));
        Assert.Equal(0, Talents.ElementsOpened([Real.Talents["bogging"]]));
        Assert.Equal(2, Talents.MaxOpened);
        Assert.Equal((0, 0, 410), (Talents.ChangeCost(30), Talents.ChangeCost(40), Talents.ChangeCost(41)));
        Assert.True(Talents.GivesBack(["bogging", "up-close"], ["up-close", "chilling"]));
        Assert.False(Talents.GivesBack(["bogging"], ["bogging", "chilling"]));
        Assert.False(Talents.GivesBack(null, ["bogging"]));
    }
}
