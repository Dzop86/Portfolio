using System.Globalization;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>The mini-map's three sizes (sprint 108).</summary>
public enum MiniMapSize
{
    Small,
    Medium,
    Large,
}

/// <summary>What a dot of the mini-map is.</summary>
public enum MiniMarkKind
{
    Player,
    Group,
    Horde,
    Chief,
    Spot,
    Inhabitant,
    Way,
    Door,
    Workshop,
    Goal,
}

/// <summary>
/// A dot of the mini-map, where it is drawn in the view and what its legend says under the pointer: for an
/// inhabitant, its "!" or "?" (<see cref="Quest"/>); for a gathering spot, its kind and whether the trade is too low
/// (<see cref="Low"/>); for a way, the direction it leaves in and, when it leads to another zone, that zone's name
/// (<see cref="Label"/>).
/// </summary>
public sealed record MiniMark(MiniMarkKind Kind, double X, double Y, string Legend, QuestMark Quest = QuestMark.None, NodeKind? Spot = null, bool Low = false,
    Cell Direction = default, string? Label = null);

/// <summary>What the client knows of the map the player is on, for the mini-map's dots.</summary>
public sealed record MiniScene(Town Map, IReadOnlyDictionary<string, Cell> Groups, IReadOnlyCollection<string> Growing, CharacterSummary? Character = null,
    QuestBook? Book = null, TrailGoal? Goal = null);

/// <summary>
/// The mini-map's settings (sprint 108), kept between two games: shown or not, its size, its zoom and, once moved,
/// its top-left corner on the screen (none: its place by default, at the top right).
/// </summary>
public sealed record MiniMapSettings(bool Shown = true, MiniMapSize Size = MiniMapSize.Medium, int Zoom = MiniMap.DefaultZoom, int? X = null, int? Y = null)
{
    public const string FileName = "minimap.txt";

    public string ToText() => string.Create(CultureInfo.InvariantCulture,
        $"shown={(Shown ? 1 : 0)}\nsize={(int)Size}\nzoom={Zoom}\n{(X is int x && Y is int y ? $"x={x}\ny={y}\n" : "")}");

    /// <summary>The settings in <paramref name="text"/>; what is missing or unreadable keeps its default, the size and the zoom kept in their range.</summary>
    public static MiniMapSettings Parse(string? text)
    {
        var s = new MiniMapSettings();
        foreach (string line in (text ?? "").Split('\n'))
        {
            string[] kv = line.Trim().Split('=', 2);
            if (kv.Length != 2 || !int.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                continue;
            s = kv[0] switch
            {
                "shown" => s with { Shown = n != 0 },
                "size" when Enum.IsDefined((MiniMapSize)n) => s with { Size = (MiniMapSize)n },
                "zoom" => s with { Zoom = Math.Clamp(n, 0, MiniMap.Spans.Count - 1) },
                "x" => s with { X = n },
                "y" => s with { Y = n },
                _ => s,
            };
        }
        // A corner needs both its coordinates.
        return s.X is null || s.Y is null ? s with { X = null, Y = null } : s;
    }
}

/// <summary>
/// The mini-map (sprint 108, D74, docs/suite-apres-d69.md §5 ter.6) without any engine: a window of the world seen
/// from the sky (<see cref="WorldPicture"/>) centred on the player and following their steps, at one of
/// <see cref="Spans"/> (from about 16 cells across to 3 × 3 maps), in one of three sizes; the dots it shows on it
/// (<see cref="Marks"/>): the player, the groups of monsters (a horde bigger, a chief crowned), the gathering spots
/// (in their trade's colour, greyed when the trade is too low, hidden while they grow back), the inhabitants with
/// their "!" and "?", the ways to the next maps (arrows at the edge, the zone's name when it changes), the dungeons'
/// doors and the workshops, the goal of the quest followed; the legend under the pointer, and the cell a click
/// walks to.
/// </summary>
public sealed class MiniMap
{
    /// <summary>The cells across at each zoom, the tightest first: about 16 cells, a whole map (48), up to 3 maps (144).</summary>
    public static readonly IReadOnlyList<int> Spans = [16, 24, 48, 96, 144];

    /// <summary>The zoom it opens at the first time: a whole map across.</summary>
    public const int DefaultZoom = 2;

    /// <summary>A group of this many monsters or more is a horde (D63: made for several players).</summary>
    public const int Horde = 6;

    /// <summary>How near a dot the pointer must be, in pixels of the view, for its legend.</summary>
    public const double Reach = 8;

    private readonly GameData _data;
    private readonly WorldPicture _world;
    private int _zoom = DefaultZoom;

    public MiniMap(GameData data, WorldPicture? world = null)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _world = world ?? new WorldPicture(data);
    }

    public WorldPicture World => _world;

    /// <summary>The view's size in pixels, as wide as 3 maps are to 2 (a map's 48 × 32).</summary>
    public static (int Width, int Height) ViewOf(MiniMapSize size) => size switch
    {
        MiniMapSize.Small => (192, 128),
        MiniMapSize.Large => (360, 240),
        _ => (264, 176),
    };

    public MiniMapSize Size { get; set; } = MiniMapSize.Medium;

    public int Width => ViewOf(Size).Width;
    public int Height => ViewOf(Size).Height;

    /// <summary>The zoom, an index of <see cref="Spans"/>, kept in its range.</summary>
    public int Zoom
    {
        get => _zoom;
        set => _zoom = Math.Clamp(value, 0, Spans.Count - 1);
    }

    /// <summary>Closer: fewer cells across (the wheel up, the + button); false when already the closest.</summary>
    public bool ZoomIn()
    {
        int before = Zoom;
        Zoom--;
        return Zoom != before;
    }

    /// <summary>Farther: more cells across (the wheel down, the − button); false when already the farthest.</summary>
    public bool ZoomOut()
    {
        int before = Zoom;
        Zoom++;
        return Zoom != before;
    }

    /// <summary>The cells across the view at this zoom.</summary>
    public int Span => Spans[Zoom];

    /// <summary>Pixels of the view a pixel of the world's picture.</summary>
    public double Scale => (double)Width / (Span * WorldPicture.CellPixels);

    /// <summary>The map the player is on, and the pixel of the world's picture at the view's centre: the player.</summary>
    public string Map { get; private set; } = "";
    public (double X, double Y) Centre { get; private set; }

    /// <summary>
    /// Centres the view on the player, at (<paramref name="x"/>, <paramref name="y"/>) in cells of
    /// <paramref name="map"/>, fractional as they walk; a dungeon's hall, apart from the world, has no place in it.
    /// </summary>
    public void Follow(string map, double x, double y)
    {
        Map = map;
        if (_data.Towns.TryGetValue(map, out Town? t) && !t.IsHall)
        {
            (int cx, int cy) = _world.Corner(t);
            Centre = (cx + (x + 0.5) * WorldPicture.CellPixels, cy + (y + 0.5) * WorldPicture.CellPixels);
        }
    }

    /// <summary>Whether the map shown is in the world (not a dungeon's hall, which the mini-map leaves blank).</summary>
    public bool InWorld => _data.Towns.TryGetValue(Map, out Town? t) && !t.IsHall;

    /// <summary>Where a pixel of the world's picture is in the view.</summary>
    public (double X, double Y) ToView(double px, double py) => ((px - Centre.X) * Scale + Width / 2.0, (py - Centre.Y) * Scale + Height / 2.0);

    /// <summary>The pixel of the world's picture under a point of the view.</summary>
    public (double X, double Y) FromView(double vx, double vy) => ((vx - Width / 2.0) / Scale + Centre.X, (vy - Height / 2.0) / Scale + Centre.Y);

    /// <summary>The part of the world's picture the view shows: its top-left pixel and its size, in pixels of the picture.</summary>
    public (double X, double Y, double Width, double Height) Source
    {
        get
        {
            (double x, double y) = FromView(0, 0);
            return (x, y, Width / Scale, Height / Scale);
        }
    }

    /// <summary>Where a cell of a map is in the view: its centre.</summary>
    public (double X, double Y) ViewOfCell(string map, Cell cell)
    {
        (double px, double py) = _world.PixelOf(map, cell);
        return ToView(px, py);
    }

    /// <summary>The map and the cell under a point of the view, if a map is there.</summary>
    public (Town Map, Cell Cell)? CellAt(double vx, double vy)
    {
        (double px, double py) = FromView(vx, vy);
        if (_world.MapAtPixel(px, py) is not Town map)
            return null;
        (int cx, int cy) = _world.Corner(map);
        return (map, new Cell((int)((px - cx) / WorldPicture.CellPixels), (int)((py - cy) / WorldPicture.CellPixels)));
    }

    /// <summary>The cell a click on the view walks to: one of the map the player is on, never of another (nor in a hall).</summary>
    public Cell? WalkTo(double vx, double vy) => InWorld && CellAt(vx, vy) is (Town map, Cell cell) && map.Id == Map ? cell : null;

    /// <summary>The colour of a kind of gathering spot: the element of its trade (wood Fire, ore Earth, fish Water, crops Air), herbs the heal's.</summary>
    public static string SpotColour(NodeKind kind) => kind switch
    {
        NodeKind.Tree => ElementStyle.Colour(Element.Fire),
        NodeKind.Rock => ElementStyle.Colour(Element.Earth),
        NodeKind.Water => ElementStyle.Colour(Element.Water),
        NodeKind.Plot => ElementStyle.Colour(Element.Air),
        _ => ElementStyle.Heal,
    };

    /// <summary>A way's direction out of its map: towards the map it leads to, if next to it; else none.</summary>
    public Cell WayDirection(Town here, ZoneLink link)
    {
        ArgumentNullException.ThrowIfNull(here);
        ArgumentNullException.ThrowIfNull(link);
        if (!_data.Towns.TryGetValue(link.To, out Town? to) || to.IsHall)
            return default;
        var step = new Cell(to.MapAt.X - here.MapAt.X, to.MapAt.Y - here.MapAt.Y);
        return Math.Abs(step.X) + Math.Abs(step.Y) == 1 ? step : default;
    }

    /// <summary>
    /// The dots in the view, drawn in this order (the player last, over the others): the ways, doors and workshops of
    /// the player's map; the gathering spots and the inhabitants of every map in the view; the groups of the player's
    /// map; the goal of the quest followed; the player. None in a dungeon's hall.
    /// </summary>
    public IReadOnlyList<MiniMark> Marks(MiniScene scene, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(texts);
        var marks = new List<MiniMark>();
        if (!InWorld || scene.Map.Id != Map)
            return marks;
        string lang = texts.Lang;
        Town here = scene.Map;
        bool Inside((double X, double Y) v) => v.X >= 0 && v.Y >= 0 && v.X < Width && v.Y < Height;
        void Add(string map, Cell at, Func<(double X, double Y), MiniMark> make)
        {
            (double X, double Y) v = ViewOfCell(map, at);
            if (Inside(v))
                marks.Add(make(v));
        }
        // The ways out, one arrow per neighbour map at the middle of its cells.
        foreach (IGrouping<string, ZoneLink> way in (here.Links ?? []).GroupBy(l => l.To))
        {
            ZoneLink[] cells = [.. way.OrderBy(l => l.At.X).ThenBy(l => l.At.Y)];
            ZoneLink middle = cells[cells.Length / 2];
            if (!_data.Towns.TryGetValue(way.Key, out Town? to))
                continue;
            string? zone = to.ZoneId != here.ZoneId ? (middle.Name ?? to.Name).In(lang) : null;
            Add(here.Id, middle.At, v => new MiniMark(MiniMarkKind.Way, v.X, v.Y, texts.MapTitle(to), Direction: WayDirection(here, middle), Label: zone));
        }
        foreach (DungeonDoor door in here.Doors)
        {
            if (_data.Dungeons.TryGetValue(door.Dungeon, out Dungeon? d))
                Add(here.Id, door.At, v => new MiniMark(MiniMarkKind.Door, v.X, v.Y, texts["map.dungeon", d.Name.In(lang)]));
        }
        foreach (Workshop bench in here.Workshops ?? [])
        {
            if (_data.Jobs.TryGetValue(bench.Job, out Job? job))
                Add(here.Id, bench.At, v => new MiniMark(MiniMarkKind.Workshop, v.X, v.Y, (job.Workshop ?? job.Name).In(lang)));
        }
        // The maps in the view: the player's and, zoomed out, its neighbours.
        (double sx, double sy, double sw, double sh) = Source;
        foreach (Town map in _data.Towns.Values.Where(t => !t.IsHall).OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            (int cx, int cy) = _world.Corner(map);
            if (cx >= sx + sw || cy >= sy + sh || cx + map.Width * WorldPicture.CellPixels <= sx || cy + map.Rows.Count * WorldPicture.CellPixels <= sy)
                continue;
            foreach (HarvestNode node in _data.Nodes.Values.Where(n => n.Map == map.Id))
            {
                // Growing back: known on the player's map only, and hidden there.
                if (map.Id == here.Id && scene.Growing.Contains(node.Id))
                    continue;
                bool low = scene.Character is CharacterSummary c && c.JobLevel(node.Job) < node.Level;
                string legend = texts["minimap.spot", _data.Items[node.Item].Name.In(lang), _data.Jobs[node.Job].Name.In(lang), node.Level];
                Add(map.Id, node.At, v => new MiniMark(MiniMarkKind.Spot, v.X, v.Y, legend, Spot: node.Kind, Low: low));
            }
            foreach (Npc npc in map.Npcs)
            {
                QuestMark mark = scene.Book?.Mark(_data.RefOf(map, npc)) ?? QuestMark.None;
                Add(map.Id, npc.At, v => new MiniMark(MiniMarkKind.Inhabitant, v.X, v.Y, npc.Name.In(lang), Quest: mark));
            }
        }
        foreach ((string id, Cell at) in scene.Groups.OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            if (!_data.Groups.TryGetValue(id, out MonsterGroup? group))
                continue;
            MiniMarkKind kind = group.Monsters.Any(m => _data.Monsters[m].Role == MonsterRole.Boss) ? MiniMarkKind.Chief
                : group.Monsters.Count >= Horde ? MiniMarkKind.Horde : MiniMarkKind.Group;
            Add(here.Id, at, v => new MiniMark(kind, v.X, v.Y, texts.GroupLabel(group, _data)));
        }
        if (scene.Goal is TrailGoal goal && _data.Towns.TryGetValue(goal.Map, out Town? goalMap) && !goalMap.IsHall)
            Add(goal.Map, goal.At, v => new MiniMark(MiniMarkKind.Goal, v.X, v.Y, texts["minimap.goal"]));
        marks.Add(new MiniMark(MiniMarkKind.Player, Width / 2.0, Height / 2.0, texts["minimap.you"]));
        return marks;
    }

    /// <summary>
    /// The legend under a point of the view: the nearest dot within <see cref="Reach"/> pixels (the one drawn on top
    /// on a tie), else the map's name; empty off the maps.
    /// </summary>
    public string Legend(IReadOnlyList<MiniMark> marks, double vx, double vy, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(marks);
        ArgumentNullException.ThrowIfNull(texts);
        MiniMark? best = null;
        double nearest = Reach * Reach;
        for (int i = marks.Count - 1; i >= 0; i--)
        {
            double dx = marks[i].X - vx, dy = marks[i].Y - vy, d = dx * dx + dy * dy;
            if (d < nearest || (best is null && d <= nearest))
                (best, nearest) = (marks[i], d);
        }
        if (best is not null)
            return best.Legend;
        return CellAt(vx, vy) is (Town map, _) ? texts.MapTitle(map) : "";
    }
}
