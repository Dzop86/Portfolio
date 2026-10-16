using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The mini-map (sprint 108, §5 ter.6): shown by default at the top right, left of the side bar; moved by its
/// header, three sizes, zoomed with the wheel over it or its − and + buttons, its place, size and zoom kept between
/// two games; the world seen from the sky around the player (<see cref="MiniMap"/>), its dots drawn over it, the
/// legend of the one under the pointer below; a click walks to the cell. N or its icon shows and hides it; a fight,
/// another screen, has none.
/// </summary>
public partial class MiniMapPanel : CanvasLayer
{
    private const string SettingsFile = "user://" + MiniMapSettings.FileName;
    private const float Header = 30;

    // The world's picture, computed once for the game (the maps do not change).
    private static WorldPicture? _picture;

    private MiniMap _mini = null!;
    private Texts _texts = null!;
    private PanelContainer _panel = null!;
    private Control _view = null!;
    private TextureRect _world = null!;
    private MiniDots _dots = null!;
    private Label _legend = null!;
    private Label _title = null!;
    private Button _out = null!, _in = null!, _size = null!;
    private Func<MiniScene>? _scene;
    private Vector2? _pointer;
    private bool _dragging;
    private bool _keep;

    /// <summary>A click on a cell of the player's map.</summary>
    public event Action<Cell>? WalkTo;

    public MiniMap Mini => _mini;
    public IReadOnlyList<MiniMark> Marks { get; private set; } = [];

    /// <summary>The settings as they are now, saved when they change (not by the self-tests, <paramref name="keep"/> false).</summary>
    public MiniMapSettings Settings { get; private set; } = new();

    public string LegendText => _legend.Text;
    public Vector2 PanelAt => _panel.Position;
    public Vector2 ViewSize => _view.Size;

    /// <summary>The settings kept by the last game, or the defaults.</summary>
    public static MiniMapSettings Load()
    {
        try
        {
            if (Godot.FileAccess.FileExists(SettingsFile))
            {
                using var file = Godot.FileAccess.Open(SettingsFile, Godot.FileAccess.ModeFlags.Read);
                return MiniMapSettings.Parse(file?.GetAsText());
            }
        }
        catch (IOException)
        {
            // The defaults.
        }
        return new MiniMapSettings();
    }

    public void Build(Texts texts, MiniMapSettings settings, Func<MiniScene> scene, bool keep)
    {
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _keep = keep;
        Layer = 6;
        _picture ??= new WorldPicture(GameData.Embedded);
        _mini = new MiniMap(GameData.Embedded, _picture) { Size = settings.Size, Zoom = settings.Zoom };
        Settings = settings;

        _panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        var frame = new StyleBoxFlat { BgColor = new Color(0.12f, 0.12f, 0.12f, 0.88f), BorderColor = new Color(1, 1, 1, 0.18f), ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 4, ContentMarginBottom = 6 };
        frame.SetBorderWidthAll(1);
        frame.SetCornerRadiusAll(8);
        _panel.AddThemeStyleboxOverride("panel", frame);
        AddChild(_panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        _panel.AddChild(column);

        // The header, grabbed to move it: its name, then − + and the size.
        var header = new HBoxContainer { CustomMinimumSize = new Vector2(0, Header - 4) };
        column.AddChild(header);
        _title = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0.75f) };
        _title.AddThemeFontSizeOverride("font_size", 14);
        header.AddChild(_title);
        header.AddChild(_out = Small("−", () => Zoomed(_mini.ZoomOut())));
        header.AddChild(_in = Small("+", () => Zoomed(_mini.ZoomIn())));
        header.AddChild(_size = Small("▣", NextSize));

        _view = new Control { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Stop };
        column.AddChild(_view);
        _view.AddChild(new ColorRect { Color = new Color(0.094f, 0.094f, 0.094f), MouseFilter = Control.MouseFilterEnum.Ignore, AnchorRight = 1, AnchorBottom = 1 });
        // The image is copied into the texture, then let go at once (see Main.QuitCleanly).
        using (Image image = Image.CreateFromData(_picture.Width, _picture.Height, false, Image.Format.Rgba8, _picture.Pixels))
        {
            _world = new TextureRect
            {
                Texture = ImageTexture.CreateFromImage(image),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
        }
        _view.AddChild(_world);
        _dots = new MiniDots { MouseFilter = Control.MouseFilterEnum.Ignore, AnchorRight = 1, AnchorBottom = 1 };
        _view.AddChild(_dots);
        _view.GuiInput += OnViewInput;
        _view.MouseExited += () => _pointer = null;

        _legend = new Label { CustomMinimumSize = new Vector2(0, 20), ClipText = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        _legend.AddThemeFontSizeOverride("font_size", 13);
        column.AddChild(_legend);

        _panel.GuiInput += OnPanelInput;
        SetTexts(texts);
        Resize();
        Callable.From(Place).CallDeferred();
        Visible = settings.Shown;
    }

    /// <summary>Its words in the language chosen.</summary>
    public void SetTexts(Texts texts)
    {
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _title.Text = texts["minimap.title"];
        _out.TooltipText = texts["minimap.out"];
        _in.TooltipText = texts["minimap.in"];
        _size.TooltipText = texts["minimap.size"];
    }

    /// <summary>Shows or hides it (N, its icon), the choice kept.</summary>
    public void Toggle()
    {
        Visible = !Visible;
        Keep(Settings with { Shown = Visible });
    }

    /// <summary>Each frame: the view on the player as drawn, (<paramref name="x"/>, <paramref name="y"/>) in cells of the map, facing <paramref name="facing"/>.</summary>
    public void Follow(string map, float x, float y, Vector2 facing)
    {
        if (_scene is null)
            return;
        _mini.Follow(map, x, y);
        (double sx, double sy, _, _) = _mini.Source;
        _world.Visible = _mini.InWorld;
        _world.Position = new Vector2((float)(-sx * _mini.Scale), (float)(-sy * _mini.Scale));
        _world.Size = new Vector2(_picture!.Width, _picture.Height) * (float)_mini.Scale;
        Marks = _mini.Marks(_scene(), _texts);
        _dots.Show(Marks, facing, _mini.InWorld);
        _legend.Text = _pointer is Vector2 p ? _mini.Legend(Marks, p.X, p.Y, _texts) : "";
    }

    /// <summary>The legend under a point of the view (the self-test).</summary>
    public string LegendAt(Vector2 inView) => _mini.Legend(Marks, inView.X, inView.Y, _texts);

    /// <summary>What a click on a point of the view does: the cell walked to, if any (the self-test too).</summary>
    public Cell? Click(Vector2 inView)
    {
        Cell? cell = _mini.WalkTo(inView.X, inView.Y);
        if (cell is Cell c)
            WalkTo?.Invoke(c);
        return cell;
    }

    /// <summary>Moves it as a drag of its header would, kept on screen (the self-test too).</summary>
    public void MoveBy(Vector2 by)
    {
        _panel.Position = Clamp(_panel.Position + by);
        Keep(Settings with { X = (int)_panel.Position.X, Y = (int)_panel.Position.Y });
    }

    public void NextSize()
    {
        _mini.Size = (MiniMapSize)(((int)_mini.Size + 1) % 3);
        Resize();
        _panel.Position = Clamp(_panel.Position);
        Keep(Settings with { Size = _mini.Size });
    }

    private void Zoomed(bool changed)
    {
        if (changed)
            Keep(Settings with { Zoom = _mini.Zoom });
    }

    private void OnViewInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
                Zoomed(_mini.ZoomIn());
                _view.AcceptEvent();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
                Zoomed(_mini.ZoomOut());
                _view.AcceptEvent();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } b:
                Click(b.Position);
                _view.AcceptEvent();
                break;
            case InputEventMouseMotion m:
                _pointer = m.Position;
                break;
        }
    }

    private void OnPanelInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } b:
                _dragging = b.Pressed && b.Position.Y <= Header;
                _panel.AcceptEvent();
                break;
            case InputEventMouseMotion m when _dragging:
                MoveBy(m.Relative);
                _panel.AcceptEvent();
                break;
            // The wheel over its frame is its own, not the camera's.
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown }:
                _panel.AcceptEvent();
                break;
        }
    }

    private void Resize()
    {
        var size = new Vector2(_mini.Width, _mini.Height);
        _view.CustomMinimumSize = size;
        _view.Size = size;
        _legend.CustomMinimumSize = new Vector2(size.X, 20);
        _panel.Size = Vector2.Zero;
        _panel.ResetSize();
    }

    /// <summary>Where it was left, or at the top right, left of the side bar, under the experience and the life.</summary>
    private void Place()
    {
        Vector2 screen = _panel.GetViewportRect().Size;
        Vector2 at = Settings.X is int x && Settings.Y is int y ? new Vector2(x, y) : new Vector2(screen.X - 76 - _panel.Size.X, 116);
        _panel.Position = Clamp(at);
    }

    /// <summary>A place where its header stays on screen.</summary>
    private Vector2 Clamp(Vector2 to)
    {
        Vector2 screen = _panel.GetViewportRect().Size;
        return new Vector2(Mathf.Clamp(to.X, -_panel.Size.X + 80, screen.X - 80), Mathf.Clamp(to.Y, 0, screen.Y - Header));
    }

    private void Keep(MiniMapSettings settings)
    {
        Settings = settings;
        if (!_keep)
            return;
        using var file = Godot.FileAccess.Open(SettingsFile, Godot.FileAccess.ModeFlags.Write);
        file?.StoreString(settings.ToText());
    }

    private static Button Small(string text, Action pressed)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(28, 26), FocusMode = Control.FocusModeEnum.None, Flat = true };
        b.AddThemeFontSizeOverride("font_size", 16);
        b.Pressed += pressed;
        return b;
    }
}

/// <summary>The mini-map's dots, drawn over the world's picture in their order, the player last.</summary>
public partial class MiniDots : Control
{
    private static readonly Color Monster = FighterView.EnemyColour, Gold = new("ffd85a"), Grey = new(0.55f, 0.55f, 0.55f, 0.8f), Way = new("e6e1d2"), Ink = new(0.08f, 0.08f, 0.08f);

    private IReadOnlyList<MiniMark> _marks = [];
    private Vector2 _facing = new(0, 1);
    private bool _inWorld;

    public void Show(IReadOnlyList<MiniMark> marks, Vector2 facing, bool inWorld)
    {
        _marks = marks;
        if (facing.LengthSquared() > 1e-6f)
            _facing = facing.Normalized();
        _inWorld = inWorld;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_inWorld)
            return;
        Font font = ThemeDB.FallbackFont;
        foreach (MiniMark m in _marks)
        {
            var at = new Vector2((float)m.X, (float)m.Y);
            switch (m.Kind)
            {
                case MiniMarkKind.Way:
                    Vector2 d = m.Direction == default ? new Vector2(0, 0) : new Vector2(m.Direction.X, m.Direction.Y);
                    if (d == Vector2.Zero)
                        DrawCircle(at, 3, Way);
                    else
                        Arrow(at, d, 6, Way);
                    if (m.Label is string zone)
                    {
                        Vector2 size = font.GetStringSize(zone, HorizontalAlignment.Left, -1, 11);
                        // Inside the view, back from the edge the way leaves by.
                        Vector2 text = at - d * 12 - new Vector2(size.X / 2, -4);
                        text.X = Mathf.Clamp(text.X, 2, Size.X - size.X - 2);
                        text.Y = Mathf.Clamp(text.Y, 12, Size.Y - 3);
                        DrawString(font, text + Vector2.One, zone, HorizontalAlignment.Left, -1, 11, Ink);
                        DrawString(font, text, zone, HorizontalAlignment.Left, -1, 11, Way);
                    }
                    break;
                case MiniMarkKind.Door:
                    DrawRect(new Rect2(at - new Vector2(4, 4), new Vector2(8, 8)), TownView.DoorColour);
                    DrawRect(new Rect2(at - new Vector2(4, 4), new Vector2(8, 8)), Ink, false, 1);
                    break;
                case MiniMarkKind.Workshop:
                    DrawColoredPolygon([at + new Vector2(0, -4), at + new Vector2(4, 0), at + new Vector2(0, 4), at + new Vector2(-4, 0)], new Color("c49a6c"));
                    break;
                case MiniMarkKind.Spot:
                    Color c = m.Low ? Grey : new Color(MiniMap.SpotColour(m.Spot ?? NodeKind.Herb));
                    DrawRect(new Rect2(at - new Vector2(2, 2), new Vector2(4, 4)), c);
                    break;
                case MiniMarkKind.Inhabitant:
                    DrawCircle(at, 3.5f, Gold);
                    DrawCircle(at, 3.5f, Ink, false, 1);
                    string? sign = m.Quest switch { QuestMark.Offer => "!", QuestMark.Ready or QuestMark.Waiting => "?", _ => null };
                    if (sign is not null)
                    {
                        Color ink = m.Quest == QuestMark.Waiting ? Grey : Gold;
                        DrawString(font, at + new Vector2(-3, -6) + Vector2.One, sign, HorizontalAlignment.Left, -1, 14, Ink);
                        DrawString(font, at + new Vector2(-3, -6), sign, HorizontalAlignment.Left, -1, 14, ink);
                    }
                    break;
                // The monsters ringed in dark, apart from the woodcutter's spots of the same warm colour.
                case MiniMarkKind.Group:
                    DrawCircle(at, 4, Monster);
                    DrawCircle(at, 4, Ink, false, 1.5f);
                    break;
                case MiniMarkKind.Horde:
                    DrawCircle(at, 6, Monster);
                    DrawCircle(at, 6, Ink, false, 1.5f);
                    break;
                case MiniMarkKind.Chief:
                    DrawCircle(at, 6, Monster);
                    DrawCircle(at, 6, Ink, false, 1.5f);
                    // A crown over it: three points of gold.
                    DrawColoredPolygon([at + new Vector2(-5, -7), at + new Vector2(-5, -12), at + new Vector2(-2.5f, -9), at + new Vector2(0, -13), at + new Vector2(2.5f, -9), at + new Vector2(5, -12), at + new Vector2(5, -7)], Gold);
                    break;
                case MiniMarkKind.Goal:
                    DrawArc(at, 7, 0, Mathf.Tau, 24, Gold, 2);
                    break;
                case MiniMarkKind.Player:
                    Arrow(at, _facing, 7, FighterView.PlayerColour);
                    break;
            }
        }
    }

    /// <summary>A triangle pointing along <paramref name="d"/>, its tip <paramref name="length"/> from its centre.</summary>
    private void Arrow(Vector2 at, Vector2 d, float length, Color colour)
    {
        Vector2 side = new Vector2(-d.Y, d.X) * length * 0.65f;
        Vector2[] points = [at + d * length, at - d * length * 0.6f + side, at - d * length * 0.6f - side];
        DrawColoredPolygon(points, colour);
        DrawPolyline([.. points, points[0]], Ink, 1);
    }
}
