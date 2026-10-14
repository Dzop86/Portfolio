using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The world seen from the sky (sprint 71).</summary>
public class WorldPictureTests
{
    private static readonly WorldPicture Picture = new(GameData.Embedded);

    private static Rgb Centre(string map, Cell c)
    {
        (double x, double y) = Picture.PixelOf(map, c);
        return Picture.At((int)x, (int)y);
    }

    [Fact]
    public void ThePicture_LaysEveryMapInItsPlace_EdgeToEdge()
    {
        // From the mouldy woods' west column (-10) to the village (0), from the woods' north row (-2) to their south row (2).
        Assert.Equal((11, 5, 16, 12), (Picture.Columns, Picture.RowsOfMaps, Picture.MapWidth, Picture.MapHeight));
        Assert.Equal((11 * 16 * 8, 5 * 12 * 8), (Picture.Width, Picture.Height));
        Assert.Equal(Picture.Width * Picture.Height * 4, Picture.Pixels.Length);
        Assert.Equal(Picture.Pixels, new WorldPicture(GameData.Embedded).Pixels);
        foreach (Town t in GameData.Embedded.Towns.Values)
        {
            (double x, double y) = Picture.PixelOf(t.Id, t.Spawn);
            Assert.Equal(t.Id, Picture.MapAtPixel(x, y)?.Id);
        }
        // North of the village, no map: the dark of the interface, and no name.
        (double vx, double vy) = Picture.PixelOf("clairval", new Cell(8, 6));
        Assert.Null(Picture.MapAtPixel(vx, vy - 12 * 8));
        Assert.Equal(WorldPicture.Void, Picture.At((int)vx, (int)vy - 12 * 8));
        Assert.Null(Picture.MapAtPixel(-1, 0));
        Assert.Null(Picture.MapAtPixel(Picture.Width, 0));
    }

    [Fact]
    public void EachCell_IsDrawnAfterWhatStandsOnIt()
    {
        Town village = GameData.Embedded.Towns["clairval"];
        Assert.Equal('=', village[new Cell(6, 2)]);
        Assert.Equal(WorldPicture.Road, Centre("clairval", new Cell(6, 2)));
        // The fountain: stone around, water inside.
        Assert.Equal('F', village[new Cell(7, 4)]);
        (int cx, int cy) = Picture.Corner(village);
        Assert.Equal(WorldPicture.Stone, Picture.At(cx + 7 * 8, cy + 4 * 8));
        Assert.Equal(WorldPicture.Water, Picture.At(cx + 7 * 8 + 4, cy + 4 * 8 + 4));
        // A roof, its edge where the house ends.
        Assert.Equal('H', village[new Cell(4, 0)]);
        Assert.Equal(WorldPicture.Roof, Picture.At(cx + 4 * 8 + 4, cy + 4));
        Assert.Equal(WorldPicture.RoofEdge, Picture.At(cx + 4 * 8, cy + 4));
        // A tree seen from above on its grass, water in the pond.
        Assert.Equal('T', village[new Cell(0, 0)]);
        Assert.Equal(WorldPicture.Tree, Picture.At(cx + 4, cy + 5));
        Assert.Equal(WorldPicture.Grass, Picture.At(cx, cy));
        Assert.Equal('~', village[new Cell(3, 9)]);
        Assert.Equal(WorldPicture.Water, Picture.At(cx + 3 * 8, cy + 9 * 8));
        // Grass in checks, as on the board.
        Assert.Equal(WorldPicture.Grass, Picture.At(cx + 2 * 8, cy + 2 * 8));
        Assert.Equal(WorldPicture.GrassDark, Picture.At(cx + 3 * 8, cy + 2 * 8));
    }

    [Fact]
    public void TheWaysBetweenMaps_MeetAsOnOneMap()
    {
        // The village's west road and the woods' east opening are side by side, both open ground.
        (double x, double y) = Picture.PixelOf("clairval", new Cell(0, 5));
        Assert.Equal("woods-4-2", Picture.MapAtPixel(x - 8, y)?.Id);
        Assert.Equal(Picture.PixelOf("woods-4-2", new Cell(15, 5)), (x - 8, y));
        Assert.NotEqual(WorldPicture.Tree, Picture.At((int)x - 8, (int)y));
        // Every way of the woods has open ground on both sides of the edge.
        foreach (Town t in GameData.Embedded.Towns.Values)
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
