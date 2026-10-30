using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A spell's icon (sprint 67): since sprint 164 (docs/graphisme/s164-icones-arbre.md §1), a tile in relief in the colour of its
/// element, drawn whole by tools/spell_art.py (<c>assets/icons/spells/&lt;id&gt;.svg</c>, the game-icons.net authors in
/// <c>credits.json</c>): flat (<c>-locked</c>) when it is not unlocked or not castable now, without its marks (<c>-small</c>)
/// under 32 pixels; the chosen one ringed in pistachio. A spell without a drawing (the monsters') shows the first letter of
/// its name on its element's colour.
/// </summary>
public partial class SpellIcon : Control
{
    private Spell? _spell;

    public Spell? Spell
    {
        get => _spell;
        set
        {
            _spell = value;
            QueueRedraw();
        }
    }

    /// <summary>Drawn again at its new size (in a row or a grid, the size comes after the first drawing).</summary>
    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            QueueRedraw();
    }

    /// <summary>A spell not unlocked yet, or not castable now: drawn faint.</summary>
    public bool Faint { get; set; }

    // Kept for the whole game: a texture only drawn, not held by any node, was freed when the C# garbage
    // collector let its wrapper go, and the canvas then drew a white square in its place (Charles, D67).
    private static readonly Dictionary<string, Texture2D?> Textures = [];

    /// <summary>The drawing of a spell, from the icons imported with the game (<paramref name="variant"/>: "", "-locked" or "-small"); null without one.</summary>
    public static Texture2D? TextureOf(Spell spell, string variant = "")
    {
        string key = spell.Id + variant;
        if (Textures.TryGetValue(key, out Texture2D? known))
            return known;
        string path = $"res://assets/icons/spells/{key}.svg";
        return Textures[key] = spell.Icon is not null && ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    /// <summary>The spell chosen (the combat bar's) or looked at (the book's): ringed in pistachio (§1.6).</summary>
    public bool Selected { get; set; }

    /// <summary>Lets the textures go, before the game quits (no resource left alive at exit).</summary>
    public static void Forget() => Textures.Clear();

    /// <summary>The textures kept (for the self-test: still valid after a garbage collection).</summary>
    public static IEnumerable<Texture2D> Kept => Textures.Values.OfType<Texture2D>();

    public override void _Draw()
    {
        if (_spell is null)
        {
            // An empty place of the deck: a faint frame.
            DrawStyleBox(new StyleBoxFlat
            {
                BgColor = new Color(1, 1, 1, 0.04f),
                BorderColor = new Color(1, 1, 1, 0.18f),
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                BorderWidthTop = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
            }, new Rect2(Vector2.Zero, Size));
            return;
        }
        var whole = new Rect2(Vector2.Zero, Size);
        if (TextureOf(_spell, Faint ? "-locked" : Mathf.Min(Size.X, Size.Y) < 32 ? "-small" : "") is Texture2D tex)
        {
            DrawTextureRect(tex, whole, false);
            if (Selected)
            {
                // A halo of 3 pixels at 35 %, then a ring of 2 (the theme's "tile-lit").
                DrawStyleBox(Ring(new Color("bef374", 0.35f), 3, -3), whole);
                DrawStyleBox(Ring(new Color("bef374"), 2, -1), whole);
            }
            return;
        }
        var colour = new Color(ElementStyle.Colour(_spell.Element));
        float alpha = Faint ? 0.4f : 1f;
        DrawStyleBox(new StyleBoxFlat
        {
            BgColor = new Color(colour.Darkened(0.45f), alpha),
            BorderColor = new Color(colour, alpha),
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        }, whole);
        Font font = ThemeDB.FallbackFont;
        int size = Mathf.RoundToInt(Size.Y * 0.5f);
        string letter = _spell.Name.En[..1];
        Vector2 at = new((Size.X - font.GetStringSize(letter, HorizontalAlignment.Left, -1, size).X) / 2, Size.Y * 0.68f);
        DrawString(font, at, letter, HorizontalAlignment.Left, -1, size, new Color(1, 1, 1, alpha));
    }

    private static StyleBoxFlat Ring(Color colour, int width, float grow) => new()
    {
        DrawCenter = false,
        BorderColor = colour,
        BorderWidthLeft = width,
        BorderWidthRight = width,
        BorderWidthTop = width,
        BorderWidthBottom = width,
        CornerRadiusTopLeft = 10,
        CornerRadiusTopRight = 10,
        CornerRadiusBottomLeft = 10,
        CornerRadiusBottomRight = 10,
        ExpandMarginLeft = -grow,
        ExpandMarginRight = -grow,
        ExpandMarginTop = -grow,
        ExpandMarginBottom = -grow,
    };
}
