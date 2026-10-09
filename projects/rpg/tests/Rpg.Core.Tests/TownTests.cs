using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

public class TownTests
{
    private static readonly DialogueAnswer Bye = new(Text);

    private static Dialogue Talk(string id = "d", params (string Id, DialogueAnswer[] Answers)[] lines) =>
        new(id, lines.Length == 0 ? "a" : lines[0].Id,
            lines.Length == 0 ? new Dictionary<string, DialogueLine> { ["a"] = new(Text, [Bye]) } : lines.ToDictionary(l => l.Id, l => new DialogueLine(Text, l.Answers)));

    private static Town Town(string[] rows, Cell spawn, Npc[]? npcs = null, TownExit[]? exits = null) =>
        new("t", Text, rows, spawn, npcs ?? [], exits ?? []);

    private static Npc Npc(Cell at) => new("n", Text, "male-a", 0, at, "d");

    private static GameData With(Town? town = null, params Dialogue[] dialogues) =>
        new(Spells, [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])], null,
            town is null ? [] : [town], dialogues.Length == 0 ? [Talk()] : dialogues);

    [Fact]
    public void TheVillage_IsConsistent()
    {
        Town clairval = Real.Towns["clairval"];
        Assert.Equal(["aubin", "rose", "garance"], clairval.Npcs.Select(n => n.Id));
        Assert.Equal(["guide", "merchant", "trainer"], Real.Dialogues.Keys.Order(StringComparer.Ordinal));
        Board board = clairval.Board();
        Assert.Equal((16, 12), (board.Width, board.Height));
        // Houses and the fountain stand in the way, roads and grass do not.
        Assert.Equal(Terrain.Obstacle, board[new Cell(4, 0)]);
        Assert.Equal(Terrain.Obstacle, board[new Cell(8, 5)]);
        Assert.Equal(Terrain.Floor, board[clairval.Spawn]);
        Assert.Equal(Terrain.Hole, board[new Cell(3, 9)]);
        // The trainer can start the training fight, which the east gate leads to as well.
        Assert.Contains(Real.Dialogues["trainer"].Lines.Values.SelectMany(l => l.Answers), a => a.Fight == "training");
        Assert.Equal("training", clairval.ExitAt(new Cell(15, 5))!.Scenario);
        foreach (Dialogue d in Real.Dialogues.Values)
        {
            foreach (LocalizedText t in d.Lines.Values.SelectMany(l => l.Answers.Select(a => a.Text).Append(l.Text)))
                Assert.False(string.IsNullOrWhiteSpace(t.Fr) || string.IsNullOrWhiteSpace(t.En), d.Id);
        }
    }

    [Fact]
    public void ATownIsRead_FromItsRows()
    {
        string[] rows = ["Hh.", "hh.", "..."];
        GameData data = With(Town(rows, new Cell(2, 0), [Npc(new Cell(0, 2))]));
        Town t = data.Towns["t"];
        Assert.True(t.IsFree(t.Board(), new Cell(2, 1)));
        Assert.False(t.IsFree(t.Board(), new Cell(0, 2)));
        Assert.Equal("n", t.NpcAt(new Cell(0, 2))!.Id);
    }

    [Fact]
    public void InconsistentTowns_AreRefused()
    {
        Assert.Throws<InvalidDataException>(() => With(Town(["..?"], new Cell(0, 0))));
        Assert.Throws<InvalidDataException>(() => With(Town(["H.", "h."], new Cell(1, 0))));
        Assert.Throws<InvalidDataException>(() => With(Town([".h"], new Cell(0, 0))));
        Assert.Throws<InvalidDataException>(() => With(Town([".T"], new Cell(1, 0))));
        // Walled in by trees: nobody can talk to them.
        Assert.Throws<InvalidDataException>(() => With(Town([".T.", "T.T", ".T."], new Cell(0, 0), [Npc(new Cell(1, 1))])));
        Assert.Throws<InvalidDataException>(() => With(Town(["..."], new Cell(0, 0), [Npc(new Cell(2, 0)) with { Dialogue = "none" }])));
        Assert.Throws<InvalidDataException>(() => With(Town(["..."], new Cell(0, 0), [Npc(new Cell(2, 0)) with { Look = "orc" }])));
        Assert.Throws<InvalidDataException>(() => With(Town(["..."], new Cell(0, 0), null, [new TownExit(new Cell(2, 0), "nowhere", Text)])));
        Assert.Throws<InvalidDataException>(() => With(Town([".T."], new Cell(0, 0), null, [new TownExit(new Cell(2, 0), "s", Text)])));
    }

    [Fact]
    public void InconsistentDialogues_AreRefused()
    {
        Assert.Equal("a", With(null, Talk("d", ("a", [new(Text, "b"), Bye]), ("b", [new(Text, Fight: "s")]))).Dialogues["d"].Start);
        // A line nobody reaches; an answer to no line; a loop with no way out.
        Assert.Throws<InvalidDataException>(() => With(null, Talk("d", ("a", [Bye]), ("b", [Bye]))));
        Assert.Throws<InvalidDataException>(() => With(null, Talk("d", ("a", [new(Text, "z")]))));
        Assert.Throws<InvalidDataException>(() => With(null, Talk("d", ("a", [new(Text, "b"), Bye]), ("b", [new(Text, "c")]), ("c", [new(Text, "b")]))));
        // No answer, too many, a fight that goes on talking, a fight in an unknown scenario.
        Assert.Throws<InvalidDataException>(() => With(null, Talk("d", ("a", []))));
        Assert.Throws<InvalidDataException>(() => With(null, Talk("d", ("a", [Bye, Bye, Bye, Bye, Bye]))));
        Assert.Throws<InvalidDataException>(() => With(null, Talk("d", ("a", [new(Text, "a", "s"), Bye]))));
        Assert.Throws<InvalidDataException>(() => With(null, Talk("d", ("a", [new(Text, Fight: "nowhere")]))));
        Assert.Throws<InvalidDataException>(() => With(null, new Dialogue("d", "x", new Dictionary<string, DialogueLine> { ["a"] = new(Text, [Bye]) })));
    }
}
