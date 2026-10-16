using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The gathering trades and their spots (sprint 99, D71 and D72, docs/suite-apres-d69.md §5.3 and §5.4).</summary>
public class HarvestTests
{
    [Fact]
    public void FiveGatheringTrades_EachANewResourceEveryFiveLevels_UpTo35()
    {
        Job[] gathering = [.. Real.Jobs.Values.Where(j => j.Gathers)];
        Assert.Equal(["woodcutter", "miner", "fisher", "farmer", "herbalist"], gathering.Select(j => j.Id));
        Assert.All(gathering, j => Assert.Equal([1, 5, 10, 15, 20, 25, 30, 35], j.Harvests!.Select(h => h.Level)));
        Assert.All(gathering, j => Assert.Single(j.Harvests!.Select(h => h.Node).Distinct()));
        Assert.Equal(40, gathering.SelectMany(j => j.Harvests!).Select(h => h.Item).Distinct().Count());
        Assert.All(gathering.SelectMany(j => j.Harvests!), h => Assert.Equal(h.Level, Real.Items[h.Item].Level));
    }

    [Fact]
    public void EveryMapOfAZone_HasSpots_OnTheRightDecor_AwayFromTheWays_TheVillagesOfTheFirstLevel()
    {
        // Since sprint 102, the village has a few spots, of the first level only.
        Assert.All(Real.Nodes.Values.Where(n => Shop.InVillage(n.Map)), n => Assert.Equal(1, n.Level));
        Assert.Contains(Real.Nodes.Values, n => Shop.InVillage(n.Map));
        foreach (Town map in Real.Towns.Values.Where(t => t.Zone is string z && Harvesting.Tiers.ContainsKey(z) && !Shop.InVillage(t.Id)))
        {
            HarvestNode[] here = [.. Real.Nodes.Values.Where(n => n.Map == map.Id)];
            Assert.InRange(here.Length, 5, 12);
            foreach (HarvestNode n in here)
            {
                char decor = map.Rows[n.At.Y][n.At.X];
                Assert.Contains(decor, n.Kind switch { NodeKind.Tree => "T.", NodeKind.Rock => "R.", NodeKind.Water => "~", NodeKind.Plot => ".", _ => ".M," });
                Assert.True(map.Spawn.DistanceTo(n.At) >= Harvesting.Distance);
                Assert.Equal(Real.Jobs[n.Job].Harvests!.Single(h => h.Item == n.Item).Level, n.Level);
            }
            // Each resource of the zone's two levels, the lake's deep fish aside.
            (int near, int far) = Harvesting.Tiers[map.Zone!];
            Assert.All(here.Where(n => !(map.Zone == "lake" && n.Kind == NodeKind.Water && n.Level >= 30)), n => Assert.Contains(n.Level, new[] { near, far }));
        }
        // Every one of the 40 resources grows somewhere.
        Assert.Equal(40, Real.Nodes.Values.Select(n => n.Item).Distinct().Count());
    }

    [Fact]
    public void AGathering_GivesMoreWithTheTradesLead_ItsExperienceStopsTwentyLevelsBelow_AndGrowsBack()
    {
        Assert.Equal((1, 3, 3), (Harvesting.Quantity(1, 1), Harvesting.Quantity(41, 1), Harvesting.Quantity(99, 1)));
        Assert.Equal(2, Harvesting.Quantity(30, 10));
        Assert.Equal((30L, 75L, 0L), (Harvesting.Xp(1, 1), Harvesting.Xp(10, 10), Harvesting.Xp(21, 1)));
        Assert.Equal((TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5)), (Harvesting.Regrowth(1), Harvesting.Regrowth(35)));
        HarvestNode node = Real.Nodes.Values.First(n => n.Level == 5);
        Cell next = node.At.Neighbours().First();
        Assert.Null(Harvesting.WhyNot(node, 5, node.Map, next));
        Assert.NotNull(Harvesting.WhyNot(node, 4, node.Map, next));
        Assert.NotNull(Harvesting.WhyNot(node, 5, "clairval", next));
        Assert.NotNull(Harvesting.WhyNot(node, 5, node.Map, new Cell(node.At.X + 3, node.At.Y)));
        Assert.Null(Harvesting.WhyNot(node, 5, node.Map, new Cell(node.At.X + 1, node.At.Y + 1)));
    }
}
