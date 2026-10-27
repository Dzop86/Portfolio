using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The notice board's window (sprint 153, D83): opened by examining a board, each notice with its title, its text, how far
/// it went and its reward, a "Group" badge for a horde or a chief; "Hand in" when it is fulfilled, "Swap" with its price.
/// </summary>
public partial class ContractsPanel : CanvasLayer
{
    private VBoxContainer _list = null!;
    private Label _message = null!, _count = null!;
    private Texts _texts = null!;

    public event Action<int>? HandInPressed;
    public event Action<int>? ChangePressed;
    public event Action? ClosePressed;

    /// <summary>The notices shown, by title (for the self-test).</summary>
    public List<string> Titles { get; } = [];

    /// <summary>The notices that can be handed in now, by place (for the self-test).</summary>
    public List<int> Fulfilled { get; } = [];

    public void Build(Texts texts)
    {
        _texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -300, OffsetRight = 300, OffsetTop = -300, OffsetBottom = 300 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "contracts");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        panel.AddChild(column);
        var head = new HBoxContainer();
        var title = new Label { Text = texts["contracts.window"], SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 20);
        head.AddChild(title);
        _count = new Label();
        head.AddChild(_count);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44) };
        close.Pressed += () => ClosePressed?.Invoke();
        head.AddChild(close);
        column.AddChild(head);
        column.AddChild(new Label { Text = texts["contracts.renewal"], Modulate = new Color(1, 1, 1, 0.7f) });
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_list);
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", new Color("ffb4a2"));
        column.AddChild(_message);
    }

    /// <summary>The board as the server says it, for a character of this level with these items.</summary>
    public void Show(ContractBoard board, int level, IReadOnlyList<ItemCount> owned)
    {
        ArgumentNullException.ThrowIfNull(board);
        foreach (Node n in _list.GetChildren())
            n.QueueFree();
        Titles.Clear();
        Fulfilled.Clear();
        _count.Text = _texts["contracts.pending", board.Offered.Count];
        IReadOnlyList<ContractLine> lines = ContractsPage.Lines(board, level, owned, _texts, GameData.Embedded);
        if (lines.Count == 0)
            _list.AddChild(new Label { Text = _texts["contracts.empty"] });
        for (int i = 0; i < lines.Count; i++)
        {
            int index = i;
            ContractLine line = lines[i];
            var card = new VBoxContainer();
            var top = new HBoxContainer();
            var name = new Label { Text = line.Title, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            name.AddThemeFontSizeOverride("font_size", 17);
            name.AddThemeColorOverride("font_color", FighterView.PlayerColour);
            top.AddChild(name);
            if (line.Group)
                top.AddChild(new Label { Text = $"[{_texts["contracts.group"]}]", Modulate = new Color("f2876b") });
            top.AddChild(new Label { Text = line.Progress });
            card.AddChild(top);
            card.AddChild(new Label { Text = line.Text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(540, 0) });
            var buttons = new HBoxContainer();
            buttons.AddChild(new Label { Text = line.Reward, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
            var hand = new Button { Text = _texts["contracts.handIn"], Disabled = !line.CanHandIn, CustomMinimumSize = new Vector2(0, 44) };
            hand.Pressed += () => HandInPressed?.Invoke(index);
            buttons.AddChild(hand);
            var change = new Button { Text = line.Change, CustomMinimumSize = new Vector2(0, 44) };
            change.Pressed += () => ChangePressed?.Invoke(index);
            buttons.AddChild(change);
            card.AddChild(buttons);
            _list.AddChild(card);
            Titles.Add(line.Title);
            if (line.CanHandIn)
                Fulfilled.Add(index);
        }
    }

    /// <summary>What went wrong with the server, or a word about the reward.</summary>
    public void Tell(string text) => _message.Text = text;
}
