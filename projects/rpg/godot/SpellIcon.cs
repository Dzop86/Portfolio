using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A spell's icon (sprint 67): a rounded tile in the colour of its element, its game-icons.net drawing
/// (CC BY 3.0, the authors in <c>assets/icons/spells/credits.json</c>) in white on it; a spell without
/// an icon shows the first letter of its name.
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

    /// <summary>A spell not unlocked yet, or not castable now: drawn faint.</summary>
    public bool Faint { get; set; }

    /// <summary>The drawing of a spell, from the icons imported with the game; null without one.</summary>
    public static Texture2D? TextureOf(Spell spell)
    {
        string path = $"res://assets/icons/spells/{spell.Id}.svg";
        return spell.Icon is not null && ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

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
        }, new Rect2(Vector2.Zero, Size));
        float inset = Mathf.Min(Size.X, Size.Y) * 0.14f;
        if (TextureOf(_spell) is Texture2D tex)
        {
            DrawTextureRect(tex, new Rect2(new Vector2(inset, inset), Size - new Vector2(inset * 2, inset * 2)), false, new Color(1, 1, 1, alpha));
            return;
        }
        Font font = ThemeDB.FallbackFont;
        int size = Mathf.RoundToInt(Size.Y * 0.5f);
        string letter = _spell.Name.En[..1];
        Vector2 at = new((Size.X - font.GetStringSize(letter, HorizontalAlignment.Left, -1, size).X) / 2, Size.Y * 0.68f);
        DrawString(font, at, letter, HorizontalAlignment.Left, -1, size, new Color(1, 1, 1, alpha));
    }
}
