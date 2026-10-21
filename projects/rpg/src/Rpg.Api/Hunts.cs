using System.Collections.Concurrent;
using Rpg.Core;

namespace Rpg.Api;

/// <summary>
/// The groups of monsters of the world as every player sees them (sprint 118, D74): a group is fought by one
/// character at a time, then away for <see cref="Roam.Respawn"/> once beaten. Kept in memory, in one server process,
/// like the gathering spots: a restart brings every group back, which harms nobody. A fight never reported frees its
/// group after <see cref="Forgotten"/>. The groups of a dungeon's halls are each player's own and are not kept here.
/// </summary>
public sealed class Hunts(TimeProvider clock)
{
    public static readonly TimeSpan Forgotten = TimeSpan.FromMinutes(30);

    private sealed record Hunt(Guid? Fighter, DateTimeOffset Since, DateTimeOffset? BeatenUntil);

    private readonly ConcurrentDictionary<string, Hunt> _hunts = new(StringComparer.Ordinal);

    /// <summary>The group of the world a scenario fights ("group:woods-2-2.0", or a group's own fight), none for the others.</summary>
    public static MonsterGroup? GroupOf(string scenario) =>
        GameData.Embedded.Groups.Values.FirstOrDefault(g => g.Scenario == scenario && GameData.Embedded.Towns[g.Map].Dungeon is null);

    /// <summary>A character starts fighting a group: false when someone else fights it, or it is beaten and not back.</summary>
    public bool Start(string group, Guid character)
    {
        DateTimeOffset now = clock.GetUtcNow();
        lock (_hunts)
        {
            // A character fights one group at a time: a fight it never reported frees the group it was on.
            foreach ((string other, Hunt h) in _hunts.Where(x => x.Value.Fighter == character && x.Key != group).ToList())
                _hunts.TryRemove(other, out _);
            if (_hunts.TryGetValue(group, out Hunt? hunt) && (hunt.BeatenUntil > now || (hunt.Fighter is Guid fighter && fighter != character && now - hunt.Since < Forgotten)))
                return false;
            _hunts[group] = new Hunt(character, now, null);
            return true;
        }
    }

    /// <summary>The character's fight on the group is over: beaten until a moment, or back at once. Null when it was not its fight.</summary>
    public DateTimeOffset? End(string group, Guid character, bool won, out bool ended)
    {
        DateTimeOffset now = clock.GetUtcNow();
        lock (_hunts)
        {
            ended = _hunts.TryGetValue(group, out Hunt? hunt) && hunt.Fighter == character;
            if (!ended)
                return null;
            if (!won)
            {
                _hunts.TryRemove(group, out _);
                return null;
            }
            _hunts[group] = new Hunt(null, now, now + Roam.Respawn);
            return now + Roam.Respawn;
        }
    }

    /// <summary>What a newcomer on a map is told of its groups: those fought now, those beaten and when they come back.</summary>
    public IReadOnlyList<MapNews> On(string map)
    {
        DateTimeOffset now = clock.GetUtcNow();
        return [.. GameData.Embedded.GroupsOn(map).Select(g => (g.Id, Hunt: _hunts.GetValueOrDefault(g.Id))).Where(x => x.Hunt is not null)
            .Select(x => x.Hunt!.BeatenUntil > now ? new MapNews(MapNewsKind.Beaten, Group: x.Id, Until: x.Hunt.BeatenUntil)
                : x.Hunt.Fighter is not null && now - x.Hunt.Since < Forgotten ? new MapNews(MapNewsKind.Fighting, Group: x.Id)
                : null)
            .OfType<MapNews>()];
    }
}
