using System.Text.RegularExpressions;

namespace Rpg.Core;

/// <summary>
/// The player's character as the rules see it: it takes the place of the scenario's first fighter of
/// team A. Its class (<c>data/classes.json</c>) gives its characteristics and spells; without one, it
/// keeps the scenario's. Its name and appearance (look, outfit colour, hair colour, skin tone,
/// height, build) only change what is shown. With its level come points (<see cref="Progression"/>):
/// the characteristics it was given (<see cref="Stats"/>) and the ranks of its spells
/// (<see cref="Ranks"/>, rank 1 when not listed); and what it wears (<see cref="Worn"/>, sprint 59).
/// </summary>
public sealed partial record Hero(string Name, string Look, string? Class = null, int Colour = 0, int Hair = 0, int Skin = 0, int Height = 0, int Build = 0, int Level = 1,
    Characteristics? Stats = null, IReadOnlyDictionary<string, int>? Ranks = null, IReadOnlyDictionary<Slot, string>? Worn = null)
{
    /// <summary>The same hero: the ranks compare by content, not by dictionary.</summary>
    public bool Equals(Hero? other) =>
        other is not null && (Name, Look, Class, Colour, Hair, Skin, Height, Build, Level, Stats ?? Characteristics.None) == (other.Name, other.Look, other.Class, other.Colour, other.Hair, other.Skin, other.Height, other.Build, other.Level, other.Stats ?? Characteristics.None)
        && RanksGiven.SequenceEqual(other.RanksGiven) && WornItems.SequenceEqual(other.WornItems);

    public override int GetHashCode() => HashCode.Combine(Name, Look, Class, Colour, Level, Stats ?? Characteristics.None);

    /// <summary>What the hero wears, slot by slot in order.</summary>
    public IEnumerable<KeyValuePair<Slot, string>> WornItems => (Worn ?? new Dictionary<Slot, string>()).OrderBy(w => w.Key);

    /// <summary>The ranks above 1, by spell id in order.</summary>
    public IEnumerable<KeyValuePair<string, int>> RanksGiven =>
        (Ranks ?? new Dictionary<string, int>()).Where(r => r.Value > 1).OrderBy(r => r.Key, StringComparer.Ordinal);

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
            : PointsProblem(data) ?? (Worn is null ? null : Equipment.Problem(Worn, Level, data));
    }

    /// <summary>Characteristic and spell points within what the level gives; ranks of unlocked spells of the class.</summary>
    private string? PointsProblem(GameData data)
    {
        Characteristics st = Stats ?? Characteristics.None;
        int[] values = [st.Vitality, st.Earth, st.Fire, st.Water, st.Air];
        if (values.Any(v => v < 0) || values.Sum() > Progression.CharacteristicPoints(Level))
            return $"At most {Progression.CharacteristicPoints(Level)} characteristic points at level {Level}, none below zero.";
        if (Ranks is null || Ranks.Count == 0)
            return null;
        HeroClass? c = Class is null ? null : data.Class(Class);
        foreach ((string spell, int rank) in Ranks)
        {
            if (c is null || !c.Spells.Contains(spell) || data.Spells[spell].Level > Level)
                return $"The spell '{spell}' is not one this hero has.";
            if (rank < 1 || rank > data.Spells[spell].MaxRank)
                return $"The spell '{spell}' has ranks 1 to {data.Spells[spell].MaxRank}.";
        }
        return Ranks.Values.Sum(Progression.RankCost) > Progression.SpellPoints(Level)
            ? $"At most {Progression.SpellPoints(Level)} spell points at level {Level}."
            : null;
    }
}
