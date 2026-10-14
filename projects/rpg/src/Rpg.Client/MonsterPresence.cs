using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// Which groups of monsters are on their maps (sprint 72): a group the player has beaten is away for
/// <see cref="Respawn"/>, then back at its home. Kept by the client for its session; the time is given,
/// so that the tests need no clock.
/// </summary>
public sealed class MonsterPresence
{
    public static readonly TimeSpan Respawn = TimeSpan.FromMinutes(2);

    private readonly Dictionary<string, DateTimeOffset> _beaten = new(StringComparer.Ordinal);

    public void Beaten(string group, DateTimeOffset at) => _beaten[group ?? throw new ArgumentNullException(nameof(group))] = at;

    public bool IsAway(string group, DateTimeOffset now) => _beaten.TryGetValue(group, out DateTimeOffset at) && now - at < Respawn;

    /// <summary>The groups away now, among all those of the world.</summary>
    public IReadOnlyList<string> Away(DateTimeOffset now) => [.. _beaten.Keys.Where(g => IsAway(g, now))];
}

/// <summary>
/// How a group of monsters wanders (sprint 72): from time to time, a few steps to a free cell no more
/// than <see cref="Radius"/> from its home, never onto the player, another group, a way or an exit,
/// nor onto a cell that would cut the map in two (a pontoon, sprint 75).
/// </summary>
public static class Roaming
{
    public const int Radius = 3;

    /// <summary>The cells to walk to the next place (without the start), or null when none is free; <paramref name="rng"/> chooses.</summary>
    public static IReadOnlyList<Cell>? Next(TownController town, string groupId, Rng rng)
    {
        ArgumentNullException.ThrowIfNull(town);
        ArgumentNullException.ThrowIfNull(rng);
        MonsterGroup group = town.Data.Groups.TryGetValue(groupId, out MonsterGroup? g) && g.Map == town.Town.Id
            ? g : throw new KeyNotFoundException($"No group '{groupId}' on {town.Town.Id}.");
        Cell from = town.Groups[groupId];
        bool Free(Cell c) => (c == from || town.IsFree(c)) && c != town.Position;
        Cell[] places = [.. Enumerable.Range(-Radius, 2 * Radius + 1)
            .SelectMany(dx => Enumerable.Range(-Radius, 2 * Radius + 1).Select(dy => group.Home + new Cell(dx, dy)))
            .Where(c => c != from && c.DistanceTo(group.Home) <= Radius && town.Board.Contains(c) && Free(c)
                && town.Town.LinkAt(c) is null && town.Town.ExitAt(c) is null
                && MonsterGroups.KeepsWaysOpen(town.Town, town.Board, [.. town.Groups.Where(o => o.Key != groupId).Select(o => o.Value), c]))];
        if (places.Length == 0)
            return null;
        Cell to = places[rng.Next(0, places.Length - 1)];
        return Wander.FindPath(town.Board, from, to, Free);
    }
}
