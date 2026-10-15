using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// Rose's stall (sprint 97), over the village: the Éclats owned, what she sells (buy one, or ten) and what
/// she would buy of the character's (sell one, or all of it). The server makes each deal.
/// </summary>
public partial class ShopPanel : CanvasLayer
{
    private Label _title = null!, _message = null!;
    private VBoxContainer _buy = null!, _sell = null!;
    private Button _close = null!;
    private readonly List<(StallRow Row, Button Button)> _buyButtons = [], _sellButtons = [];

    public Texts Texts { get; private set; } = new("en");

    /// <summary>The item, how many, and whether it is bought (else sold).</summary>
    public event Action<string, int, bool>? DealPressed;

    public event Action? ClosePressed;

    public void Build(Stall stall, Texts texts)
    {
        Texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -460, OffsetRight = 460, OffsetTop = -280, OffsetBottom = 280 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "shop");
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
        var halves = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        halves.AddThemeConstantOverride("separation", 16);
        column.AddChild(halves);
        _buy = Half(halves);
        _sell = Half(halves);
        _message = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", FighterView.PlayerColour);
        column.AddChild(_message);
        _close = new Button { CustomMinimumSize = new Vector2(160, 44), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        _close.Pressed += () => ClosePressed?.Invoke();
        column.AddChild(_close);
        Show(stall);
    }

    private static VBoxContainer Half(HBoxContainer halves)
    {
        var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        halves.AddChild(scroll);
        var rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        rows.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(rows);
        return rows;
    }

    /// <summary>The stall again, after a deal (the Éclats and the inventory changed).</summary>
    public void Show(Stall stall)
    {
        _title.Text = stall.Title;
        _close.Text = Texts["craft.close"];
        Fill(_buy, _buyButtons, Texts["shop.buy"], stall.ToBuy, buying: true);
        Fill(_sell, _sellButtons, Texts["shop.sell"], stall.ToSell, buying: false);
        if (_sellButtons.Count == 0)
            _sell.AddChild(new Label { Text = Texts["shop.nothing"], Modulate = new Color(1, 1, 1, 0.6f) });
    }

    private void Fill(VBoxContainer rows, List<(StallRow, Button)> buttons, string heading, IReadOnlyList<StallRow> list, bool buying)
    {
        foreach (Node n in rows.GetChildren())
            n.QueueFree();
        buttons.Clear();
        var head = new Label { Text = heading };
        head.AddThemeFontSizeOverride("font_size", 18);
        rows.AddChild(head);
        foreach (StallRow row in list)
        {
            var line = new HBoxContainer();
            line.AddThemeConstantOverride("separation", 6);
            rows.AddChild(line);
            line.AddChild(new Label { Text = row.Title, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true, TooltipText = row.Refusal ?? "", MouseFilter = Control.MouseFilterEnum.Pass });
            var one = new Button { Text = "1", CustomMinimumSize = new Vector2(44, 44), Disabled = row.Most < 1 };
            one.Pressed += () => DealPressed?.Invoke(row.Item.Id, 1, buying);
            line.AddChild(one);
            // Ten at once to buy, all of it to sell.
            int many = buying ? Math.Min(10, row.Most) : row.Most;
            var more = new Button { Text = buying ? "10" : Texts["craft.make-all", row.Most], CustomMinimumSize = new Vector2(64, 44), Disabled = buying ? row.Most < 10 : row.Most < 2 };
            more.Pressed += () => DealPressed?.Invoke(row.Item.Id, many, buying);
            line.AddChild(more);
            buttons.Add((row, one));
        }
    }

    public void ShowMessage(string text) => _message.Text = text;

    /// <summary>For the self-test: the title and each line of both halves with whether a deal is possible.</summary>
    public string TitleText => _title.Text;

    public IEnumerable<(string Item, bool Can)> Buying => _buyButtons.Select(b => (b.Row.Item.Id, !b.Button.Disabled));

    public IEnumerable<(string Item, bool Can)> Selling => _sellButtons.Select(b => (b.Row.Item.Id, !b.Button.Disabled));
}
