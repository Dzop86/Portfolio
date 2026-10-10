using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// An item's icon, drawn rather than painted (sprint 65): a rounded tile, a simple shape for its slot or
/// kind (<see cref="ItemStyle.GlyphOf"/>) in its colour (<see cref="ItemStyle.Colour"/>), its count in a
/// corner; an empty slot shows its own shape, faint. Nothing is taken from another game.
/// </summary>
public partial class ItemIcon : Control
{
    public Glyph Glyph { get; set; }
    public Color Tint { get; set; } = Colors.White;
    public int Count { get; set; } = 1;

    /// <summary>An empty slot or an item that cannot be worn now: drawn faint.</summary>
    public bool Faint { get; set; }

    public override void _Draw()
    {
        float s = Mathf.Min(Size.X, Size.Y);
        var c = new Vector2(Size.X / 2, Size.Y / 2);
        DrawStyleBox(new StyleBoxFlat
        {
            BgColor = new Color("2b2b2b"),
            BorderColor = Faint ? new Color(1, 1, 1, 0.12f) : new Color(Tint, 0.55f),
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        }, new Rect2(Vector2.Zero, Size));
        Color col = Faint ? new Color(1, 1, 1, 0.22f) : Tint;
        float r = s * 0.32f, w = Mathf.Max(2, s * 0.07f);
        switch (Glyph)
        {
            case Glyph.Ring:
                DrawArc(c + new Vector2(0, r * 0.15f), r * 0.75f, 0, Mathf.Tau, 32, col, w * 1.4f, true);
                DrawCircle(c - new Vector2(0, r * 0.65f), r * 0.28f, col);
                break;
            case Glyph.Amulet:
                DrawArc(c - new Vector2(0, r * 0.2f), r, Mathf.Pi * 1.05f, Mathf.Pi * 1.95f, 24, col, w * 0.6f, true);
                DrawCircle(c + new Vector2(0, r * 0.55f), r * 0.42f, col);
                break;
            case Glyph.Belt:
                DrawRect(new Rect2(c.X - r * 1.2f, c.Y - r * 0.3f, r * 2.4f, r * 0.6f), col);
                DrawRect(new Rect2(c.X - r * 0.35f, c.Y - r * 0.5f, r * 0.7f, r), new Color("2b2b2b"), false, w);
                break;
            case Glyph.Cape:
                DrawColoredPolygon([c + new Vector2(-r * 0.45f, -r), c + new Vector2(r * 0.45f, -r), c + new Vector2(r, r), c + new Vector2(-r, r)], col);
                break;
            case Glyph.Hat:
                DrawColoredPolygon([c + new Vector2(0, -r * 1.05f), c + new Vector2(r * 0.65f, r * 0.45f), c + new Vector2(-r * 0.65f, r * 0.45f)], col);
                DrawRect(new Rect2(c.X - r * 1.1f, c.Y + r * 0.45f, r * 2.2f, r * 0.3f), col);
                break;
            case Glyph.Chest:
                DrawColoredPolygon([c + new Vector2(-r, -r * 0.8f), c + new Vector2(-r * 0.35f, -r), c + new Vector2(r * 0.35f, -r), c + new Vector2(r, -r * 0.8f), c + new Vector2(r * 0.75f, r), c + new Vector2(-r * 0.75f, r)], col);
                break;
            case Glyph.Shoulders:
                DrawCircle(c + new Vector2(-r * 0.6f, 0), r * 0.55f, col);
                DrawCircle(c + new Vector2(r * 0.6f, 0), r * 0.55f, col);
                break;
            case Glyph.Sword:
                DrawLine(c + new Vector2(-r * 0.8f, r * 0.8f), c + new Vector2(r * 0.9f, -r * 0.9f), col, w * 1.5f, true);
                DrawLine(c + new Vector2(-r * 0.75f, r * 0.15f), c + new Vector2(-r * 0.15f, r * 0.75f), col, w * 1.3f, true);
                break;
            case Glyph.Axe:
                DrawLine(c + new Vector2(-r * 0.9f, r), c + new Vector2(r * 0.5f, -r * 0.9f), col, w * 1.2f, true);
                DrawColoredPolygon([c + new Vector2(r * 0.1f, -r * 0.9f), c + new Vector2(r * 1.05f, -r * 0.55f), c + new Vector2(r * 0.65f, r * 0.25f), c + new Vector2(r * 0.15f, -r * 0.15f)], col);
                break;
            case Glyph.Shield:
                DrawColoredPolygon([c + new Vector2(-r * 0.85f, -r * 0.9f), c + new Vector2(r * 0.85f, -r * 0.9f), c + new Vector2(r * 0.75f, r * 0.2f), c + new Vector2(0, r), c + new Vector2(-r * 0.75f, r * 0.2f)], col);
                break;
            case Glyph.Pet:
                DrawCircle(c + new Vector2(0, r * 0.35f), r * 0.5f, col);
                foreach (float x in new[] { -0.7f, -0.25f, 0.25f, 0.7f })
                    DrawCircle(c + new Vector2(r * x, -r * (Mathf.Abs(x) > 0.5f ? 0.25f : 0.6f)), r * 0.22f, col);
                break;
            case Glyph.Mount:
                DrawArc(c + new Vector2(0, r * 0.1f), r * 0.8f, -Mathf.Pi / 2 + 0.7f, Mathf.Pi * 1.5f - 0.7f, 28, col, w * 1.8f, true);
                break;
            case Glyph.Boots:
                DrawColoredPolygon([c + new Vector2(-r * 0.6f, -r), c + new Vector2(r * 0.05f, -r), c + new Vector2(r * 0.05f, r * 0.2f), c + new Vector2(r, r * 0.45f), c + new Vector2(r, r), c + new Vector2(-r * 0.6f, r)], col);
                break;
            case Glyph.Potion:
                DrawCircle(c + new Vector2(0, r * 0.35f), r * 0.65f, col);
                DrawRect(new Rect2(c.X - r * 0.22f, c.Y - r * 0.85f, r * 0.44f, r * 0.7f), col);
                break;
            case Glyph.Leaf:
                DrawColoredPolygon([c + new Vector2(0, -r), c + new Vector2(r * 0.6f, 0), c + new Vector2(0, r), c + new Vector2(-r * 0.6f, 0)], col);
                DrawLine(c + new Vector2(0, -r * 0.8f), c + new Vector2(0, r * 0.8f), new Color("2b2b2b"), w * 0.6f);
                break;
            default:
                DrawRect(new Rect2(c.X - r * 0.8f, c.Y - r * 0.6f, r * 1.6f, r * 1.2f), col);
                DrawCircle(c + new Vector2(-r * 0.8f, 0), r * 0.25f, col);
                DrawCircle(c + new Vector2(r * 0.8f, 0), r * 0.25f, col);
                break;
        }
        if (Count > 1)
        {
            Font font = ThemeDB.FallbackFont;
            int size = Mathf.RoundToInt(s * 0.24f);
            string text = Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Vector2 at = new(Size.X - 5 - font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X, Size.Y - 6);
            DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, size, 4, Colors.Black);
            DrawString(font, at, text, HorizontalAlignment.Left, -1, size, Colors.White);
        }
    }

    /// <summary>A clickable tile holding an icon: a flat button the size of the icon, with its card as tooltip.</summary>
    public static (Button Tile, ItemIcon Icon) Tile(float size)
    {
        var tile = new Button { CustomMinimumSize = new Vector2(size, size), Flat = true, FocusMode = Control.FocusModeEnum.All };
        var icon = new ItemIcon { MouseFilter = MouseFilterEnum.Ignore, AnchorRight = 1, AnchorBottom = 1 };
        tile.AddChild(icon);
        return (tile, icon);
    }
}
