using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The talent tree's vertical slice (sprint 137, D80, T103): its points and rules, the marks a hero's hits leave, the
/// reaction Bogged (Earth then Water, or the other way) and Up close (−2 range, +10 % within 3 cells); on small boards
/// written here, with a class of their own, then on the real data.
/// </summary>
public class TalentTests
{
    // Fixed damage, nine casts a turn, from 1 to 6 cells; a zone of Earth, a neutral spell.
    private static readonly Spell Clod = new("clod", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Earth);
    private static readonly Spell Splash = new("splash", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Water);
    private static readonly Spell Spark = new("spark", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Fire);
    private static readonly Spell Slab = new("slab", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Earth, new Area(AreaShape.Cross, 1));
    private static readonly Spell Poke = new("poke", Text, 1, 1, 1, false, false, 10, 10, 9);
    private static readonly HeroClass Tester = new("tester", Text, Text, 500, 99, 6, 999, ["clod", "splash", "spark", "slab", "poke"], HpPerLevel: 0);

    private static GameData Data(string[] rows, params FighterSpec[] fighters) =>
        new([.. Spells, .. TreeSpells, Clod, Splash, Spark, Slab, Poke], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], [Tester],
            summons: TreeSummons, talents: Real.Talents.Values, reactions: Real.Reactions.Values);

    private static readonly GameData Tree = Data(["A.B"], Spec(0), Monster());

    private static Hero Hero(int level = 30, params string[] talents) =>
        new("Ondine", "female-a", "tester", Level: level, Talents: talents.Length == 0 ? null : talents);

    private static FighterSpec Monster(int start = 0, int hp = 200, bool chief = false) => Spec(1, start, hp: hp, initiative: 1, spells: ["strike"]) with { Chief = chief };

    private static Fight Play(string[] rows, Hero hero, params FighterSpec[] monsters) =>
        new(Data(rows, [Spec(0, initiative: 999, spells: ["strike"]), .. monsters.Length == 0 ? [Monster()] : monsters]), "s", 1, hero);

    private static Cell C(int x, int y = 0) => new(x, y);

    // ---- the tree's rules ----

    [Theory]
    [InlineData(29, 0)]
    [InlineData(30, 2)]
    [InlineData(32, 2)]
    [InlineData(33, 3)]
    [InlineData(45, 7)]
    [InlineData(100, 25)]
    public void TheTree_GivesTwoPointsAtThirty_ThenOneEveryThreeLevels(int level, int points) => Assert.Equal(points, Talents.Points(level));

    [Fact]
    public void TheRealTree_HoldsBoggingAndUpClose_AndTheReactionBogged()
    {
        Assert.True(Real.Talents.ContainsKey("bogging") && Real.Talents.ContainsKey("up-close"));
        TalentSpec bogging = Real.Talents["bogging"], close = Real.Talents["up-close"];
        Assert.Equal((Branch.Reactions, Tier.First, TalentFamily.Manner, 1), (bogging.Branch, bogging.Tier, bogging.Family, bogging.Cost));
        Assert.Equal((Branch.Distance, Tier.First, TalentFamily.Manner, 1), (close.Branch, close.Tier, close.Family, close.Cost));
        ReactionSpec bogged = Real.Reactions["bogged"];
        Assert.True(bogged.Joins(Element.Water, Element.Earth));
        Assert.Equal(new StatusEffect(Affects.Enemies, Stat.Mp, -2, 1), Assert.Single(bogged.Statuses));
        foreach (TalentSpec t in Real.Talents.Values)
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Name.Fr) || string.IsNullOrWhiteSpace(t.Name.En) || string.IsNullOrWhiteSpace(t.Description.Fr) || string.IsNullOrWhiteSpace(t.Description.En));
            Assert.NotEqual(t.Name.Fr, t.Description.Fr);
        }
    }

    [Fact]
    public void AHero_HoldsTalents_FromLevelThirty_WithinItsPoints_EachOnce_KnownOnes()
    {
        Assert.Null(Hero(30, "bogging", "up-close").Problem(Tree));
        Assert.Contains("level 30", Hero(29, "bogging").Problem(Tree));
        Assert.Contains("once", Hero(35, "bogging", "bogging").Problem(Tree));
        Assert.Contains("Unknown talent", Hero(35, "flight").Problem(Tree));
        // No talent at all: as before the tree.
        Assert.Null(Hero(29).Problem(Tree));
        Assert.Null((Hero(10) with { Talents = [] }).Problem(Tree));
    }

    [Fact]
    public void TheRules_CountPoints_Floors_Keys_Exclusions_AndRequirements()
    {
        // A tree of its own, every floor of two branches.
        static TalentSpec T(string id, Branch b, Tier tier, string[]? excludes = null, string[]? requires = null) =>
            new(id, Text, Text, b, tier, [new RangeRule(0)], excludes, requires);
        TalentSpec[] tree =
        [
            T("a1", Branch.Distance, Tier.First, excludes: ["a2"]), T("a2", Branch.Distance, Tier.First), T("a3", Branch.Distance, Tier.First),
            T("a4", Branch.Distance, Tier.Second), T("ak", Branch.Distance, Tier.Key), T("ak2", Branch.Distance, Tier.Key),
            T("b1", Branch.Braids, Tier.First), T("b2", Branch.Braids, Tier.First), T("b3", Branch.Braids, Tier.First), T("bk", Branch.Braids, Tier.Key),
            T("p1", Branch.Poles, Tier.First), T("p2", Branch.Poles, Tier.First), T("p3", Branch.Poles, Tier.First), T("pk", Branch.Poles, Tier.Key),
            T("s1", Branch.Support, Tier.First), T("s2", Branch.Support, Tier.First, requires: ["s1"]),
        ];
        var data = new GameData(Spells, [], [], talents: tree);
        string? P(int level, params string[] ids) => Talents.Problem(ids, level, data);
        Assert.Null(P(30, "a1", "a3"));
        Assert.Contains("At most 2 talent points", P(30, "a1", "a3", "b1"));
        Assert.Contains("exclude each other", P(35, "a1", "a2"));
        // The second floor after 2 points of the first; the key after 3, and it costs 2.
        Assert.Contains("asks 2 points", P(35, "a1", "a4"));
        Assert.Null(P(35, "a1", "a3", "a4"));
        Assert.Contains("asks 3 points", P(45, "a1", "a3", "ak"));
        Assert.Null(P(45, "a1", "a3", "a4", "ak"));
        Assert.Contains("At most 4", P(36, "a1", "a3", "a4", "ak", "b1"));
        Assert.Contains("One key per branch", P(65, "a1", "a3", "a4", "ak", "ak2"));
        // One key of Matter in the whole tree, from two branches of Matter.
        Assert.Null(P(65, "b1", "b2", "b3", "bk", "p1", "p2", "p3"));
        Assert.Contains("One key of Matter", P(80, "b1", "b2", "b3", "bk", "p1", "p2", "p3", "pk"));
        Assert.Contains("asks for 's1'", P(30, "s2"));
        Assert.Null(P(30, "s1", "s2"));
    }

    [Fact]
    public void TheData_RefuseAReactionOfOneElement_TwoReactionsOfAPair_AndATalentOfAnUnknownReaction()
    {
        ReactionSpec bogged = Real.Reactions["bogged"];
        Assert.Throws<InvalidDataException>(() => new GameData(Spells, [], [], reactions: [bogged with { Second = Element.Earth }]));
        Assert.Throws<InvalidDataException>(() => new GameData(Spells, [], [], reactions: [bogged, bogged with { Id = "again", First = Element.Water, Second = Element.Earth }]));
        Assert.Throws<InvalidDataException>(() => new GameData(Spells, [], [], reactions: [bogged with { First = Element.Neutral }]));
        Assert.Throws<InvalidDataException>(() => new GameData(Spells, [], [], talents: [Real.Talents["bogging"]]));
        Assert.Throws<InvalidDataException>(() => new GameData(Spells, [], [], talents: [Real.Talents["up-close"] with { Excludes = ["nothing"] }]));
    }

    // ---- the marks ----

    [Fact]
    public void AHerosHit_MarksTheEnemy_UntilTheEndOfItsNextTurn_EvenWithoutTalents()
    {
        Fight f = Play(["A..B"], Hero());
        Fighter hero = f.HeroFighter!, monster = f.Fighters[1];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("clod", C(3))));
        Assert.Equal(new Mark(Element.Earth, hero.Id, 2), monster.Mark);
        Assert.Contains(new Marked(1, Element.Earth, 0), f.Events);
        // Another element: the new mark in place of the old one, nothing else.
        f.Apply(new CastAction("splash", C(3)));
        Assert.Equal(Element.Water, monster.Mark!.Element);
        Assert.DoesNotContain(f.Events, e => e is ReactionTriggered);
        Assert.Empty(monster.Statuses);
        // The end of this turn, then the monster's: still there; gone at the end of the hero's next one.
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(f.HeroFighter, f.Current);
        Assert.NotNull(monster.Mark);
        f.Apply(new EndTurnAction());
        Assert.Null(monster.Mark);
        Assert.Contains(new MarkEnded(1), f.Events);
    }

    [Fact]
    public void NeitherMonstersNorNeutralSpells_LeaveMarks_NorAHeroOnItsOwnSide()
    {
        // A fight without a hero: nobody marks.
        Fight alone = new(Data(["A.B"], Spec(0, initiative: 999, spells: ["clod"]), Monster(0)), "s", 1);
        alone.Apply(new CastAction("clod", C(2)));
        Assert.Null(alone.Fighters[1].Mark);
        // A neutral hit marks nothing; the monster's hit on the hero neither.
        Fight f = Play(["AB"], Hero());
        f.Apply(new CastAction("poke", C(1)));
        Assert.Null(f.Fighters[1].Mark);
        f.Apply(new EndTurnAction());
        Assert.Null(f.HeroFighter!.Mark);
    }

    // ---- Bogged ----

    [Fact]
    public void WithBogging_WaterOnAnEarthMark_Bogs_TheMarkUsedUp_MinusTwoMovementPointsForOneTurn()
    {
        Fight f = Play(["A....B"], Hero(30, "bogging"));
        Fighter monster = f.Fighters[1];
        f.Apply(new CastAction("clod", C(5)));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("splash", C(5))));
        Assert.Contains(new ReactionTriggered(1, "bogged", 0), f.Events);
        Assert.Null(monster.Mark);
        Assert.Equal(new Status(Stat.Mp, -2, 1, Element.Neutral, 0, "bogged"), Assert.Single(monster.Statuses));
        Assert.Contains(f.Events, e => e is StatusAdded { Fighter: 1, Stat: Stat.Mp, Value: -2, Reaction: "bogged" });
        // A third hit marks again, it does not react again on nothing.
        f.Apply(new CastAction("clod", C(5)));
        Assert.Equal(Element.Earth, monster.Mark!.Element);
        Assert.Single(f.Events.OfType<ReactionTriggered>());
        // Its turn: 3 movement points less 2.
        f.Apply(new EndTurnAction());
        Assert.Equal(monster, f.Current);
        Assert.Equal(1, monster.Mp);
        f.Apply(new EndTurnAction());
        Assert.Empty(monster.Statuses);
    }

    [Fact]
    public void Bogging_GoesBothWays_ButNotWithFire_NorWithoutTheTalent()
    {
        Fight water = Play(["A..B"], Hero(30, "bogging"));
        water.Apply(new CastAction("splash", C(3)));
        water.Apply(new CastAction("clod", C(3)));
        Assert.Single(water.Events.OfType<ReactionTriggered>());
        Fight fire = Play(["A..B"], Hero(30, "bogging"));
        fire.Apply(new CastAction("clod", C(3)));
        fire.Apply(new CastAction("spark", C(3)));
        Assert.Empty(fire.Events.OfType<ReactionTriggered>());
        Assert.Equal(Element.Fire, fire.Fighters[1].Mark!.Element);
        Fight none = Play(["A..B"], Hero(30, "up-close"));
        none.Apply(new CastAction("clod", C(3)));
        none.Apply(new CastAction("splash", C(3)));
        Assert.Empty(none.Events.OfType<ReactionTriggered>());
    }

    [Fact]
    public void AZone_ReactsOnTheFighterAimedAt_Only_AndMarksTheOthers()
    {
        Fight f = Play(["A...BB"], Hero(30, "bogging"), Monster(0), Monster(1));
        f.Apply(new CastAction("splash", C(4)));
        f.Apply(new CastAction("splash", C(5)));
        Assert.Equal(Element.Water, f.Fighters[1].Mark!.Element);
        Assert.Equal(Element.Water, f.Fighters[2].Mark!.Element);
        // The cross around (4, 0) touches both: the one aimed at reacts, the other is marked by the Earth.
        f.Apply(new CastAction("slab", C(4)));
        Assert.Equal([1], f.Events.OfType<ReactionTriggered>().Select(r => r.Fighter));
        Assert.Null(f.Fighters[1].Mark);
        Assert.Equal(Element.Earth, f.Fighters[2].Mark!.Element);
    }

    [Fact]
    public void AChief_LosesAtMostTwoMovementPoints_ToTheReactions()
    {
        // Two heroes' reactions on a chief: the second adds nothing beyond −2.
        Fight f = new(Data(["A..B.", ".....", "A...."], Spec(0, initiative: 999, spells: ["strike"]), Monster(0, chief: true)), "s", 1,
            Hero(30, "bogging"), allies: [Hero(30, "bogging") with { Name = "Romane" }]);
        Fighter chief = f.Fighters[1];
        f.Apply(new CastAction("clod", chief.Cell));
        f.Apply(new CastAction("splash", chief.Cell));
        f.Apply(new EndTurnAction());
        Fighter ally = f.Current;
        Assert.NotEqual(f.HeroFighter, ally);
        f.Apply(new CastAction("clod", chief.Cell));
        f.Apply(new CastAction("splash", chief.Cell));
        Assert.Equal(2, f.Events.OfType<ReactionTriggered>().Count());
        Assert.Equal(-2, chief.Statuses.Where(s => s.Stat == Stat.Mp).Sum(s => s.Value));
    }

    [Fact]
    public void TheForecast_TellsTheReaction_BeforeTheCast()
    {
        Fight f = Play(["A..B"], Hero(30, "bogging"));
        Assert.Null(Assert.Single(f.Foresee(Splash, C(3))).Reaction);
        f.Apply(new CastAction("clod", C(3)));
        Assert.Equal("bogged", Assert.Single(f.Foresee(f.HeroFighter!.Spells.First(s => s.Id == "splash"), C(3))).Reaction);
        Assert.Null(Assert.Single(f.Foresee(f.HeroFighter!.Spells.First(s => s.Id == "spark"), C(3))).Reaction);
    }

    // ---- Up close ----

    [Fact]
    public void UpClose_TakesTwoCellsOfRange_NeverUnderTheMinimumNorUnderOne()
    {
        Fight f = Play(["A..B"], Hero(30, "up-close"));
        Assert.Equal(4, f.HeroFighter!.Spells.First(s => s.Id == "clod").MaxRange);
        Assert.Equal(1, f.HeroFighter!.Spells.First(s => s.Id == "poke").MaxRange);
        Spell longShot = new("long", Text, 1, 3, 4, false, false, 1, 1, 1);
        Assert.Equal(3, Talents.Compile(longShot, [Real.Talents["up-close"]]).MaxRange);
        Assert.Equal(1, Talents.Compile(longShot with { MinRange = 0, MaxRange = 2 }, [Real.Talents["up-close"]]).MaxRange);
        Assert.Equal(0, Talents.Compile(longShot with { MinRange = 0, MaxRange = 0 }, [Real.Talents["up-close"]]).MaxRange);
    }

    [Fact]
    public void UpClose_HitsTenPercentHarder_WithinThreeCells_NotFarther()
    {
        // 10 damage: 11 at 3 cells, 10 at 4 (the range of 6 less 2).
        Fight near = Play(["A..B"], Hero(30, "up-close"));
        near.Apply(new CastAction("clod", C(3)));
        Assert.Equal(189, near.Fighters[1].Hp);
        Fight far = Play(["A...B"], Hero(30, "up-close"));
        far.Apply(new CastAction("clod", C(4)));
        Assert.Equal(190, far.Fighters[1].Hp);
        Assert.Equal(ActionError.OutOfRange, Play(["A....B"], Hero(30, "up-close")).Check(new CastAction("clod", C(5))));
        Assert.Equal((11, 11), (near.Foresee(Clod, C(3)).Single().DamageMin, Play(["A..B"], Hero(30, "up-close")).Foresee(Clod, C(3)).Single().DamageMax));
    }

    [Fact]
    public void TheLivingBonuses_StopAtThirtyFivePercent()
    {
        TalentSpec big = new("big", Text, Text, Branch.Distance, Tier.First, [new DistanceRule(0, 9, 30)]);
        Assert.Equal(35, Talents.Percent([big, big with { Id = "more" }], C(0), C(2)));
        Assert.Equal(0, Talents.Percent([big], C(0), C(10)));
    }

    // ---- the record and the AI ----

    [Fact]
    public void AFightWithTalents_IsReplayedTheSame_FromItsRecord()
    {
        Fight f = new(Real, "group:fields-0-4.0", 4, Progression.Built(new Rpg.Core.Hero("Ondine", "female-a", "sentinel", Level: 35, Talents: ["bogging", "up-close"]), Real));
        Ai.PlayOut(f);
        Assert.True(f.IsOver);
        string json = FightRecord.Of(f).ToJson();
        Assert.Contains("\"talents\":[\"bogging\",\"up-close\"]", json);
        Assert.DoesNotContain("talentsTaken", json);
        Assert.Equal(Fingerprint(f), Fingerprint(FightRecord.FromJson(json).Replay(Real)));
    }

    [Fact]
    public void TheAI_SetsOffTheReaction_WhenItCan()
    {
        // Marked by the Earth, the monster out of the hero's reach to walk: Water is worth the reaction.
        Fight f = Play(["A....B"], Hero(30, "bogging"));
        f.Apply(new CastAction("clod", C(5)));
        Assert.Equal(new CastAction("splash", C(5)), Ai.Decide(f));
    }

    [Fact]
    public void TheHeroEquality_IgnoresTheOrderOfTheTalents()
    {
        Assert.Equal(Hero(35, "bogging", "up-close"), Hero(35, "up-close", "bogging"));
        Assert.NotEqual(Hero(35, "bogging"), Hero(35, "up-close"));
        Assert.Equal(Hero(35), Hero(35) with { Talents = [] });
    }
}
