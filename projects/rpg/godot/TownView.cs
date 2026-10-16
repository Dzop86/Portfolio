using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A town drawn from Rpg.Client's <see cref="TownController"/>: grass, roads and water, houses built
/// from Kenney's Fantasy Town Kit, its people with their names above them, the player walking, the
/// path under the mouse, the gates and the ways to the other zones. The talks are drawn by <see cref="TalkPanel"/>.
/// </summary>
public partial class TownView : Node3D
{
    private const string Kit = "res://assets/kenney/town/";
    private const string Nature = "res://assets/kenney/nature/";

    private readonly List<MeshInstance3D> _pathMarks = [];
    private readonly List<(Label3D Label, LocalizedText Text)> _exitLabels = [];
    private StandardMaterial3D _pathMaterial = null!;
    private AnimationPlayer? _anim;

    public TownController Controller { get; private set; } = null!;

    /// <summary>The ground tiles drawn: one per cell.</summary>
    public int Tiles { get; private set; }

    /// <summary>The names over the gates and the ways, as the self-test counts them.</summary>
    public int WayLabels => _exitLabels.Count;

    public IEnumerable<string> WayNames => _exitLabels.Select(l => l.Label.Text);
    public Node3D Player { get; private set; } = null!;
    public Dictionary<string, Node3D> People { get; } = [];

    /// <summary>The dungeons' keeps drawn (sprint 86), for the self-test.</summary>
    public int Keeps { get; private set; }

    /// <summary>The ring of a dungeon's door: the chocolate of the charter, lightened to show on grass.</summary>
    public static readonly Color DoorColour = new("8a5a34");

    /// <summary>An inhabitant's name shows only within this many steps of the player.</summary>
    public const int NameDistance = 3;

    public void Build(TownController controller, string lang, Hero player)
    {
        Controller = controller;
        Town town = controller.Town;
        (string light, string dark) = ZoneStyle.Ground(town.Zone);
        StandardMaterial3D[] grass = [.. new[] { light, dark }.Select(g => new StandardMaterial3D { AlbedoColor = new Color(g), Roughness = 1 })];
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
            Tiles++;
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
        // The ways to the other maps, each cell ringed in the accent colour; a way to another zone has the
        // zone's name once, above its middle cell; a way inside the zone, none (D64).
        IReadOnlyList<ZoneLink> links = town.Links ?? [];
        var named = links.Where(l => l.Name is not null).GroupBy(l => l.To).Select(g => g.OrderBy(l => l.At.X).ThenBy(l => l.At.Y).ElementAt(g.Count() / 2)).ToHashSet();
        // A hall's far door while its group stands (sprint 86): ringed in the enemies' colour.
        foreach (ZoneLink link in links)
        {
            AddChild(new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.32f, OuterRadius = 0.45f, Rings = 24 },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = controller.ShutWays.Contains(link) ? FighterView.EnemyColour : FighterView.PlayerColour, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
                Scale = new Vector3(1, 0.15f, 1),
                Position = BoardView.ToWorld(link.At) + new Vector3(0, 0.05f, 0),
            });
            if (!named.Contains(link))
                continue;
            Label3D label = Label(link.Name!.In(lang), BoardView.ToWorld(link.At) + new Vector3(0, 0.7f, 0), 36);
            AddChild(label);
            _exitLabels.Add((label, link.Name));
        }
        // The doors of dungeons (sprint 80): a stone doorway behind a ring in chocolate, the dungeon's name above.
        foreach (DungeonDoor door in town.Doors)
        {
            Vector3 at = BoardView.ToWorld(door.At);
            // Under a keep (sprint 86), the keep's own doorway; otherwise a small one.
            if (town[door.At + new Cell(0, -1)] != 'k')
                Place(Kit + "wall-door.glb", at + new Vector3(0, 0, -0.5f), 0, 1);
            AddChild(new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.32f, OuterRadius = 0.45f, Rings = 24 },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = DoorColour, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
                Scale = new Vector3(1, 0.15f, 1),
                Position = at + new Vector3(0, 0.05f, 0),
            });
            LocalizedText name = controller.Data.Dungeons[door.Dungeon].Name;
            Label3D label = Label(name.In(lang), at + new Vector3(0, 1.4f, 0), 36);
            AddChild(label);
            _exitLabels.Add((label, name));
        }
        // The workshops (sprint 81): a ring in chocolate and the bench's name.
        foreach (Workshop w in town.Workshops ?? [])
        {
            Vector3 at = BoardView.ToWorld(w.At);
            AddChild(new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.32f, OuterRadius = 0.45f, Rings = 24 },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = DoorColour, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
                Scale = new Vector3(1, 0.15f, 1),
                Position = at + new Vector3(0, 0.05f, 0),
            });
            LocalizedText name = controller.Data.Jobs[w.Job].Workshop!;
            Label3D label = Label(name.In(lang), at + new Vector3(0, 0.9f, 0), 36);
            AddChild(label);
            _exitLabels.Add((label, name));
        }
        foreach ((string id, Cell at) in controller.Groups)
            AddGroup(controller.Data.Groups[id], at, lang);
        // The gathering spots (sprint 99): a small gem in the trade's colour over each, grey while it grows back;
        // a rock on the grass where a map has none to mine.
        foreach (HarvestNode node in controller.Nodes)
        {
            Vector3 at = BoardView.ToWorld(node.At);
            bool onDecor = town.Rows[node.At.Y][node.At.X] != '.' && town.Rows[node.At.Y][node.At.X] != ',';
            if (node.Kind == NodeKind.Rock && !onDecor)
                Place(Kit + "rock-large.glb", at, (node.At.X * 37) % 360, 0.35f);
            var material = new StandardMaterial3D { AlbedoColor = new Color(NodeColour(node.Kind)), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
            var gem = new MeshInstance3D
            {
                Mesh = new PrismMesh { Size = new Vector3(0.22f, 0.3f, 0.22f) },
                MaterialOverride = material,
                Position = at + new Vector3(0, node.Kind is NodeKind.Tree ? 2.2f : node.Kind is NodeKind.Rock && onDecor ? 1.1f : 0.7f, 0),
                RotationDegrees = new Vector3(180, 0, 0),
            };
            AddChild(gem);
            _nodes[node.Id] = (gem, material, node.Kind);
        }
        Player = Person(player, null);
        _anim = Player.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        Player.Position = BoardView.ToWorld(controller.Position);
        ShowNamesNear(controller.Position);
        AddChild(Player);
        _pathMaterial = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.9f, 0.35f, 0.8f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
    }

    private readonly Dictionary<string, (MeshInstance3D Gem, StandardMaterial3D Material, NodeKind Kind)> _nodes = [];

    /// <summary>The colour of a kind of gathering spot: the element of its trade (wood Fire, ore Earth, fish Water, crops Air), herbs the heal's.</summary>
    public static string NodeColour(NodeKind kind) => kind switch
    {
        NodeKind.Tree => ElementStyle.Colour(Element.Fire),
        NodeKind.Rock => ElementStyle.Colour(Element.Earth),
        NodeKind.Water => ElementStyle.Colour(Element.Water),
        NodeKind.Plot => ElementStyle.Colour(Element.Air),
        _ => ElementStyle.Heal,
    };

    /// <summary>The spots growing back drawn grey and small, the others in their colour.</summary>
    public void ShowGrowing(IReadOnlyCollection<string> growing)
    {
        foreach ((string id, (MeshInstance3D gem, StandardMaterial3D material, NodeKind kind)) in _nodes)
        {
            bool waiting = growing.Contains(id);
            material.AlbedoColor = waiting ? new Color(0.45f, 0.45f, 0.45f) : new Color(NodeColour(kind));
            gem.Scale = Vector3.One * (waiting ? 0.6f : 1f);
        }
    }

    /// <summary>How many spots are drawn, and how many grey (for the self-test).</summary>
    public (int Drawn, int Growing) NodesShown => (_nodes.Count, _nodes.Values.Count(n => n.Gem.Scale.X < 1));

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
            case 'K':
                Keep(at + new Vector3(1, 0, 1));
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
            case 'M':
                Place(Nature + "mushroom_tan.glb", at, (c.X * 53) % 360, 2.4f);
                break;
            case 'B':
                Place(Nature + "plant_bushSmall.glb", at, (c.X * 41) % 360, 1.8f);
                break;
            case 'P':
                Stakes(at);
                break;
            case ',':
                Place(Nature + ((c.X + c.Y) % 2 == 0 ? "flower_yellowA.glb" : "flower_redA.glb"), at, (c.Y * 29) % 360, 1.2f);
                break;
        }
    }

    /// <summary>
    /// A dungeon's keep (sprint 86, D67), over 3 × 3 cells: two storeys of stone, the doorway south over
    /// the dungeon's door, a pointed roof, and a tower at each corner, taller than the houses around.
    /// </summary>
    private void Keep(Vector3 centre)
    {
        Keeps++;
        foreach ((string piece, float turn) in new[] { ("wall-door", -90f), ("wall", 0f), ("wall", 90f), ("wall", 180f) })
            Place(Kit + piece + ".glb", centre, turn, 3);
        foreach (float turn in new[] { -90f, 0f, 90f, 180f })
            Place(Kit + "wall-window-shutters.glb", centre + new Vector3(0, 3, 0), turn, 3);
        Place(Kit + "roof-point.glb", centre + new Vector3(0, 6, 0), 0, 3);
        foreach ((float dx, float dz) in new[] { (-1.4f, -1.4f), (1.4f, -1.4f), (-1.4f, 1.4f), (1.4f, 1.4f) })
        {
            Vector3 tower = centre + new Vector3(dx, 0, dz);
            for (int storey = 0; storey < 3; storey++)
            {
                foreach (float turn in new[] { -90f, 0f, 90f, 180f })
                    Place(Kit + "wall.glb", tower + new Vector3(0, storey * 2.6f, 0), turn, new Vector3(1.1f, 2.6f, 1.1f));
            }
            Place(Kit + "roof-point.glb", tower + new Vector3(0, 7.8f, 0), 0, 1.1f);
        }
    }

    private static StandardMaterial3D? _stake;

    /// <summary>
    /// The village's palisade (sprint 102, no free model of one): three stakes of wood a cell, cut to a point,
    /// a little uneven, in the chart's chocolate.
    /// </summary>
    private void Stakes(Vector3 at)
    {
        _stake ??= new StandardMaterial3D { AlbedoColor = new Color("6b4a2c"), Roughness = 0.9f };
        for (int i = 0; i < 3; i++)
        {
            float height = 1.5f + 0.15f * (((int)(at.X * 7 + at.Z * 13) + i) % 3);
            Vector3 foot = at + new Vector3(-0.33f + 0.33f * i, 0, 0.02f * (i - 1));
            AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.14f, BottomRadius = 0.16f, Height = height, RadialSegments = 8 }, MaterialOverride = _stake, Position = foot + new Vector3(0, height / 2, 0) });
            AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0, BottomRadius = 0.14f, Height = 0.3f, RadialSegments = 8 }, MaterialOverride = _stake, Position = foot + new Vector3(0, height + 0.15f, 0) });
        }
    }

    private void Place(string path, Vector3 at, float turn, float scale) => Place(path, at, turn, new Vector3(scale, scale, scale));

    private void Place(string path, Vector3 at, float turn, Vector3 scale)
    {
        Node3D model = Model(path);
        model.Position = at;
        model.RotationDegrees = new Vector3(0, turn, 0);
        model.Scale = scale;
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

    /// <summary>Shows the names of the inhabitants close to <paramref name="player"/> (Manhattan distance), hides the others.</summary>
    public void ShowNamesNear(Cell player)
    {
        foreach (Npc npc in Controller.Town.Npcs)
        {
            if (People.TryGetValue(npc.Id, out Node3D? person))
            {
                foreach (Label3D label in person.FindChildren("*", nameof(Label3D), true, false).OfType<Label3D>())
                    label.Visible = !_namesHidden && npc.At.DistanceTo(player) <= NameDistance;
            }
        }
        foreach ((string id, Label3D label) in _groupLabels)
            label.Visible = !_namesHidden && Controller.Groups.TryGetValue(id, out Cell at) && at.DistanceTo(player) <= NameDistance + 1;
    }

    /// <summary>Whether an inhabitant's name is shown (for the self-test).</summary>
    public bool NameShown(string npc) => People[npc].FindChildren("*", nameof(Label3D), true, false).OfType<Label3D>().Any(l => l.Visible);

    private bool _namesHidden;

    /// <summary>Hides every name (the launcher's banner, a picture without text).</summary>
    public void HideNames()
    {
        _namesHidden = true;
        ShowNamesNear(Controller.Position);
    }

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
        foreach ((string id, Label3D label) in _groupLabels)
            label.Text = new Texts(lang).GroupLabel(Controller.Data.Groups[id], Controller.Data);
    }

    /// <summary>The groups of monsters drawn (sprint 72), by id: their models together on their cell.</summary>
    public Dictionary<string, Node3D> Groups { get; } = [];

    private readonly Dictionary<string, Label3D> _groupLabels = [];
    private readonly Dictionary<string, int> _groupWalks = [];

    // Where each monster of a group stands on its cell: three at most are drawn, the label says how many.
    private static readonly Vector3[][] GroupPlaces =
    [
        [Vector3.Zero],
        [new(-0.22f, 0, 0.14f), new(0.22f, 0, -0.14f)],
        [new(0, 0, 0.24f), new(-0.26f, 0, -0.18f), new(0.26f, 0, -0.18f)],
    ];

    private void AddGroup(MonsterGroup group, Cell at, string lang)
    {
        var root = new Node3D { Position = BoardView.ToWorld(at), Name = "Group " + group.Id };
        root.AddChild(new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.4f, OuterRadius = 0.48f, Rings = 24 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = FighterView.EnemyColour, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
            Scale = new Vector3(1, 0.12f, 1),
            Position = new Vector3(0, 0.04f, 0),
        });
        Vector3[] places = GroupPlaces[Math.Min(group.Monsters.Count, GroupPlaces.Length) - 1];
        for (int i = 0; i < group.Monsters.Count && i < places.Length; i++)
        {
            Node3D model = Looks.Model(Controller.Data.Monsters[group.Monsters[i]].Look);
            model.Position = places[i];
            model.RotationDegrees = new Vector3(0, 30 + 40 * i, 0);
            root.AddChild(model);
            Animate(model, "idle");
        }
        Label3D label = Label(new Texts(lang).GroupLabel(group, Controller.Data), new Vector3(0, 1.1f, 0), 32);
        label.Modulate = new Color("ffb3a3");
        root.AddChild(label);
        _groupLabels[group.Id] = label;
        AddChild(root);
        Groups[group.Id] = root;
    }

    private static void Animate(Node3D model, string name)
    {
        if (model.FindChild("AnimationPlayer", true, false) is AnimationPlayer anim && anim.HasAnimation(name))
        {
            anim.GetAnimation(name).LoopMode = Animation.LoopModeEnum.Linear;
            anim.Play(name);
        }
    }

    /// <summary>A group walks along <paramref name="path"/>, <paramref name="speed"/> cells a second, turned the way it goes.</summary>
    public async Task MoveGroup(string id, IReadOnlyList<Cell> path, double speed)
    {
        Node3D root = Groups[id];
        int me = _groupWalks[id] = _groupWalks.GetValueOrDefault(id) + 1;
        Node3D[] models = [.. root.GetChildren().OfType<Node3D>().Where(n => n.FindChild("AnimationPlayer", true, false) is not null)];
        foreach (Node3D m in models)
            Animate(m, "walk");
        foreach (Cell c in path)
        {
            Vector3 to = BoardView.ToWorld(c);
            foreach (Node3D m in models)
                m.Rotation = new Vector3(0, Mathf.Atan2(to.X - root.Position.X, to.Z - root.Position.Z), 0);
            while (root.Position != to)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (_groupWalks[id] != me || !IsInstanceValid(root))
                    return;
                root.Position = root.Position.MoveToward(to, (float)(speed * GetProcessDeltaTime()));
            }
            ShowNamesNear(Standing);
        }
        foreach (Node3D m in models)
            Animate(m, "idle");
    }

    /// <summary>Puts a group on a cell at once (the self-test).</summary>
    public void PlaceGroup(string id, Cell at)
    {
        _groupWalks[id] = _groupWalks.GetValueOrDefault(id) + 1;
        Groups[id].Position = BoardView.ToWorld(at);
    }

    /// <summary>Whether a group's label is shown (for the self-test).</summary>
    public bool GroupNamed(string id) => _groupLabels[id].Visible;

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

    // Each walk's number: a newer walk, or Stop, ends the one going on.
    private int _walk;

    /// <summary>A footstep sounds every this many cells walked.</summary>
    public const double StepLength = 0.6;

    /// <summary>Called at each footstep of a walk (the client plays the sound).</summary>
    public event Action? Footstep;

    /// <summary>
    /// Walks the player along the walk's straight lines (sprint 69) at <paramref name="speed"/> cells a
    /// second, at once when it is 0, turning to whom they talk to. True when the walk got to its end;
    /// false when a newer walk or <see cref="Stop"/> cut it short, the player left where they were.
    /// </summary>
    public async Task<bool> Walk(TownStep move, double speed)
    {
        int me = ++_walk;
        ShowPath(null);
        if (move.Waypoints.Count > 0)
        {
            _anim?.Play("walk");
            double walked = 0;
            foreach (Cell c in move.Waypoints)
            {
                Vector3 to = BoardView.ToWorld(c);
                Face(Player, to - Player.Position);
                if (speed <= 0)
                {
                    Player.Position = to;
                    ShowNamesNear(c);
                    continue;
                }
                while (Player.Position != to)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (me != _walk)
                        return false;
                    float stride = (float)(speed * GetProcessDeltaTime());
                    Player.Position = Player.Position.MoveToward(to, stride);
                    ShowNamesNear(Standing);
                    walked += stride;
                    if (walked >= StepLength)
                    {
                        walked -= StepLength;
                        Footstep?.Invoke();
                    }
                }
            }
            _anim?.Play("idle");
        }
        if (move.TalkTo is Npc npc)
        {
            Face(Player, BoardView.ToWorld(npc.At) - Player.Position);
            Face(People[npc.Id], Player.Position - People[npc.Id].Position);
        }
        return true;
    }

    /// <summary>Ends the walk going on where the player is; they stand still.</summary>
    public void Stop()
    {
        _walk++;
        _anim?.Play("idle");
    }

    /// <summary>The cell under the player as drawn, mid-walk too.</summary>
    public Cell Standing => new(Mathf.RoundToInt(Player.Position.X), Mathf.RoundToInt(Player.Position.Z));

    /// <summary>Turns a person's model (their first child) towards a direction; models look along +z.</summary>
    private static void Face(Node3D person, Vector3 direction)
    {
        if (direction.LengthSquared() > 0.001f)
            person.GetChild<Node3D>(0).Rotation = new Vector3(0, Mathf.Atan2(direction.X, direction.Z), 0);
    }
}
