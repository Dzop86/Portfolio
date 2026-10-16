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
        // The gathering spots (sprint 99): a small gem in the trade's colour over each. Since sprint 103 (Charles),
        // the resource itself is always seen (the tree, the rock, a fish in the water, wheat on a plot, a tuft of
        // herbs) and, once gathered, it and its gem vanish until it grows back, leaving a stump or a few stones.
        foreach (HarvestNode node in controller.Nodes)
        {
            Vector3 at = BoardView.ToWorld(node.At);
            char decor = town.Rows[node.At.Y][node.At.X];
            Node3D resource = node.Kind switch
            {
                NodeKind.Tree or NodeKind.Rock when _decor.TryGetValue(node.At, out Node3D? standing) => standing,
                NodeKind.Rock => Place(Kit + "rock-large.glb", at, (node.At.X * 37) % 360, 0.35f),
                NodeKind.Water => Fish(at),
                NodeKind.Plot => Crops(at, node.At),
                _ when _decor.TryGetValue(node.At, out Node3D? standing) => standing,
                _ => Herbs(at, node.At),
            };
            Node3D? remnant = node.Kind switch
            {
                NodeKind.Tree => Stump(at),
                NodeKind.Rock => Stones(at),
                NodeKind.Plot => Soil(at),
                _ when decor == 'M' => Stump(at, new Color("e8e0cc")),
                _ => null,
            };
            if (remnant is not null && node.Kind != NodeKind.Plot)
                remnant.Visible = false;
            var material = new StandardMaterial3D { AlbedoColor = new Color(NodeColour(node.Kind)), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
            var gem = new MeshInstance3D
            {
                Mesh = new PrismMesh { Size = new Vector3(0.22f, 0.3f, 0.22f) },
                MaterialOverride = material,
                Position = at + new Vector3(0, GemHeight(node, town), 0),
                RotationDegrees = new Vector3(180, 0, 0),
            };
            AddChild(gem);
            _nodes[node.Id] = (gem, resource, remnant, node.Kind);
        }
        Player = Person(player, null);
        _anim = Player.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        Player.Position = BoardView.ToWorld(controller.Position);
        ShowNamesNear(controller.Position);
        AddChild(Player);
        _pathMaterial = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.9f, 0.35f, 0.8f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
    }

    private readonly Dictionary<string, (MeshInstance3D Gem, Node3D Resource, Node3D? Remnant, NodeKind Kind)> _nodes = [];

    // The trees, rocks, giant mushrooms and flowers drawn, by cell: a gathering spot's resource when it stands on one.
    private readonly Dictionary<Cell, Node3D> _decor = [];

    /// <summary>How high a spot's gem floats: over a tree's crown, a rock of the decor, or a little over what grows.</summary>
    public static float GemHeight(HarvestNode node, Town town)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(town);
        char decor = town[node.At];
        return node.Kind switch
        {
            NodeKind.Tree => 2.2f,
            NodeKind.Rock when decor == 'R' => 1.1f,
            _ when decor == 'M' => 2.4f,
            NodeKind.Plot => 1.0f,
            _ => 0.8f,
        };
    }

    private static StandardMaterial3D Matte(Color colour) => new() { AlbedoColor = colour, Roughness = 0.9f };

    private static MeshInstance3D Part(Node3D parent, Mesh mesh, Color colour, Vector3 at, Vector3? turn = null)
    {
        var part = new MeshInstance3D { Mesh = mesh, MaterialOverride = Matte(colour), Position = at, RotationDegrees = turn ?? Vector3.Zero };
        parent.AddChild(part);
        return part;
    }

    private Node3D Group(Vector3 at)
    {
        var node = new Node3D { Position = at };
        AddChild(node);
        return node;
    }

    /// <summary>Ripe stalks on a plot (a farmer's spot): a dozen golden stems, each with its ear, a little uneven.</summary>
    private Node3D Crops(Vector3 at, Cell c)
    {
        Node3D crops = Group(at);
        var straw = new Color("d9b44a");
        for (int i = 0; i < 12; i++)
        {
            float x = -0.3f + 0.2f * (i % 4) + 0.05f * ((c.X + i) % 3 - 1), z = -0.25f + 0.25f * (i / 4) + 0.05f * ((c.Y + i) % 3 - 1);
            float h = 0.55f + 0.08f * ((c.X * 7 + i * 3) % 3);
            Part(crops, new CylinderMesh { TopRadius = 0.015f, BottomRadius = 0.02f, Height = h, RadialSegments = 4 }, straw, new Vector3(x, h / 2, z), new Vector3(0, 0, 4 * ((i % 3) - 1)));
            Part(crops, new CapsuleMesh { Radius = 0.04f, Height = 0.18f, RadialSegments = 6, Rings = 2 }, new Color("e8c45c"), new Vector3(x, h + 0.06f, z));
        }
        return crops;
    }

    /// <summary>A plot's tilled earth, seen with or without its crop.</summary>
    private Node3D Soil(Vector3 at)
    {
        Node3D soil = Group(at);
        for (int row = 0; row < 3; row++)
            Part(soil, new BoxMesh { Size = new Vector3(0.85f, 0.06f, 0.18f) }, new Color("6b4a2c"), new Vector3(0, 0.03f, -0.25f + 0.25f * row));
        return soil;
    }

    /// <summary>A tuft of herbs (a herbalist's spot): leaves round a few small flowers.</summary>
    private Node3D Herbs(Vector3 at, Cell c)
    {
        Node3D herbs = Group(at);
        var leaf = new Color("2f8a3e");
        for (int i = 0; i < 7; i++)
        {
            float angle = Mathf.Tau * i / 7 + c.X;
            Part(herbs, new PrismMesh { Size = new Vector3(0.14f, 0.4f, 0.05f) }, leaf, new Vector3(0.18f * Mathf.Cos(angle), 0.2f, 0.18f * Mathf.Sin(angle)), new Vector3(0, -Mathf.RadToDeg(angle), 18));
        }
        foreach ((float x, float z) in new[] { (0f, 0f), (0.1f, 0.08f), (-0.08f, 0.1f) })
        {
            Part(herbs, new CylinderMesh { TopRadius = 0.01f, BottomRadius = 0.01f, Height = 0.45f, RadialSegments = 4 }, leaf, new Vector3(x, 0.22f, z));
            Part(herbs, new SphereMesh { Radius = 0.05f, Height = 0.1f, RadialSegments = 6, Rings = 3 }, new Color("ff7eb6"), new Vector3(x, 0.47f, z));
        }
        return herbs;
    }

    /// <summary>A fish leaping out of rings on the water (a fisher's spot).</summary>
    private Node3D Fish(Vector3 at)
    {
        Node3D fish = Group(at);
        Part(fish, new TorusMesh { InnerRadius = 0.25f, OuterRadius = 0.3f, Rings = 16, RingSegments = 4 }, new Color("d6eefc"), new Vector3(0, 0.04f, 0));
        Part(fish, new TorusMesh { InnerRadius = 0.4f, OuterRadius = 0.44f, Rings = 16, RingSegments = 4 }, new Color("a9d8f5"), new Vector3(0, 0.03f, 0));
        Part(fish, new SphereMesh { Radius = 0.1f, Height = 0.36f, RadialSegments = 8, Rings = 4 }, new Color("9fb3c2"), new Vector3(0, 0.3f, 0), new Vector3(0, 0, 70));
        Part(fish, new PrismMesh { Size = new Vector3(0.18f, 0.14f, 0.04f) }, new Color("7f95a5"), new Vector3(-0.12f, 0.16f, 0), new Vector3(0, 0, -20));
        return fish;
    }

    /// <summary>What a felled tree leaves (or a giant mushroom, in its own colour): a stump.</summary>
    private Node3D Stump(Vector3 at, Color? colour = null)
    {
        Node3D stump = Group(at);
        Part(stump, new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.26f, Height = 0.3f, RadialSegments = 10 }, colour ?? new Color("8a5a36"), new Vector3(0, 0.15f, 0));
        Part(stump, new CylinderMesh { TopRadius = 0.17f, BottomRadius = 0.17f, Height = 0.02f, RadialSegments = 10 }, new Color("e2c08d"), new Vector3(0, 0.31f, 0));
        return stump;
    }

    /// <summary>What a mined rock leaves: a few stones, still in the way.</summary>
    private Node3D Stones(Vector3 at)
    {
        Node3D stones = Group(at);
        foreach ((float x, float z, float r) in new[] { (-0.15f, 0.05f, 0.14f), (0.12f, -0.1f, 0.11f), (0.1f, 0.15f, 0.09f) })
            Part(stones, new SphereMesh { Radius = r, Height = r * 1.4f, RadialSegments = 6, Rings = 3 }, new Color("9a9a96"), new Vector3(x, r * 0.6f, z));
        return stones;
    }

    /// <summary>The colour of a kind of gathering spot: the element of its trade (wood Fire, ore Earth, fish Water, crops Air), herbs the heal's.</summary>
    public static string NodeColour(NodeKind kind) => kind switch
    {
        NodeKind.Tree => ElementStyle.Colour(Element.Fire),
        NodeKind.Rock => ElementStyle.Colour(Element.Earth),
        NodeKind.Water => ElementStyle.Colour(Element.Water),
        NodeKind.Plot => ElementStyle.Colour(Element.Air),
        _ => ElementStyle.Heal,
    };

    /// <summary>The spots growing back without their resource nor their gem (a stump, stones or bare earth left), the others whole.</summary>
    public void ShowGrowing(IReadOnlyCollection<string> growing)
    {
        foreach ((string id, (MeshInstance3D gem, Node3D resource, Node3D? remnant, NodeKind kind)) in _nodes)
        {
            bool waiting = growing.Contains(id);
            gem.Visible = resource.Visible = !waiting;
            if (remnant is not null && kind != NodeKind.Plot)
                remnant.Visible = waiting;
        }
    }

    /// <summary>How many spots are drawn, and how many have vanished, growing back (for the self-test).</summary>
    public (int Drawn, int Growing) NodesShown => (_nodes.Count, _nodes.Values.Count(n => !n.Gem.Visible && !n.Resource.Visible));

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
                _decor[c] = Place(Kit + ((c.X + c.Y) % 2 == 0 ? "tree.glb" : "tree-high.glb"), at, 0, 1);
                break;
            case 'R':
                _decor[c] = Place(Kit + "rock-large.glb", at, (c.X * 37) % 360, 0.6f);
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
                _decor[c] = Place(Nature + "mushroom_tan.glb", at, (c.X * 53) % 360, 2.4f);
                break;
            case 'B':
                Place(Nature + "plant_bushSmall.glb", at, (c.X * 41) % 360, 1.8f);
                break;
            case 'P':
                Stakes(at);
                break;
            case ',':
                _decor[c] = Place(Nature + ((c.X + c.Y) % 2 == 0 ? "flower_yellowA.glb" : "flower_redA.glb"), at, (c.Y * 29) % 360, 1.2f);
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

    private Node3D Place(string path, Vector3 at, float turn, float scale) => Place(path, at, turn, new Vector3(scale, scale, scale));

    private Node3D Place(string path, Vector3 at, float turn, Vector3 scale)
    {
        Node3D model = Model(path);
        model.Position = at;
        model.RotationDegrees = new Vector3(0, turn, 0);
        model.Scale = scale;
        AddChild(model);
        return model;
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
