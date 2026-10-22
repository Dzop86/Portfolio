using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A waystone's window (sprint 127, D79): the world seen from the sky with every waystone on it (those discovered in the
/// accent colour, the others faint, the one here outlined), and the list of them: those discovered to travel to for
/// their price in Éclats, or to make one's home; the others not discovered yet.
/// </summary>
public partial class WaystonePanel : CanvasLayer
{
    private const int PictureWidth = 520;
    private VBoxContainer _list = null!;
    private Label _shards = null!, _message = null!;
    private Marks _marks = null!;
    private Texts _texts = null!;
    private WorldPicture _world = null!;
    private string _here = "";

    public event Action<string>? TravelPressed;
    public event Action<string>? HomePressed;
    public event Action? ClosePressed;

    /// <summary>The lines shown, one a waystone (for the self-test).</summary>
    public List<string> Lines { get; } = [];

    /// <summary>The waystones one may travel to now (for the self-test).</summary>
    public List<string> Travels { get; } = [];

    public void Build(Texts texts, string here)
    {
        _texts = texts;
        _here = here;
        Layer = 15;
        _world = new WorldPicture(GameData.Embedded);
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -290, OffsetRight = 290, OffsetTop = -320, OffsetBottom = 320 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "waystones");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        panel.AddChild(column);
        var head = new HBoxContainer();
        var title = new Label { Text = texts["waystone.title"], SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 20);
        head.AddChild(title);
        _shards = new Label();
        head.AddChild(_shards);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44) };
        close.Pressed += () => ClosePressed?.Invoke();
        head.AddChild(close);
        column.AddChild(head);
        // The world from the sky (T38), scaled to the window, the waystones marked over it.
        using Image image = Image.CreateFromData(_world.Width, _world.Height, false, Image.Format.Rgba8, _world.Pixels);
        float scale = PictureWidth / (float)_world.Width;
        var picture = new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(image),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            CustomMinimumSize = new Vector2(PictureWidth, _world.Height * scale),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        _marks = new Marks { Scale0 = scale, World = _world, Here = here };
        _marks.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        picture.AddChild(_marks);
        column.AddChild(picture);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(_list);
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _message.AddThemeColorOverride("font_color", new Color("ffb4a2"));
        column.AddChild(_message);
    }

    /// <summary>The waystones as the server says the character knows them, with its Éclats.</summary>
    public void Show(WaystoneView view, long shards)
    {
        ArgumentNullException.ThrowIfNull(view);
        foreach (Node n in _list.GetChildren())
            n.QueueFree();
        Lines.Clear();
        Travels.Clear();
        _shards.Text = _texts["waystone.shards", shards];
        _marks.Known = [.. view.Known];
        _marks.QueueRedraw();
        foreach ((string id, string map, _) in Waystones.All(GameData.Embedded))
        {
            string name = _texts.ZoneName(Contacts.ZoneOf(map, GameData.Embedded), GameData.Embedded);
            bool known = view.Known.Contains(id);
            string line = !known ? _texts["waystone.unknown", name] : view.Home == id || (view.Home is null && id == Waystones.Village) ? _texts["waystone.home", name] : _texts["waystone.known", name];
            var row = new HBoxContainer();
            var label = new Label { Text = line, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true, Modulate = known ? Colors.White : new Color(1, 1, 1, 0.5f) };
            row.AddChild(label);
            if (known)
            {
                long cost = Waystones.Cost(id, GameData.Embedded);
                bool here = map == _here;
                var travel = new Button { Text = here ? _texts["waystone.here"] : _texts["waystone.travel", cost], Disabled = here || cost > shards, CustomMinimumSize = new Vector2(0, 44) };
                travel.Pressed += () => TravelPressed?.Invoke(id);
                row.AddChild(travel);
                if (!travel.Disabled)
                    Travels.Add(id);
                if (view.Home != id)
                {
                    var home = new Button { Text = _texts["waystone.make-home"], CustomMinimumSize = new Vector2(0, 44) };
                    home.Pressed += () => HomePressed?.Invoke(id);
                    row.AddChild(home);
                }
            }
            _list.AddChild(row);
            Lines.Add(line);
        }
    }

    /// <summary>What went wrong with the server, or nothing.</summary>
    public void Tell(string text) => _message.Text = text;

    // The waystones over the picture: discovered in the accent colour, the others faint, the one of this map ringed.
    private sealed partial class Marks : Control
    {
        public float Scale0 { get; set; }
        public WorldPicture World { get; set; } = null!;
        public string Here { get; set; } = "";
        public HashSet<string> Known { get; set; } = [];

        public override void _Draw()
        {
            foreach ((string id, string map, Cell at) in Waystones.All(GameData.Embedded))
            {
                (double x, double y) = World.PixelOf(map, at);
                var p = new Vector2((float)x, (float)y) * Scale0;
                DrawCircle(p, 5, Known.Contains(id) ? FighterView.PlayerColour : new Color(1, 1, 1, 0.45f));
                if (map == Here)
                    DrawArc(p, 9, 0, Mathf.Tau, 24, Colors.White, 2, true);
            }
        }
    }
}
