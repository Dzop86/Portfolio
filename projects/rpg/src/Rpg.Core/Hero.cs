using System.Text.RegularExpressions;

namespace Rpg.Core;

/// <summary>
/// The player's character as the rules see it: it takes the place of the scenario's first fighter of
/// team A, keeping its characteristics. Its name and look only change what is shown, so a recorded
/// fight replays the same with any hero; classes (sprint 48) will change its spells.
/// </summary>
public sealed partial record Hero(string Name, string Look)
{
    /// <summary>The looks a player may choose (Kenney's Mini Characters); monsters use others.</summary>
    public static readonly IReadOnlyList<string> Looks =
        ["female-a", "female-b", "female-c", "female-d", "female-e", "female-f", "male-a", "male-b", "male-c", "male-d", "male-e", "male-f"];

    /// <summary>3 to 20 letters, with single hyphens or apostrophes inside: "Élise", "Jean-Luc", "O'Neil".</summary>
    [GeneratedRegex(@"^\p{L}+(?:['-]\p{L}+)*$")]
    private static partial Regex NamePattern();

    public static bool IsValidName(string? name) => name is { Length: >= 3 and <= 20 } && NamePattern().IsMatch(name);

    /// <summary>Why this hero cannot fight, or null.</summary>
    public string? Problem() =>
        !IsValidName(Name) ? "A name has 3 to 20 letters, with single hyphens or apostrophes inside."
        : !Looks.Contains(Look) ? $"Unknown look '{Look}'."
        : null;
}
