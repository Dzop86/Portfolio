using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The party's banner (sprint 119, D74), on the right of the screen: how many of four, each member's name and level,
/// the leader said, those not in play faded; "Leave the party" under them. Hidden when not in a party.
/// </summary>
public partial class PartyBanner : CanvasLayer
{
    private PanelContainer _panel = null!;
    private VBoxContainer _column = null!;
    private Texts _texts = null!;

    public event Action? LeavePressed;

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
    }

    /// <summary>The party as the server says, or none.</summary>
    public void Show(PartyNews? party)
    {
        foreach (Node child in _column.GetChildren())
            child.QueueFree();
        Lines.Clear();
        _panel.Visible = party is { Party: PartyNewsKind.Changed, Members.Count: > 0 };
        if (!_panel.Visible)
            return;
        IReadOnlyList<PartyMember> members = party!.Members!;
        Add(_texts["party.title", members.Count, PartyRules.Most], FighterView.PlayerColour, 16);
        foreach (PartyMember m in members)
        {
            string line = m.Map is null ? _texts["party.offline", m.Name] : _texts[m.Id == party.Leader ? "party.leader" : "party.member", m.Name, m.Level];
            Add(line, m.Map is null ? new Color(1, 1, 1, 0.5f) : Colors.White, 14);
        }
        var leave = new Button { Text = _texts["party.leave"], CustomMinimumSize = new Vector2(0, 44) };
        leave.Pressed += () => LeavePressed?.Invoke();
        _column.AddChild(leave);
        _panel.Size = Vector2.Zero;
    }

    public bool Shown => _panel.Visible;

    private void Add(string text, Color colour, int size)
    {
        var l = new Label { Text = text, ClipText = true };
        l.AddThemeColorOverride("font_color", colour);
        l.AddThemeFontSizeOverride("font_size", size);
        _column.AddChild(l);
        Lines.Add(text);
    }
}
