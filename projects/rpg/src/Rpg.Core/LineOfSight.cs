namespace Rpg.Core;

/// <summary>
/// One crossing of the segment between two cell centres: the cell entered, or, when the segment
/// passes exactly through a corner, the two cells on either side of that corner (<see cref="Other"/>).
/// </summary>
public readonly record struct LineStep(Cell Cell, Cell? Other = null);

/// <summary>
/// Line of sight between cell centres. The segment is followed exactly in integers (no rounding),
/// so the cells it crosses are the same in both directions: if A sees B, B sees A.
/// </summary>
public static class LineOfSight
{
    /// <summary>
    /// The cells strictly between <paramref name="from"/> and <paramref name="to"/> that the segment
    /// joining their centres crosses, in order. A corner passed exactly gives one step with both
    /// side cells; the diagonal cell after it is a step of its own.
    /// </summary>
    public static IReadOnlyList<LineStep> Trace(Cell from, Cell to)
    {
        int nx = Math.Abs(to.X - from.X), ny = Math.Abs(to.Y - from.Y);
        int sx = Math.Sign(to.X - from.X), sy = Math.Sign(to.Y - from.Y);
        var steps = new List<LineStep>(nx + ny);
        Cell current = from;
        int ix = 0, iy = 0;
        while (ix < nx || iy < ny)
        {
            // The segment meets the next vertical side at t = (2ix + 1) / 2nx and the next horizontal
            // one at t = (2iy + 1) / 2ny: compare them by cross-multiplying, exactly.
            long decision = (long)(2 * ix + 1) * ny - (long)(2 * iy + 1) * nx;
            if (decision == 0)
            {
                steps.Add(new LineStep(new Cell(current.X + sx, current.Y), new Cell(current.X, current.Y + sy)));
                current = new Cell(current.X + sx, current.Y + sy);
                ix++;
                iy++;
            }
            else if (decision < 0)
            {
                current = new Cell(current.X + sx, current.Y);
                ix++;
            }
            else
            {
                current = new Cell(current.X, current.Y + sy);
                iy++;
            }
            if (current != to)
                steps.Add(new LineStep(current));
        }
        return steps;
    }

    /// <summary>
    /// True when nothing between the two cells blocks the view. A corner passed exactly is blocked
    /// only when both cells beside it block: a line grazing a single pillar still passes.
    /// </summary>
    public static bool IsClear(Cell from, Cell to, Func<Cell, bool> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        foreach (LineStep step in Trace(from, to))
        {
            bool blocked = step.Other is Cell other ? blocks(step.Cell) && blocks(other) : blocks(step.Cell);
            if (blocked)
                return false;
        }
        return true;
    }
}
