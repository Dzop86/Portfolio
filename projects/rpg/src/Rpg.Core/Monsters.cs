using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>What a monster does in its family (D63): every family has one of each.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MonsterRole>))]
public enum MonsterRole
{
    /// <summary>Hits hard at close range.</summary>
    Melee,

    /// <summary>Hits from afar.</summary>
    Ranged,

    /// <summary>Takes the blows: many hit points, resistances.</summary>
    Tank,

    /// <summary>Heals or shields the others.</summary>
    Support,

    /// <summary>The family's chief, met on one map of its zone only.</summary>
    Boss,
}

/// <summary>A monster, as described in <c>data/monsters.json</c>: a fighter of team B, with its level, role and loot.</summary>
public sealed record MonsterSpec(
    string Id,
    MonsterRole Role,
    LocalizedText Name,
    string Look,
    int Level,
    int Hp,
    int Ap,
    int Mp,
    int Initiative,
    IReadOnlyList<string> Spells,
    IReadOnlyDictionary<Element, int>? Resistances = null,
    IReadOnlyList<LootDrop>? Loot = null)
{
    /// <summary>
    /// The monster as a fighter of team B on start cell <paramref name="start"/>, at <paramref name="power"/>
    /// percent of its hit points and damage (a dungeon's first room, sprint 80).
    /// </summary>
    public FighterSpec Fighter(int start, int power = 100) =>
        new(Name, Look, 1, Math.Max(1, Hp * power / 100), Ap, Mp, Initiative, start, Spells, Resistances, null, null, Level, Loot, power);
}

/// <summary>
/// A family of monsters (sprint 72, D63): five monsters of the same kind, one per role, living in a
/// zone; their fights take place on <see cref="Battle"/>, and the boss is met on <see cref="BossMap"/>.
/// </summary>
public sealed record MonsterFamily(string Id, LocalizedText Name, string Zone, string Battle, string BossMap, IReadOnlyList<MonsterSpec> Monsters)
{
    public MonsterSpec this[MonsterRole role] => Monsters.First(m => m.Role == role);
}

/// <summary>
/// A group of monsters on a map: where it lives (<see cref="Home"/>, it wanders around it) and who is
/// in it. Fighting it is the scenario <see cref="Scenario"/>, which the rules build from the group.
/// </summary>
public sealed record MonsterGroup(string Id, string Map, string Family, Cell Home, IReadOnlyList<string> Monsters, string? Fight = null)
{
    public const string Prefix = "group:";

    /// <summary>Its fight: the group's own, or, guarding a dungeon's room (sprint 86), that room's.</summary>
    public string Scenario => Fight ?? Prefix + Id;
}

/// <summary>
/// Where the groups of monsters live (sprints 72 to 74): drawn once from each map's name, so that the
/// game, the server and the tests see the same groups. Two to four groups on every map of a zone that
/// has families, each of one family and of one to ten monsters: one to three near the zone's way in,
/// up to six a little farther, up to ten at the far end (<see cref="Depth"/>); the chief, on its map,
/// with five to nine of its family. Groups of one to three are for a player alone; of six and more,
/// for several (D63, Charles). A support is never alone, and one for three monsters at most. Their
/// homes are free cells reachable from the arrival, away from the ways, the exits and the arrival,
/// and apart from one another.
/// </summary>
public static class MonsterGroups
{
    public const int Fewest = 2, Most = 4;

    /// <summary>The largest group; a battle map has room for that many monsters.</summary>
    public const int Largest = 10;

    /// <summary>How far a group's home is from a way, an exit or the arrival, and from another group, at least.</summary>
    public const int Distance = 3;

    /// <summary>The roles a group is drawn from, besides the chief.</summary>
    private static readonly MonsterRole[] Common = [MonsterRole.Melee, MonsterRole.Ranged, MonsterRole.Tank, MonsterRole.Support];

    // The sizes of groups, in tenths of a hundred, by depth: near the way in, farther, at the far end.
    private static readonly int[][] Sizes =
    [
        [50, 35, 15],
        [25, 30, 25, 10, 5, 5],
        [15, 20, 20, 10, 10, 8, 6, 5, 3, 3],
    ];

    public static IReadOnlyList<MonsterGroup> For(Town map, IReadOnlyList<MonsterFamily> families, IReadOnlyCollection<Town> world)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(families);
        ArgumentNullException.ThrowIfNull(world);
        MonsterFamily[] here = [.. families.Where(f => map.Zone is not null && f.Zone == map.Zone)];
        if (here.Length == 0)
            return [];
        var rng = new Rng(Seed(map.Id));
        Board board = map.Board();
        Cell[] away = [.. (map.Links ?? []).Select(l => l.At), .. map.Exits.Select(e => e.At), .. map.Doors.Select(d => d.At), .. (map.Workshops ?? []).Select(w => w.At), map.Spawn];
        var homes = Pathfinding.Reachable(board, map.Spawn, board.Width * board.Height, c => map.IsFree(board, c)).Keys
            .Where(c => away.All(a => a.DistanceTo(c) >= Distance))
            .OrderBy(c => c.Y).ThenBy(c => c.X)
            .ToList();
        var groups = new List<MonsterGroup>();
        MonsterFamily? chief = here.FirstOrDefault(f => f.BossMap == map.Id);
        int depth = Depth(map, world), count = rng.Next(Fewest, Most);
        var taken = new List<Cell>();
        for (int i = 0; i < count && homes.Count > 0; i++)
        {
            Cell home = homes[rng.Next(0, homes.Count - 1)];
            // Never on a pontoon or in a gap the ways need: everything must stay reachable around the groups.
            if (!KeepsWaysOpen(map, board, [.. taken, home]))
            {
                homes.Remove(home);
                i--;
                continue;
            }
            taken.Add(home);
            homes.RemoveAll(c => c.DistanceTo(home) < Distance);
            bool boss = i == 0 && chief is not null;
            MonsterFamily family = boss ? chief! : here[rng.Next(0, here.Length - 1)];
            List<MonsterRole> roles = boss ? [MonsterRole.Boss] : [];
            int size = boss ? rng.Next(6, Largest) : Size(rng, depth);
            while (roles.Count < size)
                roles.Add(Draw(rng, alone: size == 1, support: roles.Count(r => r == MonsterRole.Support) < size / 3));
            groups.Add(new MonsterGroup($"{map.Id}.{i}", map.Id, family.Id, home, [.. roles.Select(r => family[r].Id)]));
        }
        return groups;
    }

    /// <summary>
    /// Whether the ways, the exits and the arrival of a map can all still be reached from one another
    /// with groups of monsters standing on <paramref name="occupied"/> (a group must not cut a map in two).
    /// </summary>
    public static bool KeepsWaysOpen(Town map, Board board, IReadOnlyCollection<Cell> occupied)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(occupied);
        var reach = Pathfinding.Reachable(board, map.Spawn, board.Width * board.Height, c => map.IsFree(board, c) && !occupied.Contains(c)).Keys.ToHashSet();
        return (map.Links ?? []).Select(l => l.At).Concat(map.Exits.Select(e => e.At)).Concat(map.Doors.Select(d => d.At)).Concat((map.Workshops ?? []).Select(w => w.At)).All(reach.Contains)
            && map.Npcs.All(n => n.At.Neighbours().Any(reach.Contains));
    }

    /// <summary>
    /// How deep a map is in its zone: 0 next to the way in, 1 a little farther, 2 at the far end. The
    /// way in: the maps of easier zones (a lower level) that open on this one (the village for the
    /// woods, the woods for the others).
    /// </summary>
    public static int Depth(Town map, IReadOnlyCollection<Town> world)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(world);
        HashSet<string> zone = [.. world.Where(t => t.ZoneId == map.ZoneId).Select(t => t.Id)];
        Cell[] ways = [.. world.Where(t => t.ZoneId != map.ZoneId && t.Level < map.Level && (t.Links ?? []).Any(l => zone.Contains(l.To))).Select(t => t.MapAt)];
        int distance = ways.Length == 0 ? 0 : ways.Min(w => w.DistanceTo(map.MapAt));
        return distance switch
        {
            <= 2 => 0,
            <= 4 => 1,
            _ => 2,
        };
    }

    /// <summary>The largest group of a map at that depth: 3, 6 or 10.</summary>
    public static int LargestAt(int depth) => Sizes[depth].Length;

    private static int Size(Rng rng, int depth)
    {
        int roll = rng.Next(1, 100);
        int[] weights = Sizes[depth];
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
                return i + 1;
        }
        return weights.Length;
    }

    /// <summary>A role for one more monster: a support neither alone nor beyond the share allowed.</summary>
    private static MonsterRole Draw(Rng rng, bool alone, bool support) => Common[rng.Next(0, alone || !support ? 2 : Common.Length - 1)];

    /// <summary>A seed from a name, the same on every platform (FNV-1a, 64 bits).</summary>
    public static ulong Seed(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        ulong hash = 14695981039346656037UL;
        foreach (char c in name)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    /// <summary>
    /// The fight against a group: the family's battle map, a hero on team A (played by the player's
    /// character), the group's monsters on team B in their order.
    /// </summary>
    public static Scenario Scenario(MonsterGroup group, MonsterFamily family, IReadOnlyDictionary<string, MonsterSpec> monsters)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(monsters);
        FighterSpec hero = new(new LocalizedText("Héros", "Hero"), "female-d", 0, 80, 6, 3, 120, 0, ["strike", "arrow", "spear"]);
        return new Scenario(group.Scenario, family.Name, family.Battle, [hero, .. group.Monsters.Select((id, i) => monsters[id].Fighter(i))]);
    }
}
