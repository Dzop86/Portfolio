using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The bestiary (sprint 154, D83): on the left the families by zone, in the order of the world, with how many of their
/// monsters are known; on the right the chosen family's monsters, a silhouette until fought, then what the victories tell.
/// </summary>
public partial class BestiaryPanel : CanvasLayer
{
    private VBoxContainer _families = null!, _monsters = null!;
    private Label _count = null!;
    private Texts _texts = null!;
    private IReadOnlyList<BestiaryFamily> _page = [];

    public event Action? ClosePressed;

    /// <summary>The family shown on the right, and its monsters' names (for the self-test).</summary>
    public string? Shown { get; private set; }

    public List<string> Names { get; } = [];

    public void Build(Texts texts)
    {
        _texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -380, OffsetRight = 380, OffsetTop = -300, OffsetBottom = 300 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "bestiary");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        panel.AddChild(column);
        var head = new HBoxContainer();
        var title = new Label { Text = texts["bestiary.title"], SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 20);
        head.AddChild(title);
        _count = new Label();
        head.AddChild(_count);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44) };
        close.Pressed += () => ClosePressed?.Invoke();
        head.AddChild(close);
        column.AddChild(head);
        var body = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        column.AddChild(body);
        var left = new ScrollContainer { CustomMinimumSize = new Vector2(230, 0), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _families = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        left.AddChild(_families);
        body.AddChild(left);
        var right = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _monsters = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _monsters.AddThemeConstantOverride("separation", 10);
        right.AddChild(_monsters);
        body.AddChild(right);
    }

    /// <summary>The bestiary for these victories; the first family that has a monster met is shown, else the first.</summary>
    public void Show(IReadOnlyDictionary<string, int> victories)
    {
        ArgumentNullException.ThrowIfNull(victories);
        _page = BestiaryPage.Families(GameData.Embedded, victories, _texts);
        foreach (Node n in _families.GetChildren())
            n.QueueFree();
        int known = _page.Sum(f => f.Monsters.Count(m => m.Known != Knowledge.Unknown)), all = _page.Sum(f => f.Monsters.Count);
        _count.Text = _texts["bestiary.known", known, all];
        string? zone = null;
        foreach (BestiaryFamily family in _page)
        {
            if (family.Zone != zone)
            {
                zone = family.Zone;
                _families.AddChild(new Label { Text = zone, Modulate = new Color(1, 1, 1, 0.6f) });
            }
            int met = family.Monsters.Count(m => m.Known != Knowledge.Unknown);
            var b = new Button { Text = $"{family.Name} ({met}/{family.Monsters.Count})", Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 44) };
            b.Pressed += () => ShowFamily(family.Id);
            _families.AddChild(b);
        }
        if (_page.Count > 0)
            ShowFamily((_page.FirstOrDefault(f => f.Monsters.Any(m => m.Known != Knowledge.Unknown)) ?? _page[0]).Id);
    }

    public void ShowFamily(string id)
    {
        foreach (Node n in _monsters.GetChildren())
            n.QueueFree();
        Names.Clear();
        Shown = id;
        foreach (BestiaryMonster m in _page.First(f => f.Id == id).Monsters)
        {
            var card = new VBoxContainer();
            var name = new Label { Text = m.Name };
            name.AddThemeFontSizeOverride("font_size", 17);
            if (m.Known != Knowledge.Unknown)
                name.AddThemeColorOverride("font_color", FighterView.PlayerColour);
            card.AddChild(name);
            foreach (string line in m.Lines)
                card.AddChild(new Label { Text = line, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(460, 0) });
            _monsters.AddChild(card);
            Names.Add(m.Name);
        }
    }
}
