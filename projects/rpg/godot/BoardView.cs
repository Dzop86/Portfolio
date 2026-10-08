using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The board in 3D: cell (x, y) is the unit square centred on (x, 0, y). Grass for the floor, trees
/// and rocks for obstacles, water for holes; one flat overlay per floor cell shows the preview.
/// </summary>
public partial class BoardView : Node3D
{
    // Two greens in a checkerboard, so that cells can be counted at a glance.
    private static readonly Color[] Grass = [new("37602a"), new("406c31")];
    private readonly Dictionary<Cell, MeshInstance3D> _overlays = [];
    private readonly Dictionary<string, StandardMaterial3D> _marks = [];

    public static Vector3 ToWorld(Cell c) => new(c.X, 0, c.Y);

    public int Tiles { get; private set; }

    public void Build(Board board)
    {
        StandardMaterial3D[] grass = [.. Grass.Select(g => new StandardMaterial3D { AlbedoColor = g, Roughness = 1 })];
        foreach (string name in new[] { "reach", "path", "range", "targetable", "target" })
            _marks[name] = Mark(name);
        foreach (Cell c in board.Cells())
        {
            Terrain t = board[c];
            Node3D tile = Looks.Nature(t == Terrain.Hole ? "ground_riverTile" : "ground_grass").Instantiate<Node3D>();
            tile.Position = ToWorld(c);
            if (t != Terrain.Hole)
                Paint(tile, grass[(c.X + c.Y) % 2]);
            AddChild(tile);
            Tiles++;
            if (t == Terrain.Obstacle)
                Place(Looks.Obstacle(c.X, c.Y), c, Vector3.Zero);
            else if (t == Terrain.Floor && Looks.Decoration(c.X, c.Y) is string deco)
                Place(deco, c, new Vector3(0.3f, 0, -0.3f));
            if (t == Terrain.Floor)
            {
                var overlay = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(0.9f, 0.9f) }, Visible = false };
                overlay.Position = ToWorld(c) + new Vector3(0, 0.02f, 0);
                AddChild(overlay);
                _overlays[c] = overlay;
            }
        }
    }

    /// <summary>Shows a preview; the strongest mark wins on a cell (target, then path, then the others).</summary>
    public void Show(Preview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var path = preview.Path.ToHashSet();
        foreach ((Cell c, MeshInstance3D overlay) in _overlays)
        {
            string? mark = c == preview.Target ? "target"
                : path.Contains(c) ? "path"
                : preview.Targetable.Contains(c) ? "targetable"
                : preview.InRange.Contains(c) ? "range"
                : preview.Reachable.Contains(c) ? "reach"
                : null;
            overlay.Visible = mark is not null;
            if (mark is not null)
                overlay.MaterialOverride = _marks[mark];
        }
    }

    /// <summary>The marks shown, by kind: what the self-test checks against the preview.</summary>
    public IReadOnlyDictionary<Cell, string> Shown() =>
        _overlays.Where(o => o.Value.Visible).ToDictionary(o => o.Key, o => _marks.First(m => m.Value == o.Value.MaterialOverride).Key);

    /// <summary>The cell under a point of the screen, found where the camera's ray meets the ground.</summary>
    public static Cell? Pick(Camera3D camera, Vector2 screen)
    {
        ArgumentNullException.ThrowIfNull(camera);
        Vector3 from = camera.ProjectRayOrigin(screen), dir = camera.ProjectRayNormal(screen);
        if (Mathf.IsZeroApprox(dir.Y))
            return null;
        Vector3 hit = from - dir * (from.Y / dir.Y);
        return new Cell(Mathf.RoundToInt(hit.X), Mathf.RoundToInt(hit.Z));
    }

    private void Place(string model, Cell c, Vector3 offset)
    {
        Node3D prop = Looks.Nature(model).Instantiate<Node3D>();
        prop.Position = ToWorld(c) + offset;
        AddChild(prop);
    }

    private static void Paint(Node node, Material material)
    {
        if (node is MeshInstance3D mesh)
            mesh.MaterialOverride = material;
        foreach (Node child in node.GetChildren())
            Paint(child, material);
    }

    private static StandardMaterial3D Mark(string name) => new()
    {
        AlbedoColor = name switch
        {
            "reach" => new Color(0.55f, 0.85f, 1f, 0.45f),
            "path" => new Color(1f, 0.9f, 0.35f, 0.8f),
            "range" => new Color(1f, 1f, 1f, 0.18f),
            "targetable" => new Color(0.4f, 0.6f, 1f, 0.55f),
            _ => new Color(1f, 0.3f, 0.2f, 0.85f),
        },
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
    };
}
