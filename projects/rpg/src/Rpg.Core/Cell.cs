namespace Rpg.Core;

/// <summary>
/// A cell of the board. The board is a square grid, drawn as diamonds by the client's isometric
/// camera: moving goes to one of the four cells sharing a side, and every distance
/// is counted in such steps (Manhattan distance), so a range draws a diamond around the caster.
/// </summary>
public readonly record struct Cell(int X, int Y)
{
    /// <summary>The four neighbours, always in this order: the order fixes ties in path finding.</summary>
    public static readonly Cell[] Steps = [new(1, 0), new(0, 1), new(-1, 0), new(0, -1)];

    public static Cell operator +(Cell a, Cell b) => new(a.X + b.X, a.Y + b.Y);

    public int DistanceTo(Cell other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    /// <summary>True when both cells are on the same row or column ("cast in line").</summary>
    public bool IsInLineWith(Cell other) => X == other.X || Y == other.Y;

    public IEnumerable<Cell> Neighbours()
    {
        foreach (Cell step in Steps)
            yield return this + step;
    }

    public override string ToString() => $"({X}, {Y})";
}
