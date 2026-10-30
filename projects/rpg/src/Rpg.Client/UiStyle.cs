namespace Rpg.Client;

/// <summary>
/// The interface's colours (sprint 157, D84, <c>docs/graphisme/s157-maquettes.md</c>), each a token of
/// <c>src/assets/tokens.css</c> (<c>--osmose-ui-*</c>, or the charter's own): the Godot theme (OsmoseTheme) reads them
/// here, never from a colour written in a window. Hexadecimal, an alpha in the last two digits where there is one.
/// </summary>
public static class UiStyle
{
    // The charter: dark grey, pistachio for the accent (never as text on a light ground), chocolate for the touches.
    public const string Pistachio = "#bef374", PistachioPale = "#e6f8c9", Chocolate = "#5a3a22", ChocolateLight = "#c9a27f", Bamboo = "#4d7a1c";

    // The windows: a flat dark ground for reading, framed in a lightened chocolate.
    public const string Window = "#1f1f1f", Hud = "#1b1b1ceb", TitleBar = "#181818", Frame = "#7a5434", FrameDark = "#3a2616";

    // The raised things (tiles, buttons), their dark edge, and the lit one (a window open, a tab, a row chosen).
    public const string RaisedTop = "#3a3a3d", RaisedBottom = "#262628", RaisedHoverTop = "#46464a", RaisedHoverBottom = "#2e2e31", Edge = "#0d0d0d";
    public const string PrimaryTop = "#d4f79e", PrimaryBottom = "#a3d95a", LitTop = "#3d4f24", LitBottom = "#25301a", Selected = "#33421f";

    /// <summary>The hollow of a bar, a field or a box.</summary>
    public const string Well = "#141414";

    // The gold of the points and the rewards, and the text written on it.
    public const string GoldTop = "#ffe6ab", Gold = "#ffd166", GoldBottom = "#c9962e", GoldText = "#3a2410";

    /// <summary>Where to go and what to hand back (sprint 165, --osmose-cost): the quest followed's hints.</summary>
    public const string Cost = "#f2d36b";

    /// <summary>What is missing or out of reach (sprint 166, --osmose-losing): an ingredient short, a recipe too high.</summary>
    public const string Losing = "#f08070";

    // The texts, from the strongest to the one switched off.
    public const string TextStrong = "#e0e0e0", Text = "#cccccc", TextDim = "#9d9d9d", TextOff = "#6a6a6a";

    public const string Hover = "#2d2d2d", Tab = "#252526", Tooltip = "#181818f7", CloseHover = "#3a2424", CloseHoverText = "#f08070",
        Scroll = "#4a4a4a", ScrollHover = "#6a6a6a";

    /// <summary>Every colour above, for the test that finds each one in <c>tokens.css</c>.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Pistachio, PistachioPale, Chocolate, ChocolateLight, Bamboo, Window, Hud, TitleBar, Frame, FrameDark, RaisedTop, RaisedBottom,
        RaisedHoverTop, RaisedHoverBottom, Edge, PrimaryTop, PrimaryBottom, LitTop, LitBottom, Selected, Well, GoldTop, Gold, GoldBottom,
        GoldText, TextStrong, Text, TextDim, TextOff, Hover, Tab, Tooltip, CloseHover, CloseHoverText, Scroll, ScrollHover, Cost, Losing,
    ];

    /// <summary>The smallest a thing one clicks may be, in pixels (the charter's 44).</summary>
    public const int Target = 44;
}
