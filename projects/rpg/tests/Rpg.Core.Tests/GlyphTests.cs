using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The glyphs (sprint 130, T98): laid on the cells of a spell's area, they strike who walks onto them, lands on
/// them or starts its turn there, once per turn of play; the board's path goes round them, the AI too, and it
/// lays them; on small boards written here.
/// </summary>
public class GlyphTests
{
    // Fixed numbers where a test counts hit points; nine casts a turn, so that one fighter can do it all.
    private static readonly Spell Ward = new("ward", Text, 1, 0, 9, false, false, 0, 0, 9, Element.Earth, null, [new GlyphEffect(Affects.Enemies, 2, 10, 10, Element.Earth)]);
    private static readonly Spell Field = new("field", Text, 1, 0, 9, false, false, 0, 0, 9, Element.Earth, new Area(AreaShape.Cross, 1), [new GlyphEffect(Affects.Enemies, 2, 5, 5, Element.Earth)]);
    private static readonly Spell Mire = new("mire", Text, 1, 0, 9, false, false, 0, 0, 9, Element.Earth, null, [new GlyphEffect(Affects.Enemies, 2, Status: Stat.Mp, Value: -9, StatusTurns: 1)]);
    private static readonly Spell Venom = new("venom", Text, 1, 1, 9, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Poison, 6, 2)]);
    private static readonly Spell Shove = new("shove", Text, 1, 1, 9, false, false, 0, 0, 9, Element.Neutral, null, [new PushEffect(Affects.Enemies, 2)]);
    private static readonly Spell Hit = new("hit", Text, 1, 1, 9, false, false, 10, 10, 9);
    private static readonly Spell Jump = new("jump", Text, 1, 1, 9, false, false, 0, 0, 9, Element.Neutral, null, [new LeapEffect(Affects.Caster)]);
    private static readonly Spell[] All = [.. Spells, Ward, Field, Mire, Venom, Shove, Hit, Jump];

    private static readonly PassiveSpec Mend = new("mend", Text, Text, Trigger.TurnStart, [new HealEffect(Affects.Caster, 1, 1)]);
    private static readonly PassiveSpec Low = new("low", Text, Text, Trigger.LowHp, [new ShieldEffect(Affects.Caster, 1, 1)]) { Percent = 90 };

    private static GameData Data(string[] rows, params FighterSpec[] fighters) =>
        new(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], passives: [Mend, Low]);

    private static Fight Play(string[] rows, params FighterSpec[] fighters) => new(Data(rows, fighters), "s", 1);

    /// <summary>Team A's caster: plays first, every point it needs, walks if asked.</summary>
    private static FighterSpec Caster(int mp = 6, params string[] spells) =>
        Spec(0, ap: 99, mp: mp, initiative: 999, spells: spells.Length == 0 ? ["ward", "field", "mire", "venom", "shove", "hit", "jump"] : spells);

    private static FighterSpec Monster(int start = 0, int hp = 50, int mp = 3, params string[] spells) =>
        Spec(1, start, hp: hp, mp: mp, spells: spells.Length == 0 ? ["strike"] : spells);

    private static Cell C(int x, int y = 0) => new(x, y);

    private static int Struck(Fight f, int fighter) => f.Events.OfType<GlyphTriggered>().Count(g => g.Fighter == fighter);

    [Fact]
    public void AGlyph_IsLaidOnTheFloorCellsOfTheArea_AndStrikesNobodyAsItIsLaid()
    {
        Fight f = Play(["A.#.B"], Caster(), Monster());
        Assert.Equal(ActionError.None, f.Apply(new CastAction("field", C(3))));
        Glyph g = Assert.Single(f.Glyphs);
        // The cross of 1 around (3, 0), on a board of one row: the obstacle and the off-board cells are left out; the monster stands on it.
        Assert.Equal([C(3), C(4)], g.Cells);
        Assert.Equal(2, g.TurnsLeft);
        Assert.Contains(f.Events, e => e is GlyphPlaced { Glyph: 0, Caster: 0, Spell: "field" } p && p.Cells.SequenceEqual(g.Cells));
        Assert.Equal(0, Struck(f, 1));
        Assert.Equal(50, f.Fighters[1].Hp);
    }

    [Fact]
    public void WalkingOntoAGlyph_Strikes_TheWalkToldInTwoParts()
    {
        Fight f = Play(["A....B"], Caster(), Monster());
        f.Apply(new CastAction("ward", C(3)));
        f.Apply(new EndTurnAction());
        Assert.Equal(ActionError.None, f.Apply(new MoveAction(C(2))));
        Assert.Equal(40, f.Fighters[1].Hp);
        Assert.Equal(C(2), f.Fighters[1].Cell);
        Assert.Equal(0, f.Fighters[1].Mp);
        Moved[] walks = [.. f.Events.OfType<Moved>()];
        Assert.Equal([[C(4), C(3)], [C(2)]], walks.Select(m => m.Path.ToArray()));
        int glyph = f.Events.ToList().FindIndex(e => e is GlyphTriggered);
        Assert.Equal(new Damaged(1, 10, 40, Element.Earth), f.Events[glyph + 1]);
        Assert.IsType<Moved>(f.Events[glyph + 2]);
    }

    [Fact]
    public void AGlyph_StrikesAFighterOncePerTurnOfPlay_HoweverManyOfItsCellsItWalks()
    {
        Fight f = Play(["A.....B"], Caster(), Monster(mp: 5));
        f.Apply(new CastAction("field", C(4)));
        f.Apply(new EndTurnAction());
        // (5, 0), (4, 0) and (3, 0) are the glyph's: one strike of 5.
        f.Apply(new MoveAction(C(2)));
        Assert.Equal(1, Struck(f, 1));
        Assert.Equal(45, f.Fighters[1].Hp);
    }

    [Fact]
    public void AGlyph_StrikesAtTheStartOfTheTurn_AfterThePoisons_BeforeThePassives()
    {
        // The glyph laid beside the monster, which steps onto it and stays: its next turn starts there.
        Fight f = Play(["A..B"], Caster(), Monster(mp: 1) with { Passives = ["mend"] });
        f.Apply(new CastAction("ward", C(2)));
        f.Apply(new EndTurnAction());
        f.Apply(new MoveAction(C(2)));
        f.Apply(new EndTurnAction());
        f.Apply(new CastAction("venom", C(2)));
        int before = f.Events.Count;
        f.Apply(new EndTurnAction());
        List<FightEvent> events = [.. f.Events.Skip(before)];
        int poison = events.FindIndex(e => e is Damaged { Fighter: 1, Amount: 6 });
        int glyph = events.FindIndex(e => e is GlyphTriggered { Fighter: 1 });
        int mend = events.FindIndex(e => e is PassiveTriggered { Fighter: 1, Passive: "mend" });
        Assert.True(poison >= 0 && glyph > poison && mend > glyph, string.Join(" ", events));
        Assert.Equal(f.Fighters[1].Hp - 1, ((Damaged)events[glyph + 1]).HpLeft);
    }

    [Fact]
    public void AGlyphLaidUnderAFighter_SparesItAtItsNextTurn_SoItCanWalkOff()
    {
        // Sprint 152 (D83, the limit of T100): already there when it was laid, the monster is not struck as its turn starts.
        Fight f = Play(["A.B."], Caster(), Monster());
        f.Apply(new CastAction("ward", C(2)));
        Assert.Equal(ActionError.None, f.Apply(new EndTurnAction()));
        Assert.Equal(0, Struck(f, 1));
        Assert.Equal(50, f.Fighters[1].Hp);
        // It stays: the turn after, it is struck.
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(1, Struck(f, 1));
        Assert.Equal(40, f.Fighters[1].Hp);
    }

    [Fact]
    public void AGlyphsDamage_IsNoSpells_YetALowHealthPassiveFiresAfterIt()
    {
        Fight f = Play(["A.B"], Caster(), Monster() with { Passives = ["low"] });
        f.Apply(new CastAction("ward", C(2)));
        // Spared at its first turn (sprint 152), struck at its second.
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        List<FightEvent> events = [.. f.Events];
        int glyph = events.FindIndex(e => e is GlyphTriggered);
        int low = events.FindIndex(e => e is PassiveTriggered { Passive: "low" });
        Assert.True(glyph >= 0 && low > glyph);
    }

    [Fact]
    public void AGlyph_LastsItsTurns_OfItsCaster_ThenVanishes()
    {
        Fight f = Play(["A.B"], Caster(), Monster());
        f.Apply(new CastAction("ward", C(1)));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        // The caster's next turn: one left.
        Assert.Equal(1, Assert.Single(f.Glyphs).TurnsLeft);
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Empty(f.Glyphs);
        Assert.Contains(f.Events, e => e is GlyphEnded { Glyph: 0 });
    }

    [Fact]
    public void AGlyph_LaidAgain_ReplacesTheCastersLastOfThatSpell()
    {
        Fight f = Play(["AB."], Caster(), Monster());
        f.Apply(new CastAction("ward", C(2)));
        f.Apply(new CastAction("ward", C(2)));
        Assert.Equal(1, Assert.Single(f.Glyphs).Id);
        Assert.Contains(f.Events, e => e is GlyphEnded { Glyph: 0 });
        // Another spell's glyph adds to it.
        f.Apply(new CastAction("field", C(2)));
        Assert.Equal(2, f.Glyphs.Count);
        Assert.Equal(2, f.GlyphsAt(C(2)).Count);
    }

    [Fact]
    public void AGlyph_VanishesWithItsCaster()
    {
        // The monster lays the glyph; the hero kills it.
        Fight g = Play(["A..BB"], Spec(0, ap: 99, initiative: 1, spells: ["hit"]), Monster(0, hp: 10, spells: ["ward"]) with { Initiative = 999 }, Monster(1));
        g.Apply(new CastAction("ward", C(1)));
        g.Apply(new EndTurnAction());
        g.Apply(new EndTurnAction());
        Assert.Single(g.Glyphs);
        g.Apply(new CastAction("hit", C(3)));
        Assert.False(g.Fighters[1].IsAlive);
        Assert.Empty(g.Glyphs);
        Assert.Contains(g.Events, e => e is GlyphEnded { Glyph: 0 });
    }

    [Fact]
    public void AGlyph_SparesTheSideItDoesNotTouch()
    {
        Fight f = Play(["A...B"], Caster(), Monster());
        f.Apply(new CastAction("ward", C(1)));
        f.Apply(new MoveAction(C(2)));
        Assert.Equal(0, Struck(f, 0));
        Assert.Equal(50, f.Fighters[0].Hp);
    }

    [Fact]
    public void LandingOnAGlyph_Strikes_PushedOrLeaping()
    {
        Fight f = Play(["AB...", "....."], Caster(), Monster());
        f.Apply(new CastAction("ward", C(3)));
        f.Apply(new CastAction("shove", C(1)));
        Assert.Equal(C(3), f.Fighters[1].Cell);
        Assert.Equal(1, Struck(f, 1));
        // A monster leaps onto the hero's glyph.
        Fight g = Play(["A...B"], Caster(), Monster(spells: ["jump"]));
        g.Apply(new CastAction("ward", C(2)));
        g.Apply(new EndTurnAction());
        Assert.Equal(ActionError.None, g.Apply(new CastAction("jump", C(2))));
        Assert.Equal(1, Struck(g, 1));
        Assert.Equal(40, g.Fighters[1].Hp);
    }

    [Fact]
    public void AGlyphThatKills_EndsTheWalk_AndTheTurn()
    {
        Fight f = Play(["A....BB"], Caster(), Monster(0, hp: 8), Monster(1));
        f.Apply(new CastAction("ward", C(3)));
        f.Apply(new EndTurnAction());
        Assert.Equal(1, f.Current.Id);
        f.Apply(new MoveAction(C(2)));
        Assert.False(f.Fighters[1].IsAlive);
        Assert.Equal(C(3), f.Fighters[1].Cell);
        Assert.Equal(2, f.Current.Id);
    }

    [Fact]
    public void AGlyphThatTakesTheMovementPoints_StopsTheWalk()
    {
        Fight f = Play(["A....B"], Caster(), Monster());
        f.Apply(new CastAction("mire", C(4)));
        f.Apply(new EndTurnAction());
        f.Apply(new MoveAction(C(2)));
        Assert.Equal(C(4), f.Fighters[1].Cell);
        Assert.Equal(0, f.Fighters[1].Mp);
        Assert.Contains(f.Events, e => e is StatusAdded { Fighter: 1, Stat: Stat.Mp, Value: -9 });
    }

    [Fact]
    public void ThePath_GoesRoundAGlyph_WhenTheMovementPointsAllowIt()
    {
        string[] rows = ["A....B", "......"];
        Fight f = Play(rows, Caster(), Monster(mp: 5));
        f.Apply(new CastAction("ward", C(3)));
        f.Apply(new EndTurnAction());
        // The shortest walk to (2, 0) is 3 steps through the glyph; 5 points go round it by the row below.
        IReadOnlyList<Cell> path = f.PathTo(C(2))!;
        Assert.DoesNotContain(C(3), path);
        Assert.Equal(5, path.Count);
        f.Apply(new MoveAction(C(2)));
        Assert.Equal(0, Struck(f, 1));
        Assert.Equal(C(2), f.Fighters[1].Cell);
        // With 3 points only, it walks through.
        Fight g = Play(rows, Caster(), Monster(mp: 3));
        g.Apply(new CastAction("ward", C(3)));
        g.Apply(new EndTurnAction());
        Assert.Contains(C(3), g.PathTo(C(2))!);
        // A glyph the walker does not fear changes nothing: the shortest path, as before.
        Fight h = Play(rows, Caster(), Monster());
        h.Apply(new CastAction("ward", C(2)));
        Assert.Equal(Pathfinding.FindPath(h.Board, C(0), C(4), h.IsFree), h.PathTo(C(4)));
    }

    [Fact]
    public void ATollPath_IsTheCheapestThenTheShortest_WithinItsSteps()
    {
        Board board = Board.Parse(["....", "....", "...."]);
        Func<Cell, bool> free = _ => true;
        // A wall of tolls across the middle row but one cell.
        int Toll(Cell c) => c.Y == 1 && c.X != 3 ? 1 : 0;
        IReadOnlyList<Cell> round = Pathfinding.FindPath(board, C(0, 0), C(0, 2), free, 8, Toll)!;
        Assert.Equal(8, round.Count);
        Assert.Equal(0, round.Sum(Toll));
        IReadOnlyList<Cell> through = Pathfinding.FindPath(board, C(0, 0), C(0, 2), free, 2, Toll)!;
        Assert.Equal([C(0, 1), C(0, 2)], through);
        Assert.Null(Pathfinding.FindPath(board, C(0, 0), C(0, 2), free, 1, Toll));
    }

    [Fact]
    public void AFightWithGlyphs_ReplaysIdentically_ThroughItsRecord()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            GameData data = Data(["A.......", "..#..#..", ".......B", "......BB"], Caster(spells: ["field", "ward", "hit"]), Monster(0, spells: ["field", "strike"]), Monster(1), Monster(2));
            var f = new Fight(data, "s", seed);
            Ai.PlayOut(f);
            Fight again = FightRecord.FromJson(FightRecord.Of(f).ToJson()).Replay(data);
            Assert.Equal(Fingerprint(f), Fingerprint(again));
        }
    }

    [Fact]
    public void TheAi_GoesRoundAGlyph_AndStepsOffOne()
    {
        // The monster walks to the hero: round the glyph by the row below, which costs no more steps.
        Fight f = Play(["A....B", "......"], Caster(), Monster(mp: 3));
        f.Apply(new CastAction("ward", C(4)));
        f.Apply(new EndTurnAction());
        FightAction walk = Ai.Decide(f);
        f.Apply(walk);
        Assert.Equal(0, Struck(f, 1));
        Assert.True(f.Fighters[1].Cell.DistanceTo(C(0)) < 5, $"{walk}");
        // Laid under a monster with nothing to cast: it walks off before its turn ends.
        Fight g = Play([".....A", "B....."], Caster(), Monster(mp: 3, spells: ["hit"]) with { Ap = 0 });
        g.Apply(new CastAction("field", C(0, 1)));
        g.Apply(new EndTurnAction());
        // Spared as its turn starts since sprint 152: it has the time to leave.
        Assert.Equal(0, Struck(g, 1));
        FightAction off = Ai.Decide(g);
        Assert.IsType<MoveAction>(off);
        g.Apply(off);
        Assert.Empty(g.GlyphsAt(g.Fighters[1].Cell));
    }

    [Fact]
    public void TheAi_LaysAGlyphUnderAnEnemy()
    {
        Fight f = Play(["B...A"], Spec(0, initiative: 1), Monster(spells: ["ward"]) with { Initiative = 999, Mp = 0 });
        Assert.Equal(new CastAction("ward", C(4)), Ai.Decide(f));
    }

    [Fact]
    public void AGlyph_IsChecked_AsTheDataLoads()
    {
        string[] rows = ["AB"];
        GameData With(GlyphEffect g) => new([Ward with { Effects = [g] }], [new MapSpec("m", Text, rows)], []);
        Assert.NotNull(With(new GlyphEffect(Affects.All, 10, 1, 2)));
        Assert.NotNull(With(new GlyphEffect(Affects.Allies, 1, Status: Stat.Damage, Value: 10, StatusTurns: 2)));
        Assert.Throws<InvalidDataException>(() => With(new GlyphEffect(Affects.Enemies, 0, 1, 2)));
        Assert.Throws<InvalidDataException>(() => With(new GlyphEffect(Affects.Enemies, 11, 1, 2)));
        Assert.Throws<InvalidDataException>(() => With(new GlyphEffect(Affects.Caster, 2, 1, 2)));
        Assert.Throws<InvalidDataException>(() => With(new GlyphEffect(Affects.Enemies, 2, 3, 2)));
        // Damage or a status, the status in a spell's bounds, its numbers only with it.
        Assert.Throws<InvalidDataException>(() => With(new GlyphEffect(Affects.Enemies, 2)));
        Assert.Throws<InvalidDataException>(() => With(new GlyphEffect(Affects.Enemies, 2, Status: Stat.Mp, Value: -1, StatusTurns: 0)));
        Assert.Throws<InvalidDataException>(() => With(new GlyphEffect(Affects.Enemies, 2, 1, 2, Value: 3)));
        // Never a passive's.
        PassiveSpec p = new("p", Text, Text, Trigger.Death, [new GlyphEffect(Affects.Enemies, 2, 1, 2)]);
        Assert.Throws<InvalidDataException>(() => new GameData(Spells, [new MapSpec("m", Text, rows)], [], passives: [p]));
    }

    [Fact]
    public void AGlyph_ReadsFromJson()
    {
        const string json = """[{"id":"g","name":{"fr":"g","en":"g"},"apCost":2,"minRange":1,"maxRange":4,"lineOfSight":true,"inLine":false,"damageMin":0,"damageMax":0,"perTurn":1,"effects":[{"kind":"glyph","affects":"Enemies","turns":2,"damageMin":4,"damageMax":6,"element":"Earth","status":"Mp","value":-1,"statusTurns":1}]}]""";
        Spell s = Assert.Single(System.Text.Json.JsonSerializer.Deserialize<List<Spell>>(json, GameData.Json)!);
        GlyphEffect g = Assert.IsType<GlyphEffect>(Assert.Single(s.AllEffects));
        Assert.Equal(new GlyphEffect(Affects.Enemies, 2, 4, 6, Element.Earth, Stat.Mp, -1, 1), g);
    }
}
