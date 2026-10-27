using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The achievements' window (sprint 155, D83): on the left the seven categories with how many are earned, the points and
/// the title worn (a list of those earned to choose from); on the right the chosen category's achievements and their progress.
/// </summary>
public partial class AchievementsPanel : CanvasLayer
{
    private VBoxContainer _categories = null!, _lines = null!;
    private Label _points = null!, _message = null!;
    private OptionButton _title = null!;
    private Texts _texts = null!;
    private IReadOnlyList<AchievementCategory> _page = [];
    private IReadOnlyList<(string Achievement, string Title)> _titles = [];

    public event Action<string?>? TitleChosen;
    public event Action? ClosePressed;

    /// <summary>The category shown and its achievements' names (for the self-test).</summary>
    public string? Shown { get; private set; }

    public List<string> Names { get; } = [];

    public void Build(Texts texts)
    {
        _texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -400, OffsetRight = 400, OffsetTop = -310, OffsetBottom = 310 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "achievements");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        panel.AddChild(column);
        var head = new HBoxContainer();
        var title = new Label { Text = texts["achievements.title"], SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 20);
        head.AddChild(title);
        _points = new Label();
        head.AddChild(_points);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44) };
        close.Pressed += () => ClosePressed?.Invoke();
        head.AddChild(close);
        column.AddChild(head);
        var wear = new HBoxContainer();
        wear.AddChild(new Label { Text = texts["achievements.wear"] });
        _title = new OptionButton { CustomMinimumSize = new Vector2(260, 44) };
        _title.ItemSelected += i => TitleChosen?.Invoke(i == 0 ? null : _titles[(int)i - 1].Achievement);
        wear.AddChild(_title);
        column.AddChild(wear);
        var body = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        column.AddChild(body);
        _categories = new VBoxContainer { CustomMinimumSize = new Vector2(220, 0) };
        body.AddChild(_categories);
        var right = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _lines = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _lines.AddThemeConstantOverride("separation", 8);
        right.AddChild(_lines);
        body.AddChild(right);
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", new Color("ffb4a2"));
        column.AddChild(_message);
    }

    public void Show(AchievementView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        _page = AchievementsPage.Categories(view, _texts);
        _titles = AchievementsPage.Titles(view, _texts);
        _points.Text = _texts["achievements.points", view.Points];
        _title.Clear();
        _title.AddItem(_texts["achievements.no-title"]);
        foreach ((_, string t) in _titles)
            _title.AddItem(t);
        int worn = _titles.ToList().FindIndex(t => t.Achievement == view.Title);
        _title.Selected = worn + 1;
        foreach (Node n in _categories.GetChildren())
            n.QueueFree();
        foreach (AchievementCategory c in _page)
        {
            var b = new Button { Text = $"{c.Name} ({c.Earned}/{c.Lines.Count})", Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 44) };
            b.Pressed += () => ShowCategory(c.Id);
            _categories.AddChild(b);
        }
        if (_page.Count > 0)
            ShowCategory(Shown is string shown && _page.Any(c => c.Id == shown) ? shown : _page[0].Id);
    }

    public void ShowCategory(string id)
    {
        foreach (Node n in _lines.GetChildren())
            n.QueueFree();
        Names.Clear();
        Shown = id;
        foreach (AchievementLine line in _page.First(c => c.Id == id).Lines)
        {
            var card = new VBoxContainer();
            var top = new HBoxContainer();
            var name = new Label { Text = line.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            name.AddThemeFontSizeOverride("font_size", 16);
            if (line.Earned)
                name.AddThemeColorOverride("font_color", FighterView.PlayerColour);
            top.AddChild(name);
            top.AddChild(new Label { Text = $"{line.Progress} · {_texts["achievements.worth", line.Points]}" });
            card.AddChild(top);
            card.AddChild(new Label { Text = line.Text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(500, 0), Modulate = new Color(1, 1, 1, line.Earned ? 1 : 0.7f) });
            if (line.Title is string t)
                card.AddChild(new Label { Text = _texts["achievements.gives", t], Modulate = new Color("d9b38c") });
            _lines.AddChild(card);
            Names.Add(line.Name);
        }
    }

    public void Tell(string text) => _message.Text = text;
}
