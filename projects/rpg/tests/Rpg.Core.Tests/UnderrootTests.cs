using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Underroot Mines (sprint 224, D102, docs/banc-groupes.md §5): the first zone past level 45, 25 maps south of the
/// Forest of the Ancestors, open on its far end by one map, their own decor, the gathered resources of levels 50 and 55,
/// a waystone at their way in. Their families come with sprints 225 and 226.
/// </summary>
public class UnderrootTests
{
    private static readonly int[] Levels = [50, 55];

    private static Town[] World => [.. Real.Towns.Values];

    private static Town[] Mines => [.. Real.Towns.Values.Where(t => t.Zone == "underroot")];

    [Fact]
    public void TheMines_AreFiveByFiveMaps_SouthOfTheForest_AtLevelFortySix()
    {
        Assert.Equal(25, Mines.Length);
        Assert.All(Mines, t => Assert.Equal((48, 32, 46, "Mines de Sous-Racine", "Underroot Mines"), (t.Width, t.Rows.Count, t.Level, t.Name.Fr, t.Name.En)));
        Assert.Equal([.. Enumerable.Range(4, 5).SelectMany(x => Enumerable.Range(6, 5).Select(y => new Cell(x, y)))],
            Mines.Select(t => t.MapAt).OrderBy(c => c.X).ThenBy(c => c.Y));
        // No two maps of the world in the same place.
        Assert.Equal(World.Count(t => t.Dungeon is null && t.Zone is not null), World.Where(t => t.Dungeon is null && t.Zone is not null).Select(t => t.MapAt).Distinct().Count());
    }

    [Fact]
    public void TheWayDown_LeavesTheForestsFarEnd_IntoTheMines_AndBack()
    {
        Town forest = Real.Towns["ancestors-2-4"], entry = Real.Towns["underroot-2-0"];
        ZoneLink[] down = [.. forest.Links!.Where(l => l.To == entry.Id)];
        ZoneLink[] up = [.. entry.Links!.Where(l => l.To == forest.Id)];
        Assert.Equal([23, 24, 25], down.Select(l => l.At.X));
        Assert.All(down, l => Assert.Equal((forest.Rows.Count - 1, 1, l.At.X), (l.At.Y, l.Arrival.Y, l.Arrival.X)));
        Assert.Equal([23, 24, 25], up.Select(l => l.At.X));
        Assert.All(up, l => Assert.Equal((0, entry.Rows.Count - 2, l.At.X), (l.At.Y, l.Arrival.Y, l.Arrival.X)));
        Assert.All(down, l => Assert.Equal(("Mines de Sous-Racine", "Underroot Mines"), (l.Name!.Fr, l.Name.En)));
        Assert.All(up, l => Assert.Equal("Forest of the Ancestors", l.Name!.En));
        // Every cell of the way is walkable and reached from each map's arrival.
        Assert.All(down, l => Assert.True(forest.CanStand(l.At), $"{l.At} is not walkable on the forest's side"));
        Assert.All(up, l => Assert.True(entry.CanStand(l.At), $"{l.At} is not walkable on the mines' side"));
        Assert.All(down.Select(l => l.Arrival), a => Assert.True(entry.CanStand(a)));
        Assert.All(up.Select(l => l.Arrival), a => Assert.True(forest.CanStand(a)));
        // The only ways between the mines and the rest of the world.
        Assert.Equal(6, World.Where(t => t.Dungeon is null).SelectMany(t => (t.Links ?? []).Select(l => (From: t, l.To)))
            .Count(w => (w.From.Zone == "underroot") != (Real.Towns[w.To].Zone == "underroot") && Real.Towns[w.To].Dungeon is null));
    }

    [Fact]
    public void EveryWayOfTheMines_LeadsToAWalkableCell_AndEveryMapIsReachedFromTheForest()
    {
        foreach (Town t in Mines)
            foreach (ZoneLink l in t.Links ?? [])
            {
                Assert.True(t.CanStand(l.At), $"{t.Id}: the way at {l.At} is not walkable");
                Assert.True(Real.Towns[l.To].CanStand(l.Arrival), $"{t.Id}: the way at {l.At} arrives on a blocked cell of {l.To}");
            }
        HashSet<string> seen = ["underroot-2-0"];
        var todo = new Queue<string>(seen);
        while (todo.TryDequeue(out string? id))
            foreach (ZoneLink l in Real.Towns[id].Links ?? [])
                if (Real.Towns[l.To].Zone == "underroot" && seen.Add(l.To))
                    todo.Enqueue(l.To);
        Assert.Equal(25, seen.Count);
    }

    [Fact]
    public void TheMines_AreBehindClairvalsEastGate_TheirMapsNeedFourTrails()
    {
        Assert.All(Mines, t => Assert.Equal("four-trails", Real.QuestToReach(t.Id)));
    }

    [Fact]
    public void TheMines_HaveTheirOwnDecor_CragsAboveAll_AFewTrees_CartsAndLanterns()
    {
        double Count(string zone, char c) => Real.Towns.Values.Where(t => t.Zone == zone).Average(t => t.Rows.Sum(r => r.Count(x => x == c)));
        // Rock where the forest has trees: more rocks than trees, more rocks than in the forest, fewer trees.
        Assert.True(Count("underroot", 'R') > 2 * Count("underroot", 'T'), $"{Count("underroot", 'R')} rocks a map, {Count("underroot", 'T')} trees");
        Assert.True(Count("underroot", 'R') > Count("ancestors", 'R'), $"{Count("underroot", 'R')} rocks a map, the forest {Count("ancestors", 'R')}");
        Assert.True(Count("underroot", 'T') < Count("ancestors", 'T'), $"{Count("underroot", 'T')} trees a map, the forest {Count("ancestors", 'T')}");
        Assert.All(Mines, t => Assert.Contains(t.Rows, r => r.Contains('C')));
        Assert.All(Mines, t => Assert.Contains(t.Rows, r => r.Contains('L')));
        Assert.InRange(Mines.Count(t => t.Rows.Any(r => r.Contains('~'))), 5, 22);
    }

    [Fact]
    public void TheMinesSpots_CarryLevelFifty_NearTheWayIn_AndFiftyFive_AtTheFarEnd()
    {
        HarvestNode[] spots = [.. Real.Nodes.Values.Where(n => Real.Towns[n.Map].Zone == "underroot")];
        Assert.All(spots, n => Assert.Contains(n.Level, Levels));
        foreach (Town t in Mines)
        {
            int depth = MonsterGroups.Depth(t, World);
            HarvestNode[] here = [.. spots.Where(n => n.Map == t.Id)];
            Assert.NotEmpty(here);
            if (depth == 0)
                Assert.All(here, n => Assert.Equal(50, n.Level));
            if (depth == 2)
                Assert.All(here, n => Assert.Equal(55, n.Level));
        }
        // The way in is the forest's far end: the entry map is near, the south corners far.
        Assert.Equal(0, MonsterGroups.Depth(Real.Towns["underroot-2-0"], World));
        Assert.Equal(2, MonsterGroups.Depth(Real.Towns["underroot-0-4"], World));
        Assert.Equal(2, MonsterGroups.Depth(Real.Towns["underroot-4-4"], World));
        // The forest's own depths unchanged: its way in is still Quatre-Chemins, the mines lying higher.
        Assert.Equal(0, MonsterGroups.Depth(Real.Towns["ancestors-1-0"], World));
        Assert.Equal(2, MonsterGroups.Depth(Real.Towns["ancestors-2-4"], World));
        // Every one of the ten resources of the five gathering trades grows there, and only there.
        string[] ten = [.. Real.Jobs.Values.Where(j => j.Gathers).SelectMany(j => j.Harvests!).Where(h => h.Level >= 50).Select(h => h.Item)];
        Assert.Equal(10, ten.Length);
        Assert.Equal(ten.Order(), spots.Select(n => n.Item).Distinct().Order());
        Assert.All(Real.Nodes.Values.Where(n => ten.Contains(n.Item)), n => Assert.Equal("underroot", Real.Towns[n.Map].Zone));
    }

    [Fact]
    public void TheMinesResources_AreNamedInBothLanguages_AndDrawn()
    {
        string[] ids = ["larch-log", "rootwood-log", "cobalt-ore", "gold-ore", "grayling", "cavefish", "barley", "millet", "arnica", "gentian"];
        Assert.All(ids, id =>
        {
            Item item = Real.Items[id];
            Assert.Equal(ItemKind.Resource, item.Kind);
            Assert.Contains(item.Level, Levels);
            Assert.False(string.IsNullOrWhiteSpace(item.Name.Fr));
            Assert.False(string.IsNullOrWhiteSpace(item.Name.En));
            Assert.False(string.IsNullOrWhiteSpace(item.Icon));
        });
    }

    [Fact]
    public void TheMines_HaveAWaystoneAtTheirWayIn_ForOneHundredAndTwoÉclats()
    {
        (string id, string map, Cell at) = Waystones.All(Real).Single(w => Real.Towns[w.Map].Zone == "underroot");
        Assert.Equal(("waystone-underroot", "underroot-2-0"), (id, map));
        Assert.InRange(at.DistanceTo(Real.Towns[map].Spawn), 1, 2);
        Assert.Equal(102, Waystones.Cost(id, Real));
    }
}
