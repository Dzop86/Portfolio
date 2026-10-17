namespace Rpg.Core;

/// <summary>What a trade makes (sprint 81): an item, from the job's level, out of its ingredients, and since sprint 97 Éclats.</summary>
public sealed record Recipe(string Item, int Level, IReadOnlyList<ItemCount> Ingredients, long Shards = 0);

/// <summary>The kinds of spots a gathering trade works (sprint 99): a marked tree, a veined rock, water, a plot, herbs.</summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<NodeKind>))]
public enum NodeKind
{
    Tree,
    Rock,
    Water,
    Plot,
    Herb,
}

/// <summary>What a gathering trade takes (sprint 99): a resource, from the trade's level, on a kind of spot.</summary>
public sealed record Harvest(string Item, int Level, NodeKind Node);

/// <summary>
/// A trade, as described in <c>data/jobs.json</c> (sprint 81, D65): its name, its workshop's, and what it
/// makes. The first is the locksmith, who makes the dungeons' keys; since sprint 99 the gathering trades
/// (woodcutter, miner, fisher, farmer, herbalist) have no workshop and take resources (<see cref="Harvests"/>).
/// </summary>
public sealed record Job(string Id, LocalizedText Name, IReadOnlyList<Recipe> Recipes, LocalizedText? Workshop = null, IReadOnlyList<Harvest>? Harvests = null)
{
    public Recipe? RecipeFor(string item) => Recipes.FirstOrDefault(r => r.Item == item);

    /// <summary>A gathering trade (sprint 99).</summary>
    public bool Gathers => Harvests is { Count: > 0 };
}

/// <summary>
/// The rules of the trades: a level from 1 to 100 that follows the trade's experience, and experience
/// for each item made, more for the harder recipes. A recipe is made from its level on, with its
/// ingredients owned.
/// </summary>
public static class Crafting
{
    public const int MaxLevel = 100;

    /// <summary>The most made at once.</summary>
    public const int MostAtOnce = 20;

    /// <summary>The experience a trade needs for a level: 10 × (level − 1) × level (90 for level 4, 900 for level 10).</summary>
    public static long XpToReach(int level) => 10L * (Math.Clamp(level, 1, MaxLevel) - 1) * Math.Clamp(level, 1, MaxLevel);

    public static int LevelFor(long xp)
    {
        int level = 1;
        while (level < MaxLevel && XpToReach(level + 1) <= xp)
            level++;
        return level;
    }

    /// <summary>Recipes this many levels or more under the trade give no experience, as gathering (sprint 109, D76).</summary>
    public const int NoXpBelow = 20;

    /// <summary>
    /// The experience one item of a recipe gives: a piece of equipment 150, and 15 for each level the recipe needs
    /// (sprint 109, D76: 165 at level 1, 675 at level 35); anything else 20, and 5 a level; nothing from a recipe
    /// <see cref="NoXpBelow"/> levels or more under the trade's.
    /// </summary>
    public static long Xp(Recipe recipe, Item made, int jobLevel = 1)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(made);
        if (jobLevel - recipe.Level >= NoXpBelow)
            return 0;
        return made.Kind == ItemKind.Equipment ? 150 + 15L * recipe.Level : 20 + 5L * recipe.Level;
    }

    /// <summary>Why <paramref name="count"/> of a recipe cannot be made now, or null if they can; <paramref name="shards"/> owned, if known.</summary>
    public static string? Problem(Recipe recipe, int jobLevel, IEnumerable<ItemCount> owned, int count = 1, long? shards = null)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(owned);
        if (count is < 1 or > MostAtOnce)
            return $"1 to {MostAtOnce} at once.";
        if (jobLevel < recipe.Level)
            return $"The trade's level {recipe.Level} is needed.";
        if (shards is long owns && recipe.Shards * count > owns)
            return $"{recipe.Shards * count} Éclats needed.";
        var have = owned.GroupBy(i => i.Item).ToDictionary(g => g.Key, g => g.Sum(i => i.Count), StringComparer.Ordinal);
        return recipe.Ingredients.FirstOrDefault(i => have.GetValueOrDefault(i.Item) < i.Count * count) is ItemCount missing
            ? $"Not enough '{missing.Item}': {missing.Count * count} needed."
            : null;
    }

    /// <summary>How many of a recipe the ingredients owned are enough for.</summary>
    public static int MostFrom(Recipe recipe, IEnumerable<ItemCount> owned)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(owned);
        var have = owned.GroupBy(i => i.Item).ToDictionary(g => g.Key, g => g.Sum(i => i.Count), StringComparer.Ordinal);
        return recipe.Ingredients.Min(i => have.GetValueOrDefault(i.Item) / i.Count);
    }
}
