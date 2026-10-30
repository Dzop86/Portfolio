using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The header the panels laid on the map fold to (sprint 165, D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §1.3 and
/// §2.3): a flat button 44 high across the panel, its pictogram, its title in bold capitals of light chocolate, what it
/// still says folded (a count, or a badge) and the chevron towards where it folds; the only part of the panel that stops
/// the pointer. <see cref="HudFile"/> keeps the panels' state.
/// </summary>
public partial class HudHeader : Button
{
    private readonly Label _title = new() { VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
    private readonly Label _count = new() { VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore, Visible = false };
    private readonly Label _badge = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(20, 20), Visible = false };
    private readonly TextureRect _chevron = new() { StretchMode = TextureRect.StretchModeEnum.KeepCentered, MouseFilter = MouseFilterEnum.Ignore };
    private HBoxContainer _row = null!;

    /// <summary>Lays out the header, <paramref name="pictogram"/> on its left.</summary>
    public void Build(Texture2D pictogram)
    {
        ThemeTypeVariation = OsmoseTheme.HudHeader;
        FocusMode = FocusModeEnum.None;
        var row = _row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.SetAnchorsPreset(LayoutPreset.FullRect);
        (row.OffsetLeft, row.OffsetRight) = (8, -8);
        row.AddThemeConstantOverride("separation", 6);
        AddChild(row);
        row.AddChild(new TextureRect { Texture = pictogram, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, CustomMinimumSize = new Vector2(20, 20), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter });
        _title.AddThemeFontOverride("font", OsmoseTheme.Bold(1));
        _title.AddThemeFontSizeOverride("font_size", 14);
        _title.AddThemeColorOverride("font_color", new Color(UiStyle.ChocolateLight));
        row.AddChild(_title);
        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        _count.AddThemeFontSizeOverride("font_size", 14);
        _count.AddThemeColorOverride("font_color", new Color(UiStyle.Text));
        row.AddChild(_count);
        // The badge of the menus' tiles (s157 §3.3): pistachio, its text in chocolate.
        _badge.AddThemeFontSizeOverride("font_size", 14);
        _badge.AddThemeColorOverride("font_color", new Color(UiStyle.Chocolate));
        _badge.AddThemeStyleboxOverride("normal", OsmoseTheme.Flat(UiStyle.Pistachio, UiStyle.Window, 2, 10));
        _badge.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(_badge);
        row.AddChild(_chevron);
        Show(false, "", "");
    }

    /// <summary>The theme's panel for the map, its top and bottom margins changed: a header from the panel's top edge.</summary>
    public static StyleBox Panel(float top, float bottom)
    {
        var box = (StyleBox)OsmoseTheme.Apply().GetStylebox("panel", OsmoseTheme.HudPanel).Duplicate();
        (box.ContentMarginTop, box.ContentMarginBottom) = (top, bottom);
        return box;
    }

    public string Title
    {
        get => _title.Text;
        set
        {
            _title.Text = value;
            Fit();
        }
    }

    public bool Folded { get; private set; }

    /// <summary>Folded or not, its chevron (down to fold, up to unfold) and the tooltip that says what a click does.</summary>
    public void Show(bool folded, string foldTip, string unfoldTip)
    {
        Folded = folded;
        _chevron.Texture = GD.Load<Texture2D>($"res://assets/ui/chevron-{(folded ? "up" : "down")}.svg");
        TooltipText = folded ? unfoldTip : foldTip;
        Fit();
    }

    /// <summary>A button takes no width from the row laid over it: as wide as its content, 44 high.</summary>
    private void Fit() => CustomMinimumSize = new Vector2(_row.GetCombinedMinimumSize().X + 16, HudLayout.Header);

    /// <summary>What it still says folded: a count in text ("1/2"), none when empty.</summary>
    public string Count
    {
        get => _count.Visible ? _count.Text : "";
        set
        {
            (_count.Text, _count.Visible) = (value, value.Length > 0);
            Fit();
        }
    }

    /// <summary>The badge folded (the sum of the tiles'), none at 0.</summary>
    public int Badge
    {
        get => _badge.Visible ? int.Parse(_badge.Text, System.Globalization.CultureInfo.InvariantCulture) : 0;
        set
        {
            (_badge.Text, _badge.Visible) = (value.ToString(System.Globalization.CultureInfo.InvariantCulture), value > 0);
            Fit();
        }
    }
}

/// <summary>The folding panels' state on this computer (<see cref="HudSettings"/>); the self-tests keep nothing.</summary>
public static class HudFile
{
    private const string Path = "user://" + HudSettings.FileName;

    /// <summary>Whether a change is written; false during the self-tests.</summary>
    public static bool Keep { get; set; } = true;

    private static HudSettings? _settings;

    public static HudSettings Settings => _settings ??= Load();

    private static HudSettings Load()
    {
        if (!Keep)
            return new HudSettings();
        try
        {
            if (Godot.FileAccess.FileExists(Path))
            {
                using var file = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Read);
                return HudSettings.Parse(file?.GetAsText());
            }
        }
        catch (IOException)
        {
            // The defaults.
        }
        return new HudSettings();
    }

    public static void Save(HudSettings settings)
    {
        _settings = settings;
        if (!Keep)
            return;
        using var file = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Write);
        file?.StoreString(settings.ToText());
    }
}
