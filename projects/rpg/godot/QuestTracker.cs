using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The quest followed (sprint 106, D75), on the left of the screen: its name, its objectives (those reached in the
/// accent colour) and where to go. Clicks go through it to the map; hidden when no quest is followed.
/// </summary>
public partial class QuestTracker : CanvasLayer
{
    private PanelContainer _panel = null!;
    private VBoxContainer _column = null!;

    /// <summary>What it shows (for the self-test): the name, then each line.</summary>
    public string TitleText { get; private set; } = "";
    public List<string> Lines { get; } = [];

    public override void _Ready()
    {
        Layer = 5;
        _panel = new PanelContainer { Position = new Vector2(16, 96), CustomMinimumSize = new Vector2(320, 0), MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        var style = new StyleBoxFlat { BgColor = new Color(0.12f, 0.12f, 0.12f, 0.72f), ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8 };
        style.SetCornerRadiusAll(6);
        _panel.AddThemeStyleboxOverride("panel", style);
        AddChild(_panel);
        _column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _column.AddThemeConstantOverride("separation", 2);
        _panel.AddChild(_column);
    }

    /// <summary>The quest followed as the journal shows it, or none.</summary>
    public void Show(JournalQuest? quest, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        foreach (Node child in _column.GetChildren())
            child.QueueFree();
        Lines.Clear();
        TitleText = quest?.Name ?? "";
        _panel.Visible = quest is not null;
        if (quest is null)
            return;
        var name = new Label { Text = quest.Name, MouseFilter = Control.MouseFilterEnum.Ignore };
        name.AddThemeFontSizeOverride("font_size", 17);
        _column.AddChild(name);
        foreach ((string line, bool reached) in quest.Objectives)
            Add((reached ? "✓ " : "– ") + line, reached ? FighterView.PlayerColour : new Color(1, 1, 1, 0.9f));
        if (quest.Ready)
            Add(texts["journal.ready", quest.Giver], new Color("ffd85a"));
        if (quest.Where is string where)
            Add(texts["journal.where", where], new Color("ffd85a"));
        // Shrinks back to its lines.
        _panel.Size = Vector2.Zero;
    }

    private void Add(string text, Color colour)
    {
        var l = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(296, 0) };
        l.AddThemeColorOverride("font_color", colour);
        l.AddThemeFontSizeOverride("font_size", 14);
        _column.AddChild(l);
        Lines.Add(text);
    }
}
