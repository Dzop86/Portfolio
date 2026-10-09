using System.Text.Json;
using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

public class DataTests
{
    [Fact]
    public void TheGamesData_LoadsAndIsConsistent()
    {
        // The nine spells of the first sprints, still there for the monsters and the recorded fights.
        Assert.Subset(Real.Spells.Keys.ToHashSet(), new HashSet<string>(["arrow", "axe", "club", "fireball", "sling", "spark", "spear", "staff", "strike"]));
        Assert.Equal(["sentinel", "guard", "mage"], Real.Classes.Select(c => c.Id));
        foreach (HeroClass c in Real.Classes)
            Assert.False(string.IsNullOrWhiteSpace(c.Description.Fr) || string.IsNullOrWhiteSpace(c.Description.En), c.Id);
        Assert.Equal(["duel", "training"], Real.Scenarios.Keys.Order(StringComparer.Ordinal));
        foreach (Spell s in Real.Spells.Values)
            Assert.False(string.IsNullOrWhiteSpace(s.Name.Fr) || string.IsNullOrWhiteSpace(s.Name.En), s.Id);
        Board arena = Real.Board("arena");
        Assert.Equal((11, 11), (arena.Width, arena.Height));
        // The arena is the same seen from either side: turned by half a turn, A becomes B.
        foreach (Cell c in arena.Cells())
            Assert.Equal(arena[c], arena[new Cell(arena.Width - 1 - c.X, arena.Height - 1 - c.Y)]);
        Assert.Equal(arena.Starts[0].Count, arena.Starts[1].Count);
    }

    [Fact]
    public void TheBuiltInData_IsTheDataFolder()
    {
        GameData files = GameData.Load(Path.Combine(AppContext.BaseDirectory, "data"));
        // Spells hold lists (ranks, effects): compared by what they write.
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(files.Spells), System.Text.Json.JsonSerializer.Serialize(GameData.Embedded.Spells));
        Assert.Equal(files.Maps.Keys, GameData.Embedded.Maps.Keys);
        Assert.Equal(files.Scenarios.Keys, GameData.Embedded.Scenarios.Keys);
        Assert.Equal(files.Classes.Select(c => c.Id), GameData.Embedded.Classes.Select(c => c.Id));
    }

    [Fact]
    public void EveryClass_HasTwentySpells_OnTwoElementsAtLeast_UnlockedFromLevel1To100()
    {
        string[] shared = ["strike", "arrow", "spear", "axe", "staff", "spark", "fireball"];
        foreach (HeroClass c in Real.Classes)
        {
            Spell[] spells = [.. c.Spells.Select(id => Real.Spells[id])];
            Assert.Equal(20, spells.Length);
            Assert.True(spells.Where(s => s.DamageMax > 0).Select(s => s.Element).Distinct().Count() >= 2, c.Id);
            // A spell of its own from level 1, the last one at level 100, in the order they unlock.
            Assert.Contains(spells, s => s.Level == 1 && !shared.Contains(s.Id));
            Assert.Equal(100, spells.Max(s => s.Level));
            Assert.Equal(spells.Select(s => s.Level).Order(), spells.Select(s => s.Level));
        }
        // No spell of a class belongs to another: the shared ones are the first sprints'.
        string[][] own = [.. Real.Classes.Select(c => c.Spells.Except(shared).ToArray())];
        Assert.Equal(own.Sum(o => o.Length), own.SelectMany(o => o).Distinct().Count());
    }

    [Fact]
    public void InconsistentData_IsRefused()
    {
        string[] rows = ["AB"];
        Assert.Throws<InvalidDataException>(() => Data(rows, Spec(0, spells: ["nothing"]), Spec(1)));
        Assert.Throws<InvalidDataException>(() => Data(rows, Spec(0, start: 1), Spec(1)));
        Assert.Throws<InvalidDataException>(() => Data(["AAB"], Spec(0), Spec(0), Spec(1)));
        Assert.Throws<InvalidDataException>(() => Data(rows, Spec(0), Spec(0, start: 0) with { Team = 2 }));
        Assert.Throws<InvalidDataException>(() => Data(rows, Spec(0), Spec(1, hp: 0)));
        Assert.Throws<InvalidDataException>(() => Data(rows, Spec(0), Spec(1) with { Look = " " }));
        Assert.Throws<InvalidDataException>(() => Data(rows, Spec(0)));
        Assert.Throws<InvalidDataException>(() => new GameData([Strike, Strike], [], []));
        Assert.Throws<InvalidDataException>(() => new GameData([Strike with { MinRange = 3 }], [], []));
        Assert.Throws<InvalidDataException>(() => new GameData([Strike with { ApCost = 0 }], [], []));
        Assert.Throws<KeyNotFoundException>(() => new GameData(Spells, [], [new Scenario("s", Text, "nowhere", [])]));
    }

    [Theory]
    [InlineData("""{"id":"x","name":{"fr":"a","en":"b"},"apCost":3,"minRange":1,"maxRange":1,"lineOfSight":true,"inLine":false,"damageMin":1,"damageMax":2}""")]
    [InlineData("""{"id":"x","name":{"fr":"a","en":"b"},"apCost":3,"minRange":1,"maxRange":1,"lineOfSight":true,"inLine":false,"damageMin":1,"damageMax":2,"perTurn":1,"heal":4}""")]
    [InlineData("""{"id":null,"name":{"fr":"a","en":"b"},"apCost":3,"minRange":1,"maxRange":1,"lineOfSight":true,"inLine":false,"damageMin":1,"damageMax":2,"perTurn":1}""")]
    public void ASpellWithAMissingUnknownOrNullField_IsRefused(string json) =>
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<Spell>(json, GameData.Json));

    private static HeroClass Class(string id, params string[] spells) => new(id, Text, Text, 50, 6, 3, 100, spells);

    [Fact]
    public void InconsistentClasses_AreRefused()
    {
        GameData Classes(params HeroClass[] classes) =>
            new(Spells, [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])], classes);
        Assert.Equal(["a", "b"], Classes(Class("a", "strike"), Class("b", "bow")).Classes.Select(c => c.Id));
        Assert.Throws<InvalidDataException>(() => Classes(Class("a", "nothing")));
        Assert.Throws<InvalidDataException>(() => Classes(Class("a")));
        Assert.Throws<InvalidDataException>(() => Classes(Class("a", [.. Enumerable.Repeat("strike", 31)])));
        Assert.Throws<InvalidDataException>(() => Classes(Class("a", "strike"), Class("a", "bow")));
        Assert.Throws<InvalidDataException>(() => Classes(Class("a", "strike") with { Hp = 0 }));
        Assert.Throws<InvalidDataException>(() => Classes(Class("a", "strike") with { HpPerLevel = -1 }));
    }
}
