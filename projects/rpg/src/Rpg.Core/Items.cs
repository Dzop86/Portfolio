using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>The fourteen places a hero wears or holds something (sprint 59).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Slot>))]
public enum Slot
{
    Ring1,
    Ring2,
    Amulet,
    Belt,
    Cape,
    Hat,
    Chest,
    Shoulders,
    OneHanded,
    TwoHanded,
    Shield,
    Pet,
    Mount,
    Boots,
}

/// <summary>The pages of the inventory, after "everything".</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ItemKind>))]
public enum ItemKind
{
    Equipment,
    Consumable,
    Resource,
    Quest,
}

/// <summary>
/// The kind of place a piece of equipment goes: a ring fits either ring slot, everything else
/// its own slot.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ItemSlot>))]
public enum ItemSlot
{
    Ring,
    Amulet,
    Belt,
    Cape,
    Hat,
    Chest,
    Shoulders,
    OneHanded,
    TwoHanded,
    Shield,
    Pet,
    Mount,
    Boots,
}

/// <summary>
/// An item, as described in <c>data/items.json</c>: a piece of equipment (its slot, the level it
/// needs, the characteristics, action and movement points it gives, its set, and since sprint 95 the
/// initiative of boots, capes and rings), a consumable, a resource or a quest item.
/// </summary>
public sealed record Item(
    string Id,
    LocalizedText Name,
    ItemKind Kind,
    ItemSlot? Slot = null,
    int Level = 1,
    Characteristics? Stats = null,
    int Ap = 0,
    int Mp = 0,
    string? Set = null,
    int Initiative = 0,
    string? Icon = null,
    int Heal = 0)
{
    /// <summary>The kinds of pieces that may carry initiative (sprint 95, D71): the light ones.</summary>
    public static readonly IReadOnlyList<ItemSlot> Quick = [ItemSlot.Boots, ItemSlot.Cape, ItemSlot.Ring];

    /// <summary>The slots this item may go in (none for what is not equipment).</summary>
    [JsonIgnore]
    public IReadOnlyList<Slot> Slots => Slot switch
    {
        null => [],
        ItemSlot.Ring => [Core.Slot.Ring1, Core.Slot.Ring2],
        ItemSlot s => [Enum.Parse<Slot>(s.ToString())],
    };
}

/// <summary>What a set gives when that many of its pieces are worn.</summary>
public sealed record SetBonus(int Pieces, Characteristics? Stats = null, int Ap = 0, int Mp = 0, int Initiative = 0);

/// <summary>A set of items, as described in <c>data/sets.json</c>: bonuses for wearing several pieces.</summary>
public sealed record ItemSet(string Id, LocalizedText Name, IReadOnlyList<SetBonus> Bonuses);

/// <summary>
/// What a monster may leave: an item, its chance in percent, how many. A quest's loot (sprint 105, D75)
/// falls only while <see cref="Quest"/> is under way and does not have all of it yet.
/// </summary>
public sealed record LootDrop(string Item, int Percent, int Count = 1, string? Quest = null);

/// <summary>Some of an item, in an inventory or in loot.</summary>
public sealed record ItemCount(string Item, int Count);

/// <summary>
/// Equipment rules (sprint 59): what a hero wears adds up with its own characteristics, the sets'
/// bonuses on top; and what the monsters of a won fight leave, drawn from the fight's seed so that
/// the server and the game agree without trusting the game.
/// </summary>
public static class Equipment
{
    /// <summary>Why the hero cannot wear this, or null: equipment, a slot it fits, its level, a two-handed weapon alone, two different rings.</summary>
    public static string? Problem(IReadOnlyDictionary<Slot, string> worn, int level, GameData data)
    {
        ArgumentNullException.ThrowIfNull(worn);
        ArgumentNullException.ThrowIfNull(data);
        foreach ((Slot slot, string id) in worn)
        {
            if (!data.Items.TryGetValue(id, out Item? item))
                return $"Unknown item '{id}'.";
            if (!item.Slots.Contains(slot))
                return $"'{id}' does not go in {slot}.";
            if (item.Level > level)
                return $"'{id}' needs level {item.Level}.";
        }
        if (worn.ContainsKey(Slot.TwoHanded) && (worn.ContainsKey(Slot.OneHanded) || worn.ContainsKey(Slot.Shield)))
            return "A two-handed weapon takes both hands: no one-handed weapon, no shield.";
        // The same ring on both hands (sprint 110, D76): the jeweller would make the double trivial.
        if (worn.TryGetValue(Slot.Ring1, out string? first) && worn.TryGetValue(Slot.Ring2, out string? second) && first == second)
            return "The same ring cannot be worn twice.";
        return null;
    }

    /// <summary>The bonuses of the sets of what is worn: for each set, its best bonus for the pieces worn.</summary>
    public static IEnumerable<SetBonus> SetBonuses(IReadOnlyDictionary<Slot, string> worn, GameData data)
    {
        ArgumentNullException.ThrowIfNull(worn);
        ArgumentNullException.ThrowIfNull(data);
        foreach (IGrouping<string, string> set in worn.Values.Distinct().Where(data.Items.ContainsKey).Where(id => data.Items[id].Set is not null).GroupBy(id => data.Items[id].Set!))
        {
            if (data.Sets.TryGetValue(set.Key, out ItemSet? s) && s.Bonuses.Where(b => b.Pieces <= set.Count()).MaxBy(b => b.Pieces) is SetBonus best)
                yield return best;
        }
    }

    /// <summary>The characteristics, action and movement points and initiative that what is worn adds, sets included.</summary>
    public static (Characteristics Stats, int Ap, int Mp, int Initiative) Total(IReadOnlyDictionary<Slot, string>? worn, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Characteristics sum = Characteristics.None;
        int ap = 0, mp = 0, initiative = 0;
        if (worn is null)
            return (sum, ap, mp, initiative);
        foreach (Item item in worn.Values.Where(data.Items.ContainsKey).Select(id => data.Items[id]))
        {
            sum = Add(sum, item.Stats);
            (ap, mp, initiative) = (ap + item.Ap, mp + item.Mp, initiative + item.Initiative);
        }
        foreach (SetBonus bonus in SetBonuses(worn, data))
        {
            sum = Add(sum, bonus.Stats);
            (ap, mp, initiative) = (ap + bonus.Ap, mp + bonus.Mp, initiative + bonus.Initiative);
        }
        return (sum, ap, mp, initiative);
    }

    public static Characteristics Add(Characteristics a, Characteristics? b) => b is null ? a
        : new(a.Vitality + b.Vitality, a.Earth + b.Earth, a.Fire + b.Fire, a.Water + b.Water, a.Air + b.Air);

    /// <summary>
    /// What the monsters of a fight the hero's team won leave: each drop of each defeated monster is
    /// drawn once, in order, from a generator seeded by the fight's seed (not the fight's own rolls).
    /// A quest's loot (sprint 105) is drawn like the rest but kept only if <paramref name="wanted"/> has it
    /// ("quest/item", <see cref="Quests.LootWanted"/>): the server knows the quests, the draws stay the same.
    /// </summary>
    public static IReadOnlyList<ItemCount> Loot(Fight fight, IReadOnlySet<string>? wanted = null, int draw = 0)
    {
        ArgumentNullException.ThrowIfNull(fight);
        var loot = new List<ItemCount>();
        if (fight.Hero is null || fight.WinningTeam != 0)
            return loot;
        // Each hero of a party draws its own loot (sprint 120): the k-th from the k-th stream of the seed; the hero alone, the first.
        var rng = new Rng(fight.Seed ^ 0x9E3779B97F4A7C15 ^ ((ulong)draw * 0xBF58476D1CE4E5B9UL));
        foreach (Fighter f in fight.Fighters.Where(f => f.Team != 0 && !f.IsSummon))
        {
            foreach (LootDrop drop in f.Spec.Loot ?? [])
            {
                if (rng.Next(1, 100) <= drop.Percent && (drop.Quest is null || wanted?.Contains($"{drop.Quest}/{drop.Item}") == true))
                    loot.Add(new ItemCount(drop.Item, drop.Count));
            }
        }
        return [.. loot.GroupBy(s => s.Item).Select(g => new ItemCount(g.Key, g.Sum(s => s.Count)))];
    }
}
