using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The branch Terrain (sprint 142, D80, T108): Side Step, Backed, Relay, Seal, Remanence (the glyph of a missed spell,
/// Charles's idea), Decoy, Scorched Earth, Nomad and Echo; on small boards written here with a class of their own.
/// </summary>
public class TerrainTests
{
    private static readonly Spell Bolt = new("bolt", Text, 2, 1, 6, false, false, 10, 10, 9, Element.Earth);
    private static readonly Spell Blast = new("blast", Text, 5, 1, 9, false, false, 20, 20, 9, Element.Fire);
    private static readonly HeroClass Tester = new("tester", Text, Text, 500, 20, 6, 999, ["bolt", "blast"], HpPerLevel: 0);

    private static GameData Data(string[] rows, params FighterSpec[] fighters) =>
        new([.. Spells, .. TreeSpells, Bolt, Blast], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], [Tester],
            summons: TreeSummons, talents: Real.Talents.Values, reactions: Real.Reactions.Values);

    // Two talents of the first floor of Terrain under the others, when they need them.
    private static Hero Hero(params string[] talents) => new("Ondine", "female-a", "tester", Level: 100, Talents: talents);

    private static FighterSpec Monster(int start = 0, int hp = 300, int mp = 3, params string[] spells) =>
        Spec(1, start, hp: hp, mp: mp, initiative: 1, spells: spells.Length == 0 ? ["strike"] : spells);

    private static Fight Play(string[] rows, Hero hero, params FighterSpec[] monsters) =>
        new(Data(rows, [Spec(0, initiative: 999, spells: ["strike"]), .. monsters.Length == 0 ? [Monster()] : monsters]), "s", 1, hero);

    private static Cell C(int x, int y = 0) => new(x, y);

    [Fact]
    public void TheBranch_HoldsNineTalents_ThreeByFloor()
    {
        TalentSpec[] terrain = [.. Real.Talents.Values.Where(t => t.Branch == Branch.Terrain)];
        Assert.Equal([3, 3, 3], terrain.GroupBy(t => t.Tier).OrderBy(g => g.Key).Select(g => g.Count()));
    }

    [Fact]
    public void SideStep_GivesAMovementPointAfterASpell_OnceATurn_ForOneLessEachTurn()
    {
        Fight f = Play(["A..B"], Hero("side-step"));
        Fighter me = f.HeroFighter!;
        Assert.Equal(5, me.Mp);
        f.Apply(new CastAction("bolt", C(3)));
        f.Apply(new CastAction("bolt", C(3)));
        Assert.Equal(6, me.Mp);
    }

    [Fact]
    public void Backed_StrikesHarderAndResistsNextToAnObstacle_SofterElsewhere()
    {
        Fight backed = Play(["#A..B"], Hero("backed"));
        backed.Apply(new CastAction("bolt", C(4)));
        Assert.Equal(300 - 11, backed.Fighters[1].Hp);
        Fight open = Play([".A..B"], Hero("backed"));
        open.Apply(new CastAction("bolt", C(4)));
        Assert.Equal(300 - 9, open.Fighters[1].Hp);
        Assert.Equal(10, backed.GuardOf(backed.HeroFighter!));
        Assert.Equal(0, open.GuardOf(open.HeroFighter!));
    }

    [Fact]
    public void Relay_SwapsWithAnAlly_AndCostsARangeCellForTheTurn()
    {
        var data = Data(["A.A..B"], Spec(0, initiative: 999, spells: ["strike"]), Monster());
        var f = new Fight(data, "s", 1, Hero("relay-talent") with { Deck = ["bolt", "relay"] }, allies: [new Rpg.Core.Hero("Romane", "female-b", "tester", Level: 100)]);
        Fighter me = f.HeroFighter!, ally = f.Fighters[2];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("relay", ally.Cell)));
        Assert.Equal((C(2), C(0)), (me.Cell, ally.Cell));
        Assert.Equal(5, me.MaxRangeOf(me.Spells.First(s => s.Id == "bolt")));
    }

    [Fact]
    public void ASeal_MarksTheEnemyWhoStartsItsTurnOnIt_WithoutReacting()
    {
        Fight f = Play(["A...B"], Hero("side-step", "backed", "seal", "bogging") with { Deck = ["bolt", "seal-water"] });
        f.Apply(new CastAction("seal-water", C(4)));
        f.Apply(new CastAction("bolt", C(4)));
        f.Apply(new EndTurnAction());
        // The monster starts its turn on the Seal: marked by the Water, after the Earth's mark of the bolt; no reaction.
        Assert.Equal(Element.Water, f.Fighters[1].Mark!.Element);
        Assert.Empty(f.Events.OfType<ReactionTriggered>());
    }

    [Fact]
    public void Remanence_LaysADecayingGlyphWhereASpellHitsNobody_OneAtATime()
    {
        Fight f = Play(["A.....B.", "........", "........", "........"], Hero("side-step", "backed", "remanence"));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("bolt", C(4, 1))));
        Glyph g = Assert.Single(f.Glyphs);
        // 2 AP: a diamond of radius 2, 30 % of 10 (3), around the cell aimed at.
        Assert.Equal((C(4, 1), 2, 3, Element.Earth, true), (g.Centre, g.TurnsLeft, g.Effect.DamageMax, g.Effect.Element, g.Effect.Decays));
        Assert.Equal(13 - 1, g.Cells.Count);
        Assert.Equal([100, 70, 45, 25], new[] { C(4, 1), C(5, 1), C(6, 1), C(7, 1) }.Select(g.PercentAt));
        // A second miss replaces it; 5 AP: radius 3, 30 % of 20 and 20 % more.
        f.Apply(new CastAction("blast", C(3, 2)));
        Glyph again = Assert.Single(f.Glyphs);
        Assert.Equal((C(3, 2), 7), (again.Centre, again.Effect.DamageMax));
        // A hit on a fighter lays nothing, and is 5 % softer.
        f.Apply(new CastAction("bolt", C(6)));
        Assert.Single(f.Glyphs);
        // 10, −5 % and −10 % (Backed, nothing beside): 8.
        Assert.Equal(300 - 8, f.Fighters[1].Hp);
    }

    [Fact]
    public void ADecoy_HasATenthOfItsCastersLife_ActsNot_AndDrawsTheMonstersNearIt()
    {
        Fight f = Play(["A.B"], Hero("side-step", "backed", "decoy-talent") with { Deck = ["decoy", "bolt"] }, Monster(mp: 0));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("decoy", C(1))));
        Fighter decoy = f.Fighters.Single(x => x.SummonKind == "decoy");
        // A tenth of 525 (the trunk's 5 % since sprint 146).
        Assert.Equal(52, decoy.MaxHp);
        f.Apply(new EndTurnAction());
        Assert.Equal(decoy, f.Current);
        Assert.Equal(new EndTurnAction(), Ai.Decide(f));
        f.Apply(new EndTurnAction());
        // The monster, next to both, strikes the decoy rather than the hero.
        Assert.Equal(new CastAction("strike", C(1)), Ai.Decide(f));
    }

    [Fact]
    public void ScorchedEarth_StrikesHarderInYourGlyphs_SofterElsewhere()
    {
        Fight f = Play(["A....B"], Hero("side-step", "backed", "remanence", "scorched-earth"));
        // Outside: −10 % and −5 % (Remanence), −10 % next to nothing (Backed): 7.
        f.Apply(new CastAction("bolt", C(5)));
        Assert.Equal(300 - 7, f.Fighters[1].Hp);
        f.Apply(new CastAction("bolt", C(4)));
        f.Apply(new CastAction("bolt", C(5)));
        // Inside its Remanence: +20 −5 −10, and the trunk's 3 % (sprint 146): 10.
        Assert.Equal(300 - 7 - 10, f.Fighters[1].Hp);
    }

    [Fact]
    public void Nomad_GivesTwoMovementPoints_ForFifteenPercentOfLife()
    {
        Fight f = Play(["A..B"], Hero("side-step", "backed", "remanence", "nomad"));
        // −15 % and the trunk's +5 % (sprint 146): 90 % of 500.
        Assert.Equal((8 - 1, 450), (f.HeroFighter!.Mp, f.HeroFighter.MaxHp));
    }

    [Fact]
    public void Echo_StrikesAgainAtFortyPercent_FromWhereTheTurnStarted_AfterThreeCellsWalked()
    {
        Fight f = Play(["A.....B"], Hero("side-step", "backed", "remanence", "echo"));
        f.Apply(new CastAction("bolt", C(6)));
        f.Apply(new MoveAction(C(3)));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Echoed echo = Assert.Single(f.Events.OfType<Echoed>());
        Assert.Equal((0, 1, C(0)), (echo.Fighter, echo.Target, echo.From));
        // The bolt (10, −10 and −5 %: 8), then 40 % of a bolt, no talent read (4).
        Assert.Equal(300 - 8 - 4, f.Fighters[1].Hp);
        // Walking less, no echo.
        Fight still = Play(["A.....B"], Hero("side-step", "backed", "remanence", "echo"));
        still.Apply(new CastAction("bolt", C(6)));
        still.Apply(new MoveAction(C(2)));
        still.Apply(new EndTurnAction());
        still.Apply(new EndTurnAction());
        Assert.Empty(still.Events.OfType<Echoed>());
    }
}
