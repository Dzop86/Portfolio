namespace Rogue.Core.Tests;

/// <summary>Builds small floors by hand: # wall, . floor, &gt; stairs, @ player, r g o T monsters, ! potion, $ 10 gold.</summary>
internal static class Floors
{
    public static Game Play(int depth, params string[] rows) => Play(1, depth, rows);

    public static Game Play(ulong seed, int depth, params string[] rows)
    {
        var map = new Map(rows[0].Length, rows.Length);
        Point start = default, stairs = new(-1, -1);
        var monsters = new List<Monster>();
        var items = new Dictionary<Point, Item>();
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
            {
                var p = new Point(x, y);
                char c = rows[y][x];
                map[p] = c switch { '#' => Tile.Wall, '>' => Tile.Stairs, _ => Tile.Floor };
                switch (c)
                {
                    case '@': start = p; break;
                    case '>': stairs = p; break;
                    case 'r': monsters.Add(new Monster(MonsterKind.Rat, p)); break;
                    case 'g': monsters.Add(new Monster(MonsterKind.Goblin, p)); break;
                    case 'o': monsters.Add(new Monster(MonsterKind.Orc, p)); break;
                    case 'T': monsters.Add(new Monster(MonsterKind.Troll, p)); break;
                    case '!': items[p] = new Item(ItemKind.Potion); break;
                    case '$': items[p] = new Item(ItemKind.Gold, 10); break;
                }
            }
        return new Game(seed, new Rng(seed), depth, new Level(map, start, stairs, monsters, items));
    }

    /// <summary>Lets the autopilot play until the run ends, or gives up after <paramref name="maxTurns"/>.</summary>
    public static Game AutoPlay(ulong seed, int maxTurns = 20_000)
    {
        var game = new Game(seed);
        while (game.State == GameState.Playing && game.Turn < maxTurns)
            game.Apply(Autopilot.Choose(game));
        return game;
    }
}
