using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A spell's icon (sprint 67): since sprint 163 (docs/graphisme/s163-icones-arbre.md §1), a tile in relief in the colour of its
/// element, drawn whole by tools/spell_art.py (<c>assets/icons/spells/&lt;id&gt;.svg</c>, the game-icons.net authors in
/// <c>credits.json</c>): flat (<c>-locked</c>) when it is not unlocked or not castable now, without its marks (<c>-small</c>)
/// under 32 pixels; ringed in chocolate under the pointer and in pistachio when chosen, the same in the book, the deck and the
/// combat bar (<see cref="SpellIconStates"/>). A spell without a drawing (the monsters') shows the first letter of its name on its
/// element's colour, flat as well when it is faint.
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

    /// <summary>
    /// Drawn again at its new size (in a row or a grid, the size comes after the first drawing); ringed under the pointer
    /// when it takes the mouse itself (a place of the deck), else its parent says so through <see cref="Hovered"/>.
    /// </summary>
    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            QueueRedraw();
        else if (what == NotificationMouseEnter)
            Hovered = true;
        else if (what == NotificationMouseExit)
            Hovered = false;
    }

    private bool _hovered, _pressed;

    /// <summary>The pointer over it (§1.6): a ring of light chocolate, the tile a pixel higher.</summary>
    public bool Hovered
    {
        get => _hovered;
        set
        {
            _hovered = value;
            QueueRedraw();
        }
    }

    /// <summary>Pressed (the combat bar's button held down): the tile a pixel lower.</summary>
    public bool Pressed
    {
        get => _pressed;
        set
        {
            _pressed = value;
            QueueRedraw();
        }
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
            // An empty place of the deck: a hollow (the theme's "frame-well").
            DrawStyleBox(new StyleBoxFlat
            {
                BgColor = new Color(UiStyle.Well),
                BorderColor = new Color(UiStyle.Edge),
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
        SpellLook look = SpellIconStates.Look(Faint, Mathf.Min(Size.X, Size.Y), _hovered, Selected, _pressed);
        var whole = new Rect2(Vector2.Zero, Size);
        if (look.Ring == SpellRing.Selected)
        {
            // A halo of 3 pixels at 35 %, then a ring of 2 (the theme's "tile-lit").
            DrawStyleBox(Ring(new Color(SpellIconStates.SelectedRing, 0.35f), 3, -3), whole);
            DrawStyleBox(Ring(new Color(SpellIconStates.SelectedRing), 2, -1), whole);
        }
        else if (look.Ring == SpellRing.Hover)
            DrawStyleBox(Ring(new Color(SpellIconStates.HoverRing), 1, -2), whole);
        var tile = new Rect2(new Vector2(0, look.Lift), Size);
        if (TextureOf(_spell, look.Variant) is Texture2D tex)
        {
            DrawTextureRect(tex, tile, false);
            return;
        }
        var colour = new Color(ElementStyle.Colour(_spell.Element));
        // Flat when faint, as the drawn tiles (§1.6): the element's colour kept in the edge, a grey letter.
        DrawStyleBox(new StyleBoxFlat
        {
            BgColor = Faint ? colour.Lerp(new Color(UiStyle.Well), 0.85f) : colour.Darkened(0.45f),
            BorderColor = Faint ? colour.Lerp(new Color(UiStyle.Window), 0.60f) : colour,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        }, tile);
        Font font = ThemeDB.FallbackFont;
        int size = Mathf.RoundToInt(Size.Y * 0.5f);
        string letter = _spell.Name.En[..1];
        Vector2 at = new((Size.X - font.GetStringSize(letter, HorizontalAlignment.Left, -1, size).X) / 2, Size.Y * 0.68f + look.Lift);
        DrawString(font, at, letter, HorizontalAlignment.Left, -1, size, Faint ? new Color(UiStyle.TextDim) : Colors.White);
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
