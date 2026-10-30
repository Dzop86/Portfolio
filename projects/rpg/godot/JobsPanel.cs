using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The trades window (sprint 100, key J, D72): on the left the trades, gathering then making, each a row with its icon in
/// relief, its name, its level and an experience bar (sprint 164, docs/graphisme/s163-icones-arbre.md §2.3); on the right, for the trade chosen, its recipes (searched, filtered by level and kind,
/// sorted, the possible ones only if asked) or its resources and where they grow. It only draws
/// <see cref="JobBook"/>; at a workshop, "Make" and "All (n)" on that trade's recipes, the server making them.
/// </summary>
public partial class JobsPanel : CanvasLayer
{
    private JobBook _book = null!;
    private Job _job = null!;
    private bool _resources;
    private VBoxContainer _jobs = null!, _list = null!;
    private Label _title = null!, _message = null!;
    private Button _recipesTab = null!, _resourcesTab = null!, _close = null!;
    private LineEdit _search = null!;
    private SpinBox _from = null!, _to = null!;
    private OptionButton _kind = null!, _order = null!;
    private CheckBox _possible = null!;
    private readonly List<(Job Job, Button Button)> _jobButtons = [];

    public Texts Texts { get; private set; } = new("en");

    /// <summary>The trade, the item to make and how many.</summary>
    public event Action<Job, string, int>? MakePressed;

    public event Action? ClosePressed;

    /// <summary>The trade shown (for the self-test and the workshop).</summary>
    public Job Shown => _job;

    public void Build(JobBook book, Texts texts, string? job = null)
    {
        Texts = texts;
        _book = book;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -560, OffsetRight = 560, OffsetTop = -320, OffsetBottom = 320 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "jobs");
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 24);
        column.AddChild(_title);
        var halves = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        halves.AddThemeConstantOverride("separation", 16);
        column.AddChild(halves);

        // The trades.
        var left = new ScrollContainer { CustomMinimumSize = new Vector2(360, 0), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        halves.AddChild(left);
        _jobs = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _jobs.AddThemeConstantOverride("separation", 6);
        left.AddChild(_jobs);

        // The trade chosen: tabs, tools, list.
        var right = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 6);
        halves.AddChild(right);
        var tabs = new HBoxContainer();
        right.AddChild(tabs);
        _recipesTab = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(150, 44) };
        _recipesTab.Pressed += () => ShowTab(resources: false);
        tabs.AddChild(_recipesTab);
        _resourcesTab = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(150, 44) };
        _resourcesTab.Pressed += () => ShowTab(resources: true);
        tabs.AddChild(_resourcesTab);
        var tools = new HBoxContainer();
        tools.AddThemeConstantOverride("separation", 6);
        right.AddChild(tools);
        _search = new LineEdit { CustomMinimumSize = new Vector2(180, 44), ClearButtonEnabled = true };
        _search.TextChanged += t => Refilter(() => _book.Search = t);
        tools.AddChild(_search);
        tools.AddChild(_fromLabel = new Label { VerticalAlignment = VerticalAlignment.Center });
        _from = new SpinBox { MinValue = 1, MaxValue = Crafting.MaxLevel, Value = 1, CustomMinimumSize = new Vector2(80, 44) };
        _from.ValueChanged += v => Refilter(() => _book.MinLevel = (int)v);
        tools.AddChild(_from);
        tools.AddChild(_toLabel = new Label { VerticalAlignment = VerticalAlignment.Center });
        _to = new SpinBox { MinValue = 1, MaxValue = Crafting.MaxLevel, Value = Crafting.MaxLevel, CustomMinimumSize = new Vector2(80, 44) };
        _to.ValueChanged += v => Refilter(() => _book.MaxLevel = (int)v);
        tools.AddChild(_to);
        var tools2 = new HBoxContainer();
        tools2.AddThemeConstantOverride("separation", 6);
        right.AddChild(tools2);
        _kind = new OptionButton { CustomMinimumSize = new Vector2(200, 44) };
        _kind.ItemSelected += i => Refilter(() => _book.Kind = i == 0 ? null : _kind.GetItemMetadata((int)i).AsString());
        tools2.AddChild(_kind);
        _order = new OptionButton { CustomMinimumSize = new Vector2(200, 44) };
        _order.ItemSelected += i => Refilter(() => _book.Order = (RecipeOrder)(int)i);
        tools2.AddChild(_order);
        _possible = new CheckBox { CustomMinimumSize = new Vector2(0, 44) };
        _possible.Toggled += on => Refilter(() => _book.PossibleOnly = on);
        tools2.AddChild(_possible);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        right.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_list);

        _message = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", FighterView.PlayerColour);
        column.AddChild(_message);
        _close = new Button { CustomMinimumSize = new Vector2(160, 44), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        _close.Pressed += () => ClosePressed?.Invoke();
        column.AddChild(_close);
        _job = (job is not null ? book.Data.Jobs.GetValueOrDefault(job) : null) ?? book.Jobs[0].Job;
        Show(book);
    }

    private Label _fromLabel = null!, _toLabel = null!;

    private void Refilter(Action change)
    {
        change();
        ShowList();
    }

    private void ShowTab(bool resources)
    {
        _resources = resources;
        ShowList();
    }

    /// <summary>The window again (after something made, the character's inventory and trades changed).</summary>
    public void Show(JobBook book)
    {
        JobBook old = _book;
        _book = book;
        (book.Search, book.MinLevel, book.MaxLevel, book.Kind, book.Order, book.PossibleOnly) = (old.Search, old.MinLevel, old.MaxLevel, old.Kind, old.Order, old.PossibleOnly);
        _title.Text = Texts["jobs.title"];
        _recipesTab.Text = Texts["jobs.recipes"];
        _resourcesTab.Text = Texts["jobs.resources"];
        _search.PlaceholderText = Texts["jobs.search"];
        _fromLabel.Text = Texts["jobs.from"];
        _toLabel.Text = Texts["jobs.to"];
        _possible.Text = Texts["jobs.possible"];
        _close.Text = Texts["points.close"];
        _order.Clear();
        foreach (RecipeOrder o in Enum.GetValues<RecipeOrder>())
            _order.AddItem(Texts["jobs.order." + o]);
        _order.Select((int)book.Order);
        foreach (Node n in _jobs.GetChildren())
            n.QueueFree();
        _jobButtons.Clear();
        bool? gathering = null;
        foreach (JobLine line in book.Jobs)
        {
            if (gathering != line.Gathers)
            {
                gathering = line.Gathers;
                var head = new Label { Text = Texts[line.Gathers ? "jobs.gathering" : "jobs.making"].ToUpperInvariant(), ThemeTypeVariation = OsmoseTheme.SectionLabel };
                _jobs.AddChild(head);
            }
            Button b = Row(line);
            Job job = line.Job;
            b.Pressed += () =>
            {
                _job = job;
                (_book.Kind, _kind.Selected) = (null, 0);
                Show(_book);
            };
            _jobs.AddChild(b);
            _jobButtons.Add((job, b));
        }
        foreach ((Job job, Button b) in _jobButtons)
            b.SetPressedNoSignal(job == _job);
        _kind.Clear();
        _kind.AddItem(Texts["jobs.all-kinds"]);
        foreach (string kind in book.Kinds(_job))
        {
            _kind.AddItem(Enum.TryParse<ItemKind>(kind, out _) ? Texts["kind." + kind] : Texts["slot." + kind]);
            _kind.SetItemMetadata(_kind.ItemCount - 1, kind);
        }
        _message.Text = book.Workshop is null ? Texts["jobs.look"] : "";
        ShowList();
    }

    private void ShowList()
    {
        _recipesTab.SetPressedNoSignal(!_resources);
        _resourcesTab.SetPressedNoSignal(_resources);
        foreach (Node n in _list.GetChildren())
            n.QueueFree();
        if (_resources)
        {
            foreach (ResourceLine r in _book.Resources(_job))
                _list.AddChild(ResourceRow(r));
            return;
        }
        IReadOnlyList<RecipeLine> recipes = _book.Recipes(_job);
        if (recipes.Count == 0)
            _list.AddChild(new Label { Text = Texts["jobs.none"], Modulate = new Color(1, 1, 1, 0.6f) });
        foreach (RecipeLine r in recipes)
            _list.AddChild(RecipeRow(r));
    }

    private HBoxContainer RecipeRow(RecipeLine r)
    {
        var line = new HBoxContainer();
        line.AddThemeConstantOverride("separation", 8);
        (Button tile, ItemIcon icon) = ItemIcon.Tile(48);
        (icon.Glyph, icon.Item, icon.Tint) = (ItemStyle.GlyphOf(r.Item), r.Item, new Color(ItemStyle.Colour(r.Item)));
        tile.TooltipText = ItemMenu.Tooltip(r.Item, Texts);
        line.AddChild(tile);
        var text = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        line.AddChild(text);
        var name = new Label { Text = Texts["jobs.recipe", r.Item.Name.In(Texts.Lang), r.Recipe.Level, r.Xp] };
        if (r.TooHigh)
            name.AddThemeColorOverride("font_color", new Color("ff7a7a"));
        text.AddChild(name);
        // Each ingredient as its icon (Charles, sprint 101): how many the recipe needs on it, owned / needed under it, in red if missing.
        var needs = new HBoxContainer();
        needs.AddThemeConstantOverride("separation", 6);
        text.AddChild(needs);
        foreach (Needed n in r.Ingredients)
        {
            var one = new VBoxContainer();
            one.AddThemeConstantOverride("separation", 0);
            needs.AddChild(one);
            (Button small, ItemIcon mark) = ItemIcon.Tile(34);
            (mark.Glyph, mark.Item, mark.Tint, mark.Count, mark.Faint) = (ItemStyle.GlyphOf(n.Item), n.Item, new Color(ItemStyle.Colour(n.Item)), n.Count, n.Missing);
            // The whole card on an ingredient too (sprint 117, D77), what is owned of it under it.
            small.TooltipText = ItemMenu.Tooltip(n.Item, Texts) + $"\n{n.Owned}/{n.Count}";
            one.AddChild(small);
            var count = new Label { Text = $"{n.Owned}/{n.Count}", HorizontalAlignment = HorizontalAlignment.Center };
            count.AddThemeFontSizeOverride("font_size", 11);
            if (n.Missing)
                count.AddThemeColorOverride("font_color", new Color("ff7a7a"));
            one.AddChild(count);
        }
        if (_book.Workshop == r.Job.Id)
        {
            var make = new Button { Text = Texts["craft.make"], CustomMinimumSize = new Vector2(110, 44), Disabled = !r.CanMake };
            make.Pressed += () => MakePressed?.Invoke(r.Job, r.Recipe.Item, 1);
            line.AddChild(make);
            var all = new Button { Text = Texts["craft.make-all", r.Most], CustomMinimumSize = new Vector2(100, 44), Disabled = r.Most < 2 };
            all.Pressed += () => MakePressed?.Invoke(r.Job, r.Recipe.Item, r.Most);
            line.AddChild(all);
        }
        return line;
    }

    private HBoxContainer ResourceRow(ResourceLine r)
    {
        var line = new HBoxContainer { Modulate = r.Item is null ? new Color(1, 1, 1, 0.4f) : Colors.White };
        line.AddThemeConstantOverride("separation", 8);
        (Button tile, ItemIcon icon) = ItemIcon.Tile(40);
        if (r.Item is Item item)
        {
            (icon.Glyph, icon.Item, icon.Tint) = (ItemStyle.GlyphOf(item), item, new Color(ItemStyle.Colour(item)));
            tile.TooltipText = ItemMenu.Tooltip(item, Texts);
        }
        else
            (icon.Glyph, icon.Faint) = (Rpg.Client.Glyph.Leaf, true);
        line.AddChild(tile);
        string name = r.Item?.Name.In(Texts.Lang) ?? "?";
        line.AddChild(new Label { Text = $"{Texts["jobs.level", r.Level]} · {name} · {r.Where}" + (r.Item is null ? "" : " · " + Texts["jobs.owned", r.Owned]), VerticalAlignment = VerticalAlignment.Center });
        return line;
    }

    public void ShowMessage(string text) => _message.Text = text;

    /// <summary>For the self-test: the trades listed, the lines of the list shown.</summary>
    public int JobsListed => _jobButtons.Count;

    /// <summary>The trades' rows that show their icon (for the self-test: every one).</summary>
    public int IconsShown => _jobButtons.Count(j => j.Button.FindChildren("*", nameof(TextureRect), true, false).OfType<TextureRect>().Any(r => r.Texture is not null));

    /// <summary>
    /// A trade's row, 64 high (§2.3): its icon on the neutral tile, its name and level, its experience bar; a trade never
    /// practised greyed in its icon and its name, no longer the whole row at 60 %; chosen, lit as a row of the tree.
    /// </summary>
    private Button Row(JobLine line)
    {
        var b = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(0, 64), ThemeTypeVariation = OsmoseTheme.TreeRow };
        var row = new HBoxContainer { AnchorRight = 1, AnchorBottom = 1, OffsetLeft = 12, OffsetRight = -12, MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 12);
        b.AddChild(row);
        var tile = new PanelContainer { CustomMinimumSize = new Vector2(48, 48), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        tile.AddThemeStyleboxOverride("panel", OsmoseTheme.Raised("tile", 6, 6, 6));
        tile.AddChild(new TextureRect
        {
            Texture = GD.Load<Texture2D>($"res://assets/icons/relief/{line.Icon}.svg"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(36, 36),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        row.AddChild(tile);
        var text = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        text.AddThemeConstantOverride("separation", 6);
        row.AddChild(text);
        var top = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        text.AddChild(top);
        var name = new Label { Text = line.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        name.AddThemeFontOverride("font", OsmoseTheme.Bold());
        name.AddThemeFontSizeOverride("font_size", 16);
        name.AddThemeColorOverride("font_color", new Color(line.Practised ? UiStyle.TextStrong : UiStyle.TextDim));
        top.AddChild(name);
        var level = new Label { Text = Texts["jobs.level", line.Level], MouseFilter = Control.MouseFilterEnum.Ignore };
        level.AddThemeFontSizeOverride("font_size", 14);
        level.AddThemeColorOverride("font_color", new Color(UiStyle.Text));
        top.AddChild(level);
        var bottom = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        bottom.AddThemeConstantOverride("separation", 8);
        text.AddChild(bottom);
        bottom.AddChild(new ProgressBar
        {
            ThemeTypeVariation = OsmoseTheme.ProgressBarCompact,
            CustomMinimumSize = new Vector2(0, 8),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            ShowPercentage = false,
            MaxValue = 1,
            Step = 0,
            Value = line.Progress,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        var xp = new Label { Text = Texts["jobs.xp", line.XpIn, line.XpNeeded], MouseFilter = Control.MouseFilterEnum.Ignore };
        xp.AddThemeFontSizeOverride("font_size", 14);
        xp.AddThemeColorOverride("font_color", new Color(UiStyle.TextDim));
        bottom.AddChild(xp);
        return b;
    }

    public int Lines => _list.GetChildCount();
}
