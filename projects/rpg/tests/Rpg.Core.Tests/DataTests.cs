using System.Text.Json;
using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

public class DataTests
{
    [Fact]
    public void TheGamesData_LoadsAndIsConsistent()
    {
        Assert.Equal(["arrow", "bite", "spear", "spit", "strike"], Real.Spells.Keys.Order(StringComparer.Ordinal));
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
    public void InconsistentData_IsRefused()
    {
        string[] rows = ["AB"];
        Assert.Throws<InvalidDataException>(() => Data(rows, Spec(0, spells: ["nothing"]), Spec(1)));
        Assert.Throws<InvalidDataException>(() => Data(rows, Spec(0, start: 1), Spec(1)));
        Assert.Throws<InvalidDataException>(() => Data(["AAB"], Spec(0), Spec(0), Spec(1)));
        Assert.Throws<InvalidDataException>(() => Data(rows, Spec(0), Spec(0, start: 0) with { Team = 2 }));
        Assert.Throws<InvalidDataException>(() => Data(rows, Spec(0), Spec(1, hp: 0)));
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
}
