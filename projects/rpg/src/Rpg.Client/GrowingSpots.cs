using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The gathering spots of a map growing back (sprint 103, Charles: the resource and its gem vanish once
/// gathered, and come back after a fixed time), each with when it is ready again: as the server said on
/// entering the map, or from the gathering just made.
/// </summary>
public sealed class GrowingSpots
{
    private readonly Dictionary<string, DateTimeOffset> _until = new(StringComparer.Ordinal);

    /// <summary>The spots growing back now.</summary>
    public IReadOnlyCollection<string> Ids => _until.Keys;

    /// <summary>The spots the server says are growing back, and until when; the others are ready.</summary>
    public void Reset(IReadOnlyDictionary<string, DateTimeOffset> growing)
    {
        ArgumentNullException.ThrowIfNull(growing);
        _until.Clear();
        foreach ((string id, DateTimeOffset at) in growing)
            _until[id] = at;
    }

    /// <summary>A spot just gathered: back after <see cref="Harvesting.Regrowth"/>.</summary>
    public void Taken(string id, DateTimeOffset now) => _until[id] = now + Harvesting.Regrowth;

    public bool Contains(string id) => _until.ContainsKey(id);

    /// <summary>The spots whose time has come, no longer growing: to draw again.</summary>
    public IReadOnlyList<string> Ready(DateTimeOffset now)
    {
        string[] ready = [.. _until.Where(x => x.Value <= now).Select(x => x.Key).Order(StringComparer.Ordinal)];
        foreach (string id in ready)
            _until.Remove(id);
        return ready;
    }

    /// <summary>How far a gathering is, from 0 to 1, for its bar (sprint 103).</summary>
    public static double Progress(TimeSpan elapsed) => Math.Clamp(elapsed / Harvesting.Duration, 0, 1);
}
