using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// An item's icon: since sprint 166 (D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §4) its tile in relief drawn by
/// <c>tools/item_art.py</c> (<see cref="ItemStyle.Tile(Rpg.Core.Item, bool)"/>): its game-icons.net drawing (CC BY 3.0, the
/// authors in <c>assets/icons/items/credits.json</c>) in the colour of what it is made of, its rim telling where it comes
/// from, the dot of its element; flat for an item that cannot be worn now; an empty slot shows its slot's drawing in a
/// hollow. Its count in the bottom right corner from 40 pixels up.
/// </summary>
public partial class ItemIcon : Control
{
    /// <summary>The item drawn (null: the empty <see cref="Slot"/>, or nothing).</summary>
    public Rpg.Core.Item? Item { get; set; }

    /// <summary>The slot an empty case shows, when no item is drawn.</summary>
    public Rpg.Core.ItemSlot? Slot { get; set; }

    // Kept for the whole game, as the spells' (a texture held by no node was freed, D67).
    private static readonly Dictionary<string, Texture2D?> Textures = [];

    /// <summary>A tile of <c>assets/icons/items/tiles</c>; null when the game has none of that name.</summary>
    public static Texture2D? TextureOf(string tile)
    {
        if (Textures.TryGetValue(tile, out Texture2D? known))
            return known;
        string path = $"res://assets/icons/items/tiles/{tile}.svg";
        return Textures[tile] = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    /// <summary>Lets the textures go, before the game quits.</summary>
    public static void Forget() => Textures.Clear();

    /// <summary>The tiles shown so far (for the self-test).</summary>
    public static int Drawn => Textures.Values.Count(t => t is not null);

    public int Count { get; set; } = 1;

    /// <summary>An empty slot or an item that cannot be worn now: its flat tile.</summary>
    public bool Faint { get; set; }

    /// <summary>The tile drawn now (for the self-test), or none.</summary>
    public string? Shown => Item is Rpg.Core.Item item ? ItemStyle.Tile(item, Faint) : Slot is Rpg.Core.ItemSlot slot ? ItemStyle.Tile(slot) : null;

    public override void _Draw()
    {
        // Square and centred, whatever the row stretches the control to.
        float s = Mathf.Min(Size.X, Size.Y);
        var square = new Rect2((Size - new Vector2(s, s)) / 2, new Vector2(s, s));
        if (Shown is string tile && TextureOf(tile) is Texture2D texture)
            DrawTextureRect(texture, square, false);
        // Its count: 14 px, bold white edged in black, in the corner the tile leaves free; none on a tile under 40.
        if (Count > 1 && s >= 40)
        {
            Font font = OsmoseTheme.Bold();
            const int size = 14;
            string text = Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Vector2 at = square.End - new Vector2(4 + font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X, 5);
            DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, size, 3, Colors.Black);
            DrawString(font, at, text, HorizontalAlignment.Left, -1, size, Colors.White);
        }
    }

    /// <summary>A clickable tile holding an icon: a flat button the size of the icon, with its card as tooltip.</summary>
    public static (Button Tile, ItemIcon Icon) Tile(float size)
    {
        var tile = new Button { CustomMinimumSize = new Vector2(size, size), Flat = true, FocusMode = Control.FocusModeEnum.All };
        var icon = new ItemIcon { MouseFilter = MouseFilterEnum.Ignore, AnchorRight = 1, AnchorBottom = 1 };
        tile.AddChild(icon);
        // A right click on any item's tile opens its menu (sprint 117, D77), wherever the tile is.
        tile.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } && icon.Item is Rpg.Core.Item item)
            {
                tile.AcceptEvent();
                RightClicked?.Invoke(item, tile);
            }
        };
        return (tile, icon);
    }

    /// <summary>An item's tile right-clicked (sprint 117): the item, and the tile (its window says where it is).</summary>
    public static event Action<Rpg.Core.Item, Control>? RightClicked;

    /// <summary>A right click on a tile, as the mouse would (for the self-test).</summary>
    public static void RightClick(Rpg.Core.Item item, Control tile) => RightClicked?.Invoke(item, tile);
}
