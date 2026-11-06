namespace Rpg.Core;

/// <summary>
/// One evolution of a path (sprint 186, D93, docs/elements-specialisations.md §v3.4), at level 30, 50 or 100: the title it
/// gives, what it does, the passives it adds (data/passives.json) or takes the place of (<see cref="Replaces"/>), the rules
/// it compiles into the hero's spells, as a talent's, and the spells it gives.
/// </summary>
public sealed record SpecTier(int Level, LocalizedText Title, LocalizedText Description, IReadOnlyList<string>? Passives = null, IReadOnlyList<string>? Replaces = null,
    IReadOnlyList<TalentRule>? Rules = null, IReadOnlyList<string>? Spells = null);

/// <summary>
/// A lineage (sprint 190, D95, docs/elements-specialisations.md §v4): one of the two ways a branch divides into at level 50, its
/// evolutions at 50 and 100.
/// </summary>
public sealed record SpecFork(string Id, IReadOnlyList<SpecTier> Tiers, LocalizedText? Lore = null, string? Icon = null, IReadOnlyList<SpecFinal>? Finals = null)
{
    /// <summary>The final class of this id, or null.</summary>
    public SpecFinal? FinalOf(string? id) => Finals?.FirstOrDefault(f => f.Id == id);
}

/// <summary>
/// A final class (sprint 194, D96, docs/classes-conception.md §2.2): one of the two a lineage ends in at level 100, a signature without
/// a spell. A lineage without <see cref="SpecFork.Finals"/> keeps its one evolution at 100, until its class is converted (S197 on).
/// </summary>
public sealed record SpecFinal(string Id, FinalTemplate Template, SpecTier Tier, LocalizedText? Lore = null, string? Icon = null);

/// <summary>
/// What kind of signature a final class is (sprint 195, D96, docs/classes-conception.md §2.3): two sisters of two kinds, so that
/// the choice is a decision and never "+15 % or +20 %".
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<FinalTemplate>))]
public enum FinalTemplate
{
    /// <summary>A spell reaches further.</summary>
    Extends,

    /// <summary>A spell leaves something on the ground.</summary>
    Trace,

    /// <summary>A spell comes back sooner.</summary>
    Faster,

    /// <summary>A second creature, a second trap.</summary>
    Second,

    /// <summary>Once a fight, it saves someone.</summary>
    Saves,

    /// <summary>An effect under a threshold of hit points.</summary>
    LowHp,

    /// <summary>Finishing an enemy pays.</summary>
    Kill,

    /// <summary>The first turn of the fight.</summary>
    FirstTurn,

    /// <summary>A bonus for standing still, or for each cell walked.</summary>
    StayOrMove,

    /// <summary>More against a target in a given state.</summary>
    TargetState,

    /// <summary>What dies pays.</summary>
    Death,

    /// <summary>A lasting transformation.</summary>
    Form,
}

/// <summary>
/// A branch of a class's tree (sprint 186, D93; sprint 190, D95): its evolution at 30 and its two lineages;
/// <see cref="Keeps"/>, class spells the automatic deck always keeps with it (the Ambush's Hawk).
/// </summary>
public sealed record Specialisation(string Id, string Class, LocalizedText Name, SpecTier Base, IReadOnlyList<SpecFork> Forks, LocalizedText? Lore = null,
    IReadOnlyList<string>? Keeps = null, string? Icon = null)
{
    /// <summary>The lineage of this id, or null.</summary>
    public SpecFork? ForkOf(string? id) => Forks.FirstOrDefault(f => f.Id == id);

    /// <summary>The spells of this leaf, evolution by evolution, whatever the level: the branch's, then the lineage's.</summary>
    public IEnumerable<string> SpellsWith(string? fork) => (Base.Spells ?? []).Concat(ForkOf(fork)?.Tiers.SelectMany(t => t.Spells ?? []) ?? []);
}

/// <summary>The rules of the paths (sprint 186, D93, docs/elements-specialisations.md §v3).</summary>
public static class Specs
{
    /// <summary>The level a path opens at.</summary>
    public const int Opens = 30;

    /// <summary>The levels of the three evolutions: they come alone with the level (§v3.4).</summary>
    public static readonly IReadOnlyList<int> TierLevels = [30, 50, 100];

    /// <summary>The quest that opens the choice (§v3.5).</summary>
    public const string Quest = "a-road-of-ones-own";

    /// <summary>The level a branch divides at (D95).</summary>
    public const int Forks = 50;

    /// <summary>The level a lineage ends in its final class (D96).</summary>
    public const int Finals = 100;

    /// <summary>
    /// What this hero pays to take this branch and lineage (§v4.5): the first branch and the first lineage are free at any level;
    /// another final class 5 × the level (D96), another lineage 10 × the level in Éclats, another branch 30 × the level.
    /// </summary>
    public static long ChangeCost(Hero hero, string spec, string? fork, string? final = null)
    {
        ArgumentNullException.ThrowIfNull(hero);
        return hero.Spec is null ? 0
            : hero.Spec != spec ? 30L * hero.Level
            : hero.Fork is not null && hero.Fork != fork ? 10L * hero.Level
            : hero.Final is null || final is null || hero.Final == final ? 0
            : 5L * hero.Level;
    }

    /// <summary>The hero's lineage, if it has one its branch knows and its level opens.</summary>
    public static SpecFork? ForkOf(Hero hero, GameData data) => hero?.Level >= Forks ? Of(hero, data)?.ForkOf(hero.Fork) : null;

    /// <summary>The hero's final class, if it has one its lineage knows and its level opens.</summary>
    public static SpecFinal? FinalOf(Hero hero, GameData data) => hero?.Level >= Finals ? ForkOf(hero, data)?.FinalOf(hero.Final) : null;

    /// <summary>The hero's path, if this data knows it, of its class, and its level opens it; null for a hero who has not chosen.</summary>
    public static Specialisation? Of(Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        return hero.Spec is string id && hero.Level >= Opens && data.Specs.TryGetValue(id, out Specialisation? s) && s.Class == hero.Class ? s : null;
    }

    /// <summary>The evolutions the hero has: none without a branch, the branch's without a lineage, all those of its level at once when it chooses late (§v4.5).</summary>
    public static IReadOnlyList<SpecTier> TiersOf(Hero hero, GameData data)
    {
        if (Of(hero, data) is not Specialisation branch)
            return [];
        return [branch.Base, .. ForkOf(hero, data)?.Tiers.Where(t => t.Level <= hero.Level) ?? [], .. FinalOf(hero, data) is SpecFinal final ? [final.Tier] : Array.Empty<SpecTier>()];
    }

    /// <summary>The spells its evolutions give it.</summary>
    public static IEnumerable<string> SpellsOf(Hero hero, GameData data) => TiersOf(hero, data).SelectMany(t => t.Spells ?? []);

    /// <summary>The rules its evolutions compile into its spells.</summary>
    public static IReadOnlyList<TalentRule> RulesOf(Hero hero, GameData data) => [.. TiersOf(hero, data).SelectMany(t => t.Rules ?? [])];

    /// <summary>The passives its evolutions give it, without those a later one took the place of.</summary>
    public static IReadOnlyList<string> PassivesOf(Hero hero, GameData data)
    {
        IReadOnlyList<SpecTier> tiers = TiersOf(hero, data);
        HashSet<string> replaced = [.. tiers.SelectMany(t => t.Replaces ?? [])];
        return [.. tiers.SelectMany(t => t.Passives ?? []).Where(p => !replaced.Contains(p)).Distinct(StringComparer.Ordinal)];
    }

    /// <summary>The title it bears: its last evolution's; null without a path.</summary>
    public static LocalizedText? TitleOf(Hero hero, GameData data)
    {
        IReadOnlyList<SpecTier> tiers = TiersOf(hero, data);
        return tiers.Count == 0 ? null : tiers[^1].Title;
    }

    /// <summary>A path's rules as a talent of the fight (sprint 190, D95, brick B): the compiled ones and the living ones, read like a talent's.</summary>
    public static TalentSpec AsTalent(IReadOnlyList<TalentRule> rules) => new("path", new LocalizedText("", ""), new LocalizedText("", ""), default, default, rules);

    /// <summary>The highest tier a gain of levels has just passed, or null: what the game announces (§v3.5).</summary>
    public static int? TierPassed(int before, int after) => TierLevels.Where(l => before < l && l <= after).Cast<int?>().LastOrDefault();

    /// <summary>The spells the automatic deck keeps for a path: its own, then its <see cref="Specialisation.Keeps"/>, each once, those of the grimoire.</summary>
    public static IReadOnlyList<string> Reserved(Hero hero, GameData data, IReadOnlyList<string> grimoire)
    {
        ArgumentNullException.ThrowIfNull(grimoire);
        return [.. SpellsOf(hero, data).Concat(Of(hero, data)?.Keeps ?? []).Distinct(StringComparer.Ordinal).Where(grimoire.Contains)];
    }

    /// <summary>
    /// The element a hero's adaptive spells strike with: among the elements of the spells that hurt of its grimoire, adaptive
    /// ones left out, the one it is strongest in, what it wears counted, past the threshold too; on a tie, the enum's order.
    /// Neutral without one.
    /// </summary>
    public static Element AdaptiveElement(Characteristics stats, IEnumerable<Spell> grimoire, int threshold)
    {
        ArgumentNullException.ThrowIfNull(stats);
        ArgumentNullException.ThrowIfNull(grimoire);
        Element[] elements = [.. grimoire.Where(s => s.DamageMax > 0 && !s.Adaptive && s.Element != Element.Neutral).Select(s => s.Element).Distinct().Order()];
        return elements.Length == 0 ? Element.Neutral
            : elements.OrderByDescending(e => Characteristics.Effective(stats.For(e), threshold)).ThenBy(e => e).First();
    }

    /// <summary>
    /// An adaptive spell given its element, a compound's damage times 0.8 as a twin's, the damage of its glyph too (sprint 191, Jaw Trap); any other
    /// spell as it is.
    /// </summary>
    public static Spell Adapted(Spell spell, Element element)
    {
        ArgumentNullException.ThrowIfNull(spell);
        if (!spell.Adaptive || element == Element.Neutral)
            return spell;
        bool compound = Elements.IsCompound(element);
        int Scaled(int damage) => compound ? Math.Max(1, (int)Math.Round(damage * 0.8)) : damage;
        Spell s = spell with
        {
            Element = element,
            Effects = spell.Effects?.Select(e => e is GlyphEffect { DamageMax: > 0 } g ? g with { Element = element, DamageMin = Scaled(g.DamageMin), DamageMax = Scaled(g.DamageMax) } : e).ToList(),
        };
        return s.DamageMax > 0 ? s with { DamageMin = Scaled(s.DamageMin), DamageMax = Scaled(s.DamageMax) } : s;
    }

    /// <summary>
    /// The hero on another path, its ranks and deck following (§v3.4): the old path's spells give their place, one for one in
    /// the order of the evolutions, to the new path's that its level has; what has no heir leaves the deck and gives its
    /// points back. Its class's spells do not move. A final class names no spell (D96): <paramref name="final"/> replaces the
    /// hero's; left out, the same lineage keeps its own, another lineage has none, as the server writes it (sprint 196).
    /// </summary>
    public static Hero Transferred(Hero hero, string to, string? fork, GameData data, string? final = null)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        Hero moved = hero with { Spec = to, Fork = fork, Final = final ?? (to == hero.Spec && fork == hero.Fork ? hero.Final : null) };
        if (Of(hero, data) is not Specialisation from)
            return moved;
        string[] old = [.. from.SpellsWith(hero.Fork)];
        string[] heirs = Of(moved, data)?.SpellsWith(fork).ToArray() ?? [];
        string? HeirOf(string spell)
        {
            int i = Array.IndexOf(old, spell);
            return i >= 0 && i < heirs.Length && data.Spells[heirs[i]].Level <= hero.Level ? heirs[i] : null;
        }
        Dictionary<string, int>? ranks = hero.Ranks?.Select(r => (Id: old.Contains(r.Key) ? HeirOf(r.Key) : r.Key, r.Value))
            .Where(r => r.Id is not null)
            .ToDictionary(r => r.Id!, r => Math.Min(r.Value, data.Spells[r.Id!].MaxRank), StringComparer.Ordinal);
        string[]? deck = hero.Deck?.Select(id => old.Contains(id) ? HeirOf(id) : id).OfType<string>().ToArray();
        return moved with { Ranks = ranks, Deck = deck };
    }
}
