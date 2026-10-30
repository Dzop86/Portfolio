using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The world map as an atlas (sprint 167, D87, s165-retours-30-oct.md §5.4): regions, coasts, sea, water, trees and roads.</summary>
public class WorldAtlasTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly WorldAtlas Atlas = new(Data);
    private static Town[] World => [.. Data.Towns.Values.Where(t => !t.IsHall)];

    [Fact]
    public void EachZone_HasItsRegion_EachMapsCentreInsideIt_ItsNameAmongItsMaps()
    {
        Assert.Equal(World.Select(WorldAtlas.Zone).Distinct().Order(StringComparer.Ordinal), Atlas.Regions.Select(r => r.Zone));
        foreach (Town map in World)
        {
            (double l, double t, double r, double b) = Atlas.Bounds(map);
            Assert.Equal(WorldAtlas.Zone(map), Atlas.RegionAt(new AtlasPoint((l + r) / 2, (t + b) / 2)));
        }
        foreach (AtlasRegion region in Atlas.Regions)
        {
            Assert.Equal(region.Zone, Atlas.RegionAt(region.Centre));
            Town[] maps = [.. World.Where(t => WorldAtlas.Zone(t) == region.Zone)];
            Assert.Equal((maps.Min(t => t.Level), maps.Max(t => t.Level)), (region.MinLevel, region.MaxLevel));
        }
    }

    [Fact]
    public void NoCoast_CutsAMap_TheLandSpillingPastThem()
    {
        // Every corner and every middle of an edge of every map is on land.
        foreach (Town map in World)
        {
            (double l, double t, double r, double b) = Atlas.Bounds(map);
            foreach (double x in new[] { l + 1, (l + r) / 2, r - 1 })
                foreach (double y in new[] { t + 1, (t + b) / 2, b - 1 })
                    Assert.True(Atlas.RegionAt(new AtlasPoint(x, y)) is not null, $"{map.Id} at ({x}, {y})");
        }
        // Far from the maps, the sea.
        Assert.Null(Atlas.RegionAt(new AtlasPoint(-200, -200)));
        Assert.Null(Atlas.ZoneAt(-200, -200));
    }

    [Fact]
    public void TheOutlines_AreClosed_AndWave_NoMoreRectangles()
    {
        foreach (AtlasRegion region in Atlas.Regions)
            foreach (IReadOnlyList<AtlasPoint> outline in region.Outlines)
            {
                Assert.True(outline.Count >= 8, region.Zone);
                // Smoothed: no edge longer than a step of the field.
                Assert.All(outline.Zip(outline.Skip(1).Append(outline[0])), e => Assert.True(Math.Sqrt(Math.Pow(e.First.X - e.Second.X, 2) + Math.Pow(e.First.Y - e.Second.Y, 2)) <= WorldAtlas.Step * WorldPicture.CellPixels, region.Zone));
                // Waved: few points on a map's straight line.
                int straight = outline.Count(p => Math.Abs(p.X % (48 * WorldPicture.CellPixels)) < 0.01);
                Assert.True(straight < outline.Count / 4, $"{region.Zone}: {straight} of {outline.Count}");
            }
    }

    [Fact]
    public void TheMarchingSquares_CloseEveryLoop_ASaddleIncluded()
    {
        // A cross of five samples and two corners touching at a saddle: every loop closed, the inside kept.
        bool[,] grid = new bool[7, 7];
        foreach ((int x, int y) in new[] { (2, 1), (1, 2), (2, 2), (3, 2), (2, 3), (4, 4), (5, 5) })
            grid[x, y] = true;
        List<List<AtlasPoint>> loops = WorldAtlas.Contours(7, 7, (i, j) => grid[i, j]);
        Assert.NotEmpty(loops);
        Assert.All(loops, l => Assert.True(l.Count >= 4));
        Assert.Contains(loops, l => WorldAtlas.Inside(l, new AtlasPoint(2, 2)));
        Assert.DoesNotContain(loops, l => WorldAtlas.Inside(l, new AtlasPoint(0.2, 0.2)));
    }

    [Fact]
    public void TheWater_TheTreesAndTheRoads_ComeFromTheMapsCells()
    {
        Assert.NotEmpty(Atlas.Waters);
        Assert.NotEmpty(Atlas.Trees);
        Assert.NotEmpty(Atlas.Roads);
        Assert.All(Atlas.Trees, t => Assert.InRange(t.Variant, 0, 2));
        // Every stamp and dot on a map.
        Assert.All(Atlas.Trees.Select(t => t.At).Concat(Atlas.Roads), p => Assert.InRange(p.X, 0, Atlas.Width));
        // Woods where the trees stand thick: the ancestors' forest holds one, each of eight cells across or more.
        Assert.NotEmpty(Atlas.Forests);
        Assert.Contains(Atlas.Forests, f => Atlas.RegionAt(f[0]) == "ancestors");
        Assert.All(Atlas.Forests, f => Assert.True(WorldAtlas.Area(f) >= 1024));
        // No puddle: a pool of five cells across or more.
        Assert.All(Atlas.Waters, w => Assert.True(WorldAtlas.Area(w) >= 400));
        Assert.Equal(12, WorldAtlas.Area([new(0, 0), new(4, 0), new(4, 3), new(0, 3)]));
        // The lake's zone holds water.
        Assert.Contains(Atlas.Waters, w => Atlas.RegionAt(w[0]) == "lake");
    }

    [Fact]
    public void ALandmark_ForEachCity_Dungeon_Waystone_AndWorkshop_OnItsOwnMap()
    {
        Assert.Equal(["clairval", "four-ways"], Atlas.Marks.Where(m => m.Kind == AtlasMarkKind.City).Select(m => m.Id));
        Assert.Equal(Data.Dungeons.Count, Atlas.Marks.Count(m => m.Kind == AtlasMarkKind.Dungeon));
        Assert.Equal(Waystones.All(Data).Count, Atlas.Marks.Count(m => m.Kind == AtlasMarkKind.Waystone));
        Assert.Equal(World.Sum(t => t.Workshops?.Count ?? 0), Atlas.Marks.Count(m => m.Kind == AtlasMarkKind.Workshop));
        foreach (AtlasMark mark in Atlas.Marks.Where(m => m.Kind != AtlasMarkKind.City))
        {
            (double l, double t, double r, double b) = Atlas.Bounds(Data.Towns[mark.Map]);
            Assert.InRange(mark.At.X, l, r);
            Assert.InRange(mark.At.Y, t, b);
        }
    }

    [Fact]
    public void TwoAtlases_OfTheSameWorld_AreTheSame()
    {
        var again = new WorldAtlas(Data);
        Assert.Equal(Atlas.Regions.SelectMany(r => r.Outlines.SelectMany(o => o)), again.Regions.SelectMany(r => r.Outlines.SelectMany(o => o)));
        Assert.Equal(Atlas.Trees, again.Trees);
        Assert.Equal(Atlas.Roads, again.Roads);
        Assert.Equal(Atlas.Waters.SelectMany(w => w), again.Waters.SelectMany(w => w));
        Assert.Equal(Atlas.Forests.SelectMany(w => w), again.Forests.SelectMany(w => w));
    }

    [Fact]
    public void EachColourOfTheAtlas_IsATokenOfTheCharter_EachZoneItsOwn()
    {
        string tokens = File.ReadAllText(Path.Combine(Repo(), "src", "assets", "tokens.css")).ToLowerInvariant();
        Assert.All(AtlasStyle.All, c => Assert.Contains("--osmose-map-", tokens, StringComparison.Ordinal));
        Assert.All(AtlasStyle.All, c => Assert.Contains(c + ";", tokens, StringComparison.Ordinal));
        string[] zones = [.. Atlas.Regions.Select(r => r.Zone).Where(z => z is not ("clairval" or "four-ways"))];
        Assert.Equal(zones.Length, zones.Select(AtlasStyle.Biome).Distinct().Count());
        Assert.Equal(AtlasStyle.Biome("clairval"), AtlasStyle.Biome("four-ways"));
    }

    private static string Repo([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", ".."));
}
