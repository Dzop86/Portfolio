using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>Puddles and ponds with rounded shores (sprint 179, D90).</summary>
public class WatersTests
{
    private static Func<Cell, bool> Pond(params Cell[] cells)
    {
        HashSet<Cell> set = [.. cells];
        return set.Contains;
    }

    [Fact]
    public void ALonePuddle_IsRound_AndLeavesABankAllAround()
    {
        IReadOnlyList<(float X, float Z)> o = Waters.Outline(Pond(new Cell(0, 0)), new Cell(0, 0));
        Assert.Equal(4 * (Waters.Segments + 1), o.Count);
        // Inside the shore everywhere: the bank shows on every side.
        Assert.All(o, p => Assert.True(Math.Abs(p.X) <= Waters.Shore + 1e-4 && Math.Abs(p.Z) <= Waters.Shore + 1e-4));
        // Its corners cut: no point near the square's corners.
        Assert.All(o, p => Assert.True(MathF.Sqrt(p.X * p.X + p.Z * p.Z) < Waters.Shore * 1.3f));
        // Some points on each side, at the shore.
        Assert.Contains(o, p => MathF.Abs(p.X - Waters.Shore) < 1e-4);
        Assert.Contains(o, p => MathF.Abs(p.Z + Waters.Shore) < 1e-4);
    }

    [Fact]
    public void TheOutline_GoesRoundOnce_WithoutTurningBack()
    {
        foreach (Func<Cell, bool> pond in new[] { Pond(new Cell(0, 0)), Pond(new Cell(0, 0), new Cell(1, 0)), Pond(new Cell(0, 0), new Cell(0, 1), new Cell(1, 0)) })
        {
            IReadOnlyList<(float X, float Z)> o = Waters.Outline(pond, new Cell(0, 0));
            // Convex, always turning the same way: a fan from the middle covers it.
            int sign = 0;
            for (int i = 0; i < o.Count; i++)
            {
                (float ax, float az) = o[i];
                (float bx, float bz) = o[(i + 1) % o.Count];
                (float cx, float cz) = o[(i + 2) % o.Count];
                float cross = (bx - ax) * (cz - bz) - (bz - az) * (cx - bx);
                if (MathF.Abs(cross) < 1e-6)
                    continue;
                if (sign == 0)
                    sign = Math.Sign(cross);
                Assert.Equal(sign, Math.Sign(cross));
            }
        }
    }

    [Fact]
    public void WhereTheWaterGoesOn_TheSideReachesTheEdge_AndMeetsItsNeighbour()
    {
        Func<Cell, bool> pond = Pond(new Cell(0, 0), new Cell(1, 0));
        IReadOnlyList<(float X, float Z)> left = Waters.Outline(pond, new Cell(0, 0));
        IReadOnlyList<(float X, float Z)> right = Waters.Outline(pond, new Cell(1, 0));
        // Square on the shared side, at the edge: both cells end at x = ±0.5, at the shore north and south.
        Assert.Contains(left, p => p == (0.5f, -Waters.Shore));
        Assert.Contains(left, p => p == (0.5f, Waters.Shore));
        Assert.Contains(right, p => p == (-0.5f, -Waters.Shore));
        Assert.Contains(right, p => p == (-0.5f, Waters.Shore));
        Assert.Equal(2 * (Waters.Segments + 1) + 2, left.Count);
    }

    [Fact]
    public void ReedsGrowOnTheShore_AndLiliesFloatOnTheWater_NeverElsewhere()
    {
        HashSet<Cell> water = [.. Enumerable.Range(0, 30).SelectMany(x => Enumerable.Range(0, 30).Select(y => new Cell(x, y))).Where(c => (c.X / 6 + c.Y / 6) % 2 == 0)];
        Func<Cell, bool> isWater = water.Contains;
        Cell[] all = [.. Enumerable.Range(-1, 32).SelectMany(x => Enumerable.Range(-1, 32).Select(y => new Cell(x, y)))];
        Cell[] reeds = [.. all.Where(c => Waters.Reeds(isWater, c, _ => true))];
        Cell[] lilies = [.. all.Where(c => Waters.Lily(isWater, c, _ => false))];
        Assert.NotEmpty(reeds);
        Assert.NotEmpty(lilies);
        Assert.All(reeds, c => Assert.DoesNotContain(c, water));
        Assert.All(reeds, c => Assert.Contains(new[] { new Cell(1, 0), new Cell(-1, 0), new Cell(0, 1), new Cell(0, -1) }, d => water.Contains(c + d)));
        Assert.All(lilies, c => Assert.Contains(c, water));
        Assert.DoesNotContain(all, c => Waters.Lily(isWater, c, _ => true));
        Assert.DoesNotContain(all, c => Waters.Reeds(isWater, c, _ => false));
    }
}
