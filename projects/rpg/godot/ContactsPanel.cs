using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The contacts window (sprint 126, D79), F: a name to add as a friend or to ignore; the friends, in play with their level
/// and zone (never the map) or offline, each to invite, to write to, or to take out; those ignored, to take out; whether
/// to be told when a friend comes into play. The server keeps the lists, by account.
/// </summary>
public partial class ContactsPanel : CanvasLayer
{
    private LineEdit _name = null!;
    private VBoxContainer _friends = null!, _ignored = null!;
    private Label _friendsTitle = null!, _ignoredTitle = null!, _message = null!;
    private CheckBox _notify = null!;
    private Texts _texts = null!;

    public event Action<string, bool>? AddPressed;
    public event Action<Guid>? RemovePressed;
    public event Action<string>? InvitePressed;
    public event Action<string>? WhisperPressed;
    public event Action<bool>? NotifyChanged;
    public event Action? ClosePressed;

    /// <summary>The lines shown, friends then ignored (for the self-test).</summary>
    public List<string> Lines { get; } = [];

    public void Build(Texts texts, bool notify)
    {
        _texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -300, OffsetRight = 300, OffsetTop = -300, OffsetBottom = 300 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "contacts");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        panel.AddChild(column);
        var head = new HBoxContainer();
        var title = new Label { Text = texts["contacts.title"], SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 20);
        head.AddChild(title);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44) };
        close.Pressed += () => ClosePressed?.Invoke();
        head.AddChild(close);
        column.AddChild(head);
        var add = new HBoxContainer();
        _name = new LineEdit { PlaceholderText = texts["contacts.name"], SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 44), MaxLength = 20 };
        add.AddChild(_name);
        var friend = new Button { Text = texts["contacts.add"], CustomMinimumSize = new Vector2(0, 44) };
        friend.Pressed += () => Add(false);
        add.AddChild(friend);
        var ignore = new Button { Text = texts["contacts.ignore"], CustomMinimumSize = new Vector2(0, 44) };
        ignore.Pressed += () => Add(true);
        add.AddChild(ignore);
        column.AddChild(add);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        var lists = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        lists.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(lists);
        _friendsTitle = new Label();
        _friendsTitle.AddThemeColorOverride("font_color", FighterView.PlayerColour);
        lists.AddChild(_friendsTitle);
        _friends = new VBoxContainer();
        lists.AddChild(_friends);
        _ignoredTitle = new Label();
        _ignoredTitle.AddThemeColorOverride("font_color", FighterView.PlayerColour);
        lists.AddChild(_ignoredTitle);
        _ignored = new VBoxContainer();
        lists.AddChild(_ignored);
        _notify = new CheckBox { Text = texts["contacts.notify"], ButtonPressed = notify, CustomMinimumSize = new Vector2(0, 44) };
        _notify.Toggled += on => NotifyChanged?.Invoke(on);
        column.AddChild(_notify);
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", new Color("ffb4a2"));
        column.AddChild(_message);
        Show(new ContactList([], []));
    }

    private void Add(bool ignore)
    {
        string name = _name.Text.Trim();
        if (name.Length > 0)
            AddPressed?.Invoke(name, ignore);
    }

    /// <summary>The contacts as the server says; the name field emptied.</summary>
    public void Show(ContactList list)
    {
        ArgumentNullException.ThrowIfNull(list);
        _name.Text = "";
        Lines.Clear();
        foreach (Node n in _friends.GetChildren().Concat(_ignored.GetChildren()))
            n.QueueFree();
        _friendsTitle.Text = _texts["contacts.friends", list.Friends.Count];
        _ignoredTitle.Text = _texts["contacts.ignored", list.Ignored.Count];
        foreach (Contact c in list.Friends)
        {
            string line = c.Zone is string zone ? _texts["contacts.online", c.Name, c.Level, _texts.ZoneName(zone, GameData.Embedded)] : _texts["contacts.offline", c.Name, c.Level];
            Row(_friends, line, c, c.Zone is not null);
        }
        foreach (Contact c in list.Ignored)
            Row(_ignored, c.Name, c, false);
        if (list.Friends.Count == 0)
            _friends.AddChild(new Label { Text = _texts["contacts.none"], Modulate = new Color(1, 1, 1, 0.6f) });
    }

    private void Row(VBoxContainer into, string text, Contact c, bool inPlay)
    {
        var row = new HBoxContainer();
        var label = new Label { Text = text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true, Modulate = inPlay || into == _ignored ? Colors.White : new Color(1, 1, 1, 0.55f) };
        row.AddChild(label);
        if (inPlay)
        {
            var invite = new Button { Text = _texts["contacts.invite"], CustomMinimumSize = new Vector2(0, 44) };
            invite.Pressed += () => InvitePressed?.Invoke(c.Name);
            row.AddChild(invite);
            var write = new Button { Text = _texts["contacts.whisper"], CustomMinimumSize = new Vector2(0, 44) };
            write.Pressed += () => WhisperPressed?.Invoke(c.Name);
            row.AddChild(write);
        }
        var remove = new Button { Text = "×", TooltipText = _texts["contacts.remove", c.Name], CustomMinimumSize = new Vector2(44, 44) };
        remove.Pressed += () => RemovePressed?.Invoke(c.Id);
        row.AddChild(remove);
        into.AddChild(row);
        Lines.Add(text);
    }

    /// <summary>What went wrong with the server, or nothing.</summary>
    public void Tell(string text) => _message.Text = text;

    public string Message => _message.Text;
}
