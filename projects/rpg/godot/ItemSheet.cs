using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// An item's sheet (sprint 117, D77), opened by its right click or its link in the chat: its icon and card on top,
/// then three tabs, the recipes that make it or need it, the monsters that leave it and at what chance, its set with
/// its pieces and bonuses. Each item on it is a tile of its own, with its card and its right click. It only draws
/// <see cref="ItemInfo"/>.
/// </summary>
public partial class ItemSheet : CanvasLayer
{
    private static readonly ItemAction[] Tabs = [ItemAction.Recipes, ItemAction.Bestiary, ItemAction.Set];

    private Texts _texts = null!;
    private TabContainer _tabs = null!;

    public event Action? ClosePressed;

    /// <summary>The item shown, the tab open, how many lines each tab has (for the self-test).</summary>
    public Item Item { get; private set; } = null!;
    public ItemAction Tab => Tabs[_tabs.CurrentTab];
    public Dictionary<ItemAction, List<string>> Lines { get; } = [];

    public void Build(Item item, ItemAction tab, Texts texts)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        _texts = texts;
        Layer = 20;
        var info = new ItemInfo(GameData.Embedded);
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -330, OffsetRight = 330, OffsetTop = -260, OffsetBottom = 260 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "item");
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 12);
        column.AddChild(top);
        top.AddChild(Tile(item, 64));
        var card = new Label { Text = texts.ItemCard(item), AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        top.AddChild(card);
        _tabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        column.AddChild(_tabs);
        Fill(ItemAction.Recipes, info.Recipes(item.Id).Select(r =>
        {
            Item made = GameData.Embedded.Items[r.Recipe.Item];
            string job = GameData.Embedded.Jobs[r.Job.Id].Name.In(texts.Lang);
            string text = r.Makes ? texts["item.makes", job, r.Recipe.Level, texts.Items(r.Recipe.Ingredients)]
                : texts["item.needs", made.Name.In(texts.Lang), job, r.Recipe.Level, r.Recipe.Ingredients.First(i => i.Item == item.Id).Count];
            return ((Item?)made, text);
        }));
        Fill(ItemAction.Bestiary, info.Drops(item.Id).Select(d => ((Item?)null, texts[d.ForQuest ? "item.drop-quest" : "item.drop", d.Monster.Name.In(texts.Lang), d.Monster.Level,
            d.Family?.Name.In(texts.Lang) ?? "", d.Percent])));
        Fill(ItemAction.Set, info.Set(item) is (ItemSet set, IReadOnlyList<Item> pieces)
            ? [(null, set.Name.In(texts.Lang)), .. pieces.Select(p => ((Item?)p, texts["item.piece", p.Name.In(texts.Lang), texts["slot." + p.Slot], p.Level])),
               .. set.Bonuses.Select(b => ((Item?)null, texts["inventory.pieces", b.Pieces, Bonus(b)]))]
            : []);
        _tabs.CurrentTab = Math.Max(0, Array.IndexOf(Tabs, tab));
        var close = new Button { Text = texts["craft.close"], CustomMinimumSize = new Vector2(160, 44), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        close.Pressed += () => ClosePressed?.Invoke();
        column.AddChild(close);
    }

    private string Bonus(SetBonus b) => _texts.Gives(b.Stats, b.Ap, b.Mp, b.Initiative);

    private void Fill(ItemAction tab, IEnumerable<(Item? Item, string Text)> rows)
    {
        var scroll = new ScrollContainer { Name = _texts["item." + tab], HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _tabs.AddChild(scroll);
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(box);
        List<string> lines = Lines[tab] = [];
        foreach ((Item? item, string text) in rows)
        {
            var line = new HBoxContainer();
            line.AddThemeConstantOverride("separation", 8);
            if (item is not null)
                line.AddChild(Tile(item, 40));
            line.AddChild(new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center });
            box.AddChild(line);
            lines.Add(text);
        }
        if (lines.Count == 0)
            box.AddChild(new Label { Text = _texts["item.none"], Modulate = new Color(1, 1, 1, 0.6f) });
    }

    private Button Tile(Item item, float size)
    {
        (Button tile, ItemIcon icon) = ItemIcon.Tile(size);
        (icon.Glyph, icon.Item, icon.Tint) = (ItemStyle.GlyphOf(item), item, new Color(ItemStyle.Colour(item)));
        tile.TooltipText = ItemMenu.Tooltip(item, _texts);
        return tile;
    }
}
