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
[JsonDerivedType(typeof(OpenRule), "open")]
[JsonDerivedType(typeof(FullGrimoireRule), "full-grimoire")]
[JsonDerivedType(typeof(TransmuteRule), "transmute")]
[JsonDerivedType(typeof(ConvertRule), "convert")]
[JsonDerivedType(typeof(SignatureRule), "signature")]
[JsonDerivedType(typeof(ElementRule), "element")]
[JsonDerivedType(typeof(CompoundRule), "compound")]
[JsonDerivedType(typeof(PrismRule), "prism")]
public abstract record TalentRule;

/// <summary>
/// Compiled (sprint 138): the compound joins the grimoire, its spells transposed from the first two class spells of each
/// parent (all of them with <see cref="FullGrimoireRule"/>).
/// </summary>
public sealed record OpenRule(Element Element) : TalentRule;

/// <summary>Compiled: every spell of the compounds opened, not only the first four.</summary>
public sealed record FullGrimoireRule : TalentRule;

/// <summary>Compiled: the spells of the base element <see cref="From"/> become their twins of the compound <see cref="To"/>.</summary>
public sealed record TransmuteRule(Element From, Element To) : TalentRule;

/// <summary>Compiled: the spells of both parents become their twins of the compound (a key of Matter).</summary>
public sealed record ConvertRule(Element Element) : TalentRule;

/// <summary>Living: the compounds' signatures (the Arc) <see cref="Percent"/> % stronger.</summary>
public sealed record SignatureRule(int Percent) : TalentRule;

/// <summary>Living: the spells of an element hit <see cref="Percent"/> % harder, the other elements' (not the neutral) <see cref="Others"/> %.</summary>
public sealed record ElementRule(Element Element, int Percent, int Others = 0) : TalentRule;

/// <summary>Living: the compounds' spells hit <see cref="Percent"/> % harder (or softer).</summary>
public sealed record CompoundRule(int Percent) : TalentRule;

/// <summary>Living: a pair's hit leaves the mark of its first parent; it still sets off nothing.</summary>
public sealed record PrismRule : TalentRule;

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

/// <summary>A hero's grimoire (sprint 138): the spells it may fight with, and the class spells its talents changed into twins.</summary>
public sealed record Grimoire(IReadOnlyList<string> Spells, IReadOnlyDictionary<string, string> Replaced);

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
            int asked = t.Tier == Tier.Key ? 3 : t.Tier == Tier.Second ? SecondFloor(t.Branch) : 0;
            if (below < asked)
                return $"'{t.Id}' asks {asked} points spent below it in its branch.";
            if (taken.FirstOrDefault(o => o != t && ((t.Excludes ?? []).Contains(o.Id) || (o.Excludes ?? []).Contains(t.Id))) is TalentSpec other)
                return $"'{t.Id}' and '{other.Id}' exclude each other.";
            if ((t.Requires ?? []).FirstOrDefault(r => !talents.Contains(r)) is string missing)
                return $"'{t.Id}' asks for '{missing}'.";
        }
        if (taken.Where(t => t.Tier == Tier.Key).GroupBy(t => t.Branch).Any(g => g.Count() > 1))
            return "One key per branch.";
        return taken.Count(t => t.Tier == Tier.Key && t.Family == TalentFamily.Matter) > 1 ? "One key of Matter in the whole tree." : null;
    }

    /// <summary>
    /// The points below a second floor: 2, but 1 in Braids, whose first floor opens elements, two at most; the game
    /// designer's builds (§4.5) take a second floor of Braids after one opening (sprint 138).
    /// </summary>
    public static int SecondFloor(Branch branch) => branch == Branch.Braids ? 1 : 2;

    /// <summary>
    /// The spells a hero may put in its deck (sprint 138): its class's that its level has unlocked, in the class's order,
    /// those of a transmuted or converted element replaced by their twins; then the twins of the compounds it opened.
    /// <see cref="Grimoire.Replaced"/> says what became what, for a deck chosen before.
    /// </summary>
    public static Grimoire GrimoireOf(Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        if (data.Class(hero.Class) is not HeroClass c)
            return new Grimoire([], new Dictionary<string, string>());
        TalentRule[] rules = [.. (hero.Talents ?? []).Where(data.Talents.ContainsKey).SelectMany(id => data.Talents[id].Rules)];
        var into = new Dictionary<Element, Element>();
        foreach (TransmuteRule t in rules.OfType<TransmuteRule>())
            into[t.From] = t.To;
        foreach (ConvertRule cv in rules.OfType<ConvertRule>())
        {
            foreach (Element parent in Elements.Parents(cv.Element))
                into[parent] = cv.Element;
        }
        Spell? TwinOf(string origin, Element compound) => data.Spells.Values.FirstOrDefault(s => s.Twin == origin && s.Element == compound);
        var spells = new List<string>();
        var replaced = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] unlocked = [.. c.Spells.Where(id => data.Spells[id].Level <= hero.Level)];
        foreach (string id in unlocked)
        {
            if (into.TryGetValue(data.Spells[id].Element, out Element compound) && TwinOf(id, compound) is Spell twin)
            {
                replaced[id] = twin.Id;
                spells.Add(twin.Id);
            }
            else
            {
                spells.Add(id);
            }
        }
        bool full = rules.OfType<FullGrimoireRule>().Any();
        foreach (Element compound in rules.OfType<OpenRule>().Select(o => o.Element).Concat(rules.OfType<ConvertRule>().Select(r => r.Element)).Distinct().Order())
        {
            foreach (Element parent in Elements.Parents(compound))
            {
                IEnumerable<string> origins = c.Spells.Where(id => data.Spells[id].Element == parent);
                foreach (string origin in (full ? origins : origins.Take(2)).Where(unlocked.Contains))
                {
                    if (TwinOf(origin, compound) is Spell twin && !spells.Contains(twin.Id))
                        spells.Add(twin.Id);
                }
            }
        }
        return new Grimoire(spells, replaced);
    }

    /// <summary>The compounds' signatures' bonus, in percent: the strongest, they do not add up (Pressed Signature and Conversion, §4.5: "Arc at 50 %").</summary>
    public static int SignatureBonus(IReadOnlyList<TalentSpec> talents) => talents.SelectMany(t => t.Rules).OfType<SignatureRule>().Select(r => r.Percent).DefaultIfEmpty(0).Max();

    /// <summary>A pair's hit leaves its first parent's mark (<see cref="PrismRule"/>).</summary>
    public static bool HasPrism(IReadOnlyList<TalentSpec> talents) => talents.SelectMany(t => t.Rules).OfType<PrismRule>().Any();

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
    /// The living talents' bonus on a hit in <paramref name="element"/> from <paramref name="from"/> on a fighter standing
    /// on <paramref name="at"/>, in percent: at most <see cref="Ceiling"/>, at least -100.
    /// </summary>
    public static int Percent(IReadOnlyList<TalentSpec> talents, Cell from, Cell at, Element element = Element.Neutral)
    {
        ArgumentNullException.ThrowIfNull(talents);
        int distance = from.DistanceTo(at);
        TalentRule[] rules = [.. talents.SelectMany(t => t.Rules)];
        int sum = rules.OfType<DistanceRule>().Where(r => distance >= r.MinCells && distance <= r.MaxCells).Sum(r => r.Percent)
            + rules.OfType<ElementRule>().Sum(r => r.Element == element ? r.Percent : element == Element.Neutral ? 0 : r.Others)
            + (Elements.IsCompound(element) ? rules.OfType<CompoundRule>().Sum(r => r.Percent) : 0);
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
