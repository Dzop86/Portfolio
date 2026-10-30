using Rpg.Core;

namespace Rpg.Client;

/// <summary>A point of the world's picture (<see cref="WorldPicture"/>'s pixels).</summary>
public readonly record struct AtlasPoint(double X, double Y);

/// <summary>A zone of the atlas: its outlines (closed, one or more), its colour's key and where its name goes.</summary>
public sealed record AtlasRegion(string Zone, IReadOnlyList<IReadOnlyList<AtlasPoint>> Outlines, AtlasPoint Centre, int MinLevel, int MaxLevel);

/// <summary>A stamp of trees on the atlas: where, and which of the three drawings.</summary>
public readonly record struct AtlasTree(AtlasPoint At, int Variant);

/// <summary>
/// The world map seen as an illustrated atlas (sprint 167, D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §5.4), without
/// any engine, in the pixels of <see cref="WorldPicture"/> so that both lie on each other: each zone a region whose outline
/// waves instead of following the maps' edges, the land spilling a quarter of a map past the maps so that the coast never
/// hugs them, the sea all around; the water of the maps as smoothed pools, their trees as stamps, their roads as dots.
/// Deterministic: the same world gives the same shapes.
/// </summary>
public sealed class WorldAtlas
{
    /// <summary>The cells between two samples of the zones' field.</summary>
    public const int Step = 4;

    /// <summary>How far the land spills past the maps, in cells: a quarter of a map's width.</summary>
    public const int Spill = 12;

    /// <summary>The most an outline waves off its place, in cells (≤ 3, s165 §5.4).</summary>
    public const double Wave = 2.5;

    private readonly Town[] _world;
    private readonly Cell _origin;
    private readonly int _mapWidth, _mapHeight;

    public WorldAtlas(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _world = [.. data.Towns.Values.Where(t => !t.IsHall)];
        if (_world.Length == 0)
            throw new ArgumentException("A world without maps has no atlas.", nameof(data));
        // The geometry of WorldPicture: every map the room of the largest, edge to edge.
        _mapWidth = _world.Max(t => t.Width);
        _mapHeight = _world.Max(t => t.Rows.Count);
        _origin = new Cell(_world.Min(t => t.MapAt.X), _world.Min(t => t.MapAt.Y));
        Width = (_world.Max(t => t.MapAt.X) - _origin.X + 1) * _mapWidth * WorldPicture.CellPixels;
        Height = (_world.Max(t => t.MapAt.Y) - _origin.Y + 1) * _mapHeight * WorldPicture.CellPixels;
        Regions = BuildRegions();
        Waters = BuildWaters();
        Trees = BuildTrees();
        Roads = BuildRoads();
    }

    /// <summary>The maps' extent in pixels; the land spills past it, the sea around it.</summary>
    public int Width { get; }
    public int Height { get; }

    public IReadOnlyList<AtlasRegion> Regions { get; }

    /// <summary>The maps' water as closed outlines.</summary>
    public IReadOnlyList<IReadOnlyList<AtlasPoint>> Waters { get; }

    public IReadOnlyList<AtlasTree> Trees { get; }

    /// <summary>The dots of the roads.</summary>
    public IReadOnlyList<AtlasPoint> Roads { get; }

    /// <summary>A map's rectangle in pixels.</summary>
    public (double Left, double Top, double Right, double Bottom) Bounds(Town map)
    {
        ArgumentNullException.ThrowIfNull(map);
        double left = (map.MapAt.X - _origin.X) * _mapWidth * WorldPicture.CellPixels, top = (map.MapAt.Y - _origin.Y) * _mapHeight * WorldPicture.CellPixels;
        return (left, top, left + _mapWidth * WorldPicture.CellPixels, top + _mapHeight * WorldPicture.CellPixels);
    }

    /// <summary>The zone the land at a pixel belongs to: its map's, or within the spill the nearest map's; null at sea.</summary>
    public string? ZoneAt(double x, double y)
    {
        double spill = Spill * WorldPicture.CellPixels;
        Town? best = null;
        double nearest = double.MaxValue;
        foreach (Town t in _world)
        {
            (double l, double top, double r, double b) = Bounds(t);
            double dx = Math.Max(0, Math.Max(l - x, x - r)), dy = Math.Max(0, Math.Max(top - y, y - b));
            double d = Math.Max(dx, dy);
            if (d < nearest)
                (nearest, best) = (d, t);
        }
        return nearest <= spill ? Zone(best!) : null;
    }

    /// <summary>A map's zone; the village's maps have none of their own: Clairval's.</summary>
    public static string Zone(Town map) => (map ?? throw new ArgumentNullException(nameof(map))).Zone ?? "clairval";

    private List<AtlasRegion> BuildRegions()
    {
        double px = Step * WorldPicture.CellPixels;
        // The field of zones, sampled from a step beyond the spill on each side.
        int pad = Spill / Step + 2;
        int cols = Width / (int)px + 2 * pad + 1, rows = Height / (int)px + 2 * pad + 1;
        var field = new string?[cols, rows];
        for (int i = 0; i < cols; i++)
            for (int j = 0; j < rows; j++)
                field[i, j] = ZoneAt((i - pad) * px, (j - pad) * px);
        var regions = new List<AtlasRegion>();
        foreach (IGrouping<string, Town> zone in _world.GroupBy(Zone).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            List<List<AtlasPoint>> loops = Contours(cols, rows, (i, j) => field[i, j] == zone.Key);
            var outlines = loops.Select(l => (IReadOnlyList<AtlasPoint>)Smooth([.. l.Select(p => new AtlasPoint((p.X - pad) * px, (p.Y - pad) * px))], 2, true)).ToList();
            // Its name at the centre of its maps.
            var centres = zone.Select(t => Bounds(t)).Select(b => new AtlasPoint((b.Left + b.Right) / 2, (b.Top + b.Bottom) / 2)).ToList();
            var centre = new AtlasPoint(centres.Average(c => c.X), centres.Average(c => c.Y));
            regions.Add(new AtlasRegion(zone.Key, outlines, centre, zone.Min(t => t.Level), zone.Max(t => t.Level)));
        }
        return regions;
    }

    private List<IReadOnlyList<AtlasPoint>> BuildWaters()
    {
        // Every other cell of the maps: water where '~' is.
        const int step = 2;
        int cols = Width / WorldPicture.CellPixels / step + 3, rows = Height / WorldPicture.CellPixels / step + 3;
        var water = new bool[cols, rows];
        foreach (Town t in _world)
        {
            int ox = (t.MapAt.X - _origin.X) * _mapWidth, oy = (t.MapAt.Y - _origin.Y) * _mapHeight;
            for (int y = 0; y < t.Rows.Count; y += step)
                for (int x = 0; x < t.Width; x += step)
                    if (t[new Cell(x, y)] == '~')
                        water[(ox + x) / step + 1, (oy + y) / step + 1] = true;
        }
        double px = step * WorldPicture.CellPixels;
        // A pool of about five cells across or more: a puddle would only speckle the atlas.
        double least = Math.Pow(5 * WorldPicture.CellPixels, 2);
        return [.. Contours(cols, rows, (i, j) => water[i, j])
            .Select(l => (IReadOnlyList<AtlasPoint>)Smooth([.. l.Select(p => new AtlasPoint((p.X - 1) * px + px / 2, (p.Y - 1) * px + px / 2))], 1, false))
            .Where(l => Area(l) >= least)];
    }

    private List<AtlasTree> BuildTrees()
    {
        // A stamp for about every twelfth of a map in trees, on trees of the map, spread over it.
        var trees = new List<AtlasTree>();
        foreach (Town t in _world.OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            List<Cell> cells = [.. Enumerable.Range(0, t.Rows.Count).SelectMany(y => Enumerable.Range(0, t.Width).Select(x => new Cell(x, y))).Where(c => t[c] == 'T')];
            int count = Math.Min(8, (int)Math.Round(cells.Count * 12.0 / (t.Width * t.Rows.Count)));
            (double left, double top, _, _) = Bounds(t);
            for (int k = 0; k < count; k++)
            {
                Cell c = cells[(int)((k + 0.5) * cells.Count / count)];
                trees.Add(new AtlasTree(new AtlasPoint(left + (c.X + 0.5) * WorldPicture.CellPixels, top + (c.Y + 0.5) * WorldPicture.CellPixels), (int)(Hash(c.X, c.Y) * 3) % 3));
            }
        }
        return trees;
    }

    private List<AtlasPoint> BuildRoads()
    {
        // One road cell in three, a dot each.
        var dots = new List<AtlasPoint>();
        foreach (Town t in _world.OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            (double left, double top, _, _) = Bounds(t);
            for (int y = 0; y < t.Rows.Count; y++)
                for (int x = 0; x < t.Width; x++)
                    if (t[new Cell(x, y)] == '=' && (x + y) % 3 == 0)
                        dots.Add(new AtlasPoint(left + (x + 0.5) * WorldPicture.CellPixels, top + (y + 0.5) * WorldPicture.CellPixels));
        }
        return dots;
    }

    /// <summary>
    /// The closed outlines of where <paramref name="inside"/> holds, by marching squares on the samples (the edge between
    /// a sample in and one out at its middle), in sample units; the field's border must be outside.
    /// </summary>
    public static List<List<AtlasPoint>> Contours(int cols, int rows, Func<int, int, bool> inside)
    {
        ArgumentNullException.ThrowIfNull(inside);
        // Each square's segments, oriented so that the inside is on the left; chained by their starts.
        var next = new Dictionary<(int, int), (int, int)>();
        for (int i = 0; i < cols - 1; i++)
            for (int j = 0; j < rows - 1; j++)
            {
                bool a = inside(i, j), b = inside(i + 1, j), c = inside(i + 1, j + 1), d = inside(i, j + 1);
                // The middles of the square's edges, doubled to stay whole: top, right, bottom, left.
                (int, int) top = (2 * i + 1, 2 * j), right = (2 * i + 2, 2 * j + 1), bottom = (2 * i + 1, 2 * j + 2), left = (2 * i, 2 * j + 1);
                int index = (a ? 8 : 0) | (b ? 4 : 0) | (c ? 2 : 0) | (d ? 1 : 0);
                switch (index)
                {
                    case 1: next[bottom] = left; break;
                    case 2: next[right] = bottom; break;
                    case 3: next[right] = left; break;
                    case 4: next[top] = right; break;
                    case 5: next[top] = left; next[bottom] = right; break;
                    case 6: next[top] = bottom; break;
                    case 7: next[top] = left; break;
                    case 8: next[left] = top; break;
                    case 9: next[bottom] = top; break;
                    case 10: next[left] = bottom; next[right] = top; break;
                    case 11: next[right] = top; break;
                    case 12: next[left] = right; break;
                    case 13: next[bottom] = right; break;
                    case 14: next[left] = bottom; break;
                }
            }
        var loops = new List<List<AtlasPoint>>();
        var seen = new HashSet<(int, int)>();
        foreach ((int, int) start in next.Keys.OrderBy(k => k.Item2).ThenBy(k => k.Item1))
        {
            if (seen.Contains(start))
                continue;
            var loop = new List<AtlasPoint>();
            (int, int) at = start;
            while (seen.Add(at))
            {
                loop.Add(new AtlasPoint(at.Item1 / 2.0, at.Item2 / 2.0));
                if (!next.TryGetValue(at, out at))
                    break;
            }
            loops.Add(loop);
        }
        return loops;
    }

    /// <summary>A closed outline rounded by Chaikin's corner cutting, waved by a noise of the place when asked.</summary>
    private static List<AtlasPoint> Smooth(List<AtlasPoint> loop, int passes, bool wave)
    {
        if (wave)
        {
            double amplitude = Wave * WorldPicture.CellPixels;
            loop = [.. loop.Select(p => new AtlasPoint(p.X + amplitude * Noise(p.X, p.Y, 1), p.Y + amplitude * Noise(p.X, p.Y, 2)))];
        }
        for (int k = 0; k < passes; k++)
        {
            var cut = new List<AtlasPoint>(loop.Count * 2);
            for (int i = 0; i < loop.Count; i++)
            {
                AtlasPoint p = loop[i], q = loop[(i + 1) % loop.Count];
                cut.Add(new AtlasPoint(0.75 * p.X + 0.25 * q.X, 0.75 * p.Y + 0.25 * q.Y));
                cut.Add(new AtlasPoint(0.25 * p.X + 0.75 * q.X, 0.25 * p.Y + 0.75 * q.Y));
            }
            loop = cut;
        }
        return loop;
    }

    /// <summary>A smooth noise of the place, -1 to 1: two neighbouring zones wave their shared edge alike.</summary>
    private static double Noise(double x, double y, int seed)
    {
        const double cell = 96;
        double gx = x / cell, gy = y / cell;
        int x0 = (int)Math.Floor(gx), y0 = (int)Math.Floor(gy);
        double fx = gx - x0, fy = gy - y0;
        double Ease(double t) => t * t * (3 - 2 * t);
        double V(int i, int j) => Hash(i * 7 + seed * 131, j * 13 - seed * 17) * 2 - 1;
        double top = V(x0, y0) + (V(x0 + 1, y0) - V(x0, y0)) * Ease(fx);
        double bottom = V(x0, y0 + 1) + (V(x0 + 1, y0 + 1) - V(x0, y0 + 1)) * Ease(fx);
        return top + (bottom - top) * Ease(fy);
    }

    /// <summary>A number of [0, 1) fixed for two whole numbers.</summary>
    private static double Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u + (uint)y * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h ^ (h >> 16)) / 4294967296.0;
        }
    }

    /// <summary>The area a closed outline encloses (shoelace), in square pixels.</summary>
    public static double Area(IReadOnlyList<AtlasPoint> outline)
    {
        ArgumentNullException.ThrowIfNull(outline);
        double twice = 0;
        for (int i = 0, j = outline.Count - 1; i < outline.Count; j = i++)
            twice += (outline[j].X * outline[i].Y) - (outline[i].X * outline[j].Y);
        return Math.Abs(twice) / 2;
    }

    /// <summary>Whether a point is inside a closed outline (even-odd).</summary>
    public static bool Inside(IReadOnlyList<AtlasPoint> outline, AtlasPoint p)
    {
        ArgumentNullException.ThrowIfNull(outline);
        bool inside = false;
        for (int i = 0, j = outline.Count - 1; i < outline.Count; j = i++)
        {
            AtlasPoint a = outline[i], b = outline[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>The zone whose region holds a point, if any.</summary>
    public string? RegionAt(AtlasPoint p) => Regions.FirstOrDefault(r => r.Outlines.Any(o => Inside(o, p)))?.Zone;
}
