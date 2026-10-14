using Godot;

namespace Rpg.Desktop;

/// <summary>
/// The monsters' models (sprint 72), built by the game from blocks in the spirit of Kenney's blocky
/// characters, since no free pack has every kind and role (D63): a boar's body, head, snout, tusks,
/// ears and four legs, its size, colours and details telling its role (a crested charger, a striped
/// piglet, a big grey loner, a tuskless sow, a crowned king). Each has an "idle" and a "walk"
/// animation, as Kenney's models do, and looks along +z.
/// </summary>
public static class Creatures
{
    /// <summary>Whether a look is one of these models ("boar-melee"...).</summary>
    public static bool Knows(string look) => look.StartsWith("boar-", StringComparison.Ordinal);

    public static Node3D Build(string look) => look switch
    {
        "boar-melee" => Boar(1.0f, new Color("6b4a2f"), crest: true),
        "boar-ranged" => Boar(0.72f, new Color("a57a4f"), stripes: true, tusks: false),
        "boar-tank" => Boar(1.25f, new Color("4d4741"), bigTusks: true),
        "boar-support" => Boar(1.0f, new Color("8f5f3f"), tusks: false),
        "boar-boss" => Boar(1.45f, new Color("3b2a1e"), crest: true, bigTusks: true, crown: true),
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
        root.AddChild(Animations());
        return root;
    }

    private static void Block(Node3D parent, Vector3 size, Vector3 at, Color colour) => parent.AddChild(new MeshInstance3D
    {
        Mesh = new BoxMesh { Size = size },
        Position = at,
        MaterialOverride = new StandardMaterial3D { AlbedoColor = colour, Roughness = 0.9f },
    });

    /// <summary>"idle": the body breathes; "walk": the legs swing in pairs and the body bobs.</summary>
    private static AnimationPlayer Animations()
    {
        var idle = new Animation { Length = 1.2f, LoopMode = Animation.LoopModeEnum.Linear };
        int t = idle.AddTrack(Animation.TrackType.Value);
        idle.TrackSetPath(t, "Model/Body:position");
        idle.TrackInsertKey(t, 0, Vector3.Zero);
        idle.TrackInsertKey(t, 0.6, new Vector3(0, 0.015f, 0));
        idle.TrackInsertKey(t, 1.2, Vector3.Zero);

        var walk = new Animation { Length = 0.5f, LoopMode = Animation.LoopModeEnum.Linear };
        foreach ((string leg, float sign) in new[] { ("LegFL", 1f), ("LegBR", 1f), ("LegFR", -1f), ("LegBL", -1f) })
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
