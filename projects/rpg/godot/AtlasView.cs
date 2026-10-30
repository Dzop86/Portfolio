using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The world map's atlas (sprint 167, D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §5.4), drawn from
/// <see cref="WorldAtlas"/> at the world map's zoom and offset: the sea, a shallow ring and a beach along the coasts, the
/// zones in their biome's colour, then since sprint 169 (D88) each map's photograph from the sky (<see cref="MapPhotos"/>:
/// its real trees, houses, water and rocks, its sides without a neighbour faded into the biome), the coast's line and the
/// zones' edges. <see cref="EdgesOnly"/> draws the edges alone, laid over the plan of the cells when zoomed in.
/// </summary>
public partial class AtlasView : Control
{
    private WorldAtlas _atlas = null!;
    private readonly List<(Vector2 A, Vector2 B)> _coast = [], _edges = [];
    private readonly List<(string Zone, Vector2[] Points)> _lands = [];
    private readonly List<(Rect2 At, Texture2D Photo, Color Tint)> _photos = [];
    private readonly List<Vector2[]> _forests = [], _waters = [];

    /// <summary>The zooms the photographs start to show at and cover the drawn atlas from (Charles, sprint 169).</summary>
    public const float PhotosFrom = 0.9f, PhotosFull = 1.3f;

    /// <summary>How much the photographs show at the zoom drawn, 0 to 1.</summary>
    public float PhotoOpacity => Mathf.Clamp((Zoom - PhotosFrom) / (PhotosFull - PhotosFrom), 0, 1);

    /// <summary>The maps' photographs drawn (for the self-test).</summary>
    public int Photos => _photos.Count;

    /// <summary>The picture's pixels per screen pixel and where its top-left corner is, as the world map says.</summary>
    public float Zoom { get; set; } = 1;
    public Vector2 Offset { get; set; }

    /// <summary>The zones' edges alone, at this opacity (over the plan).</summary>
    public bool EdgesOnly { get; set; }

    /// <summary>The lands that would not draw (a broken outline), for the self-test.</summary>
    public int Broken { get; private set; }

    public int Lands => _lands.Count;

    public void Build(WorldAtlas atlas)
    {
        _atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
        foreach (AtlasRegion region in atlas.Regions)
            foreach (IReadOnlyList<AtlasPoint> outline in region.Outlines)
            {
                Vector2[] points = [.. outline.Select(p => new Vector2((float)p.X, (float)p.Y))];
                if (Geometry2D.TriangulatePolygon(points).Length == 0)
                    Broken++;
                _lands.Add((region.Zone, points));
                // Each piece of the outline: a coast where the sea is on one side, else an edge between two zones (drawn once).
                for (int i = 0; i < points.Length; i++)
                {
                    Vector2 a = points[i], b = points[(i + 1) % points.Length];
                    Vector2 normal = (b - a).Orthogonal().Normalized() * 10, m = (a + b) / 2;
                    string? left = atlas.ZoneAt(m.X + normal.X, m.Y + normal.Y), right = atlas.ZoneAt(m.X - normal.X, m.Y - normal.Y);
                    if (left is null || right is null)
                        _coast.Add((a, b));
                    else if (string.CompareOrdinal(region.Zone, left == region.Zone ? right : left) < 0)
                        _edges.Add((a, b));
                }
            }
        if (EdgesOnly)
            return;
        foreach (Town map in MapPhotos.World(GameData.Embedded))
        {
            string path = $"res://{MapPhotos.Folder}/{MapPhotos.File(map.Id)}";
            if (!ResourceLoader.Exists(path))
                continue;
            (double l, double t, double r, double b) = atlas.Bounds(map);
            // A little of its zone's colour, so that the maps of a zone read as one.
            _photos.Add((new Rect2((float)l, (float)t, (float)(r - l), (float)(b - t)), GD.Load<Texture2D>(path), Colors.White.Lerp(new Color(AtlasStyle.Biome(WorldAtlas.Zone(map))), 0.2f)));
        }
        foreach (IReadOnlyList<AtlasPoint> outline in atlas.Forests)
            _forests.Add([.. outline.Select(p => new Vector2((float)p.X, (float)p.Y))]);
        foreach (IReadOnlyList<AtlasPoint> outline in atlas.Waters)
            _waters.Add([.. outline.Select(p => new Vector2((float)p.X, (float)p.Y))]);
    }

    private Vector2 Screen(Vector2 p) => Offset + p * Zoom;

    private Vector2[] Screen(Vector2[] points) => [.. points.Select(Screen)];

    public override void _Draw()
    {
        if (_atlas is null)
            return;
        float width = Mathf.Clamp(3 * Zoom, 1.5f, 4);
        if (EdgesOnly)
        {
            foreach ((Vector2 a, Vector2 b) in _edges)
                DrawLine(Screen(a), Screen(b), new Color(UiStyle.FrameDark), width, true);
            return;
        }
        // The open sea, darker away from the land.
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(AtlasStyle.SeaDeep));
        foreach ((_, Vector2[] land) in _lands)
            DrawPolyline([.. Screen(land), Screen(land[0])], new Color(AtlasStyle.Sea), Mathf.Max(24, 160 * Zoom), true);
        // The shallows along the coasts, then the beach.
        Color shallow = new Color(AtlasStyle.Sea).Lerp(Colors.White, 0.25f);
        foreach ((Vector2 a, Vector2 b) in _coast)
            DrawLine(Screen(a), Screen(b), shallow, Mathf.Max(8, 48 * Zoom), true);
        foreach ((Vector2 a, Vector2 b) in _coast)
            DrawLine(Screen(a), Screen(b), new Color(AtlasStyle.Shore), Mathf.Max(3, 16 * Zoom), true);
        foreach ((string zone, Vector2[] land) in _lands)
            DrawColoredPolygon(Screen(land), new Color(AtlasStyle.Biome(zone)));
        // Zoomed out, the drawn atlas: the woods as masses edged with crowns, the pools, the roads' dots.
        Color wood = new(AtlasStyle.Tree), crown = new Color(AtlasStyle.Tree).Lerp(Colors.White, 0.22f);
        float r = Mathf.Clamp(5 * Zoom, 1.5f, 6);
        foreach (Vector2[] forest in _forests)
        {
            Vector2[] shown = Screen(forest);
            if (Geometry2D.TriangulatePolygon(shown).Length > 0)
                DrawColoredPolygon(shown, wood);
            for (int i = 0; i < shown.Length; i += 2)
            {
                DrawCircle(shown[i], r, wood);
                DrawCircle(shown[i] - new Vector2(r * 0.3f, r * 0.35f), r * 0.45f, crown);
            }
        }
        foreach (Vector2[] water in _waters)
        {
            Vector2[] shown = Screen(water);
            if (Geometry2D.TriangulatePolygon(shown).Length > 0)
                DrawColoredPolygon(shown, new Color(AtlasStyle.Sea));
            DrawPolyline([.. shown, shown[0]], new Color(AtlasStyle.Shore), Mathf.Clamp(2 * Zoom, 1, 2), true);
        }
        float dot = Mathf.Clamp(1.4f * Zoom, 1, 2.5f);
        foreach (AtlasPoint p in _atlas.Roads)
            DrawCircle(Screen(new Vector2((float)p.X, (float)p.Y)), dot, new Color(AtlasStyle.Road));
        // Closer, each map as the sky sees it (sprint 169, D88): its photograph, faded where the sea begins.
        float photos = PhotoOpacity;
        if (photos > 0)
            foreach ((Rect2 at, Texture2D photo, Color tint) in _photos)
                DrawTextureRect(photo, new Rect2(Screen(at.Position), at.Size * Zoom), false, new Color(tint, photos));
        foreach ((Vector2 a, Vector2 b) in _coast)
            DrawLine(Screen(a), Screen(b), new Color(AtlasStyle.Coast), 2, true);
        // Between two zones: the frame's dark brown, a faint light inside.
        foreach ((Vector2 a, Vector2 b) in _edges)
            DrawLine(Screen(a), Screen(b), new Color(UiStyle.FrameDark), width, true);
        foreach ((Vector2 a, Vector2 b) in _edges)
            DrawLine(Screen(a), Screen(b), new Color(1, 1, 1, 0.2f), 1, true);
    }
}
