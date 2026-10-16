using System.Text.Json.Serialization;

namespace Rpg.Core;

// What the game and its server exchange, in JSON (camelCase). Shared, so both sides cannot drift apart.

/// <summary>A name and a password, to create an account or to sign in.</summary>
public sealed record Credentials(string? Name, string? Password);

public sealed record AccountCreated(string Name);

/// <summary>A signed access token (JWT) and the moment it stops working.</summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// A character to create: its name, look, class (<c>data/classes.json</c>), outfit colour (0 to 6)
/// and server (null: the first one).
/// </summary>
public sealed record NewCharacter(string? Name, string? Look, string? Class = null, int Colour = 0, string? Server = null, int Hair = 0, int Skin = 0, int Height = 0, int Build = 0);

/// <summary>A game server the player can choose before their characters, as in the games of the genre.</summary>
public sealed record ServerInfo(string Id, string Name);

/// <summary>One of the player's characters.</summary>
/// <summary>Where a character stands in a town; the game brings them back there.</summary>
public sealed record Place(string Town, int X, int Y)
{
    [JsonIgnore]
    public Cell Cell => new(X, Y);
}

/// <summary>
/// One of the player's characters, on one server; <see cref="Place"/> is null until they first walk
/// in a town. <see cref="Inventory"/> holds everything it owns, what it wears included
/// (<see cref="Worn"/>, sprint 59). Its level follows its experience; its points are spent as <see cref="Stats"/> and
/// <see cref="Ranks"/> say (sprint 58); <see cref="Quests"/> are the ids of the quests it has done.
/// </summary>
public sealed record CharacterSummary(Guid Id, string Name, string Look, string Class, int Colour, DateTimeOffset CreatedAt, Place? Place = null, string Server = Servers.Default, int Level = 1,
    int Hair = 0, int Skin = 0, int Height = 0, int Build = 0, long Xp = 0, Characteristics? Stats = null, IReadOnlyDictionary<string, int>? Ranks = null, IReadOnlyList<string>? Quests = null,
    IReadOnlyList<ItemCount>? Inventory = null, IReadOnlyDictionary<Slot, string>? Worn = null, IReadOnlyList<string>? Deck = null, DungeonRun? Run = null,
    IReadOnlyDictionary<string, long>? Jobs = null, long Shards = 0, IReadOnlyList<QuestProgress>? QuestLog = null,
    int? Hp = null, DateTimeOffset? HpAt = null, bool InFight = false)
{
    /// <summary>The character's hit points at a moment (sprint 101): set, grown back since, frozen in a fight.</summary>
    public int LifeAt(DateTimeOffset now, GameData data) => Life.Now(Life.MaxHp(Hero, data), Hp, HpAt, InFight, Place?.Town, now);

    [JsonIgnore]
    public Hero Hero => new(Name, Look, Class, Colour, Hair, Skin, Height, Build, Level, Stats, Ranks, Worn, Deck);

    /// <summary>The same summary: ranks and quests compare by content.</summary>
    public bool Equals(CharacterSummary? other) =>
        other is not null && (Id, CreatedAt, Place, Server, Xp, Shards, Hp, HpAt, InFight) == (other.Id, other.CreatedAt, other.Place, other.Server, other.Xp, other.Shards, other.Hp, other.HpAt, other.InFight)
        && Hero == other.Hero && (Quests ?? []).SequenceEqual(other.Quests ?? []) && (QuestLog ?? []).SequenceEqual(other.QuestLog ?? []) && (Inventory ?? []).SequenceEqual(other.Inventory ?? []) && Run == other.Run
        && (Jobs ?? new Dictionary<string, long>()).OrderBy(j => j.Key, StringComparer.Ordinal).SequenceEqual((other.Jobs ?? new Dictionary<string, long>()).OrderBy(j => j.Key, StringComparer.Ordinal));

    /// <summary>A trade's level, from its experience (1 before any).</summary>
    public int JobLevel(string job) => Crafting.LevelFor(Jobs?.GetValueOrDefault(job) ?? 0);

    public override int GetHashCode() => HashCode.Combine(Id, Hero, Xp);
}

/// <summary>A fight the player is about to play, on the server's word: its scenario, the seed the server drew, and since sprint 101 the hero's hit points to start with (null: all).</summary>
public sealed record FightTicket(Guid Id, string Scenario, [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)] ulong Seed, int? HeroHp = null);

/// <summary>A fight to start: one of the scenarios of the rules.</summary>
public sealed record NewFight(string? Scenario);

/// <summary>
/// What the server gave for a fight it replayed: the experience earned (the fight's, a quest's, a
/// dungeon's last room's), the new total and level, the quest done, if any, and the items won (loot and
/// the quest's); in a dungeon (sprint 80), the room to fight next (null when out of it) and whether it
/// was just finished; since sprint 87, the Éclats earned and the character's new total.
/// </summary>
public sealed record FightResult(long Xp, long TotalXp, int Level, string? Quest = null, IReadOnlyList<ItemCount>? Loot = null, DungeonRun? Run = null, bool DungeonDone = false,
    long Shards = 0, long TotalShards = 0);

/// <summary>
/// Where a character is in a dungeon (sprint 80): the dungeon, and the room to fight (1 to 3); since
/// sprint 86, the hit points kept from the room before (null: all of them, until a fountain heals).
/// </summary>
public sealed record DungeonRun(string Dungeon, int Room, int? Hp = null)
{
    /// <summary>At that room of that dungeon, whatever the hit points.</summary>
    public bool IsAt(string dungeon, int room) => (Dungeon, Room) == (dungeon, room);

    /// <summary>The room of a dungeon's hall is behind: its group beaten, its far door open.</summary>
    public bool Cleared(string dungeon, int room) => Dungeon == dungeon && room < Room;
}

/// <summary>Items to make at a trade's workshop (sprint 81): the recipe's item, how many.</summary>
public sealed record Craft(string? Job, string? Item, int Count = 1);

/// <summary>An item to use out of a fight (sprint 101): food, which heals.</summary>
public sealed record Use(string? Item);

/// <summary>A gathering spot to gather (sprint 99), from the cell the character stands on.</summary>
public sealed record Gather(string? Node, Cell From);

/// <summary>What a gathering gave: the character after it, the resource, the trade's experience, when the spot grows back.</summary>
public sealed record Gathered(CharacterSummary Character, ItemCount Item, long Xp, DateTimeOffset GrowsBack);

/// <summary>The spots of a map still growing back (sprint 99), and when each will be ready.</summary>
public sealed record Growing(IReadOnlyDictionary<string, DateTimeOffset> Spots);

/// <summary>Items to buy from Rose or to sell her (sprint 97): which, how many.</summary>
public sealed record Trade(string? Item, int Count = 1);

/// <summary>A quest to take from its giver (sprint 88).</summary>
public sealed record TakeQuest(string? Quest);

/// <summary>Someone talked to ("town/npc") for a quest under way (sprint 88).</summary>
public sealed record QuestTalk(string? Npc);

/// <summary>What one examines for a quest under way (sprint 105): a thing on a map, by its id.</summary>
public sealed record QuestInteract(string? Prop);

/// <summary>
/// What a quest handed back gave (sprint 88): its experience, Éclats and items, the character's new
/// totals and level, and the character itself (its quests, inventory).
/// </summary>
public sealed record QuestReward(string Quest, long Xp, long Shards, IReadOnlyList<ItemCount> Items, int Level, CharacterSummary Character);

/// <summary>A dungeon to go into, through its door.</summary>
public sealed record EnterDungeon(string? Dungeon);

/// <summary>What a character is to wear, slot by slot: items it owns.</summary>
public sealed record Wear(IReadOnlyDictionary<Slot, string>? Worn);

/// <summary>How a character spends its points: characteristics, and the ranks of its spells (rank 1 when not listed).</summary>
public sealed record Points(Characteristics? Stats, IReadOnlyDictionary<string, int>? Ranks, IReadOnlyList<string>? Deck = null);

public static class Servers
{
    /// <summary>The first server, and the one of the characters made before servers.</summary>
    public const string Default = "osmeria";
}

public static class Accounts
{
    /// <summary>Characters per account, as in the games of the genre.</summary>
    public const int MaxCharacters = 5;
}
