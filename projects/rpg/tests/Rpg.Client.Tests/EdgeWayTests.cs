using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>Changing maps as in the games of the genre (sprint 83, D66): the edge leads to its way out.</summary>
public class EdgeWayTests
{
    private static readonly GameData Data = GameData.Embedded;

    [Fact]
    public void AClickOnAnEdge_EvenOnItsTrees_WalksToTheWayOutOnThatSide_AndTakesIt()
    {
        var town = new TownController(Data, "woods-2-2");
        Town map = town.Town;
        // A cell of the west border that is not a way: a tree, most likely.
        Cell border = Enumerable.Range(5, map.Rows.Count - 10).Select(y => new Cell(0, y)).First(c => map.LinkAt(c) is null);
        (ZoneLink way, Cell direction) = town.EdgeWay(border)!.Value;
        Assert.Equal(0, way.At.X);
        Assert.Equal(new Cell(-1, 0), direction);
        Assert.Equal(way.To, map.LinkAt(way.At)!.To);
        // One cell in leads there too.
        Assert.Equal(way.To, town.EdgeWay(border + new Cell(1, 0))!.Value.Way.To);
        TownStep step = town.Click(border)!;
        Assert.Equal(way, step.Link);
        Assert.Equal(way, town.Travel);
        Assert.Equal(way.At, town.Position);
    }

    [Fact]
    public void EachSide_PointsItsOwnWay()
    {
        var town = new TownController(Data, "woods-2-2");
        Town map = town.Town;
        int w = map.Width - 1, h = map.Rows.Count - 1;
        Assert.Equal(new Cell(1, 0), town.EdgeWay(new Cell(w, h / 2))!.Value.Direction);
        Assert.Equal(new Cell(0, -1), town.EdgeWay(new Cell(w / 2, 0))!.Value.Direction);
        Assert.Equal(new Cell(0, 1), town.EdgeWay(new Cell(w / 2, h))!.Value.Direction);
        Assert.All(new[] { new Cell(w, h / 2), new Cell(w / 2, 0), new Cell(w / 2, h) }, c => Assert.True(town.EdgeWay(c)!.Value.Way.At.DistanceTo(c) <= Math.Max(map.Width, map.Rows.Count)));
        // Away from the edges, nothing; two cells in, still the edge (sprint 85).
        Assert.Null(town.EdgeWay(new Cell(w / 2, h / 2)));
        Assert.Null(town.EdgeWay(new Cell(3, h / 2)));
        Assert.Equal(new Cell(-1, 0), town.EdgeWay(new Cell(2, h / 2))!.Value.Direction);
    }

    [Fact]
    public void ASideWithoutAWay_LeadsNowhere_AndAWayItselfIsTakenAsBefore()
    {
        // The village's quarry, in its north-east corner (sprint 102), opens west and south only.
        var clairval = new TownController(Data, "clairval-3-0");
        Town map = clairval.Town;
        Assert.Null(clairval.EdgeWay(new Cell(map.Width - 1, 2)));
        Assert.Null(clairval.EdgeWay(new Cell(6, 0)));
        Assert.Equal(new Cell(-1, 0), clairval.EdgeWay(new Cell(0, 9))!.Value.Direction);
        ZoneLink link = map.Links!.First(l => l.At.X == 0);
        Assert.Equal(link, clairval.Click(link.At)!.Link);
        // Busy (a walk to a way under way), no arrow.
        Assert.Null(clairval.EdgeWay(new Cell(0, 9)));
    }

    [Fact]
    public void AClickBeyondTheMap_OnTheRightSide_LeadsThere_TheFarthestSideFirst()
    {
        var town = new TownController(Data, "woods-2-2");
        Town map = town.Town;
        int w = map.Width - 1, h = map.Rows.Count - 1;
        // In the dark on the left, on the right, above, below.
        Assert.Equal(new Cell(-1, 0), town.EdgeWay(new Cell(-6, h / 2))!.Value.Direction);
        Assert.Equal(new Cell(1, 0), town.EdgeWay(new Cell(w + 3, 4))!.Value.Direction);
        Assert.Equal(new Cell(0, -1), town.EdgeWay(new Cell(w / 2, -9))!.Value.Direction);
        Assert.Equal(new Cell(0, 1), town.EdgeWay(new Cell(-1, h + 7))!.Value.Direction);
        TownStep step = town.Click(new Cell(-6, h / 2))!;
        Assert.Equal(0, step.Link!.At.X);
        Assert.Equal(step.Link, town.Travel);
        // The quarry has no way north: beyond its top left corner, the west way.
        var clairval = new TownController(Data, "clairval-3-0");
        Assert.Equal(new Cell(-1, 0), clairval.EdgeWay(new Cell(-1, -8))!.Value.Direction);
        // Beyond a side without a way: nothing.
        Assert.Null(clairval.EdgeWay(new Cell(clairval.Town.Width + 5, 6)));
    }
}
