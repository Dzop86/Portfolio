namespace Rogue.Core;

public enum Direction
{
    North,
    South,
    East,
    West,
}

public readonly record struct Point(int X, int Y)
{
    public Point Step(Direction direction) => direction switch
    {
        Direction.North => new(X, Y - 1),
        Direction.South => new(X, Y + 1),
        Direction.East => new(X + 1, Y),
        Direction.West => new(X - 1, Y),
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    /// <summary>True when the two points share a side (no diagonal moves in this game).</summary>
    public bool IsAdjacentTo(Point other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y) == 1;
}

/// <summary>A rectangular room: its floor covers [X, X + Width) × [Y, Y + Height).</summary>
public readonly record struct Room(int X, int Y, int Width, int Height)
{
    public Point Center => new(X + Width / 2, Y + Height / 2);

    public bool Contains(Point p) => p.X >= X && p.X < X + Width && p.Y >= Y && p.Y < Y + Height;

    /// <summary>True when the rooms overlap or are closer than <paramref name="margin"/> tiles.</summary>
    public bool Overlaps(Room other, int margin) =>
        X - margin < other.X + other.Width && other.X - margin < X + Width
        && Y - margin < other.Y + other.Height && other.Y - margin < Y + Height;
}
