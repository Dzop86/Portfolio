using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The world map, over the map the player is on (sprint 71): the world seen from the sky, every map in
/// its place as one big map (<see cref="WorldPicture"/>), dragged with the mouse and zoomed with the
/// wheel around the pointer; a ring marks the player, the map under the pointer is named below with
/// its level. M or its icon opens and closes it.
/// </summary>
public partial class WorldMapPanel : CanvasLayer
{
    private static readonly Vector2 ViewSize = new(800, 420);
    private const float MaxZoom = 4;

    private WorldPicture _world = null!;
    private Texts _texts = null!;
    private Control _view = null!;
    private TextureRect _picture = null!;
    private Control _marker = null!;
    private Label _hover = null!;
    private Vector2 _player;
    private bool _dragging;

    public string Current { get; private set; } = "";

    /// <summary>The picture's pixels per screen pixel, and where its top-left corner is in the view.</summary>
    public float Zoom { get; private set; } = 1.5f;
    public Vector2 PictureAt { get; private set; }

    /// <summary>The smallest zoom: the whole world fits in the view.</summary>
    public float MinZoom => Math.Min(ViewSize.X / _world.Width, ViewSize.Y / _world.Height);

    public Vector2I PictureSize => new(_world.Width, _world.Height);

    /// <summary>Where the player's ring is, in the view.</summary>
    public Vector2 MarkerCentre => _marker.Position + _marker.Size / 2;

    // The place of the quest followed (sprint 106): a yellow ring, if any.
    private Control? _goal;
    private Vector2 _goalPixel;

    /// <summary>Where the ring of the quest followed is in the view, if any (for the self-test).</summary>
    public Vector2? GoalCentre => _goal is null ? null : _goal.Position + _goal.Size / 2;

    public void Build(GameData data, string current, Cell at, Texts texts, TrailGoal? goal = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        Current = current;
        _world = new WorldPicture(data);
        (double px, double py) = _world.PixelOf(current, at);
        _player = new Vector2((float)px, (float)py);

        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -420, OffsetRight = 420, OffsetTop = -270, OffsetBottom = 270 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);

        WindowDrag.Make(panel, "map");
        var area = new Control { CustomMinimumSize = new Vector2(840, 540) };
        panel.AddChild(area);
        var title = new Label { Text = texts["map.title"], Position = new Vector2(20, 10) };
        title.AddThemeFontSizeOverride("font_size", 24);
        area.AddChild(title);

        _view = new Control { Position = new Vector2(20, 54), Size = ViewSize, ClipContents = true, MouseFilter = Control.MouseFilterEnum.Stop, MouseDefaultCursorShape = Control.CursorShape.Drag };
        area.AddChild(_view);
        _view.AddChild(new ColorRect { Size = ViewSize, Color = new Color(0.094f, 0.094f, 0.094f), MouseFilter = Control.MouseFilterEnum.Ignore });
        // The image is copied into the texture, then let go at once (see Main.QuitCleanly).
        using Image image = Image.CreateFromData(_world.Width, _world.Height, false, Image.Format.Rgba8, _world.Pixels);
        _picture = new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(image),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _view.AddChild(_picture);
        // A light grid on the maps' edges (sprint 85, D67), shown or not, the choice kept.
        _grid = new MapGrid { MouseFilter = Control.MouseFilterEnum.Ignore, Cell = new Vector2(_world.MapWidth, _world.MapHeight) * WorldPicture.CellPixels, Visible = ShowGrid };
        _view.AddChild(_grid);
        var gridBox = new CheckBox { Text = texts["map.grid"], ButtonPressed = ShowGrid, Position = new Vector2(620, 8), CustomMinimumSize = new Vector2(0, 44) };
        gridBox.Toggled += on =>
        {
            ShowGrid = on;
            _grid.Visible = on;
        };
        area.AddChild(gridBox);
        // The player: a ring in the accent colour, the same size at every zoom.
        _marker = new Panel { Size = new Vector2(22, 22), MouseFilter = Control.MouseFilterEnum.Ignore };
        _marker.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0),
            BorderColor = FighterView.PlayerColour,
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            BorderWidthTop = 4,
            BorderWidthBottom = 4,
            CornerRadiusTopLeft = 11,
            CornerRadiusTopRight = 11,
            CornerRadiusBottomLeft = 11,
            CornerRadiusBottomRight = 11,
        });
        _view.AddChild(_marker);
        // The dungeons' doors (sprint 83, D66): a chocolate square each, named on hover.
        foreach (Dungeon d in data.Dungeons.Values)
        {
            (double dx, double dy) = _world.PixelOf(d.Map, d.Door);
            var door = new Panel { Size = new Vector2(16, 16), MouseFilter = Control.MouseFilterEnum.Ignore };
            door.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = TownView.DoorColour,
                BorderColor = new Color(0.1f, 0.07f, 0.04f),
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                BorderWidthTop = 2,
                BorderWidthBottom = 2,
                CornerRadiusTopLeft = 3,
                CornerRadiusTopRight = 3,
                CornerRadiusBottomLeft = 3,
                CornerRadiusBottomRight = 3,
            });
            _view.AddChild(door);
            _doors.Add((d, new Vector2((float)dx, (float)dy), door));
        }
        if (goal is not null && data.Towns.TryGetValue(goal.Map, out Town? goalMap) && !goalMap.IsHall)
        {
            (double gx, double gy) = _world.PixelOf(goal.Map, goal.At);
            _goalPixel = new Vector2((float)gx, (float)gy);
            _goal = new Panel { Size = new Vector2(26, 26), MouseFilter = Control.MouseFilterEnum.Ignore };
            var ring = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = new Color("ffd85a") };
            ring.SetBorderWidthAll(4);
            ring.SetCornerRadiusAll(13);
            _goal.AddThemeStyleboxOverride("panel", ring);
            _view.AddChild(_goal);
        }
        _view.GuiInput += OnInput;
        _view.MouseExited += () => ShowHover(null);

        _hover = new Label { Position = new Vector2(20, 486), Size = new Vector2(800, 30) };
        area.AddChild(_hover);
        // Opened on the player, zoomed in enough to see the map around them.
        Zoom = Math.Max(1.5f, MinZoom);
        PictureAt = ViewSize / 2 - _player * Zoom;
        Place();
        ShowHover(MarkerCentre);
    }

    /// <summary>The map under a point of the view, as the line below names it, with its level; empty off the maps.</summary>
    public string HoverAt(Vector2 inView)
    {
        // A dungeon's door under the pointer: its name first.
        if (_doors.FirstOrDefault(d => (d.Door.Position + d.Door.Size / 2).DistanceTo(inView) <= 10) is { Dungeon: Dungeon dungeon })
            return _texts["map.dungeon", dungeon.Name.In(_texts.Lang)];
        Vector2 p = (inView - PictureAt) / Zoom;
        return _world.MapAtPixel(p.X, p.Y) is Town map ? $"{_texts.MapTitle(map)} · {_texts["map.level", map.Level]}{(map.Id == Current ? " · " + _texts["map.here"] : "")}" : "";
    }

    public string HoverText => _hover.Text;

    private readonly List<(Dungeon Dungeon, Vector2 Pixel, Panel Door)> _doors = [];

    private MapGrid _grid = null!;

    /// <summary>Whether the grid of the maps is shown, for every opening of the world map until changed.</summary>
    public static bool ShowGrid { get; set; } = true;

    public bool GridShown => _grid.Visible;

    /// <summary>The dungeons' doors on the map and where they are drawn in the view (for the self-test).</summary>
    public IEnumerable<(string Dungeon, Vector2 Centre)> Doors => _doors.Select(d => (d.Dungeon.Id, d.Door.Position + d.Door.Size / 2));

    /// <summary>Zooms by <paramref name="factor"/>, the point of the picture under <paramref name="around"/> staying there.</summary>
    public void ZoomAround(Vector2 around, float factor)
    {
        float zoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
        PictureAt = around - (around - PictureAt) * (zoom / Zoom);
        Zoom = zoom;
        Place();
    }

    /// <summary>Moves the picture by <paramref name="by"/> screen pixels.</summary>
    public void Drag(Vector2 by)
    {
        PictureAt += by;
        Place();
    }

    private void OnInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp } wheel:
                ZoomAround(wheel.Position, 1.25f);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown } wheel:
                ZoomAround(wheel.Position, 0.8f);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                _dragging = button.Pressed;
                break;
            case InputEventMouseMotion motion:
                if (_dragging)
                    Drag(motion.Relative);
                ShowHover(motion.Position);
                break;
        }
    }

    /// <summary>The picture where <see cref="PictureAt"/> and <see cref="Zoom"/> say, never dragged out of the view.</summary>
    private void Place()
    {
        Vector2 size = new Vector2(_world.Width, _world.Height) * Zoom;
        // Smaller than the view along an axis: centred on it; larger: no empty band at its edges.
        float Keep(float offset, float picture, float view) => picture <= view ? (view - picture) / 2 : Math.Clamp(offset, view - picture, 0);
        PictureAt = new Vector2(Keep(PictureAt.X, size.X, ViewSize.X), Keep(PictureAt.Y, size.Y, ViewSize.Y));
        _picture.Position = PictureAt;
        _picture.Size = size;
        _grid.Position = PictureAt;
        _grid.Size = size;
        _grid.Zoom = Zoom;
        _grid.QueueRedraw();
        _marker.Position = PictureAt + _player * Zoom - _marker.Size / 2;
        foreach ((_, Vector2 pixel, Panel door) in _doors)
            door.Position = PictureAt + pixel * Zoom - door.Size / 2;
        if (_goal is not null)
            _goal.Position = PictureAt + _goalPixel * Zoom - _goal.Size / 2;
    }

    private void ShowHover(Vector2? at) => _hover.Text = at is Vector2 p ? HoverAt(p) : "";
}

/// <summary>The edges of the maps over the world's picture: thin and faint, not to spoil it.</summary>
public partial class MapGrid : Control
{
    /// <summary>The size of a map in pixels of the picture, and the zoom it is drawn at.</summary>
    public Vector2 Cell { get; set; }
    public float Zoom { get; set; } = 1;

    public override void _Draw()
    {
        var line = new Color(1, 1, 1, 0.22f);
        float step = Cell.X * Zoom, rise = Cell.Y * Zoom;
        for (float x = step; x < Size.X - 0.5f; x += step)
            DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), line, 1);
        for (float y = rise; y < Size.Y - 0.5f; y += rise)
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), line, 1);
    }
}
