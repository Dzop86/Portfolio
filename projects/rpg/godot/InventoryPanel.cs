using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The inventory and equipment screen, over the town (sprint 65): the hero in 3D, turning, its fourteen
/// slots around it (a click takes off); on the right a search field, the pages of the inventory and a
/// grid of item tiles with their counts (a click puts on); the card of the item under the mouse;
/// "Save" and "Close". It only draws <see cref="InventoryEditor"/>.
/// </summary>
public partial class InventoryPanel : CanvasLayer
{
    // Seven slots on each side of the hero, head to feet on the left, hands and companions on the right.
    private static readonly Slot[] Left = [Slot.Hat, Slot.Amulet, Slot.Cape, Slot.Shoulders, Slot.Chest, Slot.Belt, Slot.Boots];
    private static readonly Slot[] Right = [Slot.Ring1, Slot.Ring2, Slot.OneHanded, Slot.TwoHanded, Slot.Shield, Slot.Pet, Slot.Mount];
    private const float TileSize = 62;

    private readonly Dictionary<Slot, (Button Tile, ItemIcon Icon)> _slots = [];
    private readonly List<Button> _pages = [];
    private GridContainer _grid = null!;
    private LineEdit _search = null!;
    private Label _title = null!, _message = null!, _details = null!, _empty = null!;
    private Button _save = null!, _close = null!;
    private OptionButton _sort = null!;
    private Label _sortLabel = null!;
    private Control? _picker;
    private Slot? _pickerSlot;

    public InventoryEditor Editor { get; private set; } = null!;
    public Texts Texts { get; private set; } = new("en");
    public InventoryPage Page { get; private set; }

    /// <summary>How the page is ordered (sprint 181, D92).</summary>
    public InventorySort Sort { get; private set; }

    public event Action<IReadOnlyDictionary<Slot, string>>? SavePressed;

    /// <summary>Food clicked, to eat (sprint 101).</summary>
    public event Action<Item>? EatPressed;
    public event Action? ClosePressed;

    public void Build(InventoryEditor editor, Texts texts)
    {
        // Over the mini-map (layer 6) like the other windows (sprint 181: the mini-map covered the inventory's tabs).
        Layer = 15;
        Editor = editor;
        Texts = texts;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -520, OffsetRight = 520, OffsetTop = -290, OffsetBottom = 300 };
        panel.AddThemeStyleboxOverride("panel", Background());
        AddChild(panel);
        WindowDrag.Make(panel, "inventory");
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        _title = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _title.AddThemeFontSizeOverride("font_size", 24);
        // The title, and the Éclats owned on its right (Charles, sprint 103).
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        column.AddChild(header);
        header.AddChild(_title);
        header.AddChild(new ShardIcon { CustomMinimumSize = new Vector2(24, 24), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        _shards = new Label { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        _shards.AddThemeFontSizeOverride("font_size", 20);
        header.AddChild(_shards);
        var halves = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        halves.AddThemeConstantOverride("separation", 18);
        column.AddChild(halves);

        // The hero between its two columns of slots, on a darker stage.
        var stage = new PanelContainer { CustomMinimumSize = new Vector2(470, 0) };
        stage.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("181818"), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 10, ContentMarginBottom = 10 });
        halves.AddChild(stage);
        var around = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        around.AddThemeConstantOverride("separation", 10);
        stage.AddChild(around);
        around.AddChild(SlotColumn(Left));
        Portrait hero = Portrait.Make(new Vector2(300, 440), turning: true, distance: 4.2f);
        hero.Show(editor.Hero);
        around.AddChild(hero);
        around.AddChild(SlotColumn(Right));

        var right = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 8);
        halves.AddChild(right);
        _search = new LineEdit { CustomMinimumSize = new Vector2(0, 44), ClearButtonEnabled = true };
        _search.TextChanged += _ => Refresh();
        right.AddChild(_search);
        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 4);
        right.AddChild(tabs);
        foreach (InventoryPage page in Enum.GetValues<InventoryPage>())
        {
            // Each tab as wide as its name (sprint 181, D92: « Équipement » and « Consommables » were cut off).
            var tab = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            tab.AddThemeFontSizeOverride("font_size", 13);
            tab.Pressed += () => ShowPage(page);
            tabs.AddChild(tab);
            _pages.Add(tab);
        }
        // The order of the page: by kind, set, level, slot or name (sprint 181, D92).
        var sortRow = new HBoxContainer();
        sortRow.AddThemeConstantOverride("separation", 8);
        right.AddChild(sortRow);
        _sortLabel = new Label { VerticalAlignment = VerticalAlignment.Center };
        _sortLabel.AddThemeFontSizeOverride("font_size", 14);
        sortRow.AddChild(_sortLabel);
        _sort = new OptionButton { CustomMinimumSize = new Vector2(180, 44) };
        foreach (InventorySort sort in Enum.GetValues<InventorySort>())
            _sort.AddItem(sort.ToString(), (int)sort);
        _sort.ItemSelected += i => SortBy((InventorySort)_sort.GetItemId((int)i));
        sortRow.AddChild(_sort);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        right.AddChild(scroll);
        var holder = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(holder);
        _grid = new GridContainer { Columns = 7 };
        _grid.AddThemeConstantOverride("h_separation", 6);
        _grid.AddThemeConstantOverride("v_separation", 6);
        holder.AddChild(_grid);
        _empty = new Label { Visible = false };
        holder.AddChild(_empty);
        var card = new PanelContainer { CustomMinimumSize = new Vector2(0, 92) };
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("262626"), CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6 });
        right.AddChild(card);
        _details = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, VerticalAlignment = VerticalAlignment.Center };
        _details.AddThemeFontSizeOverride("font_size", 14);
        card.AddChild(_details);

        var bottom = new HBoxContainer();
        bottom.AddThemeConstantOverride("separation", 10);
        column.AddChild(bottom);
        _message = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        bottom.AddChild(_message);
        _save = new Button { CustomMinimumSize = new Vector2(150, 44) };
        _save.Pressed += () => SavePressed?.Invoke(Editor.Draft.Worn!);
        bottom.AddChild(_save);
        _close = new Button { CustomMinimumSize = new Vector2(150, 44) };
        _close.Pressed += () => ClosePressed?.Invoke();
        bottom.AddChild(_close);
        Refresh();
    }

    private VBoxContainer SlotColumn(Slot[] slots)
    {
        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        col.AddThemeConstantOverride("separation", 6);
        foreach (Slot slot in slots)
        {
            (Button tile, ItemIcon icon) = ItemIcon.Tile(TileSize);
            // What can go there, offered (sprint 181, D92), where a click took it off.
            tile.Pressed += () => OpenPicker(slot);
            tile.MouseEntered += () => _details.Text = SlotText(slot).Replace("\n", " · ", StringComparison.Ordinal) + (Worn(slot) is Item it ? "\n" + Texts.ItemCard(it) : "");
            col.AddChild(tile);
            _slots[slot] = (tile, icon);
        }
        return col;
    }

    /// <summary>
    /// The panels over the town: opaque, the interface's dark grey; framed in lightened chocolate with a shadow since
    /// sprint 157 (D84, the theme's window), where it was outlined in the accent colour.
    /// </summary>
    public static StyleBoxFlat Background() => OsmoseTheme.Window(0);

    /// <summary>An item put on from its right click (sprint 117), as its left click does.</summary>
    public void Put(Item item)
    {
        Editor.Put(item);
        Refresh();
    }

    /// <summary>An item taken off from its right click (sprint 117): the first slot that wears it.</summary>
    public void TakeOff(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Editor.Worn.Where(w => w.Value == item.Id).Select(w => (Slot?)w.Key).FirstOrDefault() is Slot slot)
            Editor.TakeOff(slot);
        Refresh();
    }

    public void SortBy(InventorySort sort)
    {
        Sort = sort;
        Refresh();
    }

    /// <summary>
    /// The items of the inventory that go in a slot, by its side (sprint 181, D92: « quand je clique sur l'item équipé,
    /// qu'il n'y ait rien ou quelque chose, ça me propose les équipements dans mon inventaire »): a click puts one there;
    /// « Take off » when something is worn; a click elsewhere closes it.
    /// </summary>
    public void OpenPicker(Slot slot)
    {
        ClosePicker();
        _pickerSlot = slot;
        var shade = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        shade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        shade.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true })
                ClosePicker();
        };
        AddChild(shade);
        _picker = shade;
        var box = new PanelContainer();
        box.AddThemeStyleboxOverride("panel", OsmoseTheme.Window(0));
        shade.AddChild(box);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 10);
        box.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        var title = new Label { Text = Texts["slot.offer", Texts["slot." + slot]] };
        title.AddThemeFontSizeOverride("font_size", 16);
        column.AddChild(title);
        IReadOnlyList<Item> offered = Editor.ReadOnly ? [] : Editor.For(slot);
        if (offered.Count == 0)
            column.AddChild(new Label { Text = Texts["slot.none"] });
        var grid = new GridContainer { Columns = Math.Clamp(offered.Count, 1, 5) };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        column.AddChild(grid);
        foreach (Item item in offered)
        {
            (Button tile, ItemIcon icon) = ItemIcon.Tile(TileSize);
            icon.Item = item;
            icon.Faint = Editor.WhyNot(item) is not null;
            tile.Disabled = Editor.WhyNot(item) is not null;
            tile.TooltipText = Texts.ItemCard(item);
            tile.MouseEntered += () => _details.Text = Texts.ItemCard(item);
            tile.Pressed += () => Choose(item);
            grid.AddChild(tile);
        }
        if (Worn(slot) is not null && !Editor.ReadOnly)
        {
            var off = new Button { Text = Texts["slot.take-off"], CustomMinimumSize = new Vector2(0, 44) };
            off.Pressed += () =>
            {
                Editor.TakeOff(slot);
                ClosePicker();
                Refresh();
            };
            column.AddChild(off);
        }
        // By the slot, inside the game's window.
        Rect2 at = _slots[slot].Tile.GetGlobalRect();
        Vector2 size = box.GetCombinedMinimumSize();
        Vector2 screen = GetViewport().GetVisibleRect().Size;
        bool left = Array.IndexOf(Left, slot) >= 0;
        float x = left ? at.End.X + 8 : at.Position.X - size.X - 8;
        box.Position = new Vector2(Math.Clamp(x, 0, Math.Max(0, screen.X - size.X)), Math.Clamp(at.Position.Y, 0, Math.Max(0, screen.Y - size.Y)));
    }

    /// <summary>The item clicked in the slot's offer, put in that very slot.</summary>
    public void Choose(Item item)
    {
        if (_pickerSlot is Slot slot)
            Editor.PutIn(slot, item);
        ClosePicker();
        Refresh();
    }

    public void ClosePicker()
    {
        _picker?.QueueFree();
        _picker = null;
        _pickerSlot = null;
    }

    /// <summary>What the open offer lists (the self-test reads it), or null when none is open.</summary>
    public IReadOnlyList<string>? PickerItems => _pickerSlot is Slot s ? [.. Editor.For(s).Select(i => i.Id)] : null;

    /// <summary>Whether every tab shows its whole name (none narrower than its text needs).</summary>
    public bool TabsReadable => _pages.All(t =>
        t.Size.X + 0.5f >= t.GetThemeFont("font").GetStringSize(t.Text, HorizontalAlignment.Left, -1, t.GetThemeFontSize("font_size")).X
            + t.GetThemeStylebox("normal").GetMinimumSize().X);

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

    /// <summary>The kind of item a slot takes: a ring for either ring slot, otherwise its own.</summary>
    private static ItemSlot KindOf(Slot slot) => slot is Slot.Ring1 or Slot.Ring2 ? ItemSlot.Ring : Enum.Parse<ItemSlot>(slot.ToString());

    private Item? Worn(Slot slot) => Editor.Worn.TryGetValue(slot, out string? id) ? Editor.Data.Items[id] : null;

    private string SlotText(Slot slot) => $"{Texts["slot." + slot]}\n{Worn(slot)?.Name.In(Texts.Lang) ?? "—"}";

    /// <summary>The items of the page that the search finds, with their counts.</summary>
    private IReadOnlyList<(Item Item, int Count, int Free)> Shown() =>
        [.. Editor.Page(Page, Sort, Texts.Lang).Where(i => ItemStyle.Matches(i.Item, _search.Text, Texts.Lang))];

    private Label _shards = null!;
    private long _owned;

    /// <summary>The Éclats the character owns, shown by the title.</summary>
    public void SetShards(long shards)
    {
        _owned = shards;
        _shards.Text = Texts["inventory.shards", shards];
    }

    public string ShardsText => _shards.Text;

    public void Refresh()
    {
        _title.Text = Texts["inventory.title"];
        SetShards(_owned);
        _search.PlaceholderText = Texts["inventory.search"];
        foreach ((Slot slot, (Button tile, ItemIcon icon)) in _slots)
        {
            Item? worn = Worn(slot);
            icon.Item = worn;
            icon.Slot = KindOf(slot);
            icon.Faint = worn is null;
            icon.Count = 1;
            icon.QueueRedraw();
            tile.TooltipText = SlotText(slot) + (worn is null ? "" : "\n" + ItemMenu.Tooltip(worn, Texts));
            // Empty or not, a slot offers what can go in it (sprint 181); in a fight it only shows.
            tile.Disabled = Editor.ReadOnly && worn is null;
        }
        InventoryPage[] pages = Enum.GetValues<InventoryPage>();
        for (int i = 0; i < _pages.Count; i++)
        {
            _pages[i].Text = Texts["page." + pages[i]];
            _pages[i].SetPressedNoSignal(pages[i] == Page);
            // Room by the length of the name: the long ones get more of the row.
            _pages[i].SizeFlagsStretchRatio = Math.Max(3, _pages[i].Text.Length);
        }
        _sortLabel.Text = Texts["inventory.sort"];
        foreach (InventorySort sort in Enum.GetValues<InventorySort>())
            _sort.SetItemText(_sort.GetItemIndex((int)sort), Texts["sort." + sort]);
        _sort.Select(_sort.GetItemIndex((int)Sort));
        foreach (Node child in _grid.GetChildren())
        {
            _grid.RemoveChild(child);
            child.QueueFree();
        }
        IReadOnlyList<(Item Item, int Count, int Free)> items = Shown();
        _empty.Visible = items.Count == 0;
        _empty.Text = Texts["inventory.empty"];
        foreach ((Item item, int count, int free) in items)
        {
            (Button tile, ItemIcon icon) = ItemIcon.Tile(TileSize);
            icon.Item = item;
            icon.Count = count;
            string card = Texts.ItemCard(item) + (free < count ? "\n" + Texts["inventory.free", free] : "");
            tile.TooltipText = card + "\n" + Texts["item.hint"];
            // Equipment above the hero's level is drawn faint; all of it worn, dimmed; the rest cannot be worn at all.
            icon.Faint = item.Kind == ItemKind.Equipment && item.Level > Editor.Hero.Level;
            icon.Modulate = item.Kind == ItemKind.Equipment && free == 0 ? new Color(1, 1, 1, 0.5f) : Colors.White;
            // In a fight, every item stays readable (sprint 93); a click does nothing.
            tile.Disabled = !Editor.ReadOnly && Editor.WhyNot(item) is not null && !item.Usable;
            tile.MouseEntered += () => _details.Text = card;
            tile.Pressed += () =>
            {
                // Food is eaten out of a fight (sprint 101), the recall scroll read (sprint 127); equipment is put on.
                if (item.Usable && !Editor.ReadOnly)
                {
                    EatPressed?.Invoke(item);
                    return;
                }
                Editor.Put(item);
                Refresh();
            };
            _grid.AddChild(tile);
        }
        if (_details.Text.Length == 0)
            _details.Text = Texts["inventory.hint"];
        _save.Text = Texts["points.save"];
        _save.Disabled = !Editor.Changed;
        _save.Visible = !Editor.ReadOnly;
        if (Editor.ReadOnly)
            _message.Text = Texts["window.in-fight"];
        _close.Text = Texts["points.close"];
    }

    /// <summary>What each slot shows, in the order of the slots, and the items on the page, as the self-test reads them.</summary>
    public IEnumerable<string> SlotsText => Enum.GetValues<Slot>().Select(SlotText);

    public IEnumerable<string> ListText => Shown().Select(i => $"{i.Item.Name.In(Texts.Lang)}  ×{i.Count}");

    public string MessageText => _message.Text;
}
