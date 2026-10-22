using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The party's banner (sprint 119, D74), on the right of the screen: how many of four, each member's name and level,
/// the leader said, those not in play faded; "Leave the party" under them. Hidden when not in a party. Since sprint 124,
/// the leader sends a member out with the cross by its name, and a line for each fight an ally opened, with the seconds
/// left to join it, joins it with a click (the question closed or not).
/// </summary>
public partial class PartyBanner : CanvasLayer
{
    private PanelContainer _panel = null!;
    private VBoxContainer _column = null!;
    private VBoxContainer _members = null!;
    private VBoxContainer _joins = null!;
    private Texts _texts = null!;
    private readonly List<(Guid Fight, Button Line)> _joinLines = [];

    public event Action? LeavePressed;

    /// <summary>The leader sends this member out (sprint 124).</summary>
    public event Action<Guid>? KickPressed;

    /// <summary>A fight an ally opened, to join (sprint 124).</summary>
    public event Action<Guid>? JoinPressed;

    /// <summary>The lines shown, the title first (for the self-test).</summary>
    public List<string> Lines { get; } = [];

    public void Build(Texts texts)
    {
        _texts = texts;
        Layer = 5;
        _panel = new PanelContainer { AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -300, OffsetRight = -84, OffsetTop = 120, Visible = false };
        var style = new StyleBoxFlat { BgColor = new Color(0.12f, 0.12f, 0.12f, 0.8f), ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8 };
        style.SetCornerRadiusAll(6);
        _panel.AddThemeStyleboxOverride("panel", style);
        AddChild(_panel);
        _column = new VBoxContainer();
        _column.AddThemeConstantOverride("separation", 2);
        _panel.AddChild(_column);
        _members = new VBoxContainer();
        _members.AddThemeConstantOverride("separation", 2);
        _column.AddChild(_members);
        _joins = new VBoxContainer();
        _joins.AddThemeConstantOverride("separation", 2);
        _column.AddChild(_joins);
    }

    /// <summary>The party as the server says, or none; <paramref name="me"/> leading, a cross to send each other member out.</summary>
    public void Show(PartyNews? party, Guid? me = null)
    {
        foreach (Node child in _members.GetChildren())
            child.QueueFree();
        Kicks.Clear();
        Lines.Clear();
        _panel.Visible = party is { Party: PartyNewsKind.Changed, Members.Count: > 0 };
        if (!_panel.Visible)
            return;
        IReadOnlyList<PartyMember> members = party!.Members!;
        Add(_texts["party.title", members.Count, PartyRules.Most], FighterView.PlayerColour, 16);
        foreach (PartyMember m in members)
        {
            string line = m.Map is null ? _texts["party.offline", m.Name] : _texts[m.Id == party.Leader ? "party.leader" : "party.member", m.Name, m.Level];
            Label label = Add(line, m.Map is null ? new Color(1, 1, 1, 0.5f) : Colors.White, 14);
            if (me is Guid self && self == party.Leader && m.Id != self)
            {
                // The label moves into a row with its cross.
                _members.RemoveChild(label);
                var row = new HBoxContainer();
                label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                row.AddChild(label);
                Guid member = m.Id;
                var kick = new Button { Text = "×", TooltipText = _texts["party.kick", m.Name], CustomMinimumSize = new Vector2(44, 44) };
                kick.Pressed += () => KickPressed?.Invoke(member);
                row.AddChild(kick);
                _members.AddChild(row);
                Kicks.Add(m.Name);
            }
        }
        var leave = new Button { Text = _texts["party.leave"], CustomMinimumSize = new Vector2(0, 44) };
        leave.Pressed += () => LeavePressed?.Invoke();
        _members.AddChild(leave);
        _panel.Size = Vector2.Zero;
    }

    /// <summary>The members the leader can send out (for the self-test).</summary>
    public List<string> Kicks { get; } = [];

    /// <summary>The lines of the fights to join, each with its seconds left; rebuilt only when the fights change.</summary>
    public void ShowJoins(IReadOnlyList<(Guid Fight, string Text)> joins)
    {
        ArgumentNullException.ThrowIfNull(joins);
        if (!joins.Select(j => j.Fight).SequenceEqual(_joinLines.Select(l => l.Fight)))
        {
            foreach ((_, Button line) in _joinLines)
                line.QueueFree();
            _joinLines.Clear();
            foreach ((Guid fight, _) in joins)
            {
                var line = new Button { CustomMinimumSize = new Vector2(0, 44), ClipText = true };
                line.AddThemeColorOverride("font_color", FighterView.PlayerColour);
                line.Pressed += () => JoinPressed?.Invoke(fight);
                _joins.AddChild(line);
                _joinLines.Add((fight, line));
            }
            _panel.Size = Vector2.Zero;
        }
        for (int i = 0; i < joins.Count; i++)
            _joinLines[i].Line.Text = joins[i].Text;
    }

    /// <summary>The lines of the fights to join shown (for the self-test).</summary>
    public IEnumerable<string> JoinLines => _joinLines.Select(l => l.Line.Text);

    public bool Shown => _panel.Visible;

    private Label Add(string text, Color colour, int size)
    {
        var l = new Label { Text = text, ClipText = true };
        l.AddThemeColorOverride("font_color", colour);
        l.AddThemeFontSizeOverride("font_size", size);
        _members.AddChild(l);
        Lines.Add(text);
        return l;
    }
}
