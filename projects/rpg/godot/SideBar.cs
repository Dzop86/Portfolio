using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The side bar over the town (sprint 69): a column of discreet icons on the right edge, one per
/// window of <see cref="WindowState.Bar"/> then the menu; hovering one shows its name and key, a click
/// opens or closes its window, the open one stays lit. The icons are game-icons.net's (CC BY 3.0,
/// authors in <c>assets/icons/bar/credits.json</c>).
/// </summary>
public partial class SideBar : CanvasLayer
{
    private readonly Dictionary<GameWindow, Button> _buttons = [];
    private Button _menu = null!;

    public event Action<GameWindow>? WindowPressed;
    public event Action? MenuPressed;

    public void Build(Texts texts, bool withCharacter)
    {
        var column = new VBoxContainer { AnchorLeft = 1, AnchorRight = 1, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -64, OffsetRight = -12, OffsetTop = -150, OffsetBottom = 150, Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 8);
        AddChild(column);
        foreach ((GameWindow window, _) in WindowState.Bar)
        {
            Button b = Icon(column, window.ToString().ToLowerInvariant());
            b.ToggleMode = true;
            // The character's windows need a character (not the classless hero of the self-tests).
            b.Visible = withCharacter || window == GameWindow.Map;
            b.Pressed += () => WindowPressed?.Invoke(window);
            _buttons[window] = b;
        }
        _menu = Icon(column, "menu");
        _menu.Pressed += () => MenuPressed?.Invoke();
        SetTexts(texts);
    }

    public void SetTexts(Texts texts)
    {
        foreach ((GameWindow window, char key) in WindowState.Bar)
            _buttons[window].TooltipText = texts["bar.tip", texts["bar." + window], key];
        _menu.TooltipText = texts["bar.tip", texts["bar.menu"], texts["menu.key"]];
    }

    /// <summary>Lights the open window's icon, and only it.</summary>
    public void Show(GameWindow? open)
    {
        foreach ((GameWindow window, Button b) in _buttons)
            b.SetPressedNoSignal(window == open);
    }

    /// <summary>The tooltips, top to bottom, of the icons shown (for the self-test).</summary>
    public IEnumerable<string> Tips => _buttons.Values.Where(b => b.Visible).Append(_menu).Select(b => b.TooltipText);

    public bool Lit(GameWindow window) => _buttons[window].ButtonPressed;

    private static Button Icon(VBoxContainer parent, string name)
    {
        var b = new Button
        {
            CustomMinimumSize = new Vector2(52, 52),
            Icon = GD.Load<Texture2D>($"res://assets/icons/bar/{name}.svg"),
            ExpandIcon = true,
            IconAlignment = HorizontalAlignment.Center,
            FocusMode = Control.FocusModeEnum.None,
            Modulate = new Color(1, 1, 1, 0.8f),
        };
        // Discreet: a dark rounded tile, lit in the accent when its window is open.
        b.AddThemeStyleboxOverride("normal", Tile(new Color(0.12f, 0.12f, 0.12f, 0.75f), new Color(1, 1, 1, 0.15f)));
        b.AddThemeStyleboxOverride("hover", Tile(new Color(0.2f, 0.2f, 0.2f, 0.9f), new Color(1, 1, 1, 0.4f)));
        b.AddThemeStyleboxOverride("pressed", Tile(new Color(0.2f, 0.25f, 0.15f, 0.95f), new Color("bef374")));
        b.AddThemeStyleboxOverride("hover_pressed", Tile(new Color(0.24f, 0.3f, 0.18f, 0.95f), new Color("bef374")));
        b.AddThemeConstantOverride("icon_max_width", 34);
        parent.AddChild(b);
        return b;
    }

    private static StyleBoxFlat Tile(Color bg, Color border) => new()
    {
        BgColor = bg,
        BorderColor = border,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        CornerRadiusTopLeft = 10,
        CornerRadiusTopRight = 10,
        CornerRadiusBottomLeft = 10,
        CornerRadiusBottomRight = 10,
        ContentMarginLeft = 8,
        ContentMarginRight = 8,
        ContentMarginTop = 8,
        ContentMarginBottom = 8,
    };
}
