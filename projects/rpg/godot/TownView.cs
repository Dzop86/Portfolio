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

    /// <summary>The rings drawn on ways between maps: only a hall's shut doors since sprint 179 (for the self-test).</summary>
    public int ShutRings { get; private set; }

    /// <summary>The dungeons' keeps drawn (sprint 86), for the self-test.</summary>
    public int Keeps { get; private set; }

    /// <summary>The ring of a dungeon's door: the chocolate of the charter, lightened to show on grass.</summary>
    public static readonly Color DoorColour = new("8a5a34");

    /// <summary>An inhabitant's name shows only within this many steps of the player.</summary>
    public const int NameDistance = 3;

    /// <summary>The ground in one tone, without the checks that count the cells (the world map's photographs, sprint 169).</summary>
    public bool PlainGround { get; set; }

    public void Build(TownController controller, string lang, Hero player)
    {
        Controller = controller;
        _lang = lang;
        Town town = controller.Town;
        (string light, string dark) = ZoneStyle.Ground(town.Zone);
        // Since sprint 178 (D87, Q1 b) no chequerboard out of a fight: the zone's two tones in patches, a drier
        // tone along the roads. The fights keep their checks (BoardView).
        StandardMaterial3D[] grass = [.. Enumerable.Range(0, DecorScatter.Tones).Select(t => Grass(new Color(light).Lerp(new Color(dark), t / (DecorScatter.Tones - 1f))))];
        StandardMaterial3D[] dry = [.. grass.Select(g => Grass(g.AlbedoColor.Lerp(DryGrass, 0.3f)))];
        // Since sprint 179 (D90) the ground is one mesh over the map and its skirt, following the relief; the water
        // is drawn over it in rounded puddles and ponds; only the roads keep their tiles (they lie on flat ground).
        Relief = new Relief(controller.Data, town);
        SkirtCells = PlainGround ? [] : MapSkirt.Around(controller.Data, town);
        Ground(controller, grass, dry);
        foreach (Cell c in controller.Board.Cells())
        {
            char k = town[c];
            if (k == '=')
            {
                Node3D road = Model(Kit + "road.glb");
                road.Position = BoardView.ToWorld(c);
                AddChild(road);
            }
            Tiles++;
            Decorate(c, k);
        }
        foreach (SkirtCell s in SkirtCells)
            SkirtDecor(s);
        foreach (Sprig bush in DecorScatter.Rim(town, RimPieces.Length))
        {
            (string path, float scale) = RimPieces[bush.Variant];
            Shade(Place(path, BoardView.ToWorld(bush.At) + new Vector3(bush.Dx, 0, bush.Dz), bush.Turn, scale), Foliage);
        }
        foreach (Prop prop in town.Props ?? [])
            AddProp(prop);
        // The village's beasts (sprint 104), each where the map says, turned a little at random.
        Herd = new Herd(town);
        for (int i = 0; i < Herd.Animals.Count; i++)
        {
            Node3D beast = Creatures.Beast(Herd.Animals[i].Kind);
            beast.Position = W(Herd.At(i));
            _onRelief.Add(beast);
            beast.RotationDegrees = new Vector3(0, (Herd.At(i).X * 67 + Herd.At(i).Y * 31) % 360, 0);
            AddChild(beast);
            Animate(beast, "idle");
            Beasts.Add(beast);
            _beastWalks.Add(0);
        }
        foreach (Npc npc in town.Npcs)
        {
            Node3D person = Person(new Hero("", npc.Look, null, npc.Colour), npc.Name.In(lang));
            person.Position = W(npc.At);
            _onRelief.Add(person);
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
        // Since sprint 179 (Charles, D90) the ways between maps have no ring: the zone's name and the edge's arrow
        // say enough. Only a hall's door still shut keeps its ring, in the enemies' colour.
        foreach (ZoneLink link in links)
        {
            if (controller.ShutWays.Contains(link))
            {
                AddChild(new MeshInstance3D
                {
                    Mesh = new TorusMesh { InnerRadius = 0.32f, OuterRadius = 0.45f, Rings = 24 },
                    MaterialOverride = new StandardMaterial3D { AlbedoColor = FighterView.EnemyColour, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
                    Scale = new Vector3(1, 0.15f, 1),
                    Position = BoardView.ToWorld(link.At) + new Vector3(0, 0.05f, 0),
                });
                ShutRings++;
            }
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
            // A tree or a rock of the decor stands off its cell's middle (sprint 178): its stump and gem follow it.
            if (node.Kind is NodeKind.Tree or NodeKind.Rock && _decor.ContainsKey(node.At))
            {
                Vector3 shift = new(resource.Position.X - at.X, 0, resource.Position.Z - at.Z);
                if (remnant is not null)
                    remnant.Position += shift;
                at += shift;
            }
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
        Tufts(town, controller);
        Player = Person(player, null);
        // The player's own words over it (sprint 173, D89): its name, its guild's set apart by ShowOwnGuild; on hovering it only.
        _playerName = string.IsNullOrEmpty(player.Name) ? null : player.Name;
        Tag(Player, _playerName, null, null, null);
        _anim = Player.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        _riderModel = Player.GetChild<Node3D>(0);
        Ride(Mounts.Ridden(player));
        Player.Position = W(controller.Position);
        _onRelief.Add(Player);
        ShowNamesNear(controller.Position);
        AddChild(Player);
        _pathMaterial = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.9f, 0.35f, 0.8f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        // Everything laid on the ground above, at the height of the relief under it (sprint 179); what moves was
        // placed on it already.
        foreach (Node3D child in GetChildren().OfType<Node3D>())
        {
            if (_onRelief.Add(child))
                child.Position = Lift(child.Position);
        }
    }

    /// <summary>The mount the player rides (sprint 179), or null on foot.</summary>
    public string? Riding { get; private set; }

    private Node3D? _riderModel, _saddle;
    private float _rise;

    // How far the seated character sinks below its feet's height (the sit pose lowers its hips to a chair).
    private const float SeatDrop = -0.02f;

    /// <summary>
    /// The player on its mount, or back on foot (sprint 179, Charles): the character and the mount on one saddle
    /// that turns with the walk, the character seated on the mount's back, the mount walking under it; the words
    /// over the player raised as high. Called on building the map and when the mount is put on or taken off.
    /// </summary>
    public void Ride(string? mount)
    {
        if (mount == Riding || _riderModel is null)
            return;
        Node3D model = _riderModel;
        AnimationPlayer? feet = model.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (_saddle is not null)
        {
            // Down from the mount: the character back where it stood, turned as the saddle was.
            float turn = _saddle.Rotation.Y;
            _saddle.RemoveChild(model);
            Player.RemoveChild(_saddle);
            _saddle.QueueFree();
            _saddle = null;
            model.Position = Vector3.Zero;
            model.Rotation = new Vector3(0, turn, 0);
            Player.AddChild(model);
            Player.MoveChild(model, 0);
            foreach (Node3D words in Player.GetChildren().OfType<Node3D>().Where(n => n != model))
                words.Position -= new Vector3(0, _rise, 0);
            _rise = 0;
            _anim = feet;
            feet?.Play("idle");
        }
        Riding = mount;
        if (mount is null)
            return;
        Node3D beast = Creatures.Mount(mount);
        // The player's root is half as large again as the world (Person): the mount keeps its own size.
        beast.Scale = Vector3.One / Player.Scale.X;
        float size = beast.GetNode<Node3D>("Model").Scale.Y;
        _rise = Creatures.BackHeight * size / Player.Scale.X - SeatDrop;
        _saddle = new Node3D { Name = "Saddle", Rotation = model.Rotation };
        Player.RemoveChild(model);
        foreach (Node3D words in Player.GetChildren().OfType<Node3D>())
            words.Position += new Vector3(0, _rise, 0);
        Player.AddChild(_saddle);
        Player.MoveChild(_saddle, 0);
        _saddle.AddChild(beast);
        model.Rotation = Vector3.Zero;
        model.Position = new Vector3(0, _rise, -0.03f);
        _saddle.AddChild(model);
        if (feet is not null && feet.HasAnimation("sit"))
        {
            feet.GetAnimation("sit").LoopMode = Animation.LoopModeEnum.Linear;
            feet.Play("sit");
        }
        _anim = beast.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        _anim?.Play("idle");
    }

    /// <summary>The ground's heights (sprint 179, D90): hills in the forests, flat elsewhere.</summary>
    public Relief Relief { get; private set; } = null!;

    // The nodes already standing on the relief: the ground itself, and what moves.
    private readonly HashSet<Node3D> _onRelief = [];

    /// <summary>A cell's middle, on the ground (at the height of the relief).</summary>
    public Vector3 W(Cell c) => BoardView.ToWorld(c) + new Vector3(0, Relief.At(c), 0);

    /// <summary>
    /// One stride towards a point, over the relief (sprint 179, Charles: the player walked under a hill): along the
    /// flat ground, then set on the ground under it, so a long straight line climbs and goes down the slopes
    /// instead of cutting through them. It ends exactly on the point when the point is on the ground.
    /// </summary>
    private Vector3 Step(Vector3 from, Vector3 to, float stride) =>
        Lift(new Vector3(from.X, 0, from.Z).MoveToward(new Vector3(to.X, 0, to.Z), stride));

    /// <summary>A point raised by the height of the relief under it.</summary>
    public Vector3 Lift(Vector3 v) => v + new Vector3(0, Relief.At(v.X, v.Z), 0);

    /// <summary>
    /// The cell under a point of the screen, where the camera's ray meets the relief: from the flat ground, a few
    /// steps up to the height found under the last hit (the slopes are gentle, it settles at once).
    /// </summary>
    public Cell? Pick(Camera3D camera, Vector2 screen)
    {
        ArgumentNullException.ThrowIfNull(camera);
        Vector3 from = camera.ProjectRayOrigin(screen), dir = camera.ProjectRayNormal(screen);
        if (Mathf.IsZeroApprox(dir.Y))
            return null;
        float h = 0;
        Vector3 hit = from;
        for (int i = 0; i < 8; i++)
        {
            hit = from + dir * ((h - from.Y) / dir.Y);
            h = Relief.At(hit.X, hit.Z);
        }
        return new Cell(Mathf.RoundToInt(hit.X), Mathf.RoundToInt(hit.Z));
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
            // A giant of the Forest of the Ancestors stands higher (sprint 131).
            NodeKind.Tree when town.Zone == "ancestors" => Giant(node.At) ? 4.2f : 3f,
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
    public static string NodeColour(NodeKind kind) => MiniMap.SpotColour(kind);

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
            case 'T' when Ancestral:
                _decor[c] = Ancestor(c, at);
                break;
            case 'T':
                _decor[c] = Scattered(c, k, at, TreesOf(Controller.Town.ZoneId));
                break;
            case 'R' when Ancestral:
                _decor[c] = MossyRock(c, at);
                break;
            case 'R':
                _decor[c] = Scattered(c, k, at, Rocks);
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
                _decor[c] = Scattered(c, k, at, Mushrooms);
                break;
            case 'B':
                Scattered(c, k, at, Hedges);
                break;
            case 'P':
                Stakes(at);
                break;
            case 'X':
                Fence(c);
                break;
            case 'Q':
                HenHouse(block);
                break;
            case 'E':
                Stable(block);
                break;
            case ',':
                _decor[c] = Scattered(c, k, at, Flowers);
                break;
        }
    }

    // The Forest of the Ancestors (sprint 131, D76: no new model): its own trees and rocks.
    private bool Ancestral => Controller.Town.Zone == "ancestors";

    /// <summary>How many giant trunks the map shows (for the self-test).</summary>
    public int Giants { get; private set; }

    /// <summary>
    /// A tree of the Forest of the Ancestors: one cell in three a giant (the nature kit's tree, more than twice its
    /// size, its trunk thickened by a column of bark), the others younger pines grown tall in their shade.
    /// </summary>
    private Node3D Ancestor(Cell c, Vector3 at)
    {
        Scatter s = DecorScatter.For(c, 'T', 1);
        at += new Vector3(s.Dx, 0, s.Dz);
        if (!Giant(c))
            return Place(Nature + "tree_pineDefaultA.glb", at, s.Turn, 1.5f * s.Scale);
        Giants++;
        Node3D tree = Place(Nature + "tree_default.glb", at, s.Turn, 2.4f * Math.Clamp(s.Scale, 0.95f, 1.15f));
        Part(tree, new CylinderMesh { TopRadius = 0.09f, BottomRadius = 0.16f, Height = 0.7f, RadialSegments = 10 }, new Color("4a3524"), new Vector3(0, 0.35f, 0));
        return tree;
    }

    /// <summary>Whether a tree of the forest is a giant: one cell in three, the same every time.</summary>
    public static bool Giant(Cell c) => (c.X * 7 + c.Y * 5) % 3 == 0;

    /// <summary>A boulder of the forest: the nature kit's rock, its top under a cushion of moss.</summary>
    private Node3D MossyRock(Cell c, Vector3 at)
    {
        Scatter s = DecorScatter.For(c, 'R', 2);
        Node3D rock = Place(Nature + (s.Variant == 0 ? "rock_largeA.glb" : "rock_largeB.glb"), at + new Vector3(s.Dx, 0, s.Dz), s.Turn, 0.8f * s.Scale);
        Part(rock, new SphereMesh { Radius = 0.32f, Height = 0.22f, RadialSegments = 10, Rings = 4 }, new Color("4f7a34"), new Vector3(0.04f, 0.42f, -0.03f));
        return rock;
    }

    // The models a piece of decor is drawn among (sprint 178), each with the scale that fits it to a cell: the
    // nature kit's trees (1.15 to 1.7 high) brought to about 2.1, like the town kit's; tufts under 0.15.
    private static readonly (string Path, float Scale)[] TownTrees = [(Kit + "tree.glb", 1), (Kit + "tree-high.glb", 1)];

    private static (string, float) Tree(string name, float height) => (Nature + name + ".glb", 2.1f / height);

    private static readonly (string Path, float Scale)[] WoodsTrees =
    [
        .. TownTrees, Tree("tree_default", 1.71f), (Nature + "tree_pineDefaultA.glb", 1.1f), Tree("tree_oak", 1.23f),
        Tree("tree_cone", 1.43f), Tree("tree_pineRoundA", 1.37f), Tree("tree_pineTallA", 1.53f), Tree("tree_tall", 1.69f),
    ];

    /// <summary>
    /// The trees of a zone: the woods have them all; the fields, broad leaves; the lake, tall and slender ones. The
    /// kit's tree_detailed (a cluster of cubes) is left out: its style is not the others'.
    /// </summary>
    private static (string Path, float Scale)[] TreesOf(string zone) => zone switch
    {
        "fields" => [TownTrees[0], Tree("tree_oak", 1.23f), Tree("tree_fat", 1.15f), Tree("tree_default", 1.71f), Tree("tree_simple", 1.52f)],
        "lake" => [TownTrees[1], Tree("tree_oak", 1.23f), Tree("tree_tall", 1.69f), Tree("tree_simple", 1.52f), Tree("tree_pineRoundA", 1.37f)],
        "mouldy" => [TownTrees[0], Tree("tree_fat", 1.15f), Tree("tree_oak", 1.23f), Tree("tree_pineRoundA", 1.37f), Tree("tree_cone", 1.43f)],
        "woods" => WoodsTrees,
        _ => [.. TownTrees, Tree("tree_oak", 1.23f), Tree("tree_fat", 1.15f), Tree("tree_default", 1.71f)],
    };

    private static readonly (string Path, float Scale)[] Rocks =
    [
        (Kit + "rock-large.glb", 0.6f), (Nature + "rock_largeA.glb", 0.7f), (Nature + "rock_largeB.glb", 0.7f),
        (Nature + "rock_largeC.glb", 0.65f), (Nature + "rock_largeD.glb", 0.6f), (Nature + "rock_tallA.glb", 0.5f),
    ];

    private static readonly (string Path, float Scale)[] Flowers =
        [(Nature + "flower_yellowA.glb", 1.2f), (Nature + "flower_redA.glb", 1.2f), (Nature + "flower_purpleA.glb", 1.1f), (Nature + "flower_yellowB.glb", 1.4f)];

    private static readonly (string Path, float Scale)[] Hedges =
        [(Nature + "plant_bushSmall.glb", 1.8f), (Nature + "plant_bush.glb", 1.8f), (Nature + "plant_bushLarge.glb", 1.9f)];

    private static readonly (string Path, float Scale)[] Mushrooms = [(Nature + "mushroom_tan.glb", 2.4f), (Nature + "mushroom_redGroup.glb", 1.6f)];

    // On a grove's rim: bushes, a fern, an old stump, a fallen log.
    private static readonly (string Path, float Scale)[] RimPieces =
    [
        (Nature + "plant_bushSmall.glb", 1.5f), (Nature + "plant_bush.glb", 1.4f), (Nature + "plant_bushDetailed.glb", 1.0f),
        (Nature + "plant_flatTall.glb", 1.3f), (Nature + "stump_old.glb", 1.2f), (Nature + "log.glb", 1.1f),
    ];

    // On the walkable grass, under 0.15 high.
    private static readonly (string Path, float Scale)[] Grasses =
    [
        (Nature + "grass.glb", 0.55f), (Nature + "grass_large.glb", 0.55f), (Nature + "grass_leafs.glb", 1.0f), (Nature + "plant_flatShort.glb", 0.6f),
        (Nature + "flower_yellowA.glb", 0.75f), (Nature + "flower_purpleA.glb", 0.6f), (Nature + "flower_yellowB.glb", 0.9f), (Nature + "stump_round.glb", 0.6f),
    ];

    // Far in a made-up skirt, decorative cliffs and hills.
    private static readonly (string Path, float Scale)[] Cliffs =
        [(Nature + "cliff_block_rock.glb", 1), (Nature + "cliff_large_rock.glb", 1), (Nature + "cliff_half_rock.glb", 1), (Nature + "cliff_blockHalf_rock.glb", 1)];

    private static readonly Color DryGrass = new("9a9050");

    private static readonly Color Foliage = new(1.5f, 1.6f, 1.45f);

    private static readonly Dictionary<Material, Material> _shaded = [];

    /// <summary>Multiplies the green surfaces of a model by a colour (its trunk untouched), one material per original (one colour for all, <see cref="Foliage"/>).</summary>
    private static void Shade(Node node, Color by)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh is not null)
        {
            for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                if (mesh.Mesh.SurfaceGetMaterial(i) is not StandardMaterial3D m || m.AlbedoColor.G <= m.AlbedoColor.R)
                    continue;
                if (!_shaded.TryGetValue(m, out Material? dark))
                {
                    var copy = (StandardMaterial3D)m.Duplicate();
                    copy.AlbedoColor = m.AlbedoColor * by;
                    _shaded[m] = dark = copy;
                }
                mesh.SetSurfaceOverrideMaterial(i, dark);
            }
        }
        foreach (Node child in node.GetChildren())
            Shade(child, by);
    }

    private static StandardMaterial3D Grass(Color colour) => new() { AlbedoColor = colour, Roughness = 1 };

    /// <summary>The tree models drawn on this map, and how many trees stand at the exact middle of their cell (for the self-test).</summary>
    public HashSet<string> TreeModels { get; } = [];

    public int CentredTrees { get; private set; }

    /// <summary>How many tufts of grass and small flowers grow on the walkable grass (for the self-test).</summary>
    public int TuftCount { get; private set; }

    /// <summary>A piece of decor shifted, turned and scaled in its cell, its model drawn among several (sprint 178, <see cref="DecorScatter"/>).</summary>
    private Node3D Scattered(Cell c, char kind, Vector3 at, (string Path, float Scale)[] models)
    {
        Scatter s = DecorScatter.For(c, kind, models.Length);
        (string path, float scale) = models[s.Variant];
        Node3D piece = Place(path, at + new Vector3(s.Dx, 0, s.Dz), s.Turn, scale * s.Scale);
        if (kind == 'T')
        {
            // The nature kit's foliage is a deep green beside the town kit's pale mint: lightened to sit together.
            if (path.StartsWith(Nature, StringComparison.Ordinal))
                Shade(piece, Foliage);
            TreeModels.Add(path);
            if (s.Dx == 0 && s.Dz == 0)
                CentredTrees++;
        }
        return piece;
    }

    /// <summary>
    /// The tufts of grass, the ferns and the small flowers on one walkable cell in eight (sprint 178), never where
    /// something stands or is to be clicked; lower than a sixth of a cell, they hide nothing.
    /// </summary>
    private void Tufts(Town town, TownController controller)
    {
        HashSet<Cell> taken = [.. town.Npcs.Select(n => n.At), .. town.Exits.Select(e => e.At), .. (town.Links ?? []).Select(l => l.At),
            .. town.Doors.Select(d => d.At), .. (town.Props ?? []).Select(p => p.At), .. (town.Workshops ?? []).Select(w => w.At),
            .. controller.Nodes.Select(n => n.At), .. controller.Groups.Values];
        (string Path, float Scale)[] tufts = town.ZoneId == "mouldy" ? [.. Grasses, (Nature + "mushroom_redGroup.glb", 0.55f)] : Grasses;
        foreach (Sprig t in DecorScatter.Tufts(town, taken.Contains, tufts.Length))
        {
            (string path, float scale) = tufts[t.Variant];
            Shade(Place(path, BoardView.ToWorld(t.At) + new Vector3(t.Dx, 0, t.Dz), t.Turn, scale), Foliage);
            TuftCount++;
        }
    }

    /// <summary>The cells of the skirt drawn past the edges, and the pieces of decor on them (for the self-test).</summary>
    public IReadOnlyList<SkirtCell> SkirtCells { get; private set; } = [];

    public int SkirtPieces { get; private set; }

    private static readonly Color Background = new("1f1f1f");
    private static readonly Dictionary<(Material, int), Material> _faded = [];
    private static float? _tileTop;

    private static readonly Color Mud = new("5a4630");
    private static readonly Color Road = new("8a7a5c");

    /// <summary>
    /// The ground (sprint 179, D90): one mesh over the map and its skirt (sprint 178), a quad a cell, its corners at
    /// the relief's heights and lit by its slope, coloured by the cells around them (the zone's tones in patches,
    /// drier along the roads, mud along the water), the skirt's darker and greyer outwards until it melts into the
    /// background. Then the water over it, and reeds and water lilies.
    /// </summary>
    private void Ground(TownController controller, StandardMaterial3D[] grass, StandardMaterial3D[] dry)
    {
        Town town = controller.Town;
        float tileTop = _tileTop ??= TileTop();
        int w = town.Width, h = town.Rows.Count;
        Dictionary<Cell, SkirtCell> skirt = SkirtCells.ToDictionary(s => s.At);
        int margin = SkirtCells.Count == 0 ? 0 : MapSkirt.Depth;
        bool OnMap(Cell c) => c.X >= 0 && c.X < w && c.Y >= 0 && c.Y < h;
        char? Kind(Cell c) => OnMap(c) ? town[c] : skirt.TryGetValue(c, out SkirtCell s) ? s.Kind : null;
        bool IsWater(Cell c) => Kind(c) == '~';
        Color? CellColour(Cell c) => Kind(c) switch
        {
            null => null,
            '~' => Mud,
            '=' => Road,
            _ when PlainGround => grass[0].AlbedoColor,
            _ when OnMap(c) && DecorScatter.Dry(town, c) => dry[DecorScatter.Tone(c)].AlbedoColor,
            _ => grass[DecorScatter.Tone(c)].AlbedoColor,
        };
        // A corner's colour: the cells around it, faded as far as the corner lies from the map.
        Color Corner(int x, int y)
        {
            Color sum = new(0, 0, 0);
            int n = 0;
            foreach (Cell c in new Cell[] { new(x - 1, y - 1), new(x, y - 1), new(x - 1, y), new(x, y) })
            {
                if (CellColour(c) is Color colour)
                {
                    sum += colour;
                    n++;
                }
            }
            float away = Math.Max(Math.Max(-x, x - w), Math.Max(-y, y - h));
            return n == 0 ? Background : Faded(sum / n, MapSkirt.Fade(away));
        }
        Vector3 Point(int x, int y) => new(x - 0.5f, tileTop + Relief.Corner(x, y), y - 0.5f);
        Vector3 Normal(int x, int y) => new Vector3(Relief.Corner(x - 1, y) - Relief.Corner(x + 1, y), 2, Relief.Corner(x, y - 1) - Relief.Corner(x, y + 1)).Normalized();
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int y = -margin; y < h + margin; y++)
        {
            for (int x = -margin; x < w + margin; x++)
            {
                (int X, int Y)[] quad = [(x, y), (x + 1, y), (x + 1, y + 1), (x, y + 1)];
                foreach (int i in new[] { 0, 1, 2, 0, 2, 3 })
                {
                    st.SetNormal(Normal(quad[i].X, quad[i].Y));
                    st.SetColor(Corner(quad[i].X, quad[i].Y));
                    st.AddVertex(Point(quad[i].X, quad[i].Y));
                }
            }
        }
        var ground = new MeshInstance3D
        {
            Mesh = st.Commit(),
            MaterialOverride = new StandardMaterial3D { VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true, Roughness = 1 },
            Name = "Ground",
        };
        AddChild(ground);
        _onRelief.Add(ground);
        Water(controller, IsWater, margin);
    }

    /// <summary>How many water cells are drawn as rounded puddles and ponds, and how many reeds and lilies (for the self-test).</summary>
    public (int Cells, int Reeds, int Lilies) WaterShown { get; private set; }

    /// <summary>
    /// The water (sprint 179, D90): every water cell of the map and its skirt a rounded polygon (<see cref="Waters"/>)
    /// a little over the ground, all in one glossy mesh, the skirt's faded; reeds on the shore and water lilies on the
    /// map's own water, never where something stands or is to be clicked.
    /// </summary>
    private void Water(TownController controller, Func<Cell, bool> isWater, int margin)
    {
        Town town = controller.Town;
        int w = town.Width, h = town.Rows.Count;
        Color deep = town.ZoneId == "lake" ? new Color("4f9cc4") : new Color("4f93a3");
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        int cells = 0;
        for (int y = -margin; y < h + margin; y++)
        {
            for (int x = -margin; x < w + margin; x++)
            {
                Cell c = new(x, y);
                if (!isWater(c))
                    continue;
                cells++;
                float away = Math.Max(Math.Max(-x, x - w + 1), Math.Max(-y, y - h + 1)) - 0.5f;
                Color colour = Faded(deep, MapSkirt.Fade(away));
                float top = _tileTop!.Value + Relief.At(c) + 0.035f;
                IReadOnlyList<(float X, float Z)> outline = Waters.Outline(isWater, c);
                for (int i = 0; i < outline.Count; i++)
                {
                    (float ax, float az) = outline[i];
                    (float bx, float bz) = outline[(i + 1) % outline.Count];
                    foreach (Vector3 v in new[] { new Vector3(x, top, y), new Vector3(x + ax, top, y + az), new Vector3(x + bx, top, y + bz) })
                    {
                        st.SetNormal(Vector3.Up);
                        st.SetColor(colour);
                        st.AddVertex(v);
                    }
                }
            }
        }
        if (cells == 0)
            return;
        var water = new MeshInstance3D
        {
            Mesh = st.Commit(),
            MaterialOverride = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true,
                VertexColorIsSrgb = true,
                Roughness = 0.2f,
                MetallicSpecular = 0.8f,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            },
            Name = "Water",
        };
        AddChild(water);
        _onRelief.Add(water);
        HashSet<Cell> taken = [.. town.Npcs.Select(n => n.At), .. town.Exits.Select(e => e.At), .. (town.Links ?? []).Select(l => l.At),
            .. town.Doors.Select(d => d.At), .. (town.Props ?? []).Select(p => p.At), .. (town.Workshops ?? []).Select(k => k.At),
            .. controller.Nodes.Select(n => n.At), .. controller.Groups.Values, town.Spawn];
        int reeds = 0, lilies = 0;
        foreach (Cell c in controller.Board.Cells())
        {
            if (Waters.Reeds(isWater, c, x => town[x] == '.' && !taken.Contains(x)))
            {
                Reeds(c, isWater);
                reeds++;
            }
            else if (Waters.Lily(isWater, c, taken.Contains))
            {
                Lily(c);
                lilies++;
            }
        }
        WaterShown = (cells, reeds, lilies);
    }

    private static StandardMaterial3D? _reed, _lily;

    /// <summary>A clump of reeds on the shore, leaning out over the water from the side of the cell that faces it.</summary>
    private void Reeds(Cell c, Func<Cell, bool> isWater)
    {
        _reed ??= new StandardMaterial3D { AlbedoColor = new Color("8fa85a"), Roughness = 0.9f };
        Cell toward = new[] { new Cell(1, 0), new Cell(-1, 0), new Cell(0, 1), new Cell(0, -1) }.First(d => isWater(c + d));
        var clump = new Node3D { Position = BoardView.ToWorld(c) + new Vector3(toward.X * 0.36f, 0, toward.Y * 0.36f) };
        AddChild(clump);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 1.1f + DecorScatter.Unit(c, (uint)i) * 0.6f, r = 0.04f + 0.05f * (i % 3);
            float height = 0.38f + 0.14f * DecorScatter.Unit(c, 10 + (uint)i);
            clump.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.006f, BottomRadius = 0.016f, Height = height, RadialSegments = 5, Rings = 1 },
                MaterialOverride = _reed,
                Position = new Vector3(Mathf.Cos(a) * r, height / 2, Mathf.Sin(a) * r),
                RotationDegrees = new Vector3(toward.Y * 8 + (i % 2 == 0 ? 4 : -4), 0, -toward.X * 8),
            });
        }
    }

    /// <summary>A water lily: a round leaf with its notch, floating, now and then a white flower on it.</summary>
    private void Lily(Cell c)
    {
        _lily ??= new StandardMaterial3D { AlbedoColor = new Color("4f8a3a"), Roughness = 0.6f };
        Vector3 at = BoardView.ToWorld(c) + new Vector3((DecorScatter.Unit(c, 0x1112) - 0.5f) * 0.4f, 0.04f, (DecorScatter.Unit(c, 0x1113) - 0.5f) * 0.4f);
        var leaf = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.13f, BottomRadius = 0.13f, Height = 0.012f, RadialSegments = 14, Rings = 1 },
            MaterialOverride = _lily,
            Position = at,
            RotationDegrees = new Vector3(0, DecorScatter.Unit(c, 0x1114) * 360, 0),
        };
        AddChild(leaf);
        if (DecorScatter.Hash(c, 0x1115) % 3 == 0)
            Part(leaf, new SphereMesh { Radius = 0.045f, Height = 0.06f, RadialSegments = 8, Rings = 4 }, new Color("f2efe6"), new Vector3(0.03f, 0.03f, 0));
    }

    /// <summary>The height of a ground tile's top, where the skirt's ground lies to meet it.</summary>
    private static float TileTop()
    {
        Node3D tile = Looks.Nature("ground_grass").Instantiate<Node3D>();
        // Through every node down to the mesh: the kit's tile is lowered under its root.
        float Top(MeshInstance3D m)
        {
            Transform3D t = Transform3D.Identity;
            for (Node3D? n = m; n is not null && n != tile; n = n.GetParent() as Node3D)
                t = n.Transform * t;
            return (t * m.GetAabb()).End.Y;
        }
        float top = tile.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().Select(Top).DefaultIfEmpty(0).Max();
        tile.Free();
        return top;
    }

    /// <summary>The decor of a skirt cell: the map's models, scattered as on a map, then faded as deep as it lies.</summary>
    private void SkirtDecor(SkirtCell s)
    {
        Vector3 at = BoardView.ToWorld(s.At);
        Scatter sc = DecorScatter.For(s.At, s.Kind, 4);
        Vector3 spot = at + new Vector3(sc.Dx, 0, sc.Dz);
        (string Path, float Scale)[]? models = s.Kind switch
        {
            'T' when s.From?.StartsWith("ancestors", StringComparison.Ordinal) == true => [(Nature + "tree_pineDefaultA.glb", 1.5f)],
            'T' => TreesOf(s.From is null ? Controller.Town.ZoneId : Controller.Data.Towns[s.From].ZoneId),
            'R' => Rocks,
            'B' => Hedges,
            'M' => Mushrooms,
            ',' => Flowers,
            '^' => Cliffs,
            _ => null,
        };
        Node3D? piece = null;
        if (models is not null)
        {
            (string path, float scale) = models[sc.Variant % models.Length];
            // A cliff squarely on its cell, turned by quarters, at its size; the rest scattered.
            piece = s.Kind == '^' ? Place(path, at, 90 * (sc.Variant % 4), scale) : Place(path, spot, sc.Turn, scale * sc.Scale);
            if (s.Kind == 'T' && path.StartsWith(Nature, StringComparison.Ordinal))
                Shade(piece, Foliage);
        }
        else if (s.Kind == 'H')
        {
            piece = new Node3D { Position = at + new Vector3(0.5f, 0, 0.5f) };
            AddChild(piece);
            foreach ((string part, float turn) in new[] { ("wall-door", -90f), ("wall-window-shutters", 0f), ("wall", 90f), ("wall-window-shutters", 180f) })
                piece.AddChild(Positioned(Model(Kit + part + ".glb"), Vector3.Zero, turn, 2));
            piece.AddChild(Positioned(Model(Kit + "roof-point.glb"), new Vector3(0, 2, 0), 0, 2));
        }
        if (piece is null)
            return;
        Fade(piece, s.Depth);
        SkirtPieces++;
    }

    private static Node3D Positioned(Node3D model, Vector3 at, float turn, float scale)
    {
        model.Position = at;
        model.RotationDegrees = new Vector3(0, turn, 0);
        model.Scale = new Vector3(scale, scale, scale);
        return model;
    }

    /// <summary>A colour greyed and darkened towards the background by <paramref name="fade"/> (0 none, 1 the background).</summary>
    private static Color Faded(Color c, float fade)
    {
        fade = Math.Clamp(fade, 0, 1);
        float grey = c.R * 0.3f + c.G * 0.59f + c.B * 0.11f;
        Color greyed = c.Lerp(new Color(grey, grey, grey), fade * 0.7f);
        return greyed.Lerp(Background, fade);
    }

    /// <summary>Every surface of a model faded as deep in the skirt as it stands: one material per original and depth.</summary>
    private static void Fade(Node node, int depth)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh is not null)
        {
            for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                if ((mesh.GetSurfaceOverrideMaterial(i) ?? mesh.Mesh.SurfaceGetMaterial(i)) is not BaseMaterial3D m)
                    continue;
                if (!_faded.TryGetValue((m, depth), out Material? faded))
                {
                    var copy = (BaseMaterial3D)m.Duplicate();
                    copy.AlbedoColor = Faded(m.AlbedoColor, MapSkirt.Fade(depth - 0.5f));
                    _faded[(m, depth)] = faded = copy;
                }
                mesh.SetSurfaceOverrideMaterial(i, faded);
            }
        }
        foreach (Node child in node.GetChildren())
            Fade(child, depth);
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

    /// <summary>
    /// A fence (sprint 104, no free model of one): a post on each cell and two rails to the next fence
    /// east and south, in a lighter wood than the palisade.
    /// </summary>
    private void Fence(Cell c)
    {
        Fences++;
        Vector3 at = BoardView.ToWorld(c);
        Color wood = new("9a673c");
        Part(this, new BoxMesh { Size = new Vector3(0.12f, 0.8f, 0.12f) }, wood, at + new Vector3(0, 0.4f, 0));
        Town town = Controller.Town;
        foreach (Cell d in new[] { new Cell(1, 0), new Cell(0, 1) })
        {
            if (town[c + d] != 'X')
                continue;
            Vector3 mid = at + new Vector3(d.X * 0.5f, 0, d.Y * 0.5f);
            Vector3 size = d.X == 1 ? new Vector3(1, 0.07f, 0.05f) : new Vector3(0.05f, 0.07f, 1);
            foreach (float h in new[] { 0.35f, 0.65f })
                Part(this, new BoxMesh { Size = size }, wood.Darkened(0.1f), mid + new Vector3(0, h, 0));
        }
    }

    /// <summary>Léonie's hen house (sprint 104), over 2 × 2 cells: a cabin of planks on stilts, a ramp to its door, a roof.</summary>
    private void HenHouse(Vector3 centre)
    {
        Sheds++;
        Color planks = new("b07a48"), dark = new("5a3a22");
        foreach ((float x, float z) in new[] { (-0.6f, -0.5f), (0.6f, -0.5f), (-0.6f, 0.5f), (0.6f, 0.5f) })
            Part(this, new BoxMesh { Size = new Vector3(0.1f, 0.35f, 0.1f) }, dark, centre + new Vector3(x, 0.17f, z));
        Part(this, new BoxMesh { Size = new Vector3(1.4f, 0.8f, 1.2f) }, planks, centre + new Vector3(0, 0.75f, 0));
        Part(this, new BoxMesh { Size = new Vector3(0.3f, 0.4f, 0.04f) }, dark, centre + new Vector3(0.3f, 0.6f, 0.61f));
        Part(this, new BoxMesh { Size = new Vector3(0.3f, 0.04f, 0.6f) }, planks.Darkened(0.2f), centre + new Vector3(0.3f, 0.22f, 0.85f), new Vector3(-35, 0, 0));
        Place(Kit + "roof-point.glb", centre + new Vector3(0, 1.15f, 0), 0, new Vector3(1.6f, 0.8f, 1.4f));
    }

    /// <summary>The stables (sprint 104), over 2 × 2 cells: three walls of planks open to the south, hay inside, a roof on posts.</summary>
    private void Stable(Vector3 centre)
    {
        Sheds++;
        Color planks = new("8a5a34"), hay = new("e3c35a");
        Part(this, new BoxMesh { Size = new Vector3(1.9f, 1.4f, 0.1f) }, planks, centre + new Vector3(0, 0.7f, -0.9f));
        Part(this, new BoxMesh { Size = new Vector3(0.1f, 1.4f, 1.9f) }, planks, centre + new Vector3(-0.9f, 0.7f, 0));
        Part(this, new BoxMesh { Size = new Vector3(0.1f, 1.4f, 1.9f) }, planks, centre + new Vector3(0.9f, 0.7f, 0));
        foreach (float x in new[] { -0.9f, 0f, 0.9f })
            Part(this, new BoxMesh { Size = new Vector3(0.12f, 1.4f, 0.12f) }, planks.Darkened(0.3f), centre + new Vector3(x, 0.7f, 0.9f));
        Part(this, new BoxMesh { Size = new Vector3(0.7f, 0.3f, 0.5f) }, hay, centre + new Vector3(-0.5f, 0.15f, -0.55f));
        Part(this, new BoxMesh { Size = new Vector3(0.5f, 0.25f, 0.4f) }, hay.Darkened(0.1f), centre + new Vector3(0.45f, 0.12f, -0.6f));
        Place(Kit + "roof-point.glb", centre + new Vector3(0, 1.4f, 0), 0, new Vector3(2.1f, 1f, 2.1f));
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

    /// <summary>
    /// A person of the map: its model and, an inhabitant, its name above it; a player (<paramref name="player"/>), since sprint 173
    /// (D89) its guild's name, its name under it and its title, shown on hovering it only (<see cref="Tag"/>).
    /// </summary>
    private static Node3D Person(Hero appearance, string? name, string? guild = null, string? title = null, GuildCrest? crest = null, bool player = false)
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
        if (player)
            Tag(root, name, guild, title, crest);
        else if (name is not null)
            root.AddChild(Label(name, new Vector3(0, (float)NameTags.Lowest, 0), 40));
        return root;
    }

    /// <summary>The words over a player, drawn again: from the top its guild (its crest on the left), its name, its title; hidden unless hovered.</summary>
    private static void Tag(Node3D root, string? name, string? guild, string? title, GuildCrest? crest)
    {
        foreach (string drawn in Tags)
        {
            if (root.FindChild(drawn, false, false) is Node3D old)
            {
                root.RemoveChild(old);
                old.QueueFree();
            }
        }
        bool shown = root.HasMeta(Hovered);
        foreach ((NameTagLine line, double at) in NameTags.Layout(guild is not null, name is not null, title is not null))
        {
            var y = new Vector3(0, (float)at, 0);
            Label3D label = line switch
            {
                NameTagLine.Guild => Label($"‹{guild}›", y, 30),
                NameTagLine.Name => Label(name!, y, 40),
                _ => Label(title!, y, 30),
            };
            label.Name = line.ToString();
            label.Visible = shown;
            if (line == NameTagLine.Guild)
                label.Modulate = FighterView.PlayerColour;
            else if (line == NameTagLine.Title)
                label.Modulate = new Color("d9b38c");
            root.AddChild(label);
            if (line == NameTagLine.Guild && crest is not null)
            {
                float width = ThemeDB.FallbackFont.GetStringSize(label.Text, HorizontalAlignment.Left, -1, label.FontSize).X * label.PixelSize;
                Node3D sprites = CrestView.Sprites(crest, label.Position, width);
                sprites.Visible = shown;
                root.AddChild(sprites);
            }
        }
    }

    private static readonly string[] Tags = [nameof(NameTagLine.Guild), nameof(NameTagLine.Name), nameof(NameTagLine.Title), "Crest"];
    private const string Hovered = "hovered";

    private static void ShowTag(Node3D root, bool shown)
    {
        if (shown)
            root.SetMeta(Hovered, true);
        else if (root.HasMeta(Hovered))
            root.RemoveMeta(Hovered);
        foreach (string drawn in Tags)
        {
            if (root.FindChild(drawn, false, false) is Node3D node)
                node.Visible = shown;
        }
    }

    /// <summary>
    /// The player under the mouse, if any (the player's own character among them, the nearest to the camera first), its words
    /// shown and every other player's hidden (sprint 173, D89).
    /// </summary>
    public Node3D? HoverAt(Camera3D camera, Vector2 mouse)
    {
        ArgumentNullException.ThrowIfNull(camera);
        Node3D? over = null;
        float nearest = float.MaxValue;
        foreach (Node3D person in Others.Values.Prepend(Player))
        {
            Vector3 feet = person.GlobalPosition, head = feet + (Vector3.Up * (float)NameTags.Height * person.Scale.Y);
            if (camera.IsPositionBehind(feet))
                continue;
            Vector2 f = camera.UnprojectPosition(feet), h = camera.UnprojectPosition(head);
            float distance = camera.GlobalPosition.DistanceTo(feet);
            if (NameTags.Over(f.X, f.Y, h.Y, mouse.X, mouse.Y) && distance < nearest)
                (over, nearest) = (person, distance);
        }
        foreach (Node3D person in Others.Values.Prepend(Player))
            ShowTag(person, person == over);
        return over;
    }

    /// <summary>Whether a player's words are shown (for the self-test): the player's own with null.</summary>
    public bool TagShown(Guid? other = null) =>
        (other is Guid id ? Others.GetValueOrDefault(id) : Player) is Node3D person && person.FindChild(nameof(NameTagLine.Name), false, false) is Label3D { Visible: true };

    /// <summary>The words over a player from the top, shown or not (for the self-test): the player's own with null.</summary>
    public IReadOnlyList<string> TagLines(Guid? other = null) =>
        (other is Guid id ? Others.GetValueOrDefault(id) : Player) is Node3D person
            ? [.. person.GetChildren().OfType<Label3D>().Where(l => Tags.Contains((string)l.Name)).OrderByDescending(l => l.Position.Y).Select(l => l.Text)]
            : [];

    private string? _playerName;

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

    // The "!" and "?" over the inhabitants (sprint 105), apart from their names, always seen.
    private readonly Dictionary<string, Label3D> _marks = [];

    /// <summary>"!" over who has a quest to give, "?" over who waits for one (grey while it is not done), from the character's book.</summary>
    public void ShowMarks(QuestBook? book)
    {
        foreach (Npc npc in Controller.Town.Npcs)
        {
            QuestMark mark = book?.Mark(GameData.Embedded.RefOf(Controller.Town, npc)) ?? QuestMark.None;
            if (!_marks.TryGetValue(npc.Id, out Label3D? label))
            {
                label = Label("", W(npc.At) + new Vector3(0, 1.9f, 0), 72);
                AddChild(label);
                _marks[npc.Id] = label;
            }
            label.Text = mark switch { QuestMark.Offer => "!", QuestMark.Ready or QuestMark.Waiting => "?", _ => "" };
            label.Modulate = mark == QuestMark.Waiting ? new Color(0.75f, 0.75f, 0.75f) : WorldLight;
            label.Visible = mark != QuestMark.None;
        }
        // The clues the quests under way still ask for glint (sprint 105).
        foreach ((string id, Node3D glint) in _clues)
            glint.Visible = book?.Interacts(id).Count > 0;
    }

    // The ring on the cell the quest followed leads to on this map (sprint 106), a goal or a way out.
    private MeshInstance3D? _trail;

    /// <summary>The cell the trail ring is on, if shown (for the self-test).</summary>
    public Cell? TrailAt { get; private set; }

    /// <summary>Puts the trail's yellow ring on a cell of the map, or takes it away.</summary>
    public void ShowTrail(Cell? at)
    {
        TrailAt = at;
        _trail ??= new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.36f, OuterRadius = 0.5f, Rings = 24 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = WorldLight, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
            Scale = new Vector3(1, 0.12f, 1),
        };
        if (_trail.GetParent() is null)
            AddChild(_trail);
        _trail.Visible = at is not null;
        if (at is Cell c)
            _trail.Position = W(c) + new Vector3(0, 0.07f, 0);
    }

    /// <summary>The yellow of the marks, of the lanterns' light on the world map.</summary>
    private static readonly Color WorldLight = new("ffd85a");

    /// <summary>The mark shown over an inhabitant, "" for none (for the self-test).</summary>
    public string MarkShown(string npc) => _marks.TryGetValue(npc, out Label3D? l) && l.Visible ? l.Text : "";

    // The things to examine (sprint 105) and the glint over each.
    private readonly Dictionary<string, Node3D> _clues = [];

    /// <summary>Whether the glint over a thing to examine is shown (for the self-test).</summary>
    public bool ClueShown(string prop) => _clues.TryGetValue(prop, out Node3D? g) && g.Visible;

    /// <summary>The things to examine drawn (sprint 105), for the self-test.</summary>
    public int PropsDrawn => _clues.Count;

    /// <summary>
    /// A thing to examine (sprint 105): the old stone a standing stone with its four signs carved on its south face;
    /// since sprint 112 claw marks on a stump, a ring of roots, a knot of threads, the crypt's seal; since sprint 129 the
    /// signpost, the rockfall, the flooded ford and the blaze across Quatre-Chemins's shut roads; any other a small
    /// cairn until it has its own look. A glint turns over it while a quest asks for it.
    /// </summary>
    private void AddProp(Prop prop)
    {
        Vector3 at = BoardView.ToWorld(prop.At);
        Color grey = new("8f8d86");
        if (prop.Look == "hatch")
        {
            // A wooden hatch in the ground by the granary, its gnawed latch.
            Part(this, new BoxMesh { Size = new Vector3(0.8f, 0.08f, 0.8f) }, new Color("8a5a34"), at + new Vector3(0, 0.04f, 0));
            foreach (float z in new[] { -0.2f, 0f, 0.2f })
                Part(this, new BoxMesh { Size = new Vector3(0.82f, 0.02f, 0.03f) }, new Color("5a3a22"), at + new Vector3(0, 0.09f, z));
            Part(this, new BoxMesh { Size = new Vector3(0.12f, 0.04f, 0.06f) }, new Color("9a9a96"), at + new Vector3(0.3f, 0.1f, 0));
        }
        else if (prop.Look == "grave")
        {
            // An old grave: a slab and a headstone, mossy.
            Part(this, new BoxMesh { Size = new Vector3(0.5f, 0.08f, 0.8f) }, grey, at + new Vector3(0, 0.04f, 0));
            Part(this, new BoxMesh { Size = new Vector3(0.5f, 0.7f, 0.12f) }, grey.Darkened(0.1f), at + new Vector3(0, 0.35f, -0.4f));
            Part(this, new BoxMesh { Size = new Vector3(0.3f, 0.06f, 0.4f) }, new Color("5f7a3a"), at + new Vector3(0.05f, 0.09f, 0.1f));
        }
        else if (prop.Look == "stone")
        {
            Part(this, new BoxMesh { Size = new Vector3(0.55f, 1.5f, 0.35f) }, grey, at + new Vector3(0, 0.75f, 0), new Vector3(0, 0, 3));
            foreach ((float x, float y) in new[] { (-0.12f, 1.15f), (0.12f, 1.15f), (-0.12f, 0.8f), (0.12f, 0.8f) })
                Part(this, new BoxMesh { Size = new Vector3(0.12f, 0.12f, 0.02f) }, new Color("4a4844"), at + new Vector3(x, y, 0.18f));
            Part(this, new BoxMesh { Size = new Vector3(0.025f, 1.0f, 0.02f) }, new Color("2e2c29"), at + new Vector3(0.05f, 0.7f, 0.181f), new Vector3(0, 0, 12));
        }
        else if (prop.Look == "claws")
        {
            // A stump scored with claw marks (sprint 112).
            Part(this, new CylinderMesh { TopRadius = 0.22f, BottomRadius = 0.26f, Height = 0.7f }, new Color("6b4a2b"), at + new Vector3(0, 0.35f, 0));
            foreach (float y in new[] { 0.3f, 0.42f, 0.54f })
                Part(this, new BoxMesh { Size = new Vector3(0.3f, 0.03f, 0.02f) }, new Color("e8d8b0"), at + new Vector3(0, y, 0.25f), new Vector3(0, 0, -18));
        }
        else if (prop.Look == "roots")
        {
            // Thick roots in a ring around a mound.
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.Tau / 8;
                Part(this, new BoxMesh { Size = new Vector3(0.36f, 0.12f, 0.12f) }, new Color("5a3a22"), at + new Vector3(Mathf.Cos(a) * 0.38f, 0.06f, Mathf.Sin(a) * 0.38f), new Vector3(0, -Mathf.RadToDeg(a) + 90, 0));
            }
            Part(this, new SphereMesh { Radius = 0.22f, Height = 0.2f }, new Color("6f5a3a"), at + new Vector3(0, 0.05f, 0));
        }
        else if (prop.Look == "mycelium")
        {
            // White threads in a knot over a shard.
            Part(this, new BoxMesh { Size = new Vector3(0.3f, 0.12f, 0.2f) }, grey, at + new Vector3(0, 0.06f, 0), new Vector3(0, 20, 0));
            for (int i = 0; i < 5; i++)
                Part(this, new BoxMesh { Size = new Vector3(0.6f, 0.03f, 0.03f) }, new Color("f2efe6"), at + new Vector3(0, 0.1f + 0.02f * i, 0), new Vector3(0, i * 36, 8 * (i % 2 == 0 ? 1 : -1)));
        }
        else if (prop.Look == "reeds")
        {
            // A clump of tall reeds (sprint 113).
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f;
                Part(this, new CylinderMesh { TopRadius = 0.015f, BottomRadius = 0.025f, Height = 0.9f + 0.1f * (i % 3) }, new Color("8fa85a"), at + new Vector3(Mathf.Cos(a) * 0.15f * (i % 3), 0.45f, Mathf.Sin(a) * 0.15f * (i % 3)), new Vector3(4 * (i % 2 == 0 ? 1 : -1), 0, 6));
            }
        }
        else if (prop.Look == "tunnel")
        {
            // A mole's tunnel: a mound of earth around a dark mouth.
            Part(this, new SphereMesh { Radius = 0.3f, Height = 0.3f }, new Color("6f5136"), at + new Vector3(0, 0.05f, 0));
            Part(this, new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.12f, Height = 0.04f }, new Color("1f1812"), at + new Vector3(0, 0.19f, 0));
        }
        else if (prop.Look == Waystones.Look)
        {
            // A waystone (sprint 127): a squat grey stone on a round base, a pistachio band of light around it.
            Part(this, new CylinderMesh { TopRadius = 0.42f, BottomRadius = 0.46f, Height = 0.12f }, grey.Darkened(0.15f), at + new Vector3(0, 0.06f, 0));
            Part(this, new BoxMesh { Size = new Vector3(0.5f, 1.0f, 0.5f) }, grey, at + new Vector3(0, 0.62f, 0), new Vector3(0, 45, 0));
            Part(this, new BoxMesh { Size = new Vector3(0.54f, 0.08f, 0.54f) }, FighterView.PlayerColour, at + new Vector3(0, 0.82f, 0), new Vector3(0, 45, 0));
        }
        else if (prop.Look == "signpost")
        {
            // Quatre-Chemins's oak signpost: four arms, a sign painted at the end of each (seed, drop, hammer, feather).
            Part(this, new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.07f, Height = 1.6f }, new Color("6b4a2b"), at + new Vector3(0, 0.8f, 0));
            foreach ((float a, string colour) in new[] { (0f, "6b8f3a"), (90f, "4a7fa8"), (180f, "c9783a"), (270f, "d8d4c4") })
            {
                Vector3 arm = new Vector3(Mathf.Cos(Mathf.DegToRad(a)), 0, Mathf.Sin(Mathf.DegToRad(a))) * 0.3f;
                Part(this, new BoxMesh { Size = new Vector3(0.55f, 0.1f, 0.04f) }, new Color("8a5a34"), at + arm + new Vector3(0, 1.35f, 0), new Vector3(0, -a, 0));
                Part(this, new BoxMesh { Size = new Vector3(0.1f, 0.1f, 0.05f) }, new Color(colour), at + arm * 1.8f + new Vector3(0, 1.35f, 0), new Vector3(0, -a, 0));
            }
        }
        else if (prop.Look == "rockfall")
        {
            // Quatre-Chemins's north road (sprint 129): boulders fallen across it, of three sizes.
            foreach ((float x, float z, float size) in new[] { (-0.25f, -0.1f, 0.5f), (0.25f, 0.15f, 0.36f), (0.05f, -0.35f, 0.28f), (-0.3f, 0.3f, 0.22f) })
                Part(this, new BoxMesh { Size = new Vector3(size, size * 0.8f, size) }, grey.Darkened(size * 0.3f), at + new Vector3(x, size * 0.4f, z), new Vector3(8, x * 90, 12));
        }
        else if (prop.Look == "ford")
        {
            // The south-west road's ford in flood: the posts of a footbridge, its plank broken and tilted.
            foreach (float x in new[] { -0.3f, 0.3f })
                Part(this, new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.06f, Height = 0.7f }, new Color("6b4a2b"), at + new Vector3(x, 0.35f, 0));
            Part(this, new BoxMesh { Size = new Vector3(0.7f, 0.05f, 0.2f) }, new Color("8a5a34"), at + new Vector3(0.05f, 0.25f, 0), new Vector3(0, 0, -18));
            Part(this, new BoxMesh { Size = new Vector3(0.9f, 0.02f, 0.5f) }, new Color("4a7fa8"), at + new Vector3(0, 0.03f, 0.1f));
        }
        else if (prop.Look == "blaze")
        {
            // The east road's blaze: charred logs and flames that do not go out.
            foreach (float a in new[] { 0f, 60f, 120f })
                Part(this, new BoxMesh { Size = new Vector3(0.7f, 0.1f, 0.1f) }, new Color("2e2620"), at + new Vector3(0, 0.06f, 0), new Vector3(0, a, 0));
            foreach ((float x, float z, float h, string colour) in new[] { (0f, 0f, 0.7f, "e8742a"), (0.15f, 0.1f, 0.45f, "f2b53a"), (-0.14f, -0.08f, 0.5f, "d9502a") })
            {
                var flame = new MeshInstance3D
                {
                    Mesh = new CylinderMesh { TopRadius = 0f, BottomRadius = 0.14f, Height = h },
                    MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(colour), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
                    Position = at + new Vector3(x, 0.1f + h / 2, z),
                };
                AddChild(flame);
            }
        }
        else if (prop.Look == "notice-board")
        {
            // The notice board (sprint 153): two posts, a board under a little roof, three notices pinned on it.
            foreach (float x in new[] { -0.32f, 0.32f })
                Part(this, new BoxMesh { Size = new Vector3(0.07f, 1.3f, 0.07f) }, new Color("6b4a2b"), at + new Vector3(x, 0.65f, 0));
            Part(this, new BoxMesh { Size = new Vector3(0.75f, 0.55f, 0.05f) }, new Color("8a5a34"), at + new Vector3(0, 0.95f, 0));
            Part(this, new BoxMesh { Size = new Vector3(0.9f, 0.05f, 0.25f) }, new Color("5a3a22"), at + new Vector3(0, 1.32f, 0), new Vector3(12, 0, 0));
            foreach ((float x, float y) in new[] { (-0.2f, 1.02f), (0.12f, 1.05f), (-0.02f, 0.84f) })
                Part(this, new BoxMesh { Size = new Vector3(0.18f, 0.2f, 0.01f) }, new Color("efe6cf"), at + new Vector3(x, y, -0.035f));
        }
        else if (prop.Look == "seal")
        {
            // A dark disc on the floor, cracked across.
            Part(this, new CylinderMesh { TopRadius = 0.42f, BottomRadius = 0.42f, Height = 0.06f }, new Color("2f2b33"), at + new Vector3(0, 0.03f, 0));
            Part(this, new BoxMesh { Size = new Vector3(0.7f, 0.02f, 0.03f) }, new Color("c9783a"), at + new Vector3(0, 0.07f, 0), new Vector3(0, 35, 0));
        }
        else
        {
            for (int i = 0; i < 3; i++)
                Part(this, new BoxMesh { Size = new Vector3(0.4f - 0.1f * i, 0.18f, 0.4f - 0.1f * i) }, grey, at + new Vector3(0, 0.09f + 0.18f * i, 0));
        }
        var glint = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.12f, Height = 0.24f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = WorldLight, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
            Position = at + new Vector3(0, prop.Look switch { "stone" => 1.85f, "signpost" => 1.85f, "grave" => 1.05f, "notice-board" => 1.6f, _ => 0.6f }, 0),
            Visible = false,
        };
        AddChild(glint);
        _clues[prop.Id] = glint;
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

    private string _lang = "en";

    public void SetLanguage(string lang)
    {
        _lang = lang;
        foreach ((Label3D label, LocalizedText text) in _exitLabels)
            label.Text = text.In(lang);
        foreach ((string id, Label3D label) in _groupLabels)
            label.Text = new Texts(lang).GroupLabel(Controller.Data.Groups[id], Controller.Data);
    }

    /// <summary>The groups of monsters drawn (sprint 72), by id: their models together on their cell.</summary>
    public Dictionary<string, Node3D> Groups { get; } = [];

    /// <summary>The beasts of the map (sprint 104), where they wander, and their models, in the same order.</summary>
    public Herd Herd { get; private set; } = null!;

    public List<Node3D> Beasts { get; } = [];

    private readonly List<int> _beastWalks = [];

    /// <summary>The hen houses, stables and fence cells drawn (sprint 104), for the self-test.</summary>
    public int Sheds { get; private set; }

    public int Fences { get; private set; }

    /// <summary>A beast walks along <paramref name="path"/> at its own pace, turned the way it goes, then stands.</summary>
    public async Task MoveBeast(int i, IReadOnlyList<Cell> path, double speed)
    {
        Node3D beast = Beasts[i];
        int me = ++_beastWalks[i];
        Animate(beast, "walk");
        foreach (Cell c in path)
        {
            Vector3 to = W(c);
            beast.Rotation = new Vector3(0, Mathf.Atan2(to.X - beast.Position.X, to.Z - beast.Position.Z), 0);
            while (beast.Position != to)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (!IsInstanceValid(beast) || _beastWalks[i] != me)
                    return;
                beast.Position = Step(beast.Position, to, (float)(speed * GetProcessDeltaTime()));
            }
        }
        Animate(beast, "idle");
    }

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
        var root = new Node3D { Position = W(at), Name = "Group " + group.Id };
        _onRelief.Add(root);
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
            Vector3 to = W(c);
            foreach (Node3D m in models)
                m.Rotation = new Vector3(0, Mathf.Atan2(to.X - root.Position.X, to.Z - root.Position.Z), 0);
            while (root.Position != to)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (_groupWalks[id] != me || !IsInstanceValid(root))
                    return;
                root.Position = Step(root.Position, to, (float)(speed * GetProcessDeltaTime()));
            }
            ShowNamesNear(Standing);
        }
        foreach (Node3D m in models)
            Animate(m, "idle");
    }

    /// <summary>A group back on the map (sprint 118): drawn again on its cell.</summary>
    public void ShowGroup(string id, Cell at)
    {
        if (Groups.TryGetValue(id, out Node3D? root))
        {
            root.Visible = true;
            PlaceGroup(id, at);
        }
        else
            AddGroup(Controller.Data.Groups[id], at, _lang);
    }

    private readonly Dictionary<string, (Node3D Mark, Label3D Seconds)> _fightMarks = [];

    /// <summary>
    /// Crossed swords where each group an ally attacks stood, with the seconds left to join (sprint 124, D79), by
    /// group id; the others taken away.
    /// </summary>
    public void MarkFights(IReadOnlyDictionary<string, string> marks)
    {
        foreach (string gone in _fightMarks.Keys.Where(k => !marks.ContainsKey(k)).ToList())
        {
            _fightMarks[gone].Mark.QueueFree();
            _fightMarks.Remove(gone);
        }
        foreach ((string id, string text) in marks)
        {
            if (!_fightMarks.TryGetValue(id, out var mark))
            {
                if (!Groups.TryGetValue(id, out Node3D? group))
                    continue;
                var root = new Node3D { Position = group.Position + new Vector3(0, 1.5f, 0), Name = "Fight " + id };
                var steel = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.9f, 0.92f), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
                foreach (float angle in new[] { 45f, -45f })
                {
                    root.AddChild(new MeshInstance3D
                    {
                        Mesh = new BoxMesh { Size = new Vector3(0.06f, 0.7f, 0.06f) },
                        MaterialOverride = steel,
                        RotationDegrees = new Vector3(0, 45, angle),
                    });
                }
                Label3D seconds = Label(text, new Vector3(0, -0.55f, 0), 40);
                seconds.Modulate = FighterView.PlayerColour;
                root.AddChild(seconds);
                AddChild(root);
                mark = (root, seconds);
                _fightMarks[id] = mark;
            }
            mark.Seconds.Text = text;
        }
    }

    /// <summary>The seconds shown by the swords over each group an ally attacks (for the self-test).</summary>
    public IReadOnlyDictionary<string, string> FightMarks => _fightMarks.ToDictionary(m => m.Key, m => m.Value.Seconds.Text);

    /// <summary>A group fought or beaten by someone (sprint 118): hidden until it is back.</summary>
    public void HideGroup(string id)
    {
        if (Groups.TryGetValue(id, out Node3D? root))
            root.Visible = false;
    }

    /// <summary>The other players drawn (sprint 118), by id: their own looks, their names above them, their guild's under.</summary>
    public Dictionary<Guid, Node3D> Others { get; } = [];

    private readonly Dictionary<Guid, Cell> _otherAt = [];
    private readonly Dictionary<Guid, string?> _otherGuild = [];
    private readonly Dictionary<Guid, string?> _otherTitle = [];
    private readonly Dictionary<Guid, string?> _otherCrest = [];

    /// <summary>The title drawn under another player's name, or null (for the self-test).</summary>
    public string? TitleShownFor(Guid id) =>
        Others.TryGetValue(id, out Node3D? person) && person.FindChild("Title", false, false) is Label3D label ? label.Text : null;

    /// <summary>
    /// The player's own guild over its own character (sprint 152, D83, the limit of T112); since sprint 173 (D89), above its
    /// name, both on hovering it only, as the other players'; none out of a guild.
    /// </summary>
    public void ShowOwnGuild(string? guild, GuildCrest? crest = null) => Tag(Player, _playerName, guild, null, crest);

    /// <summary>Whether a crest is drawn beside the player's own guild (for the self-test).</summary>
    public bool OwnCrestShown => Player.FindChild("Crest", false, false) is not null;

    /// <summary>Whether a crest is drawn beside another player's guild (for the self-test).</summary>
    public bool CrestShownFor(Guid id) => Others.TryGetValue(id, out Node3D? person) && person.FindChild("Crest", false, false) is not null;

    /// <summary>The guild's name drawn under the player's own character, or null (for the self-test).</summary>
    public string? OwnGuildShown => Player.FindChild("Guild", false, false) is Label3D label ? label.Text : null;

    /// <summary>The guild's name drawn under another player's, or null (for the self-test).</summary>
    public string? GuildShownFor(Guid id) =>
        Others.TryGetValue(id, out Node3D? person) && person.FindChild("Guild", false, false) is Label3D label ? label.Text : null;

    /// <summary>The other player drawn on a cell, mid-walk too (sprint 119: their right click).</summary>
    public Guid? OtherAt(Cell cell) =>
        Others.Where(o => new Cell(Mathf.RoundToInt(o.Value.Position.X), Mathf.RoundToInt(o.Value.Position.Z)) == cell).Select(o => (Guid?)o.Key).FirstOrDefault();
    private readonly Dictionary<Guid, int> _otherWalks = [];

    /// <summary>
    /// The other players as the server says (sprint 118): the newcomers drawn where they stand, those gone taken away,
    /// the others walking to their new cells around the decor, <paramref name="speed"/> cells a second.
    /// </summary>
    public void ShowOthers(IReadOnlyCollection<OtherPlayer> others, double speed)
    {
        ArgumentNullException.ThrowIfNull(others);
        foreach (Guid gone in Others.Keys.Except(others.Select(o => o.Id)).ToList())
        {
            Others[gone].QueueFree();
            Others.Remove(gone);
            _otherAt.Remove(gone);
            _otherGuild.Remove(gone);
            _otherTitle.Remove(gone);
            _otherCrest.Remove(gone);
        }
        foreach (OtherPlayer other in others)
        {
            // Joined a guild or left it (sprint 145): drawn again with its name, where it stands.
            if (Others.TryGetValue(other.Id, out Node3D? redrawn) && (_otherGuild.GetValueOrDefault(other.Id) != other.Guild || _otherTitle.GetValueOrDefault(other.Id) != other.Title
                || _otherCrest.GetValueOrDefault(other.Id) != other.Crest))
            {
                redrawn.QueueFree();
                Others.Remove(other.Id);
            }
            if (!Others.TryGetValue(other.Id, out Node3D? person))
            {
                person = Person(other.Look, other.Name, other.Guild, AchievementsPage.TitleOf(other.Title, _lang), GuildPage.CrestOf(other.Crest), player: true);
                person.Position = W(other.At);
                _onRelief.Add(person);
                AddChild(person);
                Others[other.Id] = person;
            }
            else if (_otherAt[other.Id] != other.At)
            {
                Board board = Controller.Board;
                IReadOnlyList<Cell> path = Wander.FindPath(board, _otherAt[other.Id], other.At, c => Controller.Town.IsFree(board, c)) ?? [other.At];
                _ = MoveOther(other.Id, path, speed);
            }
            _otherAt[other.Id] = other.At;
            _otherGuild[other.Id] = other.Guild;
            _otherTitle[other.Id] = other.Title;
            _otherCrest[other.Id] = other.Crest;
        }
    }

    private async Task MoveOther(Guid id, IReadOnlyList<Cell> path, double speed)
    {
        Node3D root = Others[id];
        int me = _otherWalks[id] = _otherWalks.GetValueOrDefault(id) + 1;
        Node3D model = root.GetChild<Node3D>(0);
        Animate(model, "walk");
        foreach (Cell c in path)
        {
            Vector3 to = W(c);
            Face(root, to - root.Position);
            while (root.Position != to)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (_otherWalks.GetValueOrDefault(id) != me || !IsInstanceValid(root))
                    return;
                root.Position = Step(root.Position, to, (float)(speed * GetProcessDeltaTime()));
            }
        }
        Animate(model, "idle");
    }

    /// <summary>Puts a group on a cell at once (the self-test).</summary>
    public void PlaceGroup(string id, Cell at)
    {
        _groupWalks[id] = _groupWalks.GetValueOrDefault(id) + 1;
        Groups[id].Position = W(at);
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
            var mark = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(0.9f, 0.9f) }, MaterialOverride = _pathMaterial, Position = W(c) + new Vector3(0, 0.05f, 0) };
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
                Vector3 to = W(c);
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
                    Player.Position = Step(Player.Position, to, stride);
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
            Face(Player, W(npc.At) - Player.Position);
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
