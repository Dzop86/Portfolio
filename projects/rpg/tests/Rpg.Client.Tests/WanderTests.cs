using Rpg.Core;

namespace Rpg.Client.Tests;

public class WanderTests
{
    private static Board Empty(int w, int h) => Board.Parse([.. Enumerable.Repeat(new string('.', w), h)]);

    [Fact]
    public void OnAnEmptyBoard_TheWalkGoesDiagonally_ThenStraight()
    {
        Board board = Empty(8, 8);
        IReadOnlyList<Cell> path = Wander.FindPath(board, new Cell(0, 0), new Cell(5, 3), _ => true)!;
        // Five steps, not eight: three diagonals and two sides.
        Assert.Equal(5, path.Count);
        Assert.Equal(new Cell(5, 3), path[^1]);
        Assert.Equal(3, path.Zip(path.Prepend(new Cell(0, 0))).Count(z => z.First.X != z.Second.X && z.First.Y != z.Second.Y));
        Assert.Equal(10 * 5 + 4 * 3, Wander.Distance(new Cell(0, 0), new Cell(5, 3)));
    }

    [Fact]
    public void ADiagonal_NeverCutsTheCornerOfAWall()
    {
        // From (0, 0) to (1, 1) with a wall at (1, 0): around it, not through its corner.
        Board board = Board.Parse([".#", ".."]);
        Assert.Equal([new Cell(0, 1), new Cell(1, 1)], Wander.FindPath(board, new Cell(0, 0), new Cell(1, 1), board.IsFloor)!);
        Assert.DoesNotContain(new Cell(1, 1), Wander.Neighbours(board, new Cell(0, 0), board.IsFloor));
    }

    [Fact]
    public void NoWay_GivesNull_AndTheStartGivesNoStep()
    {
        Board board = Board.Parse(["..#..", "..#.."]);
        Assert.Null(Wander.FindPath(board, new Cell(0, 0), new Cell(4, 0), board.IsFloor));
        Assert.Null(Wander.FindPath(board, new Cell(0, 0), new Cell(2, 0), board.IsFloor));
        Assert.Empty(Wander.FindPath(board, new Cell(1, 1), new Cell(1, 1), board.IsFloor)!);
    }

    [Fact]
    public void ThePathPulledTaut_IsOneStraightLine_WhenNothingIsInTheWay()
    {
        Board board = Empty(10, 10);
        IReadOnlyList<Cell> path = Wander.FindPath(board, new Cell(0, 0), new Cell(7, 2), _ => true)!;
        IReadOnlyList<Cell> waypoints = Wander.Smooth(new Cell(0, 0), path, _ => true);
        Assert.Equal([new Cell(7, 2)], waypoints);
        Assert.Equal(Math.Sqrt(53), Wander.Length(new Cell(0, 0), waypoints), 6);
        Assert.Empty(Wander.Smooth(new Cell(0, 0), [], _ => true));
    }

    [Fact]
    public void AroundAWall_TheStraightLinesKeepClearOfIt_AndAreShorterThanTheSteps()
    {
        Board board = Board.Parse([
            "........",
            "...##...",
            "...##...",
            "........",
        ]);
        var from = new Cell(0, 2);
        var to = new Cell(7, 1);
        IReadOnlyList<Cell> path = Wander.FindPath(board, from, to, board.IsFloor)!;
        IReadOnlyList<Cell> waypoints = Wander.Smooth(from, path, board.IsFloor);
        Assert.True(waypoints.Count >= 2 && waypoints.Count < path.Count);
        Assert.Equal(to, waypoints[^1]);
        // Every corner is a cell of the path, every line between them clear of the wall.
        Assert.All(waypoints, w => Assert.Contains(w, path));
        Cell at = from;
        foreach (Cell w in waypoints)
        {
            Assert.True(Wander.StraightLine(at, w, board.IsFloor));
            at = w;
        }
        Assert.True(Wander.Length(from, waypoints) < Wander.Length(from, path));
        // A line through the wall is not straight-walkable.
        Assert.False(Wander.StraightLine(new Cell(2, 1), new Cell(5, 2), board.IsFloor));
    }

    [Fact]
    public void ALineAlongAWall_KeepsItsClearance()
    {
        Board board = Board.Parse([
            "#####",
            ".....",
        ]);
        Assert.True(Wander.StraightLine(new Cell(0, 1), new Cell(4, 1), board.IsFloor));
        // Brushing past the corner of a wall is not clear.
        Board corner = Board.Parse(["#....", "....."]);
        Assert.False(Wander.StraightLine(new Cell(0, 1), new Cell(2, 0), corner.IsFloor));
    }
}
