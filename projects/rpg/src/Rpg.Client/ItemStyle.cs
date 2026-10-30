using System.Text;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// How an item looks, without any engine (sprint 65): since sprint 166 (D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §4)
/// a tile in relief drawn by <c>tools/item_art.py</c>, its drawing in the colour of what it is made of, its rim telling
/// where it comes from (<see cref="Frame"/>), a dot of its element on equipment (<see cref="Element"/>); its colour in the
/// chat (<see cref="Colour"/>); and the search through item names.
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
    /// <summary>The file of an item's tile in <c>assets/icons/items/tiles</c>, without its extension; flat for one not wearable now.</summary>
    public static string Tile(Item item, bool locked = false)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Id + (locked ? "-locked" : "");
    }

    /// <summary>The file of an empty slot's tile: the slot's drawing, flat in a hollow.</summary>
    public static string Tile(ItemSlot slot) => "slot-" + slot.ToString().ToLowerInvariant();

    /// <summary>
    /// The dot on an equipment's tile: its strongest characteristic's element (ties in the sheet's order), Vitality the
    /// heal's; none for an item that gives nothing or is not worn.
    /// </summary>
    public static string? Element(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Kind != ItemKind.Equipment)
            return null;
        Characteristics st = item.Stats ?? Characteristics.None;
        (int Value, string Colour)[] all =
        [
            (st.Vitality, ElementStyle.Heal),
            (st.Earth, ElementStyle.Colour(Rpg.Core.Element.Earth)),
            (st.Fire, ElementStyle.Colour(Rpg.Core.Element.Fire)),
            (st.Water, ElementStyle.Colour(Rpg.Core.Element.Water)),
            (st.Air, ElementStyle.Colour(Rpg.Core.Element.Air)),
        ];
        (int best, string colour) = all.Aggregate((a, b) => b.Value > a.Value ? b : a);
        return best > 0 ? colour : null;
    }

    /// <summary>
    /// The colour of an item's name in the chat: its element's (<see cref="Element"/>); food and potions the healing's.
    /// </summary>
    public static string Colour(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Heal > 0 ? ElementStyle.Heal : Element(item) ?? (item.Kind == ItemKind.Quest ? "#f0c060" : "#bdbdbd");
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

    /// <summary>
    /// The rim's colour of each provenance (sprint 166, s165 §4.5): a discreet grey, light chocolate, the violet of a set's
    /// piece (the pistachio is the ring of what is chosen), the top of the gold of a chief's (a gradient on the tile).
    /// </summary>
    public static string Frame(Provenance provenance) => provenance switch
    {
        Provenance.Craft => "#c9a27f",
        Provenance.Set => "#b48cff",
        Provenance.Chief => UiStyle.GoldTop,
        _ => "#5c5c5c",
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
