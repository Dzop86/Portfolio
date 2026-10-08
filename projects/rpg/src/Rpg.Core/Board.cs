namespace Rpg.Core;

/// <summary>What a cell is made of.</summary>
public enum Terrain
{
    /// <summary>Walkable; a fighter may stand on it.</summary>
    Floor,

    /// <summary>A pillar or a tree: nobody walks on it, and it blocks the line of sight.</summary>
    Obstacle,

    /// <summary>Water or a hole: nobody walks on it, but spells fly over it.</summary>
    Hole,
}

/// <summary>A fighting board, read from rows of characters (see <see cref="Parse"/>).</summary>
public sealed class Board
{
    private readonly Terrain[] _cells;

    public int Width { get; }
    public int Height { get; }

    /// <summary>Starting cells of each team, in reading order: index 0 for team A, 1 for team B.</summary>
    public IReadOnlyList<IReadOnlyList<Cell>> Starts { get; }

    private Board(int width, int height, Terrain[] cells, IReadOnlyList<IReadOnlyList<Cell>> starts)
    {
        Width = width;
        Height = height;
        _cells = cells;
        Starts = starts;
    }

    /// <summary>
    /// Reads a board: '.' floor, '#' obstacle, '~' hole, 'A' and 'B' floor cells where teams A and B
    /// start. Row y is the y-th string, column x its x-th character. All rows have the same length.
    /// </summary>
    public static Board Parse(IReadOnlyList<string> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0 || rows[0].Length == 0)
            throw new FormatException("A board needs at least one row and one column.");
        int width = rows[0].Length;
        var cells = new Terrain[width * rows.Count];
        List<Cell>[] starts = [[], []];
        for (int y = 0; y < rows.Count; y++)
        {
            if (rows[y].Length != width)
                throw new FormatException($"Row {y} has {rows[y].Length} cells, the first one {width}.");
            for (int x = 0; x < width; x++)
            {
                char c = rows[y][x];
                cells[y * width + x] = c switch
                {
                    '.' or 'A' or 'B' => Terrain.Floor,
                    '#' => Terrain.Obstacle,
                    '~' => Terrain.Hole,
                    _ => throw new FormatException($"Unknown cell '{c}' at ({x}, {y})."),
                };
                if (c is 'A' or 'B')
                    starts[c - 'A'].Add(new Cell(x, y));
            }
        }
        return new Board(width, rows.Count, cells, starts);
    }

    public bool Contains(Cell c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;

    /// <summary>The terrain of a cell; outside the board counts as a hole (not walkable, sight passes).</summary>
    public Terrain this[Cell c] => Contains(c) ? _cells[c.Y * Width + c.X] : Terrain.Hole;

    public bool IsFloor(Cell c) => this[c] == Terrain.Floor;

    public IEnumerable<Cell> Cells()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                yield return new Cell(x, y);
    }
}
