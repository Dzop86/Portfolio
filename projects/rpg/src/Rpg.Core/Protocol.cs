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
/// in a town. The level is 1 until the progression comes (experience, characteristics).
/// </summary>
public sealed record CharacterSummary(Guid Id, string Name, string Look, string Class, int Colour, DateTimeOffset CreatedAt, Place? Place = null, string Server = Servers.Default, int Level = 1,
    int Hair = 0, int Skin = 0, int Height = 0, int Build = 0)
{
    [JsonIgnore]
    public Hero Hero => new(Name, Look, Class, Colour, Hair, Skin, Height, Build);
}

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
