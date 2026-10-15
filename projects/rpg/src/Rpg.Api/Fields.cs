using System.Collections.Concurrent;

namespace Rpg.Api;

/// <summary>
/// The state of the gathering spots (sprint 99, D71): when each spot gathered grows back, and when each
/// character last gathered (one gathering every 3 seconds). Kept in memory, in one server process: a spot
/// grows back in 2 to 5 minutes, so a restart that lets them all grow back at once harms nobody.
/// </summary>
public sealed class Fields(TimeProvider clock)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _growsBack = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastGathered = new();

    public DateTimeOffset Now => clock.GetUtcNow();

    /// <summary>When a spot can be gathered again, if it was gathered and has not grown back.</summary>
    public DateTimeOffset? GrowsBack(string node) => _growsBack.TryGetValue(node, out DateTimeOffset at) && at > Now ? at : null;

    /// <summary>The spots of a map still growing back, and when.</summary>
    public IReadOnlyDictionary<string, DateTimeOffset> Growing(IEnumerable<string> nodes) =>
        nodes.Select(n => (Node: n, At: GrowsBack(n))).Where(x => x.At is not null).ToDictionary(x => x.Node, x => x.At!.Value, StringComparer.Ordinal);

    /// <summary>Takes a spot for a character, if it is ready and the character has waited the gathering's time; false otherwise.</summary>
    public bool Take(string node, Guid character, TimeSpan regrowth, TimeSpan duration)
    {
        DateTimeOffset now = Now;
        if (_lastGathered.TryGetValue(character, out DateTimeOffset last) && now - last < duration)
            return false;
        lock (_growsBack)
        {
            if (GrowsBack(node) is not null)
                return false;
            _growsBack[node] = now + regrowth;
        }
        _lastGathered[character] = now;
        return true;
    }
}
