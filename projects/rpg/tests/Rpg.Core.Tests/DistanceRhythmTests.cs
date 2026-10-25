using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The branches Distance and Rhythm (sprint 141, D80, T107): the compiled talents (range, minimum, cap, sight, area, push,
/// cooldown, cost, casts per turn, deck), the living ones (still, dancing, cost, first gesture, flat bonus) and the points
/// at the start of a turn (Tight Deck, Reserve, Long Breath); on small boards written here with a class of their own.
/// </summary>
public class DistanceRhythmTests
{
    // Fixed damage, nine casts a turn: a bolt from 1 to 6 cells, a heavy one of 4 AP, a melee one, a cross, a push, a line.
    private static readonly Spell Bolt = new("bolt", Text, 2, 1, 6, true, false, 10, 10, 9, Element.Earth);
    private static readonly Spell Heavy = new("heavy", Text, 4, 1, 6, false, false, 10, 10, 9, Element.Earth);
    private static readonly Spell Jab = new("jab", Text, 2, 1, 1, false, false, 10, 10, 9, Element.Earth);
    private static readonly Spell Burst = new("burst", Text, 3, 1, 6, false, false, 8, 8, 9, Element.Earth, new Area(AreaShape.Cross, 1));
    private static readonly Spell Shove = new("shove", Text, 2, 1, 6, false, false, 1, 1, 9, Element.Earth, null, [new PushEffect(Affects.Enemies, 1)]);
    private static readonly Spell Lance = new("lance", Text, 2, 1, 6, true, true, 10, 10, 9, Element.Earth);
    private static readonly Spell Slow = new("slow", Text, 2, 1, 6, false, false, 12, 12, 9, Element.Earth, Cooldown: 2);
    private static readonly HeroClass Tester = new("tester", Text, Text, 500, 8, 4, 999, ["bolt", "heavy", "jab", "burst", "shove", "lance", "slow"], HpPerLevel: 0);

    private static GameData Data(string[] rows, params FighterSpec[] fighters) =>
        new([.. Spells, .. TreeSpells, Bolt, Heavy, Jab, Burst, Shove, Lance, Slow], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], [Tester],
            summons: TreeSummons, talents: Real.Talents.Values, reactions: Real.Reactions.Values);

    private static Hero Hero(params string[] talents) => new("Ondine", "female-a", "tester", Level: 100, Talents: WithDoor(talents));

    private static FighterSpec Monster(int start = 0, int hp = 300) => Spec(1, start, hp: hp, initiative: 1, spells: ["strike"]);

    private static Fight Play(string[] rows, Hero hero, params FighterSpec[] monsters) =>
        new(Data(rows, [Spec(0, initiative: 999, spells: ["strike"]), .. monsters.Length == 0 ? [Monster()] : monsters]), "s", 1, hero);

    private static Cell C(int x, int y = 0) => new(x, y);

    private static Spell Compiled(Spell s, params string[] talents) => Talents.Compile(s, [.. talents.Select(id => Real.Talents[id])]);

    // ---- the rules of the branches ----

    [Fact]
    public void TheBranches_HoldNineTalentsEach_AndTheirExclusions()
    {
        // Since sprint 147 (D82, E4 a and E5 a): Distance cut into Melee and Distance, Dancing Step in Terrain, Rhythm as Tempo.
        Assert.Equal(4, Real.Talents.Values.Count(t => t.Branch == Branch.Melee));
        Assert.Equal(4, Real.Talents.Values.Count(t => t.Branch == Branch.Distance));
        Assert.Equal(9, Real.Talents.Values.Count(t => t.Branch == Branch.Tempo));
        GameData data = Data(["A.B"], Spec(0), Monster());
        Assert.Contains("exclude each other", Hero("up-close", "long-sight").Problem(data));
        Assert.Contains("exclude each other", Hero("heavy-blows", "light-blows").Problem(data));
        Assert.Contains("exclude each other", Hero("long-sight", "firm-footing", "side-step", "dancing-step", "close-quarters").Problem(data));
        Assert.Null(Hero("up-close", "firm-footing", "heavy-push", "side-step", "dancing-step", "close-quarters").Problem(data));
    }

    // ---- compiled ----

    [Fact]
    public void LongSight_Artillery_CloseQuarters_AndClearLine_ChangeTheRanges()
    {
        Assert.Equal(8, Compiled(Bolt, "long-sight").MaxRange);
        Assert.Equal(1, Compiled(Jab, "long-sight").MaxRange);
        Assert.Equal((3, 8), (Compiled(Bolt, "artillery").MinRange, Compiled(Bolt, "artillery").MaxRange));
        // A melee spell is cast from 3 cells, and its 2 cells more follow: 3 to 5.
        Assert.Equal((3, 5), (Compiled(Jab, "artillery").MinRange, Compiled(Jab, "artillery").MaxRange));
        Assert.Equal(2, Compiled(Bolt, "close-quarters").MaxRange);
        Assert.Equal((5, false), (Compiled(Bolt, "clear-line").MaxRange, Compiled(Lance, "clear-line").LineOfSight));
        Assert.Equal(6, Compiled(Lance, "clear-line").MaxRange);
    }

    [Fact]
    public void TightZone_HeavyPush_ShortCooldown_Thrift_AndOneGesture_ChangeTheSpells()
    {
        Spell tight = Compiled(Burst, "tight-zone");
        Assert.Equal((Area.One, 10), (tight.Zone, tight.DamageMax));
        Spell pushed = Compiled(Shove, "heavy-push");
        Assert.Equal((3, 2), (pushed.ApCost, pushed.AllEffects.OfType<PushEffect>().Single().Cells));
        Assert.Equal(2, Compiled(Bolt, "heavy-push").ApCost);
        Spell quick = Compiled(Slow, "short-cooldown");
        Assert.Equal((1, 9), (quick.Cooldown, quick.DamageMax));
        Assert.Equal((3, 2), (Compiled(Heavy, "thrift").ApCost, Compiled(Bolt, "thrift").ApCost));
        Assert.Equal(1, Compiled(Bolt, "one-gesture").PerTurn);
    }

    [Fact]
    public void TightDeck_AndThrift_ShrinkTheDeck()
    {
        GameData data = Data(["A.B"], Spec(0), Monster());
        Assert.Equal(12, Hero().DeckLimit(data));
        Assert.Equal(10, Hero("first-gesture", "heavy-blows", "tight-deck").DeckLimit(data));
        Assert.Equal(8, Hero("first-gesture", "heavy-blows", "tight-deck", "thrift").DeckLimit(data));
        Assert.Null(Hero("first-gesture", "heavy-blows", "tight-deck", "thrift").Problem(data));
    }

    [Fact]
    public void HeavyPush_HitsEightACellOfCollision()
    {
        // Two talents of the first floor before it (Firm Footing and Long Sight leave a hit of 1 at 1).
        Fight f = Play(["A..B#"], Hero("firm-footing", "long-sight", "heavy-push"));
        f.Apply(new CastAction("shove", C(3)));
        // 1 of damage, then pushed 2 against the obstacle: 2 cells not travelled, 16.
        Assert.Equal(300 - 1 - 16, f.Fighters[1].Hp);
    }

    // ---- living ----

    [Fact]
    public void FirmFooting_RewardsStandingStill_DancingStep_TheCellsWalked()
    {
        // The trunk's 3 % at 100 on every hit (sprint 146): 10 × 1.15 × 1.03, 11.
        Fight still = Play(["A....B"], Hero("firm-footing"));
        still.Apply(new CastAction("bolt", C(5)));
        Assert.Equal(300 - 11, still.Fighters[1].Hp);
        Fight moved = Play(["A....B"], Hero("firm-footing"));
        moved.Apply(new MoveAction(C(1)));
        moved.Apply(new CastAction("bolt", C(5)));
        Assert.Equal(300 - 9, moved.Fighters[1].Hp);
        // With Firm Footing and Long Sight before it: 3 cells walked, +12 % and −5 % (11); then −5 % alone (9).
        Fight dance = Play([".A......B"], Hero("firm-footing", "long-sight", "side-step", "dancing-step"));
        dance.Apply(new MoveAction(C(4)));
        dance.Apply(new CastAction("bolt", C(8)));
        Assert.Equal(300 - 11, dance.Fighters[1].Hp);
        dance.Apply(new CastAction("bolt", C(8)));
        Assert.Equal(300 - 20, dance.Fighters[1].Hp);
    }

    [Fact]
    public void HeavyBlows_FirstGesture_AndOneGesture_ChangeTheHits()
    {
        Fight heavy = Play(["A..B"], Hero("heavy-blows"));
        heavy.Apply(new CastAction("heavy", C(3)));
        heavy.Apply(new CastAction("bolt", C(3)));
        // 11 and 8.
        Assert.Equal(300 - 11 - 8, heavy.Fighters[1].Hp);
        Fight first = Play(["A..B"], Hero("first-gesture"));
        first.Apply(new CastAction("bolt", C(3)));
        first.Apply(new CastAction("bolt", C(3)));
        Assert.Equal(300 - 12 - 9, first.Fighters[1].Hp);
        Fight once = Play(["A..B"], Hero("first-gesture", "heavy-blows", "tight-deck", "one-gesture"));
        once.Apply(new CastAction("lance", C(3)));
        Assert.Equal(ActionError.CastLimit, once.Check(new CastAction("lance", C(3))));
    }

    // ---- points of a turn ----

    [Fact]
    public void TightDeck_Reserve_AndLongBreath_ChangeThePointsOfTheTurns()
    {
        Fight deck = Play(["A..B"], Hero("first-gesture", "heavy-blows", "tight-deck", "reserve"));
        // 8 + 1 (Tight Deck) − 1 (Reserve's first turn).
        Assert.Equal(8, deck.HeroFighter!.Ap);
        deck.Apply(new CastAction("bolt", C(3)));
        deck.Apply(new EndTurnAction());
        deck.Apply(new EndTurnAction());
        // 6 left, 2 kept: 8 + 1 + 2.
        Assert.Equal(11, deck.HeroFighter.Ap);
        Fight breath = Play(["A..B"], Hero("first-gesture", "heavy-blows", "tight-deck", "long-breath"));
        int[] ap = new int[5];
        int[] mp = new int[5];
        for (int turn = 0; turn < 5; turn++)
        {
            (ap[turn], mp[turn]) = (breath.HeroFighter!.Ap, breath.HeroFighter.Mp);
            breath.Apply(new EndTurnAction());
            breath.Apply(new EndTurnAction());
        }
        // 8 + 1 (Tight Deck) − 1 (Long Breath, three turns), then + 1 and + 1 MP from the fourth.
        Assert.Equal([8, 8, 8, 10, 10], ap);
        Assert.Equal([4, 4, 4, 5, 5], mp);
    }

    [Fact]
    public void TheAI_ReadsTheLivingTalents_FromWhereItWouldCast()
    {
        // With Firm Footing, the same hit is worth more from where it stands than after walking.
        Fight f = Play(["A....B"], Hero("firm-footing"));
        Fighter me = f.HeroFighter!;
        Spell bolt = me.Spells.First(s => s.Id == "bolt");
        Assert.True(Ai.Value(f, me, bolt, me.Cell, C(5)) > Ai.Value(f, me, bolt, C(1), C(5)));
        // 10, +10 %, and the trunk's 3 % (sprint 146).
        Assert.Equal(11.33, Fight.Expected(10, me, f.Fighters[1], Element.Earth, 10), 6);
        Assert.Equal(Ai.Value(f, me, bolt, me.Cell, C(5)), Fight.Expected(bolt.AverageDamage, me, f.Fighters[1], Element.Earth, 15), 6);
    }
}
