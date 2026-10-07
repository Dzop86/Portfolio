namespace Rogue.Core.Tests;

public class RngTests
{
    [Fact]
    public void NextUInt64_MatchesTheSplitMix64Reference()
    {
        // Values of Vigna's reference implementation (splitmix64.c) for the seed 1234567.
        var rng = new Rng(1234567);
        ulong[] expected = [6457827717110365317, 3203168211198807973, 9817491932198370423, 4593380528125082431, 16408922859458223821];
        Assert.Equal(expected, expected.Select(_ => rng.NextUInt64()));
    }

    [Fact]
    public void SameSeed_SameSequence()
    {
        var a = new Rng(42);
        var b = new Rng(42);
        Assert.Equal(Enumerable.Range(0, 100).Select(_ => a.Next(1000)), Enumerable.Range(0, 100).Select(_ => b.Next(1000)));
    }

    [Fact]
    public void Next_StaysInItsBoundsAndCoversThem()
    {
        var rng = new Rng(7);
        var counts = new int[6];
        for (int i = 0; i < 60_000; i++)
            counts[rng.Next(6)]++;
        // Each face of a die comes up about 10,000 times (standard deviation about 91).
        Assert.All(counts, c => Assert.InRange(c, 9_500, 10_500));
        Assert.All(Enumerable.Range(0, 1000).Select(_ => rng.Next(-1, 1)), v => Assert.InRange(v, -1, 1));
        Assert.Equal(5, rng.Next(5, 5));
    }

    [Fact]
    public void Next_RejectsEmptyRanges()
    {
        var rng = new Rng(0);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(3, 2));
    }
}
