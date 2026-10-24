using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rpg.Core;

/// <summary>
/// The player's character as the rules see it: it takes the place of the scenario's first fighter of
/// team A. Its class (<c>data/classes.json</c>) gives its characteristics and spells; without one, it
/// keeps the scenario's. Its name and appearance (look, outfit colour, hair colour, skin tone,
/// height, build) only change what is shown. With its level come points (<see cref="Progression"/>):
/// the characteristics it was given (<see cref="Stats"/>) and the ranks of its spells
/// (<see cref="Ranks"/>, rank 1 when not listed); what it wears (<see cref="Worn"/>, sprint 59); and the
/// spells it fights with (<see cref="Deck"/>, sprint 68); from level 30, the talents of the tree shared by every
/// class (<see cref="Talents"/>, sprint 137, D80).
/// </summary>
public sealed partial record Hero(string Name, string Look, string? Class = null, int Colour = 0, int Hair = 0, int Skin = 0, int Height = 0, int Build = 0, int Level = 1,
    Characteristics? Stats = null, IReadOnlyDictionary<string, int>? Ranks = null, IReadOnlyDictionary<Slot, string>? Worn = null, IReadOnlyList<string>? Deck = null,
    IReadOnlyList<string>? Talents = null)
{
    /// <summary>The spells a hero fights with: twelve, in two rows of six (D62).</summary>
    public const int DeckSize = 12;

    /// <summary>
    /// The spells this hero fights with, in the order of its bar: its deck when it has chosen one, of 0 to
    /// <see cref="DeckSize"/> spells (D66: an empty deck is a choice too); without a choice (null), its
    /// class's first spells unlocked, up to <see cref="DeckSize"/>. Since sprint 138, the spells its talents
    /// turned into twins of a compound are those twins (<see cref="Talents.GrimoireOf"/>).
    /// </summary>
    public IReadOnlyList<string> DeckOf(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Grimoire grimoire = Rpg.Core.Talents.GrimoireOf(this, data);
        if (Deck is null)
            return [.. grimoire.Spells.Take(DeckLimit(data))];
        return [.. Deck.Select(id => grimoire.Replaced.GetValueOrDefault(id, id))];
    }

    /// <summary>The spells this hero's deck holds at most: <see cref="DeckSize"/>, fewer with some talents (sprint 141).</summary>
    public int DeckLimit(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Rpg.Core.Talents.DeckSize((Talents ?? []).Where(data.Talents.ContainsKey).Select(id => data.Talents[id]));
    }

    /// <summary>The same hero: the ranks compare by content, not by dictionary.</summary>
    public bool Equals(Hero? other) =>
        other is not null && (Name, Look, Class, Colour, Hair, Skin, Height, Build, Level, Stats ?? Characteristics.None) == (other.Name, other.Look, other.Class, other.Colour, other.Hair, other.Skin, other.Height, other.Build, other.Level, other.Stats ?? Characteristics.None)
        && RanksGiven.SequenceEqual(other.RanksGiven) && WornItems.SequenceEqual(other.WornItems)
        && (Deck is null) == (other.Deck is null) && (Deck ?? []).SequenceEqual(other.Deck ?? [])
        && TalentsTaken.SequenceEqual(other.TalentsTaken);

    /// <summary>The talents taken, by id in order: the order they were taken in does not matter.</summary>
    [JsonIgnore]
    public IEnumerable<string> TalentsTaken => (Talents ?? []).Order(StringComparer.Ordinal);

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

    /// <summary>A new character's name (Charles, sprint 93): a capital, then letters only, 3 to 20 in all: "Élise", "Ondine".</summary>
    [GeneratedRegex(@"^\p{Lu}\p{L}{2,19}$")]
    private static partial Regex NamePattern();

    /// <summary>The rule before sprint 93: letters with single hyphens or apostrophes inside ("plop", "Jean-Luc").</summary>
    [GeneratedRegex(@"^\p{L}+(?:['-]\p{L}+)*$")]
    private static partial Regex OldNamePattern();

    /// <summary>Whether a new character may take this name.</summary>
    public static bool IsValidName(string? name) => name is { Length: >= 3 and <= 20 } && NamePattern().IsMatch(name);

    /// <summary>Whether a character may fight under this name: the characters named before sprint 93 keep theirs.</summary>
    public static bool IsKnownName(string? name) => IsValidName(name) || name is { Length: >= 3 and <= 20 } && OldNamePattern().IsMatch(name);

    /// <summary>Why this hero cannot fight with these data, or null.</summary>
    public string? Problem(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return !IsKnownName(Name) ? "A name has 3 to 20 letters, the first a capital."
            : !Looks.Contains(Look) ? $"Unknown look '{Look}'."
            : Colour is < 0 or >= Colours ? $"The colour is 0 to {Colours - 1}."
            : Hair is < 0 or >= HairColours ? $"The hair colour is 0 to {HairColours - 1}."
            : Skin is < 0 or >= SkinTones ? $"The skin tone is 0 to {SkinTones - 1}."
            : Math.Abs(Height) > Shape || Math.Abs(Build) > Shape ? $"Height and build are -{Shape} to {Shape}."
            : Level is < 1 or > MaxLevel ? $"The level is 1 to {MaxLevel}."
            : Class is not null && data.Class(Class) is null ? $"Unknown class '{Class}'."
            : PointsProblem(data) ?? DeckProblem(data) ?? (Worn is null ? null : Equipment.Problem(Worn, Level, data))
                ?? (Talents is null ? null : Rpg.Core.Talents.Problem(Talents, Level, data));
    }

    /// <summary>At most twelve spells, each once, each of its grimoire: a spell of the class the level has unlocked, or a compound's its talents give (sprint 138).</summary>
    private string? DeckProblem(GameData data)
    {
        if (Deck is null)
            return null;
        Grimoire grimoire = Rpg.Core.Talents.GrimoireOf(this, data);
        IReadOnlyList<string> deck = DeckOf(data);
        if (deck.Count > DeckLimit(data) || deck.Distinct(StringComparer.Ordinal).Count() != deck.Count)
            return $"A deck holds at most {DeckLimit(data)} spells, each once.";
        int stranger = deck.ToList().FindIndex(id => !grimoire.Spells.Contains(id));
        return stranger < 0 ? null : $"The spell '{Deck[stranger]}' is not one this hero has.";
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
