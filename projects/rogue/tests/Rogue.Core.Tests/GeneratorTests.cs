namespace Rogue.Core.Tests;

public class GeneratorTests
{
    public static TheoryData<ulong, int> SeedsAndDepths()
    {
        var data = new TheoryData<ulong, int>();
        for (ulong seed = 0; seed < 100; seed++)
            for (int depth = 1; depth <= Game.MaxDepth; depth++)
                data.Add(seed * 7919, depth);
        return data;
    }

    [Theory]
    [MemberData(nameof(SeedsAndDepths))]
    public void Floor_IsWellFormed(ulong seed, int depth)
    {
        Level level = DungeonGenerator.Generate(new Rng(seed), depth);
        Map map = level.Map;

        Assert.InRange(map.Rooms.Count, 2, 9);
        for (int i = 0; i < map.Rooms.Count; i++)
            for (int j = i + 1; j < map.Rooms.Count; j++)
                Assert.False(map.Rooms[i].Overlaps(map.Rooms[j], 1), $"rooms {i} and {j} touch");

        // A wall all around.
        foreach (Point p in map.Points().Where(p => p.X == 0 || p.Y == 0 || p.X == map.Width - 1 || p.Y == map.Height - 1))
            Assert.Equal(Tile.Wall, map[p]);

        // Every walkable tile can be reached from the start.
        int[] distances = map.Distances(level.Start);
        Assert.All(map.Points().Where(map.IsWalkable), p => Assert.True(distances[map.Index(p)] >= 0, $"{p} unreachable"));

        Assert.Equal(Tile.Stairs, map[level.Stairs]);
        Assert.Single(map.Points(), p => map[p] == Tile.Stairs);
        Assert.Equal(Tile.Floor, map[level.Start]);
        Assert.True(map.Rooms[0].Contains(level.Start));

        // Monsters and items stand on floor tiles, never on each other, the start or the stairs.
        var taken = new HashSet<Point> { level.Start, level.Stairs };
        foreach (Monster m in level.Monsters)
        {
            Assert.Equal(Tile.Floor, map[m.Position]);
            Assert.False(map.Rooms[0].Contains(m.Position), "monster in the first room");
            Assert.True(taken.Add(m.Position));
            Assert.False(m.Awake);
        }
        foreach ((Point p, Item item) in level.Items)
        {
            Assert.Equal(Tile.Floor, map[p]);
            Assert.True(taken.Add(p));
            Assert.True(item.Amount > 0);
        }
    }

    [Fact]
    public void SameSeed_SameFloor_OtherSeed_OtherFloor()
    {
        static string Draw(ulong seed)
        {
            Map map = DungeonGenerator.Generate(new Rng(seed), 1).Map;
            return string.Concat(map.Points().Select(p => map[p] == Tile.Wall ? '#' : '.'));
        }
        Assert.Equal(Draw(12), Draw(12));
        Assert.Equal(50, Enumerable.Range(0, 50).Select(s => Draw((ulong)s)).Distinct().Count());
    }

    [Fact]
    public void DeeperFloors_HoldStrongerMonsters()
    {
        static double AverageValue(int depth) => Enumerable.Range(0, 200)
            .SelectMany(s => DungeonGenerator.Generate(new Rng((ulong)s), depth).Monsters)
            .Average(m => m.Value);
        Assert.True(AverageValue(1) < AverageValue(3));
        Assert.True(AverageValue(3) < AverageValue(5));
        Assert.DoesNotContain(Enumerable.Range(0, 200).SelectMany(s => DungeonGenerator.Generate(new Rng((ulong)s), 1).Monsters),
            m => m.Kind is MonsterKind.Orc or MonsterKind.Troll);
    }
}
