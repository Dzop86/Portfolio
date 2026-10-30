using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The guild's chest at Edmée's (sprint 156, D83, question 81 a), opened by her answer: the four tabs (those the guild's
/// level has not opened yet greyed, with the level they need), the tab's forty slots, the bag beside it, how many to move,
/// and the journal of the last movements under them. A click on the bag deposits, a click in the tab takes out, when one's
/// rank allows (<see cref="GuildPage.CanTake"/>); the leader opens a tab to the members or closes it. The server checks it all.
/// </summary>
public partial class GuildChestPanel : CanvasLayer
{
    private const int TileSize = 48;

    private Label _title = null!, _state = null!, _message = null!, _room = null!;
    private HBoxContainer _tabs = null!;
    private GridContainer _slots = null!, _bag = null!;
    private VBoxContainer _journal = null!;
    private Button _open = null!;
    private SpinBox _count = null!;
    private Texts _texts = null!;
    private ChestView? _chest;

    public event Action<string, int, int>? DepositPressed;
    public event Action<string, int, int>? WithdrawPressed;
    public event Action<int, bool>? OpenPressed;
    public event Action? ClosePressed;

    /// <summary>The tab shown, from 0.</summary>
    public int Tab { get; private set; }

    /// <summary>For the self-test: the stacks of the tab shown, the bag's, the journal's lines, the tabs one may click.</summary>
    public List<string> Shown { get; } = [];

    public List<string> Bag { get; } = [];

    public List<string> Journal { get; } = [];

    public int TabsOpen => _tabs.GetChildren().OfType<Button>().Count(b => !b.Disabled);

    public string Message => _message.Text;

    public void Build(Texts texts)
    {
        _texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -440, OffsetRight = 440, OffsetTop = -290, OffsetBottom = 290 };
        StyleBoxFlat frame = InventoryPanel.Background();
        frame.ContentMarginLeft = frame.ContentMarginRight = 14;
        frame.ContentMarginTop = frame.ContentMarginBottom = 10;
        panel.AddThemeStyleboxOverride("panel", frame);
        AddChild(panel);
        WindowDrag.Make(panel, "guild-chest");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);

        var head = new HBoxContainer();
        _title = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 20);
        head.AddChild(_title);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44), Flat = true };
        close.Pressed += () => ClosePressed?.Invoke();
        head.AddChild(close);
        column.AddChild(head);
        column.AddChild(new HSeparator());

        var bar = new HBoxContainer();
        bar.AddThemeConstantOverride("separation", 6);
        column.AddChild(bar);
        _tabs = new HBoxContainer();
        _tabs.AddThemeConstantOverride("separation", 4);
        bar.AddChild(_tabs);
        _state = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center, ClipText = true, Modulate = new Color(1, 1, 1, 0.75f) };
        bar.AddChild(_state);
        _open = new Button { CustomMinimumSize = new Vector2(190, 44) };
        _open.Pressed += () =>
        {
            if (_chest is not null)
                OpenPressed?.Invoke(Tab, !_chest.Chest.Tabs[Tab].Open);
        };
        bar.AddChild(_open);
        bar.AddChild(new Label { Text = texts["chest.count"], VerticalAlignment = VerticalAlignment.Center });
        _count = new SpinBox { MinValue = 1, MaxValue = 9999, Value = 1, CustomMinimumSize = new Vector2(100, 44) };
        bar.AddChild(_count);

        var halves = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        halves.AddThemeConstantOverride("separation", 16);
        column.AddChild(halves);
        var chest = new VBoxContainer();
        halves.AddChild(chest);
        _room = new Label { Modulate = new Color(1, 1, 1, 0.6f) };
        chest.AddChild(_room);
        _slots = new GridContainer { Columns = 8 };
        _slots.AddThemeConstantOverride("h_separation", 4);
        _slots.AddThemeConstantOverride("v_separation", 4);
        chest.AddChild(_slots);
        var bag = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        halves.AddChild(bag);
        bag.AddChild(new Label { Text = texts["chest.bag"], Modulate = new Color(1, 1, 1, 0.6f) });
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        bag.AddChild(scroll);
        _bag = new GridContainer { Columns = 6 };
        _bag.AddThemeConstantOverride("h_separation", 4);
        _bag.AddThemeConstantOverride("v_separation", 4);
        scroll.AddChild(_bag);

        column.AddChild(new HSeparator());
        column.AddChild(new Label { Text = texts["chest.journal"], Modulate = new Color(1, 1, 1, 0.6f) });
        var log = new ScrollContainer { CustomMinimumSize = new Vector2(0, 96), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(log);
        _journal = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        log.AddChild(_journal);
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", new Color("ffb4a2"));
        column.AddChild(_message);
    }

    /// <summary>The chest as the server says, for a guild of that name and level, with the bag beside it, at <paramref name="now"/>.</summary>
    public void Show(ChestView chest, string guild, int level, IReadOnlyList<ItemCount> bag, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(chest);
        _chest = chest;
        Tab = Math.Clamp(Tab, 0, chest.Chest.Tabs.Count - 1);
        _title.Text = $"{_texts["chest.title"]} · {guild}";
        Empty(_tabs);
        for (int t = 0; t < 4; t++)
        {
            int tab = t;
            bool has = t < chest.Chest.Tabs.Count;
            var b = new Button { Text = _texts["chest.tab", t + 1], CustomMinimumSize = new Vector2(96, 44), ToggleMode = true, ButtonPressed = t == Tab, Disabled = !has,
                TooltipText = has ? _texts[chest.Chest.Tabs[t].Open ? "chest.tab.open" : "chest.tab.closed", t + 1] : _texts["chest.tab.locked", t + 1, GuildPage.TabLevel(t)] };
            b.Pressed += () =>
            {
                Tab = tab;
                Show(chest, guild, level, bag, now);
            };
            _tabs.AddChild(b);
        }
        GuildTab shown = chest.Chest.Tabs[Tab];
        _state.Text = _texts[shown.Open ? "chest.tab.open" : "chest.tab.closed", Tab + 1];
        _open.Visible = chest.Rank == GuildRank.Leader;
        _open.Text = _texts[shown.Open ? "chest.close" : "chest.open"];
        _room.Text = _texts["chest.room", shown.Items.Count, GuildChests.TabSize];
        bool take = GuildPage.CanTake(chest, Tab);
        Shown.Clear();
        Empty(_slots);
        foreach (ItemCount stack in shown.Items)
        {
            Shown.Add($"{stack.Item}:{stack.Count}");
            _slots.AddChild(Tile(stack, take ? _texts["chest.withdraw", "{0}", ItemName(stack.Item)] : _texts["chest.no-withdraw"], take,
                n => WithdrawPressed?.Invoke(stack.Item, n, Tab)));
        }
        for (int i = shown.Items.Count; i < GuildChests.TabSize; i++)
        {
            // An empty slot: a faint square.
            var slot = new Panel { CustomMinimumSize = new Vector2(TileSize, TileSize), MouseFilter = Control.MouseFilterEnum.Ignore };
            slot.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.05f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
            _slots.AddChild(slot);
        }
        Bag.Clear();
        Empty(_bag);
        foreach (ItemCount stack in bag)
        {
            Bag.Add($"{stack.Item}:{stack.Count}");
            _bag.AddChild(Tile(stack, _texts["chest.deposit", "{0}", ItemName(stack.Item)], true, n => DepositPressed?.Invoke(stack.Item, n, Tab)));
        }
        Journal.Clear();
        Empty(_journal);
        foreach (GuildMove move in chest.Chest.Journal)
        {
            string ago = GuildPage.Ago(move.At, now) is (int count, string unit) ? _texts["guild.seen." + unit, count] : "";
            string line = _texts[move.Count > 0 ? "chest.in" : "chest.out", ago, move.Who, Math.Abs(move.Count), ItemName(move.Item), move.Tab + 1];
            Journal.Add(line);
            _journal.AddChild(new Label { Text = line, ClipText = true, Modulate = new Color(1, 1, 1, 0.8f) });
        }
        if (chest.Chest.Journal.Count == 0)
            _journal.AddChild(new Label { Text = _texts["chest.journal.empty"], Modulate = new Color(1, 1, 1, 0.5f) });
    }

    private string ItemName(string item) => GameData.Embedded.Items.TryGetValue(item, out Item? found) ? found.Name.In(_texts.Lang) : item;

    // A stack's tile: its card and what a click does in its tooltip ("{0}": how many, as the count says).
    private Button Tile(ItemCount stack, string action, bool can, Action<int> pressed)
    {
        (Button tile, ItemIcon icon) = ItemIcon.Tile(TileSize);
        Item? item = GameData.Embedded.Items.GetValueOrDefault(stack.Item);
        if (item is not null)
            icon.Item = item;
        icon.Count = stack.Count;
        int many() => Math.Min(stack.Count, (int)_count.Value);
        tile.MouseEntered += () => tile.TooltipText = (item is null ? "" : ItemMenu.Tooltip(item, _texts) + "\n") + string.Format(System.Globalization.CultureInfo.InvariantCulture, action, many());
        tile.Disabled = !can;
        icon.Modulate = can ? Colors.White : new Color(1, 1, 1, 0.6f);
        tile.Pressed += () => pressed(many());
        return tile;
    }

    private static void Empty(Node node)
    {
        foreach (Node n in node.GetChildren())
        {
            node.RemoveChild(n);
            n.QueueFree();
        }
    }

    /// <summary>What went wrong with the server, or nothing.</summary>
    public void Tell(string text) => _message.Text = text;

    /// <summary>A tile of the tab or of the bag clicked, as the mouse would (for the self-test).</summary>
    public void Click(bool inBag, string item)
    {
        GridContainer grid = inBag ? _bag : _slots;
        List<string> listed = inBag ? Bag : Shown;
        int i = listed.FindIndex(l => l.StartsWith(item + ":", StringComparison.Ordinal));
        if (i >= 0 && grid.GetChild(i) is Button { Disabled: false } tile)
            tile.EmitSignal(BaseButton.SignalName.Pressed);
    }
}
