using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Forest of the Ancestors (sprint 131, D79, docs/suite-apres-d69.md §11.6): 25 maps south of Quatre-Chemins, open on
/// its road house by the south road, its own decor, the gathered resources of levels 40 and 45, a waystone at its way in.
/// </summary>
public class ForestTests
{
    private static readonly int[] Levels = [40, 45];

    private static Town[] World => [.. Real.Towns.Values];

    private static Town[] Forest => [.. Real.Towns.Values.Where(t => t.Zone == "ancestors")];

    [Fact]
    public void TheForest_IsFiveByFiveMaps_SouthOfQuatreChemins_AtLevelThirtyOne()
    {
        Assert.Equal(25, Forest.Length);
        Assert.All(Forest, t => Assert.Equal((48, 32, 31, "Forêt des Aïeux", "Forest of the Ancestors"), (t.Width, t.Rows.Count, t.Level, t.Name.Fr, t.Name.En)));
        Assert.Equal([.. Enumerable.Range(4, 5).SelectMany(x => Enumerable.Range(1, 5).Select(y => new Cell(x, y)))],
            Forest.Select(t => t.MapAt).OrderBy(c => c.X).ThenBy(c => c.Y));
        // No monsters before its families come (sprints 132 and 133).
        Assert.All(Forest, t => Assert.Empty(Real.GroupsOn(t.Id)));
    }

    [Fact]
    public void TheSouthRoad_GoesThroughTheRoadHousesPalisade_IntoTheForest_AndBack()
    {
        Town house = Real.Towns["four-ways-1-1"], entry = Real.Towns["ancestors-1-0"];
        ZoneLink[] down = [.. house.Links!.Where(l => l.To == entry.Id)];
        ZoneLink[] up = [.. entry.Links!.Where(l => l.To == house.Id)];
        Assert.Equal([23, 24, 25], down.Select(l => l.At.X));
        Assert.All(down, l => Assert.Equal((house.Rows.Count - 1, 1), (l.At.Y, l.Arrival.Y)));
        Assert.Equal([23, 24, 25], up.Select(l => l.At.X));
        Assert.All(up, l => Assert.Equal((0, house.Rows.Count - 2), (l.At.Y, l.Arrival.Y)));
        Assert.All(down, l => Assert.Equal("Forêt des Aïeux", l.Name!.Fr));
        Assert.All(up, l => Assert.Equal("Four Ways", l.Name!.En));
        // The only ways between the forest and the rest of the world.
        Assert.Equal(6, Real.Towns.Values.SelectMany(t => (t.Links ?? []).Select(l => (From: t, l.To)))
            .Count(w => (w.From.Zone == "ancestors") != (Real.Towns[w.To].Zone == "ancestors")));
    }

    [Fact]
    public void TheForest_IsBehindClairvalsEastGate_ItsMapsNeedFourTrails()
    {
        Assert.All(Forest, t => Assert.Equal("four-trails", Real.QuestToReach(t.Id)));
    }

    [Fact]
    public void TheForest_HasItsOwnDecor_TightStandsOfTrees_MossyBoulders_FernsAndSprings()
    {
        // Not the woods' scattered clumps: fewer trees, in bigger stands.
        double Trees(string zone) => Real.Towns.Values.Where(t => t.Zone == zone).Average(t => t.Rows.Sum(r => r.Count(c => c == 'T')));
        Assert.True(Trees("ancestors") < Trees("woods"), $"{Trees("ancestors")} trees a map, the woods {Trees("woods")}");
        Assert.All(Forest, t => Assert.Contains(t.Rows, r => r.Contains('R')));
        Assert.All(Forest, t => Assert.Contains(t.Rows, r => r.Contains('B')));
        Assert.InRange(Forest.Count(t => t.Rows.Any(r => r.Contains('~'))), 10, 24);
    }

    [Fact]
    public void TheForestsSpots_CarryLevelForty_NearTheWayIn_AndFortyFive_AtTheFarEnd()
    {
        HarvestNode[] spots = [.. Real.Nodes.Values.Where(n => Real.Towns[n.Map].Zone == "ancestors")];
        Assert.All(spots, n => Assert.Contains(n.Level, Levels));
        foreach (Town t in Forest)
        {
            int depth = MonsterGroups.Depth(t, World);
            HarvestNode[] here = [.. spots.Where(n => n.Map == t.Id)];
            if (depth == 0)
                Assert.All(here, n => Assert.Equal(40, n.Level));
            if (depth == 2)
                Assert.All(here, n => Assert.Equal(45, n.Level));
        }
        // The way in is the road house's map: the entry map is near, the south corners far.
        Assert.Equal(0, MonsterGroups.Depth(Real.Towns["ancestors-1-0"], World));
        Assert.Equal(2, MonsterGroups.Depth(Real.Towns["ancestors-4-4"], World));
        // Every one of the ten resources of the five gathering trades grows there.
        string[] ten = [.. Real.Jobs.Values.Where(j => j.Gathers).SelectMany(j => j.Harvests!).Where(h => h.Level >= 40).Select(h => h.Item)];
        Assert.Equal(10, ten.Length);
        Assert.Equal(ten.Order(), spots.Select(n => n.Item).Distinct().Order());
    }

    [Fact]
    public void TheForest_HasAWaystoneAtItsWayIn_ForSeventyTwoÉclats()
    {
        (string id, string map, Cell at) = Waystones.All(Real).Single(w => Real.Towns[w.Map].Zone == "ancestors");
        Assert.Equal(("waystone-ancestors", "ancestors-1-0"), (id, map));
        Assert.InRange(at.DistanceTo(Real.Towns[map].Spawn), 1, 2);
        Assert.Equal(72, Waystones.Cost(id, Real));
    }
}
