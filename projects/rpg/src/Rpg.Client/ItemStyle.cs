using System.Text;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>The drawing of an item's icon: what its shape shows.</summary>
public enum Glyph
{
    Ring,
    Amulet,
    Belt,
    Cape,
    Hat,
    Chest,
    Shoulders,
    Sword,
    Axe,
    Shield,
    Pet,
    Mount,
    Boots,
    Potion,
    Leaf,
    Scroll,
}

/// <summary>
/// How an item looks in the inventory, without any engine (sprint 65): the shape of its icon (its slot,
/// or its kind) and its colour (the element of the characteristic it gives most, the heal colour for
/// Vitality, grey for nothing), drawn by the client; and the search through item names.
/// </summary>
/// <summary>Where an item comes from (sprint 98): common, a craftsman's, a set's piece, a chief's.</summary>
public enum Provenance
{
    Common,
    Craft,
    Set,
    Chief,
}

public static class ItemStyle
{
    public static Glyph GlyphOf(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Slot switch
        {
            ItemSlot.Ring => Glyph.Ring,
            ItemSlot.Amulet => Glyph.Amulet,
            ItemSlot.Belt => Glyph.Belt,
            ItemSlot.Cape => Glyph.Cape,
            ItemSlot.Hat => Glyph.Hat,
            ItemSlot.Chest => Glyph.Chest,
            ItemSlot.Shoulders => Glyph.Shoulders,
            ItemSlot.OneHanded => Glyph.Sword,
            ItemSlot.TwoHanded => Glyph.Axe,
            ItemSlot.Shield => Glyph.Shield,
            ItemSlot.Pet => Glyph.Pet,
            ItemSlot.Mount => Glyph.Mount,
            ItemSlot.Boots => Glyph.Boots,
            _ => item.Kind switch
            {
                ItemKind.Consumable => Glyph.Potion,
                ItemKind.Quest => Glyph.Scroll,
                _ => Glyph.Leaf,
            },
        };
    }

    /// <summary>The colour of an item: its strongest characteristic's element (ties in the sheet's order).</summary>
    public static string Colour(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Characteristics st = item.Stats ?? Characteristics.None;
        (int Value, string Colour)[] all =
        [
            (st.Vitality, ElementStyle.Heal),
            (st.Earth, ElementStyle.Colour(Element.Earth)),
            (st.Fire, ElementStyle.Colour(Element.Fire)),
            (st.Water, ElementStyle.Colour(Element.Water)),
            (st.Air, ElementStyle.Colour(Element.Air)),
        ];
        (int best, string colour) = all.Aggregate((a, b) => b.Value > a.Value ? b : a);
        return best > 0 ? colour : item.Kind == ItemKind.Quest ? "#f0c060" : "#bdbdbd";
    }

    /// <summary>
    /// Where an item comes from (sprint 98, D72), told by the frame of its icon: a chief's piece (left by a
    /// chief alone), a set's piece (the monsters' loot), a craftsman's work (a trade's recipe), or common.
    /// </summary>
    public static Provenance ProvenanceOf(Item item, GameData data)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(data);
        MonsterSpec[] droppers = [.. data.Families.Values.SelectMany(f => f.Monsters).Where(m => (m.Loot ?? []).Any(l => l.Item == item.Id))];
        if (item.Kind == ItemKind.Equipment && droppers.Length > 0 && droppers.All(m => m.Role == MonsterRole.Boss))
            return Provenance.Chief;
        if (item.Set is not null)
            return Provenance.Set;
        return data.Jobs.Values.Any(j => j.RecipeFor(item.Id) is not null) ? Provenance.Craft : Provenance.Common;
    }

    /// <summary>The frame's colour of each provenance: grey, the chart's chocolate, its pistachio, gold.</summary>
    public static string Frame(Provenance provenance) => provenance switch
    {
        Provenance.Craft => "#8a5a36",
        Provenance.Set => "#bef374",
        Provenance.Chief => "#e0b84a",
        _ => "#7a7a7a",
    };

    /// <summary>Whether an item's name, in a language, holds the words searched (case and accents aside).</summary>
    public static bool Matches(Item item, string? search, string lang)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (string.IsNullOrWhiteSpace(search))
            return true;
        string name = Plain(item.Name.In(lang));
        return search.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(w => name.Contains(Plain(w), StringComparison.Ordinal));
    }

    // The project runs with invariant globalization, where Unicode normalization does not take accents
    // off: the letters of French and English names are mapped by hand.
    private const string Accented = "àâäáãåçéèêëîïíìôöóòõùûüúÿñ";
    private const string Bare = "aaaaaaceeeeiiiiooooouuuuyn";

    private static string Plain(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char ch in text.ToLowerInvariant())
        {
            int i = Accented.IndexOf(ch, StringComparison.Ordinal);
            sb.Append(ch switch { 'œ' => "oe", 'æ' => "ae", _ => i >= 0 ? Bare[i].ToString() : ch.ToString() });
        }
        return sb.ToString();
    }
}
