namespace Rogue.Core;

/// <summary>
/// What the player sees: every tile within <see cref="Radius"/> whose straight line (Bresenham)
/// from the player crosses no wall. Walls themselves are seen, which outlines the rooms.
/// </summary>
public static class FieldOfView
{
    public const int Radius = 6;

    public static bool[] Compute(Map map, Point origin)
    {
        ArgumentNullException.ThrowIfNull(map);
        var visible = new bool[map.Width * map.Height];
        for (int y = origin.Y - Radius; y <= origin.Y + Radius; y++)
            for (int x = origin.X - Radius; x <= origin.X + Radius; x++)
            {
                var p = new Point(x, y);
                int dx = x - origin.X, dy = y - origin.Y;
                if (map.InBounds(p) && dx * dx + dy * dy <= Radius * Radius && LineIsClear(map, origin, p))
                    visible[map.Index(p)] = true;
            }
        return visible;
    }

    /// <summary>True when no tile strictly between the two ends of the line is a wall.</summary>
    internal static bool LineIsClear(Map map, Point from, Point to)
    {
        int dx = Math.Abs(to.X - from.X), sx = Math.Sign(to.X - from.X);
        int dy = -Math.Abs(to.Y - from.Y), sy = Math.Sign(to.Y - from.Y);
        int error = dx + dy;
        int x = from.X, y = from.Y;
        while (true)
        {
            if ((x, y) == (to.X, to.Y))
                return true;
            if ((x, y) != (from.X, from.Y) && map[new Point(x, y)] == Tile.Wall)
                return false;
            int e2 = 2 * error;
            if (e2 >= dy)
            {
                error += dy;
                x += sx;
            }
            if (e2 <= dx)
            {
                error += dx;
                y += sy;
            }
        }
    }
}
