namespace Rpg.Core;

/// <summary>
/// SplitMix64 (Steele, Lea and Flood, 2014): its output is fixed by the seed on every platform and
/// .NET version, unlike <see cref="Random"/>. A fight replays identically only if every roll does.
/// </summary>
public sealed class Rng
{
    private ulong _state;

    public Rng(ulong seed) => _state = seed;

    public ulong NextUInt64()
    {
        ulong z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>A uniform integer in [<paramref name="min"/>, <paramref name="maxInclusive"/>], without modulo bias.</summary>
    public int Next(int min, int maxInclusive)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxInclusive, min);
        ulong bound = (ulong)(maxInclusive - min) + 1;
        // Draws below 2^64 mod bound would make the small values slightly more likely: reject them.
        ulong threshold = (0UL - bound) % bound;
        while (true)
        {
            ulong r = NextUInt64();
            if (r >= threshold)
                return min + (int)(r % bound);
        }
    }
}
