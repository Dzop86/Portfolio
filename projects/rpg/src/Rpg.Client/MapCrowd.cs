using Rpg.Core;

namespace Rpg.Client;

/// <summary>Another player on the map (sprint 118): their name, their look, the cell they stand on or walk to.</summary>
public sealed record OtherPlayer(Guid Id, string Name, Hero Look, Cell At);

/// <summary>
/// The map as the other players share it (sprint 118, D74), from the server's <see cref="MapNews"/>: who else is on
/// it and where they go, the groups of monsters someone fights or has beaten (and until when), the gathering spots
/// someone took. Emptied on each new map: the server tells everything again on arriving.
/// </summary>
public sealed class MapCrowd
{
    private readonly Dictionary<Guid, OtherPlayer> _others = [];
    private readonly HashSet<string> _fighting = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _beaten = new(StringComparer.Ordinal);

    public IReadOnlyCollection<OtherPlayer> Others => _others.Values;

    /// <summary>The groups fought by someone now.</summary>
    public IReadOnlyCollection<string> Fighting => _fighting;

    /// <summary>A new map: nobody known on it until the server says.</summary>
    public void Clear()
    {
        _others.Clear();
        _fighting.Clear();
        _beaten.Clear();
    }

    /// <summary>Whether a group is away now: fought by someone, or beaten and not back.</summary>
    public bool IsAway(string group, DateTimeOffset now) => _fighting.Contains(group) || (_beaten.TryGetValue(group, out DateTimeOffset until) && until > now);

    /// <summary>The groups away now.</summary>
    public IReadOnlyList<string> Away(DateTimeOffset now) => [.. _fighting.Concat(_beaten.Keys).Distinct().Where(g => IsAway(g, now)).Order(StringComparer.Ordinal)];

    /// <summary>
    /// Takes the news in: a player here (or back here) at a cell, walking, gone; a group fought, beaten, back; a spot
    /// taken goes to <paramref name="spots"/>, growing back. The news about oneself are left out.
    /// </summary>
    public void Apply(MapNews news, Guid me, GrowingSpots? spots = null)
    {
        ArgumentNullException.ThrowIfNull(news);
        if (news.Character == me && news.Kind is MapNewsKind.Here or MapNewsKind.Walk or MapNewsKind.Gone)
            return;
        switch (news.Kind)
        {
            case MapNewsKind.Here when news is { Character: Guid id, Name: string name, Look: Hero look, At: Cell at }:
                _others[id] = new OtherPlayer(id, name, look, at);
                break;
            case MapNewsKind.Walk when news is { Character: Guid id, At: Cell to } && _others.TryGetValue(id, out OtherPlayer? other):
                _others[id] = other with { At = to };
                break;
            case MapNewsKind.Gone when news.Character is Guid id:
                _others.Remove(id);
                break;
            case MapNewsKind.Fighting when news.Group is string group:
                _fighting.Add(group);
                _beaten.Remove(group);
                break;
            case MapNewsKind.Beaten when news is { Group: string group, Until: DateTimeOffset until }:
                _fighting.Remove(group);
                _beaten[group] = until;
                break;
            case MapNewsKind.Back when news.Group is string group:
                _fighting.Remove(group);
                _beaten.Remove(group);
                break;
            case MapNewsKind.Taken when news is { Spot: string spot, Until: DateTimeOffset until }:
                spots?.GrowsUntil(spot, until);
                break;
        }
    }
}
