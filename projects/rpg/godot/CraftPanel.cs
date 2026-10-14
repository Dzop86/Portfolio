using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// A trade's workshop (sprint 81), over the town: the trade's level and experience, then each recipe
/// with its ingredients as owned/needed, "Make" and "All (n)", or why not. The server makes the items.
/// </summary>
public partial class CraftPanel : CanvasLayer
{
    private Label _title = null!, _xp = null!, _message = null!;
    private VBoxContainer _rows = null!;
    private Button _close = null!;
    private Workbench _bench = null!;
    private readonly List<(RecipeRow Row, Button Make, Button All)> _buttons = [];

    public Texts Texts { get; private set; } = new("en");

    /// <summary>The item to make and how many.</summary>
    public event Action<string, int>? MakePressed;

    public event Action? ClosePressed;

    public void Build(Workbench bench, Texts texts)
    {
        Texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -300, OffsetRight = 300, OffsetTop = -260, OffsetBottom = 260 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 24);
        column.AddChild(_title);
        _xp = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1, 1, 1, 0.7f) };
        column.AddChild(_xp);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        _rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _rows.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_rows);
        _message = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", FighterView.PlayerColour);
        column.AddChild(_message);
        _close = new Button { CustomMinimumSize = new Vector2(160, 44), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        _close.Pressed += () => ClosePressed?.Invoke();
        column.AddChild(_close);
        Show(bench);
    }

    /// <summary>The workshop again, after something was made (the character's inventory and trade changed).</summary>
    public void Show(Workbench bench)
    {
        _bench = bench;
        _title.Text = bench.Title;
        _xp.Text = bench.XpText;
        _close.Text = Texts["craft.close"];
        foreach (Node n in _rows.GetChildren())
            n.QueueFree();
        _buttons.Clear();
        foreach (RecipeRow row in bench.Rows)
        {
            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 2);
            _rows.AddChild(box);
            var name = new Label { Text = row.Title };
            name.AddThemeFontSizeOverride("font_size", 18);
            box.AddChild(name);
            box.AddChild(new Label { Text = string.Join(" · ", row.Ingredients), Modulate = new Color(1, 1, 1, 0.8f) });
            var line = new HBoxContainer();
            line.AddThemeConstantOverride("separation", 8);
            box.AddChild(line);
            var make = new Button { Text = Texts["craft.make"], CustomMinimumSize = new Vector2(120, 44), Disabled = row.Most < 1 };
            make.Pressed += () => MakePressed?.Invoke(row.Recipe.Item, 1);
            line.AddChild(make);
            var all = new Button { Text = Texts["craft.make-all", row.Most], CustomMinimumSize = new Vector2(120, 44), Disabled = row.Most < 2 };
            all.Pressed += () => MakePressed?.Invoke(row.Recipe.Item, row.Most);
            line.AddChild(all);
            if (row.Refusal is string why)
                line.AddChild(new Label { Text = why, Modulate = new Color(1, 1, 1, 0.6f), VerticalAlignment = VerticalAlignment.Center });
            _buttons.Add((row, make, all));
        }
    }

    public void ShowMessage(string text) => _message.Text = text;

    /// <summary>For the self-test: the title, each recipe's title and whether "Make" is on.</summary>
    public string TitleText => _title.Text;

    public IEnumerable<(string Item, bool CanMake)> Recipes => _buttons.Select(b => (b.Row.Recipe.Item, !b.Make.Disabled));
}
