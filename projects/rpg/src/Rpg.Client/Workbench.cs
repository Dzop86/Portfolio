using Rpg.Core;

namespace Rpg.Client;

/// <summary>A recipe as the workshop shows it: its name and level, each ingredient as owned/needed, how many can be made, or why none.</summary>
public sealed record RecipeRow(Recipe Recipe, string Title, IReadOnlyList<string> Ingredients, int Most, string? Refusal);

/// <summary>
/// A trade's workshop (sprint 81) without an engine: the trade's level and experience, its recipes in
/// order of level with what each needs and what the character owns. The Godot client draws it; the
/// server decides (<see cref="GameServer.Craft"/>).
/// </summary>
public sealed class Workbench(Job job, CharacterSummary? character, Texts texts)
{
    public Job Job { get; } = job ?? throw new ArgumentNullException(nameof(job));

    public long Xp => character?.Jobs?.GetValueOrDefault(Job.Id) ?? 0;

    public int Level => Crafting.LevelFor(Xp);

    public string Title => texts["craft.title", Job.Name.In(texts.Lang), Level];

    public string XpText => texts["craft.xp", Xp, Crafting.XpToReach(Math.Min(Level + 1, Crafting.MaxLevel))];

    public IReadOnlyList<RecipeRow> Rows
    {
        get
        {
            IReadOnlyList<ItemCount> owned = character?.Inventory ?? [];
            int Have(string item) => owned.Where(i => i.Item == item).Sum(i => i.Count);
            return [.. Job.Recipes.OrderBy(r => r.Level).ThenBy(r => r.Item, StringComparer.Ordinal).Select(r =>
            {
                int most = Math.Min(Crafting.MostFrom(r, owned), Crafting.MostAtOnce);
                string? refusal = character is null ? texts["craft.offline"]
                    : Level < r.Level ? texts["craft.level", r.Level]
                    : most == 0 ? texts["craft.missing"]
                    : null;
                return new RecipeRow(r, texts["craft.recipe", texts.Items([new ItemCount(r.Item, 1)]), r.Level],
                    [.. r.Ingredients.Select(i => $"{texts.Items([new ItemCount(i.Item, 1)])} {Have(i.Item)}/{i.Count}")],
                    refusal is null ? most : 0, refusal);
            })];
        }
    }
}
