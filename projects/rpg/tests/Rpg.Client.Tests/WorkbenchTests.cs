using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The locksmith's bench (sprint 81): recipes by level, what is owned, what can be made.</summary>
public class WorkbenchTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly Job Locksmith = Data.Jobs["locksmith"];

    private static CharacterSummary With(long xp, params ItemCount[] owned) =>
        new(Guid.NewGuid(), "Serrure", "male-c", "guard", 0, DateTimeOffset.UnixEpoch, Inventory: owned, Jobs: new Dictionary<string, long> { ["locksmith"] = xp });

    [Fact]
    public void WalkingOntoTheBench_OpensIt_AndClosingIt_LetsThePlayerWalkOn()
    {
        var town = new TownController(Data, "clairval");
        TownStep step = town.Click(new Cell(11, 9))!;
        Assert.NotNull(step);
        Assert.Equal(new Workshop(new Cell(11, 9), "locksmith"), town.Bench);
        Assert.Null(town.Click(town.Town.Spawn));
        town.CloseDoor();
        Assert.Null(town.Bench);
        Assert.NotNull(town.Click(town.Town.Spawn));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheRecipes_ComeByLevel_EachWithItsIngredientsAndWhatCanBeMade(string lang)
    {
        var texts = new Texts(lang);
        var bench = new Workbench(Locksmith, With(30, new("boar-tusk", 13), new("boar-bristle", 9), new("frog-leg", 60), new("slime", 40)), texts);
        Assert.Equal(2, bench.Level);
        Assert.Equal(texts["craft.title", Locksmith.Name.In(lang), 2], bench.Title);
        Assert.Equal(texts["craft.xp", 30, 60], bench.XpText);
        IReadOnlyList<RecipeRow> rows = bench.Rows;
        Assert.Equal(Locksmith.Recipes.Count, rows.Count);
        Assert.Equal(rows.Select(r => r.Recipe.Level).Order(), rows.Select(r => r.Recipe.Level));
        RecipeRow boar = rows.Single(r => r.Recipe.Item == "boar-wallow-key");
        Assert.Equal((2, (string?)null), (boar.Most, boar.Refusal));
        Assert.Equal([$"{Data.Items["boar-tusk"].Name.In(lang)} 13/6", $"{Data.Items["boar-bristle"].Name.In(lang)} 9/4"], boar.Ingredients);
        // Ingredients enough, level not.
        RecipeRow frog = rows.Single(r => r.Recipe.Item == "frog-grotto-key");
        Assert.Equal((0, texts["craft.level", 20]), (frog.Most, frog.Refusal));
        RecipeRow wolf = rows.Single(r => r.Recipe.Item == "wolf-den-key");
        Assert.Equal((0, texts["craft.missing"]), (wolf.Most, wolf.Refusal));
        // Without a character, nothing is made.
        Assert.All(new Workbench(Locksmith, null, texts).Rows, r => Assert.Equal(texts["craft.offline"], r.Refusal));
        // At most twenty at once.
        Assert.Equal(Crafting.MostAtOnce, new Workbench(Locksmith, With(0, new("boar-tusk", 600), new("boar-bristle", 400)), texts).Rows.Single(r => r.Recipe.Item == "boar-wallow-key").Most);
    }
}
