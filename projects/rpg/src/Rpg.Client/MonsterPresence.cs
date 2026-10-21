using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// Which groups of monsters are on their maps (sprint 72): a group the player has beaten is away for
/// <see cref="Respawn"/>, then back at its home. Kept by the client for its session; the time is given,
/// so that the tests need no clock.
/// </summary>
public sealed class MonsterPresence
{
    public static readonly TimeSpan Respawn = Roam.Respawn;

    private readonly Dictionary<string, DateTimeOffset> _beaten = new(StringComparer.Ordinal);

    public void Beaten(string group, DateTimeOffset at) => _beaten[group ?? throw new ArgumentNullException(nameof(group))] = at;

    public bool IsAway(string group, DateTimeOffset now) => _beaten.TryGetValue(group, out DateTimeOffset at) && now - at < Respawn;

    /// <summary>The groups away now, among all those of the world.</summary>
    public IReadOnlyList<string> Away(DateTimeOffset now) => [.. _beaten.Keys.Where(g => IsAway(g, now))];
}
