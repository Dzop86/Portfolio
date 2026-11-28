using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// Wellspring Vale (sprint 233, D103, docs/zone-61-75.md): the zone of levels 61 to 75, 25 maps on the far side of the
/// mountain, south of the Underroot Mines, open on their far end by one map; a vale of springs and terraces, the gathered
/// resources of levels 65 and 70, a waystone at its way in. Its families come with sprints 234 and 235.
/// </summary>
public class WellspringTests
{
    private static readonly int[] Levels = [65, 70];

    private static Town[] World => [.. Real.Towns.Values];

    private static Town[] Vale => [.. Real.Towns.Values.Where(t => t.Zone == "wellspring")];

    [Fact]
    public void TheVale_IsFiveByFiveMaps_SouthOfTheMines_AtLevelSixtyOne()
    {
        Assert.Equal(25, Vale.Length);
        Assert.All(Vale, t => Assert.Equal((48, 32, 61, "Val des Sources", "Wellspring Vale"), (t.Width, t.Rows.Count, t.Level, t.Name.Fr, t.Name.En)));
        Assert.Equal([.. Enumerable.Range(4, 5).SelectMany(x => Enumerable.Range(11, 5).Select(y => new Cell(x, y)))],
            Vale.Select(t => t.MapAt).OrderBy(c => c.X).ThenBy(c => c.Y));
        Assert.Equal(World.Count(t => t.Dungeon is null && t.Zone is not null), World.Where(t => t.Dungeon is null && t.Zone is not null).Select(t => t.MapAt).Distinct().Count());
    }

    [Fact]
    public void TheWayThrough_LeavesTheMinesFarEnd_IntoTheVale_AndBack()
    {
        Town mines = Real.Towns["underroot-2-4"], entry = Real.Towns["wellspring-2-0"];
        ZoneLink[] down = [.. mines.Links!.Where(l => l.To == entry.Id)];
        ZoneLink[] up = [.. entry.Links!.Where(l => l.To == mines.Id)];
        Assert.Equal([23, 24, 25], down.Select(l => l.At.X));
        Assert.All(down, l => Assert.Equal((mines.Rows.Count - 1, 1, l.At.X), (l.At.Y, l.Arrival.Y, l.Arrival.X)));
        Assert.Equal([23, 24, 25], up.Select(l => l.At.X));
        Assert.All(up, l => Assert.Equal((0, entry.Rows.Count - 2, l.At.X), (l.At.Y, l.Arrival.Y, l.Arrival.X)));
        Assert.All(down, l => Assert.Equal(("Val des Sources", "Wellspring Vale"), (l.Name!.Fr, l.Name.En)));
        Assert.All(up, l => Assert.Equal("Underroot Mines", l.Name!.En));
        Assert.All(down, l => Assert.True(mines.CanStand(l.At), $"{l.At} is not walkable on the mines' side"));
        Assert.All(up, l => Assert.True(entry.CanStand(l.At), $"{l.At} is not walkable on the vale's side"));
        Assert.All(down.Select(l => l.Arrival), a => Assert.True(entry.CanStand(a)));
        Assert.All(up.Select(l => l.Arrival), a => Assert.True(mines.CanStand(a)));
        // The only ways between the vale and the rest of the world.
        Assert.Equal(6, World.Where(t => t.Dungeon is null).SelectMany(t => (t.Links ?? []).Select(l => (From: t, l.To)))
            .Count(w => (w.From.Zone == "wellspring") != (Real.Towns[w.To].Zone == "wellspring") && Real.Towns[w.To].Dungeon is null));
    }

    [Fact]
    public void EveryWayOfTheVale_LeadsToAWalkableCell_AndEveryMapIsReachedFromTheMines()
    {
        foreach (Town t in Vale)
            foreach (ZoneLink l in t.Links ?? [])
            {
                Assert.True(t.CanStand(l.At), $"{t.Id}: the way at {l.At} is not walkable");
                Assert.True(Real.Towns[l.To].CanStand(l.Arrival), $"{t.Id}: the way at {l.At} arrives on a blocked cell of {l.To}");
            }
        HashSet<string> seen = ["wellspring-2-0"];
        var todo = new Queue<string>(seen);
        while (todo.TryDequeue(out string? id))
            foreach (ZoneLink l in Real.Towns[id].Links ?? [])
                if (Real.Towns[l.To].Zone == "wellspring" && seen.Add(l.To))
                    todo.Enqueue(l.To);
        Assert.Equal(25, seen.Count);
    }

    [Fact]
    public void TheVale_IsBehindClairvalsEastGate_ItsMapsNeedFourTrails()
    {
        Assert.All(Vale, t => Assert.Equal("four-trails", Real.QuestToReach(t.Id)));
    }

    [Fact]
    public void TheVale_HasItsOwnDecor_WaterOnEveryMap_Terraces_TreesAndFlowers()
    {
        double Count(string zone, char c) => Real.Towns.Values.Where(t => t.Zone == zone).Average(t => t.Rows.Sum(r => r.Count(x => x == c)));
        // Springs everywhere: water on every map, more than in the mines; trees and dry-stone walls; flowers on the terraces.
        Assert.All(Vale, t => Assert.Contains(t.Rows, r => r.Contains('~')));
        Assert.True(Count("wellspring", '~') > 2 * Count("underroot", '~'), $"{Count("wellspring", '~')} water a map, the mines {Count("underroot", '~')}");
        Assert.True(Count("wellspring", 'R') < Count("underroot", 'R'), $"{Count("wellspring", 'R')} rocks a map, the mines {Count("underroot", 'R')}");
        Assert.True(Count("wellspring", 'T') > Count("underroot", 'T'), $"{Count("wellspring", 'T')} trees a map, the mines {Count("underroot", 'T')}");
        Assert.All(Vale, t => Assert.Contains(t.Rows, r => r.Contains(',')));
        Assert.DoesNotContain(Vale, t => t.Rows.Any(r => r.Contains('C') || r.Contains('L')));
    }

    [Fact]
    public void TheValesSpots_CarryLevelSixtyFive_NearTheWayIn_AndSeventy_AtTheFarEnd()
    {
        HarvestNode[] spots = [.. Real.Nodes.Values.Where(n => Real.Towns[n.Map].Zone == "wellspring")];
        Assert.All(spots, n => Assert.Contains(n.Level, Levels));
        foreach (Town t in Vale)
        {
            int depth = MonsterGroups.Depth(t, World);
            HarvestNode[] here = [.. spots.Where(n => n.Map == t.Id)];
            Assert.NotEmpty(here);
            if (depth == 0)
                Assert.All(here, n => Assert.Equal(65, n.Level));
            if (depth == 2)
                Assert.All(here, n => Assert.Equal(70, n.Level));
        }
        Assert.Equal(0, MonsterGroups.Depth(Real.Towns["wellspring-2-0"], World));
        Assert.Equal(2, MonsterGroups.Depth(Real.Towns["wellspring-0-4"], World));
        Assert.Equal(2, MonsterGroups.Depth(Real.Towns["wellspring-4-4"], World));
        string[] ten = [.. Real.Jobs.Values.Where(j => j.Gathers).SelectMany(j => j.Harvests!).Where(h => Levels.Contains(h.Level)).Select(h => h.Item)];
        Assert.Equal(10, ten.Length);
        Assert.Equal(ten.Order(), spots.Select(n => n.Item).Distinct().Order());
        Assert.All(Real.Nodes.Values.Where(n => ten.Contains(n.Item)), n => Assert.Equal("wellspring", Real.Towns[n.Map].Zone));
    }

    [Fact]
    public void TheValesResources_AreNamedInBothLanguages_AndDrawn()
    {
        string[] ids = ["birch-log", "walnut-log", "calcite", "platinum-ore", "barbel", "arctic-char", "maize", "emmer", "watercress", "valerian"];
        Assert.All(ids, id =>
        {
            Item item = Real.Items[id];
            Assert.Equal(ItemKind.Resource, item.Kind);
            Assert.Contains(item.Level, Levels);
            Assert.False(string.IsNullOrWhiteSpace(item.Name.Fr));
            Assert.False(string.IsNullOrWhiteSpace(item.Name.En));
            Assert.False(string.IsNullOrWhiteSpace(item.Icon));
        });
        // No name twice among the items.
        Assert.All(ids, id => Assert.Single(Real.Items.Values, i => i.Name.Fr == Real.Items[id].Name.Fr));
    }

    [Fact]
    public void TheVale_HasAWaystoneAtItsWayIn_ForOneHundredAndThirtyTwoÉclats()
    {
        (string id, string map, Cell at) = Waystones.All(Real).Single(w => Real.Towns[w.Map].Zone == "wellspring");
        Assert.Equal(("waystone-wellspring", "wellspring-2-0"), (id, map));
        Assert.InRange(at.DistanceTo(Real.Towns[map].Spawn), 1, 2);
        Assert.Equal(132, Waystones.Cost(id, Real));
    }
}
