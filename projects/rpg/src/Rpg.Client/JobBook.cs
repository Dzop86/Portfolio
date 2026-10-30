using Rpg.Core;

namespace Rpg.Client;

/// <summary>A trade in the list: its name, its level, the experience gained in the level and needed for the next, whether it gathers, whether it was ever practised.</summary>
public sealed record JobLine(Job Job, string Name, int Level, long XpIn, long XpNeeded, bool Gathers, bool Practised)
{
    /// <summary>
    /// Its icon in relief (sprint 164, <c>docs/graphisme/s163-icones-arbre.md</c> §2), in <c>assets/icons/relief/</c>: in
    /// its matter's colour, grey (<c>-locked</c>) while it was never practised.
    /// </summary>
    public string Icon => $"job-{Job.Id}" + (Practised ? "" : "-locked");

    /// <summary>The part of its experience bar filled, from 0 to 1 (full once nothing more is needed).</summary>
    public double Progress => XpNeeded <= 0 ? 1 : Math.Clamp((double)XpIn / XpNeeded, 0, 1);
}

/// <summary>An ingredient of a recipe: how many are needed, how many are owned.</summary>
public sealed record Needed(Item Item, int Count, int Owned)
{
    public bool Missing => Owned < Count;
}

/// <summary>A recipe in the list: what it makes, its level (too high for the trade?), its ingredients, its experience, how many the ingredients allow.</summary>
public sealed record RecipeLine(Job Job, Recipe Recipe, Item Item, bool TooHigh, IReadOnlyList<Needed> Ingredients, long Xp, int Most, bool CanMake);

/// <summary>A resource of a gathering trade: its level, where it grows, how many are owned; "soon" for the levels not in the game yet.</summary>
public sealed record ResourceLine(Item? Item, int Level, string Where, int Owned);

/// <summary>How the recipes are sorted (the highest level first, by default, as in the games of the genre).</summary>
public enum RecipeOrder
{
    Highest,
    Lowest,
    Name,
}

/// <summary>
/// The trades window (sprint 100, D72, docs/suite-apres-d69.md §5.9) without an engine: the trades, gathering
/// then making, with their level and experience; for the trade chosen, its recipes (for a gathering trade,
/// those using its resources), searched, filtered by level and kind, sorted, the possible ones only if asked;
/// its resources and where they grow. Making happens only at the trade's workshop (<see cref="Workshop"/>).
/// The Godot client draws it; the server decides.
/// </summary>
public sealed class JobBook(GameData data, CharacterSummary? character, Texts texts, string? workshop = null)
{
    /// <summary>A gathering trade's resources: one every 5 levels up to 100.</summary>
    public const int Step = 5;

    /// <summary>
    /// The trades a character went up in, and their new level (sprint 103, Charles: a message on a trade's new
    /// level), from what it was to what the server sent back; none without a character before.
    /// </summary>
    public static IReadOnlyList<(string Job, int Level)> LevelsGained(CharacterSummary? before, CharacterSummary after)
    {
        ArgumentNullException.ThrowIfNull(after);
        if (before is null || before.Id != after.Id)
            return [];
        return [.. (after.Jobs ?? new Dictionary<string, long>()).Keys.Order(StringComparer.Ordinal)
            .Where(j => after.JobLevel(j) > before.JobLevel(j))
            .Select(j => (j, after.JobLevel(j)))];
    }

    public GameData Data { get; } = data ?? throw new ArgumentNullException(nameof(data));

    /// <summary>The trade whose workshop the character stands at, where it can make (null: to look at only).</summary>
    public string? Workshop { get; } = workshop;

    private IReadOnlyList<ItemCount> Owned => character?.Inventory ?? [];

    private int Have(string item) => Owned.Where(i => i.Item == item).Sum(i => i.Count);

    public long XpOf(Job job) => character?.Jobs?.GetValueOrDefault((job ?? throw new ArgumentNullException(nameof(job))).Id) ?? 0;

    public int LevelOf(Job job) => Crafting.LevelFor(XpOf(job));

    /// <summary>The trades: the gathering ones, then those that make, each in the data's order.</summary>
    public IReadOnlyList<JobLine> Jobs =>
        [.. Data.Jobs.Values.OrderBy(j => j.Gathers ? 0 : 1).Select(j =>
        {
            int level = LevelOf(j);
            long from = Crafting.XpToReach(level), to = Crafting.XpToReach(Math.Min(level + 1, Crafting.MaxLevel));
            return new JobLine(j, j.Name.In(texts.Lang), level, XpOf(j) - from, Math.Max(0, to - from), j.Gathers, XpOf(j) > 0);
        })];

    public string Search { get; set; } = "";

    public int MinLevel { get; set; } = 1;

    public int MaxLevel { get; set; } = Crafting.MaxLevel;

    /// <summary>The kind of item shown (null: all), among <see cref="Kinds"/>.</summary>
    public string? Kind { get; set; }

    public RecipeOrder Order { get; set; } = RecipeOrder.Highest;

    public bool PossibleOnly { get; set; }

    /// <summary>A kind of item a recipe makes, for the filter: its slot, else its kind of item.</summary>
    public static string KindOf(Item item) => (item ?? throw new ArgumentNullException(nameof(item))).Slot?.ToString() ?? item.Kind.ToString();

    /// <summary>The recipes of a trade; for a gathering one, the recipes of every trade using its resources.</summary>
    private IEnumerable<(Job Job, Recipe Recipe)> AllRecipes(Job job)
    {
        if (!job.Gathers)
            return job.Recipes.Select(r => (job, r));
        HashSet<string> mine = [.. job.Harvests!.Select(h => h.Item)];
        return Data.Jobs.Values.SelectMany(j => j.Recipes.Where(r => r.Ingredients.Any(i => mine.Contains(i.Item))).Select(r => (j, r)));
    }

    /// <summary>The kinds of items a trade's recipes make, for the filter.</summary>
    public IReadOnlyList<string> Kinds(Job job) => [.. AllRecipes(job).Select(x => KindOf(Data.Items[x.Recipe.Item])).Distinct()];

    /// <summary>The recipes of the trade chosen, as the filters and the order say.</summary>
    public IReadOnlyList<RecipeLine> Recipes(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        IEnumerable<RecipeLine> lines = AllRecipes(job).Select(x =>
        {
            Item item = Data.Items[x.Recipe.Item];
            int level = LevelOf(x.Job);
            Needed[] needed = [.. x.Recipe.Ingredients.Select(i => new Needed(Data.Items[i.Item], i.Count, Have(i.Item)))];
            int most = Math.Min(Crafting.MostFrom(x.Recipe, Owned), Crafting.MostAtOnce);
            bool can = character is not null && Workshop == x.Job.Id && Crafting.Problem(x.Recipe, level, Owned, 1, character.Shards) is null;
            return new RecipeLine(x.Job, x.Recipe, item, level < x.Recipe.Level, needed, Crafting.Xp(x.Recipe, item, level), can ? most : 0, can);
        }).Where(l => l.Recipe.Level >= MinLevel && l.Recipe.Level <= MaxLevel
            && (Kind is null || KindOf(l.Item) == Kind)
            && ItemStyle.Matches(l.Item, Search, texts.Lang)
            && (!PossibleOnly || (!l.TooHigh && l.Ingredients.All(i => !i.Missing))));
        return Order switch
        {
            RecipeOrder.Lowest => [.. lines.OrderBy(l => l.Recipe.Level).ThenBy(l => l.Item.Name.In(texts.Lang), StringComparer.Ordinal)],
            RecipeOrder.Name => [.. lines.OrderBy(l => l.Item.Name.In(texts.Lang), StringComparer.Ordinal)],
            _ => [.. lines.OrderByDescending(l => l.Recipe.Level).ThenBy(l => l.Item.Name.In(texts.Lang), StringComparer.Ordinal)],
        };
    }

    /// <summary>
    /// A gathering trade's resources, one every 5 levels up to 100, where each grows ("Bois de Clairval, near the
    /// way in"), "soon" past those in the game; a making trade's ingredients, with where they come from.
    /// </summary>
    public IReadOnlyList<ResourceLine> Resources(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (!job.Gathers)
        {
            return [.. job.Recipes.SelectMany(r => r.Ingredients).Select(i => i.Item).Distinct()
                .Select(id => Data.Items[id]).Select(i => new ResourceLine(i, Shop.Level(i, Data), From(i), Have(i.Id)))];
        }
        return [.. Enumerable.Range(0, Crafting.MaxLevel / Step).Select(k => Math.Max(1, k * Step)).Select(level =>
            job.Harvests!.FirstOrDefault(h => h.Level == level) is Harvest h
                ? new ResourceLine(Data.Items[h.Item], level, Where(h), Have(h.Item))
                : new ResourceLine(null, level, texts["jobs.soon"], 0))];
    }

    /// <summary>Where a gathered resource grows: its zone, near the way in or at the far end.</summary>
    private string Where(Harvest harvest)
    {
        foreach ((string zone, (int near, int far)) in Harvesting.Tiers)
        {
            // The fields have no water: their fish grow deep in the lake.
            if (harvest.Node == NodeKind.Water && zone == "fields")
                continue;
            string name = Data.Towns.Values.FirstOrDefault(t => t.Zone == zone)?.Name.In(texts.Lang) ?? zone;
            if (harvest.Level == near || harvest.Level == far)
                return texts[harvest.Level == near ? "jobs.near" : "jobs.far", name];
        }
        // The lake's deep fish (sprint 99).
        return texts["jobs.deep", Data.Towns.Values.FirstOrDefault(t => t.Zone == "lake")?.Name.In(texts.Lang) ?? "lake"];
    }

    /// <summary>Where an ingredient comes from: a gathering trade, monsters, a trade's recipe.</summary>
    private string From(Item item)
    {
        if (Data.Jobs.Values.FirstOrDefault(j => j.Harvests?.Any(h => h.Item == item.Id) == true) is Job gatherer)
            return gatherer.Name.In(texts.Lang);
        if (Data.Families.Values.FirstOrDefault(f => f.Monsters.Any(m => (m.Loot ?? []).Any(l => l.Item == item.Id))) is MonsterFamily family)
            return family.Name.In(texts.Lang);
        return texts["jobs.made"];
    }
}
