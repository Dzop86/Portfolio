using Rpg.Core;

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

/// <summary>
/// A character: unique name over the whole server (case aside), a look among the playable ones, a
/// class of <c>data/classes.json</c> and an outfit colour.
/// </summary>
public sealed class Character
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string Look { get; set; }
    public required string Class { get; set; }
    public int Colour { get; set; }

    /// <summary>The game server the character lives on.</summary>
    public string Server { get; set; } = Servers.Default;

    public int Level { get; set; } = 1;

    /// <summary>The appearance beyond look and colour: hair colour, skin tone, height, build (0: as drawn).</summary>
    public int Hair { get; set; }
    public int Skin { get; set; }
    public int Height { get; set; }
    public int Build { get; set; }

    /// <summary>Where the character stands: a town and a cell, or nothing before their first walk.</summary>
    public string? Town { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Experience, earned only from fights the server replayed; the level follows it.</summary>
    public long Xp { get; set; }

    /// <summary>The characteristic points spent (sprint 58).</summary>
    public int Vitality { get; set; }
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int Chance { get; set; }
    public int Agility { get; set; }

    /// <summary>The ranks of the spells above 1, as "spell:rank" separated by commas.</summary>
    public string Ranks { get; set; } = "";

    /// <summary>The quests done, their ids separated by commas.</summary>
    public string Quests { get; set; } = "";

    public Characteristics Stats => new(Vitality, Strength, Intelligence, Chance, Agility);

    public Dictionary<string, int> RankList => Ranks.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(r => r.Split(':')).ToDictionary(r => r[0], r => int.Parse(r[1], System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);

    public string[] QuestList => Quests.Split(',', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The character as the rules see it.</summary>
    public Hero Hero => new(Name, Look, Class, Colour, Hair, Skin, Height, Build, Level, Stats, RankList);
}

/// <summary>
/// A fight the server allowed a character to play: the scenario and the seed it drew. Its record
/// comes back once, is replayed, and the ticket is gone: the same fight cannot earn twice.
/// </summary>
public sealed class PendingFight
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }
    public Character? Character { get; set; }
    public required string Scenario { get; set; }
    public long Seed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
