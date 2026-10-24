using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>The two families of the talent tree (D80): Matter changes what the hero strikes with, Manner the rules it strikes by.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TalentFamily>))]
public enum TalentFamily
{
    Matter,
    Manner,
}

/// <summary>The tree's seven branches (D80, <c>docs/talents-arbre.md</c> §4.1): two of Matter, five of Manner.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Branch>))]
public enum Branch
{
    Braids,
    Poles,
    Reactions,
    Distance,
    Rhythm,
    Terrain,
    Support,
}

/// <summary>A talent's floor in its branch: the first is open, the second asks 2 points spent below it, the key 3 and costs 2.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Tier>))]
public enum Tier
{
    First = 1,
    Second = 2,
    Key = 3,
}

/// <summary>What a talent changes (sprint 137): each kind is a piece of the engine, read before the fight (compiled) or during it (living).</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ReactionRule), "reaction")]
[JsonDerivedType(typeof(RangeRule), "range")]
[JsonDerivedType(typeof(DistanceRule), "distance")]
public abstract record TalentRule;

/// <summary>The bearer's hit of another base element on a fighter it did not mark itself sets off this reaction (<see cref="ReactionSpec"/>).</summary>
public sealed record ReactionRule(string Reaction) : TalentRule;

/// <summary>
/// Compiled: every spell reaching farther than 1 cell reaches <see cref="Change"/> cells more (or fewer), never under
/// its minimum range nor under 1.
/// </summary>
public sealed record RangeRule(int Change) : TalentRule;

/// <summary>Living: the bearer's spells hit <see cref="Percent"/> % harder (or softer) on a target <see cref="MinCells"/> to <see cref="MaxCells"/> cells away.</summary>
public sealed record DistanceRule(int MinCells, int MaxCells, int Percent) : TalentRule;

/// <summary>
/// A talent of the tree shared by every class (D80), as described in <c>data/talents.json</c>: no talent names a class
/// or a spell. <see cref="Excludes"/> cannot be taken with it; <see cref="Requires"/> must be taken first.
/// </summary>
public sealed record TalentSpec(string Id, LocalizedText Name, LocalizedText Description, Branch Branch, Tier Tier, IReadOnlyList<TalentRule> Rules,
    IReadOnlyList<string>? Excludes = null, IReadOnlyList<string>? Requires = null)
{
    public TalentFamily Family => Branch is Branch.Braids or Branch.Poles ? TalentFamily.Matter : TalentFamily.Manner;

    /// <summary>1 point, 2 for a key.</summary>
    public int Cost => Tier == Tier.Key ? 2 : 1;
}

/// <summary>
/// An elemental reaction (D80, <c>docs/talents-arbre.md</c> §6), as described in <c>data/reactions.json</c>: a hit of
/// <see cref="First"/> on a fighter marked by <see cref="Second"/>, or the other way round, by a hero with the talent,
/// sets <see cref="Statuses"/> on it, named after the reaction.
/// </summary>
public sealed record ReactionSpec(string Id, LocalizedText Name, LocalizedText Description, Element First, Element Second, IReadOnlyList<StatusEffect> Statuses)
{
    public bool Joins(Element a, Element b) => (a == First && b == Second) || (a == Second && b == First);
}

/// <summary>
/// The mark a hero's direct hit of a base element leaves on an enemy (sprint 137): one per fighter, until the end of its
/// <see cref="Source"/>'s next turn (<see cref="TurnsLeft"/> of its turns' ends).
/// </summary>
public sealed record Mark(Element Element, int Source, int TurnsLeft);

/// <summary>The tree's rules (D80): its points by level, what a choice of talents may hold, the bonuses it gives.</summary>
public static class Talents
{
    /// <summary>The tree opens at this level: nothing changes before it (chapter 1 and the balance of the fields).</summary>
    public const int Opens = 30;

    /// <summary>The ceiling of the tree's conditional bonuses on one hit, in percent (§4.4).</summary>
    public const int Ceiling = 35;

    /// <summary>2 points at level 30, then one every 5 levels: 16 at 100.</summary>
    public static int Points(int level) => level < Opens ? 0 : 2 + (level - Opens) / 5;

    /// <summary>Earth, Fire, Water and Air: the elements that leave marks and react.</summary>
    public static bool IsBase(Element element) => element is Element.Earth or Element.Fire or Element.Water or Element.Air;

    /// <summary>
    /// Why these talents cannot be held at this level, or null: known and each once, from level 30, within its points;
    /// a second floor after 2 points in the branch's first, a key after 3 below it; one key per branch and one of Matter
    /// in the whole tree; nothing it excludes, everything it requires.
    /// </summary>
    public static string? Problem(IReadOnlyList<string> talents, int level, GameData data)
    {
        ArgumentNullException.ThrowIfNull(talents);
        ArgumentNullException.ThrowIfNull(data);
        if (talents.Count == 0)
            return null;
        if (talents.FirstOrDefault(id => !data.Talents.ContainsKey(id)) is string unknown)
            return $"Unknown talent '{unknown}'.";
        if (talents.Distinct(StringComparer.Ordinal).Count() != talents.Count)
            return "A talent is taken once.";
        if (level < Opens)
            return $"The talents open at level {Opens}.";
        TalentSpec[] taken = [.. talents.Select(id => data.Talents[id])];
        if (taken.Sum(t => t.Cost) > Points(level))
            return $"At most {Points(level)} talent points at level {level}.";
        foreach (TalentSpec t in taken)
        {
            int below = taken.Where(o => o.Branch == t.Branch && o.Tier < t.Tier).Sum(o => o.Cost);
            if (t.Tier == Tier.Second && below < 2 || t.Tier == Tier.Key && below < 3)
                return $"'{t.Id}' asks {(t.Tier == Tier.Key ? 3 : 2)} points spent below it in its branch.";
            if (taken.FirstOrDefault(o => o != t && ((t.Excludes ?? []).Contains(o.Id) || (o.Excludes ?? []).Contains(t.Id))) is TalentSpec other)
                return $"'{t.Id}' and '{other.Id}' exclude each other.";
            if ((t.Requires ?? []).FirstOrDefault(r => !talents.Contains(r)) is string missing)
                return $"'{t.Id}' asks for '{missing}'.";
        }
        if (taken.Where(t => t.Tier == Tier.Key).GroupBy(t => t.Branch).Any(g => g.Count() > 1))
            return "One key per branch.";
        return taken.Count(t => t.Tier == Tier.Key && t.Family == TalentFamily.Matter) > 1 ? "One key of Matter in the whole tree." : null;
    }

    /// <summary>A spell as the talents compile it before the fight: its range changed (<see cref="RangeRule"/>).</summary>
    public static Spell Compile(Spell spell, IReadOnlyList<TalentSpec> talents)
    {
        ArgumentNullException.ThrowIfNull(spell);
        ArgumentNullException.ThrowIfNull(talents);
        int change = talents.SelectMany(t => t.Rules).OfType<RangeRule>().Sum(r => r.Change);
        if (change == 0 || spell.MaxRange <= 1)
            return spell;
        return spell with { MaxRange = Math.Max(Math.Max(1, spell.MinRange), spell.MaxRange + change) };
    }

    /// <summary>
    /// The living talents' bonus on a hit from <paramref name="from"/> on a fighter standing on <paramref name="at"/>, in
    /// percent: at most <see cref="Ceiling"/>, at least -100.
    /// </summary>
    public static int Percent(IReadOnlyList<TalentSpec> talents, Cell from, Cell at)
    {
        ArgumentNullException.ThrowIfNull(talents);
        int distance = from.DistanceTo(at);
        int sum = talents.SelectMany(t => t.Rules).OfType<DistanceRule>().Where(r => distance >= r.MinCells && distance <= r.MaxCells).Sum(r => r.Percent);
        return Math.Clamp(sum, -100, Ceiling);
    }

    /// <summary>The reaction these talents set off between two elements, or null.</summary>
    public static ReactionSpec? ReactionOf(IReadOnlyList<TalentSpec> talents, Element a, Element b, GameData data)
    {
        ArgumentNullException.ThrowIfNull(talents);
        ArgumentNullException.ThrowIfNull(data);
        if (a == b || !IsBase(a) || !IsBase(b))
            return null;
        return talents.SelectMany(t => t.Rules).OfType<ReactionRule>().Select(r => data.Reactions[r.Reaction]).FirstOrDefault(r => r.Joins(a, b));
    }
}
