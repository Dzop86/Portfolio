using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The contacts window (sprint 126, D79), F; laid out the way Dofus does it since Charles's playtest of sprint 144: two
/// tabs, the friends and those ignored; a name to add, in the tab's sense; under the columns' names (status, name, level,
/// class, zone), one card a friend, those in play first and lit, those offline greyed (<see cref="ContactsView"/>), each
/// with its icons to invite, to write to, to take out; at the bottom, whether to be told when a friend comes into play,
/// and how many contacts of the hundred. The zone, never the map. The server keeps the lists, by account. The icons are
/// game-icons.net's (CC BY 3.0, authors in <c>assets/icons/contacts/credits.json</c>).
/// </summary>
public partial class ContactsPanel : CanvasLayer
{
    private static readonly Color Online = new("7fd06b"), Away = new(1, 1, 1, 0.3f), Gold = new("e2b23a");

    // The columns' widths, the same for the names above and the cards below; the name takes what is left.
    private const int StatusWidth = 56, LevelWidth = 70, ClassWidth = 130, ZoneWidth = 170, ActionsWidth = 148;

    private LineEdit _name = null!;
    private VBoxContainer _rows = null!;
    private Button _friendsTab = null!, _ignoredTab = null!, _add = null!;
    private HBoxContainer _columns = null!;
    private Label _room = null!, _message = null!;
    private CheckBox _notify = null!;
    private Texts _texts = null!;
    private ContactList _list = new([], []);
    private bool _ignoredShown;

    public event Action<string, bool>? AddPressed;
    public event Action<Guid>? RemovePressed;
    public event Action<string>? InvitePressed;
    public event Action<string>? WhisperPressed;
    public event Action<bool>? NotifyChanged;
    public event Action? ClosePressed;

    /// <summary>The lines of the lists, friends then ignored (for the self-test).</summary>
    public List<string> Lines { get; } = [];

    public void Build(Texts texts, bool notify)
    {
        _texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -400, OffsetRight = 400, OffsetTop = -290, OffsetBottom = 290 };
        StyleBoxFlat frame = InventoryPanel.Background();
        frame.ContentMarginLeft = frame.ContentMarginRight = 14;
        frame.ContentMarginTop = frame.ContentMarginBottom = 10;
        panel.AddThemeStyleboxOverride("panel", frame);
        AddChild(panel);
        WindowDrag.Make(panel, "contacts");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);

        var head = new HBoxContainer();
        var title = new Label { Text = texts["contacts.title"], SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 20);
        head.AddChild(title);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44), Flat = true };
        close.Pressed += () => ClosePressed?.Invoke();
        head.AddChild(close);
        column.AddChild(head);

        // The tabs, underlined in the accent when chosen.
        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 4);
        column.AddChild(tabs);
        _friendsTab = Tab(tabs, false);
        _ignoredTab = Tab(tabs, true);
        column.AddChild(new HSeparator());

        // A name, and the button of the tab: add a friend, or ignore.
        var add = new HBoxContainer();
        add.AddThemeConstantOverride("separation", 8);
        _name = new LineEdit { PlaceholderText = texts["contacts.name"], SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 44), MaxLength = 20 };
        _name.TextSubmitted += _ => Add();
        add.AddChild(_name);
        _add = new Button { CustomMinimumSize = new Vector2(190, 44), ExpandIcon = true, IconAlignment = HorizontalAlignment.Left };
        _add.AddThemeConstantOverride("icon_max_width", 22);
        _add.AddThemeColorOverride("font_color", new Color("1f1f1f"));
        _add.AddThemeColorOverride("font_hover_color", new Color("1f1f1f"));
        _add.AddThemeColorOverride("font_pressed_color", new Color("1f1f1f"));
        _add.AddThemeColorOverride("icon_normal_color", new Color("1f1f1f"));
        _add.AddThemeColorOverride("icon_hover_color", new Color("1f1f1f"));
        _add.AddThemeColorOverride("icon_pressed_color", new Color("1f1f1f"));
        _add.AddThemeStyleboxOverride("normal", Card(FighterView.PlayerColour.Darkened(0.12f), 6));
        _add.AddThemeStyleboxOverride("hover", Card(FighterView.PlayerColour, 6));
        _add.AddThemeStyleboxOverride("pressed", Card(FighterView.PlayerColour.Darkened(0.25f), 6));
        _add.Pressed += Add;
        add.AddChild(_add);
        column.AddChild(add);

        // The columns' names, over the cards.
        _columns = new HBoxContainer();
        _columns.AddThemeConstantOverride("separation", 0);
        column.AddChild(_columns);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        _rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _rows.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_rows);

        column.AddChild(new HSeparator());
        var foot = new HBoxContainer();
        _notify = new CheckBox { Text = texts["contacts.notify"], ButtonPressed = notify, CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _notify.Toggled += on => NotifyChanged?.Invoke(on);
        foot.AddChild(_notify);
        _room = new Label { VerticalAlignment = VerticalAlignment.Center, Modulate = new Color(1, 1, 1, 0.7f) };
        foot.AddChild(_room);
        column.AddChild(foot);
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", new Color("ffb4a2"));
        column.AddChild(_message);
        Show(new ContactList([], []));
    }

    private Button Tab(HBoxContainer into, bool ignored)
    {
        var b = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(150, 44), FocusMode = Control.FocusModeEnum.None };
        var idle = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), ContentMarginLeft = 14, ContentMarginRight = 14 };
        var chosen = new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.05f), BorderColor = FighterView.PlayerColour, BorderWidthBottom = 3, ContentMarginLeft = 14, ContentMarginRight = 14 };
        b.AddThemeStyleboxOverride("normal", idle);
        b.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.08f), ContentMarginLeft = 14, ContentMarginRight = 14 });
        b.AddThemeStyleboxOverride("pressed", chosen);
        b.AddThemeStyleboxOverride("hover_pressed", chosen);
        b.AddThemeColorOverride("font_pressed_color", FighterView.PlayerColour);
        b.AddThemeColorOverride("font_hover_pressed_color", FighterView.PlayerColour);
        b.Pressed += () => ShowTab(ignored);
        into.AddChild(b);
        return b;
    }

    /// <summary>The friends' tab, or those ignored.</summary>
    public void ShowTab(bool ignored)
    {
        _ignoredShown = ignored;
        Show(_list);
    }

    private void Add()
    {
        string name = _name.Text.Trim();
        if (name.Length > 0)
            AddPressed?.Invoke(name, _ignoredShown);
    }

    /// <summary>The contacts as the server says; the name field emptied.</summary>
    public void Show(ContactList list)
    {
        ArgumentNullException.ThrowIfNull(list);
        _list = list;
        _name.Text = "";
        Lines.Clear();
        foreach (Node n in _rows.GetChildren().Concat(_columns.GetChildren()))
        {
            n.GetParent().RemoveChild(n);
            n.QueueFree();
        }
        _friendsTab.Text = _texts["contacts.friends", list.Friends.Count];
        _ignoredTab.Text = _texts["contacts.ignored", list.Ignored.Count];
        _friendsTab.SetPressedNoSignal(!_ignoredShown);
        _ignoredTab.SetPressedNoSignal(_ignoredShown);
        _add.Text = _texts[_ignoredShown ? "contacts.ignore" : "contacts.add"];
        _add.Icon = GD.Load<Texture2D>($"res://assets/icons/contacts/{(_ignoredShown ? "ignore" : "add")}.svg");
        (int count, int most) = ContactsView.Room(list);
        _room.Text = _texts["contacts.room", count, most];

        // Both lists' lines, friends then ignored, whichever tab is shown.
        IReadOnlyList<Contact> inPlay = ContactsView.InPlay(list), offline = ContactsView.Offline(list), ignored = ContactsView.Ignored(list);
        foreach (Contact c in inPlay.Concat(offline))
            Lines.Add(c.Zone is string zone ? _texts["contacts.online", c.Name, c.Level, _texts.ZoneName(zone, GameData.Embedded)] : _texts["contacts.offline", c.Name, c.Level]);
        Lines.AddRange(ignored.Select(c => c.Name));

        if (_ignoredShown)
        {
            Heading(StatusWidth, "");
            Heading(0, _texts["contacts.col.name"]);
            Heading(ClassWidth, _texts["contacts.col.class"]);
            Heading(ActionsWidth, "");
            foreach (Contact c in ignored)
                Row(c, false, true);
            if (ignored.Count == 0)
                Empty(_texts["contacts.none"]);
            return;
        }
        Heading(StatusWidth, _texts["contacts.col.status"]);
        Heading(0, _texts["contacts.col.name"]);
        Heading(LevelWidth, _texts["contacts.col.level"]);
        Heading(ClassWidth, _texts["contacts.col.class"]);
        Heading(ZoneWidth, _texts["contacts.col.zone"]);
        Heading(ActionsWidth, "");
        if (list.Friends.Count == 0)
        {
            Empty(_texts["contacts.none"]);
            return;
        }
        Section(_texts["contacts.in-play", inPlay.Count]);
        foreach (Contact c in inPlay)
            Row(c, true, false);
        Section(_texts["contacts.away", offline.Count]);
        foreach (Contact c in offline)
            Row(c, false, false);
    }

    private void Heading(int width, string text)
    {
        var label = new Label { Text = text, CustomMinimumSize = new Vector2(width, 0), SizeFlagsHorizontal = width == 0 ? Control.SizeFlags.ExpandFill : Control.SizeFlags.Fill, Modulate = new Color(1, 1, 1, 0.6f), ClipText = true };
        label.AddThemeFontSizeOverride("font_size", 13);
        if (width == StatusWidth)
            label.HorizontalAlignment = HorizontalAlignment.Center;
        _columns.AddChild(label);
    }

    private void Section(string text)
    {
        var label = new Label { Text = text, Modulate = new Color(1, 1, 1, 0.7f) };
        label.AddThemeFontSizeOverride("font_size", 13);
        _rows.AddChild(label);
    }

    private void Empty(string text)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, CustomMinimumSize = new Vector2(0, 80), VerticalAlignment = VerticalAlignment.Center, Modulate = new Color(1, 1, 1, 0.55f) };
        _rows.AddChild(label);
    }

    // One contact's card: its status, name, level, class and zone; its icons on the right.
    private void Row(Contact c, bool inPlay, bool ignored)
    {
        var card = new PanelContainer { TooltipText = c.Zone is string z ? _texts["contacts.online", c.Name, c.Level, _texts.ZoneName(z, GameData.Embedded)] : _texts["contacts.offline", c.Name, c.Level] };
        card.AddThemeStyleboxOverride("panel", Card(inPlay ? new Color(0.17f, 0.18f, 0.22f) : new Color(0.13f, 0.13f, 0.15f), 6));
        _rows.AddChild(card);
        var line = new HBoxContainer { CustomMinimumSize = new Vector2(0, 48) };
        line.AddThemeConstantOverride("separation", 0);
        card.AddChild(line);
        var status = new CenterContainer { CustomMinimumSize = new Vector2(StatusWidth, 0) };
        var dot = new Panel { CustomMinimumSize = new Vector2(10, 10), MouseFilter = Control.MouseFilterEnum.Ignore };
        dot.AddThemeStyleboxOverride("panel", Card(ignored ? new Color("e06c5a") : inPlay ? Online : Away, 5));
        status.AddChild(dot);
        line.AddChild(status);
        var name = new Label { Text = c.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center, ClipText = true };
        name.AddThemeFontSizeOverride("font_size", 17);
        name.AddThemeColorOverride("font_color", inPlay ? FighterView.PlayerColour : new Color(1, 1, 1, ignored ? 0.85f : 0.5f));
        line.AddChild(name);
        if (!ignored)
        {
            var level = new CenterContainer { CustomMinimumSize = new Vector2(LevelWidth, 0) };
            var badge = new Label { Text = c.Level.ToString(System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment = HorizontalAlignment.Center, CustomMinimumSize = new Vector2(38, 0), Modulate = inPlay ? Colors.White : new Color(1, 1, 1, 0.5f) };
            badge.AddThemeStyleboxOverride("normal", Card(Gold, 4));
            badge.AddThemeColorOverride("font_color", new Color("2a1d06"));
            level.AddChild(badge);
            line.AddChild(level);
        }
        line.AddChild(Cell(c.Class is string id ? GameData.Embedded.Class(id)?.Name.In(_texts.Lang) ?? id : "—", ClassWidth, inPlay || ignored));
        if (!ignored)
            line.AddChild(Cell(c.Zone is string zone ? _texts.ZoneName(zone, GameData.Embedded) : _texts["contacts.offline-zone"], ZoneWidth, inPlay));
        var actions = new HBoxContainer { CustomMinimumSize = new Vector2(ActionsWidth, 0), Alignment = BoxContainer.AlignmentMode.End };
        actions.AddThemeConstantOverride("separation", 2);
        line.AddChild(actions);
        if (inPlay)
        {
            Icon(actions, "invite", _texts["contacts.invite"], () => InvitePressed?.Invoke(c.Name));
            Icon(actions, "whisper", _texts["contacts.whisper"], () => WhisperPressed?.Invoke(c.Name));
        }
        Icon(actions, "remove", _texts["contacts.remove", c.Name], () => RemovePressed?.Invoke(c.Id));
    }

    private static Label Cell(string text, int width, bool lit)
    {
        var label = new Label { Text = text, CustomMinimumSize = new Vector2(width, 0), VerticalAlignment = VerticalAlignment.Center, ClipText = true, Modulate = lit ? new Color(1, 1, 1, 0.9f) : new Color(1, 1, 1, 0.45f) };
        return label;
    }

    // A discreet icon, lit under the pointer; what it does in its tooltip.
    private static void Icon(HBoxContainer into, string icon, string tip, Action pressed)
    {
        var b = new Button
        {
            Icon = GD.Load<Texture2D>($"res://assets/icons/contacts/{icon}.svg"),
            ExpandIcon = true,
            IconAlignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(44, 44),
            TooltipText = tip,
            FocusMode = Control.FocusModeEnum.None,
        };
        b.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        b.AddThemeStyleboxOverride("hover", Card(new Color(1, 1, 1, 0.1f), 6));
        b.AddThemeStyleboxOverride("pressed", Card(new Color(1, 1, 1, 0.18f), 6));
        b.AddThemeConstantOverride("icon_max_width", 22);
        b.AddThemeColorOverride("icon_normal_color", new Color(1, 1, 1, 0.6f));
        b.AddThemeColorOverride("icon_hover_color", icon == "remove" ? new Color("ff8a75") : FighterView.PlayerColour);
        b.Pressed += pressed;
        into.AddChild(b);
    }

    private static StyleBoxFlat Card(Color bg, int corner) => new()
    {
        BgColor = bg,
        CornerRadiusTopLeft = corner,
        CornerRadiusTopRight = corner,
        CornerRadiusBottomLeft = corner,
        CornerRadiusBottomRight = corner,
        ContentMarginLeft = 4,
        ContentMarginRight = 4,
        ContentMarginTop = 1,
        ContentMarginBottom = 1,
    };

    /// <summary>What went wrong with the server, or nothing.</summary>
    public void Tell(string text) => _message.Text = text;

    public string Message => _message.Text;
}
