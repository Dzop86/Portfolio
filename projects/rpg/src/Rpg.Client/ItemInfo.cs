using Rpg.Core;

namespace Rpg.Client;

/// <summary>What a right click on an item offers (sprint 117, D77), in the menu's order.</summary>
public enum ItemAction
{
    Equip,
    TakeOff,
    Use,
    Recipes,
    Bestiary,
    Set,
    Share,
}

/// <summary>Where the item was right-clicked: owned or not, worn, the inventory open, in a fight, the chat at hand.</summary>
public sealed record ItemPlace(int Owned = 0, bool Worn = false, bool InInventory = false, bool InFight = false, bool CanShare = false);

/// <summary>A recipe that makes the item, or that needs it: its trade, the recipe, and which of the two.</summary>
public sealed record ItemRecipe(Job Job, Recipe Recipe, bool Makes);

/// <summary>A monster that leaves the item: its family, its level, its chance in percent, how many, for a quest only.</summary>
public sealed record ItemDrop(MonsterSpec Monster, MonsterFamily? Family, int Percent, int Count, bool ForQuest);

/// <summary>
/// What the game knows of an item beyond its card (sprint 117, D77): the recipes that make it or need it, the monsters
/// that leave it and at what chance, its set's pieces, and what a right click on it offers.
/// </summary>
public sealed class ItemInfo(GameData data)
{
    private readonly GameData _data = data ?? throw new ArgumentNullException(nameof(data));

    /// <summary>The recipes that make the item first, then those that need it, by trade and level.</summary>
    public IReadOnlyList<ItemRecipe> Recipes(string item) =>
        [.. _data.Jobs.Values.SelectMany(j => j.Recipes.Select(r => (Job: j, Recipe: r)))
            .Select(x => new ItemRecipe(x.Job, x.Recipe, x.Recipe.Item == item))
            .Where(x => x.Makes || x.Recipe.Ingredients.Any(i => i.Item == item))
            .OrderBy(x => !x.Makes).ThenBy(x => x.Recipe.Level).ThenBy(x => x.Job.Id, StringComparer.Ordinal).ThenBy(x => x.Recipe.Item, StringComparer.Ordinal)];

    /// <summary>The monsters that leave the item, the likeliest first, then the lowest levels.</summary>
    public IReadOnlyList<ItemDrop> Drops(string item) =>
        [.. _data.Monsters.Values.SelectMany(m => (m.Loot ?? []).Where(l => l.Item == item)
                .Select(l => new ItemDrop(m, _data.Families.Values.FirstOrDefault(f => f.Monsters.Any(x => x.Id == m.Id)), l.Percent, l.Count, l.Quest is not null)))
            .OrderByDescending(d => d.Percent).ThenBy(d => d.Monster.Level).ThenBy(d => d.Monster.Id, StringComparer.Ordinal)];

    /// <summary>The item's set and its pieces, by slot then level; none for an item of no set.</summary>
    public (ItemSet Set, IReadOnlyList<Item> Pieces)? Set(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Set is string id && _data.Sets.TryGetValue(id, out ItemSet? set)
            ? (set, [.. _data.Items.Values.Where(i => i.Set == id).OrderBy(i => i.Slot).ThenBy(i => i.Level).ThenBy(i => i.Id, StringComparer.Ordinal)])
            : null;
    }

    /// <summary>
    /// What the right click offers: to put on equipment owned (in the inventory, out of a fight), to take off what is
    /// worn, to use food or a potion owned out of a fight; its recipes, the monsters that leave it, its set when there
    /// are any; to share it when the chat is at hand.
    /// </summary>
    public IReadOnlyList<ItemAction> Actions(Item item, ItemPlace place)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(place);
        var actions = new List<ItemAction>();
        bool changing = place.InInventory && !place.InFight;
        if (changing && place.Worn)
            actions.Add(ItemAction.TakeOff);
        else if (changing && item.Kind == ItemKind.Equipment && place.Owned > 0)
            actions.Add(ItemAction.Equip);
        if (item.Heal > 0 && place.Owned > 0 && !place.InFight)
            actions.Add(ItemAction.Use);
        if (Recipes(item.Id).Count > 0)
            actions.Add(ItemAction.Recipes);
        if (Drops(item.Id).Count > 0)
            actions.Add(ItemAction.Bestiary);
        if (Set(item) is not null)
            actions.Add(ItemAction.Set);
        if (place.CanShare)
            actions.Add(ItemAction.Share);
        return actions;
    }
}
