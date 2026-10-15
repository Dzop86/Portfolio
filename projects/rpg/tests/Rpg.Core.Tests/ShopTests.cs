using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>Rose's stall and what the Éclats are spent on (sprint 97, D71, docs/suite-apres-d69.md §4).</summary>
public class ShopTests
{
    [Fact]
    public void Rose_PaysForResourcesByTheirLevel_AndForEquipmentTwiceItsLevel_SellingFiveTimesDearer()
    {
        Item bristle = Real.Items["boar-bristle"], comb = Real.Items["honeycomb"], fang = Real.Items["leader-fang"];
        Assert.Equal((1, 1L), (Shop.Level(bristle, Real), Shop.BuyPrice(bristle, Real)));
        Assert.Equal(3L, Shop.BuyPrice(comb, Real));
        Assert.Equal(15L, Shop.SellPrice(comb, Real));
        Assert.Equal(2L * fang.Level, Shop.BuyPrice(fang, Real));
        // She sells the monsters' resources, not their equipment, the dungeons' keys or quest items.
        Assert.Null(Shop.SellPrice(fang, Real));
        Assert.Null(Shop.SellPrice(Real.Items["boar-wallow-key"], Real));
        Assert.Null(Shop.BuyPrice(Real.Items["admin-keyring"], Real));
        // Resources, and since sprint 101 food.
        Assert.All(Shop.Stock(Real), i => Assert.True(i.Kind == ItemKind.Resource || i.Heal > 0, i.Id));
        Assert.Contains(bristle, Shop.Stock(Real));
    }

    [Fact]
    public void ADeal_IsInTheVillage_WithTheÉclats_AndNeverWhatIsWorn()
    {
        Item bristle = Real.Items["boar-bristle"], ring = Real.Items["acorn-ring"];
        Assert.Null(Shop.WhyNotBuy(bristle, 3, 15, Shop.Village, Real));
        Assert.Contains("Éclats", Shop.WhyNotBuy(bristle, 4, 15, Shop.Village, Real), StringComparison.Ordinal);
        Assert.Contains("village", Shop.WhyNotBuy(bristle, 1, 99, "woods-0-0", Real), StringComparison.Ordinal);
        Assert.NotNull(Shop.WhyNotBuy(bristle, 0, 99, Shop.Village, Real));
        ItemCount[] owned = [new("acorn-ring", 2)];
        var worn = new Dictionary<Slot, string> { [Slot.Ring1] = "acorn-ring" };
        Assert.Null(Shop.WhyNotSell(ring, 1, owned, worn, Shop.Village, Real));
        Assert.NotNull(Shop.WhyNotSell(ring, 2, owned, worn, Shop.Village, Real));
        Assert.NotNull(Shop.WhyNotSell(Real.Items["garance-badge"], 1, [new("garance-badge", 1)], null, Shop.Village, Real));
    }

    [Fact]
    public void TakingPointsBack_IsFreeUpToLevelTen_ThenTenÉclatsALevel()
    {
        Assert.Equal(0, Shop.ResetCost(10));
        Assert.Equal(110, Shop.ResetCost(11));
        Assert.Equal(1000, Shop.ResetCost(100));
        Assert.True(Shop.TakesBack(new Characteristics(10, 5), new Characteristics(9, 6)));
        Assert.False(Shop.TakesBack(new Characteristics(10, 5), new Characteristics(10, 9)));
    }

    [Fact]
    public void AKey_CostsFiveÉclatsALevelOfItsChief_AsWellAsItsIngredients()
    {
        foreach (Dungeon d in Real.Dungeons.Values)
        {
            Recipe recipe = Real.Jobs["locksmith"].RecipeFor(d.Key)!;
            Assert.Equal(5L * Real.Families[d.Family][MonsterRole.Boss].Level, recipe.Shards);
        }
        Recipe wallow = Real.Jobs["locksmith"].RecipeFor("boar-wallow-key")!;
        ItemCount[] enough = [.. wallow.Ingredients];
        Assert.Contains("Éclats", Crafting.Problem(wallow, 1, enough, 1, wallow.Shards - 1), StringComparison.Ordinal);
        Assert.Null(Crafting.Problem(wallow, 1, enough, 1, wallow.Shards));
        Assert.Null(Crafting.Problem(wallow, 1, enough));
    }
}
