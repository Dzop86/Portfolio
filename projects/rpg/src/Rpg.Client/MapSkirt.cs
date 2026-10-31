using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// A cell of the skirt drawn past a map's edge: where it lies (in the map's own coordinates, outside it),
/// what grows there, how deep in the skirt (1 next to the map, <see cref="MapSkirt.Depth"/> the farthest), and
/// the neighbour it is borrowed from (null where the skirt is made up).
/// </summary>
public readonly record struct SkirtCell(Cell At, char Kind, int Depth, string? From);

/// <summary>
/// The map no longer stops dead on the background (sprint 178, D87 retour 5, §7.2 B, Q2): past each edge the
/// client draws <see cref="Depth"/> cells of the neighbouring map (its ground and its decor only: nobody, no
/// monster, nothing to click), corners included; where the world has no map, a skirt in the zone's style (a
/// dense wood, hedges in the fields, reeds and water by the lake) that thickens outwards. Pure: the view fades it.
/// </summary>
public static class MapSkirt
{
    /// <summary>How many cells the skirt reaches past each edge.</summary>
    public const int Depth = 6;

    /// <summary>A decorative cliff, only in a made-up skirt, only this deep or deeper.</summary>
    public const char Cliff = '^';

    public const int CliffDepth = 4;

    /// <summary>The kinds of the neighbour that are drawn in a skirt; the rest (a house's other cells, a stall, a lantern...) become grass.</summary>
    private const string Drawn = ".=,~TRBMH";

    /// <summary>Every cell of the skirt around a map. None around a dungeon's hall, a place apart from the world.</summary>
    public static IReadOnlyList<SkirtCell> Around(GameData data, Town map)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(map);
        if (map.IsHall || map.Rows.Count == 0)
            return [];
        Dictionary<Cell, Town> world = [];
        foreach (Town t in MapPhotos.World(data))
            world.TryAdd(t.MapAt, t);
        int w = map.Width, h = map.Rows.Count;
        List<SkirtCell> skirt = [];
        for (int y = -Depth; y < h + Depth; y++)
        {
            for (int x = -Depth; x < w + Depth; x++)
            {
                if (x >= 0 && x < w && y >= 0 && y < h)
                    continue;
                int depth = Math.Max(Math.Max(-x, x - w + 1), Math.Max(-y, y - h + 1));
                Cell step = new(x < 0 ? -1 : x >= w ? 1 : 0, y < 0 ? -1 : y >= h ? 1 : 0);
                Cell c = new(x, y);
                if (world.TryGetValue(map.MapAt + step, out Town? next) && next.Rows.Count > 0)
                {
                    // The neighbour's cell: counted from its own far edge where it lies before us.
                    int nx = step.X switch { -1 => next.Width + x, 1 => x - w, _ => x };
                    int ny = step.Y switch { -1 => next.Rows.Count + y, 1 => y - h, _ => y };
                    char kind = nx >= 0 && nx < next.Width && ny >= 0 && ny < next.Rows.Count ? next.Rows[ny][nx] : '.';
                    skirt.Add(new SkirtCell(c, Drawn.Contains(kind, StringComparison.Ordinal) ? kind : '.', depth, next.Id));
                }
                else
                {
                    skirt.Add(new SkirtCell(c, MadeUp(map.ZoneId, c, depth), depth, null));
                }
            }
        }
        return skirt;
    }

    /// <summary>The skirt's cells past one side of the map only (left, top, right or bottom; corners excluded).</summary>
    public static IEnumerable<SkirtCell> Side(IEnumerable<SkirtCell> skirt, Town map, Cell side)
    {
        ArgumentNullException.ThrowIfNull(skirt);
        ArgumentNullException.ThrowIfNull(map);
        int w = map.Width, h = map.Rows.Count;
        return skirt.Where(s => side switch
        {
            { X: -1 } => s.At.X < 0 && s.At.Y >= 0 && s.At.Y < h,
            { X: 1 } => s.At.X >= w && s.At.Y >= 0 && s.At.Y < h,
            { Y: -1 } => s.At.Y < 0 && s.At.X >= 0 && s.At.X < w,
            _ => s.At.Y >= h && s.At.X >= 0 && s.At.X < w,
        });
    }

    /// <summary>
    /// A made-up cell where the world has no map: an obstacle more and more likely outwards (a third next to the
    /// map, nearly all at the far edge), of the zone's kind; from <see cref="CliffDepth"/> on, now and then a cliff.
    /// </summary>
    public static char MadeUp(string zone, Cell c, int depth)
    {
        float roll = DecorScatter.Unit(c, 0x5C1A7);
        float thick = 0.3f + 0.6f * (depth - 1) / (Depth - 1);
        if (roll >= thick)
            return zone == "fields" && DecorScatter.Unit(c, 0x5C1A8) < 0.15f ? ',' : '.';
        float pick = DecorScatter.Unit(c, 0x5C1A9);
        // Far out, decorative cliffs and hills (Q5: off the board, no effect on the game).
        if (depth >= CliffDepth && DecorScatter.Unit(c, 0x5C1AA) < 0.18f)
            return Cliff;
        return zone switch
        {
            "fields" => pick < 0.55f ? 'B' : pick < 0.85f ? 'T' : 'R',
            "lake" => pick < 0.4f ? '~' : pick < 0.8f ? 'T' : 'B',
            "mouldy" => pick < 0.6f ? 'T' : pick < 0.8f ? 'M' : 'R',
            _ => pick < 0.75f ? 'T' : pick < 0.88f ? 'R' : 'B',
        };
    }

    /// <summary>
    /// How much the skirt is faded at a distance from the map's edge, in cells (0 = as the map, 1 = the
    /// background): nothing at the edge, quickly at first, nearly all at <see cref="Depth"/>. The view darkens and
    /// greys the ground's corners by their distance, and a piece of decor by its cell's middle (depth − ½).
    /// </summary>
    public static float Fade(float away) => away <= 0 ? 0 : MathF.Min(0.95f, 0.95f * MathF.Pow(away / Depth, 0.8f));
}
