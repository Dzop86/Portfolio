namespace Rogue.Core;

public enum Tile : byte
{
    Wall,
    Floor,
    Stairs,
}

/// <summary>One floor of the dungeon: tiles, rooms, and what the player has already seen.</summary>
public sealed class Map
{
    public const int DefaultWidth = 60;
    public const int DefaultHeight = 20;

    private readonly Tile[] _tiles;
    private readonly bool[] _explored;
    private readonly List<Room> _rooms = [];

    public Map(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 3);
        Width = width;
        Height = height;
        _tiles = new Tile[width * height];
        _explored = new bool[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<Room> Rooms => _rooms;

    public Tile this[Point p]
    {
        get => InBounds(p) ? _tiles[Index(p)] : Tile.Wall;
        internal set => _tiles[Index(p)] = value;
    }

    public bool InBounds(Point p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;

    public bool IsWalkable(Point p) => this[p] != Tile.Wall;

    public bool IsExplored(Point p) => InBounds(p) && _explored[Index(p)];

    internal void MarkExplored(Point p) => _explored[Index(p)] = true;

    internal void AddRoom(Room room)
    {
        _rooms.Add(room);
        for (int y = room.Y; y < room.Y + room.Height; y++)
            for (int x = room.X; x < room.X + room.Width; x++)
                this[new Point(x, y)] = Tile.Floor;
    }

    public IEnumerable<Point> Points()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                yield return new Point(x, y);
    }

    /// <summary>
    /// Number of steps from <paramref name="from"/> to every walkable tile (-1 where unreachable),
    /// moving through the four sides; <paramref name="passable"/> narrows the tiles allowed.
    /// </summary>
    public int[] Distances(Point from, Func<Point, bool>? passable = null)
    {
        var distances = new int[Width * Height];
        Array.Fill(distances, -1);
        if (!IsWalkable(from))
            return distances;
        var queue = new Queue<Point>();
        distances[Index(from)] = 0;
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();
            foreach (Direction d in Directions.All)
            {
                Point q = p.Step(d);
                if (!IsWalkable(q) || distances[Index(q)] >= 0 || (passable is not null && !passable(q)))
                    continue;
                distances[Index(q)] = distances[Index(p)] + 1;
                queue.Enqueue(q);
            }
        }
        return distances;
    }

    public int Index(Point p) => p.Y * Width + p.X;
}

public static class Directions
{
    /// <summary>The fixed order in which ties are broken, so that every choice is reproducible.</summary>
    public static IReadOnlyList<Direction> All { get; } = [Direction.North, Direction.South, Direction.East, Direction.West];
}
