using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The world map's atlas (sprint 167, D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §5.4), drawn from
/// <see cref="WorldAtlas"/> at the world map's zoom and offset: the sea, a shallow ring and a beach along the coasts, the
/// zones in their biome's colour, the maps' water, the roads' dots, the trees' stamps, the coast's line and the zones'
/// edges. <see cref="EdgesOnly"/> draws the edges alone, laid over the plan of the cells when zoomed in.
/// </summary>
public partial class AtlasView : Control
{
    private WorldAtlas _atlas = null!;
    private readonly List<(Vector2 A, Vector2 B)> _coast = [], _edges = [];
    private readonly List<(string Zone, Vector2[] Points)> _lands = [];
    private readonly List<Vector2[]> _waters = [];

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
        foreach (IReadOnlyList<AtlasPoint> water in atlas.Waters)
        {
            Vector2[] points = [.. water.Select(p => new Vector2((float)p.X, (float)p.Y))];
            if (Geometry2D.TriangulatePolygon(points).Length > 0)
                _waters.Add(points);
        }
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
        foreach (Vector2[] water in _waters)
            DrawColoredPolygon(Screen(water), new Color(AtlasStyle.Sea));
        float dot = Mathf.Clamp(1.4f * Zoom, 1, 2.5f);
        foreach (AtlasPoint p in _atlas.Roads)
            DrawCircle(Screen(new Vector2((float)p.X, (float)p.Y)), dot, new Color(AtlasStyle.Road));
        // A stamp of trees: three crowns, a light on each, from 12 to 18 pixels.
        Color tree = new(AtlasStyle.Tree), light = new Color(AtlasStyle.Tree).Lerp(Colors.White, 0.3f);
        foreach (AtlasTree t in _atlas.Trees)
        {
            Vector2 c = Screen(new Vector2((float)t.At.X, (float)t.At.Y));
            float r = Mathf.Clamp(4 + 2 * Zoom, 4, 6) + t.Variant * 0.6f;
            foreach (Vector2 o in new[] { new Vector2(-r * 0.7f, r * 0.3f), new Vector2(r * 0.7f, r * 0.3f), new Vector2(0, -r * 0.4f) })
            {
                DrawCircle(c + o + new Vector2(1, 2), r * 0.75f, new Color(0, 0, 0, 0.3f));
                DrawCircle(c + o, r * 0.75f, tree);
                DrawCircle(c + o - new Vector2(r * 0.2f, r * 0.25f), r * 0.3f, light);
            }
        }
        foreach ((Vector2 a, Vector2 b) in _coast)
            DrawLine(Screen(a), Screen(b), new Color(AtlasStyle.Coast), 2, true);
        // Between two zones: the frame's dark brown, a faint light inside.
        foreach ((Vector2 a, Vector2 b) in _edges)
            DrawLine(Screen(a), Screen(b), new Color(UiStyle.FrameDark), width, true);
        foreach ((Vector2 a, Vector2 b) in _edges)
            DrawLine(Screen(a), Screen(b), new Color(1, 1, 1, 0.2f), 1, true);
    }
}
