using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The world map, over the map the player is on (sprint 71): the world seen from the sky, every map in
/// its place as one big map (<see cref="WorldPicture"/>), dragged with the mouse and zoomed with the
/// wheel around the pointer; a ring marks the player, the map under the pointer is named below with
/// its level. M or its icon opens and closes it. Since sprint 167 (D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §5)
/// an illustrated atlas zoomed out (<see cref="AtlasView"/>), the plan of the cells fading in over it from
/// <see cref="PlanFrom"/> to <see cref="PlanFull"/>, the zones' edges kept over the plan; since sprint 168 the theme's
/// window, its landmarks in relief (cities, dungeons, waystones, workshops from <see cref="WorkshopsFrom"/>, the player, the
/// quest followed), the zones' names zoomed out, a legend that folds and hides kinds of landmarks, buttons to zoom.
/// </summary>
public partial class WorldMapPanel : CanvasLayer
{
    private static readonly Vector2 ViewSize = new(928, 492);
    private const float MaxZoom = 4;

    /// <summary>The zooms the plan of the cells starts to show at, and covers the atlas from.</summary>
    public const float PlanFrom = 1.5f, PlanFull = 2.5f;

    /// <summary>The zoom the workshops show from: they would crowd the cities zoomed out.</summary>
    public const float WorkshopsFrom = 2f;

    private WorldPicture _world = null!;
    private WorldAtlas _atlasData = null!;
    private Texts _texts = null!;
    private Control _view = null!;
    private TextureRect _picture = null!;
    private AtlasView _atlas = null!, _edges = null!;
    private Control _marker = null!;
    private Label _hover = null!;
    private Vector2 _player;
    private bool _dragging;

    public event Action? ClosePressed;

    public string Current { get; private set; } = "";

    /// <summary>The picture's pixels per screen pixel, and where its top-left corner is in the view.</summary>
    public float Zoom { get; private set; } = 1.5f;
    public Vector2 PictureAt { get; private set; }

    /// <summary>The smallest zoom: the whole world in its sea, at half the size that would fill the view (Charles, sprint 168).</summary>
    public float MinZoom => 0.5f * Math.Min(ViewSize.X / _world.Width, ViewSize.Y / _world.Height);

    public Vector2I PictureSize => new(_world.Width, _world.Height);

    /// <summary>Where the player's marker points, in the view.</summary>
    public Vector2 MarkerCentre => _marker.Position + new Vector2(_marker.Size.X / 2, _marker.Size.Y);

    /// <summary>How much the plan of the cells shows over the atlas, 0 to 1 (for the self-test).</summary>
    public float PlanOpacity => _picture.Modulate.A;

    /// <summary>The atlas's lands drawn, and those that would not draw (for the self-test).</summary>
    public int AtlasLands => _atlas.Lands;
    public int AtlasBroken => _atlas.Broken;

    /// <summary>The maps' photographs on the atlas (for the self-test).</summary>
    public int AtlasPhotos => _atlas.Photos;

    /// <summary>How much the photographs show over the drawn atlas, 0 to 1 (for the self-test).</summary>
    public float PhotoOpacity => _atlas.PhotoOpacity;

    // The place of the quest followed (sprint 106): a ring in the hints' yellow, if any.
    private Control? _goal;
    private Vector2 _goalPixel;

    /// <summary>Where the ring of the quest followed is in the view, if any (for the self-test).</summary>
    public Vector2? GoalCentre => _goal is null ? null : _goal.Position + _goal.Size / 2;

    private readonly List<(AtlasMark Mark, Vector2 Pixel, Control Icon)> _marks = [];
    private readonly List<(Label Name, Vector2 Pixel)> _names = [];

    /// <summary>Which kinds of landmarks are shown and whether the legend is folded, for every opening until changed.</summary>
    public static readonly Dictionary<string, bool> Shown = new(StringComparer.Ordinal) { ["dungeons"] = true, ["waystones"] = true, ["workshops"] = true, ["quest"] = true };
    public static bool LegendFolded { get; set; }

    /// <summary>The legend's boxes, by what they show (for the self-test).</summary>
    public Dictionary<string, CheckBox> LegendBoxes { get; } = [];

    public void Build(GameData data, string current, Cell at, Texts texts, TrailGoal? goal = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        Current = current;
        _world = new WorldPicture(data);
        _atlasData = new WorldAtlas(data);
        (double px, double py) = _world.PixelOf(current, at);
        _player = new Vector2((float)px, (float)py);

        // The theme's window, 960 × 600: its title bar, the view, the status bar.
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -480, OffsetRight = 480, OffsetTop = -300, OffsetBottom = 300, ThemeTypeVariation = OsmoseTheme.WindowPanel };
        AddChild(panel);
        WindowDrag.Make(panel, "map");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 0);
        panel.AddChild(column);
        column.AddChild(TitleBar(texts));
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        column.AddChild(margin);
        var area = new Control { CustomMinimumSize = ViewSize };
        margin.AddChild(area);

        _view = new Control { Size = ViewSize, ClipContents = true, MouseFilter = Control.MouseFilterEnum.Stop, MouseDefaultCursorShape = Control.CursorShape.Drag };
        area.AddChild(_view);
        // The atlas, the sea all around (sprint 167).
        _atlas = new AtlasView { Size = ViewSize, MouseFilter = Control.MouseFilterEnum.Ignore, TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps };
        _atlas.Build(_atlasData);
        _view.AddChild(_atlas);
        // The plan of the cells over it, where no map is see-through. The image is copied into the texture, then let go
        // at once (see Main.QuitCleanly).
        byte[] pixels = (byte[])_world.Pixels.Clone();
        for (int i = 0; i < pixels.Length; i += 4)
            if (pixels[i] == WorldPicture.Void.R && pixels[i + 1] == WorldPicture.Void.G && pixels[i + 2] == WorldPicture.Void.B)
                pixels[i + 3] = 0;
        using Image image = Image.CreateFromData(_world.Width, _world.Height, false, Image.Format.Rgba8, pixels);
        _picture = new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(image),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _view.AddChild(_picture);
        _edges = new AtlasView { Size = ViewSize, MouseFilter = Control.MouseFilterEnum.Ignore, EdgesOnly = true };
        _edges.Build(_atlasData);
        _view.AddChild(_edges);
        // A light grid on the maps' edges (sprint 85, D67), shown or not from the legend, the choice kept.
        _grid = new MapGrid { MouseFilter = Control.MouseFilterEnum.Ignore, Cell = new Vector2(_world.MapWidth, _world.MapHeight) * WorldPicture.CellPixels, Visible = ShowGrid };
        _view.AddChild(_grid);
        // The zones' names zoomed out (sprint 168, s165 §5.5): bold capitals of parchment edged in black, their levels under.
        foreach (AtlasRegion region in _atlasData.Regions)
        {
            var name = new Label { Text = texts.ZoneName(region.Zone, data).ToUpperInvariant() + "\n" + (region.MinLevel == region.MaxLevel ? texts["map.levels.one", region.MinLevel] : texts["map.levels", region.MinLevel, region.MaxLevel]), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
            name.AddThemeFontOverride("font", OsmoseTheme.Bold(2));
            name.AddThemeFontSizeOverride("font_size", 16);
            name.AddThemeColorOverride("font_color", new Color(AtlasStyle.Parchment));
            name.AddThemeColorOverride("font_outline_color", new Color(AtlasStyle.Ink));
            name.AddThemeConstantOverride("outline_size", 4);
            _view.AddChild(name);
            _names.Add((name, new Vector2((float)region.Centre.X, (float)region.Centre.Y)));
        }
        // The landmarks in relief, the same size at every zoom.
        foreach (AtlasMark mark in _atlasData.Marks)
        {
            string icon = mark.Kind switch
            {
                AtlasMarkKind.City => "map-city",
                AtlasMarkKind.Dungeon => "dungeons",
                AtlasMarkKind.Waystone => "map-waystone",
                _ => "quest-trade",
            };
            var picture = new TextureRect { Texture = GD.Load<Texture2D>($"res://assets/icons/relief/{icon}.svg"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, Size = new Vector2(28, 28), MouseFilter = Control.MouseFilterEnum.Ignore };
            _view.AddChild(picture);
            _marks.Add((mark, new Vector2((float)mark.At.X, (float)mark.At.Y), picture));
        }
        if (goal is not null && data.Towns.TryGetValue(goal.Map, out Town? goalMap) && !goalMap.IsHall)
        {
            (double gx, double gy) = _world.PixelOf(goal.Map, goal.At);
            _goalPixel = new Vector2((float)gx, (float)gy);
            _goal = new Panel { Size = new Vector2(26, 26), MouseFilter = Control.MouseFilterEnum.Ignore };
            _goal.AddThemeStyleboxOverride("panel", OsmoseTheme.Flat("#00000000", UiStyle.Cost, 4, 13));
            _view.AddChild(_goal);
        }
        // The player: their marker in pistachio ("you"), pointing at where they stand.
        _marker = new TextureRect { Texture = GD.Load<Texture2D>("res://assets/icons/relief/map-player.svg"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, Size = new Vector2(32, 32), MouseFilter = Control.MouseFilterEnum.Ignore };
        _view.AddChild(_marker);
        area.AddChild(Legend(texts));
        // The buttons to zoom, bottom right of the view.
        var zoom = new HBoxContainer { Position = ViewSize - new Vector2(96, 52) };
        zoom.AddThemeConstantOverride("separation", 4);
        foreach ((string text, string tip, float factor) in new[] { ("−", "map.zoom-out", 0.8f), ("+", "map.zoom-in", 1.25f) })
        {
            var b = new Button { Text = text, TooltipText = texts[tip], CustomMinimumSize = new Vector2(44, 44), FocusMode = Control.FocusModeEnum.None };
            b.Pressed += () => ZoomAround(ViewSize / 2, factor);
            zoom.AddChild(b);
        }
        area.AddChild(zoom);
        _view.GuiInput += OnInput;
        _view.MouseExited += () => ShowHover(null);

        // The status bar: the map or the landmark under the pointer.
        var status = new MarginContainer { CustomMinimumSize = new Vector2(0, 32) };
        foreach (string side in new[] { "left", "right" })
            status.AddThemeConstantOverride("margin_" + side, 16);
        status.AddThemeConstantOverride("margin_bottom", 8);
        _hover = new Label { VerticalAlignment = VerticalAlignment.Center };
        _hover.AddThemeFontSizeOverride("font_size", 14);
        _hover.AddThemeColorOverride("font_color", new Color(UiStyle.Text));
        status.AddChild(_hover);
        column.AddChild(status);
        // Opened on the player, zoomed out on the atlas: their zone and its neighbours.
        Zoom = Math.Max(0.5f, MinZoom);
        PictureAt = ViewSize / 2 - _player * Zoom;
        Place();
        // Opened: the player's map, whatever landmark stands by them.
        _hover.Text = MapText(MarkerCentre);
    }

    private PanelContainer TitleBar(Texts texts)
    {
        var bar = new PanelContainer { ThemeTypeVariation = OsmoseTheme.TitleBar, CustomMinimumSize = new Vector2(0, 44) };
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        bar.AddChild(row);
        row.AddChild(new TextureRect { Texture = GD.Load<Texture2D>("res://assets/icons/bar/map.svg"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, CustomMinimumSize = new Vector2(24, 24), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        var title = new Label { Text = texts["map.title"], VerticalAlignment = VerticalAlignment.Center, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        title.AddThemeFontOverride("font", OsmoseTheme.Bold());
        title.AddThemeFontSizeOverride("font_size", 18);
        row.AddChild(title);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44), ThemeTypeVariation = OsmoseTheme.CloseButton, FocusMode = Control.FocusModeEnum.None };
        close.Pressed += () => ClosePressed?.Invoke();
        row.AddChild(close);
        return bar;
    }

    /// <summary>The legend, top right of the view: a header that folds it, a box for each kind of landmark and for the grid.</summary>
    private PanelContainer Legend(Texts texts)
    {
        var legend = new PanelContainer { Position = new Vector2(ViewSize.X - 212, 12), CustomMinimumSize = new Vector2(200, 0), ThemeTypeVariation = OsmoseTheme.HudPanel };
        legend.AddThemeStyleboxOverride("panel", HudHeader.Panel(0, 8));
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 0);
        legend.AddChild(column);
        var header = new HudHeader();
        header.Build(GD.Load<Texture2D>("res://assets/ui/grid.svg"));
        header.Title = texts["map.legend"].ToUpperInvariant();
        column.AddChild(header);
        var boxes = new VBoxContainer();
        column.AddChild(boxes);
        foreach (string kind in new[] { "dungeons", "waystones", "workshops", "quest" })
        {
            var box = new CheckBox { Text = texts["map." + kind], ButtonPressed = Shown[kind], CustomMinimumSize = new Vector2(0, 36), FocusMode = Control.FocusModeEnum.None };
            box.Toggled += on =>
            {
                Shown[kind] = on;
                Place();
            };
            boxes.AddChild(box);
            LegendBoxes[kind] = box;
        }
        var grid = new CheckBox { Text = texts["map.grid"], ButtonPressed = ShowGrid, CustomMinimumSize = new Vector2(0, 36), FocusMode = Control.FocusModeEnum.None };
        grid.Toggled += on =>
        {
            ShowGrid = on;
            _grid.Visible = on;
        };
        boxes.AddChild(grid);
        LegendBoxes["grid"] = grid;
        void Fold(bool folded)
        {
            LegendFolded = folded;
            header.Show(folded, texts["map.legend.fold"], texts["map.legend.unfold"]);
            boxes.Visible = !folded;
            legend.AddThemeStyleboxOverride("panel", HudHeader.Panel(0, folded ? 0 : 8));
            legend.Size = Vector2.Zero;
        }
        header.Pressed += () => Fold(!header.Folded);
        Fold(LegendFolded);
        return legend;
    }

    /// <summary>The map or the landmark under a point of the view, as the status bar names it; empty off the maps.</summary>
    public string HoverAt(Vector2 inView)
    {
        // A landmark under the pointer: its name first.
        if (_marks.FirstOrDefault(m => m.Icon.Visible && (m.Icon.Position + m.Icon.Size / 2).DistanceTo(inView) <= 14) is { Icon: not null } hit)
        {
            AtlasMark mark = hit.Mark;
            return mark.Kind switch
            {
                AtlasMarkKind.Dungeon => _texts["map.dungeon", GameData.Embedded.Dungeons[mark.Id].Name.In(_texts.Lang)],
                AtlasMarkKind.Waystone => _texts["map.waystone", _texts.MapTitle(GameData.Embedded.Towns[mark.Map])],
                AtlasMarkKind.Workshop => _texts["map.workshop", GameData.Embedded.Jobs[mark.Id].Name.In(_texts.Lang)],
                _ => _texts.ZoneName(mark.Id, GameData.Embedded),
            };
        }
        return MapText(inView);
    }

    /// <summary>The map under a point of the view, with its level and "you are here" on the player's; empty off the maps.</summary>
    public string MapText(Vector2 inView)
    {
        Vector2 p = (inView - PictureAt) / Zoom;
        return _world.MapAtPixel(p.X, p.Y) is Town map ? $"{_texts.MapTitle(map)} · {_texts["map.level", map.Level]}{(map.Id == Current ? " · " + _texts["map.here"] : "")}" : "";
    }

    public string HoverText => _hover.Text;

    private MapGrid _grid = null!;

    /// <summary>Whether the grid of the maps is shown, for every opening of the world map until changed.</summary>
    public static bool ShowGrid { get; set; } = true;

    public bool GridShown => _grid.Visible;

    /// <summary>The dungeons' doors on the map and where they are drawn in the view (for the self-test).</summary>
    public IEnumerable<(string Dungeon, Vector2 Centre)> Doors => _marks.Where(m => m.Mark.Kind == AtlasMarkKind.Dungeon && m.Icon.Visible).Select(m => (m.Mark.Id, m.Icon.Position + m.Icon.Size / 2));

    /// <summary>The landmarks shown now, by kind (for the self-test).</summary>
    public int MarksShown(AtlasMarkKind kind) => _marks.Count(m => m.Mark.Kind == kind && m.Icon.Visible);

    /// <summary>How much the zones' names show, 0 to 1 (for the self-test): gone once the plan covers the atlas.</summary>
    public float NamesOpacity => _names.Count == 0 ? 0 : _names[0].Name.Modulate.A;

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
        // The atlas alone zoomed out, the plan fading in over it, then covering it but for the zones' edges.
        float plan = Math.Clamp((Zoom - PlanFrom) / (PlanFull - PlanFrom), 0, 1);
        _picture.Modulate = new Color(1, 1, 1, plan);
        foreach (AtlasView layer in new[] { _atlas, _edges })
        {
            (layer.Zoom, layer.Offset) = (Zoom, PictureAt);
            layer.QueueRedraw();
        }
        _edges.Modulate = new Color(1, 1, 1, plan);
        _grid.Position = PictureAt;
        _grid.Size = size;
        _grid.Zoom = Zoom;
        _grid.QueueRedraw();
        // The zones' names fade as the plan comes.
        foreach ((Label name, Vector2 pixel) in _names)
        {
            name.Size = Vector2.Zero;
            name.Position = PictureAt + pixel * Zoom - name.Size / 2;
            name.Modulate = new Color(1, 1, 1, 1 - plan);
            name.Visible = plan < 1;
        }
        foreach ((AtlasMark mark, Vector2 pixel, Control icon) in _marks)
        {
            icon.Position = PictureAt + pixel * Zoom - icon.Size / 2;
            icon.Visible = mark.Kind switch
            {
                AtlasMarkKind.Dungeon => Shown["dungeons"],
                AtlasMarkKind.Waystone => Shown["waystones"],
                AtlasMarkKind.Workshop => Shown["workshops"] && Zoom >= WorkshopsFrom,
                _ => true,
            };
        }
        // The marker's point on where the player stands.
        _marker.Position = PictureAt + _player * Zoom - new Vector2(_marker.Size.X / 2, _marker.Size.Y);
        if (_goal is not null)
        {
            _goal.Position = PictureAt + _goalPixel * Zoom - _goal.Size / 2;
            _goal.Visible = Shown["quest"];
        }
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
