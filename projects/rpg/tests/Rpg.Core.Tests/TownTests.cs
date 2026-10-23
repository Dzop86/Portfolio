using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

public class TownTests
{
    private static readonly DialogueAnswer Bye = new(Text);
    private static readonly string[] Guards = ["thibaut", "mahaut", "gautier"];

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
    public void TheVillagesPeople_AreFoundByTheVillage_OnWhicheverOfItsMapsTheyLive()
    {
        // "clairval/rose" finds Rose on the market (sprint 102), as her own map's name does; the quests keep naming the village.
        (Town market, Npc rose) = Real.FindNpc("clairval/rose")!.Value;
        Assert.Equal("clairval-2-1", market.Id);
        Assert.Equal(rose, Real.FindNpc("clairval-2-1/rose")!.Value.Npc);
        Assert.Equal("clairval/rose", Real.RefOf(market, rose));
        Assert.Equal("clairval", Real.FindNpc("clairval/aubin")!.Value.Town.Id);
        Assert.Null(Real.FindNpc("clairval/nobody"));
        Assert.Null(Real.FindNpc("clairval-2-1/aubin"));
        Assert.Null(Real.FindNpc("rose"));
        // The twelve maps are the village's: Rose's stall and the faster healing hold on them all.
        Assert.All(Real.Towns.Values.Where(t => t.Zone == "clairval"), t => Assert.True(Shop.InVillage(t.Id)));
        Assert.All(new[] { null, "clairvaux", "woods-4-2" }, m => Assert.False(Shop.InVillage(m)));
        Assert.Equal((2, 2, 1), (Life.Factor("clairval"), Life.Factor("clairval-3-2"), Life.Factor("woods-4-2")));
    }

    [Fact]
    public void TheVillage_IsConsistent()
    {
        // Since sprint 102 (D74): twelve maps at x 0 to 3, y -1 to 1, the square keeping the id "clairval".
        Town[] village = [.. Real.Towns.Values.Where(t => Shop.VillageOf(t.Id) == Shop.Village)];
        Assert.Equal(12, village.Length);
        Assert.Equal([.. Enumerable.Range(0, 4).SelectMany(x => Enumerable.Range(-1, 3).Select(y => new Cell(x, y)))],
            village.Select(t => t.MapAt).OrderBy(c => c.X).ThenBy(c => c.Y));
        Assert.Equal(["aubin", "ysaline"], Real.Towns["clairval"].Npcs.Select(n => n.Id));
        Assert.Equal(["anselme", "apolline", "aubin", "basile", "eudes", "firmin", "garance", "gaspard", "gautier", "gedeon", "guard", "hortense", "leonie", "mahaut", "odalric", "perrine", "rose", "sidonie", "thibaut", "ysaline"],
            village.SelectMany(t => t.Npcs).Select(n => n.Id).Order(StringComparer.Ordinal));
        // Quatre-Chemins's seven since sprint 129 (docs/textes-v7.md); the forest's miners' camp since sprint 136 (docs/textes-v8.md).
        Assert.Equal(["anatole", "anselme", "apolline", "aristide", "barnabe", "basile", "berold", "berthe", "dungeon-keeper", "eglantine", "eudes", "firmin", "fleur", "gaspard", "gautier", "gedeon", "ghislaine", "guard", "guide", "hortense", "joachim", "josette", "leonie", "liane", "lucien", "mahaut", "mathurin", "merchant", "odalric", "perrine", "remi", "romane", "scholar", "sidonie", "thibaut", "trainer"],
            Real.Dialogues.Keys.Order(StringComparer.Ordinal));
        foreach (Town t in village)
        {
            Board board = t.Board();
            Assert.Equal((48, 32), (board.Width, board.Height));
            Assert.Equal(Terrain.Floor, board[t.Spawn]);
            // No way east to a training fight any more: Garance offers it in her talk.
            Assert.Empty(t.Exits);
        }
        // The palisade round the outer edges, its three gates west towards the woods.
        Assert.All(Enumerable.Range(0, 48), x => Assert.Equal('P', Real.Towns["clairval-1-0"][new Cell(x, 0)]));
        Assert.Equal(["woods-4-1", "woods-4-2", "woods-4-3"], village.SelectMany(t => t.Links ?? []).Where(l => l.To.StartsWith("woods-", StringComparison.Ordinal)).Select(l => l.To).Distinct().Order(StringComparer.Ordinal));
        Assert.Contains(Real.Dialogues["trainer"].Lines.Values.SelectMany(l => l.Answers), a => a.Fight == "training");
        foreach (Dialogue d in Real.Dialogues.Values)
        {
            foreach (LocalizedText t in d.Lines.Values.SelectMany(l => l.Answers.Select(a => a.Text).Append(l.Text)))
                Assert.False(string.IsNullOrWhiteSpace(t.Fr) || string.IsNullOrWhiteSpace(t.En), d.Id);
        }
    }

    [Fact]
    public void TheVillage_IsPeopled_ByTheScriptwritersInhabitants_ItsBeastsInTheirYards()
    {
        // Sprint 104 (D75, "Clairval is too empty"): docs/textes-v5.md §1.3, each on their map.
        (string Npc, string Map)[] people =
        [
            ("hortense", "clairval-0-0"), ("thibaut", "clairval-0-0"), ("firmin", "clairval-2-0"), ("gaspard", "clairval-3-0"),
            ("mahaut", "clairval-0-1"), ("guard", "clairval-0-1"), ("anselme", "clairval-3-1"), ("perrine", "clairval-3-1"),
            ("sidonie", "clairval-3-1"), ("basile", "clairval-3-1"), ("gedeon", "clairval-3-1"), ("gautier", "clairval-0-2"),
            ("leonie", "clairval-1-2"), ("odalric", "clairval-2-2"),
        ];
        foreach ((string id, string map) in people)
        {
            (Town town, Npc npc) = Real.FindNpc($"clairval/{id}")!.Value;
            Assert.Equal(map, town.Id);
            // Their own talk, which says each of their lines and lets one go.
            Dialogue talk = Real.Dialogues[npc.Dialogue];
            Assert.Equal(id, talk.Id);
            Assert.Contains(talk.Lines[talk.Start].Answers, a => a.Next is null && a.Fight is null && !a.Shop);
            Assert.All(talk.Lines.Keys.Where(k => k != talk.Start), k => Assert.Contains(talk.Lines[talk.Start].Answers, a => a.Next == k));
        }
        // The guards stand by their gate, west.
        Assert.All(Guards, g => Assert.True(Real.FindNpc($"clairval/{g}")!.Value.Npc.At.X <= 4));
        // The beasts: hens round the hen house of the granaries, horses by the stables, sheep in their meadow.
        (string Map, string Kind, char Shed)[] yards = [("clairval-1-2", "hen", 'Q'), ("clairval-0-2", "horse", 'E'), ("clairval-3-2", "sheep", 'X')];
        foreach ((string map, string kind, char shed) in yards)
        {
            Town t = Real.Towns[map];
            Assert.True((t.Animals ?? []).Count(a => a.Kind == kind) >= 3, map);
            Assert.Contains(t.Rows, r => r.Contains(shed, StringComparison.Ordinal));
        }
        Assert.All(Real.Towns.Values.Where(t => !Shop.InVillage(t.Id)), t => Assert.Empty(t.Animals ?? []));
    }

    [Fact]
    public void HenHousesStablesAndBeasts_AreChecked()
    {
        string[] rows = ["......", ".Qq...", ".qq.E.", "....e.", "XXXXXX"];
        Assert.Throws<InvalidDataException>(() => With(Town(rows, new Cell(0, 0))));
        rows[2] = ".qq.Ee";
        rows[3] = "....ee";
        Town fine = Town(rows, new Cell(0, 0)) with { Animals = [new Animal("hen", new Cell(3, 1))] };
        Assert.Equal("hen house", Rpg.Core.Town.Decor[fine[new Cell(1, 1)]]);
        Assert.Equal("fence", Rpg.Core.Town.Decor[fine[new Cell(0, 4)]]);
        Assert.False(fine.Board().IsFloor(new Cell(0, 4)));
        _ = With(fine);
        Assert.Throws<InvalidDataException>(() => With(fine with { Animals = [new Animal("dragon", new Cell(3, 1))] }));
        Assert.Throws<InvalidDataException>(() => With(fine with { Animals = [new Animal("hen", new Cell(1, 1))] }));
        Assert.Throws<InvalidDataException>(() => With(fine with { Rows = [.. rows.Select(r => r.Replace('.', '='))], Animals = [new Animal("sheep", new Cell(0, 0))] }));
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
