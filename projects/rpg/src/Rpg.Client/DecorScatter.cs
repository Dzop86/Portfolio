using Rpg.Core;

namespace Rpg.Client;

/// <summary>How one piece of decor stands in its cell: shifted from the middle, turned, scaled, and which model.</summary>
public readonly record struct Scatter(float Dx, float Dz, float Turn, float Scale, int Variant);

/// <summary>A small piece of decor that blocks nothing: a tuft of grass or a flower on a walkable cell, a bush on the rim of a grove.</summary>
public readonly record struct Sprig(Cell At, float Dx, float Dz, float Turn, int Variant);

/// <summary>
/// The world without its chequerboard (sprint 178, D87 retour 5, §7.2 A): every tree, rock, bush, mushroom
/// and flower shifted in its cell, turned and scaled, its model drawn among several by a hash of the cell
/// (never by parity any more); the ground's two tones in patches about six cells wide, a drier tone along the
/// roads; tufts on one walkable cell in eight, and bushes on the rim of the groves. Pure and deterministic:
/// the same cell always looks the same, on every machine, so the map one remembers does not move.
/// </summary>
public static class DecorScatter
{
    /// <summary>The farthest a piece is shifted from its cell's middle, in cells, on each axis.</summary>
    public const float MaxShift = 0.22f;

    public const float MinScale = 0.8f;
    public const float MaxScale = 1.25f;

    /// <summary>The width of the ground's patches, in cells.</summary>
    public const int Patch = 6;

    /// <summary>
    /// How many tones the ground's patches are cut into (one material each). Four, so that two neighbours are
    /// never more than one tone apart: the smoothed noise climbs at most a quarter of its range a cell.
    /// </summary>
    public const int Tones = 4;

    /// <summary>One walkable cell in this many has a tuft or a flower.</summary>
    public const int TuftEvery = 8;

    /// <summary>How far a grove's bush stands from the middle of its tree's cell, towards the grass.</summary>
    public const float RimShift = 0.38f;

    /// <summary>A hash of a cell and a salt, well mixed (the same on every machine).</summary>
    public static uint Hash(Cell c, uint salt)
    {
        unchecked
        {
            uint h = (uint)c.X * 0x9E3779B1u ^ (uint)c.Y * 0x85EBCA77u ^ salt * 0xC2B2AE3Du;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return h;
        }
    }

    /// <summary>A number in [0, 1) from a cell and a salt.</summary>
    public static float Unit(Cell c, uint salt) => (Hash(c, salt) >> 8) / 16777216f;

    /// <summary>How the piece of decor of this kind stands on this cell, its model one of <paramref name="variants"/>.</summary>
    public static Scatter For(Cell c, char kind, int variants)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(variants, 1);
        uint salt = kind;
        return new Scatter(
            (Unit(c, salt + 1) * 2 - 1) * MaxShift,
            (Unit(c, salt + 2) * 2 - 1) * MaxShift,
            Unit(c, salt + 3) * 360,
            MinScale + Unit(c, salt + 4) * (MaxScale - MinScale),
            (int)(Hash(c, salt + 5) % (uint)variants));
    }

    /// <summary>
    /// The ground's tone on a cell, from 0 (the zone's light grass) to <see cref="Tones"/> − 1 (its dark one):
    /// value noise on a lattice of <see cref="Patch"/> cells, smoothly interpolated, so that neighbours share
    /// almost the same tone and the patches have no edge.
    /// </summary>
    public static int Tone(Cell c, uint seed = 0)
    {
        int gx = FloorDiv(c.X, Patch), gy = FloorDiv(c.Y, Patch);
        float fx = Smooth((c.X - gx * Patch) / (float)Patch), fy = Smooth((c.Y - gy * Patch) / (float)Patch);
        float Corner(int x, int y) => Unit(new Cell(x, y), 0x5EED + seed);
        float top = Lerp(Corner(gx, gy), Corner(gx + 1, gy), fx);
        float bottom = Lerp(Corner(gx, gy + 1), Corner(gx + 1, gy + 1), fx);
        float v = Lerp(top, bottom, fy);
        return Math.Clamp((int)(v * Tones), 0, Tones - 1);
    }

    /// <summary>Whether a cell of grass is next to a road (the eight around it), where it is drier.</summary>
    public static bool Dry(Town town, Cell c)
    {
        ArgumentNullException.ThrowIfNull(town);
        if (town[c] is '=' or '~')
            return false;
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                Cell n = new(c.X + dx, c.Y + dy);
                if (n != c && Inside(town, n) && town[n] == '=')
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// The tufts of grass and the flowers on the walkable grass: one cell in <see cref="TuftEvery"/>, drawn by
    /// the hash, never on a cell <paramref name="taken"/> says holds something (a way, a door, someone, a spot).
    /// </summary>
    public static IReadOnlyList<Sprig> Tufts(Town town, Func<Cell, bool> taken, int variants)
    {
        ArgumentNullException.ThrowIfNull(town);
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentOutOfRangeException.ThrowIfLessThan(variants, 1);
        List<Sprig> tufts = [];
        for (int y = 0; y < town.Rows.Count; y++)
        {
            for (int x = 0; x < town.Rows[y].Length; x++)
            {
                Cell c = new(x, y);
                if (town[c] != '.' || Hash(c, 0x7AF7) % TuftEvery != 0 || taken(c) || c == town.Spawn)
                    continue;
                tufts.Add(new Sprig(c, (Unit(c, 0x7AF8) * 2 - 1) * 0.35f, (Unit(c, 0x7AF9) * 2 - 1) * 0.35f, Unit(c, 0x7AFA) * 360, (int)(Hash(c, 0x7AFB) % (uint)variants)));
            }
        }
        return tufts;
    }

    /// <summary>
    /// The bushes that blur a grove's edge: on a tree's cell next to walkable grass, one to three bushes, each
    /// pushed <see cref="RimShift"/> of a cell towards one grass neighbour. They stay on the tree's own cell, so
    /// they never stand on a walkable one. About one tree of the rim in two has some.
    /// </summary>
    public static IReadOnlyList<Sprig> Rim(Town town, int variants)
    {
        ArgumentNullException.ThrowIfNull(town);
        ArgumentOutOfRangeException.ThrowIfLessThan(variants, 1);
        List<Sprig> bushes = [];
        Cell[] sides = [new(0, -1), new(1, 0), new(0, 1), new(-1, 0)];
        for (int y = 0; y < town.Rows.Count; y++)
        {
            for (int x = 0; x < town.Rows[y].Length; x++)
            {
                Cell c = new(x, y);
                if (town[c] != 'T' || Hash(c, 0xB05) % 2 != 0)
                    continue;
                Cell[] open = [.. sides.Where(s => Inside(town, c + s) && town[c + s] is '.' or ',')];
                if (open.Length == 0)
                    continue;
                int count = 1 + (int)(Hash(c, 0xB06) % (uint)Math.Min(3, open.Length));
                int first = (int)(Hash(c, 0xB07) % (uint)open.Length);
                for (int i = 0; i < count; i++)
                {
                    Cell s = open[(first + i) % open.Length];
                    float slide = (Unit(c, 0xB08 + (uint)i) * 2 - 1) * 0.3f;
                    // Pushed towards the grass, slid along the side.
                    float dx = s.X * RimShift + (s.X == 0 ? slide : 0), dz = s.Y * RimShift + (s.Y == 0 ? slide : 0);
                    bushes.Add(new Sprig(c, dx, dz, Unit(c, 0xB10 + (uint)i) * 360, (int)(Hash(c, 0xB20 + (uint)i) % (uint)variants)));
                }
            }
        }
        return bushes;
    }

    private static bool Inside(Town town, Cell c) => c.Y >= 0 && c.Y < town.Rows.Count && c.X >= 0 && c.X < town.Rows[c.Y].Length;

    private static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    private static float Smooth(float t) => t * t * (3 - 2 * t);

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
