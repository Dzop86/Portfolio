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
    private static readonly Color Muted = new(1, 1, 1, 0.6f), Gold = new("ffd85a");

    private Texts _texts = null!;
    private JournalView _view = null!;
    private IReadOnlyList<JournalZone> _journal = [];
    private IReadOnlySet<string> _hidden = new HashSet<string>();
    private string? _followed;
    private VBoxContainer _list = null!, _card = null!;

    public event Action? ClosePressed;

    /// <summary>"Follow" pressed on a quest under way (sprint 106): its id.</summary>
    public event Action<string>? FollowPressed;

    /// <summary>"Hide" or "Show" pressed on a quest (sprint 116): its id, and whether it is now hidden.</summary>
    public event Action<string, bool>? HidePressed;

    /// <summary>The tabs' buttons, the search, the kinds' buttons, the levels (for the self-test).</summary>
    public Dictionary<JournalTab, Button> TabButtons { get; } = [];
    public LineEdit SearchBox { get; private set; } = null!;
    public Dictionary<QuestKind, Button> KindButtons { get; } = [];
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

    public void Build(QuestBook book, Texts texts, JournalView view, IReadOnlySet<string> hidden, string? followed = null)
    {
        _texts = texts;
        _view = view;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -480, OffsetRight = 480, OffsetTop = -300, OffsetBottom = 300 };
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
            var b = new Button { ToggleMode = true, ButtonPressed = _view.Kinds.Contains(kind), CustomMinimumSize = new Vector2(44, 44), TooltipText = _texts["questkind." + kind] };
            b.AddChild(new QuestKindIcon { Kind = kind, MouseFilter = Control.MouseFilterEnum.Ignore, Position = new Vector2(10, 10), Size = new Vector2(24, 24) });
            b.Toggled += on =>
            {
                b.Modulate = on ? Colors.White : new Color(1, 1, 1, 0.35f);
                Keep(kind, on);
            };
            b.Modulate = b.ButtonPressed ? Colors.White : new Color(1, 1, 1, 0.35f);
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
            var head = new Button { Flat = true, CustomMinimumSize = new Vector2(0, 40), Alignment = HorizontalAlignment.Left, Text = $"      {section.Name} ({section.Quests.Count})" };
            head.AddThemeColorOverride("font_color", FighterView.PlayerColour);
            head.AddThemeFontSizeOverride("font_size", 17);
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
            _list.AddChild(new Label { Text = _texts[filtered ? "journal.nomatch" : "journal.empty"], Modulate = Muted, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(360, 0) });
        }
        Card(card);
    }

    private Button Row(JournalQuest q, bool chosen)
    {
        var row = new Button { Flat = !chosen, CustomMinimumSize = new Vector2(0, 44), ClipText = true };
        var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        line.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        line.OffsetLeft = 22;
        line.OffsetRight = -8;
        line.AddThemeConstantOverride("separation", 8);
        line.AddChild(new QuestKindIcon { Kind = q.Kind, MouseFilter = Control.MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(22, 22), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, TooltipText = _texts["questkind." + q.Kind] });
        var name = new Label { Text = q.Name, MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
        if (q.Ready)
            name.AddThemeColorOverride("font_color", Gold);
        line.AddChild(name);
        line.AddChild(new Label { Text = _texts["journal.level", q.Level], Modulate = Muted, MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
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
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 10);
        top.AddChild(new QuestKindIcon { Kind = q.Kind, CustomMinimumSize = new Vector2(36, 36), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        var name = new Label { Text = q.Name, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        name.AddThemeFontSizeOverride("font_size", 22);
        top.AddChild(name);
        _card.AddChild(top);
        var kind = new Label { Text = $"{_texts["questkind." + q.Kind]} · {_texts["journal.level", q.Level]} · {q.Zone}" };
        kind.AddThemeColorOverride("font_color", QuestKindIcon.Colour(q.Kind));
        _card.AddChild(kind);
        _card.AddChild(new Label { Text = q.Done ? _texts["journal.finished"] + " · " + _texts["journal.giver", q.Giver] : q.Ready ? _texts["journal.ready", q.Giver] : _texts["journal.giver", q.Giver], Modulate = q.Ready ? Gold : Muted });
        // Where to go next (sprint 105, D75).
        if (!q.Done && q.Where is string where)
            _card.AddChild(new Label { Text = _texts["journal.where", where], Modulate = Gold });
        _card.AddChild(new Label { Text = q.Text, AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1, 1, 1, 0.85f), CustomMinimumSize = new Vector2(480, 0) });
        Heading(_texts["journal.objectives"]);
        foreach ((string line, bool reached) in q.Objectives)
        {
            var l = new Label { Text = (reached ? "✓ " : "– ") + line, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(480, 0) };
            if (reached)
                l.AddThemeColorOverride("font_color", FighterView.PlayerColour);
            _card.AddChild(l);
            ObjectiveLines.Add(line);
        }
        Heading(_texts["journal.rewards"]);
        foreach (string reward in q.Rewards ?? [])
            _card.AddChild(new Label { Text = "– " + reward });
        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        bool hidden = _hidden.Contains(q.Id);
        // Followed on the left of the screen, with an arrow (sprint 106): a quest under way, not hidden.
        if (!q.Done && !hidden)
        {
            FollowButton = new Button { Text = _texts[q.Id == _followed ? "journal.followed" : "journal.follow"], Disabled = q.Id == _followed, CustomMinimumSize = new Vector2(140, 44) };
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
        var l = new Label { Text = text };
        l.AddThemeFontSizeOverride("font_size", 17);
        l.AddThemeColorOverride("font_color", FighterView.PlayerColour);
        _card.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
        _card.AddChild(l);
    }
}
