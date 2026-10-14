using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The world seen from the sky (sprint 71): every map laid in its place (<see cref="Town.MapAt"/>),
/// edge to edge, as one big map, each cell a tile of <see cref="CellPixels"/> × <see cref="CellPixels"/>
/// pixels drawn after what stands on it: grass in checks of the zone's colour, roads, trees seen from
/// above, water, rocks, roofs, the fountain, mushrooms, bushes, flowers. Computed without an engine, as RGBA bytes the client turns into a texture;
/// where no map is, the dark of the interface.
/// </summary>
public sealed class WorldPicture
{
    public const int CellPixels = 8;

    private readonly GameData _data;
    private readonly Cell _origin;

    public WorldPicture(GameData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.Towns.Count == 0)
            throw new ArgumentException("A world without maps has no picture.", nameof(data));
        // Every map gets the room of the largest one, so that neighbours meet edge to edge.
        MapWidth = data.Towns.Values.Max(t => t.Width);
        MapHeight = data.Towns.Values.Max(t => t.Rows.Count);
        _origin = new Cell(data.Towns.Values.Min(t => t.MapAt.X), data.Towns.Values.Min(t => t.MapAt.Y));
        Columns = data.Towns.Values.Max(t => t.MapAt.X) - _origin.X + 1;
        RowsOfMaps = data.Towns.Values.Max(t => t.MapAt.Y) - _origin.Y + 1;
        Width = Columns * MapWidth * CellPixels;
        Height = RowsOfMaps * MapHeight * CellPixels;
        Pixels = new byte[Width * Height * 4];
        Fill(0, 0, Width, Height, Void);
        foreach (Town t in data.Towns.Values)
            Draw(t);
    }

    /// <summary>The cells of the largest map, the room each map gets.</summary>
    public int MapWidth { get; }
    public int MapHeight { get; }

    /// <summary>How many maps across and down the world spans.</summary>
    public int Columns { get; }
    public int RowsOfMaps { get; }

    /// <summary>The picture's size in pixels.</summary>
    public int Width { get; }
    public int Height { get; }

    /// <summary>The picture, four bytes a pixel (red, green, blue, alpha), row after row from the top.</summary>
    public byte[] Pixels { get; }

    /// <summary>Where no map is.</summary>
    public static readonly Rgb Void = new(0x18, 0x18, 0x18);

    public static readonly Rgb Grass = new(0x6c, 0xbf, 0x4c), GrassDark = new(0x62, 0xb3, 0x44);
    public static readonly Rgb Road = new(0xe6, 0xe1, 0xd2);
    public static readonly Rgb Tree = new(0x2e, 0x7d, 0x46), TreeLight = new(0x4d, 0xa8, 0x63), Shadow = new(0x4e, 0x96, 0x3a);
    public static readonly Rgb Water = new(0x3b, 0x8c, 0xcf), WaterLight = new(0x67, 0xad, 0xe6);
    public static readonly Rgb Rock = new(0x9a, 0x9a, 0x96), RockLight = new(0xc4, 0xc4, 0xbf);
    public static readonly Rgb Roof = new(0x78, 0xd9, 0xd6), RoofEdge = new(0x3f, 0x95, 0x95);
    public static readonly Rgb Stone = new(0xd8, 0xd8, 0xd8);
    public static readonly Rgb Wood = new(0x9a, 0x67, 0x3c), Light = new(0xff, 0xd8, 0x5a);
    public static readonly Rgb Cap = new(0xc8, 0x4b, 0x3a), Spots = new(0xf4, 0xee, 0xe0), Bush = new(0x3f, 0x8f, 0x45), Petal = new(0xe8, 0x5d, 0x75);

    /// <summary>The colour of a pixel of the picture.</summary>
    public Rgb At(int x, int y)
    {
        int i = (y * Width + x) * 4;
        return new Rgb(Pixels[i], Pixels[i + 1], Pixels[i + 2]);
    }

    /// <summary>The top-left pixel of a map's room.</summary>
    public (int X, int Y) Corner(Town map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return ((map.MapAt.X - _origin.X) * MapWidth * CellPixels, (map.MapAt.Y - _origin.Y) * MapHeight * CellPixels);
    }

    /// <summary>The pixel at the centre of a cell of a map: where the player is marked.</summary>
    public (double X, double Y) PixelOf(string mapId, Cell cell)
    {
        (int x, int y) = Corner(_data.Towns[mapId]);
        return (x + (cell.X + 0.5) * CellPixels, y + (cell.Y + 0.5) * CellPixels);
    }

    /// <summary>The map under a pixel of the picture, if any (what the map names under the mouse).</summary>
    public Town? MapAtPixel(double x, double y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return null;
        var at = new Cell((int)(x / (MapWidth * CellPixels)) + _origin.X, (int)(y / (MapHeight * CellPixels)) + _origin.Y);
        Town? map = _data.Towns.Values.FirstOrDefault(t => t.MapAt == at);
        if (map is null)
            return null;
        (int cx, int cy) = Corner(map);
        // A map smaller than its room leaves a margin that is no map.
        return x - cx < map.Width * CellPixels && y - cy < map.Rows.Count * CellPixels ? map : null;
    }

    private void Draw(Town map)
    {
        (int ox, int oy) = Corner(map);
        for (int y = 0; y < map.Rows.Count; y++)
        {
            for (int x = 0; x < map.Width; x++)
                DrawCell(map, new Cell(x, y), ox + x * CellPixels, oy + y * CellPixels);
        }
    }

    private void DrawCell(Town map, Cell c, int px, int py)
    {
        const int P = CellPixels;
        char k = map[c];
        // Grass in checks, as the board is drawn, in the zone's colour, under everything that stands on grass.
        (Rgb light, Rgb dark) = ZoneStyle.Sky(map.Zone);
        Rgb ground = (c.X + c.Y) % 2 == 0 ? light : dark;
        switch (k)
        {
            case '=':
                Fill(px, py, P, P, Road);
                break;
            case '~':
                Fill(px, py, P, P, Water);
                Fill(px + 1, py + 2, 3, 1, WaterLight);
                Fill(px + 4, py + 5, 3, 1, WaterLight);
                break;
            case 'T':
                Fill(px, py, P, P, ground);
                Disc(px + 4.5, py + 4.5, 3.2, Shadow);
                Disc(px + 4, py + 4, 3.4, Tree);
                Disc(px + 3.2, py + 3.2, 1.4, TreeLight);
                break;
            case 'R':
                Fill(px, py, P, P, ground);
                Disc(px + 4, py + 4.4, 2.8, Rock);
                Disc(px + 3.4, py + 3.6, 1.2, RockLight);
                break;
            case 'H' or 'h':
                Fill(px, py, P, P, Roof);
                // The edge of the roof where the house ends.
                if (!IsHouse(map[c + new Cell(-1, 0)]))
                    Fill(px, py, 1, P, RoofEdge);
                if (!IsHouse(map[c + new Cell(1, 0)]))
                    Fill(px + P - 1, py, 1, P, RoofEdge);
                if (!IsHouse(map[c + new Cell(0, -1)]))
                    Fill(px, py, P, 1, RoofEdge);
                if (!IsHouse(map[c + new Cell(0, 1)]))
                    Fill(px, py + P - 1, P, 1, RoofEdge);
                break;
            case 'F' or 'f':
                Fill(px, py, P, P, Stone);
                Fill(px + 2, py + 2, P - 4, P - 4, Water);
                break;
            case 'S':
                Fill(px, py, P, P, ground);
                Fill(px + 1, py + 2, P - 2, P - 4, Roof);
                break;
            case 'L':
                Fill(px, py, P, P, ground);
                Disc(px + 4, py + 4, 1.6, Light);
                break;
            case 'C':
                Fill(px, py, P, P, ground);
                Fill(px + 1, py + 2, P - 2, P - 4, Wood);
                break;
            case 'M':
                Fill(px, py, P, P, ground);
                Disc(px + 4, py + 4, 3.2, Cap);
                Fill(px + 2, py + 3, 1, 1, Spots);
                Fill(px + 5, py + 2, 1, 1, Spots);
                Fill(px + 4, py + 5, 1, 1, Spots);
                break;
            case 'B':
                Fill(px, py, P, P, ground);
                Fill(px + 1, py + 1, P - 2, P - 2, Bush);
                Fill(px + 2, py + 2, 2, 2, TreeLight);
                break;
            case ',':
                Fill(px, py, P, P, ground);
                Fill(px + 1, py + 2, 1, 1, Light);
                Fill(px + 5, py + 1, 1, 1, Petal);
                Fill(px + 3, py + 5, 1, 1, Light);
                Fill(px + 6, py + 6, 1, 1, Petal);
                break;
            default:
                Fill(px, py, P, P, ground);
                break;
        }
    }

    private static bool IsHouse(char k) => k is 'H' or 'h';

    private void Fill(int x0, int y0, int w, int h, Rgb colour)
    {
        for (int y = Math.Max(0, y0); y < Math.Min(Height, y0 + h); y++)
        {
            for (int x = Math.Max(0, x0); x < Math.Min(Width, x0 + w); x++)
                Set(x, y, colour);
        }
    }

    /// <summary>A disc: the pixels whose centre is within <paramref name="radius"/> of (cx, cy).</summary>
    private void Disc(double cx, double cy, double radius, Rgb colour)
    {
        for (int y = (int)Math.Floor(cy - radius); y <= (int)Math.Ceiling(cy + radius); y++)
        {
            for (int x = (int)Math.Floor(cx - radius); x <= (int)Math.Ceiling(cx + radius); x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy;
                if (dx * dx + dy * dy <= radius * radius && x >= 0 && y >= 0 && x < Width && y < Height)
                    Set(x, y, colour);
            }
        }
    }

    private void Set(int x, int y, Rgb colour)
    {
        int i = (y * Width + x) * 4;
        Pixels[i] = colour.R;
        Pixels[i + 1] = colour.G;
        Pixels[i + 2] = colour.B;
        Pixels[i + 3] = 255;
    }
}

/// <summary>A colour of the world picture.</summary>
public readonly record struct Rgb(byte R, byte G, byte B);
