using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The ground's heights out of a fight (sprint 179, D90): gentle hills in the woods, the mouldy woods and the Forest
/// of the Ancestors, flat fields, lake and villages. One height field for the whole world, in the world's
/// coordinates (a map is 48 × 32 cells at its <see cref="Town.MapAt"/>), so a map and the skirt of its neighbour
/// meet without a seam; each zone's strength is blended between the maps' middles, so a wood slopes down to the
/// fields beside it. Flattened by water and buildings. Decorative only: the rules, the paths and the fights never
/// see it. Heights are given at the cells' corners (a cell is centred on its integer position) and interpolated.
/// </summary>
public sealed class Relief
{
    public const int MapWidth = 48;
    public const int MapHeight = 32;

    /// <summary>How far from water or a building the ground starts to rise again, in cells.</summary>
    public const float FlatRadius = 6;

    /// <summary>The width of the broad hills, and of the small bumps on them, in cells.</summary>
    public const float Hills = 9, Bumps = 4;

    private readonly int _w, _h, _margin;
    private readonly float[,] _corners;

    /// <summary>The heights around a map: over it and <paramref name="margin"/> cells past each edge (its skirt).</summary>
    public Relief(GameData data, Town map, int margin = MapSkirt.Depth)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(map);
        _w = map.Width;
        _h = map.Rows.Count;
        _margin = margin;
        _corners = new float[_w + 2 * margin + 1, _h + 2 * margin + 1];
        if (map.IsHall)
            return;
        Dictionary<Cell, Town> world = [];
        foreach (Town t in MapPhotos.World(data))
            world.TryAdd(t.MapAt, t);
        // What stands on a cell around the map, read on the map itself or on its neighbour (not the skirt's
        // simplified kinds: a neighbour's house flattens its ground too).
        char Kind(int x, int y)
        {
            int mx = FloorDiv(x, MapWidth), my = FloorDiv(y, MapHeight);
            if (mx == 0 && my == 0)
                return map.Rows[y][x];
            return world.TryGetValue(map.MapAt + new Cell(mx, my), out Town? t) && t.Rows.Count == MapHeight && t.Width == MapWidth
                ? t.Rows[y - my * MapHeight][x - mx * MapWidth]
                : '.';
        }
        float[,] flat = FlatDistance(Kind);
        for (int cy = 0; cy <= _h + 2 * margin; cy++)
        {
            for (int cx = 0; cx <= _w + 2 * margin; cx++)
            {
                // The corner between cells (x − 1, y − 1) and (x, y), in the world's coordinates.
                int x = cx - margin, y = cy - margin;
                float gx = map.MapAt.X * MapWidth + x - 0.5f, gy = map.MapAt.Y * MapHeight + y - 0.5f;
                float amp = Strength(world, gx, gy);
                if (amp <= 0)
                    continue;
                // The cells around the corner, in the distance field that reaches a radius further out.
                int fx = cx + Reach, fy = cy + Reach;
                float near = Math.Min(Math.Min(flat[fx, fy], flat[fx - 1, fy]), Math.Min(flat[fx, fy - 1], flat[fx - 1, fy - 1]));
                float t = Math.Clamp(near / FlatRadius, 0, 1);
                float ease = t * t * (3 - 2 * t);
                _corners[cx, cy] = amp * ease * Shape(gx, gy);
            }
        }
    }

    /// <summary>The highest a zone's hills rise, in cells: none in the fields, by the lake or in the villages.</summary>
    public static float Amplitude(string? zone) => zone switch
    {
        "woods" => 1.7f,
        "mouldy" => 1.3f,
        "ancestors" => 2.2f,
        _ => 0,
    };

    /// <summary>The height at a corner of a cell (the corner at <c>(x − ½, y − ½)</c>), in the map's coordinates.</summary>
    public float Corner(int x, int y)
    {
        int cx = Math.Clamp(x + _margin, 0, _w + 2 * _margin), cy = Math.Clamp(y + _margin, 0, _h + 2 * _margin);
        return _corners[cx, cy];
    }

    /// <summary>The height at any point of the ground, in the map's coordinates (cell centres on integers).</summary>
    public float At(float x, float z)
    {
        float fx = x + 0.5f, fz = z + 0.5f;
        int x0 = (int)MathF.Floor(fx), z0 = (int)MathF.Floor(fz);
        float u = fx - x0, v = fz - z0;
        float top = Corner(x0, z0) + (Corner(x0 + 1, z0) - Corner(x0, z0)) * u;
        float bottom = Corner(x0, z0 + 1) + (Corner(x0 + 1, z0 + 1) - Corner(x0, z0 + 1)) * u;
        return top + (bottom - top) * v;
    }

    /// <summary>The height at a cell's middle.</summary>
    public float At(Cell c) => At(c.X, c.Y);

    /// <summary>Whether any ground around this map rises at all.</summary>
    public bool Flat
    {
        get
        {
            foreach (float h in _corners)
            {
                if (h > 0)
                    return false;
            }
            return true;
        }
    }

    /// <summary>The broad hills and the small bumps on them: from 0 to 1, smooth, the same at a point of the world for every map.</summary>
    public static float Shape(float gx, float gy) =>
        Math.Clamp(0.75f * Noise(gx / Hills, gy / Hills, 0x41) + 0.25f * Noise(gx / Bumps, gy / Bumps, 0x42), 0, 1);

    /// <summary>A zone's strength at a point of the world, blended between the middles of the maps around it.</summary>
    private static float Strength(Dictionary<Cell, Town> world, float gx, float gy)
    {
        float u = gx / MapWidth - 0.5f, v = gy / MapHeight - 0.5f;
        int mx = (int)MathF.Floor(u), my = (int)MathF.Floor(v);
        float fu = Smooth(u - mx), fv = Smooth(v - my);
        float A(int x, int y) => world.TryGetValue(new Cell(x, y), out Town? t) && !t.IsHall ? Amplitude(t.Zone) : 0;
        float top = A(mx, my) + (A(mx + 1, my) - A(mx, my)) * fu;
        float bottom = A(mx, my + 1) + (A(mx + 1, my + 1) - A(mx, my + 1)) * fu;
        return top + (bottom - top) * fv;
    }

    // How much further out than the skirt the distance to water and buildings is measured, so that a corner of the
    // skirt sees everything within the radius, as the neighbour itself does.
    private static readonly int Reach = (int)MathF.Ceiling(FlatRadius) + 1;

    /// <summary>
    /// How far each cell around the map is from water or a building (in cells, by steps of eight, up to the radius):
    /// index <c>i</c> is the cell <c>i − margin − Reach</c>.
    /// </summary>
    private float[,] FlatDistance(Func<int, int, char> kind)
    {
        int offset = _margin + Reach;
        int width = _w + 2 * offset, height = _h + 2 * offset;
        var d = new float[width, height];
        var queue = new Queue<(int X, int Y)>();
        for (int iy = 0; iy < height; iy++)
        {
            for (int ix = 0; ix < width; ix++)
            {
                d[ix, iy] = FlatRadius;
                if (Flattens(kind(ix - offset, iy - offset)))
                {
                    d[ix, iy] = 0;
                    queue.Enqueue((ix, iy));
                }
            }
        }
        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    float step = dx != 0 && dy != 0 ? 1.41f : 1;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height || d[nx, ny] <= d[x, y] + step)
                        continue;
                    d[nx, ny] = d[x, y] + step;
                    queue.Enqueue((nx, ny));
                }
            }
        }
        return d;
    }

    /// <summary>Water, roads and buildings sit on flat ground: around them the hills ease down to nothing.</summary>
    public static bool Flattens(char kind) => kind is '~' or '=' or 'H' or 'h' or 'K' or 'k' or 'F' or 'f' or 'Q' or 'q' or 'E' or 'e' or 'S' or 'C' or 'P' or 'X';

    private static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    /// <summary>Value noise: a hash at each integer point, smoothly interpolated; from 0 to 1.</summary>
    private static float Noise(float x, float y, uint salt)
    {
        int x0 = (int)MathF.Floor(x), y0 = (int)MathF.Floor(y);
        float fx = Smooth(x - x0), fy = Smooth(y - y0);
        float P(int i, int j) => DecorScatter.Unit(new Cell(i, j), salt);
        float top = P(x0, y0) + (P(x0 + 1, y0) - P(x0, y0)) * fx;
        float bottom = P(x0, y0 + 1) + (P(x0 + 1, y0 + 1) - P(x0, y0 + 1)) * fx;
        return top + (bottom - top) * fy;
    }

    private static float Smooth(float t) => t * t * (3 - 2 * t);
}
