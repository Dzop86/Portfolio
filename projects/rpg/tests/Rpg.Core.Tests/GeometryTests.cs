namespace Rpg.Core.Tests;

public class GeometryTests
{
    [Fact]
    public void Distance_CountsSteps_AndInLineMeansSameRowOrColumn()
    {
        Assert.Equal(7, new Cell(1, 2).DistanceTo(new Cell(4, -2)));
        Assert.Equal(0, new Cell(3, 3).DistanceTo(new Cell(3, 3)));
        Assert.True(new Cell(1, 2).IsInLineWith(new Cell(1, 9)));
        Assert.True(new Cell(1, 2).IsInLineWith(new Cell(-4, 2)));
        Assert.False(new Cell(1, 2).IsInLineWith(new Cell(2, 3)));
        Assert.Equal([new Cell(3, 2), new Cell(2, 3), new Cell(1, 2), new Cell(2, 1)], new Cell(2, 2).Neighbours());
    }

    [Fact]
    public void Iso_CentreOfEveryCell_ComesBackToTheCell()
    {
        for (int x = -12; x <= 12; x++)
        {
            for (int y = -12; y <= 12; y++)
            {
                (double sx, double sy) = Iso.ToScreen(new Cell(x, y), 64, 32);
                Assert.Equal(new Cell(x, y), Iso.FromScreen(sx, sy, 64, 32));
            }
        }
    }

    [Fact]
    public void Iso_PointsJustInsideTheDiamond_StayInTheCell_JustOutside_GoToTheNeighbour()
    {
        var c = new Cell(3, 5);
        (double x, double y) = Iso.ToScreen(c, 64, 32);
        // The diamond's corners are 32 px left and right, 16 px up and down from the centre.
        Assert.Equal(c, Iso.FromScreen(x + 31.5, y, 64, 32));
        Assert.Equal(c, Iso.FromScreen(x, y - 15.8, 64, 32));
        Assert.Equal(c, Iso.FromScreen(x + 15, y + 7, 64, 32));
        // Right of the right corner: cell (x + 1, y - 1); below the lower corner: (x + 1, y + 1).
        Assert.Equal(new Cell(4, 4), Iso.FromScreen(x + 32.5, y, 64, 32));
        Assert.Equal(new Cell(4, 6), Iso.FromScreen(x, y + 16.5, 64, 32));
        // Across the lower right side: the neighbour (x + 1, y).
        Assert.Equal(new Cell(4, 5), Iso.FromScreen(x + 17, y + 8, 64, 32));
    }

    [Fact]
    public void Board_ReadsTerrainAndStartsInReadingOrder()
    {
        Board b = Board.Parse(["B.#", "A~A"]);
        Assert.Equal((3, 2), (b.Width, b.Height));
        Assert.Equal(Terrain.Floor, b[new Cell(0, 0)]);
        Assert.Equal(Terrain.Obstacle, b[new Cell(2, 0)]);
        Assert.Equal(Terrain.Hole, b[new Cell(1, 1)]);
        Assert.Equal(Terrain.Hole, b[new Cell(-1, 0)]);
        Assert.Equal([new Cell(0, 1), new Cell(2, 1)], b.Starts[0]);
        Assert.Equal([new Cell(0, 0)], b.Starts[1]);
        Assert.Equal(6, b.Cells().Count());
    }

    /// <summary>Rows separated by '|'.</summary>
    [Theory]
    [InlineData("...|..")]
    [InlineData("..x")]
    [InlineData("")]
    [InlineData("|")]
    public void Board_RefusesRaggedEmptyOrUnknownRows(string rows) =>
        Assert.Throws<FormatException>(() => Board.Parse(rows.Length == 0 ? [] : rows.Split('|')));
}
