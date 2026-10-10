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
            (st.Strength, ElementStyle.Colour(Element.Earth)),
            (st.Intelligence, ElementStyle.Colour(Element.Fire)),
            (st.Chance, ElementStyle.Colour(Element.Water)),
            (st.Agility, ElementStyle.Colour(Element.Air)),
        ];
        (int best, string colour) = all.Aggregate((a, b) => b.Value > a.Value ? b : a);
        return best > 0 ? colour : item.Kind == ItemKind.Quest ? "#f0c060" : "#bdbdbd";
    }

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
