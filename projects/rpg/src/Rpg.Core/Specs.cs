namespace Rpg.Core;

/// <summary>
/// One evolution of a path (sprint 186, D93, docs/elements-specialisations.md §v3.4), at level 30, 50 or 100: the title it
/// gives, what it does, the passives it adds (data/passives.json) or takes the place of (<see cref="Replaces"/>), the rules
/// it compiles into the hero's spells, as a talent's, and the spells it gives.
/// </summary>
public sealed record SpecTier(int Level, LocalizedText Title, LocalizedText Description, IReadOnlyList<string>? Passives = null, IReadOnlyList<string>? Replaces = null,
    IReadOnlyList<TalentRule>? Rules = null, IReadOnlyList<string>? Spells = null);

/// <summary>
/// A path of a class (sprint 186, D93): one of its three branches, with its evolutions in the order of <see cref="Specs.TierLevels"/>;
/// <see cref="Keeps"/>, class spells the automatic deck always keeps with it (the Ambush's Hawk).
/// </summary>
public sealed record Specialisation(string Id, string Class, LocalizedText Name, IReadOnlyList<SpecTier> Tiers, LocalizedText? Lore = null,
    IReadOnlyList<string>? Keeps = null, string? Icon = null)
{
    /// <summary>The evolutions a level has reached, the first first.</summary>
    public IEnumerable<SpecTier> ReachedAt(int level) => Tiers.Where(t => t.Level <= level);

    /// <summary>All its spells, evolution by evolution, whatever the level.</summary>
    public IEnumerable<string> AllSpells => Tiers.SelectMany(t => t.Spells ?? []);
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

    /// <summary>What taking this path costs: the first is free at any level (Q9 b), the next 30 × the level in Éclats (§v3.4).</summary>
    public static long ChangeCost(int level, bool hadOne) => hadOne ? 30L * level : 0;

    /// <summary>The hero's path, if this data knows it, of its class, and its level opens it; null for a hero who has not chosen.</summary>
    public static Specialisation? Of(Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        return hero.Spec is string id && hero.Level >= Opens && data.Specs.TryGetValue(id, out Specialisation? s) && s.Class == hero.Class ? s : null;
    }

    /// <summary>The evolutions the hero has: none without a path, all those of its level at once when it chooses late (§v3.4).</summary>
    public static IReadOnlyList<SpecTier> TiersOf(Hero hero, GameData data) => Of(hero, data)?.ReachedAt(hero.Level).ToList() ?? [];

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

    /// <summary>An adaptive spell given its element, a compound's damage times 0.8 as a twin's; any other spell as it is.</summary>
    public static Spell Adapted(Spell spell, Element element)
    {
        ArgumentNullException.ThrowIfNull(spell);
        if (!spell.Adaptive || element == Element.Neutral)
            return spell;
        Spell s = spell with { Element = element };
        return Elements.IsCompound(element) && s.DamageMax > 0
            ? s with { DamageMin = Math.Max(1, (int)Math.Round(s.DamageMin * 0.8)), DamageMax = Math.Max(1, (int)Math.Round(s.DamageMax * 0.8)) }
            : s;
    }

    /// <summary>
    /// The hero on another path, its ranks and deck following (§v3.4): the old path's spells give their place, one for one in
    /// the order of the evolutions, to the new path's that its level has; what has no heir leaves the deck and gives its
    /// points back. Its class's spells do not move.
    /// </summary>
    public static Hero Transferred(Hero hero, string to, GameData data)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        Hero moved = hero with { Spec = to };
        if (Of(hero, data) is not Specialisation from)
            return moved;
        string[] old = [.. from.AllSpells];
        string[] heirs = Of(moved, data)?.AllSpells.ToArray() ?? [];
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
