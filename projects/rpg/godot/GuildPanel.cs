using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The guild's window (sprint 145, D79), G; laid out as the contacts' window: the guild's name; for the leader and the
/// officers, a name to invite; under the columns' names (status, name, rank, level, class, in play or last seen), one card
/// a member, by rank then those in play first (<see cref="GuildRules.Ordered"/>), each with the icons its rank allows
/// (<see cref="GuildPage"/>): name officer, make member, hand the title over, send out; at the bottom, how many of the fifty
/// and the button to leave. Out of a guild, how to get into one, and after Edmée's answer the name to found one. The zone,
/// never the map. The icons are game-icons.net's (CC BY 3.0, authors in <c>assets/icons/guild/credits.json</c>).
/// Since sprint 156 (D83), under the title, the crest, the level and its bar, the week's points against the goal and the
/// leader's message; the leader's pencil opens what it writes: the message, the goal, the crest's four parts.
/// </summary>
public partial class GuildPanel : CanvasLayer
{
    private static readonly Color Online = new("7fd06b"), Away = new(1, 1, 1, 0.3f), Gold = new("e2b23a");

    private const int StatusWidth = 48, RankWidth = 100, LevelWidth = 64, ClassWidth = 120, WhereWidth = 150, ActionsWidth = 184;

    private Label _title = null!, _room = null!, _message = null!, _none = null!, _level = null!, _week = null!, _notice = null!;
    private HBoxContainer _standing = null!;
    private VBoxContainer _editor = null!;
    private CrestView _crest = null!, _preview = null!;
    private ProgressBar _levelBar = null!, _weekBar = null!;
    private Button _edit = null!;
    private LineEdit _noticeText = null!;
    private SpinBox _goal = null!;
    private readonly Dictionary<string, OptionButton> _parts = [];
    private HBoxContainer _invite = null!, _found = null!, _columns = null!;
    private LineEdit _inviteName = null!, _foundName = null!;
    private VBoxContainer _rows = null!;
    private Button _leave = null!;
    private Texts _texts = null!;

    public event Action<string>? FoundPressed;
    public event Action<string>? InvitePressed;
    public event Action? LeavePressed;
    public event Action<Guid>? KickPressed;
    public event Action<Guid, GuildRank>? RankPressed;
    public event Action<string?, int>? NoticePressed;
    public event Action<GuildCrest>? CrestPressed;
    public event Action? ClosePressed;

    /// <summary>The members' lines as shown (for the self-test), and the guild's name ("" out of a guild).</summary>
    public List<string> Lines { get; } = [];

    public string GuildName { get; private set; } = "";

    /// <summary>The icons each member's card shows, by name (for the self-test).</summary>
    public Dictionary<string, List<string>> Icons { get; } = [];

    public void Build(Texts texts)
    {
        _texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -420, OffsetRight = 420, OffsetTop = -290, OffsetBottom = 290 };
        StyleBoxFlat frame = InventoryPanel.Background();
        frame.ContentMarginLeft = frame.ContentMarginRight = 14;
        frame.ContentMarginTop = frame.ContentMarginBottom = 10;
        panel.AddThemeStyleboxOverride("panel", frame);
        AddChild(panel);
        WindowDrag.Make(panel, "guild");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);

        var head = new HBoxContainer();
        _title = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 20);
        head.AddChild(_title);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44), Flat = true };
        close.Pressed += () => ClosePressed?.Invoke();
        head.AddChild(close);
        column.AddChild(head);
        column.AddChild(new HSeparator());
        BuildStanding(column);

        _none = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1, 1, 1, 0.75f), CustomMinimumSize = new Vector2(0, 80), VerticalAlignment = VerticalAlignment.Center };
        column.AddChild(_none);
        (_found, _foundName) = NameRow(column, texts["guild.found.name"], texts["guild.found"], "hand-over", () => FoundPressed?.Invoke(_foundName.Text.Trim()));
        (_invite, _inviteName) = NameRow(column, texts["guild.name"], texts["guild.invite"], "invite", () => InvitePressed?.Invoke(_inviteName.Text.Trim()));

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
        _room = new Label { VerticalAlignment = VerticalAlignment.Center, Modulate = new Color(1, 1, 1, 0.7f), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foot.AddChild(_room);
        _leave = new Button { Text = texts["guild.leave"], CustomMinimumSize = new Vector2(190, 44) };
        _leave.AddThemeColorOverride("font_hover_color", new Color("ff8a75"));
        _leave.Pressed += () => LeavePressed?.Invoke();
        foot.AddChild(_leave);
        column.AddChild(foot);
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", new Color("ffb4a2"));
        column.AddChild(_message);
        Show(null, Guid.Empty, DateTimeOffset.UtcNow);
    }

    // Where the guild stands (sprint 156): the crest, the level and the week, the message; the leader's editor under it.
    private void BuildStanding(VBoxContainer column)
    {
        _standing = new HBoxContainer();
        _standing.AddThemeConstantOverride("separation", 12);
        column.AddChild(_standing);
        _crest = new CrestView { CustomMinimumSize = new Vector2(72, 72), MouseFilter = Control.MouseFilterEnum.Ignore };
        _standing.AddChild(_crest);
        var bars = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, TooltipText = _texts["guild.points.how"], MouseFilter = Control.MouseFilterEnum.Pass };
        bars.AddThemeConstantOverride("separation", 2);
        _standing.AddChild(bars);
        (_level, _levelBar) = Bar(bars, Gold);
        (_week, _weekBar) = Bar(bars, FighterView.PlayerColour);
        _notice = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1, 1, 1, 0.85f) };
        bars.AddChild(_notice);
        _edit = new Button { Text = "✎", CustomMinimumSize = new Vector2(44, 44), TooltipText = _texts["guild.edit"], SizeFlagsVertical = Control.SizeFlags.ShrinkBegin };
        _edit.Pressed += () => _editor.Visible = !_editor.Visible;
        _standing.AddChild(_edit);

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
        var saveNotice = new Button { Text = _texts["guild.save"], CustomMinimumSize = new Vector2(120, 44) };
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
        var saveCrest = new Button { Text = _texts["guild.save"], CustomMinimumSize = new Vector2(120, 44) };
        saveCrest.Pressed += () => CrestPressed?.Invoke(Composed);
        crest.AddChild(saveCrest);
    }

    private static (Label Text, ProgressBar Bar) Bar(VBoxContainer into, Color fill)
    {
        var text = new Label { Modulate = new Color(1, 1, 1, 0.85f) };
        text.AddThemeFontSizeOverride("font_size", 14);
        into.AddChild(text);
        var bar = new ProgressBar { MinValue = 0, MaxValue = 1, Step = 0.001, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 8), MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AddThemeStyleboxOverride("background", Card(new Color(1, 1, 1, 0.1f), 4));
        bar.AddThemeStyleboxOverride("fill", Card(fill, 4));
        into.AddChild(bar);
        return (text, bar);
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

    // The standing told: the crest, the level, the week, the message; the editor kept to the leader, filled with what is.
    private void ShowStanding(GuildView? guild, Guid me)
    {
        GuildStanding? s = guild?.Standing;
        _standing.Visible = s is not null;
        bool lead = guild is not null && GuildPage.CanLead(guild, me);
        _edit.Visible = lead;
        if (!lead)
            _editor.Visible = false;
        if (s is null)
            return;
        _crest.Crest = s.Crest;
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

    public GuildCrest? CrestShown => _standing.Visible ? _crest.Crest : null;

    public bool CanEdit => _edit.Visible;

    /// <summary>The leader's editor opened, as the pencil does (for the self-test).</summary>
    public void OpenEditor() => _editor.Visible = _edit.Visible;

    // A name and its button in the accent: to found a guild, or to invite someone.
    private static (HBoxContainer Row, LineEdit Name) NameRow(VBoxContainer into, string placeholder, string text, string icon, Action pressed)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var name = new LineEdit { PlaceholderText = placeholder, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 44), MaxLength = 20 };
        name.TextSubmitted += _ => pressed();
        row.AddChild(name);
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(170, 44), ExpandIcon = true, IconAlignment = HorizontalAlignment.Left, Icon = GD.Load<Texture2D>($"res://assets/icons/guild/{icon}.svg") };
        b.AddThemeConstantOverride("icon_max_width", 22);
        foreach (string colour in new[] { "font_color", "font_hover_color", "font_pressed_color", "icon_normal_color", "icon_hover_color", "icon_pressed_color" })
            b.AddThemeColorOverride(colour, new Color("1f1f1f"));
        b.AddThemeStyleboxOverride("normal", Card(FighterView.PlayerColour.Darkened(0.12f), 6));
        b.AddThemeStyleboxOverride("hover", Card(FighterView.PlayerColour, 6));
        b.AddThemeStyleboxOverride("pressed", Card(FighterView.PlayerColour.Darkened(0.25f), 6));
        b.Pressed += pressed;
        row.AddChild(b);
        into.AddChild(row);
        return (row, name);
    }

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
        Lines.Clear();
        Icons.Clear();
        foreach (Node n in _rows.GetChildren().Concat(_columns.GetChildren()))
        {
            n.GetParent().RemoveChild(n);
            n.QueueFree();
        }
        _inviteName.Text = "";
        _foundName.Text = "";
        GuildName = guild?.Name ?? "";
        _title.Text = guild is null ? _texts["guild.title"] : $"{_texts["guild.title"]} · {guild.Name}";
        _none.Visible = guild is null;
        _none.Text = _texts["guild.none", GuildRules.Price];
        _found.Visible = guild is null && FoundingOffered;
        _invite.Visible = guild is not null && GuildPage.CanInvite(guild, me);
        _columns.Visible = guild is not null;
        _leave.Visible = guild is not null;
        _room.Text = guild is null ? _texts["guild.found.title", GuildRules.Price] : _texts["guild.room", GuildPage.Room(guild).Count, GuildPage.Room(guild).Most];
        ShowStanding(guild, me);
        if (guild is null)
            return;
        Heading(StatusWidth, "");
        Heading(0, _texts["guild.col.name"]);
        Heading(RankWidth, _texts["guild.col.rank"]);
        Heading(LevelWidth, _texts["guild.col.level"]);
        Heading(ClassWidth, _texts["guild.col.class"]);
        Heading(WhereWidth, _texts["guild.col.where"]);
        Heading(ActionsWidth, "");
        foreach (GuildMember m in guild.Members)
            Row(guild, me, m, now);
    }

    private string Where(GuildMember m, DateTimeOffset now) =>
        m.Zone is string zone ? _texts.ZoneName(zone, GameData.Embedded)
        : GuildPage.Ago(m.LastSeen, now) is (int count, string unit) ? _texts["guild.seen." + unit, count]
        : _texts["guild.seen.never"];

    private void Heading(int width, string text)
    {
        var label = new Label { Text = text, CustomMinimumSize = new Vector2(width, 0), SizeFlagsHorizontal = width == 0 ? Control.SizeFlags.ExpandFill : Control.SizeFlags.Fill, Modulate = new Color(1, 1, 1, 0.6f), ClipText = true };
        label.AddThemeFontSizeOverride("font_size", 13);
        _columns.AddChild(label);
    }

    // One member's card: its status, name, rank, level, class and where it is; the icons its rank allows on the right.
    private void Row(GuildView guild, Guid me, GuildMember m, DateTimeOffset now)
    {
        bool inPlay = m.Zone is not null;
        string cls = m.Class is string id ? GameData.Embedded.Class(id)?.Name.In(_texts.Lang) ?? id : "—";
        string line = _texts[inPlay ? "guild.online" : "guild.offline", m.Name, m.Level, _texts["guild.rank." + m.Rank], Where(m, now)];
        Lines.Add(line);
        var card = new PanelContainer { TooltipText = line };
        card.AddThemeStyleboxOverride("panel", Card(m.Id == me ? new Color(0.2f, 0.22f, 0.17f) : inPlay ? new Color(0.17f, 0.18f, 0.22f) : new Color(0.13f, 0.13f, 0.15f), 6));
        _rows.AddChild(card);
        var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 48) };
        row.AddThemeConstantOverride("separation", 0);
        card.AddChild(row);
        var status = new CenterContainer { CustomMinimumSize = new Vector2(StatusWidth, 0) };
        var dot = new Panel { CustomMinimumSize = new Vector2(10, 10), MouseFilter = Control.MouseFilterEnum.Ignore };
        dot.AddThemeStyleboxOverride("panel", Card(inPlay ? Online : Away, 5));
        status.AddChild(dot);
        row.AddChild(status);
        var name = new Label { Text = m.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center, ClipText = true };
        name.AddThemeFontSizeOverride("font_size", 17);
        name.AddThemeColorOverride("font_color", inPlay ? FighterView.PlayerColour : new Color(1, 1, 1, 0.5f));
        row.AddChild(name);
        var rank = Cell(_texts["guild.rank." + m.Rank], RankWidth, true);
        if (m.Rank == GuildRank.Leader)
            rank.AddThemeColorOverride("font_color", Gold);
        row.AddChild(rank);
        var level = new CenterContainer { CustomMinimumSize = new Vector2(LevelWidth, 0) };
        var badge = new Label { Text = m.Level.ToString(System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment = HorizontalAlignment.Center, CustomMinimumSize = new Vector2(38, 0), Modulate = inPlay ? Colors.White : new Color(1, 1, 1, 0.5f) };
        badge.AddThemeStyleboxOverride("normal", Card(Gold, 4));
        badge.AddThemeColorOverride("font_color", new Color("2a1d06"));
        level.AddChild(badge);
        row.AddChild(level);
        row.AddChild(Cell(cls, ClassWidth, inPlay));
        row.AddChild(Cell(Where(m, now), WhereWidth, inPlay));
        var actions = new HBoxContainer { CustomMinimumSize = new Vector2(ActionsWidth, 0), Alignment = BoxContainer.AlignmentMode.End };
        actions.AddThemeConstantOverride("separation", 2);
        row.AddChild(actions);
        var icons = Icons[m.Name] = [];
        GuildActions can = GuildPage.Actions(guild, me, m);
        if (can.Promote)
            Icon(actions, icons, "promote", _texts["guild.promote", m.Name], () => RankPressed?.Invoke(m.Id, GuildRank.Officer));
        if (can.Demote)
            Icon(actions, icons, "demote", _texts["guild.demote", m.Name], () => RankPressed?.Invoke(m.Id, GuildRank.Member));
        if (can.HandOver)
            Icon(actions, icons, "hand-over", _texts["guild.hand-over", m.Name], () => RankPressed?.Invoke(m.Id, GuildRank.Leader));
        if (can.Kick)
            Icon(actions, icons, "kick", _texts["guild.kick", m.Name], () => KickPressed?.Invoke(m.Id));
    }

    private static Label Cell(string text, int width, bool lit) =>
        new() { Text = text, CustomMinimumSize = new Vector2(width, 0), VerticalAlignment = VerticalAlignment.Center, ClipText = true, Modulate = lit ? new Color(1, 1, 1, 0.9f) : new Color(1, 1, 1, 0.45f) };

    // A discreet icon, lit under the pointer; what it does in its tooltip.
    private static void Icon(HBoxContainer into, List<string> shown, string icon, string tip, Action pressed)
    {
        var b = new Button
        {
            Icon = GD.Load<Texture2D>($"res://assets/icons/guild/{icon}.svg"),
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
        b.AddThemeColorOverride("icon_hover_color", icon == "kick" ? new Color("ff8a75") : icon == "hand-over" ? Gold : FighterView.PlayerColour);
        b.Pressed += pressed;
        into.AddChild(b);
        shown.Add(icon);
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
