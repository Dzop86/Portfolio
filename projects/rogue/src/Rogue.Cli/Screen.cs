using System.Text;
using Rogue.Core;

namespace Rogue.Cli;

/// <summary>
/// Draws the game as text. Monsters show only in sight; tiles seen before keep their floor plan
/// and items, marked so that a colour terminal can dim them.
/// </summary>
internal static class Screen
{
    public static char Glyph(MonsterKind kind) => kind switch
    {
        MonsterKind.Rat => 'r',
        MonsterKind.Goblin => 'g',
        MonsterKind.Orc => 'o',
        MonsterKind.Troll => 'T',
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>The map, one string per row, and for each tile whether it is only remembered.</summary>
    public static (string[] Rows, bool[,] Remembered) Draw(Game game)
    {
        Map map = game.Map;
        var rows = new string[map.Height];
        var remembered = new bool[map.Height, map.Width];
        var row = new StringBuilder(map.Width);
        for (int y = 0; y < map.Height; y++)
        {
            row.Clear();
            for (int x = 0; x < map.Width; x++)
            {
                var p = new Point(x, y);
                row.Append(Cell(game, p));
                remembered[y, x] = map.IsExplored(p) && !game.IsVisible(p);
            }
            rows[y] = row.ToString();
        }
        return (rows, remembered);
    }

    private static char Cell(Game game, Point p)
    {
        Map map = game.Map;
        if (!map.IsExplored(p))
            return ' ';
        if (game.IsVisible(p))
        {
            if (p == game.Player.Position)
                return '@';
            if (game.MonsterAt(p) is Monster monster)
                return Glyph(monster.Kind);
        }
        // Items do not move: the player remembers where they lie.
        if (game.Items.TryGetValue(p, out Item item))
            return item.Kind == ItemKind.Gold ? '$' : '!';
        return map[p] switch
        {
            Tile.Wall => '#',
            Tile.Stairs => '>',
            _ => '.',
        };
    }
}
