namespace Rpg.Core.Tests;

public class LineOfSightTests
{
    [Fact]
    public void Neighbours_HaveNothingBetweenThem()
    {
        foreach (Cell n in new Cell(4, 4).Neighbours())
            Assert.Empty(LineOfSight.Trace(new Cell(4, 4), n));
    }

    [Fact]
    public void Diagonal_PassesThroughCornersExactly()
    {
        Assert.Equal(
            [new LineStep(new Cell(1, 0), new Cell(0, 1)), new LineStep(new Cell(1, 1)), new LineStep(new Cell(2, 1), new Cell(1, 2))],
            LineOfSight.Trace(new Cell(0, 0), new Cell(2, 2)));
    }

    [Fact]
    public void KnightMove_CrossesTwoCells()
    {
        // From (0.5, 0.5) to (2.5, 1.5): x = 1 at y = 0.75, y = 1 at x = 1.5, x = 2 at y = 1.25.
        Assert.Equal([new LineStep(new Cell(1, 0)), new LineStep(new Cell(1, 1))], LineOfSight.Trace(new Cell(0, 0), new Cell(2, 1)));
    }

    [Fact]
    public void ACornerIsBlockedOnlyWhenBothSidesBlock()
    {
        var one = new HashSet<Cell> { new(1, 0) };
        var both = new HashSet<Cell> { new(1, 0), new(0, 1) };
        Assert.True(LineOfSight.IsClear(new Cell(0, 0), new Cell(2, 2), one.Contains));
        Assert.False(LineOfSight.IsClear(new Cell(0, 0), new Cell(2, 2), both.Contains));
        Assert.False(LineOfSight.IsClear(new Cell(0, 0), new Cell(2, 2), new HashSet<Cell> { new(1, 1) }.Contains));
    }

    [Fact]
    public void TheEndsNeverBlock()
    {
        var ends = new HashSet<Cell> { new(0, 0), new(5, 3) };
        Assert.True(LineOfSight.IsClear(new Cell(0, 0), new Cell(5, 3), ends.Contains));
    }

    /// <summary>A sees B exactly when B sees A: the same cells are crossed both ways.</summary>
    [Fact]
    public void Trace_IsTheSameInBothDirections()
    {
        var rng = new Rng(7);
        for (int i = 0; i < 3000; i++)
        {
            Cell a = new(rng.Next(-9, 9), rng.Next(-9, 9)), b = new(rng.Next(-9, 9), rng.Next(-9, 9));
            Assert.Equal(Normalise(LineOfSight.Trace(a, b)), Normalise(LineOfSight.Trace(b, a)));
        }
    }

    /// <summary>
    /// Against geometry: sampling the segment finely finds the same cells (corners aside, which the
    /// segment only touches). Crossings are at least 1 / (2 nx ny) apart, far more than the step.
    /// </summary>
    [Fact]
    public void Trace_MatchesASampledSegment()
    {
        var rng = new Rng(11);
        for (int i = 0; i < 500; i++)
        {
            Cell a = new(rng.Next(0, 14), rng.Next(0, 14)), b = new(rng.Next(0, 14), rng.Next(0, 14));
            var sampled = new HashSet<Cell>();
            const int n = 20011;
            for (int k = 0; k < n; k++)
            {
                double t = (k + 0.5) / n;
                double x = a.X + 0.5 + (b.X - a.X) * t, y = a.Y + 0.5 + (b.Y - a.Y) * t;
                if (Math.Abs(x - Math.Round(x)) < 1e-9 || Math.Abs(y - Math.Round(y)) < 1e-9)
                    continue;
                sampled.Add(new Cell((int)Math.Floor(x), (int)Math.Floor(y)));
            }
            sampled.Remove(a);
            sampled.Remove(b);
            var traced = LineOfSight.Trace(a, b).Where(s => s.Other is null).Select(s => s.Cell).ToHashSet();
            Assert.True(sampled.SetEquals(traced), $"{a} -> {b}");
        }
    }

    private static List<string> Normalise(IReadOnlyList<LineStep> steps) =>
        [.. steps.Select(s => s.Other is Cell o ? string.Join('+', new[] { s.Cell, o }.OrderBy(c => c.X).ThenBy(c => c.Y)) : s.Cell.ToString()).Order(StringComparer.Ordinal)];
}
