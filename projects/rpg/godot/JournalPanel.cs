using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The quest journal (sprint 88, key Q), as in the games of the kind since sprint 116 (D77): on the left, the tabs
/// under way, done and hidden, a search, the levels and the kinds kept, the quests zone by zone (a zone folds), each
/// with its kind's icon and its level; on the right, the card of the quest chosen: its words, its objectives, its
/// rewards, and Follow and Hide. What it shows is <see cref="QuestBook.Journal"/> through a <see cref="JournalView"/>.
/// </summary>
public partial class JournalPanel : CanvasLayer
{
    private Texts _texts = null!;
    private JournalView _view = null!;
    private IReadOnlyList<JournalZone> _journal = [];
    private IReadOnlySet<string> _hidden = new HashSet<string>();
    private string? _followed;
    private VBoxContainer _list = null!, _card = null!;
    private PanelContainer _window = null!;

    public event Action? ClosePressed;

    /// <summary>"Follow" pressed on a quest under way (sprint 106): its id.</summary>
    public event Action<string>? FollowPressed;

    /// <summary>"Hide" or "Show" pressed on a quest (sprint 116): its id, and whether it is now hidden.</summary>
    public event Action<string, bool>? HidePressed;

    /// <summary>The tabs' buttons, the search, the kinds' buttons, the levels (for the self-test).</summary>
    public Dictionary<JournalTab, Button> TabButtons { get; } = [];
    public LineEdit SearchBox { get; private set; } = null!;
    public Dictionary<QuestKind, Button> KindButtons { get; } = [];

    /// <summary>The kinds' icons on their tiles (for the self-test): greyed when the kind is left out (sprint 165).</summary>
    public Dictionary<QuestKind, QuestKindIcon> KindIcons { get; } = [];
    public SpinBox MinLevel { get; private set; } = null!;
    public SpinBox MaxLevel { get; private set; } = null!;

    /// <summary>The zones listed, their headers (for the self-test).</summary>
    public Dictionary<string, Button> SectionButtons { get; } = [];

    /// <summary>The quests listed, by their rows (for the self-test).</summary>
    public Dictionary<string, Button> Rows { get; } = [];

    /// <summary>The quests' names listed, in order (for the self-test).</summary>
    public List<string> ListedNames { get; } = [];

    /// <summary>The card's quest, its objective lines, its Follow and Hide buttons (for the self-test).</summary>
    public JournalQuest? CardQuest { get; private set; }
    public List<string> ObjectiveLines { get; } = [];
    public Button? FollowButton { get; private set; }
    public Button? HideButton { get; private set; }

    /// <summary>The card's rewards, a tile each (for the self-test, sprint 167).</summary>
    public List<Control> RewardTiles { get; } = [];

    /// <summary>The window's width (for the self-test): its 960, whatever the card holds.</summary>
    public float Width => _window.GetCombinedMinimumSize().X;

    public void Build(QuestBook book, Texts texts, JournalView view, IReadOnlySet<string> hidden, string? followed = null)
    {
        _texts = texts;
        _view = view;
        Layer = 15;
        var panel = _window = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -480, OffsetRight = 480, OffsetTop = -300, OffsetBottom = 300 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "journal");
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        var title = new Label { Text = texts["journal.title"], HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 24);
        column.AddChild(title);
        var halves = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        halves.AddThemeConstantOverride("separation", 16);
        column.AddChild(halves);
        halves.AddChild(Filters());
        halves.AddChild(new VSeparator());
        var cardScroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        halves.AddChild(cardScroll);
        _card = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _card.AddThemeConstantOverride("separation", 6);
        cardScroll.AddChild(_card);
        var close = new Button { Text = texts["craft.close"], CustomMinimumSize = new Vector2(160, 44), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        close.Pressed += () => ClosePressed?.Invoke();
        column.AddChild(close);
        Show(book, hidden, followed);
    }

    /// <summary>The book, the hidden quests and the quest followed as they are now: the tabs' counts, the list and the card again.</summary>
    public void Show(QuestBook book, IReadOnlySet<string> hidden, string? followed)
    {
        ArgumentNullException.ThrowIfNull(book);
        _journal = book.Journal(_texts);
        _hidden = hidden ?? throw new ArgumentNullException(nameof(hidden));
        _followed = followed;
        Refresh();
    }

    /// <summary>Another tab, as its button does.</summary>
    public void ChooseTab(JournalTab tab)
    {
        _view.Tab = tab;
        Refresh();
    }

    /// <summary>The search's words, as typing them does.</summary>
    public void Search(string words)
    {
        _view.Search = words;
        Refresh();
    }

    /// <summary>A kind kept or left out, as its button does.</summary>
    public void Keep(QuestKind kind, bool kept)
    {
        if (kept)
            _view.Kinds.Add(kind);
        else
            _view.Kinds.Remove(kind);
        Refresh();
    }

    /// <summary>A zone folded or opened, as its header does.</summary>
    public void Fold(string zone)
    {
        if (!_view.Folded.Remove(zone))
            _view.Folded.Add(zone);
        Refresh();
    }

    /// <summary>The quest whose card shows, as its row does.</summary>
    public void Choose(string quest)
    {
        _view.Selected = quest;
        Refresh();
    }

    private VBoxContainer Filters()
    {
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(380, 0) };
        left.AddThemeConstantOverride("separation", 6);
        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 4);
        var group = new ButtonGroup();
        foreach (JournalTab tab in Enum.GetValues<JournalTab>())
        {
            var b = new Button { ToggleMode = true, ButtonGroup = group, ButtonPressed = tab == _view.Tab, CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            b.Pressed += () => ChooseTab(tab);
            tabs.AddChild(b);
            TabButtons[tab] = b;
        }
        left.AddChild(tabs);
        SearchBox = new LineEdit { Text = _view.Search, PlaceholderText = _texts["journal.search"], ClearButtonEnabled = true, CustomMinimumSize = new Vector2(0, 44) };
        SearchBox.TextChanged += Search;
        left.AddChild(SearchBox);
        var kinds = new HBoxContainer();
        kinds.AddThemeConstantOverride("separation", 4);
        foreach (QuestKind kind in Enum.GetValues<QuestKind>())
        {
            // A tile of the menus' (sprint 165, s165 §3.3): lit while the kind is kept, its icon greyed once left out.
            var b = new Button { ToggleMode = true, ButtonPressed = _view.Kinds.Contains(kind), CustomMinimumSize = new Vector2(44, 44), TooltipText = _texts["questkind." + kind], ThemeTypeVariation = OsmoseTheme.MenuTile, FocusMode = Control.FocusModeEnum.None };
            var icon = new QuestKindIcon { Kind = kind, Locked = !b.ButtonPressed, MouseFilter = Control.MouseFilterEnum.Ignore, Position = new Vector2(8, 8), Size = new Vector2(28, 28) };
            b.AddChild(icon);
            KindIcons[kind] = icon;
            b.Toggled += on =>
            {
                icon.Locked = !on;
                Keep(kind, on);
            };
            kinds.AddChild(b);
            KindButtons[kind] = b;
        }
        left.AddChild(kinds);
        var levels = new HBoxContainer();
        levels.AddThemeConstantOverride("separation", 6);
        levels.AddChild(new Label { Text = _texts["journal.levels"], SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        MinLevel = Level(_view.MinLevel, v => _view.MinLevel = v);
        levels.AddChild(MinLevel);
        levels.AddChild(new Label { Text = _texts["journal.to"], SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        MaxLevel = Level(_view.MaxLevel, v => _view.MaxLevel = v);
        levels.AddChild(MaxLevel);
        left.AddChild(levels);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        left.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(_list);
        return left;
    }

    private SpinBox Level(int value, Action<int> set)
    {
        var box = new SpinBox { MinValue = 1, MaxValue = Hero.MaxLevel, Value = value, CustomMinimumSize = new Vector2(96, 44) };
        box.ValueChanged += v =>
        {
            set((int)v);
            Refresh();
        };
        return box;
    }

    private void Refresh()
    {
        IReadOnlyDictionary<JournalTab, int> counts = _view.Counts(_journal, _hidden);
        string[] names = [_texts["journal.active"], _texts["journal.done"], _texts["journal.hidden"]];
        foreach ((JournalTab tab, Button b) in TabButtons)
        {
            b.Text = _texts["journal.tab", names[(int)tab], counts[tab]];
            b.SetPressedNoSignal(tab == _view.Tab);
        }
        foreach (Node child in _list.GetChildren())
            child.QueueFree();
        SectionButtons.Clear();
        Rows.Clear();
        ListedNames.Clear();
        IReadOnlyList<JournalSection> page = _view.Page(_journal, _hidden);
        JournalQuest? card = _view.Card(page);
        foreach (JournalSection section in page)
        {
            bool open = !_view.Folded.Contains(section.Name);
            var head = new Button { Flat = true, CustomMinimumSize = new Vector2(0, 40), Alignment = HorizontalAlignment.Left, Text = $"      {section.Name.ToUpperInvariant()} ({section.Quests.Count})" };
            head.AddThemeColorOverride("font_color", new Color(UiStyle.ChocolateLight));
            head.AddThemeColorOverride("font_hover_color", new Color(UiStyle.TextStrong));
            head.AddThemeFontOverride("font", OsmoseTheme.Bold(1));
            head.AddThemeFontSizeOverride("font_size", 14);
            head.AddChild(new FoldArrow { Open = open, MouseFilter = Control.MouseFilterEnum.Ignore, Position = new Vector2(4, 12), Size = new Vector2(16, 16) });
            string zone = section.Name;
            head.Pressed += () => Fold(zone);
            _list.AddChild(head);
            SectionButtons[zone] = head;
            if (!open)
                continue;
            foreach (JournalQuest q in section.Quests)
            {
                _list.AddChild(Row(q, q.Id == card?.Id));
                ListedNames.Add(q.Name);
            }
        }
        if (page.Count == 0)
        {
            bool filtered = _view.Counts(_journal, _hidden)[_view.Tab] > 0;
            Label none = Text(_texts[filtered ? "journal.nomatch" : "journal.empty"], UiStyle.TextDim);
            none.CustomMinimumSize = new Vector2(360, 0);
            _list.AddChild(none);
        }
        Card(card);
    }

    private Button Row(JournalQuest q, bool chosen)
    {
        // The list's row of the theme (s157 §4), chosen like the achievements' tree.
        var row = new Button { CustomMinimumSize = new Vector2(0, 44), ClipText = true, ThemeTypeVariation = OsmoseTheme.TreeRow, ToggleMode = true, ButtonPressed = chosen, FocusMode = Control.FocusModeEnum.None };
        var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        line.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        line.OffsetLeft = 22;
        line.OffsetRight = -8;
        line.AddThemeConstantOverride("separation", 8);
        line.AddChild(new QuestKindIcon { Kind = q.Kind, MouseFilter = Control.MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(24, 24), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, TooltipText = _texts["questkind." + q.Kind] });
        // Its name in the text's grey, brighter when it is to hand back, a dot of the hints' yellow after it then.
        var name = new Label { Text = q.Name, MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
        name.AddThemeFontSizeOverride("font_size", 14);
        name.AddThemeColorOverride("font_color", new Color(q.Ready || chosen ? UiStyle.TextStrong : UiStyle.Text));
        line.AddChild(name);
        if (q.Ready)
        {
            var dot = new Panel { Name = "Ready", MouseFilter = Control.MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(8, 8), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, TooltipText = _texts["journal.ready", q.Giver] };
            dot.AddThemeStyleboxOverride("panel", OsmoseTheme.Flat(UiStyle.Cost, corner: 4));
            line.AddChild(dot);
        }
        var level = new Label { Text = _texts["journal.level", q.Level], MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        level.AddThemeFontSizeOverride("font_size", 14);
        level.AddThemeColorOverride("font_color", new Color(UiStyle.TextDim));
        line.AddChild(level);
        row.AddChild(line);
        row.Pressed += () => Choose(q.Id);
        Rows[q.Id] = row;
        return row;
    }

    private void Card(JournalQuest? q)
    {
        foreach (Node child in _card.GetChildren())
            child.QueueFree();
        ObjectiveLines.Clear();
        CardQuest = q;
        FollowButton = HideButton = null;
        if (q is null)
            return;
        // Its head (sprint 167, s165 §3.4): the kind's icon, the name, the kind, level and zone in the kind's colour, the giver.
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 12);
        top.AddChild(new QuestKindIcon { Kind = q.Kind, CustomMinimumSize = new Vector2(40, 40), SizeFlagsVertical = Control.SizeFlags.ShrinkBegin });
        var head = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        head.AddThemeConstantOverride("separation", 2);
        top.AddChild(head);
        var name = new Label { Text = q.Name, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        name.AddThemeFontOverride("font", OsmoseTheme.Bold());
        name.AddThemeFontSizeOverride("font_size", 22);
        name.AddThemeColorOverride("font_color", Colors.White);
        head.AddChild(name);
        head.AddChild(Text($"{_texts["questkind." + q.Kind]} · {_texts["journal.level", q.Level]} · {q.Zone}", QuestKindStyle.Colour(q.Kind)));
        head.AddChild(Text(q.Done ? _texts["journal.finished"] + " · " + _texts["journal.giver", q.Giver] : _texts["journal.giver", q.Giver], UiStyle.TextDim));
        _card.AddChild(top);
        // To hand back, and where to go next (sprint 105, D75), in the hints' yellow.
        if (!q.Done && q.Ready)
            _card.AddChild(Text(_texts["journal.ready", q.Giver], UiStyle.Cost));
        if (!q.Done && q.Where is string where)
            _card.AddChild(Text(_texts["journal.where", where], UiStyle.Cost));
        _card.AddChild(Text(q.Text, UiStyle.Text));
        Heading(_texts["journal.objectives"]);
        foreach ((string line, bool reached) in q.Objectives)
        {
            // Reached: ticked in relief, in the dim grey; still to do: a hollow ring, in the strong text.
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(new TextureRect { Texture = GD.Load<Texture2D>(reached ? "res://assets/icons/relief/check.svg" : "res://assets/ui/todo.svg"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, CustomMinimumSize = new Vector2(16, 16), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
            row.AddChild(Text(line, reached ? UiStyle.TextDim : UiStyle.TextStrong));
            _card.AddChild(row);
            ObjectiveLines.Add(line);
        }
        Heading(_texts["journal.rewards"]);
        // Each reward a tile of 48, its amount under it: the experience's and the Éclats' icons in relief, an item's tile.
        var rewards = new HBoxContainer();
        rewards.AddThemeConstantOverride("separation", 12);
        RewardTiles.Clear();
        foreach (JournalReward reward in q.Rewards ?? [])
        {
            var one = new VBoxContainer();
            one.AddThemeConstantOverride("separation", 2);
            Control tile;
            if (reward.Item is string id && GameData.Embedded.Items.TryGetValue(id, out Item? item))
            {
                (Button button, ItemIcon icon) = ItemIcon.Tile(48);
                icon.Item = item;
                button.TooltipText = ItemMenu.Tooltip(item, _texts);
                tile = button;
            }
            else
            {
                var frame = new Panel { CustomMinimumSize = new Vector2(48, 48), TooltipText = reward.Text };
                frame.AddThemeStyleboxOverride("panel", OsmoseTheme.Raised("tile", 6, 6, 6));
                var picture = new TextureRect { Texture = GD.Load<Texture2D>($"res://assets/icons/relief/reward-{(reward.Kind == RewardKind.Xp ? "xp" : "shards")}.svg"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore };
                picture.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                (picture.OffsetLeft, picture.OffsetTop, picture.OffsetRight, picture.OffsetBottom) = (6, 6, -6, -6);
                frame.AddChild(picture);
                tile = frame;
            }
            one.AddChild(tile);
            var amount = Text((reward.Kind == RewardKind.Item ? "×" : "") + reward.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture), UiStyle.TextStrong);
            (amount.HorizontalAlignment, amount.CustomMinimumSize, amount.AutowrapMode) = (HorizontalAlignment.Center, new Vector2(48, 0), TextServer.AutowrapMode.Off);
            amount.AddThemeFontOverride("font", OsmoseTheme.Bold());
            one.AddChild(amount);
            rewards.AddChild(one);
            RewardTiles.Add(tile);
        }
        _card.AddChild(rewards);
        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        bool hidden = _hidden.Contains(q.Id);
        // Followed on the left of the screen, with an arrow (sprint 106): a quest under way, not hidden.
        if (!q.Done && !hidden)
        {
            // The card's focal point (s165 §3.4): the theme's primary button; "Followed", the same switched off.
            FollowButton = new Button { Text = _texts[q.Id == _followed ? "journal.followed" : "journal.follow"], Disabled = q.Id == _followed, CustomMinimumSize = new Vector2(140, 44), ThemeTypeVariation = OsmoseTheme.Primary };
            FollowButton.Pressed += () => FollowPressed?.Invoke(q.Id);
            buttons.AddChild(FollowButton);
        }
        HideButton = new Button { Text = _texts[hidden ? "journal.show" : "journal.hide"], CustomMinimumSize = new Vector2(140, 44) };
        HideButton.Pressed += () => HidePressed?.Invoke(q.Id, !hidden);
        buttons.AddChild(HideButton);
        _card.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
        _card.AddChild(buttons);
    }

    private void Heading(string text)
    {
        // Bold capitals of light chocolate, as the achievements' sections.
        var l = new Label { Text = text.ToUpperInvariant() };
        l.AddThemeFontOverride("font", OsmoseTheme.Bold(1));
        l.AddThemeFontSizeOverride("font_size", 14);
        l.AddThemeColorOverride("font_color", new Color(UiStyle.ChocolateLight));
        _card.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });
        _card.AddChild(l);
    }

    /// <summary>A line of the card: 14 px, wrapped, in one of the interface's greys or the kind's colour.</summary>
    private static Label Text(string text, string colour)
    {
        var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(440, 0) };
        l.AddThemeFontSizeOverride("font_size", 14);
        l.AddThemeColorOverride("font_color", new Color(colour));
        return l;
    }
}
