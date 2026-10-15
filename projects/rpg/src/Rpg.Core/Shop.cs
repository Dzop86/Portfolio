namespace Rpg.Core;

/// <summary>
/// Rose's stall and what the Éclats are spent on (sprint 97, D71, docs/suite-apres-d69.md §4): she buys
/// resources and equipment, sells the monsters' resources dearer, in the village only; taking back
/// characteristic points costs Éclats from level 11, the locksmith's keys too. The server applies these
/// rules and keeps the Éclats; the client shows the prices.
/// </summary>
public static class Shop
{
    /// <summary>The map the stall stands on.</summary>
    public const string Village = "clairval";

    /// <summary>The most bought or sold at once.</summary>
    public const int MostAtOnce = 99;

    /// <summary>Taking back characteristic points is free up to this level.</summary>
    public const int FreeResetUpTo = 10;

    /// <summary>
    /// A resource's level: that of the weakest monster leaving it, else the item's own.
    /// </summary>
    public static int Level(Item item, GameData data)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(data);
        int[] droppers = [.. data.Families.Values.SelectMany(f => f.Monsters).Where(m => (m.Loot ?? []).Any(l => l.Item == item.Id)).Select(m => m.Level)];
        return droppers.Length > 0 ? droppers.Min() : item.Level;
    }

    /// <summary>What Rose pays for one: a resource 1 + its level / 10, a piece of equipment twice its level; null if she does not buy it.</summary>
    public static long? BuyPrice(Item item, GameData data)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Kind switch
        {
            ItemKind.Resource => 1 + Level(item, data) / 10,
            ItemKind.Equipment => 2L * item.Level,
            _ => null,
        };
    }

    /// <summary>The resources Rose sells, the monsters' and since sprint 99 the gathered ones, in the order of the data (not the dungeons' keys: the locksmith makes them).</summary>
    public static IReadOnlyList<Item> Stock(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        HashSet<string> keys = [.. data.Dungeons.Values.Select(d => d.Key)];
        HashSet<string> dropped = [.. data.Families.Values.SelectMany(f => f.Monsters).SelectMany(m => m.Loot ?? []).Select(l => l.Item),
            .. data.Jobs.Values.SelectMany(j => j.Harvests ?? []).Select(h => h.Item)];
        return [.. data.Items.Values.Where(i => i.Kind == ItemKind.Resource && dropped.Contains(i.Id) && !keys.Contains(i.Id))];
    }

    /// <summary>What Rose asks for one of her stock: five times what she would pay; null if she does not sell it.</summary>
    public static long? SellPrice(Item item, GameData data)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Stock(data).Contains(item) ? 5 * BuyPrice(item, data) : null;
    }

    /// <summary>Why <paramref name="count"/> cannot be bought now, or null: in stock, in the village, with the Éclats.</summary>
    public static string? WhyNotBuy(Item item, int count, long shards, string? place, GameData data)
    {
        ArgumentNullException.ThrowIfNull(item);
        return place != Village ? "Rose's stall is in the village."
            : count is < 1 or > MostAtOnce ? $"1 to {MostAtOnce} at once."
            : SellPrice(item, data) is not long price ? "Rose does not sell this."
            : price * count > shards ? $"{price * count} Éclats needed."
            : null;
    }

    /// <summary>Why <paramref name="count"/> cannot be sold now, or null: in the village, something Rose buys, owned and not worn.</summary>
    public static string? WhyNotSell(Item item, int count, IEnumerable<ItemCount> owned, IReadOnlyDictionary<Slot, string>? worn, string? place, GameData data)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(owned);
        int free = owned.Where(o => o.Item == item.Id).Sum(o => o.Count) - (worn ?? new Dictionary<Slot, string>()).Values.Count(w => w == item.Id);
        return place != Village ? "Rose's stall is in the village."
            : count is < 1 or > MostAtOnce ? $"1 to {MostAtOnce} at once."
            : BuyPrice(item, data) is null ? "Rose does not buy this."
            : free < count ? "Not that many, or worn." : null;
    }

    /// <summary>What taking back characteristic points costs at a level: nothing up to level 10, then ten Éclats a level.</summary>
    public static long ResetCost(int level) => level <= FreeResetUpTo ? 0 : 10L * level;

    /// <summary>Whether new characteristics take back any point put in before.</summary>
    public static bool TakesBack(Characteristics before, Characteristics after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        return after.Vitality < before.Vitality || after.Earth < before.Earth || after.Fire < before.Fire || after.Water < before.Water || after.Air < before.Air;
    }
}
