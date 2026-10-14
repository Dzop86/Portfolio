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

    public void Build(GameData data, string current, Cell at, Texts texts)
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
        var area = new Control { CustomMinimumSize = new Vector2(840, 540) };
        panel.AddChild(area);
        var title = new Label { Text = texts["map.title"], Position = new Vector2(20, 10) };
        title.AddThemeFontSizeOverride("font_size", 24);
        area.AddChild(title);

        _view = new Control { Position = new Vector2(20, 54), Size = ViewSize, ClipContents = true, MouseFilter = Control.MouseFilterEnum.Stop, MouseDefaultCursorShape = Control.CursorShape.Drag };
        area.AddChild(_view);
        _view.AddChild(new ColorRect { Size = ViewSize, Color = new Color(0.094f, 0.094f, 0.094f), MouseFilter = Control.MouseFilterEnum.Ignore });
        Image image = Image.CreateFromData(_world.Width, _world.Height, false, Image.Format.Rgba8, _world.Pixels);
        _picture = new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(image),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _view.AddChild(_picture);
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
        Vector2 p = (inView - PictureAt) / Zoom;
        return _world.MapAtPixel(p.X, p.Y) is Town map ? $"{_texts.MapTitle(map)} · {_texts["map.level", map.Level]}{(map.Id == Current ? " · " + _texts["map.here"] : "")}" : "";
    }

    public string HoverText => _hover.Text;

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
        _marker.Position = PictureAt + _player * Zoom - _marker.Size / 2;
    }

    private void ShowHover(Vector2? at) => _hover.Text = at is Vector2 p ? HoverAt(p) : "";
}
