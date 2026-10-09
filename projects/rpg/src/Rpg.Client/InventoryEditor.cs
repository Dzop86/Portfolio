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
/// The inventory and equipment screen, without any engine: the items owned, page by page, and a
/// draft of what the hero wears that clicks change, never against the rules (a slot the item fits,
/// its level, a two-handed weapon alone, no more than owned); the server receives the draft.
/// </summary>
public sealed class InventoryEditor
{
    private readonly Dictionary<Slot, string> _worn;

    public InventoryEditor(Hero hero, IReadOnlyList<ItemCount> owned, GameData data)
    {
        Hero = hero ?? throw new ArgumentNullException(nameof(hero));
        Owned = owned ?? throw new ArgumentNullException(nameof(owned));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        _worn = new Dictionary<Slot, string>(hero.Worn ?? new Dictionary<Slot, string>());
    }

    public Hero Hero { get; }
    public IReadOnlyList<ItemCount> Owned { get; }
    public GameData Data { get; }

    public IReadOnlyDictionary<Slot, string> Worn => _worn;

    /// <summary>The items of a page, in the order of the data, with how many are not worn.</summary>
    public IReadOnlyList<(Item Item, int Count, int Free)> Page(InventoryPage page) =>
        [.. Owned.Where(o => Data.Items.ContainsKey(o.Item)).Select(o => (Item: Data.Items[o.Item], o.Count))
            .Where(o => page == InventoryPage.All || o.Item.Kind.ToString() == page.ToString())
            .OrderBy(o => o.Item.Kind).ThenBy(o => o.Item.Level).ThenBy(o => o.Item.Id, StringComparer.Ordinal)
            .Select(o => (o.Item, o.Count, o.Count - _worn.Values.Count(w => w == o.Item.Id)))];

    /// <summary>Why the hero cannot put this on now, or null.</summary>
    public string? WhyNot(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Kind != ItemKind.Equipment)
            return "not equipment";
        if (item.Level > Hero.Level)
            return $"level {item.Level}";
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
        Slot slot = item.Slots.FirstOrDefault(s => !_worn.ContainsKey(s), item.Slots[0]);
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

    public void TakeOff(Slot slot) => _worn.Remove(slot);

    /// <summary>The hero as the draft would have it.</summary>
    public Hero Draft => Hero with { Worn = new Dictionary<Slot, string>(_worn) };

    public bool Changed => Draft != Hero;
}
