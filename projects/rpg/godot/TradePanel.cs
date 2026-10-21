using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The trade's window (sprint 119, D74): on the left the player's offer (a click takes an item back), its Éclats and
/// the inventory under it (a click puts an item on the table); on the right the other's offer, and who has accepted;
/// "Accept" and "Call off". Each change of the offer goes to the server, which asks both to accept again.
/// </summary>
public partial class TradePanel : CanvasLayer
{
    private Texts _texts = null!;
    private TradeDraft _draft = null!;
    private IReadOnlyList<ItemCount> _owned = [];
    private GridContainer _mine = null!, _theirs = null!, _inventory = null!;
    private SpinBox _shards = null!;
    private Label _title = null!, _theirsTitle = null!, _theirShards = null!, _status = null!;
    private Button _ready = null!;
    private TradeView? _view;

    /// <summary>The offer changed by the player: to send.</summary>
    public event Action<TradeOffer>? OfferChanged;
    public event Action? ReadyPressed;
    public event Action? CancelPressed;

    /// <summary>The player's offer as it stands (for the self-test).</summary>
    public TradeOffer Offer => _draft.Offer;

    /// <summary>The other's offer and the status line as shown (for the self-test).</summary>
    public TradeOffer? Theirs => _view?.Theirs;
    public string Status => _status.Text;

    public void Build(TradeDraft draft, IReadOnlyList<ItemCount> owned, Texts texts)
    {
        (_draft, _owned, _texts) = (draft, owned, texts);
        Layer = 16;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -420, OffsetRight = 420, OffsetTop = -280, OffsetBottom = 280 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "trade");
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 22);
        column.AddChild(_title);
        var halves = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        halves.AddThemeConstantOverride("separation", 16);
        column.AddChild(halves);
        var left = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        left.AddThemeConstantOverride("separation", 6);
        halves.AddChild(left);
        left.AddChild(Heading(texts["trade.mine"]));
        _mine = new GridContainer { Columns = 6 };
        left.AddChild(_mine);
        var shards = new HBoxContainer();
        shards.AddChild(new ShardIcon { CustomMinimumSize = new Vector2(22, 22), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        _shards = new SpinBox { MinValue = 0, MaxValue = 1e12, CustomMinimumSize = new Vector2(140, 44) };
        _shards.ValueChanged += v =>
        {
            // Within what the player has: the field shows what goes on the table.
            _draft.SetShards((long)v);
            _shards.SetValueNoSignal(_draft.Shards);
            OfferChanged?.Invoke(_draft.Offer);
        };
        shards.AddChild(_shards);
        left.AddChild(shards);
        left.AddChild(new Label { Text = texts["trade.hint"], AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1, 1, 1, 0.6f), CustomMinimumSize = new Vector2(380, 0) });
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        left.AddChild(scroll);
        _inventory = new GridContainer { Columns = 7 };
        scroll.AddChild(_inventory);
        halves.AddChild(new VSeparator());
        var right = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 6);
        halves.AddChild(right);
        _theirsTitle = Heading("");
        right.AddChild(_theirsTitle);
        _theirs = new GridContainer { Columns = 6 };
        right.AddChild(_theirs);
        _theirShards = new Label();
        right.AddChild(_theirShards);
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color("ffd85a") };
        right.AddChild(_status);
        var bottom = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        bottom.AddThemeConstantOverride("separation", 12);
        column.AddChild(bottom);
        _ready = new Button { Text = texts["trade.ready"], CustomMinimumSize = new Vector2(160, 44) };
        _ready.Pressed += () => ReadyPressed?.Invoke();
        bottom.AddChild(_ready);
        var cancel = new Button { Text = texts["trade.cancel"], CustomMinimumSize = new Vector2(160, 44) };
        cancel.Pressed += () => CancelPressed?.Invoke();
        bottom.AddChild(cancel);
        FillMine();
    }

    /// <summary>The trade as the server says: the other's offer, who has accepted.</summary>
    public void Show(TradeView view)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _title.Text = _texts["trade.title", view.WithName];
        _theirsTitle.Text = _texts["trade.theirs", view.WithName];
        Fill(_theirs, view.Theirs.Items ?? [], null);
        _theirShards.Text = $"{view.Theirs.Shards} {_texts["end.shards"]}";
        _status.Text = view.MineReady ? _texts["trade.waiting", view.WithName] : view.TheirsReady ? _texts["trade.they-ready", view.WithName] : "";
        _ready.Disabled = view.MineReady;
    }

    /// <summary>An item of the inventory clicked: on the table, as a click does (for the self-test too).</summary>
    public void Put(string item)
    {
        if (_draft.Add(item))
            Changed();
    }

    /// <summary>An item of the offer clicked: taken back.</summary>
    public void TakeBack(string item)
    {
        if (_draft.Remove(item))
            Changed();
    }

    private void Changed()
    {
        FillMine();
        OfferChanged?.Invoke(_draft.Offer);
    }

    private void FillMine()
    {
        Fill(_mine, _draft.Offer.Items ?? [], TakeBack);
        foreach (Node n in _inventory.GetChildren())
            n.QueueFree();
        foreach (ItemCount owned in _owned.Where(o => GameData.Embedded.Items.ContainsKey(o.Item)))
        {
            int left = _draft.Left(owned.Item);
            if (left > 0)
                _inventory.AddChild(Tile(owned.Item, left, Put));
        }
    }

    private void Fill(GridContainer grid, IReadOnlyList<ItemCount> items, Action<string>? click)
    {
        foreach (Node n in grid.GetChildren())
            n.QueueFree();
        foreach (ItemCount i in items.Where(i => GameData.Embedded.Items.ContainsKey(i.Item)))
            grid.AddChild(Tile(i.Item, i.Count, click));
        if (items.Count == 0)
            grid.AddChild(new Label { Text = _texts["trade.nothing"], Modulate = new Color(1, 1, 1, 0.6f) });
    }

    private Button Tile(string id, int count, Action<string>? click)
    {
        Item item = GameData.Embedded.Items[id];
        (Button tile, ItemIcon icon) = ItemIcon.Tile(52);
        (icon.Glyph, icon.Item, icon.Tint, icon.Count) = (ItemStyle.GlyphOf(item), item, new Color(ItemStyle.Colour(item)), count);
        tile.TooltipText = ItemMenu.Tooltip(item, _texts);
        if (click is not null)
            tile.Pressed += () => click(id);
        return tile;
    }

    private static Label Heading(string text)
    {
        var l = new Label { Text = text };
        l.AddThemeFontSizeOverride("font_size", 17);
        l.AddThemeColorOverride("font_color", FighterView.PlayerColour);
        return l;
    }
}
