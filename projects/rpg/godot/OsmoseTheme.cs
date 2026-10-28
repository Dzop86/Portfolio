using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The interface's one theme (sprint 157, D84, <c>docs/graphisme/s157-maquettes.md</c>): the project's theme
/// (<c>assets/ui/osmose.theme.tres</c>, empty on disk) filled at start from <see cref="UiStyle"/>, so that every window
/// of every canvas layer gets it without a style written by hand. Raised things (tiles, buttons, bar fills, the gold band)
/// are nine-patches drawn by <c>tools/ui_art.py</c>; surfaces to read on stay flat. Named variations: <see cref="MenuTile"/>,
/// <see cref="Primary"/>, <see cref="WindowPanel"/>, <see cref="HudPanel"/>, <see cref="TitleBar"/>, <see cref="CloseButton"/>,
/// <see cref="ListRow"/>, <see cref="GoldBand"/>, <see cref="GoldBandLabel"/>, <see cref="LifeBar"/>, <see cref="GoldBar"/>.
/// </summary>
public static class OsmoseTheme
{
    public const string MenuTile = "MenuTile", Primary = "ButtonPrimary", WindowPanel = "WindowPanel", HudPanel = "HudPanel", TitleBar = "TitleBar",
        CloseButton = "CloseButton", ListRow = "ListRow", GoldBand = "GoldBand", GoldBandLabel = "GoldBandLabel", LifeBar = "LifeBar", GoldBar = "GoldBar";

    /// <summary>The achievements' window's (sprint 158, <c>docs/graphisme/s158-succes.md</c> §6).</summary>
    public const string SearchField = "SearchField", TreeRow = "TreeRow", TreeToggle = "TreeToggle", AchievementTile = "AchievementTile",
        AchievementTileDone = "AchievementTileDone", AchievementCard = "AchievementCard", AchievementCardEarned = "AchievementCardEarned",
        IconFrame = "IconFrame", IconFrameLocked = "IconFrameLocked", DetailPanel = "DetailPanel", SectionLabel = "SectionLabel", Link = "Link",
        ProgressBarCompact = "ProgressBarCompact";

    /// <summary>Every variation the theme defines (for the self-test).</summary>
    public static readonly string[] Variations = [MenuTile, Primary, WindowPanel, HudPanel, TitleBar, CloseButton, ListRow, GoldBand, GoldBandLabel, LifeBar, GoldBar,
        SearchField, TreeRow, TreeToggle, AchievementTile, AchievementTileDone, AchievementCard, AchievementCardEarned, IconFrame, IconFrameLocked, DetailPanel,
        SectionLabel, Link, ProgressBarCompact];

    /// <summary>The bold of the titles: the default font emboldened, spaced a little.</summary>
    public static FontVariation Bold(int spacing = 0) => new() { BaseFont = ThemeDB.FallbackFont, VariationEmbolden = 0.6f, SpacingGlyph = spacing };

    private static Color C(string hex) => new(hex);

    /// <summary>Fills the project's theme once, before any window is built.</summary>
    public static Theme Apply()
    {
        Theme theme = ThemeDB.GetProjectTheme() ?? throw new InvalidOperationException("The project's theme (gui/theme/custom) is missing.");
        if (theme.HasStylebox("normal", MenuTile))
            return theme;
        Fill(theme);
        return theme;
    }

    /// <summary>A raised nine-patch of <c>assets/ui/frames</c>, its body's content in the given margins.</summary>
    public static StyleBoxTexture Raised(string frame, float sides = 16, float top = 6, float bottom = 6) => new()
    {
        Texture = GD.Load<Texture2D>($"res://assets/ui/frames/{frame}.svg"),
        TextureMarginLeft = 12,
        TextureMarginRight = 12,
        TextureMarginTop = 12,
        TextureMarginBottom = 12,
        // The canvas is 56 for a body of 48 at (4, 3): the shadow and the halo spill out of the control.
        ExpandMarginLeft = 4,
        ExpandMarginRight = 4,
        ExpandMarginTop = 3,
        ExpandMarginBottom = 5,
        ContentMarginLeft = sides,
        ContentMarginRight = sides,
        ContentMarginTop = top,
        ContentMarginBottom = bottom,
    };

    /// <summary>A flat box: a ground, an edge, rounded corners.</summary>
    public static StyleBoxFlat Flat(string ground, string? edge = null, int width = 0, int corner = 0, float margin = 0) => new()
    {
        BgColor = C(ground),
        BorderColor = edge is null ? Colors.Transparent : C(edge),
        BorderWidthLeft = width,
        BorderWidthRight = width,
        BorderWidthTop = width,
        BorderWidthBottom = width,
        CornerRadiusTopLeft = corner,
        CornerRadiusTopRight = corner,
        CornerRadiusBottomLeft = corner,
        CornerRadiusBottomRight = corner,
        ContentMarginLeft = margin,
        ContentMarginRight = margin,
        ContentMarginTop = margin,
        ContentMarginBottom = margin,
    };

    /// <summary>A window's panel: the flat dark ground, framed in lightened chocolate, its shadow under it.</summary>
    public static StyleBoxFlat Window(float margin = 16)
    {
        StyleBoxFlat box = Flat(UiStyle.Window, UiStyle.Frame, 2, 8, margin);
        box.ShadowColor = new Color(0, 0, 0, 0.5f);
        box.ShadowSize = 12;
        box.ShadowOffset = new Vector2(0, 6);
        return box;
    }

    private static void Fill(Theme theme)
    {
        // Buttons: the secondary relief by default, the primary in pistachio with chocolate text.
        ButtonStates(theme, "Button", "secondary", "secondary-hover", "secondary-pressed", "primary-disabled", UiStyle.TextStrong, 16);
        theme.SetTypeVariation(Primary, "Button");
        ButtonStates(theme, Primary, "primary", "primary-hover", "primary-pressed", "primary-disabled", UiStyle.Chocolate, 20);
        theme.SetTypeVariation(MenuTile, "Button");
        ButtonStates(theme, MenuTile, "tile", "tile-hover", "tile-pressed", "tile-pressed", UiStyle.TextStrong, 6);
        // An open window's tile stays lit, hovered or not.
        theme.SetStylebox("pressed", MenuTile, Raised("tile-lit", 6));
        theme.SetStylebox("hover_pressed", MenuTile, Raised("tile-lit", 6));
        foreach (string state in new[] { "icon_normal_color", "icon_hover_color", "icon_pressed_color", "icon_hover_pressed_color", "icon_focus_color" })
            theme.SetColor(state, MenuTile, Colors.White);
        theme.SetConstant("icon_max_width", MenuTile, 36);

        theme.SetTypeVariation(CloseButton, "Button");
        theme.SetStylebox("normal", CloseButton, Flat("#00000000", corner: 6, margin: 6));
        theme.SetStylebox("hover", CloseButton, Flat(UiStyle.CloseHover, corner: 6, margin: 6));
        theme.SetStylebox("pressed", CloseButton, Flat("#2a1f1f", corner: 6, margin: 6));
        theme.SetColor("font_color", CloseButton, C(UiStyle.TextDim));
        theme.SetColor("font_hover_color", CloseButton, C(UiStyle.CloseHoverText));

        // Panels: the windows', those laid on the map, a title bar, a row of a list, the gold band of the points.
        theme.SetTypeVariation(WindowPanel, "PanelContainer");
        theme.SetStylebox("panel", WindowPanel, Window());
        theme.SetTypeVariation(HudPanel, "PanelContainer");
        StyleBoxFlat hud = Flat(UiStyle.Hud, UiStyle.Chocolate, 2, 10, 8);
        hud.ShadowColor = new Color(0, 0, 0, 0.5f);
        hud.ShadowSize = 8;
        hud.ShadowOffset = new Vector2(0, 4);
        theme.SetStylebox("panel", HudPanel, hud);
        theme.SetTypeVariation(TitleBar, "PanelContainer");
        StyleBoxFlat title = Flat(UiStyle.TitleBar, UiStyle.FrameDark, 0, 8, 12);
        title.BorderWidthBottom = 2;
        (title.CornerRadiusBottomLeft, title.CornerRadiusBottomRight) = (0, 0);
        theme.SetStylebox("panel", TitleBar, title);
        theme.SetTypeVariation(ListRow, "PanelContainer");
        StyleBoxFlat row = Flat("#00000000", "#ffffff0f", 0, 0, 12);
        row.BorderWidthBottom = 1;
        theme.SetStylebox("panel", ListRow, row);
        theme.SetTypeVariation(GoldBand, "PanelContainer");
        theme.SetStylebox("panel", GoldBand, Raised("gold-band", 20, 4, 4));
        theme.SetTypeVariation(GoldBandLabel, "Label");
        theme.SetColor("font_color", GoldBandLabel, C(UiStyle.GoldText));
        theme.SetFontSize("font_size", GoldBandLabel, 16);

        // Bars: a hollow, filled in relief; the life's and the rewards' in their colours.
        theme.SetStylebox("background", "ProgressBar", Flat(UiStyle.Well, "#000000", 1, 6));
        theme.SetStylebox("fill", "ProgressBar", Raised("fill-progress", 0, 0, 0));
        theme.SetColor("font_color", "ProgressBar", Colors.White);
        theme.SetColor("font_outline_color", "ProgressBar", Colors.Black);
        theme.SetConstant("outline_size", "ProgressBar", 3);
        theme.SetTypeVariation(LifeBar, "ProgressBar");
        theme.SetStylebox("fill", LifeBar, Raised("fill-life", 0, 0, 0));
        theme.SetTypeVariation(GoldBar, "ProgressBar");
        theme.SetStylebox("fill", GoldBar, Raised("fill-gold", 0, 0, 0));

        // Fields: a hollow, outlined in pistachio when focused.
        StyleBoxFlat field = Flat(UiStyle.Well, "#ffffff2e", 1, 6, 12);
        theme.SetStylebox("normal", "LineEdit", field);
        theme.SetStylebox("focus", "LineEdit", Flat("#00000000", UiStyle.Pistachio, 2, 6));
        theme.SetStylebox("read_only", "LineEdit", Flat(UiStyle.Well, "#ffffff14", 1, 6, 12));
        theme.SetColor("font_color", "LineEdit", C(UiStyle.TextStrong));
        theme.SetColor("font_placeholder_color", "LineEdit", C(UiStyle.TextDim));
        theme.SetColor("caret_color", "LineEdit", C(UiStyle.Pistachio));
        theme.SetColor("selection_color", "LineEdit", C(UiStyle.Pistachio + "4d"));

        // Tabs: the chosen one merges with its page, a pistachio line on top.
        StyleBoxFlat chosen = Flat(UiStyle.Window, UiStyle.Pistachio, 0, 6, 16);
        chosen.BorderWidthTop = 3;
        (chosen.CornerRadiusBottomLeft, chosen.CornerRadiusBottomRight) = (0, 0);
        StyleBoxFlat other = Flat(UiStyle.Tab, "#ffffff1f", 0, 6, 16);
        other.BorderWidthBottom = 1;
        (other.CornerRadiusBottomLeft, other.CornerRadiusBottomRight) = (0, 0);
        StyleBoxFlat hovered = Flat(UiStyle.Hover, "#ffffff1f", 0, 6, 16);
        hovered.BorderWidthBottom = 1;
        (hovered.CornerRadiusBottomLeft, hovered.CornerRadiusBottomRight) = (0, 0);
        foreach (string type in new[] { "TabBar", "TabContainer" })
        {
            theme.SetStylebox("tab_selected", type, chosen);
            theme.SetStylebox("tab_unselected", type, other);
            theme.SetStylebox("tab_hovered", type, hovered);
            theme.SetColor("font_selected_color", type, Colors.White);
            theme.SetColor("font_unselected_color", type, C(UiStyle.TextDim));
            theme.SetColor("font_hovered_color", type, C(UiStyle.Text));
            theme.SetColor("font_disabled_color", type, C("#5c5c5c"));
        }
        theme.SetStylebox("panel", "TabContainer", Flat(UiStyle.Window, margin: 8));

        // The tooltips: near black, edged in light chocolate.
        theme.SetStylebox("panel", "TooltipPanel", Flat(UiStyle.Tooltip, UiStyle.ChocolateLight, 1, 6, 10));
        theme.SetColor("font_color", "TooltipLabel", C(UiStyle.Text));

        // The scroll bars: a thin track, a grabber lit in pistachio while dragged.
        foreach (string type in new[] { "VScrollBar", "HScrollBar" })
        {
            theme.SetStylebox("scroll", type, Flat("#ffffff0a", corner: 4, margin: 2));
            theme.SetStylebox("grabber", type, Flat(UiStyle.Scroll, corner: 4, margin: 4));
            theme.SetStylebox("grabber_highlight", type, Flat(UiStyle.ScrollHover, corner: 4, margin: 4));
            theme.SetStylebox("grabber_pressed", type, Flat(UiStyle.Pistachio, corner: 4, margin: 4));
        }

        FillAchievements(theme);
        // The check box's own box (sprint 158): a hollow, or pistachio ticked in chocolate.
        theme.SetIcon("unchecked", "CheckBox", GD.Load<Texture2D>("res://assets/ui/check-off.svg"));
        theme.SetIcon("checked", "CheckBox", GD.Load<Texture2D>("res://assets/ui/check-on.svg"));
        theme.SetIcon("unchecked_disabled", "CheckBox", GD.Load<Texture2D>("res://assets/ui/check-off.svg"));
        theme.SetIcon("checked_disabled", "CheckBox", GD.Load<Texture2D>("res://assets/ui/check-on.svg"));

        // The check boxes and buttons: their text in the interface's grey.
        // Their own boxes, flat: the project's theme is read before Godot's, so without them a check box would take the
        // Button's relief (seen on the achievements' window, sprint 158).
        foreach (string type in new[] { "CheckBox", "CheckButton" })
        {
            foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled", "focus" })
                theme.SetStylebox(state, type, new StyleBoxEmpty { ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 4, ContentMarginBottom = 4 });
            theme.SetConstant("h_separation", type, 8);
            theme.SetColor("font_color", type, C(UiStyle.Text));
            theme.SetColor("font_hover_color", type, C(UiStyle.TextStrong));
            theme.SetColor("font_pressed_color", type, C(UiStyle.TextStrong));
            theme.SetColor("font_disabled_color", type, C(UiStyle.TextOff));
        }
    }

    /// <summary>The achievements' window's pieces (sprint 158): the tree, the tiles, the cards, the icon frames, the detail.</summary>
    private static void FillAchievements(Theme theme)
    {
        theme.SetTypeVariation(SearchField, "LineEdit");
        StyleBoxFlat search = Flat(UiStyle.Well, "#ffffff2e", 1, 6, 12);
        search.ContentMarginLeft = 40;
        theme.SetStylebox("normal", SearchField, search);

        // A row of the tree: flat, lit in the chosen green with a pistachio line on its left.
        theme.SetTypeVariation(TreeRow, "Button");
        StyleBoxFlat chosen = Flat(UiStyle.Selected, UiStyle.Pistachio, 0, 0, 8);
        chosen.BorderWidthLeft = 3;
        foreach ((string state, StyleBox box) in new (string, StyleBox)[] { ("normal", Flat("#00000000", margin: 8)), ("hover", Flat(UiStyle.Hover, margin: 8)),
            ("pressed", chosen), ("hover_pressed", chosen), ("focus", new StyleBoxEmpty()) })
            theme.SetStylebox(state, TreeRow, box);
        theme.SetColor("font_color", TreeRow, C(UiStyle.Text));
        theme.SetColor("font_hover_color", TreeRow, C(UiStyle.TextStrong));
        theme.SetColor("font_pressed_color", TreeRow, Colors.White);
        theme.SetColor("font_hover_pressed_color", TreeRow, Colors.White);
        theme.SetFontSize("font_size", TreeRow, 14);
        theme.SetTypeVariation(TreeToggle, "Button");
        theme.SetStylebox("normal", TreeToggle, Flat("#00000000", corner: 6));
        theme.SetStylebox("hover", TreeToggle, Flat(UiStyle.Hover, corner: 6));
        theme.SetStylebox("pressed", TreeToggle, Flat(UiStyle.Hover, corner: 6));
        theme.SetStylebox("focus", TreeToggle, new StyleBoxEmpty());
        theme.SetColor("font_color", TreeToggle, C(UiStyle.TextDim));
        theme.SetColor("font_hover_color", TreeToggle, C(UiStyle.TextStrong));

        // A category's tile: a reading surface that one clicks, so a shadow; edged in gold once complete.
        foreach ((string type, string edge, int width) in new[] { (AchievementTile, UiStyle.Edge, 1), (AchievementTileDone, UiStyle.Gold, 2) })
        {
            theme.SetTypeVariation(type, "Button");
            theme.SetStylebox("normal", type, Shadowed(Flat(UiStyle.Tab, edge, width, 8)));
            theme.SetStylebox("hover", type, Shadowed(Flat(UiStyle.Hover, UiStyle.ChocolateLight, width, 8)));
            theme.SetStylebox("pressed", type, Flat(UiStyle.Tab, edge, width, 8));
            theme.SetStylebox("focus", type, Flat("#00000000", UiStyle.Pistachio, 2, 8));
        }

        // An achievement's card: grey when locked, edged in a see-through gold when earned, chosen in the green.
        foreach ((string type, string edge) in new[] { (AchievementCard, "#ffffff0f"), (AchievementCardEarned, UiStyle.Gold + "59") })
        {
            theme.SetTypeVariation(type, "Button");
            theme.SetStylebox("normal", type, Flat(UiStyle.Tab, edge, 1, 8, 12));
            theme.SetStylebox("hover", type, Flat(UiStyle.Hover, UiStyle.ChocolateLight, 1, 8, 12));
            theme.SetStylebox("pressed", type, Flat(UiStyle.Selected, UiStyle.Pistachio, 2, 8, 12));
            theme.SetStylebox("hover_pressed", type, Flat(UiStyle.Selected, UiStyle.Pistachio, 2, 8, 12));
            theme.SetStylebox("focus", type, new StyleBoxEmpty());
        }

        // The frame of an achievement's icon: raised and golden when earned, a hollow when not.
        theme.SetTypeVariation(IconFrame, "PanelContainer");
        theme.SetStylebox("panel", IconFrame, Raised("frame-gold", 6, 6, 6));
        theme.SetTypeVariation(IconFrameLocked, "PanelContainer");
        theme.SetStylebox("panel", IconFrameLocked, Raised("frame-well", 6, 6, 6));

        theme.SetTypeVariation(DetailPanel, "PanelContainer");
        theme.SetStylebox("panel", DetailPanel, Flat(UiStyle.Tab, UiStyle.FrameDark, 1, 8, 16));
        theme.SetTypeVariation(SectionLabel, "Label");
        theme.SetFont("font", SectionLabel, Bold(1));
        theme.SetFontSize("font_size", SectionLabel, 14);
        theme.SetColor("font_color", SectionLabel, C(UiStyle.ChocolateLight));

        theme.SetTypeVariation(Link, "Button");
        foreach (string state in new[] { "normal", "hover", "pressed", "focus" })
            theme.SetStylebox(state, Link, new StyleBoxEmpty());
        theme.SetColor("font_color", Link, C(UiStyle.ChocolateLight));
        theme.SetColor("font_hover_color", Link, C(UiStyle.TextStrong));
        theme.SetFontSize("font_size", Link, 14);

        theme.SetTypeVariation(ProgressBarCompact, "ProgressBar");
        theme.SetStylebox("background", ProgressBarCompact, Flat(UiStyle.Well, corner: 4));
        theme.SetStylebox("fill", ProgressBarCompact, Flat(UiStyle.PrimaryBottom, corner: 4));
    }

    private static StyleBoxFlat Shadowed(StyleBoxFlat box)
    {
        box.ShadowColor = new Color(0, 0, 0, 0.5f);
        box.ShadowSize = 6;
        box.ShadowOffset = new Vector2(0, 3);
        return box;
    }

    private static void ButtonStates(Theme theme, string type, string normal, string hover, string pressed, string disabled, string text, float sides)
    {
        theme.SetStylebox("normal", type, Raised(normal, sides));
        theme.SetStylebox("hover", type, Raised(hover, sides));
        // Pressed: the gradient upside down and the content a pixel lower.
        theme.SetStylebox("pressed", type, Raised(pressed, sides, 7, 5));
        theme.SetStylebox("hover_pressed", type, Raised(pressed, sides, 7, 5));
        theme.SetStylebox("disabled", type, Raised(disabled, sides));
        theme.SetStylebox("focus", type, new StyleBoxEmpty());
        theme.SetColor("font_color", type, C(text));
        theme.SetColor("font_hover_color", type, C(text));
        theme.SetColor("font_pressed_color", type, C(text));
        theme.SetColor("font_hover_pressed_color", type, C(text));
        theme.SetColor("font_focus_color", type, C(text));
        theme.SetColor("font_disabled_color", type, C(UiStyle.TextOff));
    }
}
