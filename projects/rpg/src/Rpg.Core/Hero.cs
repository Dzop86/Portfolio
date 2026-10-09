using System.Text.RegularExpressions;

namespace Rpg.Core;

/// <summary>
/// The player's character as the rules see it: it takes the place of the scenario's first fighter of
/// team A. Its class (<c>data/classes.json</c>) gives its characteristics and spells; without one, it
/// keeps the scenario's. Its name, look and colour only change what is shown.
/// </summary>
public sealed partial record Hero(string Name, string Look, string? Class = null, int Colour = 0)
{
    /// <summary>The outfit colours: the client turns the models' palette by 0 to 6 steps.</summary>
    public const int Colours = 7;

    /// <summary>The looks a player may choose (Kenney's Mini Characters); monsters use others.</summary>
    public static readonly IReadOnlyList<string> Looks =
        ["female-a", "female-b", "female-c", "female-d", "female-e", "female-f", "male-a", "male-b", "male-c", "male-d", "male-e", "male-f"];

    /// <summary>3 to 20 letters, with single hyphens or apostrophes inside: "Élise", "Jean-Luc", "O'Neil".</summary>
    [GeneratedRegex(@"^\p{L}+(?:['-]\p{L}+)*$")]
    private static partial Regex NamePattern();

    public static bool IsValidName(string? name) => name is { Length: >= 3 and <= 20 } && NamePattern().IsMatch(name);

    /// <summary>Why this hero cannot fight with these data, or null.</summary>
    public string? Problem(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return !IsValidName(Name) ? "A name has 3 to 20 letters, with single hyphens or apostrophes inside."
            : !Looks.Contains(Look) ? $"Unknown look '{Look}'."
            : Colour is < 0 or >= Colours ? $"The colour is 0 to {Colours - 1}."
            : Class is not null && data.Class(Class) is null ? $"Unknown class '{Class}'."
            : null;
    }
}
