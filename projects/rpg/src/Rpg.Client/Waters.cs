using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The shape of the water out of a fight (sprint 179, D90 : « les flaques ne sont pas belles »): no more square
/// river tiles, but puddles and ponds with rounded shores. Each water cell is a polygon around its middle: its
/// sides that face land drawn back a little, so that a muddy bank shows, its corners rounded where both sides
/// meeting there face land; where the water goes on, its side reaches the cell's edge and meets its neighbour's.
/// Reeds grow on the land along the shore, water lilies float on the water. Pure: the view draws it.
/// </summary>
public static class Waters
{
    /// <summary>How far a side facing land stands from the middle of its cell (half a cell where the water goes on).</summary>
    public const float Shore = 0.42f;

    /// <summary>The radius of a rounded corner, in cells.</summary>
    public const float Round = 0.32f;

    /// <summary>How many segments a rounded corner is cut into.</summary>
    public const int Segments = 5;

    /// <summary>
    /// The outline of a water cell, around its middle, counter-clockwise seen from above (x east, z south): a point
    /// list, convex, to be drawn as a fan from the middle.
    /// </summary>
    public static IReadOnlyList<(float X, float Z)> Outline(Func<Cell, bool> isWater, Cell c)
    {
        ArgumentNullException.ThrowIfNull(isWater);
        bool W(int dx, int dz) => isWater(new Cell(c.X + dx, c.Y + dz));
        // How far each side reaches: east, south, west, north.
        float east = W(1, 0) ? 0.5f : Shore, west = W(-1, 0) ? 0.5f : Shore;
        float south = W(0, 1) ? 0.5f : Shore, north = W(0, -1) ? 0.5f : Shore;
        List<(float, float)> points = [];
        // The four corners in turn, from north-east round by north-west (counter-clockwise seen from above,
        // with z growing southwards: NE, NW, SW, SE).
        foreach ((int sx, int sz) in new[] { (1, -1), (-1, -1), (-1, 1), (1, 1) })
        {
            float hx = sx > 0 ? east : west, hz = sz > 0 ? south : north;
            bool landX = !W(sx, 0), landZ = !W(0, sz);
            if (landX && landZ)
            {
                // Both sides meeting here face land: an arc around a point drawn in from the corner.
                float cx = sx * (hx - Round), cz = sz * (hz - Round);
                float start = MathF.Atan2(0, sx), end = MathF.Atan2(sz, 0);
                // From the side along x (angle of +x or −x) to the side along z, the short way round.
                float sweep = end - start;
                while (sweep > MathF.PI)
                    sweep -= MathF.Tau;
                while (sweep < -MathF.PI)
                    sweep += MathF.Tau;
                // Walk the corner in the polygon's direction: from the z side to the x side, or back.
                bool fromZ = OrderFromZ(sx, sz);
                for (int i = 0; i <= Segments; i++)
                {
                    float t = (float)i / Segments;
                    float a = fromZ ? end - sweep * t : start + sweep * t;
                    points.Add((cx + Round * MathF.Cos(a), cz + Round * MathF.Sin(a)));
                }
            }
            else
            {
                points.Add((sx * hx, sz * hz));
            }
        }
        return points;
    }

    // In the order NE, NW, SW, SE the outline comes into a corner along one side and leaves along the other:
    // into NE from the east side (going north), into NW from the north side, into SW from the west, into SE from the south.
    private static bool OrderFromZ(int sx, int sz) => (sx, sz) is (-1, -1) or (1, 1);

    /// <summary>Whether a land cell along the shore has a clump of reeds (about one in three).</summary>
    public static bool Reeds(Func<Cell, bool> isWater, Cell c, Func<Cell, bool> isGrass)
    {
        ArgumentNullException.ThrowIfNull(isWater);
        ArgumentNullException.ThrowIfNull(isGrass);
        if (isWater(c) || !isGrass(c) || DecorScatter.Hash(c, 0x4EED) % 3 != 0)
            return false;
        return isWater(c + new Cell(1, 0)) || isWater(c + new Cell(-1, 0)) || isWater(c + new Cell(0, 1)) || isWater(c + new Cell(0, -1));
    }

    /// <summary>Whether a water cell carries a water lily (about one in five, never on a cell <paramref name="taken"/> says holds something).</summary>
    public static bool Lily(Func<Cell, bool> isWater, Cell c, Func<Cell, bool> taken)
    {
        ArgumentNullException.ThrowIfNull(isWater);
        ArgumentNullException.ThrowIfNull(taken);
        return isWater(c) && !taken(c) && DecorScatter.Hash(c, 0x1111) % 5 == 0;
    }
}
