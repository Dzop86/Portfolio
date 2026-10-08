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
