using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The recall scroll and the waystones (sprint 127, D79).</summary>
public class WaystoneTests
{
    [Fact]
    public void AWaystone_StandsInTheVillage_AndAtEachZonesWayIn_NearItsArrival()
    {
        var all = Waystones.All(Real);
        Assert.Equal(["waystone-clairval", "waystone-fields", "waystone-four-ways", "waystone-lake", "waystone-mouldy", "waystone-woods"], all.Select(w => w.Id).Order(StringComparer.Ordinal));
        // Within two cells of the arrival, where no group of monsters stands: no group moved for them.
        Assert.All(all, w => Assert.InRange(w.At.DistanceTo(Real.Towns[w.Map].Spawn), 1, 2));
        Assert.All(all, w => Assert.True(Real.Towns[Waystones.Arrival(w.Id, Real).Town].CanStand(Waystones.Arrival(w.Id, Real).Cell), w.Id));
        Assert.Contains(all, w => w.Map == Shop.Village);
    }

    [Fact]
    public void PassingBy_DiscoversAWaystone_AndTravellingCostsTenAndTwoALevel()
    {
        (string id, string map, Cell at) = Waystones.All(Real).First(w => w.Id == "waystone-lake");
        Assert.Equal([id], Waystones.Near(map, at + new Cell(Waystones.Discovery, 0), Real));
        Assert.Empty(Waystones.Near(map, at + new Cell(Waystones.Discovery + 1, 0), Real));
        Assert.Equal(10 + 2 * Real.Towns[map].Level, Waystones.Cost(id, Real));
        Assert.Equal(12, Waystones.Cost(Waystones.Village, Real));
    }

    [Fact]
    public void Travelling_NeedsTheStoneKnown_OutOfAFightAndADungeon_AndTheÉclats()
    {
        string[] known = ["waystone-clairval", "waystone-lake"];
        long cost = Waystones.Cost("waystone-lake", Real);
        Assert.Null(Waystones.WhyNotTravel("waystone-lake", known, Shop.Village, false, false, cost, Real));
        Assert.NotNull(Waystones.WhyNotTravel("waystone-fields", known, Shop.Village, false, false, 1000, Real));
        Assert.NotNull(Waystones.WhyNotTravel("waystone-lake", known, Shop.Village, true, false, 1000, Real));
        Assert.NotNull(Waystones.WhyNotTravel("waystone-lake", known, Shop.Village, false, true, 1000, Real));
        Assert.NotNull(Waystones.WhyNotTravel("waystone-lake", known, Shop.Village, false, false, cost - 1, Real));
        Assert.NotNull(Waystones.WhyNotTravel("waystone-clairval", known, Shop.Village, false, false, 1000, Real));
        Assert.NotNull(Waystones.WhyNotTravel("nowhere", known, Shop.Village, false, false, 1000, Real));
    }

    [Fact]
    public void TheRecallScroll_TakesOneHome_TheVillagesByDefault()
    {
        Assert.Equal(Shop.Village, Waystones.Home(null, Real).Town);
        Assert.Equal("lake-3-4", Waystones.Home("waystone-lake", Real).Town);
        Assert.Equal(Shop.Village, Waystones.Home("nowhere", Real).Town);
        Assert.Null(Waystones.WhyNotRecall(1, false, false));
        Assert.NotNull(Waystones.WhyNotRecall(0, false, false));
        Assert.NotNull(Waystones.WhyNotRecall(1, true, false));
        Assert.NotNull(Waystones.WhyNotRecall(1, false, true));
    }
}
