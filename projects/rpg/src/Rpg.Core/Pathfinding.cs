namespace Rpg.Core;

/// <summary>Shortest walks on the board, one movement point per step.</summary>
public static class Pathfinding
{
    /// <summary>
    /// A shortest path from <paramref name="from"/> to <paramref name="to"/> by A* with the Manhattan
    /// distance (exact on an empty board, never overestimating): the cells walked, without the start,
    /// or null when <paramref name="to"/> cannot be reached. <paramref name="isFree"/> says which
    /// cells may be walked on (floor and no fighter); the start need not be free. Ties are broken by
    /// insertion order, so the same board always gives the same path.
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
        open.Enqueue(from, (from.DistanceTo(to), from.DistanceTo(to), order++));
        while (open.TryDequeue(out Cell current, out (int F, int H, long Order) key))
        {
            // A cell may be queued several times; only its best entry counts.
            if (key.F - key.H != cost[current])
                continue;
            if (current == to)
                return Rebuild(cameFrom, from, to);
            foreach (Cell next in current.Neighbours())
            {
                if (!board.Contains(next) || !isFree(next))
                    continue;
                int g = cost[current] + 1;
                if (cost.TryGetValue(next, out int known) && known <= g)
                    continue;
                cost[next] = g;
                cameFrom[next] = current;
                int h = next.DistanceTo(to);
                open.Enqueue(next, (g + h, h, order++));
            }
        }
        return null;
    }

    /// <summary>
    /// Every cell reachable in at most <paramref name="maxSteps"/> steps, with its distance in steps,
    /// the start included at 0 (a breadth-first search).
    /// </summary>
    public static IReadOnlyDictionary<Cell, int> Reachable(Board board, Cell from, int maxSteps, Func<Cell, bool> isFree)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(isFree);
        var distance = new Dictionary<Cell, int> { [from] = 0 };
        var queue = new Queue<Cell>();
        queue.Enqueue(from);
        while (queue.TryDequeue(out Cell current))
        {
            int d = distance[current];
            if (d == maxSteps)
                continue;
            foreach (Cell next in current.Neighbours())
            {
                if (board.Contains(next) && isFree(next) && distance.TryAdd(next, d + 1))
                    queue.Enqueue(next);
            }
        }
        return distance;
    }

    private static List<Cell> Rebuild(Dictionary<Cell, Cell> cameFrom, Cell from, Cell to)
    {
        var path = new List<Cell>();
        for (Cell c = to; c != from; c = cameFrom[c])
            path.Add(c);
        path.Reverse();
        return path;
    }
}
