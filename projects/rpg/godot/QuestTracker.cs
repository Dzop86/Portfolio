using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The quest followed (sprint 106, D75), on the left of the screen: its name, its objectives (those reached ticked) and
/// where to go. Since sprint 165 (D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §1) it folds to its header, which
/// still counts the objectives reached, and stays as the last game left it; clicks go through all but the header to the
/// map; hidden when no quest is followed.
/// </summary>
public partial class QuestTracker : CanvasLayer
{
    private PanelContainer _panel = null!;
    private HudHeader _header = null!;
    private VBoxContainer _body = null!;
    private Texts? _texts;
    private JournalQuest? _quest;

    /// <summary>What it shows (for the self-test): the name, then each line.</summary>
    public string TitleText { get; private set; } = "";
    public List<string> Lines { get; } = [];

    /// <summary>Whether it is folded to its header, and what the header counts then (for the self-test).</summary>
    public bool Folded => _header.Folded;
    public string Count => _header.Count;
    public bool BodyShown => _body.Visible;

    /// <summary>The panel's width and its header's (for the self-test): the panel holds its header, folded too.</summary>
    public float PanelWidth => _panel.Size.X;
    public float HeaderWidth => _header.GetCombinedMinimumSize().X;

    public override void _Ready()
    {
        Layer = 5;
        _panel = new PanelContainer { Position = new Vector2(16, 96), ThemeTypeVariation = OsmoseTheme.HudPanel, MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        // Its header from the panel's top edge.
        _panel.AddThemeStyleboxOverride("panel", HudHeader.Panel(0, 8));
        AddChild(_panel);
        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 2);
        _panel.AddChild(column);
        _header = new HudHeader();
        _header.Build(GD.Load<Texture2D>("res://assets/icons/bar/journal.svg"));
        _header.Pressed += () => Fold(!_header.Folded);
        column.AddChild(_header);
        _body = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _body.AddThemeConstantOverride("separation", 4);
        column.AddChild(_body);
    }

    /// <summary>The quest followed as the journal shows it, or none.</summary>
    public void Show(JournalQuest? quest, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        (_quest, _texts) = (quest, texts);
        foreach (Node child in _body.GetChildren())
            child.QueueFree();
        Lines.Clear();
        TitleText = quest?.Name ?? "";
        _panel.Visible = quest is not null;
        _header.Title = texts["tracker.title"].ToUpperInvariant();
        if (quest is null)
            return;
        var name = new Label { Text = quest.Name, MouseFilter = Control.MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(296, 0) };
        name.AddThemeFontOverride("font", OsmoseTheme.Bold());
        name.AddThemeFontSizeOverride("font_size", 16);
        name.AddThemeColorOverride("font_color", new Color(UiStyle.TextStrong));
        _body.AddChild(name);
        // Reached: ticked, in the dim grey, behind; still to do: a hollow ring, in the strong text.
        foreach ((string line, bool reached) in quest.Objectives)
            Add(line, reached ? "res://assets/icons/relief/check.svg" : "res://assets/ui/todo.svg", reached ? UiStyle.TextDim : UiStyle.TextStrong);
        if (quest.Ready)
            Add(texts["journal.ready", quest.Giver], null, UiStyle.Cost);
        if (quest.Where is string where)
            Add(texts["journal.where", where], null, UiStyle.Cost);
        Fold(HudFile.Settings.TrackerFolded, keep: false);
    }

    /// <summary>Folds it to its header or unfolds it, kept for the next game.</summary>
    public void Fold(bool folded, bool keep = true)
    {
        Texts texts = _texts ?? new Texts("fr");
        _header.Show(folded, texts["tracker.fold"], texts["tracker.unfold"]);
        _header.Count = folded && _quest is not null ? HudLayout.Reached(_quest) : "";
        _body.Visible = !folded;
        _panel.AddThemeStyleboxOverride("panel", HudHeader.Panel(0, folded ? 0 : 8));
        _panel.CustomMinimumSize = new Vector2(folded ? 0 : 320, 0);
        // Shrinks back to its header or its lines.
        _panel.Size = Vector2.Zero;
        if (keep)
            HudFile.Save(HudFile.Settings with { TrackerFolded = folded });
    }

    private void Add(string text, string? mark, string colour)
    {
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 6);
        if (mark is not null)
            row.AddChild(new TextureRect { Texture = GD.Load<Texture2D>(mark), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, CustomMinimumSize = new Vector2(16, 16), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, SizeFlagsVertical = Control.SizeFlags.ShrinkBegin, MouseFilter = Control.MouseFilterEnum.Ignore });
        var l = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(mark is null ? 296 : 274, 0) };
        l.AddThemeColorOverride("font_color", new Color(colour));
        l.AddThemeFontSizeOverride("font_size", 14);
        row.AddChild(l);
        _body.AddChild(row);
        Lines.Add(text);
    }
}
