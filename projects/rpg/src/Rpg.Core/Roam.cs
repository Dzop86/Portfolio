namespace Rpg.Core;

/// <summary>
/// Where the groups of monsters of a map stand (sprint 118, D74): the same for every player, drawn from the clock
/// rather than kept anywhere. Time is cut in slots of <see cref="Period"/>; in each slot, each group of the map, in
/// the order of its id, stands on a cell no more than <see cref="Radius"/> steps from its home, drawn from the group
/// and the slot, free (no inhabitant, thing, way or exit, no other group) and that cuts no way of the map; its home
/// when none is.
/// </summary>
public static class Roam
{
    public static readonly TimeSpan Period = TimeSpan.FromSeconds(8);
    public const int Radius = 3;

    /// <summary>How long a group beaten stays away (sprint 72).</summary>
    public static readonly TimeSpan Respawn = TimeSpan.FromMinutes(2);

    /// <summary>The slot of a moment: its whole periods since 1970.</summary>
    public static long Slot(DateTimeOffset now) => now.ToUnixTimeMilliseconds() / (long)Period.TotalMilliseconds;

    /// <summary>Where every group of the map stands during the slot (those beaten too: their place stays theirs).</summary>
    public static IReadOnlyDictionary<string, Cell> Places(GameData data, Town map, Board board, long slot)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(board);
        var placed = new Dictionary<string, Cell>(StringComparer.Ordinal);
        foreach (MonsterGroup g in data.GroupsOn(map.Id).OrderBy(g => g.Id, StringComparer.Ordinal))
        {
            Cell[] places = [.. Enumerable.Range(-Radius, 2 * Radius + 1)
                .SelectMany(dy => Enumerable.Range(-Radius, 2 * Radius + 1).Select(dx => g.Home + new Cell(dx, dy)))
                .Where(c => c.DistanceTo(g.Home) <= Radius && board.Contains(c) && map.IsFree(board, c) && map.LinkAt(c) is null && map.ExitAt(c) is null
                    && !placed.ContainsValue(c))];
            // The cell drawn, or the next ones in turn if it would cut a way: one look at the whole map, most of the time.
            int first = places.Length == 0 ? 0 : new Rng(Seed(g.Id, slot)).Next(0, places.Length - 1);
            placed[g.Id] = Enumerable.Range(0, places.Length).Select(i => places[(first + i) % places.Length])
                .FirstOrDefault(c => MonsterGroups.KeepsWaysOpen(map, board, [.. placed.Values, c]), g.Home);
        }
        return placed;
    }

    // The same seed on every computer: FNV-1a of the group's id (string.GetHashCode changes from one run to the next), mixed with the slot.
    private static ulong Seed(string group, long slot)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in group)
            hash = (hash ^ c) * 1099511628211UL;
        return hash ^ ((ulong)slot * 0x9E3779B97F4A7C15UL);
    }
}
