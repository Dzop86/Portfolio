using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The world seen from the sky (sprint 71).</summary>
public class WorldPictureTests
{
    private static readonly WorldPicture Picture = new(GameData.Embedded);
    private const int P = WorldPicture.CellPixels;

    private static Rgb Centre(string map, Cell c)
    {
        (double x, double y) = Picture.PixelOf(map, c);
        return Picture.At((int)x, (int)y);
    }

    [Fact]
    public void ThePicture_LaysEveryMapInItsPlace_EdgeToEdge()
    {
        // From the mouldy woods' west column (-10) to the village's east one (3, sprint 102), from the lake's north row (-7) to the fields' south row (7).
        Assert.Equal((14, 15, 48, 32), (Picture.Columns, Picture.RowsOfMaps, Picture.MapWidth, Picture.MapHeight));
        Assert.Equal((14 * 48 * P, 15 * 32 * P), (Picture.Width, Picture.Height));
        Assert.Equal(Picture.Width * Picture.Height * 4, Picture.Pixels.Length);
        Assert.Equal(Picture.Pixels, new WorldPicture(GameData.Embedded).Pixels);
        foreach (Town t in GameData.Embedded.Towns.Values.Where(t => !t.IsHall))
        {
            (double x, double y) = Picture.PixelOf(t.Id, t.Spawn);
            Assert.Equal(t.Id, Picture.MapAtPixel(x, y)?.Id);
        }
        // North of the village, no map: the dark of the interface, and no name.
        (double vx, double vy) = Picture.PixelOf("clairval-1-0", new Cell(8, 6));
        Assert.Null(Picture.MapAtPixel(vx, vy - 32 * P));
        Assert.Equal(WorldPicture.Void, Picture.At((int)vx, (int)vy - 32 * P));
        Assert.Null(Picture.MapAtPixel(-1, 0));
        Assert.Null(Picture.MapAtPixel(Picture.Width, 0));
    }

    [Fact]
    public void EachCell_IsDrawnAfterWhatStandsOnIt()
    {
        Town village = GameData.Embedded.Towns["clairval"];
        Assert.Equal('=', village[new Cell(24, 2)]);
        Assert.Equal(WorldPicture.Road, Centre("clairval", new Cell(24, 2)));
        // The fountain: stone around, water inside.
        Assert.Equal('F', village[new Cell(22, 13)]);
        (int cx, int cy) = Picture.Corner(village);
        Assert.Equal(WorldPicture.Stone, Picture.At(cx + 22 * P, cy + 13 * P));
        Assert.Equal(WorldPicture.Water, Picture.At(cx + 22 * P + P / 2, cy + 13 * P + P / 2));
        // A roof, its edge where the house ends.
        Assert.Equal('H', village[new Cell(5, 4)]);
        Assert.Equal(WorldPicture.Roof, Picture.At(cx + 5 * P + P / 2, cy + 4 * P + P / 2));
        Assert.Equal(WorldPicture.RoofEdge, Picture.At(cx + 5 * P, cy + 4 * P + P / 2));
        // A tree seen from above on its grass, water in the mill's pond.
        Assert.Equal('T', village[new Cell(43, 21)]);
        Assert.Equal(WorldPicture.Tree, Picture.At(cx + 43 * P + P / 2, cy + 21 * P + P / 2));
        Assert.Equal(WorldPicture.Grass, Picture.At(cx + 43 * P, cy + 21 * P));
        Town mill = GameData.Embedded.Towns["clairval-2-0"];
        Assert.Equal('~', mill[new Cell(33, 6)]);
        (int mx, int my) = Picture.Corner(mill);
        Assert.Equal(WorldPicture.Water, Picture.At(mx + 33 * P, my + 6 * P));
        // The palisade, in the chart's chocolate.
        Assert.Equal('P', mill[new Cell(10, 0)]);
        Assert.Equal(WorldPicture.Palisade, Picture.At(mx + 10 * P + P - 1, my + P - 1));
        // Grass in checks, as on the board.
        Assert.Equal(WorldPicture.Grass, Picture.At(cx + 2 * P, cy + 2 * P));
        Assert.Equal(WorldPicture.GrassDark, Picture.At(cx + 3 * P, cy + 2 * P));
    }

    [Fact]
    public void TheWaysBetweenMaps_MeetAsOnOneMap()
    {
        // The village's west gate and the woods' east opening are side by side, both open ground.
        (double x, double y) = Picture.PixelOf("clairval-0-1", new Cell(0, 16));
        Assert.Equal("woods-4-2", Picture.MapAtPixel(x - P, y)?.Id);
        Assert.Equal(Picture.PixelOf("woods-4-2", new Cell(47, 16)), (x - P, y));
        Assert.NotEqual(WorldPicture.Tree, Picture.At((int)x - P, (int)y));
        // Every way of the woods has open ground on both sides of the edge.
        foreach (Town t in GameData.Embedded.Towns.Values.Where(t => !t.IsHall))
        {
            foreach (ZoneLink l in t.Links ?? [])
            {
                Assert.NotEqual(WorldPicture.Tree, Centre(t.Id, l.At));
                Assert.NotEqual(WorldPicture.Tree, Centre(l.To, l.Arrival));
            }
        }
    }

    [Fact]
    public void AWorldWithoutMaps_HasNoPicture()
    {
        GameData empty = new([], [], []);
        Assert.Throws<ArgumentException>(() => new WorldPicture(empty));
    }
}
