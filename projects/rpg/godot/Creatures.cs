using Godot;

namespace Rpg.Desktop;

/// <summary>
/// The monsters' models (sprint 72), built by the game from blocks in the spirit of Kenney's blocky
/// characters, since no free pack has every kind and role (D63): a boar's body, head, snout, tusks,
/// ears and four legs, its size, colours and details telling its role (a crested charger, a striped
/// piglet, a big grey loner, a tuskless sow, a crowned king); a wolf's lean body, long muzzle, pointed
/// ears and raised tail (sprint 73: a grey wolf, a pale howler with a dark mask, a great male with a
/// ruff, a fawn she-wolf, a black leader with a scar); a mushroom's stem and cap on two feet, a zombie's
/// green body in rags, arms held out (sprint 74). Each has an "idle" and a "walk"
/// animation, as Kenney's models do, and looks along +z.
/// </summary>
public static class Creatures
{
    /// <summary>Whether a look is one of these models ("boar-melee", "wolf-boss"...).</summary>
    public static bool Knows(string look) => Kinds.Any(k => look.StartsWith(k + "-", StringComparison.Ordinal));

    private static readonly string[] Kinds = ["boar", "wolf", "mushroom", "zombie"];
    private static readonly string[] FourLegs = ["LegFL", "LegFR", "LegBL", "LegBR"], TwoLegs = ["LegL", "LegR"];

    public static Node3D Build(string look) => look switch
    {
        "boar-melee" => Boar(1.0f, new Color("6b4a2f"), crest: true),
        "boar-ranged" => Boar(0.72f, new Color("a57a4f"), stripes: true, tusks: false),
        "boar-tank" => Boar(1.25f, new Color("4d4741"), bigTusks: true),
        "boar-support" => Boar(1.0f, new Color("8f5f3f"), tusks: false),
        "boar-boss" => Boar(1.45f, new Color("3b2a1e"), crest: true, bigTusks: true, crown: true),
        "wolf-melee" => Wolf(1.0f, new Color("7d7f85")),
        "wolf-ranged" => Wolf(0.9f, new Color("a9acb2"), mask: true),
        "wolf-tank" => Wolf(1.3f, new Color("5a4636"), ruff: true),
        "wolf-support" => Wolf(0.95f, new Color("b08a62")),
        "wolf-boss" => Wolf(1.45f, new Color("2c2c30"), ruff: true, scar: true),
        "mushroom-melee" => Mushroom(1.0f, new Color("c0392b"), spots: true, fists: true),
        "mushroom-ranged" => Mushroom(0.85f, new Color("b88a55"), puff: true),
        "mushroom-tank" => Mushroom(1.3f, new Color("7a6a58"), spots: true),
        "mushroom-support" => Mushroom(0.95f, new Color("c9a46a"), morel: true),
        "mushroom-boss" => Mushroom(1.5f, new Color("6c3483"), spots: true, fists: true, crown: true),
        "zombie-melee" => Zombie(1.0f, new Color("7fa86b"), new Color("5d4a3a")),
        "zombie-ranged" => Zombie(0.9f, new Color("9bb87a"), new Color("6e7f4f"), drool: true),
        "zombie-tank" => Zombie(1.3f, new Color("6f9a5c"), new Color("4a3f35"), bloated: true),
        "zombie-support" => Zombie(1.0f, new Color("8fae78"), new Color("e8e4d8"), stitches: true),
        "zombie-boss" => Zombie(1.5f, new Color("587a4a"), new Color("2e2a2a"), bloated: true, crown: true),
        _ => throw new KeyNotFoundException($"No creature for the look '{look}'."),
    };

    private static Node3D Boar(float size, Color hide, bool crest = false, bool stripes = false, bool tusks = true, bool bigTusks = false, bool crown = false)
    {
        var root = new Node3D { Name = "Boar" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(size, size, size) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        Color dark = hide.Darkened(0.35f), snout = new("c98b7a"), ivory = new("f1ead2");
        Block(body, new Vector3(0.46f, 0.36f, 0.72f), new Vector3(0, 0.38f, 0), hide);
        Block(body, new Vector3(0.36f, 0.32f, 0.30f), new Vector3(0, 0.42f, 0.46f), hide);
        Block(body, new Vector3(0.20f, 0.15f, 0.10f), new Vector3(0, 0.36f, 0.64f), snout);
        Block(body, new Vector3(0.05f, 0.05f, 0.02f), new Vector3(-0.09f, 0.50f, 0.615f), Colors.Black);
        Block(body, new Vector3(0.05f, 0.05f, 0.02f), new Vector3(0.09f, 0.50f, 0.615f), Colors.Black);
        Block(body, new Vector3(0.09f, 0.12f, 0.05f), new Vector3(-0.13f, 0.62f, 0.38f), dark);
        Block(body, new Vector3(0.09f, 0.12f, 0.05f), new Vector3(0.13f, 0.62f, 0.38f), dark);
        Block(body, new Vector3(0.05f, 0.05f, 0.14f), new Vector3(0, 0.44f, -0.42f), dark);
        if (tusks)
        {
            float h = bigTusks ? 0.18f : 0.11f;
            Block(body, new Vector3(0.045f, h, 0.045f), new Vector3(-0.1f, 0.36f + h / 2, 0.68f), ivory);
            Block(body, new Vector3(0.045f, h, 0.045f), new Vector3(0.1f, 0.36f + h / 2, 0.68f), ivory);
        }
        if (crest)
            Block(body, new Vector3(0.10f, 0.08f, 0.60f), new Vector3(0, 0.59f, 0.02f), dark);
        if (stripes)
        {
            foreach (float z in new[] { -0.2f, 0f, 0.2f })
                Block(body, new Vector3(0.47f, 0.05f, 0.07f), new Vector3(0, 0.50f, z), new Color("e3cfa3"));
        }
        if (crown)
        {
            var gold = new Color("e8c547");
            Block(body, new Vector3(0.30f, 0.06f, 0.24f), new Vector3(0, 0.61f, 0.46f), gold);
            foreach ((float x, float z) in new[] { (-0.12f, 0.36f), (0.12f, 0.36f), (-0.12f, 0.56f), (0.12f, 0.56f) })
                Block(body, new Vector3(0.05f, 0.09f, 0.05f), new Vector3(x, 0.68f, z), gold);
        }
        // Four legs hung from the body, so that they swing from the hip.
        foreach ((string name, float x, float z) in new[] { ("LegFL", -0.15f, 0.24f), ("LegFR", 0.15f, 0.24f), ("LegBL", -0.15f, -0.24f), ("LegBR", 0.15f, -0.24f) })
        {
            var leg = new Node3D { Name = name, Position = new Vector3(x, 0.24f, z) };
            model.AddChild(leg);
            Block(leg, new Vector3(0.11f, 0.24f, 0.11f), new Vector3(0, -0.12f, 0), dark);
        }
        root.AddChild(Animations(FourLegs));
        return root;
    }

    private static Node3D Wolf(float size, Color fur, bool mask = false, bool ruff = false, bool scar = false)
    {
        var root = new Node3D { Name = "Wolf" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(size, size, size) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        Color dark = fur.Darkened(0.4f), light = fur.Lightened(0.45f);
        Block(body, new Vector3(0.30f, 0.28f, 0.70f), new Vector3(0, 0.44f, 0), fur);
        Block(body, new Vector3(0.24f, 0.18f, 0.30f), new Vector3(0, 0.40f, 0.12f), light);
        Block(body, new Vector3(0.26f, 0.26f, 0.26f), new Vector3(0, 0.58f, 0.42f), fur);
        Block(body, new Vector3(0.14f, 0.12f, 0.20f), new Vector3(0, 0.52f, 0.62f), mask ? dark : light);
        Block(body, new Vector3(0.06f, 0.05f, 0.04f), new Vector3(0, 0.56f, 0.73f), Colors.Black);
        Block(body, new Vector3(0.04f, 0.04f, 0.02f), new Vector3(-0.07f, 0.64f, 0.555f), new Color("f2c94c"));
        Block(body, new Vector3(0.04f, 0.04f, 0.02f), new Vector3(0.07f, 0.64f, 0.555f), new Color("f2c94c"));
        Block(body, new Vector3(0.07f, 0.13f, 0.05f), new Vector3(-0.08f, 0.76f, 0.38f), dark);
        Block(body, new Vector3(0.07f, 0.13f, 0.05f), new Vector3(0.08f, 0.76f, 0.38f), dark);
        var tail = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.08f, 0.08f, 0.32f) }, Position = new Vector3(0, 0.56f, -0.46f), RotationDegrees = new Vector3(-35, 0, 0), MaterialOverride = new StandardMaterial3D { AlbedoColor = dark, Roughness = 0.9f } };
        body.AddChild(tail);
        if (ruff)
            Block(body, new Vector3(0.36f, 0.32f, 0.16f), new Vector3(0, 0.52f, 0.26f), light.Lerp(fur, 0.5f));
        if (scar)
            Block(body, new Vector3(0.02f, 0.14f, 0.02f), new Vector3(0.10f, 0.62f, 0.556f), new Color("c0392b"));
        foreach ((string name, float x, float z) in new[] { ("LegFL", -0.10f, 0.24f), ("LegFR", 0.10f, 0.24f), ("LegBL", -0.10f, -0.24f), ("LegBR", 0.10f, -0.24f) })
        {
            var leg = new Node3D { Name = name, Position = new Vector3(x, 0.32f, z) };
            model.AddChild(leg);
            Block(leg, new Vector3(0.08f, 0.32f, 0.08f), new Vector3(0, -0.16f, 0), dark);
        }
        root.AddChild(Animations(FourLegs));
        return root;
    }

    private static Node3D Mushroom(float size, Color cap, bool spots = false, bool fists = false, bool puff = false, bool morel = false, bool crown = false)
    {
        var root = new Node3D { Name = "Mushroom" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(size, size, size) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        Color stem = new("efe6d2"), white = new("fbf8f0");
        Block(body, new Vector3(0.26f, 0.38f, 0.24f), new Vector3(0, 0.36f, 0), stem);
        Block(body, new Vector3(0.05f, 0.06f, 0.02f), new Vector3(-0.06f, 0.44f, 0.125f), Colors.Black);
        Block(body, new Vector3(0.05f, 0.06f, 0.02f), new Vector3(0.06f, 0.44f, 0.125f), Colors.Black);
        if (puff)
            Block(body, new Vector3(0.46f, 0.36f, 0.46f), new Vector3(0, 0.70f, 0), cap);
        else if (morel)
        {
            Block(body, new Vector3(0.34f, 0.30f, 0.34f), new Vector3(0, 0.68f, 0), cap);
            Block(body, new Vector3(0.22f, 0.20f, 0.22f), new Vector3(0, 0.90f, 0), cap.Darkened(0.2f));
        }
        else
        {
            Block(body, new Vector3(0.62f, 0.16f, 0.62f), new Vector3(0, 0.62f, 0), cap);
            Block(body, new Vector3(0.44f, 0.10f, 0.44f), new Vector3(0, 0.74f, 0), cap);
        }
        if (spots)
        {
            foreach ((float x, float z) in new[] { (-0.18f, 0.12f), (0.16f, -0.14f), (0.05f, 0.2f), (-0.1f, -0.18f) })
                Block(body, new Vector3(0.08f, 0.03f, 0.08f), new Vector3(x, 0.80f, z), white);
        }
        if (fists)
        {
            Block(body, new Vector3(0.10f, 0.10f, 0.10f), new Vector3(-0.2f, 0.34f, 0.06f), stem);
            Block(body, new Vector3(0.10f, 0.10f, 0.10f), new Vector3(0.2f, 0.34f, 0.06f), stem);
        }
        if (crown)
            Crown(body, 0.86f);
        Legs(model, TwoLegs, 0.07f, 0.16f, stem.Darkened(0.25f));
        root.AddChild(Animations(TwoLegs));
        return root;
    }

    private static Node3D Zombie(float size, Color skin, Color rags, bool bloated = false, bool drool = false, bool stitches = false, bool crown = false)
    {
        var root = new Node3D { Name = "Zombie" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(size, size, size) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        float w = bloated ? 0.44f : 0.30f;
        Block(body, new Vector3(w, 0.34f, bloated ? 0.34f : 0.20f), new Vector3(0, 0.56f, 0), rags);
        Block(body, new Vector3(0.26f, 0.26f, 0.24f), new Vector3(0, 0.86f, 0.02f), skin);
        Block(body, new Vector3(0.05f, 0.04f, 0.02f), new Vector3(-0.06f, 0.88f, 0.145f), new Color("e74c3c"));
        Block(body, new Vector3(0.05f, 0.04f, 0.02f), new Vector3(0.06f, 0.88f, 0.145f), new Color("e74c3c"));
        // Arms held out in front, as zombies do.
        Block(body, new Vector3(0.08f, 0.08f, 0.30f), new Vector3(-(w / 2 + 0.04f), 0.66f, 0.16f), skin);
        Block(body, new Vector3(0.08f, 0.08f, 0.30f), new Vector3(w / 2 + 0.04f, 0.66f, 0.16f), skin);
        if (drool)
            Block(body, new Vector3(0.04f, 0.10f, 0.02f), new Vector3(0.03f, 0.74f, 0.145f), new Color("c5e17a"));
        if (stitches)
        {
            foreach (float y in new[] { 0.48f, 0.56f, 0.64f })
                Block(body, new Vector3(0.18f, 0.02f, 0.01f), new Vector3(0, y, 0.105f), new Color("3d2b1f"));
        }
        if (crown)
            Crown(body, 1.02f);
        Legs(model, TwoLegs, 0.09f, 0.39f, rags.Darkened(0.3f));
        root.AddChild(Animations(TwoLegs));
        return root;
    }

    private static void Crown(Node3D body, float y)
    {
        var gold = new Color("e8c547");
        Block(body, new Vector3(0.28f, 0.06f, 0.28f), new Vector3(0, y, 0), gold);
        foreach ((float x, float z) in new[] { (-0.11f, -0.11f), (0.11f, -0.11f), (-0.11f, 0.11f), (0.11f, 0.11f) })
            Block(body, new Vector3(0.05f, 0.09f, 0.05f), new Vector3(x, y + 0.07f, z), gold);
    }

    /// <summary>Two legs hung from the hips, side by side, <paramref name="height"/> long.</summary>
    private static void Legs(Node3D model, string[] names, float width, float height, Color colour)
    {
        foreach ((string name, float x) in new[] { (names[0], -0.08f), (names[1], 0.08f) })
        {
            var leg = new Node3D { Name = name, Position = new Vector3(x, height, 0) };
            model.AddChild(leg);
            Block(leg, new Vector3(width, height, width), new Vector3(0, -height / 2, 0), colour);
        }
    }

    private static void Block(Node3D parent, Vector3 size, Vector3 at, Color colour) => parent.AddChild(new MeshInstance3D
    {
        Mesh = new BoxMesh { Size = size },
        Position = at,
        MaterialOverride = new StandardMaterial3D { AlbedoColor = colour, Roughness = 0.9f },
    });

    /// <summary>"idle": the body breathes; "walk": the legs swing in turn and the body bobs.</summary>
    private static AnimationPlayer Animations(string[] legs)
    {
        var idle = new Animation { Length = 1.2f, LoopMode = Animation.LoopModeEnum.Linear };
        int t = idle.AddTrack(Animation.TrackType.Value);
        idle.TrackSetPath(t, "Model/Body:position");
        idle.TrackInsertKey(t, 0, Vector3.Zero);
        idle.TrackInsertKey(t, 0.6, new Vector3(0, 0.015f, 0));
        idle.TrackInsertKey(t, 1.2, Vector3.Zero);

        var walk = new Animation { Length = 0.5f, LoopMode = Animation.LoopModeEnum.Linear };
        // Diagonal pairs on four legs, one after the other on two.
        foreach ((string leg, float sign) in legs.Select(l => (l, l is "LegFL" or "LegBR" or "LegL" ? 1f : -1f)))
        {
            int k = walk.AddTrack(Animation.TrackType.Value);
            walk.TrackSetPath(k, $"Model/{leg}:rotation");
            walk.TrackInsertKey(k, 0, new Vector3(0.5f * sign, 0, 0));
            walk.TrackInsertKey(k, 0.25, new Vector3(-0.5f * sign, 0, 0));
            walk.TrackInsertKey(k, 0.5, new Vector3(0.5f * sign, 0, 0));
        }
        int b = walk.AddTrack(Animation.TrackType.Value);
        walk.TrackSetPath(b, "Model/Body:position");
        walk.TrackInsertKey(b, 0, Vector3.Zero);
        walk.TrackInsertKey(b, 0.125, new Vector3(0, 0.03f, 0));
        walk.TrackInsertKey(b, 0.25, Vector3.Zero);
        walk.TrackInsertKey(b, 0.375, new Vector3(0, 0.03f, 0));
        walk.TrackInsertKey(b, 0.5, Vector3.Zero);

        var library = new AnimationLibrary();
        library.AddAnimation("idle", idle);
        library.AddAnimation("walk", walk);
        var player = new AnimationPlayer { Name = "AnimationPlayer" };
        player.AddAnimationLibrary("", library);
        return player;
    }
}
