using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The menus over the town (sprint 69; a grid since Charles's playtest of sprint 144, the way Dofus does it): a small
/// "Menus" panel at the bottom of the right edge, three icons a row, one per window of <see cref="WindowState.Bar"/>
/// then the mini-map and the menu, each in its own colour with its key in a corner; hovering one shows its name and key,
/// a click opens or closes its window, the open one stays lit. The icons are game-icons.net's (CC BY 3.0, authors in
/// <c>assets/icons/bar/credits.json</c>).
/// </summary>
public partial class SideBar : CanvasLayer
{
    /// <summary>Icons a row: the ten of a hero fill four rows (the tree's since sprint 148), under half the old column's height.</summary>
    public const int Columns = 3;

    private readonly Dictionary<GameWindow, Button> _buttons = [];
    private Button _menu = null!;
    private Label _title = null!;

    public event Action<GameWindow>? WindowPressed;
    public event Action? MenuPressed;

    /// <summary>The mini-map's icon (sprint 108), after the windows': it shows or hides the mini-map, lit while shown.</summary>
    public event Action? MiniMapPressed;

    private Button _miniMap = null!;

    // Each icon's colour, so that one finds it by its colour as much as by its drawing.
    private static readonly Dictionary<string, Color> Hues = new()
    {
        ["characteristics"] = new Color("e8a33d"),
        ["spells"] = new Color("b48cff"),
        ["inventory"] = new Color("d09a5c"),
        ["map"] = new Color("7fd06b"),
        ["journal"] = new Color("f2d36b"),
        ["jobs"] = new Color("a9bccd"),
        ["contacts"] = new Color("6bb8f2"),
        ["talents"] = new Color("bef374"),
        ["guild"] = new Color("f2876b"),
        ["minimap"] = new Color("4fd1c5"),
        ["menu"] = new Color("cfcfcf"),
    };

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
        StyleBoxFlat frame = Tile(new Color(0.1f, 0.1f, 0.12f, 0.88f), new Color(1, 1, 1, 0.12f), 8);
        frame.ContentMarginTop = 4;
        panel.AddThemeStyleboxOverride("panel", frame);
        AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        panel.AddChild(column);
        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1, 1, 1, 0.7f) };
        _title.AddThemeFontSizeOverride("font_size", 12);
        column.AddChild(_title);
        var grid = new GridContainer { Columns = Columns };
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
    }

    public void SetTexts(Texts texts)
    {
        _title.Text = texts["bar.title"];
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
            badge = new Label { Name = "Badge", MouseFilter = Control.MouseFilterEnum.Ignore, Position = new Vector2(2, 0), Size = new Vector2(20, 18), HorizontalAlignment = HorizontalAlignment.Center };
            badge.AddThemeFontSizeOverride("font_size", 12);
            badge.AddThemeColorOverride("font_color", new Color("5a3a22"));
            badge.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color("bef374"), CornerRadiusTopLeft = 9, CornerRadiusTopRight = 9, CornerRadiusBottomLeft = 9, CornerRadiusBottomRight = 9 });
            b.AddChild(badge);
        }
        badge.Visible = count > 0;
        badge.Text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        badge.TooltipText = tip;
        Badges[window] = count;
    }

    /// <summary>The counts on the icons (for the self-test).</summary>
    public Dictionary<GameWindow, int> Badges { get; } = [];

    private static Button Icon(GridContainer parent, string name, string key)
    {
        Color hue = Hues[name];
        var b = new Button
        {
            CustomMinimumSize = new Vector2(48, 48),
            Icon = GD.Load<Texture2D>($"res://assets/icons/bar/{name}.svg"),
            ExpandIcon = true,
            IconAlignment = HorizontalAlignment.Center,
            FocusMode = Control.FocusModeEnum.None,
        };
        // A tile tinted by its colour, its drawing in that colour; lit in the accent when its window is open.
        b.AddThemeStyleboxOverride("normal", Tile(hue.Darkened(0.78f), hue.Darkened(0.45f), 8));
        b.AddThemeStyleboxOverride("hover", Tile(hue.Darkened(0.65f), hue, 8));
        b.AddThemeStyleboxOverride("pressed", Tile(hue.Darkened(0.6f), new Color("bef374"), 8, 2));
        b.AddThemeStyleboxOverride("hover_pressed", Tile(hue.Darkened(0.55f), new Color("bef374"), 8, 2));
        foreach (string state in new[] { "icon_normal_color", "icon_pressed_color", "icon_focus_color" })
            b.AddThemeColorOverride(state, hue);
        b.AddThemeColorOverride("icon_hover_color", hue.Lightened(0.3f));
        b.AddThemeColorOverride("icon_hover_pressed_color", hue.Lightened(0.3f));
        b.AddThemeConstantOverride("icon_max_width", 30);
        // Its key in the bottom right corner, to learn it without hovering.
        if (key.Length > 0)
        {
            var letter = new Label { Text = key, MouseFilter = Control.MouseFilterEnum.Ignore, AnchorLeft = 1, AnchorRight = 1, AnchorTop = 1, AnchorBottom = 1, OffsetLeft = -14, OffsetTop = -17, OffsetRight = -3, OffsetBottom = -1, HorizontalAlignment = HorizontalAlignment.Right };
            letter.AddThemeFontSizeOverride("font_size", 11);
            letter.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.75f));
            letter.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
            letter.AddThemeConstantOverride("outline_size", 3);
            b.AddChild(letter);
        }
        parent.AddChild(b);
        return b;
    }

    private static StyleBoxFlat Tile(Color bg, Color border, int corner, int width = 1) => new()
    {
        BgColor = bg,
        BorderColor = border,
        BorderWidthLeft = width,
        BorderWidthRight = width,
        BorderWidthTop = width,
        BorderWidthBottom = width,
        CornerRadiusTopLeft = corner,
        CornerRadiusTopRight = corner,
        CornerRadiusBottomLeft = corner,
        CornerRadiusBottomRight = corner,
        ContentMarginLeft = 6,
        ContentMarginRight = 6,
        ContentMarginTop = 6,
        ContentMarginBottom = 6,
    };
}
