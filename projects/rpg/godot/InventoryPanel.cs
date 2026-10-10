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

    public InventoryEditor Editor { get; private set; } = null!;
    public Texts Texts { get; private set; } = new("en");
    public InventoryPage Page { get; private set; }

    public event Action<IReadOnlyDictionary<Slot, string>>? SavePressed;
    public event Action? ClosePressed;

    public void Build(InventoryEditor editor, Texts texts)
    {
        Editor = editor;
        Texts = texts;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -520, OffsetRight = 520, OffsetTop = -290, OffsetBottom = 300 };
        panel.AddThemeStyleboxOverride("panel", Background());
        AddChild(panel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        _title = new Label();
        _title.AddThemeFontSizeOverride("font_size", 24);
        column.AddChild(_title);
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
            var tab = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true };
            tab.AddThemeFontSizeOverride("font_size", 12);
            tab.Pressed += () => ShowPage(page);
            tabs.AddChild(tab);
            _pages.Add(tab);
        }
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
            tile.Pressed += () =>
            {
                Editor.TakeOff(slot);
                Refresh();
            };
            tile.MouseEntered += () => _details.Text = SlotText(slot).Replace("\n", " · ", StringComparison.Ordinal) + (Worn(slot) is Item it ? "\n" + Texts.ItemCard(it) : "");
            col.AddChild(tile);
            _slots[slot] = (tile, icon);
        }
        return col;
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

    /// <summary>The kind of item a slot takes: a ring for either ring slot, otherwise its own.</summary>
    private static ItemSlot KindOf(Slot slot) => slot is Slot.Ring1 or Slot.Ring2 ? ItemSlot.Ring : Enum.Parse<ItemSlot>(slot.ToString());

    private Item? Worn(Slot slot) => Editor.Worn.TryGetValue(slot, out string? id) ? Editor.Data.Items[id] : null;

    private string SlotText(Slot slot) => $"{Texts["slot." + slot]}\n{Worn(slot)?.Name.In(Texts.Lang) ?? "—"}";

    /// <summary>The items of the page that the search finds, with their counts.</summary>
    private IReadOnlyList<(Item Item, int Count, int Free)> Shown() =>
        [.. Editor.Page(Page).Where(i => ItemStyle.Matches(i.Item, _search.Text, Texts.Lang))];

    public void Refresh()
    {
        _title.Text = Texts["inventory.title"];
        _search.PlaceholderText = Texts["inventory.search"];
        foreach ((Slot slot, (Button tile, ItemIcon icon)) in _slots)
        {
            Item? worn = Worn(slot);
            icon.Glyph = worn is null ? ItemStyle.GlyphOf(new Item("", new LocalizedText("", ""), ItemKind.Equipment, KindOf(slot))) : ItemStyle.GlyphOf(worn);
            icon.Tint = worn is null ? Colors.White : new Color(ItemStyle.Colour(worn));
            icon.Faint = worn is null;
            icon.Count = 1;
            icon.QueueRedraw();
            tile.TooltipText = SlotText(slot) + (worn is null ? "" : "\n" + Texts.ItemCard(worn));
            tile.Disabled = worn is null;
        }
        InventoryPage[] pages = Enum.GetValues<InventoryPage>();
        for (int i = 0; i < _pages.Count; i++)
        {
            _pages[i].Text = Texts["page." + pages[i]];
            _pages[i].SetPressedNoSignal(pages[i] == Page);
        }
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
            icon.Glyph = ItemStyle.GlyphOf(item);
            icon.Tint = new Color(ItemStyle.Colour(item));
            icon.Count = count;
            string card = Texts.ItemCard(item) + (free < count ? "\n" + Texts["inventory.free", free] : "");
            tile.TooltipText = card;
            // Equipment above the hero's level is drawn faint; all of it worn, dimmed; the rest cannot be worn at all.
            icon.Faint = item.Kind == ItemKind.Equipment && item.Level > Editor.Hero.Level;
            icon.Modulate = item.Kind == ItemKind.Equipment && free == 0 ? new Color(1, 1, 1, 0.5f) : Colors.White;
            tile.Disabled = Editor.WhyNot(item) is not null;
            tile.MouseEntered += () => _details.Text = card;
            tile.Pressed += () =>
            {
                Editor.Put(item);
                Refresh();
            };
            _grid.AddChild(tile);
        }
        if (_details.Text.Length == 0)
            _details.Text = Texts["inventory.hint"];
        _save.Text = Texts["points.save"];
        _save.Disabled = !Editor.Changed;
        _close.Text = Texts["points.close"];
    }

    /// <summary>What each slot shows, in the order of the slots, and the items on the page, as the self-test reads them.</summary>
    public IEnumerable<string> SlotsText => Enum.GetValues<Slot>().Select(SlotText);

    public IEnumerable<string> ListText => Shown().Select(i => $"{i.Item.Name.In(Texts.Lang)}  ×{i.Count}");

    public string MessageText => _message.Text;
}
