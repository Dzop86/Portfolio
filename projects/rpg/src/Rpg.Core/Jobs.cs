namespace Rpg.Core;

/// <summary>What a trade makes (sprint 81): an item, from the job's level, out of its ingredients.</summary>
public sealed record Recipe(string Item, int Level, IReadOnlyList<ItemCount> Ingredients);

/// <summary>
/// A trade, as described in <c>data/jobs.json</c> (sprint 81, D65): its name, its workshop's, and what it
/// makes. The first is the locksmith, who makes the dungeons' keys.
/// </summary>
public sealed record Job(string Id, LocalizedText Name, LocalizedText Workshop, IReadOnlyList<Recipe> Recipes)
{
    public Recipe? RecipeFor(string item) => Recipes.FirstOrDefault(r => r.Item == item);
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

    /// <summary>The experience one item of a recipe gives: 20, and 5 for each level the recipe needs.</summary>
    public static long Xp(Recipe recipe) => 20 + 5L * (recipe ?? throw new ArgumentNullException(nameof(recipe))).Level;

    /// <summary>Why <paramref name="count"/> of a recipe cannot be made now, or null if they can.</summary>
    public static string? Problem(Recipe recipe, int jobLevel, IEnumerable<ItemCount> owned, int count = 1)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(owned);
        if (count is < 1 or > MostAtOnce)
            return $"1 to {MostAtOnce} at once.";
        if (jobLevel < recipe.Level)
            return $"The trade's level {recipe.Level} is needed.";
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
