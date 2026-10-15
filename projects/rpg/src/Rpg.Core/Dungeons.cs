namespace Rpg.Core;

/// <summary>
/// A dungeon (sprint 80, D65), as described in <c>data/dungeons.json</c>: one per family of monsters,
/// apart from the world. Its door stands on <see cref="Door"/> of the map <see cref="Map"/>, in the
/// family's zone; one goes in with its key (<see cref="Key"/>, used up) or the operator's keyring, and
/// fights its three rooms in turn, each on its own battle map (<see cref="Rooms"/>).
/// </summary>
public sealed record Dungeon(string Id, LocalizedText Name, string Family, string Map, Cell Door, string Key, IReadOnlyList<string> Rooms, IReadOnlyList<Hall>? Halls = null)
{
    /// <summary>The fight of room <paramref name="room"/> (1 to 3).</summary>
    public string Scenario(int room) => $"{Dungeons.Prefix}{Id}:{room}";

    /// <summary>The room a hall's map is, 1 to 3; 0 if it is not one of this dungeon's.</summary>
    public int RoomOf(string map) => (Halls ?? []).Select((h, i) => (h, i)).FirstOrDefault(p => p.h.Map == map) is { h: not null } found ? found.i + 1 : 0;

    /// <summary>The group guarding room <paramref name="room"/>, in its hall.</summary>
    public string GroupOf(int room) => $"{Id}.{room}";
}

/// <summary>A room of a dungeon one walks in (sprint 86, D67): its map, and where its group stands.</summary>
public sealed record Hall(string Map, Cell Group);

/// <summary>
/// The rooms of a dungeon (D65, Charles): the first with one monster of each role of the family
/// (melee, ranged, tank, support) weakened, the second the same at full strength, the third the same
/// and the chief.
/// </summary>
public static class Dungeons
{
    public const string Prefix = "dungeon:";

    public const int RoomCount = 3;

    /// <summary>The share of their hit points and damage the monsters of the first room keep, in percent.</summary>
    public const int EasyPower = 60;

    /// <summary>Opens every dungeon without being used up: the operator's, for tests (never dropped nor made).</summary>
    public const string Keyring = "admin-keyring";

    private static readonly MonsterRole[] Roles = [MonsterRole.Melee, MonsterRole.Ranged, MonsterRole.Tank, MonsterRole.Support];

    /// <summary>The roles met in a room, in order.</summary>
    public static IReadOnlyList<MonsterRole> RolesIn(int room) => room == RoomCount ? [.. Roles, MonsterRole.Boss] : Roles;

    /// <summary>The share of their strength the monsters of a room have.</summary>
    public static int PowerIn(int room) => room == 1 ? EasyPower : 100;

    /// <summary>The dungeon and room of a dungeon fight's id ("dungeon:" + dungeon + ":" + room), or null.</summary>
    public static (string Dungeon, int Room)? Parse(string scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (!scenario.StartsWith(Prefix, StringComparison.Ordinal))
            return null;
        string[] parts = scenario[Prefix.Length..].Split(':');
        return parts.Length == 2 && int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int room) && room is >= 1 and <= RoomCount
            ? (parts[0], room)
            : null;
    }

    /// <summary>The fight of a room: the room's battle map, a hero on team A, the room's monsters on team B.</summary>
    public static Scenario Scenario(Dungeon dungeon, int room, MonsterFamily family)
    {
        ArgumentNullException.ThrowIfNull(dungeon);
        ArgumentNullException.ThrowIfNull(family);
        FighterSpec hero = new(new LocalizedText("Héros", "Hero"), "female-d", 0, 80, 6, 3, 120, 0, ["strike", "arrow", "spear"]);
        int power = PowerIn(room);
        return new Scenario(dungeon.Scenario(room), dungeon.Name, dungeon.Rooms[room - 1],
            [hero, .. RolesIn(room).Select((role, i) => family[role].Fighter(i, power, family.Id))]);
    }

    /// <summary>The group guarding a room: one monster of each role (and the chief in the last), fighting that room's fight.</summary>
    public static MonsterGroup Guard(Dungeon dungeon, int room, MonsterFamily family)
    {
        ArgumentNullException.ThrowIfNull(dungeon);
        ArgumentNullException.ThrowIfNull(family);
        Hall hall = dungeon.Halls![room - 1];
        return new MonsterGroup(dungeon.GroupOf(room), hall.Map, family.Id, hall.Group, [.. RolesIn(room).Select(r => family[r].Id)], dungeon.Scenario(room));
    }

    /// <summary>Whether a character with these items may go into a dungeon: its key, or the keyring.</summary>
    public static bool CanEnter(Dungeon dungeon, IEnumerable<ItemCount> owned)
    {
        ArgumentNullException.ThrowIfNull(dungeon);
        ArgumentNullException.ThrowIfNull(owned);
        return owned.Any(i => i.Count > 0 && (i.Item == dungeon.Key || i.Item == Keyring));
    }

    /// <summary>What going in uses up: the key, unless the keyring opens it.</summary>
    public static ItemCount? UsedOnEntry(Dungeon dungeon, IEnumerable<ItemCount> owned)
    {
        ArgumentNullException.ThrowIfNull(dungeon);
        ArgumentNullException.ThrowIfNull(owned);
        return owned.Any(i => i.Item == Keyring && i.Count > 0) ? null : new ItemCount(dungeon.Key, 1);
    }
}
