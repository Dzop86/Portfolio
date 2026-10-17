using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The trades (sprint 81): the locksmith's recipes, levels, experience, and the keys left by monsters.</summary>
public class JobTests
{
    [Fact]
    public void TheLocksmith_MakesEveryDungeonsKey_FromItsFamilysResources_AtTheZonesLevel()
    {
        Job locksmith = Real.Jobs["locksmith"];
        // The keys; the tutorial's palisade stake went to the forge in sprint 109.
        Assert.Equal(Real.Dungeons.Values.Select(d => d.Key).Order(), locksmith.Recipes.Select(r => r.Item).Order());
        Assert.Equal([new ItemCount("oak-log", 3), new ItemCount("copper-ore", 2)], Real.Jobs["smith"].RecipeFor("palisade-stake")!.Ingredients);
        foreach (Dungeon d in Real.Dungeons.Values)
        {
            Recipe recipe = locksmith.RecipeFor(d.Key)!;
            MonsterFamily family = Real.Families[d.Family];
            HashSet<string> left = [.. family.Monsters.SelectMany(m => m.Loot ?? []).Select(l => l.Item)];
            Assert.All(recipe.Ingredients, i => Assert.Contains(i.Item, left));
            Assert.All(recipe.Ingredients, i => Assert.Equal(ItemKind.Resource, Real.Items[i.Item].Kind));
        }
        // The easier the zone, the lower the recipe.
        Assert.Equal(1, locksmith.RecipeFor("boar-wallow-key")!.Level);
        Assert.True(locksmith.RecipeFor("royal-hive-key")!.Level > locksmith.RecipeFor("crab-reef-key")!.Level);
        // Its bench is on Clairval's craftsmen's row (sprint 102), since sprint 104 by Gédéon's door and off the road (Charles, D75).
        Town row = Real.Towns["clairval-3-1"];
        Workshop bench = Assert.Single(row.Workshops!, w => w.Job == "locksmith");
        Assert.NotEqual('=', row[bench.At]);
        Assert.Contains(bench.At.Neighbours(), c => row[c] is 'H' or 'h');
        Assert.True(row.Npcs.Single(n => n.Id == "gedeon").At.DistanceTo(bench.At) <= 3);
    }

    [Fact]
    public void ATradesLevel_FollowsItsExperience_AndHarderRecipesGiveMore()
    {
        Assert.Equal((1, 1, 2, 4, 10, 100), (Crafting.LevelFor(0), Crafting.LevelFor(19), Crafting.LevelFor(20), Crafting.LevelFor(120), Crafting.LevelFor(900), Crafting.LevelFor(long.MaxValue)));
        Assert.Equal(0, Crafting.XpToReach(1));
        Recipe boar = Real.Jobs["locksmith"].RecipeFor("boar-wallow-key")!, bee = Real.Jobs["locksmith"].RecipeFor("royal-hive-key")!;
        Item key = Real.Items[boar.Item];
        Assert.Equal(25, Crafting.Xp(boar, key));
        Assert.True(Crafting.Xp(bee, Real.Items[bee.Item]) > Crafting.Xp(boar, key));
        // The second zone's keys are reached by making the first's about thirty-six times.
        Assert.InRange(Crafting.XpToReach(10) / Crafting.Xp(boar, key), 30, 40);
    }

    [Fact]
    public void ARecipe_NeedsItsLevel_AndItsIngredients_ForEachItemMade()
    {
        Recipe recipe = Real.Jobs["locksmith"].RecipeFor("boar-wallow-key")!;
        ItemCount[] enough = [new("boar-tusk", 12), new("boar-bristle", 8)];
        Assert.Null(Crafting.Problem(recipe, 1, enough));
        Assert.Null(Crafting.Problem(recipe, 1, enough, 2));
        Assert.NotNull(Crafting.Problem(recipe, 1, enough, 3));
        Assert.NotNull(Crafting.Problem(recipe, 1, [new("boar-tusk", 12)]));
        Assert.NotNull(Crafting.Problem(recipe, 1, enough, 0));
        Assert.NotNull(Crafting.Problem(recipe, 1, enough, Crafting.MostAtOnce + 1));
        Assert.Equal(2, Crafting.MostFrom(recipe, enough));
        Assert.Equal(0, Crafting.MostFrom(recipe, []));
        Recipe hive = Real.Jobs["locksmith"].RecipeFor("royal-hive-key")!;
        Assert.NotNull(Crafting.Problem(hive, hive.Level - 1, [.. hive.Ingredients]));
        Assert.Null(Crafting.Problem(hive, hive.Level, [.. hive.Ingredients]));
    }

    [Fact]
    public void EveryMonster_LeavesItsDungeonsKey_Rarely_TheChiefLessRarely()
    {
        foreach (Dungeon d in Real.Dungeons.Values)
        {
            foreach (MonsterSpec m in Real.Families[d.Family].Monsters)
                Assert.Equal(m.Role == MonsterRole.Boss ? 10 : 2, Assert.Single(m.Loot!, l => l.Item == d.Key).Percent);
        }
    }

    [Fact]
    public void ABadRecipe_OrAWorkshopOfNoTrade_IsRefused()
    {
        Job locksmith = Real.Jobs["locksmith"];
        GameData With(Job j, IEnumerable<Town>? towns = null) => new(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes, towns ?? Real.Towns.Values,
            Real.Dialogues.Values, Real.Summons.Values, Real.Quests.Values, Real.Items.Values, Real.Sets.Values, Real.Families.Values, Real.Dungeons.Values, [.. Real.Jobs.Values.Where(x => x.Id != j.Id), j], Real.Passives.Values);
        Assert.Equal(locksmith, With(locksmith).Jobs["locksmith"]);
        Recipe r = locksmith.Recipes[0];
        Assert.Throws<InvalidDataException>(() => With(locksmith with { Recipes = [r with { Item = "nothing" }] }));
        Assert.Throws<InvalidDataException>(() => With(locksmith with { Recipes = [r, r] }));
        Assert.Throws<InvalidDataException>(() => With(locksmith with { Recipes = [r with { Level = 0 }] }));
        Assert.Throws<InvalidDataException>(() => With(locksmith with { Recipes = [r with { Ingredients = [new ItemCount("nothing", 1)] }] }));
        Assert.Throws<InvalidDataException>(() => With(locksmith with { Recipes = [r with { Ingredients = [new ItemCount(r.Item, 1)] }] }));
        Assert.Throws<InvalidDataException>(() => With(locksmith with { Recipes = [] }));
        Town quarter = Real.Towns["clairval-3-1"];
        Assert.Throws<InvalidDataException>(() => With(locksmith, [.. Real.Towns.Values.Where(t => t.Id != quarter.Id), quarter with { Workshops = [new Workshop(new Cell(24, 12), "baker")] }]));
        Assert.Throws<InvalidDataException>(() => With(locksmith, [.. Real.Towns.Values.Where(t => t.Id != quarter.Id), quarter with { Workshops = [new Workshop(new Cell(47, 0), "locksmith")] }]));
    }
}
