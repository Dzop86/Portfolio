using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The menus over the town (sprint 69; a grid since Charles's playtest of sprint 144, the way Dofus does it): a small
/// "Menus" panel at the bottom of the right edge, three icons a row, one per window of <see cref="WindowState.Bar"/>
/// then the mini-map and the menu, each in its own colour with its key in a corner; hovering one shows its name and key,
/// a click opens or closes its window, the open one stays lit. The icons are game-icons.net's (CC BY 3.0, authors in
/// <c>assets/icons/bar/credits.json</c>); since sprint 157 (D84) they are drawn in relief by <c>tools/ui_art.py</c>, in
/// their own colours on neutral tiles, the panel and the tiles styled by <see cref="OsmoseTheme"/>.
/// </summary>
public partial class SideBar : CanvasLayer
{
    /// <summary>Icons a row: the ten of a hero fill four rows (the tree's since sprint 148), under half the old column's height.</summary>
    public const int Columns = 3;

    private readonly Dictionary<GameWindow, Button> _buttons = [];
    private Button _menu = null!;

    public event Action<GameWindow>? WindowPressed;
    public event Action? MenuPressed;

    /// <summary>The mini-map's icon (sprint 108), after the windows': it shows or hides the mini-map, lit while shown.</summary>
    public event Action? MiniMapPressed;

    private Button _miniMap = null!;

    public void Build(Texts texts, bool withCharacter)
    {
        var panel = new PanelContainer
        {
            AnchorLeft = 1,
            AnchorRight = 1,
            AnchorTop = 1,
            AnchorBottom = 1,
            OffsetRight = -12,
            OffsetBottom = -12,
            GrowHorizontal = Control.GrowDirection.Begin,
            GrowVertical = Control.GrowDirection.Begin,
        };
        panel.ThemeTypeVariation = OsmoseTheme.HudPanel;
        AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);
        // Its header (sprint 165, D87, s165-retours-30-oct.md §2): a grid and MENUS, bold, in light chocolate, a click
        // folds the panel to it, the badges of the tiles counted on it.
        _header = new HudHeader();
        _header.Build(GD.Load<Texture2D>("res://assets/ui/grid.svg"));
        _header.Pressed += () => Fold(!_header.Folded);
        column.AddChild(_header);
        _panel = panel;
        var grid = _grid = new GridContainer { Columns = Columns };
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 4);
        column.AddChild(grid);
        foreach ((GameWindow window, char key) in WindowState.Bar)
        {
            // The character's windows need a character (not the classless hero of the self-tests).
            if (!withCharacter && window != GameWindow.Map)
                continue;
            Button b = Icon(grid, window.ToString().ToLowerInvariant(), key.ToString());
            b.ToggleMode = true;
            b.Pressed += () => WindowPressed?.Invoke(window);
            _buttons[window] = b;
        }
        _miniMap = Icon(grid, "minimap", "N");
        _miniMap.ToggleMode = true;
        _miniMap.Pressed += () => MiniMapPressed?.Invoke();
        _menu = Icon(grid, "menu", "");
        _menu.Pressed += () => MenuPressed?.Invoke();
        SetTexts(texts);
        Fold(HudFile.Settings.MenusFolded, keep: false);
    }

    private PanelContainer _panel = null!;
    private GridContainer _grid = null!;
    private HudHeader _header = null!;
    private Texts _texts = null!;

    /// <summary>Folds the panel to its header or unfolds it, kept for the next game; the keys work either way.</summary>
    public void Fold(bool folded, bool keep = true)
    {
        _header.Show(folded, _texts["bar.fold"], _texts["bar.unfold"]);
        _header.Badge = HudLayout.Badges(Badges.Values);
        _grid.Visible = !folded;
        _panel.AddThemeStyleboxOverride("panel", HudHeader.Panel(0, folded ? 0 : HudLayout.Bottom));
        // As wide folded as unfolded; shrunk back to its content from its bottom right corner.
        _panel.CustomMinimumSize = new Vector2(Columns * HudLayout.Tile + (Columns - 1) * HudLayout.Gap + 16, 0);
        (_panel.OffsetLeft, _panel.OffsetTop) = (_panel.OffsetRight, _panel.OffsetBottom);
        if (keep)
            HudFile.Save(HudFile.Settings with { MenusFolded = folded });
    }

    /// <summary>Folded to its header (for the self-test), the grid hidden, the badges counted on the header.</summary>
    public bool Folded => _header.Folded;
    public bool GridShown => _grid.Visible;
    public int HeaderBadge => _header.Badge;
    public float PanelHeight => _panel.Size.Y;

    public void SetTexts(Texts texts)
    {
        _texts = texts;
        _header.Title = texts["bar.title"].ToUpperInvariant();
        _header.Show(_header.Folded, texts["bar.fold"], texts["bar.unfold"]);
        foreach ((GameWindow window, Button b) in _buttons)
            b.TooltipText = texts["bar.tip", texts["bar." + window], WindowState.Bar.First(w => w.Window == window).Key];
        _miniMap.TooltipText = texts["minimap.tip"];
        _menu.TooltipText = texts["bar.tip", texts["bar.menu"], texts["menu.key"]];
    }

    public void ShowMiniMap(bool shown) => _miniMap.SetPressedNoSignal(shown);

    public bool MiniMapLit => _miniMap.ButtonPressed;

    /// <summary>Lights the open window's icon, and only it.</summary>
    public void Show(GameWindow? open)
    {
        foreach ((GameWindow window, Button b) in _buttons)
            b.SetPressedNoSignal(window == open);
    }

    /// <summary>The tooltips, top to bottom, of the icons shown (for the self-test).</summary>
    public IEnumerable<string> Tips => _buttons.Values.Append(_miniMap).Append(_menu).Select(b => b.TooltipText);

    /// <summary>The rows of icons (for the self-test): four for a hero, where the old column took nine places.</summary>
    public int Rows => (_buttons.Count + 2 + Columns - 1) / Columns;

    public bool Lit(GameWindow window) => _buttons.TryGetValue(window, out Button? b) && b.ButtonPressed;

    /// <summary>
    /// A count on a window's icon, in its top left corner (sprint 151, D82, G9 a: the talent points left to spend); none at 0.
    /// </summary>
    public void SetBadge(GameWindow window, int count, string tip)
    {
        if (!_buttons.TryGetValue(window, out Button? b))
            return;
        if (b.GetNodeOrNull<Label>("Badge") is not Label badge)
        {
            // Over the corner, pistachio with chocolate text, edged in the interface's grey.
            badge = new Label { Name = "Badge", MouseFilter = Control.MouseFilterEnum.Ignore, Position = new Vector2(-4, -4), Size = new Vector2(20, 20), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            badge.AddThemeFontSizeOverride("font_size", 14);
            badge.AddThemeColorOverride("font_color", new Color(UiStyle.Chocolate));
            badge.AddThemeStyleboxOverride("normal", OsmoseTheme.Flat(UiStyle.Pistachio, UiStyle.Window, 2, 10));
            b.AddChild(badge);
        }
        badge.Visible = count > 0;
        badge.Text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        badge.TooltipText = tip;
        Badges[window] = count;
        _header.Badge = HudLayout.Badges(Badges.Values);
    }

    /// <summary>The counts on the icons (for the self-test).</summary>
    public Dictionary<GameWindow, int> Badges { get; } = [];

    private static Button Icon(GridContainer parent, string name, string key)
    {
        // A neutral tile, the icon in its own colours; lit in pistachio while its window is open.
        var b = new Button
        {
            CustomMinimumSize = new Vector2(48, 48),
            Icon = GD.Load<Texture2D>($"res://assets/icons/bar/{name}.svg"),
            ExpandIcon = true,
            IconAlignment = HorizontalAlignment.Center,
            FocusMode = Control.FocusModeEnum.None,
            ThemeTypeVariation = OsmoseTheme.MenuTile,
        };
        // Its key in the bottom right corner, to learn it without hovering (12 px: the tooltip says it too).
        if (key.Length > 0)
        {
            var letter = new Label { Text = key, MouseFilter = Control.MouseFilterEnum.Ignore, AnchorLeft = 1, AnchorRight = 1, AnchorTop = 1, AnchorBottom = 1, OffsetLeft = -14, OffsetTop = -17, OffsetRight = -3, OffsetBottom = -1, HorizontalAlignment = HorizontalAlignment.Right };
            letter.AddThemeFontSizeOverride("font_size", 12);
            letter.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.85f));
            letter.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
            letter.AddThemeConstantOverride("outline_size", 3);
            b.AddChild(letter);
        }
        parent.AddChild(b);
        return b;
    }

    /// <summary>The theme's variation of each icon's tile, top to bottom (for the self-test).</summary>
    public IEnumerable<string> TileStyles => _buttons.Values.Append(_miniMap).Append(_menu).Select(b => b.ThemeTypeVariation.ToString());
}
