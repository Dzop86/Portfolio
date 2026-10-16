using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The trades window (sprint 100, D72): the trades and their levels, recipes filtered and sorted, resources and where they grow.</summary>
public class JobBookTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly Job Locksmith = Data.Jobs["locksmith"], Woodcutter = Data.Jobs["woodcutter"];

    private static CharacterSummary With(long locksmith, long shards, params ItemCount[] owned) =>
        new(Guid.NewGuid(), "Serrure", "male-c", "guard", 0, DateTimeOffset.UnixEpoch, Inventory: owned, Shards: shards,
            Jobs: new Dictionary<string, long> { ["locksmith"] = locksmith, ["woodcutter"] = 30 });

    [Fact]
    public void TheTrades_GatheringThenMaking_WithTheirLevelAndTheExperienceInTheLevel()
    {
        var book = new JobBook(Data, With(30, 0), new Texts("fr"));
        Assert.Equal(["woodcutter", "miner", "fisher", "farmer", "herbalist", "locksmith"], book.Jobs.Select(j => j.Job.Id));
        JobLine wood = book.Jobs[0];
        // 30 experience: level 2 (20 needed), 10 into it of the 40 to level 3.
        Assert.Equal((2, 10L, 40L, true, true), (wood.Level, wood.XpIn, wood.XpNeeded, wood.Gathers, wood.Practised));
        Assert.False(book.Jobs.Single(j => j.Job.Id == "miner").Practised);
    }

    [Fact]
    public void TheRecipes_AreSearchedFilteredAndSorted_AndMadeOnlyAtTheWorkshop()
    {
        CharacterSummary c = With(30, 100, new ItemCount("boar-tusk", 13), new ItemCount("boar-bristle", 9));
        var look = new JobBook(Data, c, new Texts("fr"));
        Assert.Equal(Locksmith.Recipes.Count, look.Recipes(Locksmith).Count);
        Assert.Equal(look.Recipes(Locksmith).Select(r => r.Recipe.Level).OrderDescending(), look.Recipes(Locksmith).Select(r => r.Recipe.Level));
        Assert.All(look.Recipes(Locksmith), r => Assert.False(r.CanMake));
        var bench = new JobBook(Data, c, new Texts("fr"), "locksmith");
        RecipeLine wallow = bench.Recipes(Locksmith).Single(r => r.Item.Id == "boar-wallow-key");
        Assert.True(wallow.CanMake);
        Assert.Equal(2, wallow.Most);
        Assert.All(wallow.Ingredients, i => Assert.False(i.Missing));
        Assert.True(bench.Recipes(Locksmith).Single(r => r.Item.Id == "royal-hive-key").TooHigh);
        bench.PossibleOnly = true;
        Assert.Equal(["boar-wallow-key"], bench.Recipes(Locksmith).Select(r => r.Item.Id));
        (bench.PossibleOnly, bench.MinLevel, bench.MaxLevel, bench.Order) = (false, 10, 20, RecipeOrder.Lowest);
        Assert.Equal([10, 10, 20, 20], bench.Recipes(Locksmith).Select(r => r.Recipe.Level));
        (bench.MinLevel, bench.MaxLevel, bench.Search) = (1, 100, "ruche");
        Assert.Equal(["royal-hive-key"], bench.Recipes(Locksmith).Select(r => r.Item.Id));
        Assert.Equal(["Resource"], bench.Kinds(Locksmith));
    }

    [Fact]
    public void AGatheringTrade_ShowsItsTwentyResources_WhereTheyGrow_TheLaterOnesSoon()
    {
        var fr = new Texts("fr");
        IReadOnlyList<ResourceLine> lines = new JobBook(Data, With(0, 0, new ItemCount("oak-log", 4)), fr).Resources(Woodcutter);
        Assert.Equal(20, lines.Count);
        Assert.Equal([1, 5, 10, 15, 20, 25, 30, 35, 40], lines.Take(9).Select(l => l.Level));
        Assert.Equal(("oak-log", "Bois de Clairval, près de l'entrée", 4), (lines[0].Item!.Id, lines[0].Where, lines[0].Owned));
        Assert.Equal("Bois de Clairval, au fond", lines[1].Where);
        Assert.Null(lines[8].Item);
        Assert.Equal("bientôt", lines[8].Where);
        // The deep fish: in the lake, the fields having no water.
        Assert.Equal("Lac des Joncs, en eau profonde", new JobBook(Data, null, fr).Resources(Data.Jobs["fisher"])[6].Where);
        // A making trade: its ingredients and where they come from.
        Assert.Contains(new JobBook(Data, null, fr).Resources(Locksmith), r => r.Item!.Id == "boar-tusk" && r.Where == "Sangliers");
    }

    [Fact]
    public void ATradesNewLevel_IsSeen_FromWhatItWasToWhatTheServerSends()
    {
        // Charles, sprint 103: a message when a trade goes up a level.
        var before = new CharacterSummary(Guid.NewGuid(), "Élise", "female-c", "mage", 0, DateTimeOffset.UnixEpoch, Jobs: new Dictionary<string, long> { ["woodcutter"] = 0 });
        long next = Enumerable.Range(1, 1000).First(xp => Crafting.LevelFor(xp) == 2);
        CharacterSummary after = before with { Jobs = new Dictionary<string, long> { ["woodcutter"] = next, ["miner"] = 1 } };
        Assert.Equal([("woodcutter", 2)], JobBook.LevelsGained(before, after));
        Assert.Empty(JobBook.LevelsGained(after, after));
        Assert.Empty(JobBook.LevelsGained(null, after));
        Assert.Empty(JobBook.LevelsGained(before with { Id = Guid.NewGuid() }, after));
        foreach (string key in new[] { "job.levelup", "inventory.shards" })
            Assert.True(Texts.Fr.ContainsKey(key) && Texts.En.ContainsKey(key), key);
    }
}
