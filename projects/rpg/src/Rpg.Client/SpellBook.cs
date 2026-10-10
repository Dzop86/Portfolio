using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// One spell of the spells screen: unlocked or not (with the level that unlocks it), its rank and what
/// the next costs, and its damage at that rank as the hero would do it, the points of its element
/// counted (2 more for every 10): the normal range, the critical range, and the next rank's range.
/// A spell that does no damage has (0, 0) everywhere.
/// </summary>
public sealed record BookSpell(Spell Spell, bool Unlocked, int Rank, int NextCost, (int Min, int Max) Damage, (int Min, int Max) Critical, (int Min, int Max)? NextRank, bool InDeck = false)
{
    public Element Element => Spell.Element;
}

/// <summary>
/// The spells screen, without any engine (sprint 65): all the spells of the hero's class in the
/// order they unlock, from a draft of its points (<see cref="PointsEditor"/>), filtered by element.
/// </summary>
public sealed class SpellBook
{
    public SpellBook(PointsEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        Hero draft = editor.Draft;
        Characteristics stats = Fight.HeroTotals(draft, editor.Data)?.Stats ?? Characteristics.None;
        HeroClass? c = editor.Data.Class(draft.Class);
        Spells = c is null ? [] : [.. c.Spells.Select(id => editor.Data.Spells[id]).Select(s => Entry(s, editor, draft, stats))];
    }

    private static BookSpell Entry(Spell spell, PointsEditor editor, Hero draft, Characteristics stats)
    {
        int rank = editor.Rank(spell);
        int bonus = Characteristics.Bonus(stats.For(spell.Element));
        Spell now = spell.AtRank(rank);
        (int, int) Range(Spell s, int extra) => s.DamageMax > 0 ? (s.DamageMin + extra + bonus, s.DamageMax + extra + bonus) : (0, 0);
        int crit = Spell.CritBonus(now.DamageMax);
        return new BookSpell(spell, spell.Level <= draft.Level, rank, editor.NextRankCost(spell),
            Range(now, 0), spell.Crit > 0 ? Range(now, crit) : (0, 0), rank < spell.MaxRank ? Range(spell.AtRank(rank + 1), 0) : null, editor.InDeck(spell));
    }

    public IReadOnlyList<BookSpell> Spells { get; }

    /// <summary>The spells of an element; all of them without one.</summary>
    public IReadOnlyList<BookSpell> Of(Element? element) => element is Element e ? [.. Spells.Where(s => s.Element == e)] : Spells;
}
