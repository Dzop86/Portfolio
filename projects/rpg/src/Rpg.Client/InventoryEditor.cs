using Rpg.Core;

namespace Rpg.Client;

/// <summary>The pages of the inventory screen: everything, then each kind of item.</summary>
public enum InventoryPage
{
    All,
    Equipment,
    Consumable,
    Resource,
    Quest,
}

/// <summary>
/// How the items of a page are ordered (sprint 181, D92: "il faut pouvoir faire des tris par panoplies, par niveau etc.
/// dans l'équipement"): by kind then level (the order since sprint 65), by set (its pieces together, the items of no set
/// last), by level (the highest first), by slot (head to feet, then hands and companions), by name.
/// </summary>
public enum InventorySort
{
    Kind,
    Set,
    Level,
    Slot,
    Name,
}

/// <summary>
/// The inventory and equipment screen, without any engine: the items owned, page by page, and a
/// draft of what the hero wears that clicks change, never against the rules (a slot the item fits,
/// its level, a two-handed weapon alone, no more than owned); the server receives the draft.
/// </summary>
public sealed class InventoryEditor
{
    private readonly Dictionary<Slot, string> _worn;

    /// <param name="readOnly">In a fight (sprint 93): the equipment is shown, never changed.</param>
    public InventoryEditor(Hero hero, IReadOnlyList<ItemCount> owned, GameData data, bool readOnly = false)
    {
        ReadOnly = readOnly;
        Hero = hero ?? throw new ArgumentNullException(nameof(hero));
        Owned = owned ?? throw new ArgumentNullException(nameof(owned));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        _worn = new Dictionary<Slot, string>(hero.Worn ?? new Dictionary<Slot, string>());
    }

    public Hero Hero { get; }

    /// <summary>Shown only (in a fight, sprint 93): nothing can be put on or taken off.</summary>
    public bool ReadOnly { get; }

    public IReadOnlyList<ItemCount> Owned { get; }
    public GameData Data { get; }

    public IReadOnlyDictionary<Slot, string> Worn => _worn;

    /// <summary>The items of a page in the order asked (by kind then level by default), with how many are not worn.</summary>
    public IReadOnlyList<(Item Item, int Count, int Free)> Page(InventoryPage page, InventorySort sort = InventorySort.Kind, string lang = "fr")
    {
        var items = Owned.Where(o => Data.Items.ContainsKey(o.Item)).Select(o => (Item: Data.Items[o.Item], o.Count))
            .Where(o => page == InventoryPage.All || o.Item.Kind.ToString() == page.ToString());
        string Name(Item i) => i.Name.In(lang);
        string SetName(Item i) => i.Set is string set && Data.Sets.TryGetValue(set, out ItemSet? s) ? s.Name.In(lang) : "";
        var ordered = sort switch
        {
            InventorySort.Set => items.OrderBy(o => o.Item.Set is null).ThenBy(o => SetName(o.Item), StringComparer.InvariantCultureIgnoreCase)
                .ThenBy(o => SlotOrder(o.Item.Slot)).ThenBy(o => o.Item.Level),
            InventorySort.Level => items.OrderByDescending(o => o.Item.Level).ThenBy(o => o.Item.Kind),
            InventorySort.Slot => items.OrderBy(o => o.Item.Kind).ThenBy(o => SlotOrder(o.Item.Slot)).ThenBy(o => o.Item.Level),
            InventorySort.Name => items.OrderBy(o => Name(o.Item), StringComparer.InvariantCultureIgnoreCase),
            _ => items.OrderBy(o => o.Item.Kind).ThenBy(o => o.Item.Level),
        };
        return [.. ordered.ThenBy(o => o.Item.Id, StringComparer.Ordinal).Select(o => (o.Item, o.Count, o.Count - _worn.Values.Count(w => w == o.Item.Id)))];
    }

    /// <summary>The order of the slots on the screen: head to feet, then hands and companions; no slot last.</summary>
    public static int SlotOrder(ItemSlot? slot) => slot is ItemSlot s && Array.IndexOf(SlotsShown, s) is int i and >= 0 ? i : SlotsShown.Length;

    private static readonly ItemSlot[] SlotsShown =
    [
        ItemSlot.Hat, ItemSlot.Amulet, ItemSlot.Cape, ItemSlot.Shoulders, ItemSlot.Chest, ItemSlot.Belt, ItemSlot.Boots,
        ItemSlot.Ring, ItemSlot.OneHanded, ItemSlot.TwoHanded, ItemSlot.Shield, ItemSlot.Pet, ItemSlot.Mount,
    ];

    /// <summary>
    /// What may go in a slot (sprint 181, D92: a click on a slot, empty or not, offers the items of the inventory): the
    /// equipment owned for that slot and not all worn elsewhere, those the hero may put on first, then by level, highest first.
    /// </summary>
    public IReadOnlyList<Item> For(Slot slot)
    {
        ItemSlot kind = slot is Slot.Ring1 or Slot.Ring2 ? ItemSlot.Ring : Enum.Parse<ItemSlot>(slot.ToString());
        return [.. Page(InventoryPage.Equipment)
            .Where(o => o.Item.Slot == kind && o.Free > 0 && !(_worn.TryGetValue(slot, out string? on) && on == o.Item.Id))
            .Select(o => o.Item)
            .OrderBy(i => WhyNot(i) is not null).ThenByDescending(i => i.Level).ThenBy(i => i.Id, StringComparer.Ordinal)];
    }

    /// <summary>Puts an item in this very slot (the ring on the finger clicked), taking off what was there.</summary>
    public bool PutIn(Slot slot, Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (WhyNot(item) is not null || !item.Slots.Contains(slot))
            return false;
        _worn[slot] = item.Id;
        if (slot == Slot.TwoHanded)
        {
            _worn.Remove(Slot.OneHanded);
            _worn.Remove(Slot.Shield);
        }
        else if (slot is Slot.OneHanded or Slot.Shield)
        {
            _worn.Remove(Slot.TwoHanded);
        }
        return true;
    }

    /// <summary>Why the hero cannot put this on now, or null.</summary>
    public string? WhyNot(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (ReadOnly)
            return "in a fight";
        if (item.Kind != ItemKind.Equipment)
            return "not equipment";
        if (item.Level > Hero.Level)
            return $"level {item.Level}";
        // The same ring never on both hands (sprint 110, D76).
        if (item.Slot == ItemSlot.Ring && _worn.ContainsValue(item.Id))
            return "already worn";
        int owned = Owned.Where(o => o.Item == item.Id).Sum(o => o.Count);
        return owned - _worn.Values.Count(w => w == item.Id) < 1 ? "none left" : null;
    }

    /// <summary>
    /// Puts an item on: in its slot (a ring on the free finger, else the first), taking off what was
    /// there; a two-handed weapon takes off the one-handed weapon and the shield, and the other way round.
    /// </summary>
    public bool Put(Item item)
    {
        if (WhyNot(item) is not null)
            return false;
        return PutIn(item.Slots.FirstOrDefault(s => !_worn.ContainsKey(s), item.Slots[0]), item);
    }

    public void TakeOff(Slot slot)
    {
        if (!ReadOnly)
            _worn.Remove(slot);
    }

    /// <summary>The hero as the draft would have it.</summary>
    public Hero Draft => Hero with { Worn = new Dictionary<Slot, string>(_worn) };

    public bool Changed => Draft != Hero;
}
