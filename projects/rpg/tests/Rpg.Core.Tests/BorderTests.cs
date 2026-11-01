using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The natural borders of the five zones (sprint 180, D87 §7.C, tools/border_soften.py): no more straight wall of trees
/// round each map, the sides between two maps of a zone open wide, the villages left as they were.
/// </summary>
public class BorderTests
{
    private static readonly string[] Zones = ["woods", "mouldy", "lake", "fields", "ancestors"];

    private static Town[] Wild => [.. Real.Towns.Values.Where(t => t.Zone is string z && Zones.Contains(z))];

    private static bool Walkable(char c) => c is '.' or '=' or ',';

    /// <summary>The cells of one side of a map, from its first to its last, d cells inside the edge.</summary>
    private static Cell[] Side(Town t, Cell towards, int d = 0) => towards switch
    {
        { X: 1 } => [.. Enumerable.Range(0, t.Rows.Count).Select(y => new Cell(t.Width - 1 - d, y))],
        { X: -1 } => [.. Enumerable.Range(0, t.Rows.Count).Select(y => new Cell(d, y))],
        { Y: 1 } => [.. Enumerable.Range(0, t.Width).Select(x => new Cell(x, t.Rows.Count - 1 - d))],
        _ => [.. Enumerable.Range(0, t.Width).Select(x => new Cell(x, d))],
    };

    private static readonly Cell[] Towards = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

    [Fact]
    public void TwoMapsOfAZone_OpenOnEachOtherOverHalfTheSide_NeverClosedOverEightCellsInARow()
    {
        Town[] wild = Wild;
        int seams = 0;
        foreach (Town a in wild)
        {
            foreach (Cell towards in Towards)
            {
                if (wild.FirstOrDefault(b => b.Zone == a.Zone && b.MapAt == a.MapAt + towards) is not Town b)
                    continue;
                seams++;
                Cell[] edge = Side(a, towards);
                int open = a.Links!.Count(l => l.To == b.Id);
                Assert.Equal(open, b.Links!.Count(l => l.To == a.Id));
                // Water stops a way on the lake; elsewhere nothing does.
                if (a.Zone != "lake")
                    Assert.True(open >= edge.Length / 2, $"{a.Id} → {b.Id}: {open} of {edge.Length}");
                // A closed run on the edge: obstacles side by side (water is the lake going on to the next map).
                int run = 0, longest = 0;
                foreach (Cell c in edge)
                {
                    run = Walkable(a[c]) || a[c] == '~' ? 0 : run + 1;
                    longest = Math.Max(longest, run);
                }
                Assert.True(longest <= 8, $"{a.Id}, side {towards}: {longest} closed cells in a row");
            }
        }
        // 4 × 5 × 2 sides between maps in each zone, seen from both maps.
        Assert.Equal(Zones.Length * 40 * 2, seams);
    }

    [Fact]
    public void TheBand_IsOneToThreeCellsDeep_AndItsDepthVaries()
    {
        foreach (Town t in Wild)
        {
            var depths = new List<int>();
            // The sides that open on no map of the zone: the band is all there is (a side inside the zone is mostly open).
            foreach (Cell towards in Towards.Where(w => !Real.Towns.Values.Any(o => o.Zone == t.Zone && o.MapAt == t.MapAt + w)))
            {
                Cell[][] layers = [.. Enumerable.Range(0, 4).Select(d => Side(t, towards, d))];
                // The corners belong to two sides; a way's cell is open.
                for (int p = 1; p < layers[0].Length - 1; p++)
                {
                    if (t.LinkAt(layers[0][p]) is not null || t[layers[0][p]] == '~')
                        continue;
                    int d = 0;
                    while (d < 4 && !Walkable(t[layers[d][p]]) && t[layers[d][p]] != '~')
                        d++;
                    depths.Add(d);
                }
            }
            // A map inside its zone has no such side.
            if (depths.Count < 24)
                continue;
            Assert.True(depths.All(d => d >= 1), $"{t.Id}: a closed side open on the edge");
            double mean = depths.Average(), deviation = Math.Sqrt(depths.Average(d => (d - mean) * (d - mean)));
            Assert.True(deviation > 0.5, $"{t.Id}: a band of a regular depth ({deviation:0.00})");
        }
    }

    [Fact]
    public void TheBand_MixesItsObstacles_ByStyle()
    {
        foreach (string zone in Zones)
        {
            HashSet<char> seen = [.. Wild.Where(t => t.Zone == zone).SelectMany(t => Towards.SelectMany(w => Side(t, w)).Select(c => t[c]))];
            // Not only the style's border (trees, or hedges): rocks among them everywhere.
            Assert.Contains('R', seen);
            Assert.True(seen.Count(c => !Walkable(c) && c != '~') >= 3, $"{zone}: {string.Concat(seen)}");
        }
    }

    [Fact]
    public void EveryWay_Exit_PersonAndProp_StaysReachableFromTheArrival()
    {
        foreach (Town t in Wild)
            Assert.True(MonsterGroups.KeepsWaysOpen(t, t.Board(), []), t.Id);
    }

    [Fact]
    public void TheVillages_KeepTheirContour()
    {
        // Clairval's palisade and Quatre-Chemins's edge: no map of theirs has a zone the tool softens.
        Town[] villages = [.. Real.Towns.Values.Where(t => Shop.VillageOf(t.Id) is not null)];
        Assert.NotEmpty(villages);
        Assert.All(villages, v => Assert.DoesNotContain(v.ZoneId, Zones));
        Assert.Contains(villages, v => v.Rows.Any(r => r.Contains('P')));
    }
}
