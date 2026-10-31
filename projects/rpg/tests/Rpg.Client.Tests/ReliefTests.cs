using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The ground's heights out of a fight (sprint 179, D90): hills in the forests, flat fields.</summary>
public class ReliefTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static readonly Dictionary<Cell, Town> World = MapPhotos.World(Data).GroupBy(t => t.MapAt).ToDictionary(g => g.Key, g => g.First());

    private static IEnumerable<(int X, int Y)> Corners(Town map) =>
        Enumerable.Range(0, map.Rows.Count + 1).SelectMany(y => Enumerable.Range(0, map.Width + 1).Select(x => (x, y)));

    [Fact]
    public void TheForests_HaveHills_AndTheFieldsTheLakeAndTheVillage_AreFlat()
    {
        foreach (string id in new[] { "woods-2-2", "mouldy-2-2", "ancestors-2-2" })
        {
            Town map = Data.Towns[id];
            var relief = new Relief(Data, map);
            float[] heights = [.. Corners(map).Select(c => relief.Corner(c.X, c.Y))];
            Assert.True(heights.Max() > 0.3f, $"{id} {heights.Max()}");
            Assert.True(heights.Max() <= Relief.Amplitude(map.Zone) + 0.001f);
            Assert.All(heights, h => Assert.True(h >= 0));
            Assert.False(relief.Flat);
        }
        foreach (string id in new[] { "fields-2-2", "lake-2-2", "clairval" })
            Assert.True(new Relief(Data, Data.Towns[id]).Flat, id);
        Assert.True(new Relief(Data, Data.Towns.Values.First(t => t.IsHall)).Flat);
    }

    [Fact]
    public void TheSlopes_StayGentle_SoTheGridStillReads()
    {
        Town map = Data.Towns["ancestors-2-2"];
        var relief = new Relief(Data, map);
        foreach ((int x, int y) in Corners(map))
        {
            Assert.InRange(Math.Abs(relief.Corner(x + 1, y) - relief.Corner(x, y)), 0, 0.5f);
            Assert.InRange(Math.Abs(relief.Corner(x, y + 1) - relief.Corner(x, y)), 0, 0.5f);
        }
    }

    [Fact]
    public void WaterAndBuildings_SitOnFlatGround()
    {
        foreach (string id in new[] { "woods-2-2", "mouldy-2-2", "ancestors-2-2", "woods-1-1", "woods-3-3" })
        {
            Town map = Data.Towns[id];
            var relief = new Relief(Data, map);
            for (int y = 0; y < map.Rows.Count; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (!Relief.Flattens(map.Rows[y][x]))
                        continue;
                    Assert.Equal(0, relief.Corner(x, y));
                    Assert.Equal(0, relief.Corner(x + 1, y + 1));
                    Assert.Equal(0, relief.At(new Cell(x, y)));
                }
            }
        }
    }

    [Fact]
    public void AMapAndItsNeighbour_MeetWithoutASeam()
    {
        int seams = 0;
        foreach (Town map in World.Values.Where(t => t.Zone is "woods" or "ancestors"))
        {
            if (!World.TryGetValue(map.MapAt + new Cell(1, 0), out Town? right))
                continue;
            var here = new Relief(Data, map);
            var there = new Relief(Data, right);
            for (int y = 0; y <= map.Rows.Count; y++)
            {
                // Our right edge is its left edge; and our skirt shows its ground at its own heights.
                Assert.Equal(there.Corner(0, y), here.Corner(map.Width, y), 3);
                Assert.Equal(there.Corner(3, y), here.Corner(map.Width + 3, y), 3);
            }
            seams++;
        }
        Assert.True(seams > 10);
    }

    [Fact]
    public void BetweenCorners_TheGroundIsInterpolated()
    {
        var relief = new Relief(Data, Data.Towns["woods-2-2"]);
        // A cell's middle is the mean of its four corners.
        var c = new Cell(20, 14);
        float mean = (relief.Corner(20, 14) + relief.Corner(21, 14) + relief.Corner(20, 15) + relief.Corner(21, 15)) / 4;
        Assert.Equal(mean, relief.At(c), 4);
        Assert.Equal(relief.Corner(20, 14), relief.At(19.5f, 13.5f), 4);
        Assert.Equal(Relief.Shape(10, 20), Relief.Shape(10, 20));
    }
}
