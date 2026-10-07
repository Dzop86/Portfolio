using Godot;
using Rogue.Core;

namespace Rogue.Desktop;

/// <summary>
/// Draws the current floor: tiles in sight in full colour, tiles seen before darker, monsters only in
/// sight, items wherever they were seen. The cells grow with the window.
/// </summary>
public partial class MapView : Control
{
    // The portfolio's colours: VS Code grey, pistachio accent, chocolate touches.
    private static readonly Color Wall = new("#5a3a22");
    private static readonly Color Floor = new("#4d4d4d");
    private static readonly Color Accent = new("#bef374");
    private static readonly Color Potion = new("#d58ae6");
    private static readonly Color Gold = new("#f2d35b");
    // Tiles out of sight stay readable against the #1f1f1f background (#4d4d4d floor becomes #2e2e2e).
    private const float RememberedShade = 0.4f;

    public Game? Game { get; set; }

    public override void _Draw()
    {
        if (Game is null)
            return;
        Map map = Game.Map;
        float cell = Mathf.Floor(Mathf.Min(Size.X / map.Width, Size.Y / map.Height));
        var origin = new Vector2((Size.X - cell * map.Width) / 2, (Size.Y - cell * map.Height) / 2);
        Font font = GetThemeDefaultFont();
        int fontSize = (int)(cell * 0.8f);

        foreach (Point p in map.Points())
        {
            if (!map.IsExplored(p))
                continue;
            bool visible = Game.IsVisible(p);
            var rect = new Rect2(origin + new Vector2(p.X * cell, p.Y * cell), new Vector2(cell, cell));
            Color tile = map[p] switch
            {
                Tile.Wall => Wall,
                Tile.Stairs => Accent.Darkened(0.35f),
                _ => Floor,
            };
            DrawRect(rect.Grow(-0.5f), visible ? tile : tile.Darkened(RememberedShade));

            (string glyph, Color colour)? mark = null;
            if (map[p] == Tile.Stairs)
                mark = (">", Colors.Black);
            if (Game.Items.TryGetValue(p, out Item item))
                mark = item.Kind == ItemKind.Gold ? ("$", Gold) : ("!", Potion);
            if (visible && Game.MonsterAt(p) is Monster monster)
                mark = (Glyph(monster.Kind), MonsterColour(monster.Kind));
            if (p == Game.Player.Position)
                mark = ("@", Accent);
            if (mark is (string text, Color colour))
            {
                Color shown = visible ? colour : colour.Darkened(RememberedShade);
                DrawString(font, rect.Position + new Vector2(0, cell * 0.8f), text, HorizontalAlignment.Center, cell, fontSize, shown);
            }
        }
    }

    public static string Glyph(MonsterKind kind) => kind switch
    {
        MonsterKind.Rat => "r",
        MonsterKind.Goblin => "g",
        MonsterKind.Orc => "o",
        _ => "T",
    };

    private static Color MonsterColour(MonsterKind kind) => kind switch
    {
        MonsterKind.Rat => new Color("#d0d0d0"),
        MonsterKind.Goblin => new Color("#7fd0ff"),
        MonsterKind.Orc => new Color("#ffb05c"),
        _ => new Color("#ff6b6b"),
    };
}
