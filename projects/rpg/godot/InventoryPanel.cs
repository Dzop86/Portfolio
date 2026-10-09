using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The inventory and equipment screen, over the town: the fourteen slots on the left (a click takes
/// off), the pages of the inventory on the right (a click puts on), each item's card on hover,
/// "Save" and "Close". It only draws <see cref="InventoryEditor"/>.
/// </summary>
public partial class InventoryPanel : CanvasLayer
{
    private readonly Dictionary<Slot, Button> _slots = [];
    private readonly List<Button> _pages = [];
    private VBoxContainer _list = null!;
    private Label _title = null!, _message = null!;
    private Button _save = null!, _close = null!;

    public InventoryEditor Editor { get; private set; } = null!;
    public Texts Texts { get; private set; } = new("en");
    public InventoryPage Page { get; private set; }

    public event Action<IReadOnlyDictionary<Slot, string>>? SavePressed;
    public event Action? ClosePressed;

    public void Build(InventoryEditor editor, Texts texts)
    {
        Editor = editor;
        Texts = texts;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -480, OffsetRight = 480, OffsetTop = -310, OffsetBottom = 310 };
        panel.AddThemeStyleboxOverride("panel", Background());
        AddChild(panel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 18);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        _title = new Label();
        _title.AddThemeFontSizeOverride("font_size", 24);
        column.AddChild(_title);
        var halves = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        halves.AddThemeConstantOverride("separation", 20);
        column.AddChild(halves);

        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        halves.AddChild(grid);
        foreach (Slot slot in Enum.GetValues<Slot>())
        {
            var b = new Button { CustomMinimumSize = new Vector2(176, 52), ClipText = true, Alignment = HorizontalAlignment.Left };
            b.AddThemeFontSizeOverride("font_size", 13);
            b.Pressed += () =>
            {
                Editor.TakeOff(slot);
                Refresh();
            };
            grid.AddChild(b);
            _slots[slot] = b;
        }

        var right = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 6);
        halves.AddChild(right);
        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 4);
        right.AddChild(tabs);
        foreach (InventoryPage page in Enum.GetValues<InventoryPage>())
        {
            var tab = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true };
            tab.AddThemeFontSizeOverride("font_size", 12);
            tab.Pressed += () => ShowPage(page);
            tabs.AddChild(tab);
            _pages.Add(tab);
        }
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        right.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_list);

        _message = new Label();
        column.AddChild(_message);
        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        buttons.AddThemeConstantOverride("separation", 10);
        column.AddChild(buttons);
        _save = new Button { CustomMinimumSize = new Vector2(150, 44) };
        _save.Pressed += () => SavePressed?.Invoke(Editor.Draft.Worn!);
        buttons.AddChild(_save);
        _close = new Button { CustomMinimumSize = new Vector2(150, 44) };
        _close.Pressed += () => ClosePressed?.Invoke();
        buttons.AddChild(_close);
        Refresh();
    }

    /// <summary>The panels over the town: opaque, the interface's dark grey, outlined in the accent colour.</summary>
    public static StyleBoxFlat Background() => new()
    {
        BgColor = new Color("1f1f1f"),
        BorderColor = FighterView.PlayerColour,
        BorderWidthLeft = 1,
        BorderWidthRight = 1,
        BorderWidthTop = 1,
        BorderWidthBottom = 1,
        CornerRadiusTopLeft = 6,
        CornerRadiusTopRight = 6,
        CornerRadiusBottomLeft = 6,
        CornerRadiusBottomRight = 6,
    };

    public void ShowPage(InventoryPage page)
    {
        Page = page;
        Refresh();
    }

    /// <summary>After a save: a new draft from what the server kept, and a message.</summary>
    public void Saved(Hero hero, IReadOnlyList<ItemCount> owned, string message)
    {
        Editor = new InventoryEditor(hero, owned, Editor.Data);
        _message.Text = message;
        Refresh();
    }

    public void ShowMessage(string text) => _message.Text = text;

    public void Refresh()
    {
        _title.Text = Texts["inventory.title"];
        foreach ((Slot slot, Button b) in _slots)
        {
            Item? worn = Editor.Worn.TryGetValue(slot, out string? id) ? Editor.Data.Items[id] : null;
            b.Text = $"{Texts["slot." + slot]}\n{worn?.Name.In(Texts.Lang) ?? "—"}";
            b.TooltipText = worn is null ? "" : Texts.ItemCard(worn);
            b.Disabled = worn is null;
        }
        InventoryPage[] pages = Enum.GetValues<InventoryPage>();
        for (int i = 0; i < _pages.Count; i++)
        {
            _pages[i].Text = Texts["page." + pages[i]];
            _pages[i].SetPressedNoSignal(pages[i] == Page);
        }
        foreach (Node child in _list.GetChildren())
        {
            _list.RemoveChild(child);
            child.QueueFree();
        }
        IReadOnlyList<(Item Item, int Count, int Free)> items = Editor.Page(Page);
        if (items.Count == 0)
            _list.AddChild(new Label { Text = Texts["inventory.empty"] });
        foreach ((Item item, int count, int free) in items)
        {
            var b = new Button { Text = $"{item.Name.In(Texts.Lang)}  ×{count}" + (free < count ? " · " + Texts["inventory.free", free] : ""), TooltipText = Texts.ItemCard(item), Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 44) };
            b.Disabled = Editor.WhyNot(item) is not null;
            b.Pressed += () =>
            {
                Editor.Put(item);
                Refresh();
            };
            _list.AddChild(b);
        }
        _save.Text = Texts["points.save"];
        _save.Disabled = !Editor.Changed;
        _close.Text = Texts["points.close"];
    }

    /// <summary>What each slot shows, and the items listed on the page, as the self-test reads them.</summary>
    public IEnumerable<string> SlotsText => _slots.Values.Select(b => b.Text);

    public IEnumerable<string> ListText => _list.GetChildren().OfType<Button>().Select(b => b.Text);

    public string MessageText => _message.Text;
}
