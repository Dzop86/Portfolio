namespace Rpg.Core.Tests;

public class PathfindingTests
{
    private static bool Floor(Board b, Cell c) => b.IsFloor(c);

    [Fact]
    public void StraightLine_IsWalkedCellByCell()
    {
        Board b = Board.Parse(["....."]);
        Assert.Equal([new Cell(1, 0), new Cell(2, 0), new Cell(3, 0), new Cell(4, 0)], Pathfinding.FindPath(b, new Cell(0, 0), new Cell(4, 0), c => Floor(b, c)));
        Assert.Empty(Pathfinding.FindPath(b, new Cell(2, 0), new Cell(2, 0), c => Floor(b, c))!);
    }

    [Fact]
    public void ObstaclesAndHoles_AreWalkedAround()
    {
        foreach (char wall in "#~")
        {
            Board b = Board.Parse([$".{wall}.", $".{wall}.", "..."]);
            IReadOnlyList<Cell>? path = Pathfinding.FindPath(b, new Cell(0, 0), new Cell(2, 0), c => Floor(b, c));
            Assert.Equal([new Cell(0, 1), new Cell(0, 2), new Cell(1, 2), new Cell(2, 2), new Cell(2, 1), new Cell(2, 0)], path);
        }
    }

    [Fact]
    public void UnreachableOrBlockedTarget_GivesNull()
    {
        Board b = Board.Parse([".#.", "##.", "..."]);
        Assert.Null(Pathfinding.FindPath(b, new Cell(0, 0), new Cell(2, 2), c => Floor(b, c)));
        Assert.Null(Pathfinding.FindPath(b, new Cell(2, 0), new Cell(1, 0), c => Floor(b, c)));
        Assert.Null(Pathfinding.FindPath(b, new Cell(2, 0), new Cell(5, 0), c => Floor(b, c)));
        // A cell taken by someone else is not a destination.
        Assert.Null(Pathfinding.FindPath(b, new Cell(2, 0), new Cell(2, 2), c => Floor(b, c) && c != new Cell(2, 2)));
    }

    [Fact]
    public void Reachable_StopsAtTheGivenNumberOfSteps()
    {
        Board b = Board.Parse([".....", ".#...", "....."]);
        IReadOnlyDictionary<Cell, int> r = Pathfinding.Reachable(b, new Cell(0, 0), 2, c => Floor(b, c));
        Assert.Equal(new Dictionary<Cell, int>
        {
            [new(0, 0)] = 0,
            [new(1, 0)] = 1,
            [new(0, 1)] = 1,
            [new(2, 0)] = 2,
            [new(0, 2)] = 2,
        }, r);
    }

    /// <summary>On random boards, A* finds paths as short as a plain breadth-first search, made of free neighbouring cells.</summary>
    [Fact]
    public void RandomBoards_PathsAreValidAndShortest()
    {
        var rng = new Rng(2026);
        for (int board = 0; board < 200; board++)
        {
            int w = rng.Next(3, 12), h = rng.Next(3, 12);
            string[] rows = [.. Enumerable.Range(0, h).Select(_ => new string([.. Enumerable.Range(0, w).Select(_ => rng.Next(0, 99) < 28 ? "#~"[rng.Next(0, 1)] : '.')]))];
            Board b = Board.Parse(rows);
            Cell[] floor = [.. b.Cells().Where(b.IsFloor)];
            if (floor.Length < 2)
                continue;
            for (int k = 0; k < 10; k++)
            {
                Cell from = floor[rng.Next(0, floor.Length - 1)], to = floor[rng.Next(0, floor.Length - 1)];
                Dictionary<Cell, int> bfs = Bfs(b, from);
                IReadOnlyList<Cell>? path = Pathfinding.FindPath(b, from, to, b.IsFloor);
                if (!bfs.TryGetValue(to, out int d))
                {
                    Assert.Null(path);
                    continue;
                }
                Assert.NotNull(path);
                Assert.Equal(d, path.Count);
                Cell at = from;
                foreach (Cell c in path)
                {
                    Assert.Equal(1, at.DistanceTo(c));
                    Assert.True(b.IsFloor(c));
                    at = c;
                }
                Assert.Equal(to, at);
                int limit = rng.Next(0, 6);
                Assert.Equal(bfs.Where(kv => kv.Value <= limit).OrderBy(kv => kv.Key.Y).ThenBy(kv => kv.Key.X),
                    Pathfinding.Reachable(b, from, limit, b.IsFloor).OrderBy(kv => kv.Key.Y).ThenBy(kv => kv.Key.X));
            }
        }
    }

    private static Dictionary<Cell, int> Bfs(Board b, Cell from)
    {
        var d = new Dictionary<Cell, int> { [from] = 0 };
        var q = new Queue<Cell>([from]);
        while (q.TryDequeue(out Cell c))
        {
            foreach (Cell n in c.Neighbours().Where(n => b.IsFloor(n) && !d.ContainsKey(n)))
            {
                d[n] = d[c] + 1;
                q.Enqueue(n);
            }
        }
        return d;
    }
}
