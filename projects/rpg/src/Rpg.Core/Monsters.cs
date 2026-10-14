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
    /// <summary>The monster as a fighter of team B on start cell <paramref name="start"/>.</summary>
    public FighterSpec Fighter(int start) => new(Name, Look, 1, Hp, Ap, Mp, Initiative, start, Spells, Resistances, null, null, Level, Loot);
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
public sealed record MonsterGroup(string Id, string Map, string Family, Cell Home, IReadOnlyList<string> Monsters)
{
    public const string Prefix = "group:";

    public string Scenario => Prefix + Id;
}

/// <summary>
/// Where the groups of monsters live (sprint 72): drawn once from each map's name, so that the game,
/// the server and the tests see the same groups. Two groups on every map of a zone that has families
/// (the boss's map: the boss with one of its family, and one more group), each of one family, one to
/// three monsters (fewer near the village, see <see cref="Largest"/>), a support never alone nor
/// twice; their homes on free cells reachable from the arrival, away
/// from the ways, the exits and the arrival, and apart from one another.
/// </summary>
public static class MonsterGroups
{
    public const int PerMap = 2;

    /// <summary>How far a group's home is from a way, an exit or the arrival, and from another group, at least.</summary>
    public const int Distance = 3;

    /// <summary>The roles a group is drawn from, besides the boss.</summary>
    private static readonly MonsterRole[] Common = [MonsterRole.Melee, MonsterRole.Ranged, MonsterRole.Tank, MonsterRole.Support];

    public static IReadOnlyList<MonsterGroup> For(Town map, IReadOnlyList<MonsterFamily> families)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(families);
        MonsterFamily[] here = [.. families.Where(f => map.Zone is not null && f.Zone == map.Zone)];
        if (here.Length == 0)
            return [];
        var rng = new Rng(Seed(map.Id));
        Board board = map.Board();
        Cell[] away = [.. (map.Links ?? []).Select(l => l.At), .. map.Exits.Select(e => e.At), map.Spawn];
        var homes = Pathfinding.Reachable(board, map.Spawn, board.Width * board.Height, c => map.IsFree(board, c)).Keys
            .Where(c => away.All(a => a.DistanceTo(c) >= Distance))
            .OrderBy(c => c.Y).ThenBy(c => c.X)
            .ToList();
        var groups = new List<MonsterGroup>();
        MonsterFamily? boss = here.FirstOrDefault(f => f.BossMap == map.Id);
        for (int i = 0; i < PerMap && homes.Count > 0; i++)
        {
            Cell home = homes[rng.Next(0, homes.Count - 1)];
            homes.RemoveAll(c => c.DistanceTo(home) < Distance);
            MonsterFamily family = i == 0 && boss is not null ? boss : here[rng.Next(0, here.Length - 1)];
            List<MonsterRole> roles = i == 0 && boss is not null ? [MonsterRole.Boss] : [];
            int size = roles.Count == 1 ? 2 : Math.Min(Size(rng), Largest(map));
            while (roles.Count < size)
                roles.Add(Draw(rng, alone: size == 1, support: !roles.Contains(MonsterRole.Support)));
            groups.Add(new MonsterGroup($"{map.Id}.{i}", map.Id, family.Id, home, [.. roles.Select(r => family[r].Id)]));
        }
        return groups;
    }

    /// <summary>
    /// The largest group of a map: one monster next to the village (at [0, 0]), up to two a little
    /// farther, three beyond, so that the woods grow harder as one goes in.
    /// </summary>
    public static int Largest(Town map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return map.MapAt.DistanceTo(new Cell(0, 0)) switch
        {
            <= 2 => 1,
            <= 4 => 2,
            _ => 3,
        };
    }

    /// <summary>One monster four times in ten, two four times, three twice.</summary>
    private static int Size(Rng rng) => rng.Next(1, 10) switch
    {
        <= 4 => 1,
        <= 8 => 2,
        _ => 3,
    };

    /// <summary>A role for one more monster: a support neither alone nor twice in a group.</summary>
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
