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
    IReadOnlyList<ItemCount>? Inventory = null, IReadOnlyDictionary<Slot, string>? Worn = null)
{
    [JsonIgnore]
    public Hero Hero => new(Name, Look, Class, Colour, Hair, Skin, Height, Build, Level, Stats, Ranks, Worn);

    /// <summary>The same summary: ranks and quests compare by content.</summary>
    public bool Equals(CharacterSummary? other) =>
        other is not null && (Id, CreatedAt, Place, Server, Xp) == (other.Id, other.CreatedAt, other.Place, other.Server, other.Xp)
        && Hero == other.Hero && (Quests ?? []).SequenceEqual(other.Quests ?? []) && (Inventory ?? []).SequenceEqual(other.Inventory ?? []);

    public override int GetHashCode() => HashCode.Combine(Id, Hero, Xp);
}

/// <summary>A fight the player is about to play, on the server's word: its scenario and the seed the server drew.</summary>
public sealed record FightTicket(Guid Id, string Scenario, [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)] ulong Seed);

/// <summary>A fight to start: one of the scenarios of the rules.</summary>
public sealed record NewFight(string? Scenario);

/// <summary>
/// What the server gave for a fight it replayed: the experience earned (the fight's and a quest's),
/// the new total and level, the quest done, if any, and the items won (loot and the quest's).
/// </summary>
public sealed record FightResult(long Xp, long TotalXp, int Level, string? Quest = null, IReadOnlyList<ItemCount>? Loot = null);

/// <summary>What a character is to wear, slot by slot: items it owns.</summary>
public sealed record Wear(IReadOnlyDictionary<Slot, string>? Worn);

/// <summary>How a character spends its points: characteristics, and the ranks of its spells (rank 1 when not listed).</summary>
public sealed record Points(Characteristics? Stats, IReadOnlyDictionary<string, int>? Ranks);

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
