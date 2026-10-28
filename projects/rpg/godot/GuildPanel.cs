using System.Globalization;
using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The guild's window (sprint 145, D79), G; redrawn in sprint 160 (D84, <c>docs/graphisme/s160-guilde.md</c>). In a guild: a
/// head with the crest in its frame, the name, the level in its hexagon and its bar, the week, how many members and their
/// average level, the leader's message (its pencil for the leader: the message, the goal, the crest); then tabs. Members: a
/// table sorted by its headers (in play, class, name, note, rank, level, points brought, achievement points, the actions the
/// rank allows in a menu), a search, the members offline shown or not, a word when one comes into play. Chest: what each tab
/// holds and its movements, deposits and withdrawals at Edmée's. Log: the guild's last hundred events. Recruitment (leader and
/// officers): invite a character in play by its name; the leader answers the applications. Out of a guild: found one (after
/// Edmée's answer), apply to one by its name with a word, and one's applications waiting. Every colour from the theme.
/// </summary>
public partial class GuildPanel : CanvasLayer
{
    public enum Tab
    {
        Members,
        Chest,
        Log,
        Recruitment,
    }

    private enum Sort
    {
        Status,
        Class,
        Name,
        Note,
        Rank,
        Level,
        Contributed,
        Achievements,
    }

    private Label _guildName = null!;
    private Label _title = null!, _message = null!, _none = null!, _level = null!, _week = null!, _notice = null!, _roster = null!, _hexLevel = null!;
    private Control _head = null!, _member = null!, _outside = null!;
    private VBoxContainer _editor = null!, _rows = null!, _chest = null!, _log = null!, _applications = null!, _mine = null!;
    private CrestView _crest = null!, _preview = null!;
    private ProgressBar _levelBar = null!, _weekBar = null!;
    private Button _edit = null!, _leave = null!;
    private LineEdit _noticeText = null!, _inviteName = null!, _foundName = null!, _search = null!, _applyName = null!, _applyWord = null!;
    private SpinBox _goal = null!;
    private TabBar _tabs = null!;
    private Control[] _pages = [];
    private HBoxContainer _invite = null!, _found = null!, _columns = null!;
    private CheckBox _offline = null!, _notify = null!;
    private readonly Dictionary<string, OptionButton> _parts = [];
    private Texts _texts = null!;
    private GuildView? _guild;
    private Guid _me;
    private DateTimeOffset _now;
    private Sort _sort = Sort.Status;
    private bool _descending;

    public event Action<string>? FoundPressed;
    public event Action<string>? InvitePressed;
    public event Action? LeavePressed;
    public event Action<Guid>? KickPressed;
    public event Action<Guid, GuildRank>? RankPressed;
    public event Action<string?, int>? NoticePressed;
    public event Action<GuildCrest>? CrestPressed;
    public event Action? ClosePressed;

    /// <summary>Since sprint 160: a note written (or cleared), an application sent, answered or withdrawn, a tab opened.</summary>
    public event Action<Guid, string?>? NotePressed;
    public event Action<string, string?>? ApplyPressed;
    public event Action<Guid, bool>? AnswerPressed;
    public event Action<Guid>? WithdrawPressed;
    public event Action<Tab>? TabOpened;

    /// <summary>The members' lines as shown (for the self-test), and the guild's name ("" out of a guild).</summary>
    public List<string> Lines { get; } = [];

    public string GuildName { get; private set; } = "";

    /// <summary>The actions each member's menu offers, by name (for the self-test).</summary>
    public Dictionary<string, List<string>> Icons { get; } = [];

    /// <summary>Whether the player asked to be told when a member comes into play.</summary>
    public bool NotifyOnline => _notify.ButtonPressed;

    /// <summary>The tab shown, the log's and the applications' lines (for the self-test).</summary>
    public Tab Shown => (Tab)_tabs.CurrentTab;

    public List<string> LogLines { get; } = [];

    public List<string> ApplicationLines { get; } = [];

    public bool RecruitmentOffered => _tabs.IsTabHidden((int)Tab.Recruitment) == false;

    public void Build(Texts texts)
    {
        _texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -520, OffsetRight = 520, OffsetTop = -320, OffsetBottom = 320 };
        panel.AddThemeStyleboxOverride("panel", OsmoseTheme.Window(0));
        AddChild(panel);
        WindowDrag.Make(panel, "guild");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 0);
        panel.AddChild(column);
        column.AddChild(TitleBar());
        var body = new MarginContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            body.AddThemeConstantOverride("margin_" + side, side == "bottom" ? 12 : 16);
        column.AddChild(body);
        var inside = new VBoxContainer();
        inside.AddThemeConstantOverride("separation", 12);
        body.AddChild(inside);
        _member = inside;
        _head = Head(inside);
        BuildEditor(inside);
        _tabs = new TabBar { CustomMinimumSize = new Vector2(0, 44), ClipTabs = false };
        foreach (string key in new[] { "guild.tab.members", "guild.tab.chest", "guild.tab.log", "guild.tab.recruit" })
            _tabs.AddTab(texts[key]);
        _tabs.TabChanged += t => ShowTab((Tab)(int)t, true);
        inside.AddChild(_tabs);
        var pages = new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        inside.AddChild(pages);
        _pages = [MembersPage(), ChestPage(), LogPage(), RecruitmentPage()];
        foreach (Control page in _pages)
        {
            page.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            pages.AddChild(page);
        }
        _outside = OutsidePage();
        column.AddChild(_outside);
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = HorizontalAlignment.Center };
        _message.AddThemeColorOverride("font_color", new Color(UiStyle.CloseHoverText));
        column.AddChild(_message);
        Show(null, Guid.Empty, DateTimeOffset.UtcNow);
    }

    private PanelContainer TitleBar()
    {
        var bar = new PanelContainer { ThemeTypeVariation = OsmoseTheme.TitleBar, CustomMinimumSize = new Vector2(0, 44) };
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        bar.AddChild(row);
        row.AddChild(Picture("res://assets/icons/bar/guild.svg", 24));
        _title = new Label { Text = _texts["guild.title"], VerticalAlignment = VerticalAlignment.Center, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _title.AddThemeFontOverride("font", OsmoseTheme.Bold());
        _title.AddThemeFontSizeOverride("font_size", 18);
        row.AddChild(_title);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44), ThemeTypeVariation = OsmoseTheme.CloseButton };
        close.Pressed += () => ClosePressed?.Invoke();
        row.AddChild(close);
        return bar;
    }

    private static TextureRect Picture(string path, int size) => new()
    {
        Texture = GD.Load<Texture2D>(path),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        CustomMinimumSize = new Vector2(size, size),
        MouseFilter = Control.MouseFilterEnum.Ignore,
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
    };

    private static Label Text(string text, string colour, int size = 14, bool bold = false)
    {
        var label = new Label { Text = text, VerticalAlignment = VerticalAlignment.Center, ClipText = true };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(colour));
        if (bold)
            label.AddThemeFontOverride("font", OsmoseTheme.Bold());
        return label;
    }

    // The head (sprint 160): the crest in its frame; the name, the level in its hexagon and its bar, the week, the roster;
    // the leader's message with its pencil.
    private HBoxContainer Head(VBoxContainer into)
    {
        var head = new HBoxContainer { CustomMinimumSize = new Vector2(0, 104) };
        head.AddThemeConstantOverride("separation", 16);
        into.AddChild(head);
        var frame = new PanelContainer { ThemeTypeVariation = OsmoseTheme.IconFrame, CustomMinimumSize = new Vector2(104, 104) };
        _crest = new CrestView { CustomMinimumSize = new Vector2(80, 80), MouseFilter = Control.MouseFilterEnum.Ignore };
        frame.AddChild(_crest);
        head.AddChild(frame);
        var middle = new VBoxContainer { CustomMinimumSize = new Vector2(380, 0), TooltipText = _texts["guild.points.how"], MouseFilter = Control.MouseFilterEnum.Pass };
        middle.AddThemeConstantOverride("separation", 4);
        head.AddChild(middle);
        _hexLevel = new Label { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, ThemeTypeVariation = OsmoseTheme.GoldBandLabel, CustomMinimumSize = new Vector2(40, 44) };
        var named = new HBoxContainer();
        named.AddThemeConstantOverride("separation", 12);
        var hex = new TextureRect { Texture = GD.Load<Texture2D>("res://assets/ui/level-hex.svg"), CustomMinimumSize = new Vector2(40, 44), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
        _hexLevel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _hexLevel.AddThemeFontOverride("font", OsmoseTheme.Bold());
        hex.AddChild(_hexLevel);
        named.AddChild(hex);
        var names = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _level = Text("", UiStyle.Text, 14);
        _level.ClipText = false;
        _guildName = Text("", "#ffffff", 20, true);
        _guildName.ClipText = false;
        names.AddChild(_guildName);
        names.AddChild(_level);
        named.AddChild(names);
        middle.AddChild(named);
        _levelBar = new ProgressBar { MinValue = 0, MaxValue = 1, Step = 0.001, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 12), ThemeTypeVariation = OsmoseTheme.GoldBar, MouseFilter = Control.MouseFilterEnum.Ignore };
        middle.AddChild(_levelBar);
        var week = new HBoxContainer();
        week.AddThemeConstantOverride("separation", 8);
        _week = Text("", UiStyle.Text, 14);
        _week.ClipText = false;
        week.AddChild(_week);
        _weekBar = new ProgressBar { MinValue = 0, MaxValue = 1, Step = 0.001, ShowPercentage = false, CustomMinimumSize = new Vector2(120, 8), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, ThemeTypeVariation = OsmoseTheme.ProgressBarCompact, MouseFilter = Control.MouseFilterEnum.Ignore };
        week.AddChild(_weekBar);
        middle.AddChild(week);
        _roster = Text("", UiStyle.TextDim, 14);
        _roster.ClipText = false;
        middle.AddChild(_roster);
        // The leader's message, its pencil for the leader alone.
        var message = new PanelContainer { ThemeTypeVariation = OsmoseTheme.DetailPanel, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        head.AddChild(message);
        var inMessage = new VBoxContainer();
        message.AddChild(inMessage);
        var top = new HBoxContainer();
        top.AddChild(new Label { Text = _texts["guild.message.title"].ToUpperInvariant(), ThemeTypeVariation = OsmoseTheme.SectionLabel, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        _edit = new Button { CustomMinimumSize = new Vector2(44, 44), TooltipText = _texts["guild.edit"], Icon = GD.Load<Texture2D>("res://assets/icons/relief/pencil.svg"), ExpandIcon = true, ThemeTypeVariation = OsmoseTheme.TreeToggle };
        _edit.AddThemeConstantOverride("icon_max_width", 24);
        _edit.Pressed += () => _editor.Visible = !_editor.Visible;
        top.AddChild(_edit);
        inMessage.AddChild(top);
        _notice = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(240, 0) };
        _notice.AddThemeFontSizeOverride("font_size", 14);
        _notice.AddThemeColorOverride("font_color", new Color(UiStyle.Text));
        inMessage.AddChild(_notice);
        return head;
    }

    // The leader's editor (sprint 156): the message, the goal, the crest's four parts.
    private void BuildEditor(VBoxContainer column)
    {
        _editor = new VBoxContainer { Visible = false };
        _editor.AddThemeConstantOverride("separation", 6);
        column.AddChild(_editor);
        var notice = new HBoxContainer();
        notice.AddThemeConstantOverride("separation", 8);
        _editor.AddChild(notice);
        _noticeText = new LineEdit { PlaceholderText = _texts["guild.message.edit", GuildRules.MessageLength], MaxLength = GuildRules.MessageLength, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 44) };
        notice.AddChild(_noticeText);
        notice.AddChild(new Label { Text = _texts["guild.goal"], VerticalAlignment = VerticalAlignment.Center });
        _goal = new SpinBox { MinValue = 1, MaxValue = GuildRules.MostGoal, Step = 10, CustomMinimumSize = new Vector2(110, 44) };
        notice.AddChild(_goal);
        var saveNotice = new Button { Text = _texts["guild.save"], CustomMinimumSize = new Vector2(120, 44), ThemeTypeVariation = OsmoseTheme.Primary };
        saveNotice.Pressed += () => NoticePressed?.Invoke(_noticeText.Text.Trim(), (int)_goal.Value);
        notice.AddChild(saveNotice);
        var crest = new HBoxContainer();
        crest.AddThemeConstantOverride("separation", 6);
        _editor.AddChild(crest);
        _preview = new CrestView { CustomMinimumSize = new Vector2(44, 44), MouseFilter = Control.MouseFilterEnum.Ignore };
        crest.AddChild(_preview);
        Part(crest, "shape", GuildCrests.Shapes, id => _texts["guild.crest.shape." + id]);
        Part(crest, "icon", GuildCrests.Icons, id => _texts["guild.crest.icon." + id]);
        Part(crest, "field", [.. GuildCrests.Colours.Keys], id => _texts["guild.crest.colour." + id]);
        Part(crest, "charge", [.. GuildCrests.Colours.Keys], id => _texts["guild.crest.colour." + id]);
        var saveCrest = new Button { Text = _texts["guild.save"], CustomMinimumSize = new Vector2(120, 44), ThemeTypeVariation = OsmoseTheme.Primary };
        saveCrest.Pressed += () => CrestPressed?.Invoke(Composed);
        crest.AddChild(saveCrest);
    }

    // One part of the crest to choose, by its name in the player's language; the preview follows.
    private void Part(HBoxContainer into, string part, IReadOnlyList<string> ids, Func<string, string> name)
    {
        var choice = new OptionButton { CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, TooltipText = _texts["guild.crest." + part], FitToLongestItem = false, ClipText = true };
        foreach (string id in ids)
        {
            choice.AddItem(name(id));
            choice.SetItemMetadata(choice.ItemCount - 1, id);
        }
        choice.ItemSelected += _ => _preview.Crest = Composed;
        into.AddChild(choice);
        _parts[part] = choice;
    }

    private string Chosen(string part) => _parts[part].GetItemMetadata(_parts[part].Selected).AsString();

    /// <summary>The crest the leader composes in the editor.</summary>
    public GuildCrest Composed => new(Chosen("shape"), Chosen("icon"), Chosen("field"), Chosen("charge"));

    private void Choose(GuildCrest crest)
    {
        foreach ((string part, string id) in new[] { ("shape", crest.Shape), ("icon", crest.Icon), ("field", crest.Field), ("charge", crest.Charge) })
        {
            OptionButton choice = _parts[part];
            for (int i = 0; i < choice.ItemCount; i++)
            {
                if (choice.GetItemMetadata(i).AsString() == id)
                    choice.Select(i);
            }
        }
        _preview.Crest = crest;
    }

    // Members: the search, the headers that sort, the rows; the boxes and the button to leave at the foot.
    private VBoxContainer MembersPage()
    {
        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 8);
        _search = new LineEdit { PlaceholderText = _texts["guild.search"], ClearButtonEnabled = true, CustomMinimumSize = new Vector2(260, 44), SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
        _search.TextChanged += _ => Rows();
        page.AddChild(_search);
        // The headers in the rows' margins, so that each sits over its column.
        var headers = new MarginContainer();
        headers.AddThemeConstantOverride("margin_left", 12);
        headers.AddThemeConstantOverride("margin_right", 24);
        _columns = new HBoxContainer { CustomMinimumSize = new Vector2(0, 32) };
        _columns.AddThemeConstantOverride("separation", 0);
        headers.AddChild(_columns);
        page.AddChild(headers);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _rows.AddThemeConstantOverride("separation", 0);
        scroll.AddChild(_rows);
        page.AddChild(scroll);
        var foot = new HBoxContainer();
        foot.AddThemeConstantOverride("separation", 16);
        _offline = new CheckBox { Text = _texts["guild.show-offline"], ButtonPressed = true, CustomMinimumSize = new Vector2(0, 44) };
        _offline.Toggled += _ => Rows();
        foot.AddChild(_offline);
        _notify = new CheckBox { Text = _texts["guild.notify-online"], ButtonPressed = true, CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foot.AddChild(_notify);
        _leave = new Button { Text = _texts["guild.leave"], CustomMinimumSize = new Vector2(160, 44) };
        _leave.AddThemeColorOverride("font_hover_color", new Color(UiStyle.CloseHoverText));
        _leave.Pressed += () => LeavePressed?.Invoke();
        foot.AddChild(_leave);
        page.AddChild(foot);
        return page;
    }

    private static (Sort Key, int Width, string Header)[] Columns => [(Sort.Status, 32, ""), (Sort.Class, 56, "guild.col.class"), (Sort.Name, 0, "guild.col.name"),
        (Sort.Note, 200, "guild.col.note"), (Sort.Rank, 120, "guild.col.rank"), (Sort.Level, 64, "guild.col.level"), (Sort.Contributed, 96, "guild.col.contribution"),
        (Sort.Achievements, 96, "guild.col.achievements")];

    private void Headers()
    {
        Clear(_columns);
        foreach ((Sort key, int width, string header) in Columns)
        {
            string arrow = key == _sort ? (_descending ? " ▼" : " ▲") : "";
            var b = new Button { Text = header.Length == 0 ? "" : _texts[header] + arrow, CustomMinimumSize = new Vector2(width, 32), SizeFlagsHorizontal = width == 0 ? Control.SizeFlags.ExpandFill : Control.SizeFlags.Fill, Alignment = HorizontalAlignment.Left, ThemeTypeVariation = OsmoseTheme.Link, FocusMode = Control.FocusModeEnum.None, ClipText = true };
            if (header is "guild.col.contribution" or "guild.col.achievements")
                b.TooltipText = _texts[header + ".tip"];
            b.Pressed += () =>
            {
                _descending = _sort == key && !_descending;
                _sort = key;
                Rows();
            };
            _columns.AddChild(b);
        }
        _columns.AddChild(new Control { CustomMinimumSize = new Vector2(44, 0) });
    }

    private static void Clear(Node container)
    {
        foreach (Node n in container.GetChildren())
        {
            container.RemoveChild(n);
            n.QueueFree();
        }
    }

    // The rows, filtered by the search and the box, in the order chosen (the guild's order when none).
    private void Rows()
    {
        Clear(_rows);
        Lines.Clear();
        Icons.Clear();
        if (_guild is null)
            return;
        Headers();
        string wanted = AchievementsPage.Plain(_search.Text);
        IEnumerable<GuildMember> shown = _guild.Members.Where(m => (_offline.ButtonPressed || m.Online) && (wanted.Length == 0 || AchievementsPage.Plain(m.Name).Contains(wanted, StringComparison.Ordinal)));
        Func<GuildMember, IComparable> key = _sort switch
        {
            Sort.Class => m => m.Class ?? "",
            Sort.Name => m => m.Name,
            Sort.Note => m => m.Note ?? "",
            Sort.Rank => m => (int)m.Rank,
            Sort.Level => m => m.Level,
            Sort.Contributed => m => m.Contributed,
            Sort.Achievements => m => m.AchievementPoints,
            _ => m => 0,
        };
        if (_sort != Sort.Status)
            shown = _descending ? shown.OrderByDescending(key) : shown.OrderBy(key);
        foreach (GuildMember m in shown)
            Row(m);
        if (_rows.GetChildCount() == 0)
            _rows.AddChild(Text(wanted.Length > 0 ? _texts["guild.search.empty", _search.Text.Trim()] : _texts["guild.all-offline"], UiStyle.TextDim));
    }

    private string Where(GuildMember m) =>
        m.Zone is string zone ? _texts.ZoneName(zone, GameData.Embedded)
        : GuildPage.Ago(m.LastSeen, _now) is (int count, string unit) ? _texts["guild.seen." + unit, count]
        : _texts["guild.seen.never"];

    // One member's row: in play or not, its class, name (and "you"), note, rank, level, points brought, achievement points, its menu.
    private void Row(GuildMember m)
    {
        GuildView guild = _guild!;
        string line = _texts[m.Online ? "guild.online" : "guild.offline", m.Name, m.Level, _texts["guild.rank." + m.Rank], Where(m)];
        Lines.Add(line);
        var card = new PanelContainer { ThemeTypeVariation = OsmoseTheme.ListRow, TooltipText = line, CustomMinimumSize = new Vector2(0, 44) };
        if (m.Id == _me)
            card.AddThemeStyleboxOverride("panel", MeRow());
        _rows.AddChild(card);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);
        card.AddChild(row);
        string dim = m.Online ? UiStyle.Text : UiStyle.TextOff;
        var status = new CenterContainer { CustomMinimumSize = new Vector2(32, 0) };
        var dot = new Panel { CustomMinimumSize = new Vector2(10, 10), MouseFilter = Control.MouseFilterEnum.Ignore, TooltipText = _texts[m.Online ? "guild.col.status.on" : "guild.col.status.off"] };
        dot.AddThemeStyleboxOverride("panel", m.Online ? OsmoseTheme.Flat(ElementStyle.Colour(Element.Air), corner: 5) : OsmoseTheme.Flat("#00000000", UiStyle.TextOff, 2, 5));
        status.AddChild(dot);
        row.AddChild(status);
        var cls = new CenterContainer { CustomMinimumSize = new Vector2(56, 0) };
        if (m.Class is string id && ResourceLoader.Exists($"res://assets/icons/relief/class-{id}.svg"))
        {
            TextureRect icon = Picture($"res://assets/icons/relief/class-{id}.svg", 28);
            icon.TooltipText = GameData.Embedded.Class(id)?.Name.In(_texts.Lang) ?? id;
            icon.MouseFilter = Control.MouseFilterEnum.Pass;
            icon.Modulate = m.Online ? Colors.White : new Color(1, 1, 1, 0.5f);
            cls.AddChild(icon);
        }
        row.AddChild(cls);
        Label name = Text(m.Id == _me ? $"{m.Name} {_texts["guild.you"]}" : m.Name, m.Online ? "#ffffff" : UiStyle.TextOff, 16, true);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(name);
        Label note = Text(m.Note ?? "", dim);
        note.CustomMinimumSize = new Vector2(200, 0);
        note.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        row.AddChild(note);
        Label rank = Text(_texts["guild.rank." + m.Rank], m.Rank == GuildRank.Leader ? UiStyle.Gold : dim);
        rank.CustomMinimumSize = new Vector2(120, 0);
        row.AddChild(rank);
        foreach ((string value, int width) in new[] { (m.Level.ToString(CultureInfo.InvariantCulture), 64), (AchievementsPage.Grouped(m.Contributed, _texts.Lang), 96), (AchievementsPage.Grouped(m.AchievementPoints, _texts.Lang), 96) })
        {
            Label cell = Text(value, dim);
            cell.CustomMinimumSize = new Vector2(width, 0);
            row.AddChild(cell);
        }
        // Its actions, those the player's rank allows, in a menu.
        var actions = new MenuButton { Text = "⋯", CustomMinimumSize = new Vector2(44, 44), TooltipText = _texts["guild.actions"], Flat = true };
        PopupMenu menu = actions.GetPopup();
        var icons = Icons[m.Name] = [];
        var chosen = new List<Action>();
        void Offer(string icon, string text, Action pressed)
        {
            menu.AddItem(text, chosen.Count);
            chosen.Add(pressed);
            icons.Add(icon);
        }
        GuildActions can = GuildPage.Actions(guild, _me, m);
        if (can.Promote)
            Offer("promote", _texts["guild.promote", m.Name], () => RankPressed?.Invoke(m.Id, GuildRank.Officer));
        if (can.Demote)
            Offer("demote", _texts["guild.demote", m.Name], () => RankPressed?.Invoke(m.Id, GuildRank.Member));
        if (can.HandOver)
            Offer("hand-over", _texts["guild.hand-over", m.Name], () => RankPressed?.Invoke(m.Id, GuildRank.Leader));
        if (can.Kick)
            Offer("kick", _texts["guild.kick", m.Name], () => KickPressed?.Invoke(m.Id));
        // The note: one's own written in the search's place; another's cleared by the leader and the officers.
        if (m.Id == _me)
            menu.AddItem(_texts["guild.note.edit"], 100);
        else if (m.Note is not null && GuildRules.WhyNotNote(guild.Members.First(x => x.Id == _me).Rank, false, null) is null)
            menu.AddItem(_texts["guild.note.clear"], 101);
        menu.IdPressed += i =>
        {
            if (i == 100)
                AskNote(m);
            else if (i == 101)
                NotePressed?.Invoke(m.Id, null);
            else
                chosen[(int)i]();
        };
        actions.Visible = menu.ItemCount > 0;
        row.AddChild(actions);
        if (!actions.Visible)
            row.AddChild(new Control { CustomMinimumSize = new Vector2(44, 0) });
    }

    private static StyleBoxFlat MeRow()
    {
        StyleBoxFlat box = OsmoseTheme.Flat(UiStyle.Hover, UiStyle.ChocolateLight, 0, 0, 12);
        box.BorderWidthLeft = 3;
        (box.ContentMarginTop, box.ContentMarginBottom) = (0, 0);
        return box;
    }

    // One's note, asked in a small field over the list.
    private void AskNote(GuildMember m)
    {
        var dialog = new ConfirmationDialog { Title = _texts["guild.note.edit"], OkButtonText = _texts["guild.save"], CancelButtonText = _texts["guild.cancel"] };
        var field = new LineEdit { Text = m.Note ?? "", MaxLength = GuildRules.NoteLength, PlaceholderText = _texts["guild.note.hint", GuildRules.NoteLength], CustomMinimumSize = new Vector2(320, 44) };
        dialog.AddChild(field);
        dialog.Confirmed += () => NotePressed?.Invoke(m.Id, field.Text.Trim());
        AddChild(dialog);
        dialog.PopupCentered();
    }

    // Chest: what each tab holds and its last movements; deposits and withdrawals at Edmée's.
    private ScrollContainer ChestPage()
    {
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _chest = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _chest.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_chest);
        return scroll;
    }

    /// <summary>The chest as the server says (sprint 160: seen anywhere, changed at Edmée's).</summary>
    public void ShowChest(ChestView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        Clear(_chest);
        _chest.AddChild(Text(_texts["guild.chest.where"], UiStyle.TextDim));
        for (int t = 0; t < view.Chest.Tabs.Count; t++)
        {
            GuildTab tab = view.Chest.Tabs[t];
            _chest.AddChild(new Label { Text = $"{_texts["guild.chest.tab", t + 1]} · {_texts[tab.Open ? "guild.chest.open" : "guild.chest.reserved"]}".ToUpperInvariant(), ThemeTypeVariation = OsmoseTheme.SectionLabel });
            string held = string.Join(", ", tab.Items.Select(i => $"{(GameData.Embedded.Items.TryGetValue(i.Item, out Item? it) ? it.Name.In(_texts.Lang) : i.Item)} ×{i.Count}"));
            Label items = Text(held.Length == 0 ? "—" : held, UiStyle.Text);
            items.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            items.ClipText = false;
            _chest.AddChild(items);
        }
        _chest.AddChild(new Label { Text = _texts["guild.chest.moves"].ToUpperInvariant(), ThemeTypeVariation = OsmoseTheme.SectionLabel });
        foreach (GuildMove move in view.Chest.Journal.Take(20))
        {
            string item = GameData.Embedded.Items.TryGetValue(move.Item, out Item? it) ? it.Name.In(_texts.Lang) : move.Item;
            _chest.AddChild(Text($"{move.Who} · {(move.Count > 0 ? "+" : "")}{move.Count} {item}", UiStyle.Text));
        }
    }

    // Log: the guild's events, the newest first.
    private ScrollContainer LogPage()
    {
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _log = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(_log);
        return scroll;
    }

    /// <summary>The guild's log as the server says (sprint 160).</summary>
    public void ShowLog(IReadOnlyList<GuildEvent> log)
    {
        ArgumentNullException.ThrowIfNull(log);
        Clear(_log);
        LogLines.Clear();
        foreach (GuildEvent e in log)
        {
            string text = e.Kind switch
            {
                GuildEventKind.Joined when e.By is not null => _texts["guild.log.invited-by", e.Who, e.By],
                GuildEventKind.Kicked => _texts["guild.log.kicked", e.Who, e.By ?? ""],
                GuildEventKind.Rank => _texts["guild.log.rank", e.Who, e.By ?? "", Enum.TryParse(e.Value, out GuildRank r) ? _texts["guild.rank." + r].ToLowerInvariant() : e.Value ?? ""],
                GuildEventKind.Level => _texts["guild.log.level", e.Who],
                _ => _texts["guild.log." + e.Kind.ToString().ToLowerInvariant(), e.Who],
            };
            LogLines.Add(text);
            var row = new PanelContainer { ThemeTypeVariation = OsmoseTheme.ListRow, CustomMinimumSize = new Vector2(0, 44) };
            var line = new HBoxContainer();
            line.AddThemeConstantOverride("separation", 12);
            Label when = Text(GuildPage.Ago(e.At, _now) is (int count, string unit) ? _texts["guild.seen." + unit, count] : "", UiStyle.TextDim);
            when.CustomMinimumSize = new Vector2(120, 0);
            line.AddChild(when);
            Label what = Text(text, UiStyle.Text);
            what.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            line.AddChild(what);
            row.AddChild(line);
            _log.AddChild(row);
        }
        if (log.Count == 0)
            _log.AddChild(Text(_texts["guild.log.empty"], UiStyle.TextDim));
    }

    // Recruitment: invite by name (leader and officers); the applications (the leader).
    private VBoxContainer RecruitmentPage()
    {
        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 8);
        (_invite, _inviteName) = NameRow(page, _texts["guild.name"], _texts["guild.invite"], () => InvitePressed?.Invoke(_inviteName.Text.Trim()));
        page.AddChild(new Label { Text = _texts["guild.applications"].ToUpperInvariant(), ThemeTypeVariation = OsmoseTheme.SectionLabel });
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _applications = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(_applications);
        page.AddChild(scroll);
        return page;
    }

    // A name and its main button: to found a guild, to invite someone, to apply.
    private static (HBoxContainer Row, LineEdit Name) NameRow(VBoxContainer into, string placeholder, string text, Action pressed)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var name = new LineEdit { PlaceholderText = placeholder, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 44), MaxLength = 20 };
        name.TextSubmitted += _ => pressed();
        row.AddChild(name);
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(170, 44), ThemeTypeVariation = OsmoseTheme.Primary };
        b.Pressed += pressed;
        row.AddChild(b);
        into.AddChild(row);
        return (row, name);
    }

    /// <summary>
    /// The applications as the server says (sprint 160): in a guild, those to it, with the leader's answers; out of one,
    /// one's own, each to withdraw.
    /// </summary>
    public void ShowApplications(IReadOnlyList<GuildApplication> applications)
    {
        ArgumentNullException.ThrowIfNull(applications);
        VBoxContainer into = _guild is null ? _mine : _applications;
        Clear(into);
        ApplicationLines.Clear();
        foreach (GuildApplication a in applications)
        {
            string text = _guild is null ? _texts["guild.apply.mine", a.GuildName] : _texts["guild.apply.line", a.Name, a.Level];
            ApplicationLines.Add(text);
            var row = new PanelContainer { ThemeTypeVariation = OsmoseTheme.ListRow, CustomMinimumSize = new Vector2(0, 64) };
            var line = new HBoxContainer();
            line.AddThemeConstantOverride("separation", 8);
            var words = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            words.AddChild(Text(text, "#ffffff", 16, true));
            if (a.Message is string word)
                words.AddChild(Text($"« {word} »", UiStyle.Text));
            line.AddChild(words);
            if (_guild is null)
            {
                var withdraw = new Button { Text = _texts["guild.apply.withdraw"], CustomMinimumSize = new Vector2(0, 44) };
                withdraw.Pressed += () => WithdrawPressed?.Invoke(a.Guild);
                line.AddChild(withdraw);
            }
            else
            {
                var accept = new Button { Text = _texts["guild.apply.accept"], CustomMinimumSize = new Vector2(0, 44), ThemeTypeVariation = OsmoseTheme.Primary };
                accept.Pressed += () => AnswerPressed?.Invoke(a.Character, true);
                line.AddChild(accept);
                var refuse = new Button { Text = _texts["guild.apply.refuse"], CustomMinimumSize = new Vector2(0, 44) };
                refuse.Pressed += () => AnswerPressed?.Invoke(a.Character, false);
                line.AddChild(refuse);
            }
            row.AddChild(line);
            into.AddChild(row);
        }
        if (applications.Count == 0)
            into.AddChild(Text(_texts["guild.apply.none"], UiStyle.TextDim));
        if (_guild is not null)
            _tabs.SetTabTitle((int)Tab.Recruitment, applications.Count == 0 ? _texts["guild.tab.recruit"] : $"{_texts["guild.tab.recruit"]} ({applications.Count})");
    }

    // Out of a guild: what a guild is, found one (after Edmée's answer), apply to one, one's applications.
    private MarginContainer OutsidePage()
    {
        var margin = new MarginContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 12);
        margin.AddChild(page);
        _none = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(0, 44) };
        _none.AddThemeColorOverride("font_color", new Color(UiStyle.Text));
        page.AddChild(_none);
        page.AddChild(new Label { Text = _texts["guild.found.title", GuildRules.Price].ToUpperInvariant(), ThemeTypeVariation = OsmoseTheme.SectionLabel });
        (_found, _foundName) = NameRow(page, _texts["guild.found.name"], _texts["guild.found"], () => FoundPressed?.Invoke(_foundName.Text.Trim()));
        page.AddChild(new Label { Text = _texts["guild.apply.title"].ToUpperInvariant(), ThemeTypeVariation = OsmoseTheme.SectionLabel });
        var apply = new HBoxContainer();
        apply.AddThemeConstantOverride("separation", 8);
        _applyName = new LineEdit { PlaceholderText = _texts["guild.apply.name"], CustomMinimumSize = new Vector2(240, 44), MaxLength = 20 };
        apply.AddChild(_applyName);
        _applyWord = new LineEdit { PlaceholderText = _texts["guild.apply.word", GuildRules.ApplicationWordLength], CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MaxLength = GuildRules.ApplicationWordLength };
        apply.AddChild(_applyWord);
        var send = new Button { Text = _texts["guild.apply.send"], CustomMinimumSize = new Vector2(140, 44), ThemeTypeVariation = OsmoseTheme.Primary };
        send.Pressed += () => ApplyPressed?.Invoke(_applyName.Text.Trim(), _applyWord.Text.Trim() is { Length: > 0 } w ? w : null);
        apply.AddChild(send);
        page.AddChild(apply);
        page.AddChild(new Label { Text = _texts["guild.apply.waiting"].ToUpperInvariant(), ThemeTypeVariation = OsmoseTheme.SectionLabel });
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _mine = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(_mine);
        page.AddChild(scroll);
        return margin;
    }

    // A tab's page shown; the window's owner told when the player opens it, to fetch what it shows.
    private void ShowTab(Tab tab, bool opened)
    {
        for (int i = 0; i < _pages.Length; i++)
            _pages[i].Visible = i == (int)tab;
        if (opened)
            TabOpened?.Invoke(tab);
    }

    /// <summary>Opens a tab, as a click on it does (for the self-test).</summary>
    public void Open(Tab tab) => _tabs.CurrentTab = (int)tab;

    // The standing told: the crest, the level, the week, the roster, the message; the editor kept to the leader.
    private void ShowStanding(GuildView? guild)
    {
        GuildStanding? s = guild?.Standing;
        bool lead = guild is not null && GuildPage.CanLead(guild, _me);
        _edit.Visible = lead;
        if (!lead)
            _editor.Visible = false;
        if (guild is null)
            return;
        (int count, int most) = GuildPage.Room(guild);
        int average = guild.Members.Count == 0 ? 0 : (int)guild.Members.Average(m => m.Level);
        _roster.Text = _texts["guild.roster", count, most, average];
        _guildName.Text = guild.Name;
        if (s is null)
            return;
        _crest.Crest = s.Crest;
        _hexLevel.Text = s.Level.ToString(CultureInfo.InvariantCulture);
        _level.Text = _texts["guild.level", s.Level] + " · " + (s.ToNext == 0 ? _texts["guild.points.top", s.Points] : _texts["guild.points", s.Into, s.ToNext, s.Level + 1]);
        _levelBar.Value = GuildPage.Toward(s);
        _week.Text = _texts["guild.week", s.Week, s.Goal];
        _weekBar.Value = GuildPage.TowardGoal(s);
        _notice.Text = s.Message is string m ? $"« {m} »" : _texts["guild.message.none"];
        if (!_editor.Visible)
        {
            _noticeText.Text = s.Message ?? "";
            _goal.Value = s.Goal;
            Choose(s.Crest);
        }
    }

    /// <summary>For the self-test: the level's and the week's lines, the message, the crest drawn, whether the pencil is offered.</summary>
    public string StandingText => $"{_level.Text} | {_week.Text} | {_notice.Text}";

    public GuildCrest? CrestShown => _guild?.Standing is not null ? _crest.Crest : null;

    public bool CanEdit => _edit.Visible;

    /// <summary>The leader's editor opened, as the pencil does (for the self-test).</summary>
    public void OpenEditor() => _editor.Visible = _edit.Visible;

    /// <summary>After Edmée's answer: the name to found a guild with, shown while out of one.</summary>
    public bool FoundingOffered { get; private set; }

    public void OfferFounding()
    {
        FoundingOffered = true;
        _found.Visible = GuildName.Length == 0;
        _foundName.GrabFocus();
    }

    /// <summary>The guild as the server says (null: in none), seen by <paramref name="me"/> at <paramref name="now"/>; the name fields emptied.</summary>
    public void Show(GuildView? guild, Guid me, DateTimeOffset now)
    {
        (_guild, _me, _now) = (guild, me, now);
        _inviteName.Text = "";
        _foundName.Text = "";
        GuildName = guild?.Name ?? "";
        _title.Text = guild is null ? _texts["guild.title"] : $"{_texts["guild.title"]} · {guild.Name}";
        _member.Visible = guild is not null;
        _outside.Visible = guild is null;
        _none.Text = _texts["guild.none", GuildRules.Price];
        _found.Visible = guild is null && FoundingOffered;
        bool recruits = guild is not null && GuildPage.CanInvite(guild, me);
        _tabs.SetTabHidden((int)Tab.Recruitment, !recruits);
        if (!recruits && _tabs.CurrentTab == (int)Tab.Recruitment)
            _tabs.CurrentTab = (int)Tab.Members;
        _invite.Visible = recruits;
        ShowStanding(guild);
        Rows();
        ShowTab((Tab)_tabs.CurrentTab, false);
    }

    /// <summary>What went wrong with the server, or nothing.</summary>
    public void Tell(string text) => _message.Text = text;

    public string Message => _message.Text;
}
