using System.Globalization;
using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The achievements' window (sprint 155, D83; redrawn in sprint 158, D84, <c>docs/graphisme/s158-succes.md</c>): the total
/// of points in a gold band; on the left a search field and the tree of the categories and their groups, each with its
/// share; on the right either the general progress and a tile per category, or the achievements of a category or group
/// as cards with the chosen one's detail (its objective, its rewards, its title to wear or take off); below, a box to hide
/// those unlocked and the title worn. Every colour comes from the theme (<see cref="OsmoseTheme"/>).
/// </summary>
public partial class AchievementsPanel : CanvasLayer
{
    /// <summary>The view of the general progress, as <see cref="Shown"/> says it.</summary>
    public const string Overall = "overall";

    private Texts _texts = null!;
    private AchievementView? _view;
    private AchievementOverview? _overview;
    private VBoxContainer _tree = null!, _body = null!;
    private Label _points = null!, _heading = null!, _count = null!, _message = null!, _worn = null!;
    private ProgressBar _bar = null!;
    private LineEdit _search = null!;
    private CheckBox _hide = null!;
    private Button _remove = null!, _claimAll = null!;
    private readonly ButtonGroup _rows = new(), _cards = new();
    private readonly HashSet<string> _open = new(StringComparer.Ordinal);
    private string? _group;

    public event Action<string?>? TitleChosen;

    /// <summary>A reward claimed (sprint 159): an achievement's, or (null) every one claimable.</summary>
    public event Action<string?>? ClaimPressed;
    public event Action? ClosePressed;

    /// <summary>The view chosen: <see cref="Overall"/> or a category's id (for the self-test); a search shows over it.</summary>
    public string? Shown { get; private set; }

    /// <summary>The names of the cards shown, top to bottom (for the self-test).</summary>
    public List<string> Names { get; } = [];

    /// <summary>The tiles of the general progress and the rows of the tree shown (for the self-test).</summary>
    public int Tiles { get; private set; }

    public int TreeRows => _tree.GetChildCount();

    /// <summary>The achievement whose detail is shown, or null (for the self-test).</summary>
    public string? Chosen { get; private set; }

    public void Build(Texts texts)
    {
        _texts = texts;
        Layer = 15;
        var panel = new PanelContainer
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -545, OffsetRight = 545, OffsetTop = -320, OffsetBottom = 320,
            ThemeTypeVariation = OsmoseTheme.WindowPanel,
        };
        panel.AddThemeStyleboxOverride("panel", OsmoseTheme.Window(0));
        AddChild(panel);
        WindowDrag.Make(panel, "achievements");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 0);
        panel.AddChild(column);
        column.AddChild(TitleBar(texts));

        var margin = new MarginContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        foreach (string side in new[] { "left", "right", "top" })
            margin.AddThemeConstantOverride("margin_" + side, side == "top" ? 12 : 16);
        column.AddChild(margin);
        var main = new HBoxContainer();
        main.AddThemeConstantOverride("separation", 16);
        margin.AddChild(main);

        // The left column: the search field, then the tree.
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(300, 0) };
        left.AddThemeConstantOverride("separation", 12);
        main.AddChild(left);
        var field = new Control { CustomMinimumSize = new Vector2(300, 44) };
        _search = new LineEdit { PlaceholderText = texts["achievements.search"], ClearButtonEnabled = true, ThemeTypeVariation = OsmoseTheme.SearchField };
        _search.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _search.TextChanged += _ => Refresh();
        field.AddChild(_search);
        field.AddChild(new TextureRect { Texture = GD.Load<Texture2D>("res://assets/icons/relief/search.svg"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, Position = new Vector2(12, 12), Size = new Vector2(20, 20), MouseFilter = Control.MouseFilterEnum.Ignore });
        left.AddChild(field);
        var treeScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _tree = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _tree.AddThemeConstantOverride("separation", 0);
        treeScroll.AddChild(_tree);
        left.AddChild(treeScroll);

        // The right column: the view's heading, its bar, its body.
        var right = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 8);
        main.AddChild(right);
        var heading = new HBoxContainer { CustomMinimumSize = new Vector2(0, 24) };
        _heading = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true };
        _heading.AddThemeFontOverride("font", OsmoseTheme.Bold());
        _heading.AddThemeFontSizeOverride("font_size", 18);
        heading.AddChild(_heading);
        _count = new Label();
        _count.AddThemeColorOverride("font_color", new Color(UiStyle.Text));
        heading.AddChild(_count);
        right.AddChild(heading);
        _bar = new ProgressBar { CustomMinimumSize = new Vector2(0, 16), ShowPercentage = true, MaxValue = 100 };
        right.AddChild(_bar);
        _body = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        right.AddChild(_body);

        // The foot: the box that hides what is unlocked, the title worn and its button.
        var foot = new MarginContainer();
        foreach (string side in new[] { "left", "right", "bottom", "top" })
            foot.AddThemeConstantOverride("margin_" + side, side == "top" ? 8 : 16);
        column.AddChild(foot);
        var feet = new HBoxContainer { CustomMinimumSize = new Vector2(0, 44) };
        feet.AddThemeConstantOverride("separation", 12);
        foot.AddChild(feet);
        _hide = new CheckBox { Text = texts["achievements.hide-earned"], CustomMinimumSize = new Vector2(0, 44) };
        _hide.Toggled += _ => Refresh();
        feet.AddChild(_hide);
        _message = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", new Color(UiStyle.CloseHoverText));
        feet.AddChild(_message);
        // Every reward at once (sprint 159), shown while there is one.
        _claimAll = new Button { CustomMinimumSize = new Vector2(0, 44), ThemeTypeVariation = OsmoseTheme.Primary, Visible = false };
        _claimAll.Pressed += () => ClaimPressed?.Invoke(null);
        feet.AddChild(_claimAll);
        _worn = new Label { VerticalAlignment = VerticalAlignment.Center };
        _worn.AddThemeColorOverride("font_color", new Color(UiStyle.Text));
        feet.AddChild(_worn);
        _remove = new Button { Text = texts["achievements.remove"], CustomMinimumSize = new Vector2(88, 44), Visible = false };
        _remove.Pressed += () => TitleChosen?.Invoke(null);
        feet.AddChild(_remove);
        Shown = Overall;
    }

    private PanelContainer TitleBar(Texts texts)
    {
        var bar = new PanelContainer { ThemeTypeVariation = OsmoseTheme.TitleBar, CustomMinimumSize = new Vector2(0, 44) };
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        bar.AddChild(row);
        row.AddChild(Icon("achievements", true, 24));
        var title = new Label { Text = texts["achievements.title"], VerticalAlignment = VerticalAlignment.Center };
        title.AddThemeFontOverride("font", OsmoseTheme.Bold());
        title.AddThemeFontSizeOverride("font_size", 18);
        row.AddChild(title);
        var key = new Label { Text = texts["achievements.shortcut", WindowState.Bar.First(b => b.Window == GameWindow.Achievements).Key], VerticalAlignment = VerticalAlignment.Center };
        key.AddThemeColorOverride("font_color", new Color(UiStyle.TextDim));
        row.AddChild(key);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        // The total in the gold band, the window's focus.
        var band = new PanelContainer { ThemeTypeVariation = OsmoseTheme.GoldBand, CustomMinimumSize = new Vector2(160, 32), TooltipText = texts["achievements.band.tip"], SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        var inBand = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        inBand.AddThemeConstantOverride("separation", 6);
        inBand.AddChild(Icon("achievements", true, 20));
        _points = new Label { ThemeTypeVariation = OsmoseTheme.GoldBandLabel, VerticalAlignment = VerticalAlignment.Center };
        _points.AddThemeFontOverride("font", OsmoseTheme.Bold());
        inBand.AddChild(_points);
        band.AddChild(inBand);
        row.AddChild(band);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44), ThemeTypeVariation = OsmoseTheme.CloseButton };
        close.Pressed += () => ClosePressed?.Invoke();
        row.AddChild(close);
        return bar;
    }

    /// <summary>An icon in relief: in its colours, or in the greys of what is locked.</summary>
    private static TextureRect Icon(string name, bool lit, int size) => new()
    {
        Texture = GD.Load<Texture2D>(lit
            ? (name == "dungeons" ? "res://assets/icons/relief/dungeons.svg" : $"res://assets/icons/bar/{name}.svg")
            : $"res://assets/icons/relief/{name}-locked.svg"),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        CustomMinimumSize = new Vector2(size, size),
        MouseFilter = Control.MouseFilterEnum.Ignore,
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
    };

    public void Show(AchievementView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        _view = view;
        _overview = AchievementsPage.Overview(view, _texts);
        _points.Text = AchievementsPage.Grouped(view.Points, _texts.Lang);
        string? title = AchievementsPage.TitleOf(view.Title, _texts.Lang);
        _worn.Text = title is null ? _texts["achievements.worn-none"] : _texts["achievements.worn", title];
        _remove.Visible = title is not null;
        int toClaim = _overview.ToClaim;
        _claimAll.Visible = toClaim > 0;
        _claimAll.Text = _texts["achievements.claim-all", toClaim];
        Refresh();
    }

    /// <summary>The general progress (null), a category, or a group of it.</summary>
    public void ShowCategory(string? id, string? group = null)
    {
        Shown = id ?? Overall;
        _group = group;
        Chosen = null;
        if (id is not null)
            _open.Add(id);
        Refresh();
    }

    /// <summary>Shows an achievement, chosen in its category and group (sprint 159: a crown's "View").</summary>
    public void See(string achievement)
    {
        if (Achievements.All.FirstOrDefault(a => a.Id == achievement) is not AchievementSpec a)
            return;
        _search.Text = "";
        _hide.SetPressedNoSignal(false);
        ShowCategory(a.Category, a.Group);
        Chosen = a.Id;
        Refresh();
    }

    /// <summary>The buttons of the chosen achievement's detail, by their text (for the self-test).</summary>
    public List<string> DetailButtons { get; } = [];

    /// <summary>Hides the achievements unlocked, as the box does (for the self-test).</summary>
    public void HideEarned(bool hide) => _hide.ButtonPressed = hide;

    /// <summary>Searches as the field does (for the self-test).</summary>
    public void Search(string words)
    {
        _search.Text = words;
        Refresh();
    }

    public void Tell(string text) => _message.Text = text;

    private void Refresh()
    {
        if (_view is null || _overview is null)
            return;
        DrawTree(_overview);
        Clear(_body);
        Names.Clear();
        Tiles = 0;
        string words = _search.Text.Trim();
        if (words.Length >= 2)
            DrawLines(_texts["achievements.results", AchievementsPage.Lines(_view, _texts, null, search: words, hideEarned: _hide.ButtonPressed).Count], null, null,
                AchievementsPage.Lines(_view, _texts, null, search: words, hideEarned: _hide.ButtonPressed), words);
        else if (Shown == Overall || _overview.Categories.All(c => c.Id != Shown))
            DrawOverall(_overview);
        else
        {
            AchievementBranch branch = _overview.Categories.First(c => c.Id == Shown);
            AchievementGroup? group = branch.Groups.FirstOrDefault(g => g.Id == _group);
            DrawLines(group is null ? branch.Name : _texts["achievements.path", branch.Name, group.Name], group?.Share ?? branch.Share, branch,
                AchievementsPage.Lines(_view, _texts, branch.Id, group?.Id, hideEarned: _hide.ButtonPressed), null);
        }
    }

    private void Heading(string title, AchievementShare? share)
    {
        _heading.Text = title.ToUpperInvariant();
        _bar.Visible = share is not null;
        _count.Visible = share is not null;
        if (share is null)
            return;
        _count.Text = _texts["achievements.count", share.Earned, share.Count];
        _bar.Value = share.Percent;
        _bar.ThemeTypeVariation = share.Percent >= 100 ? OsmoseTheme.GoldBar : "";
    }

    private void DrawTree(AchievementOverview o)
    {
        Clear(_tree);
        bool searching = _search.Text.Trim().Length >= 2;
        _tree.Modulate = new Color(1, 1, 1, searching ? 0.5f : 1);
        TreeRow(null, _texts["achievements.overall"], "achievements", o.Share, false, Shown == Overall && !searching, () => ShowCategory(null));
        foreach (AchievementBranch c in o.Categories)
        {
            bool open = _open.Contains(c.Id);
            TreeRow(c.Groups.Count > 0 ? open : null, c.Name, c.Icon, c.Share, false, Shown == c.Id && _group is null && !searching, () => ShowCategory(c.Id), () =>
            {
                if (!_open.Remove(c.Id))
                    _open.Add(c.Id);
                Refresh();
            });
            if (open)
                foreach (AchievementGroup g in c.Groups)
                    TreeRow(null, g.Name, null, g.Share, true, Shown == c.Id && _group == g.Id && !searching, () => ShowCategory(c.Id, g.Id));
        }
    }

    /// <summary>A row of the tree: its +/- (if it has groups), its icon or the line of a group, its name and its share.</summary>
    private void TreeRow(bool? open, string name, string? icon, AchievementShare share, bool child, bool chosen, Action pressed, Action? toggle = null)
    {
        var line = new HBoxContainer { CustomMinimumSize = new Vector2(0, 44) };
        line.AddThemeConstantOverride("separation", 0);
        if (open is bool o)
        {
            var sign = new Button { Text = o ? "−" : "+", CustomMinimumSize = new Vector2(44, 44), ThemeTypeVariation = OsmoseTheme.TreeToggle, FocusMode = Control.FocusModeEnum.None, TooltipText = _texts[o ? "achievements.collapse" : "achievements.expand"] };
            sign.Pressed += () => toggle?.Invoke();
            line.AddChild(sign);
        }
        else
            line.AddChild(new Control { CustomMinimumSize = new Vector2(child ? 58 : 44, 44) });
        var row = new Button { ToggleMode = true, ButtonGroup = _rows, ThemeTypeVariation = OsmoseTheme.TreeRow, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 44), Alignment = HorizontalAlignment.Left, ClipText = true, FocusMode = Control.FocusModeEnum.None };
        row.SetPressedNoSignal(chosen);
        if (!child)
            row.AddThemeFontOverride("font", OsmoseTheme.Bold());
        var inRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        inRow.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        inRow.AddThemeConstantOverride("separation", 8);
        if (icon is not null)
            inRow.AddChild(Icon(icon, true, 28));
        else
        {
            // A group hangs from its category by a thin line.
            inRow.AddChild(new ColorRect { Color = new Color(1, 1, 1, 0.12f), CustomMinimumSize = new Vector2(2, 44), MouseFilter = Control.MouseFilterEnum.Ignore });
        }
        var label = new Label { Text = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center, ClipText = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", 14);
        if (!child)
            label.AddThemeFontOverride("font", OsmoseTheme.Bold());
        label.AddThemeColorOverride("font_color", new Color(chosen ? UiStyle.TextStrong : UiStyle.Text));
        inRow.AddChild(label);
        var percent = new Label { Text = _texts["achievements.percent", share.Percent], CustomMinimumSize = new Vector2(48, 0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        percent.AddThemeFontOverride("font", OsmoseTheme.Bold());
        percent.AddThemeFontSizeOverride("font_size", 14);
        percent.AddThemeColorOverride("font_color", new Color(share.Percent >= 100 ? UiStyle.Gold : UiStyle.TextDim));
        inRow.AddChild(percent);
        var pad = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        pad.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        pad.AddThemeConstantOverride("margin_left", 8);
        pad.AddThemeConstantOverride("margin_right", 8);
        pad.AddChild(inRow);
        row.AddChild(pad);
        row.Pressed += pressed;
        line.AddChild(row);
        _tree.AddChild(line);
    }

    /// <summary>The general progress: a tile per category, two a row, its banner, its name, its share.</summary>
    private void DrawOverall(AchievementOverview o)
    {
        Heading(_texts["achievements.overall"], o.Share);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 16);
        scroll.AddChild(grid);
        _body.AddChild(scroll);
        foreach (AchievementBranch c in o.Categories)
        {
            bool done = c.Share.Percent >= 100;
            var tile = new Button { CustomMinimumSize = new Vector2(330, 164), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ThemeTypeVariation = done ? OsmoseTheme.AchievementTileDone : OsmoseTheme.AchievementTile, ClipContents = true };
            var inTile = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            inTile.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            inTile.AddThemeConstantOverride("separation", 0);
            var banner = new TextureRect { Texture = GD.Load<Texture2D>($"res://assets/ui/achievements/banner-{c.Id}.svg"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, CustomMinimumSize = new Vector2(0, 96), MouseFilter = Control.MouseFilterEnum.Ignore };
            if (done)
            {
                TextureRect trophy = Icon("achievements", true, 20);
                trophy.SetAnchorsPreset(Control.LayoutPreset.TopRight);
                trophy.Position = new Vector2(-28, 8);
                banner.AddChild(trophy);
            }
            inTile.AddChild(banner);
            var pad = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
            foreach (string side in new[] { "left", "right", "top", "bottom" })
                pad.AddThemeConstantOverride("margin_" + side, 12);
            var lines = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            lines.AddThemeConstantOverride("separation", 8);
            var top = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            var name = new Label { Text = c.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
            name.AddThemeFontOverride("font", OsmoseTheme.Bold());
            top.AddChild(name);
            var count = new Label { Text = _texts["achievements.progress", c.Share.Earned, c.Share.Count], MouseFilter = Control.MouseFilterEnum.Ignore };
            count.AddThemeFontSizeOverride("font_size", 14);
            count.AddThemeColorOverride("font_color", new Color(UiStyle.Text));
            top.AddChild(count);
            lines.AddChild(top);
            lines.AddChild(new ProgressBar { Value = c.Share.Percent, CustomMinimumSize = new Vector2(0, 16), ShowPercentage = true, ThemeTypeVariation = done ? OsmoseTheme.GoldBar : "", MouseFilter = Control.MouseFilterEnum.Ignore });
            pad.AddChild(lines);
            inTile.AddChild(pad);
            tile.AddChild(inTile);
            string id = c.Id;
            tile.Pressed += () => ShowCategory(id);
            grid.AddChild(tile);
            Tiles++;
        }
    }

    /// <summary>A list of achievements as cards, the chosen one's detail beside it; the category's path instead of the sentence when found by a search.</summary>
    private void DrawLines(string title, AchievementShare? share, AchievementBranch? branch, IReadOnlyList<AchievementLine> lines, string? searched)
    {
        Heading(title, share);
        var row = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 16);
        _body.AddChild(row);
        if (lines.Count == 0)
        {
            row.AddChild(Empty(searched));
            return;
        }
        var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(list);
        row.AddChild(scroll);
        var detail = new PanelContainer { ThemeTypeVariation = OsmoseTheme.DetailPanel, CustomMinimumSize = new Vector2(300, 0) };
        row.AddChild(detail);
        // At first, the first one not yet earned (else the first).
        AchievementLine chosen = lines.FirstOrDefault(l => l.Id == Chosen) ?? lines.FirstOrDefault(l => !l.Earned) ?? lines[0];
        foreach (AchievementLine line in lines)
        {
            list.AddChild(Card(line, line.Id == chosen.Id, searched is null ? null : Path(line), detail));
            Names.Add(line.Name);
        }
        Detail(detail, chosen);
    }

    private string Path(AchievementLine line)
    {
        string category = _texts["achievements.category." + line.Category];
        return line.Group is null ? category : _texts["achievements.path", category, _texts["achievements.group." + line.Group]];
    }

    private VBoxContainer Empty(string? searched)
    {
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
        box.AddThemeConstantOverride("separation", 12);
        TextureRect trophy = Icon("achievements", searched is null, 64);
        trophy.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        box.AddChild(trophy);
        box.AddChild(new Label { Text = searched is null ? _texts["achievements.empty.all-earned"] : _texts["achievements.empty.search", searched], HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart });
        if (searched is not null)
        {
            var clear = new Button { Text = _texts["achievements.empty.clear"], CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
            clear.Pressed += () => Search("");
            box.AddChild(clear);
        }
        return box;
    }

    /// <summary>An achievement's card: its icon in its frame, its name and points, its sentence (or its path), its progress.</summary>
    private Button Card(AchievementLine line, bool chosen, string? path, PanelContainer detail)
    {
        var card = new Button
        {
            ToggleMode = true, ButtonGroup = _cards, CustomMinimumSize = new Vector2(0, 80), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.None,
            ThemeTypeVariation = line.Earned ? OsmoseTheme.AchievementCardEarned : OsmoseTheme.AchievementCard, TooltipText = line.Text, ClipContents = true,
        };
        card.SetPressedNoSignal(chosen);
        var pad = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        pad.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            pad.AddThemeConstantOverride("margin_" + side, side == "left" && line.Earned ? 16 : 12);
        var inCard = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        inCard.AddThemeConstantOverride("separation", 12);
        inCard.AddChild(Framed(line, 52, 40));
        var words = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        words.AddThemeConstantOverride("separation", 2);
        var top = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        top.AddThemeConstantOverride("separation", 4);
        var name = new Label { Text = line.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        name.AddThemeFontOverride("font", OsmoseTheme.Bold());
        name.AddThemeColorOverride("font_color", line.Earned ? Colors.White : new Color(UiStyle.TextStrong));
        top.AddChild(name);
        top.AddChild(Points(line, 14));
        words.AddChild(top);
        var sentence = new Label { Text = path ?? line.Text, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, MouseFilter = Control.MouseFilterEnum.Ignore };
        sentence.AddThemeFontSizeOverride("font_size", 14);
        sentence.AddThemeColorOverride("font_color", new Color(line.Earned ? UiStyle.Text : UiStyle.TextDim));
        words.AddChild(sentence);
        if (!line.Earned && line.Count > 1)
        {
            var progress = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            progress.AddThemeConstantOverride("separation", 8);
            progress.AddChild(new ProgressBar { ThemeTypeVariation = OsmoseTheme.ProgressBarCompact, MaxValue = line.Count, Value = line.Reached, ShowPercentage = false, CustomMinimumSize = new Vector2(200, 8), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore });
            var count = new Label { Text = line.Progress, MouseFilter = Control.MouseFilterEnum.Ignore };
            count.AddThemeFontSizeOverride("font_size", 14);
            count.AddThemeColorOverride("font_color", new Color(UiStyle.TextDim));
            progress.AddChild(count);
            words.AddChild(progress);
        }
        inCard.AddChild(words);
        pad.AddChild(inCard);
        card.AddChild(pad);
        if (line.Earned)
        {
            // The golden strip of an earned one, kept when the card is chosen.
            card.AddChild(new ColorRect { Color = new Color(UiStyle.Gold), Position = new Vector2(0, 12), Size = new Vector2(4, 56), MouseFilter = Control.MouseFilterEnum.Ignore });
        }
        card.Pressed += () =>
        {
            Chosen = line.Id;
            Detail(detail, line);
        };
        return card;
    }

    /// <summary>The icon of an achievement in its frame: gold and in colour when earned, a hollow and grey when not.</summary>
    private static PanelContainer Framed(AchievementLine line, int frame, int icon)
    {
        var box = new PanelContainer { ThemeTypeVariation = line.Earned ? OsmoseTheme.IconFrame : OsmoseTheme.IconFrameLocked, CustomMinimumSize = new Vector2(frame, frame), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        box.AddChild(Icon(line.Icon, line.Earned, icon));
        return box;
    }

    /// <summary>The points of an achievement and a small trophy: gold when earned.</summary>
    private static HBoxContainer Points(AchievementLine line, int size)
    {
        var points = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        points.AddThemeConstantOverride("separation", 4);
        var value = new Label { Text = line.Points.ToString(CultureInfo.InvariantCulture), MouseFilter = Control.MouseFilterEnum.Ignore, VerticalAlignment = VerticalAlignment.Center };
        value.AddThemeFontOverride("font", OsmoseTheme.Bold());
        value.AddThemeFontSizeOverride("font_size", size);
        value.AddThemeColorOverride("font_color", new Color(line.Earned ? UiStyle.Gold : UiStyle.TextDim));
        points.AddChild(value);
        points.AddChild(Icon("achievements", line.Earned, 16));
        return points;
    }

    /// <summary>The chosen achievement's detail: its head, its sentence, its objective, its rewards and its title.</summary>
    private void Detail(PanelContainer detail, AchievementLine line)
    {
        Chosen = line.Id;
        Clear(detail);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(column);
        detail.AddChild(scroll);
        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 12);
        head.AddChild(Framed(line, 64, 52));
        var named = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var name = new Label { Text = line.Name, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(180, 0) };
        name.AddThemeFontOverride("font", OsmoseTheme.Bold());
        name.AddThemeFontSizeOverride("font_size", 18);
        named.AddChild(name);
        named.AddChild(Points(line, 14));
        if (line.Earned)
        {
            var unlocked = new Label { Text = _texts["achievements.unlocked"] };
            unlocked.AddThemeColorOverride("font_color", new Color(UiStyle.Gold));
            named.AddChild(unlocked);
        }
        head.AddChild(named);
        column.AddChild(head);
        column.AddChild(Text(line.Text, UiStyle.Text));

        DetailButtons.Clear();
        Section(column, line.Required.Count > 0 ? "achievements.required" : "achievements.objectives");
        if (line.Required.Count > 0)
        {
            // A crown: each achievement it asks, earned or not, and a way to see it.
            foreach (AchievementLine needed in line.Required)
            {
                var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 44) };
                row.AddThemeConstantOverride("separation", 8);
                row.AddChild(Icon(needed.Icon, needed.Earned, 24));
                var neededName = new Label { Text = needed.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(140, 0) };
                neededName.AddThemeFontSizeOverride("font_size", 14);
                neededName.AddThemeColorOverride("font_color", new Color(needed.Earned ? UiStyle.Gold : UiStyle.Text));
                row.AddChild(neededName);
                var see = new Button { Text = _texts["achievements.see"], ThemeTypeVariation = OsmoseTheme.Link, CustomMinimumSize = new Vector2(64, 44) };
                string id = needed.Id;
                see.Pressed += () => See(id);
                row.AddChild(see);
                column.AddChild(row);
            }
        }
        else if (line.Quests.Count > 0)
        {
            // A line of quests: each one, ticked once handed back.
            foreach ((string quest, bool done) in line.Quests)
                column.AddChild(new CheckBox { Text = quest, ButtonPressed = done, MouseFilter = Control.MouseFilterEnum.Ignore, FocusMode = Control.FocusModeEnum.None, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(240, 0) });
        }
        else if (line.Count == 1)
        {
            var box = new CheckBox { Text = line.Name, ButtonPressed = line.Earned, MouseFilter = Control.MouseFilterEnum.Ignore, FocusMode = Control.FocusModeEnum.None };
            column.AddChild(box);
        }
        else
        {
            var progress = new HBoxContainer();
            progress.AddThemeConstantOverride("separation", 8);
            var count = new Label { Text = line.Progress, VerticalAlignment = VerticalAlignment.Center };
            count.AddThemeFontOverride("font", OsmoseTheme.Bold());
            progress.AddChild(count);
            progress.AddChild(new ProgressBar { MaxValue = line.Count, Value = line.Reached, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 16), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, ThemeTypeVariation = line.Earned ? OsmoseTheme.GoldBar : "" });
            column.AddChild(progress);
        }

        Section(column, "achievements.rewards");
        var points = new HBoxContainer();
        points.AddThemeConstantOverride("separation", 8);
        points.AddChild(Icon("achievements", true, 20));
        points.AddChild(Text(_texts["achievements.reward.points", line.Points], UiStyle.Text));
        column.AddChild(points);
        // Experience and Éclats at the achievement's reference level (sprint 159), claimed once.
        foreach ((string key, int value) in new[] { ("achievements.reward.xp", line.Reward.Xp), ("achievements.reward.shards", line.Reward.Shards) })
        {
            var reward = new HBoxContainer();
            reward.AddThemeConstantOverride("separation", 8);
            var amount = new Label { Text = _texts[key, value] };
            amount.AddThemeColorOverride("font_color", new Color(UiStyle.GoldTop));
            reward.AddChild(new Control { CustomMinimumSize = new Vector2(20, 0) });
            reward.AddChild(amount);
            column.AddChild(reward);
        }
        if (line.Claimed)
            column.AddChild(Text(_texts["achievements.claimed"], UiStyle.TextDim));
        else
        {
            var claim = new Button { Text = _texts["achievements.claim"], CustomMinimumSize = new Vector2(0, 44), ThemeTypeVariation = OsmoseTheme.Primary, Disabled = !line.Earned };
            string id = line.Id;
            claim.Pressed += () => ClaimPressed?.Invoke(id);
            column.AddChild(claim);
            DetailButtons.Add(claim.Text);
        }
        if (line.Title is string title)
        {
            column.AddChild(Text(_texts["achievements.reward.title", title], UiStyle.Text));
            bool worn = _view?.Title == line.Id;
            var wear = new Button { Text = _texts[worn ? "achievements.remove-title" : "achievements.wear-title"], CustomMinimumSize = new Vector2(0, 44), Disabled = !line.Earned };
            if (!line.Earned)
                wear.TooltipText = _texts["achievements.title-locked"];
            string id = line.Id;
            wear.Pressed += () => TitleChosen?.Invoke(worn ? null : id);
            column.AddChild(wear);
        }
    }

    /// <summary>Takes a container's children out at once (freed later), so that it counts only the new ones.</summary>
    private static void Clear(Node container)
    {
        foreach (Node n in container.GetChildren())
        {
            container.RemoveChild(n);
            n.QueueFree();
        }
    }

    private static Label Text(string text, string colour)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(240, 0) };
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", new Color(colour));
        return label;
    }

    private void Section(VBoxContainer column, string key)
    {
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        column.AddChild(new Label { Text = _texts[key].ToUpperInvariant(), ThemeTypeVariation = OsmoseTheme.SectionLabel });
        column.AddChild(new ColorRect { Color = new Color(1, 1, 1, 0.06f), CustomMinimumSize = new Vector2(0, 1) });
    }
}
