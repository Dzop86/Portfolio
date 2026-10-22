namespace Rpg.Core;

/// <summary>
/// The recall scroll and the waystones (sprint 127, D79, docs/suite-apres-d69.md §11.5): a waystone stands in the
/// village's square and at each zone's way in (a thing to examine of the map, look "waystone"); passing within
/// <see cref="Discovery"/> cells of one discovers it for the character, who may then travel to it from any other, for
/// <see cref="Cost"/> Éclats. The recall scroll, read out of a fight and out of a dungeon, takes one back to its home: the
/// waystone it chose, else the village's. The server applies these rules.
/// </summary>
public static class Waystones
{
    /// <summary>The look of a waystone among a map's things to examine.</summary>
    public const string Look = "waystone";

    /// <summary>The recall scroll's item.</summary>
    public const string Recall = "recall-scroll";

    /// <summary>The village's waystone, the home of whoever chose none.</summary>
    public const string Village = "waystone-clairval";

    /// <summary>How near one must pass to discover a waystone.</summary>
    public const int Discovery = 3;

    /// <summary>What the recall scroll costs at Rose's.</summary>
    public const long RecallPrice = 15;

    /// <summary>Every waystone of the world: its id, its map and its cell.</summary>
    public static IReadOnlyList<(string Id, string Map, Cell At)> All(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return [.. data.Towns.Values.OrderBy(t => t.Id, StringComparer.Ordinal)
            .SelectMany(t => (t.Props ?? []).Where(p => p.Look == Look).Select(p => (p.Id, t.Id, p.At)))];
    }

    /// <summary>The waystones within reach of a cell of a map: discovered by standing there.</summary>
    public static IReadOnlyList<string> Near(string map, Cell at, GameData data) =>
        [.. All(data).Where(w => w.Map == map && w.At.DistanceTo(at) <= Discovery).Select(w => w.Id)];

    /// <summary>What travelling to a waystone costs: 10 Éclats and two a level of the map it stands on.</summary>
    public static long Cost(string waystone, GameData data)
    {
        (_, string map, _) = All(data).First(w => w.Id == waystone);
        return 10L + 2L * data.Towns[map].Level;
    }

    /// <summary>Where one arrives at a waystone: the free cell below it, else the nearest free one, else the map's arrival.</summary>
    public static Place Arrival(string waystone, GameData data)
    {
        (_, string mapId, Cell at) = All(data).First(w => w.Id == waystone);
        Town map = data.Towns[mapId];
        Cell? free = new[] { at + new Cell(0, 1), at + new Cell(1, 0), at + new Cell(-1, 0), at + new Cell(0, -1) }.Where(map.CanStand).Cast<Cell?>().FirstOrDefault();
        Cell cell = free ?? map.Spawn;
        return new Place(mapId, cell.X, cell.Y);
    }

    /// <summary>Why one cannot travel to a waystone now (unknown, already on its map, in a fight or a dungeon, the Éclats), or null.</summary>
    public static string? WhyNotTravel(string waystone, IReadOnlyCollection<string> known, string? here, bool inFight, bool inDungeon, long shards, GameData data)
    {
        ArgumentNullException.ThrowIfNull(known);
        ArgumentNullException.ThrowIfNull(data);
        if (!All(data).Any(w => w.Id == waystone))
            return "No such waystone.";
        return !known.Contains(waystone) ? "A waystone not discovered yet."
            : inFight ? "Not in a fight."
            : inDungeon ? "Not in a dungeon."
            : here == All(data).First(w => w.Id == waystone).Map ? "Already on its map."
            : Cost(waystone, data) > shards ? $"{Cost(waystone, data)} Éclats needed."
            : null;
    }

    /// <summary>Why the recall scroll cannot be read now (none owned, in a fight or a dungeon), or null.</summary>
    public static string? WhyNotRecall(int owned, bool inFight, bool inDungeon) =>
        owned < 1 ? "None owned." : inFight ? "Not in a fight." : inDungeon ? "Not in a dungeon." : null;

    /// <summary>Where the recall scroll takes one: its home waystone, else the village's.</summary>
    public static Place Home(string? home, GameData data) =>
        Arrival(home is not null && All(data).Any(w => w.Id == home) ? home : Village, data);
}
