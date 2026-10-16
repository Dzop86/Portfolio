namespace Rpg.Core;

/// <summary>A spot where a gathering trade takes a resource (sprint 99): on a map, a cell, its kind, the resource and its level.</summary>
public sealed record HarvestNode(string Id, string Map, Cell At, NodeKind Kind, string Job, string Item, int Level);

/// <summary>
/// The gathering (sprint 99, D71 and D72, docs/suite-apres-d69.md §5.3 and §5.4): the spots of every map of a
/// zone, drawn once from the map's name as the groups of monsters are, so that the game, the server and the
/// tests see the same ones; on decor of the right kind (a tree, a rock, water, mushrooms or flowers) or on
/// grass for the plots (and an outcrop where a map has no rock), away from the ways, the exits and the arrival. Each spot carries its zone's first
/// resource near the way in, its second at the far end. What a gathering gives, its experience, and when the
/// spot grows back.
/// </summary>
public static class Harvesting
{
    /// <summary>How long a gathering takes, the character standing still.</summary>
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(3);

    /// <summary>The two resource levels of each zone's spots: near the way in, then at the far end.</summary>
    public static readonly IReadOnlyDictionary<string, (int Near, int Far)> Tiers = new Dictionary<string, (int, int)>
    {
        ["woods"] = (1, 5),
        ["mouldy"] = (10, 15),
        ["lake"] = (20, 25),
        ["fields"] = (30, 35),
        // The village's few spots, of the first level only (sprint 102).
        ["clairval"] = (1, 1),
    };

    // How many spots of each kind a map of the zone has: trees, rocks, water, plots, herbs.
    private static readonly Dictionary<string, int[]> Counts = new()
    {
        ["woods"] = [2, 2, 1, 2, 1],
        ["mouldy"] = [2, 2, 2, 1, 2],
        ["lake"] = [1, 1, 3, 2, 2],
        ["fields"] = [2, 2, 0, 4, 2],
        ["clairval"] = [1, 1, 1, 2, 1],
    };

    /// <summary>How close to a way, an exit or the arrival a spot may be, and to another spot.</summary>
    public const int Distance = MonsterGroups.Distance, Apart = 2;

    public static IReadOnlyList<HarvestNode> For(Town map, IReadOnlyCollection<Town> world, IReadOnlyCollection<Job> jobs)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(jobs);
        if (map.Zone is not string zone || !Tiers.TryGetValue(zone, out (int Near, int Far) tiers))
            return [];
        var rng = new Rng(MonsterGroups.Seed(map.Id + "~nodes"));
        Board board = map.Board();
        HashSet<Cell> reach = [.. Pathfinding.Reachable(board, map.Spawn, board.Width * board.Height, c => map.IsFree(board, c)).Keys];
        Cell[] away = [.. (map.Links ?? []).Select(l => l.At), .. map.Exits.Select(e => e.At), .. map.Doors.Select(d => d.At), .. (map.Workshops ?? []).Select(w => w.At), map.Spawn];
        char At(Cell c) => c.Y >= 0 && c.Y < map.Rows.Count && c.X >= 0 && c.X < map.Rows[c.Y].Length ? map.Rows[c.Y][c.X] : ' ';
        bool Beside(Cell c) => c.Neighbours().Any(reach.Contains);
        int depth = MonsterGroups.Depth(map, world);
        var nodes = new List<HarvestNode>();
        var taken = new List<Cell>();
        int[] counts = Counts[zone];
        // The lake's islets and pontoons hold the deep fish of the next level (the fields have no water).
        int extraWater = zone == "lake" ? 1 : 0;
        for (int k = 0; k < counts.Length; k++)
        {
            var kind = (NodeKind)k;
            Func<Cell, bool> fits = kind switch
            {
                NodeKind.Tree => c => At(c) == 'T' && Beside(c),
                NodeKind.Rock => c => At(c) == 'R' && Beside(c),
                NodeKind.Water => c => At(c) == '~' && Beside(c),
                NodeKind.Plot => c => At(c) == '.' && reach.Contains(c),
                _ => zone switch
                {
                    "mouldy" => c => At(c) == 'M' && Beside(c),
                    "fields" => c => At(c) == ',' && reach.Contains(c),
                    _ => c => At(c) == '.' && reach.Contains(c) && c.Neighbours().Any(n => At(n) == 'T'),
                },
            };
            Cell[] all = [.. Enumerable.Range(0, board.Height).SelectMany(y => Enumerable.Range(0, board.Width).Select(x => new Cell(x, y)))
                .Where(c => away.All(a => a.DistanceTo(c) >= Distance))];
            List<Cell> cells = [.. all.Where(fits)];
            // No rock in the fields' decor: an outcrop on the grass, drawn by the game (water is never made up).
            if (cells.Count == 0 && kind != NodeKind.Water)
                cells = [.. all.Where(c => At(c) == '.' && reach.Contains(c))];
            int wanted = counts[k] + (kind == NodeKind.Water ? extraWater : 0);
            for (int i = 0; i < wanted; i++)
            {
                cells.RemoveAll(c => taken.Any(t => t.DistanceTo(c) < Apart));
                if (cells.Count == 0)
                    break;
                Cell at = cells[rng.Next(0, cells.Count - 1)];
                taken.Add(at);
                int level = depth == 0 ? tiers.Near : depth == 2 ? tiers.Far : rng.Next(0, 1) == 0 ? tiers.Near : tiers.Far;
                if (kind == NodeKind.Water && zone == "lake" && i == wanted - 1)
                    level = depth <= 1 ? Tiers["fields"].Near : Tiers["fields"].Far;
                if (jobs.Where(j => j.Gathers).SelectMany(j => j.Harvests!.Select(h => (Job: j, Harvest: h))).FirstOrDefault(x => x.Harvest.Node == kind && x.Harvest.Level == level) is not (Job job, Harvest harvest))
                    continue;
                nodes.Add(new HarvestNode($"{map.Id}~{nodes.Count}", map.Id, at, kind, job.Id, harvest.Item, harvest.Level));
            }
        }
        return nodes;
    }

    /// <summary>On the spot or on one of the eight cells around it (the free walk goes in eight directions, sprint 69).</summary>
    public static bool Beside(Cell standing, Cell spot) => Math.Abs(standing.X - spot.X) <= 1 && Math.Abs(standing.Y - spot.Y) <= 1;

    /// <summary>How many a gathering gives: one, and one more for every 20 levels of the trade above the resource's, three at most.</summary>
    public static int Quantity(int jobLevel, int level) => Math.Clamp(1 + (jobLevel - level) / 20, 1, 3);

    /// <summary>The trade's experience of a gathering: 25 + 5 a level of the resource; nothing 20 levels or more below the trade.</summary>
    public static long Xp(int jobLevel, int level) => jobLevel - level >= 20 ? 0 : 25 + 5L * level;

    /// <summary>When a spot grows back: 2 minutes, and one more for every 10 levels of its resource.</summary>
    public static TimeSpan Regrowth(int level) => TimeSpan.FromMinutes(2 + level / 10);

    /// <summary>Why the character cannot gather this spot from where it stands, or null: on its map, next to it, the trade's level.</summary>
    public static string? WhyNot(HarvestNode node, int jobLevel, string? map, Cell standing)
    {
        ArgumentNullException.ThrowIfNull(node);
        return map != node.Map ? "Not on this spot's map."
            : !Beside(standing, node.At) ? "Too far from the spot."
            : jobLevel < node.Level ? $"The trade's level {node.Level} is needed."
            : null;
    }
}
