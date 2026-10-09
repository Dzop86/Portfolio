namespace Rpg.Api.Data;

/// <summary>A player's account: a name, a password hash (PBKDF2, ASP.NET Core Identity's hasher), its characters.</summary>
public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }

    /// <summary>The name in upper case: two accounts cannot differ only by case.</summary>
    public required string NormalizedName { get; set; }

    public string PasswordHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public List<Character> Characters { get; } = [];
}

/// <summary>A character: unique name over the whole server (case aside), a look among the playable ones.</summary>
public sealed class Character
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string Look { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
