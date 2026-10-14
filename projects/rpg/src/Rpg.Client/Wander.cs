using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// Walking outside fights (sprint 69): eight directions instead of four, a diagonal only between two
/// free sides (never through the corner of a house), then the path pulled taut into straight lines
/// that the client walks at an even pace. The fights keep their four directions (<see cref="Pathfinding"/>).
/// </summary>
public static class Wander
{
    /// <summary>The eight steps, sides first: the order fixes ties.</summary>
    public static readonly Cell[] Steps = [new(1, 0), new(0, 1), new(-1, 0), new(0, -1), new(1, 1), new(-1, 1), new(-1, -1), new(1, -1)];

    // A side costs 10, a diagonal 14 (about 10·√2), so that distances stay whole numbers.
    private const int Side = 10, Diagonal = 14;

    /// <summary>How far the walker keeps from what is not free, in cells, on the straight lines.</summary>
    public const double Clearance = 0.3;

    /// <summary>The length of a walk of eight directions on an empty board, in tenths of a cell.</summary>
    public static int Distance(Cell a, Cell b)
    {
        int dx = Math.Abs(a.X - b.X), dy = Math.Abs(a.Y - b.Y);
        return Side * Math.Max(dx, dy) + (Diagonal - Side) * Math.Min(dx, dy);
    }

    /// <summary>The cells one step away that a walker may go to from <paramref name="c"/>.</summary>
    public static IEnumerable<Cell> Neighbours(Board board, Cell c, Func<Cell, bool> isFree)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(isFree);
        bool Free(Cell x) => board.Contains(x) && isFree(x);
        foreach (Cell step in Steps)
        {
            Cell next = c + step;
            if (!Free(next))
                continue;
            if (step.X != 0 && step.Y != 0 && (!Free(c + new Cell(step.X, 0)) || !Free(c + new Cell(0, step.Y))))
                continue;
            yield return next;
        }
    }

    /// <summary>
    /// A shortest path of eight directions by A* (the cells walked, without the start), or null when
    /// <paramref name="to"/> cannot be reached; the same board always gives the same path.
    /// </summary>
    public static IReadOnlyList<Cell>? FindPath(Board board, Cell from, Cell to, Func<Cell, bool> isFree)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(isFree);
        if (from == to)
            return [];
        if (!board.Contains(to) || !isFree(to))
            return null;
        var cameFrom = new Dictionary<Cell, Cell>();
        var cost = new Dictionary<Cell, int> { [from] = 0 };
        var open = new PriorityQueue<Cell, (int F, int H, long Order)>();
        long order = 0;
        open.Enqueue(from, (Distance(from, to), Distance(from, to), order++));
        while (open.TryDequeue(out Cell current, out (int F, int H, long Order) key))
        {
            if (key.F - key.H != cost[current])
                continue;
            if (current == to)
            {
                var path = new List<Cell>();
                for (Cell c = to; c != from; c = cameFrom[c])
                    path.Add(c);
                path.Reverse();
                return path;
            }
            foreach (Cell next in Neighbours(board, current, isFree))
            {
                int g = cost[current] + (next.X != current.X && next.Y != current.Y ? Diagonal : Side);
                if (cost.TryGetValue(next, out int known) && known <= g)
                    continue;
                cost[next] = g;
                cameFrom[next] = current;
                int h = Distance(next, to);
                open.Enqueue(next, (g + h, h, order++));
            }
        }
        return null;
    }

    /// <summary>
    /// True when a walker as wide as twice <see cref="Clearance"/> can go straight from the centre of
    /// <paramref name="a"/> to that of <paramref name="b"/> on free cells only.
    /// </summary>
    public static bool StraightLine(Cell a, Cell b, Func<Cell, bool> isFree)
    {
        ArgumentNullException.ThrowIfNull(isFree);
        double length = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        int samples = Math.Max(1, (int)Math.Ceiling(length / 0.05));
        for (int i = 0; i <= samples; i++)
        {
            double t = (double)i / samples;
            double x = a.X + (b.X - a.X) * t, y = a.Y + (b.Y - a.Y) * t;
            // The four corners of the walker's square around the point.
            foreach ((double ox, double oy) in new[] { (-Clearance, -Clearance), (Clearance, -Clearance), (-Clearance, Clearance), (Clearance, Clearance) })
            {
                if (!isFree(new Cell((int)Math.Round(x + ox, MidpointRounding.AwayFromZero), (int)Math.Round(y + oy, MidpointRounding.AwayFromZero))))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// The path pulled taut: from each corner, the farthest cell of the path still in a straight line;
    /// the waypoints end on the path's last cell, empty for an empty path.
    /// </summary>
    public static IReadOnlyList<Cell> Smooth(Cell from, IReadOnlyList<Cell> path, Func<Cell, bool> isFree)
    {
        ArgumentNullException.ThrowIfNull(path);
        var waypoints = new List<Cell>();
        Cell corner = from;
        int i = 0;
        while (i < path.Count)
        {
            int far = i;
            for (int j = path.Count - 1; j > i; j--)
            {
                if (StraightLine(corner, path[j], isFree))
                {
                    far = j;
                    break;
                }
            }
            corner = path[far];
            waypoints.Add(corner);
            i = far + 1;
        }
        return waypoints;
    }

    /// <summary>The length of the straight lines from <paramref name="from"/> through the waypoints, in cells.</summary>
    public static double Length(Cell from, IReadOnlyList<Cell> waypoints)
    {
        ArgumentNullException.ThrowIfNull(waypoints);
        double total = 0;
        Cell at = from;
        foreach (Cell w in waypoints)
        {
            total += Math.Sqrt(Math.Pow(w.X - at.X, 2) + Math.Pow(w.Y - at.Y, 2));
            at = w;
        }
        return total;
    }
}
