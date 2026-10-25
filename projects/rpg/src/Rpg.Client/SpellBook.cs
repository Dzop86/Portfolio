using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// One spell of the spells screen: unlocked or not (with the level that unlocks it), its rank and what
/// the next costs, and its damage at that rank as the hero would do it, the points of its element
/// counted (2 more for every 10): the normal range, the critical range, and the next rank's range.
/// A spell that does no damage has (0, 0) everywhere. Since sprint 146 (D82, R2), a <see cref="Preview"/> is a spell only
/// the draft's talents give: shown dotted, out of the filter and of the deck until the talents are saved.
/// </summary>
/// <remarks>
/// Since sprint 150 (D82, F5 a, O2 a, O3 a): the talent a spell comes from (<see cref="Origin"/>), the spell it took the
/// place of (<see cref="Replaces"/>), the talents that change it (<see cref="ModifiedBy"/>), and whether it is
/// <see cref="New"/> (a spell of the tree not yet looked at in this game).
/// </remarks>
public sealed record BookSpell(Spell Spell, bool Unlocked, int Rank, int NextCost, (int Min, int Max) Damage, (int Min, int Max) Critical, (int Min, int Max)? NextRank, bool InDeck = false,
    bool Preview = false, string? Origin = null, string? Replaces = null, IReadOnlyList<string>? ModifiedBy = null, bool New = false)
{
    public Element Element => Spell.Element;
}

/// <summary>
/// The spells screen, without any engine (sprint 65): all the spells of the hero's class in the
/// order they unlock, then the compounds' its talents give (sprint 138), from a draft of its points
/// (<see cref="PointsEditor"/>), filtered by element.
/// </summary>
public sealed class SpellBook
{
    public SpellBook(PointsEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        Hero draft = editor.Draft;
        Characteristics stats = Fight.HeroTotals(draft, editor.Data)?.Stats ?? Characteristics.None;
        HeroClass? c = editor.Data.Class(draft.Class);
        GameData data = editor.Data;
        Grimoire grimoire = Talents.GrimoireOf(draft, data);
        IEnumerable<string> twins = grimoire.Spells.Where(id => data.Spells[id].Twin is not null);
        HashSet<string> previewed = [.. editor.Previewed.Select(s => s.Id)];
        // A spell a saved Transmutation or Conversion replaced is gone from the book (Charles, after 1.0.134): its twin says so.
        Grimoire saved = Talents.GrimoireOf(editor.Hero, data);
        HashSet<string> gone = [.. grimoire.Replaced.Keys.Where(saved.Replaced.ContainsKey)];
        TalentSpec[] talents = [.. editor.TalentsTaken.Select(id => data.Talents[id])];
        Spells = c is null ? [] : [.. c.Spells.Where(id => !gone.Contains(id)).Concat(twins).Select(id => data.Spells[id]).Select(s =>
        {
            string? origin = OriginOf(s, talents, data);
            return Entry(s, editor, draft, stats) with
            {
                Preview = previewed.Contains(s.Id),
                Origin = origin,
                Replaces = s.Twin is string parent && grimoire.Replaced.GetValueOrDefault(parent) == s.Id ? parent : null,
                ModifiedBy = ModifiedBy(s, talents),
                New = origin is not null && !previewed.Contains(s.Id) && !Seen.Contains(s.Id),
            };
        })];
    }

    // The spells of the tree looked at in this game (O2 a): "new" until the first hover.
    private static readonly HashSet<string> Seen = new(StringComparer.Ordinal);

    /// <summary>A spell of the tree looked at: no longer new.</summary>
    public static void Look(string spell) => Seen.Add(spell);

    /// <summary>The talent a spell comes from: the Transmutation or the Conversion of a twin, the talent that grants it; null for a class's.</summary>
    public static string? OriginOf(Spell spell, IReadOnlyList<TalentSpec> talents, GameData data)
    {
        ArgumentNullException.ThrowIfNull(spell);
        ArgumentNullException.ThrowIfNull(talents);
        ArgumentNullException.ThrowIfNull(data);
        if (spell.Twin is string parent)
        {
            Element from = data.Spells[parent].Element;
            return talents.FirstOrDefault(t => t.Rules.OfType<ConvertRule>().Any(r => r.Element == spell.Element))?.Id
                ?? talents.FirstOrDefault(t => t.Rules.OfType<TransmuteRule>().Any(r => r.From == from && r.To == spell.Element))?.Id;
        }
        return talents.FirstOrDefault(t => t.Rules.OfType<GrantRule>().Any(r => r.Spell == spell.Id))?.Id;
    }

    /// <summary>The talents that change a spell (O3 a): its cost, range, sight, area, casts a turn, cooldown or damage.</summary>
    public static IReadOnlyList<string> ModifiedBy(Spell spell, IReadOnlyList<TalentSpec> talents)
    {
        ArgumentNullException.ThrowIfNull(spell);
        ArgumentNullException.ThrowIfNull(talents);
        static bool Differs(Spell a, Spell b) => a.ApCost != b.ApCost || a.MinRange != b.MinRange || a.MaxRange != b.MaxRange || a.LineOfSight != b.LineOfSight
            || a.PerTurn != b.PerTurn || a.Cooldown != b.Cooldown || a.DamageMin != b.DamageMin || a.DamageMax != b.DamageMax || a.Area != b.Area;
        return [.. talents.Where(t => Differs(Talents.Compile(spell, [t]), spell)).Select(t => t.Id)];
    }

    private static BookSpell Entry(Spell spell, PointsEditor editor, Hero draft, Characteristics stats)
    {
        int rank = editor.Rank(spell);
        int bonus = Characteristics.Bonus(stats.For(spell.Element, Talents.Converts([.. (draft.Talents ?? []).Where(editor.Data.Talents.ContainsKey).Select(id => editor.Data.Talents[id])], spell.Element)));
        Spell now = spell.AtRank(rank);
        (int, int) Range(Spell s, int extra) => s.DamageMax > 0 ? (s.DamageMin + extra + bonus, s.DamageMax + extra + bonus) : (0, 0);
        int crit = Spell.CritBonus(now.DamageMax);
        return new BookSpell(spell, spell.Level <= draft.Level, rank, editor.NextRankCost(spell),
            Range(now, 0), spell.Crit > 0 ? Range(now, crit) : (0, 0), rank < spell.MaxRank ? Range(spell.AtRank(rank + 1), 0) : null, editor.InDeck(spell));
    }

    public IReadOnlyList<BookSpell> Spells { get; }

    /// <summary>
    /// The elements of the spells in the book, in order: a compound's only while the talents give its spells (Charles,
    /// after sprint 144: no Lightning to filter by before the tree's passive changed the elements); never a preview's
    /// (sprint 146, D82, R1: a compound only once a Transmutation or the Conversion is saved).
    /// </summary>
    public IReadOnlyList<Element> Elements => [.. Spells.Where(s => !s.Preview).Select(s => s.Element).Distinct().Order()];

    /// <summary>The spells of an element, previews left out; all of them without one, previews included.</summary>
    public IReadOnlyList<BookSpell> Of(Element? element) => element is Element e ? [.. Spells.Where(s => s.Element == e && !s.Preview)] : Spells;
}
