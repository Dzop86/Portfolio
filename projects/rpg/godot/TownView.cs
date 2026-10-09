using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A town drawn from Rpg.Client's <see cref="TownController"/>: grass, roads and water, houses built
/// from Kenney's Fantasy Town Kit, its people with their names above them, the player walking, the
/// path under the mouse and the gate. The talks are drawn by <see cref="TalkPanel"/>.
/// </summary>
public partial class TownView : Node3D
{
    private const string Kit = "res://assets/kenney/town/";
    private static readonly Color[] Grass = [new("37602a"), new("406c31")];

    private readonly List<MeshInstance3D> _pathMarks = [];
    private readonly List<(Label3D Label, LocalizedText Text)> _exitLabels = [];
    private StandardMaterial3D _pathMaterial = null!;
    private AnimationPlayer? _anim;

    public TownController Controller { get; private set; } = null!;
    public Node3D Player { get; private set; } = null!;
    public Dictionary<string, Node3D> People { get; } = [];

    public void Build(TownController controller, string lang, Hero player)
    {
        Controller = controller;
        Town town = controller.Town;
        StandardMaterial3D[] grass = [.. Grass.Select(g => new StandardMaterial3D { AlbedoColor = g, Roughness = 1 })];
        foreach (Cell c in controller.Board.Cells())
        {
            char k = town[c];
            Node3D ground = k switch
            {
                '=' => Model(Kit + "road.glb"),
                '~' => Looks.Nature("ground_riverTile").Instantiate<Node3D>(),
                _ => Looks.Nature("ground_grass").Instantiate<Node3D>(),
            };
            ground.Position = BoardView.ToWorld(c);
            if (k is not '=' and not '~')
                BoardView.Paint(ground, grass[(c.X + c.Y) % 2]);
            AddChild(ground);
            Decorate(c, k);
        }
        foreach (Npc npc in town.Npcs)
        {
            Node3D person = Person(new Hero("", npc.Look, null, npc.Colour), npc.Name.In(lang));
            person.Position = BoardView.ToWorld(npc.At);
            AddChild(person);
            People[npc.Id] = person;
        }
        foreach (TownExit exit in town.Exits)
        {
            AddChild(new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.32f, OuterRadius = 0.45f, Rings = 24 },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = FighterView.EnemyColour, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
                Scale = new Vector3(1, 0.15f, 1),
                Position = BoardView.ToWorld(exit.At) + new Vector3(0, 0.05f, 0),
            });
            Label3D label = Label(exit.Name.In(lang), BoardView.ToWorld(exit.At) + new Vector3(0, 0.7f, 0), 36);
            AddChild(label);
            _exitLabels.Add((label, exit.Name));
        }
        Player = Person(player, null);
        _anim = Player.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        Player.Position = BoardView.ToWorld(controller.Position);
        AddChild(Player);
        _pathMaterial = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.9f, 0.35f, 0.8f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
    }

    /// <summary>What stands on a cell: houses and the fountain once, from their top-left corner.</summary>
    private void Decorate(Cell c, char k)
    {
        Vector3 at = BoardView.ToWorld(c);
        Vector3 block = at + new Vector3(0.5f, 0, 0.5f);
        switch (k)
        {
            case 'H':
                // Two by two cells: the door faces south, windows east and west, a pointed roof.
                foreach ((string piece, float turn) in new[] { ("wall-door", -90f), ("wall-window-shutters", 0f), ("wall", 90f), ("wall-window-shutters", 180f) })
                    Place(Kit + piece + ".glb", block, turn, 2);
                Place(Kit + "roof-point.glb", block + new Vector3(0, 2, 0), 0, 2);
                break;
            case 'F':
                Place(Kit + "fountain-round.glb", block, 0, 1);
                break;
            case 'T':
                Place(Kit + ((c.X + c.Y) % 2 == 0 ? "tree.glb" : "tree-high.glb"), at, 0, 1);
                break;
            case 'R':
                Place(Kit + "rock-large.glb", at, (c.X * 37) % 360, 0.6f);
                break;
            case 'S':
                Place(Kit + ((c.X + c.Y) % 2 == 0 ? "stall-red.glb" : "stall-green.glb"), at, -90, 1);
                break;
            case 'L':
                Place(Kit + "lantern.glb", at, 0, 1);
                break;
            case 'C':
                Place(Kit + "cart.glb", at, 30, 1);
                break;
        }
    }

    private void Place(string path, Vector3 at, float turn, float scale)
    {
        Node3D model = Model(path);
        model.Position = at;
        model.RotationDegrees = new Vector3(0, turn, 0);
        model.Scale = new Vector3(scale, scale, scale);
        AddChild(model);
    }

    private static Node3D Model(string path) => GD.Load<PackedScene>(path).Instantiate<Node3D>();

    /// <summary>A character model facing the viewer, idle, its outfit painted, a name above it (or none).</summary>
    private static Node3D Person(Hero appearance, string? name)
    {
        // A little larger than on the fighting board, next to houses two cells wide.
        var root = new Node3D { Scale = new Vector3(1.5f, 1.5f, 1.5f) };
        Node3D model = Looks.Fighter(appearance.Look).Instantiate<Node3D>();
        Looks.Paint(model, appearance);
        model.RotationDegrees = new Vector3(0, 45, 0);
        root.AddChild(model);
        if (model.FindChild("AnimationPlayer", true, false) is AnimationPlayer anim && anim.HasAnimation("idle"))
        {
            foreach (string loop in new[] { "idle", "walk" })
            {
                if (anim.HasAnimation(loop))
                    anim.GetAnimation(loop).LoopMode = Animation.LoopModeEnum.Linear;
            }
            anim.Play("idle");
        }
        if (name is not null)
            root.AddChild(Label(name, new Vector3(0, 1.0f, 0), 40));
        return root;
    }

    private static Label3D Label(string text, Vector3 at, int size) => new()
    {
        Text = text,
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        NoDepthTest = true,
        FontSize = size,
        OutlineSize = 10,
        PixelSize = 0.009f,
        Position = at,
    };

    /// <summary>Hides the gates' names (a picture without text, for the launcher's banner).</summary>
    public void HideExitLabels()
    {
        foreach ((Label3D label, _) in _exitLabels)
            label.Visible = false;
    }

    public void SetLanguage(string lang)
    {
        foreach ((Label3D label, LocalizedText text) in _exitLabels)
            label.Text = text.In(lang);
    }

    /// <summary>The path a click would walk, in yellow.</summary>
    public void ShowPath(IReadOnlyList<Cell>? path)
    {
        foreach (MeshInstance3D m in _pathMarks)
            m.QueueFree();
        _pathMarks.Clear();
        foreach (Cell c in path ?? [])
        {
            var mark = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(0.9f, 0.9f) }, MaterialOverride = _pathMaterial, Position = BoardView.ToWorld(c) + new Vector3(0, 0.05f, 0) };
            AddChild(mark);
            _pathMarks.Add(mark);
        }
    }

    public int PathShown => _pathMarks.Count;

    /// <summary>Walks the player along the path (at once when <paramref name="step"/> is 0), turning to whom they talk to.</summary>
    public async Task Walk(TownStep move, double step)
    {
        ShowPath(null);
        if (move.Path.Count > 0)
        {
            _anim?.Play("walk");
            foreach (Cell c in move.Path)
            {
                Vector3 to = BoardView.ToWorld(c);
                Face(Player, to - Player.Position);
                if (step <= 0)
                {
                    Player.Position = to;
                    continue;
                }
                Tween tween = CreateTween();
                tween.TweenProperty(Player, "position", to, step);
                await ToSignal(tween, Tween.SignalName.Finished);
            }
            _anim?.Play("idle");
        }
        if (move.TalkTo is Npc npc)
        {
            Face(Player, BoardView.ToWorld(npc.At) - Player.Position);
            Face(People[npc.Id], Player.Position - People[npc.Id].Position);
        }
    }

    /// <summary>Turns a person's model (their first child) towards a direction; models look along +z.</summary>
    private static void Face(Node3D person, Vector3 direction)
    {
        if (direction.LengthSquared() > 0.001f)
            person.GetChild<Node3D>(0).Rotation = new Vector3(0, Mathf.Atan2(direction.X, direction.Z), 0);
    }
}
