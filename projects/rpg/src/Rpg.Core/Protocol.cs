namespace Rpg.Core;

// What the game and its server exchange, in JSON (camelCase). Shared, so both sides cannot drift apart.

/// <summary>A name and a password, to create an account or to sign in.</summary>
public sealed record Credentials(string? Name, string? Password);

public sealed record AccountCreated(string Name);

/// <summary>A signed access token (JWT) and the moment it stops working.</summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>A character to create: its name, look, class (<c>data/classes.json</c>) and outfit colour (0 to 6).</summary>
public sealed record NewCharacter(string? Name, string? Look, string? Class = null, int Colour = 0);

/// <summary>One of the player's characters.</summary>
public sealed record CharacterSummary(Guid Id, string Name, string Look, string Class, int Colour, DateTimeOffset CreatedAt)
{
    public Hero Hero => new(Name, Look, Class, Colour);
}

public static class Accounts
{
    /// <summary>Characters per account, as in the games of the genre.</summary>
    public const int MaxCharacters = 5;
}
