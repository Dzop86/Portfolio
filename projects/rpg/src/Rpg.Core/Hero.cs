using System.Text.RegularExpressions;

namespace Rpg.Core;

/// <summary>
/// The player's character as the rules see it: it takes the place of the scenario's first fighter of
/// team A. Its class (<c>data/classes.json</c>) gives its characteristics and spells; without one, it
/// keeps the scenario's. Its name and appearance (look, outfit colour, hair colour, skin tone,
/// height, build) only change what is shown.
/// </summary>
public sealed partial record Hero(string Name, string Look, string? Class = null, int Colour = 0, int Hair = 0, int Skin = 0, int Height = 0, int Build = 0, int Level = 1)
{
    /// <summary>The outfit colours: the client turns the models' palette by 0 to 6 steps.</summary>
    public const int Colours = 7;

    /// <summary>Hair colours: 0 is the look's own, then the client's palette of hair colours.</summary>
    public const int HairColours = 8;

    /// <summary>Skin tones: 0 is the look's own, then the client's palette of skin tones.</summary>
    public const int SkinTones = 5;

    /// <summary>Height and build go from -<see cref="Shape"/> (smaller, slimmer) to +<see cref="Shape"/>.</summary>
    public const int Shape = 2;

    /// <summary>The highest level, for now.</summary>
    public const int MaxLevel = 100;

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
            : Hair is < 0 or >= HairColours ? $"The hair colour is 0 to {HairColours - 1}."
            : Skin is < 0 or >= SkinTones ? $"The skin tone is 0 to {SkinTones - 1}."
            : Math.Abs(Height) > Shape || Math.Abs(Build) > Shape ? $"Height and build are -{Shape} to {Shape}."
            : Level is < 1 or > MaxLevel ? $"The level is 1 to {MaxLevel}."
            : Class is not null && data.Class(Class) is null ? $"Unknown class '{Class}'."
            : null;
    }
}
