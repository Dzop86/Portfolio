namespace Rogue.Core;

/// <summary>A freshly generated floor: its map, where the player arrives, the stairs, monsters and items.</summary>
internal sealed record Level(Map Map, Point Start, Point Stairs, List<Monster> Monsters, Dictionary<Point, Item> Items);

/// <summary>
/// Builds a floor from the game's generator: non-overlapping rooms, sorted from left to right and
/// joined in that order by L-shaped corridors, so every room can be reached from the first.
/// </summary>
internal static class DungeonGenerator
{
    private const int RoomAttempts = 40;
    private const int MaxRooms = 9;

    public static Level Generate(Rng rng, int depth, int width = Map.DefaultWidth, int height = Map.DefaultHeight)
    {
        var map = new Map(width, height);
        var rooms = new List<Room>();
        for (int attempt = 0; rooms.Count < MaxRooms && (attempt < RoomAttempts || rooms.Count < 2); attempt++)
        {
            int w = rng.Next(4, 10);
            int h = rng.Next(3, 5);
            // Keeps a wall all around the map.
            var room = new Room(rng.Next(1, width - w - 1), rng.Next(1, height - h - 1), w, h);
            if (!rooms.Exists(r => r.Overlaps(room, 1)))
                rooms.Add(room);
        }
        rooms.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
        foreach (Room room in rooms)
            map.AddRoom(room);
        for (int i = 1; i < rooms.Count; i++)
            CarveCorridor(map, rng, rooms[i - 1].Center, rooms[i].Center);

        Point start = rooms[0].Center;
        Point stairs = rooms[^1].Center;
        map[stairs] = Tile.Stairs;

        var monsters = new List<Monster>();
        var items = new Dictionary<Point, Item>();
        bool IsFree(Point p) => p != start && p != stairs && !items.ContainsKey(p) && !monsters.Exists(m => m.Position == p);

        for (int i = 0; i < rooms.Count; i++)
        {
            // The first room is left empty of monsters: the player arrives there.
            int count = i == 0 ? 0 : rng.Next(0, 1 + depth / 2);
            for (int k = 0; k < count; k++)
                if (TryPickCell(rng, rooms[i], IsFree, out Point p))
                    monsters.Add(new Monster(PickKind(rng, depth), p));
            if (rng.Chance(35) && TryPickCell(rng, rooms[i], IsFree, out Point potion))
                items[potion] = new Item(ItemKind.Potion);
            if (rng.Chance(55) && TryPickCell(rng, rooms[i], IsFree, out Point gold))
                items[gold] = new Item(ItemKind.Gold, rng.Next(5, 10 + 10 * depth));
        }
        return new Level(map, start, stairs, monsters, items);
    }

    private static void CarveCorridor(Map map, Rng rng, Point from, Point to)
    {
        // Horizontal then vertical, or the other way round.
        Point corner = rng.Chance(50) ? new Point(to.X, from.Y) : new Point(from.X, to.Y);
        CarveLine(map, from, corner);
        CarveLine(map, corner, to);
    }

    private static void CarveLine(Map map, Point from, Point to)
    {
        int dx = Math.Sign(to.X - from.X);
        int dy = Math.Sign(to.Y - from.Y);
        for (Point p = from; ; p = new Point(p.X + dx, p.Y + dy))
        {
            map[p] = Tile.Floor;
            if (p == to)
                break;
        }
    }

    private static bool TryPickCell(Rng rng, Room room, Func<Point, bool> isFree, out Point cell)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            cell = new Point(room.X + rng.Next(room.Width), room.Y + rng.Next(room.Height));
            if (isFree(cell))
                return true;
        }
        cell = default;
        return false;
    }

    /// <summary>Rats on the first floor; the deeper the floor, the more orcs and trolls.</summary>
    private static MonsterKind PickKind(Rng rng, int depth)
    {
        int roll = rng.Next(25 + 25 * depth);
        return roll switch
        {
            < 40 => MonsterKind.Rat,
            < 75 => MonsterKind.Goblin,
            < 110 => MonsterKind.Orc,
            _ => MonsterKind.Troll,
        };
    }
}
