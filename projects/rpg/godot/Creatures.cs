using Godot;

namespace Rpg.Desktop;

/// <summary>
/// The monsters' models (sprint 72), built by the game from blocks in the spirit of Kenney's blocky
/// characters, since no free pack has every kind and role (D63): a boar's body, head, snout, tusks,
/// ears and four legs, its size, colours and details telling its role (a crested charger, a striped
/// piglet, a big grey loner, a tuskless sow, a crowned king); a wolf's lean body, long muzzle, pointed
/// ears and raised tail (sprint 73: a grey wolf, a pale howler with a dark mask, a great male with a
/// ruff, a fawn she-wolf, a black leader with a scar); a mushroom's stem and cap on two feet, a zombie's
/// green body in rags, arms held out (sprint 74); a frog's squat body and bulging eyes, a crab's wide
/// shell, eye stalks and two claws (sprint 75); a bee's striped body, wings and sting, hovering on
/// thin legs, a mole's velvet body, pink snout and digging claws (sprint 76). Each has an "idle" and a "walk"
/// animation, as Kenney's models do, and looks along +z. Since sprint 104, the village's beasts too: a hen,
/// a horse and a sheep (<see cref="Beast"/>).
/// </summary>
public static class Creatures
{
    /// <summary>Whether a look is one of these models ("boar-melee", "wolf-boss"...).</summary>
    public static bool Knows(string look) => Kinds.Any(k => look.StartsWith(k + "-", StringComparison.Ordinal));

    private static readonly string[] Kinds = ["boar", "wolf", "mushroom", "zombie", "frog", "crab", "bee", "mole"];
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
        "frog-melee" => Frog(1.0f, new Color("4caf50")),
        "frog-ranged" => Frog(0.85f, new Color("8bc34a"), stripes: true),
        "frog-tank" => Frog(1.35f, new Color("6d7f3a"), warts: true),
        "frog-support" => Frog(0.8f, new Color("2ecc71"), pale: true),
        "frog-boss" => Frog(1.6f, new Color("556b2f"), warts: true, crown: true),
        "crab-melee" => Crab(1.0f, new Color("d35400")),
        "crab-ranged" => Crab(0.85f, new Color("e67e22"), bubbles: true),
        "crab-tank" => Crab(1.3f, new Color("7e3b22"), spikes: true),
        "crab-support" => Crab(0.9f, new Color("c0392b"), hermitShell: true),
        "crab-boss" => Crab(1.6f, new Color("8e2a1a"), spikes: true, crown: true),
        "bee-melee" => Bee(1.0f, new Color("f1c40f")),
        "bee-ranged" => Bee(0.85f, new Color("f7dc6f"), longSting: true),
        "bee-tank" => Bee(1.3f, new Color("d4ac0d"), fat: true),
        "bee-support" => Bee(0.9f, new Color("fad7a0")),
        "bee-boss" => Bee(1.6f, new Color("e67e22"), fat: true, longSting: true, crown: true),
        "mole-melee" => Mole(1.0f, new Color("4a4a52")),
        "mole-ranged" => Mole(0.9f, new Color("6b5b4b"), clod: true),
        "mole-tank" => Mole(1.3f, new Color("3a3a40"), helmet: true),
        "mole-support" => Mole(0.95f, new Color("7a6a62"), apron: true),
        "mole-boss" => Mole(1.6f, new Color("2b2b30"), helmet: true, crown: true),
        _ => throw new KeyNotFoundException($"No creature for the look '{look}'."),
    };

    /// <summary>A beast of the village (sprint 104): "hen", "horse" or "sheep", small beside the people, as they are.</summary>
    public static Node3D Beast(string kind) => kind switch
    {
        "hen" => Hen(),
        "horse" => Horse(),
        "sheep" => Sheep(),
        _ => throw new KeyNotFoundException($"No beast '{kind}'."),
    };

    private static Node3D Hen()
    {
        var root = new Node3D { Name = "Hen" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(0.8f, 0.8f, 0.8f) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        Color feathers = new("f3eee3"), comb = new("d63a2f"), beak = new("f0a830");
        Block(body, new Vector3(0.30f, 0.26f, 0.38f), new Vector3(0, 0.36f, 0), feathers);
        Block(body, new Vector3(0.18f, 0.20f, 0.18f), new Vector3(0, 0.56f, 0.16f), feathers);
        Block(body, new Vector3(0.05f, 0.08f, 0.12f), new Vector3(0, 0.70f, 0.16f), comb);
        Block(body, new Vector3(0.06f, 0.05f, 0.08f), new Vector3(0, 0.55f, 0.29f), beak);
        Block(body, new Vector3(0.05f, 0.06f, 0.04f), new Vector3(0, 0.48f, 0.26f), comb);
        Block(body, new Vector3(0.03f, 0.03f, 0.02f), new Vector3(-0.06f, 0.60f, 0.25f), Colors.Black);
        Block(body, new Vector3(0.03f, 0.03f, 0.02f), new Vector3(0.06f, 0.60f, 0.25f), Colors.Black);
        Block(body, new Vector3(0.20f, 0.20f, 0.08f), new Vector3(0, 0.48f, -0.22f), feathers.Darkened(0.08f));
        Legs(model, TwoLegs, 0.04f, 0.23f, beak);
        root.AddChild(Animations(TwoLegs));
        return root;
    }

    private static Node3D Horse()
    {
        var root = new Node3D { Name = "Horse" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(1.25f, 1.25f, 1.25f) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        Color coat = new("8a5a34"), mane = new("3b2a1e"), muzzle = new("d9c3a5");
        Block(body, new Vector3(0.30f, 0.30f, 0.72f), new Vector3(0, 0.62f, 0), coat);
        // The neck raised forward, the long head at its end.
        var neck = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.18f, 0.40f, 0.18f) }, Position = new Vector3(0, 0.86f, 0.36f), RotationDegrees = new Vector3(30, 0, 0), MaterialOverride = new StandardMaterial3D { AlbedoColor = coat, Roughness = 0.9f } };
        body.AddChild(neck);
        Block(body, new Vector3(0.16f, 0.16f, 0.34f), new Vector3(0, 1.04f, 0.56f), coat);
        Block(body, new Vector3(0.14f, 0.10f, 0.08f), new Vector3(0, 0.99f, 0.74f), muzzle);
        Block(body, new Vector3(0.04f, 0.04f, 0.02f), new Vector3(-0.08f, 1.08f, 0.58f), Colors.Black);
        Block(body, new Vector3(0.04f, 0.04f, 0.02f), new Vector3(0.08f, 1.08f, 0.58f), Colors.Black);
        Block(body, new Vector3(0.05f, 0.10f, 0.04f), new Vector3(-0.05f, 1.16f, 0.44f), coat.Darkened(0.2f));
        Block(body, new Vector3(0.05f, 0.10f, 0.04f), new Vector3(0.05f, 1.16f, 0.44f), coat.Darkened(0.2f));
        Block(body, new Vector3(0.06f, 0.30f, 0.10f), new Vector3(0, 1.0f, 0.30f), mane);
        var tail = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.07f, 0.07f, 0.36f) }, Position = new Vector3(0, 0.58f, -0.46f), RotationDegrees = new Vector3(55, 0, 0), MaterialOverride = new StandardMaterial3D { AlbedoColor = mane, Roughness = 0.9f } };
        body.AddChild(tail);
        foreach ((string name, float x, float z) in new[] { ("LegFL", -0.10f, 0.26f), ("LegFR", 0.10f, 0.26f), ("LegBL", -0.10f, -0.26f), ("LegBR", 0.10f, -0.26f) })
        {
            var leg = new Node3D { Name = name, Position = new Vector3(x, 0.48f, z) };
            model.AddChild(leg);
            Block(leg, new Vector3(0.08f, 0.48f, 0.08f), new Vector3(0, -0.24f, 0), coat.Darkened(0.15f));
            Block(leg, new Vector3(0.09f, 0.06f, 0.09f), new Vector3(0, -0.45f, 0), mane);
        }
        root.AddChild(Animations(FourLegs));
        return root;
    }

    private static Node3D Sheep()
    {
        var root = new Node3D { Name = "Sheep" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(0.8f, 0.8f, 0.8f) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        Color wool = new("f4f1ea"), face = new("3a3330");
        Block(body, new Vector3(0.44f, 0.36f, 0.60f), new Vector3(0, 0.46f, 0), wool);
        Block(body, new Vector3(0.36f, 0.08f, 0.50f), new Vector3(0, 0.67f, 0), wool.Darkened(0.05f));
        Block(body, new Vector3(0.20f, 0.22f, 0.20f), new Vector3(0, 0.52f, 0.36f), face);
        Block(body, new Vector3(0.26f, 0.10f, 0.12f), new Vector3(0, 0.66f, 0.32f), wool);
        Block(body, new Vector3(0.10f, 0.05f, 0.06f), new Vector3(-0.15f, 0.56f, 0.34f), face);
        Block(body, new Vector3(0.10f, 0.05f, 0.06f), new Vector3(0.15f, 0.56f, 0.34f), face);
        Block(body, new Vector3(0.04f, 0.04f, 0.02f), new Vector3(-0.05f, 0.56f, 0.465f), Colors.White);
        Block(body, new Vector3(0.04f, 0.04f, 0.02f), new Vector3(0.05f, 0.56f, 0.465f), Colors.White);
        foreach ((string name, float x, float z) in new[] { ("LegFL", -0.13f, 0.20f), ("LegFR", 0.13f, 0.20f), ("LegBL", -0.13f, -0.20f), ("LegBR", 0.13f, -0.20f) })
        {
            var leg = new Node3D { Name = name, Position = new Vector3(x, 0.28f, z) };
            model.AddChild(leg);
            Block(leg, new Vector3(0.07f, 0.28f, 0.07f), new Vector3(0, -0.14f, 0), face);
        }
        root.AddChild(Animations(FourLegs));
        return root;
    }

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

    private static Node3D Frog(float size, Color skin, bool stripes = false, bool warts = false, bool pale = false, bool crown = false)
    {
        var root = new Node3D { Name = "Frog" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(size, size, size) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        Color belly = pale ? new Color("f5f0c8") : skin.Lightened(0.5f), dark = skin.Darkened(0.35f);
        Block(body, new Vector3(0.50f, 0.26f, 0.46f), new Vector3(0, 0.26f, 0), skin);
        Block(body, new Vector3(0.40f, 0.06f, 0.36f), new Vector3(0, 0.14f, 0.02f), belly);
        Block(body, new Vector3(0.36f, 0.04f, 0.02f), new Vector3(0, 0.22f, 0.235f), dark);
        foreach (float x in new[] { -0.13f, 0.13f })
        {
            Block(body, new Vector3(0.14f, 0.12f, 0.12f), new Vector3(x, 0.44f, 0.14f), skin);
            Block(body, new Vector3(0.08f, 0.08f, 0.02f), new Vector3(x, 0.45f, 0.205f), Colors.White);
            Block(body, new Vector3(0.04f, 0.05f, 0.02f), new Vector3(x, 0.45f, 0.215f), Colors.Black);
        }
        if (stripes)
        {
            foreach (float z in new[] { -0.12f, 0.04f })
                Block(body, new Vector3(0.51f, 0.04f, 0.06f), new Vector3(0, 0.36f, z), dark);
        }
        if (warts)
        {
            foreach ((float x, float z) in new[] { (-0.15f, -0.1f), (0.12f, -0.15f), (0.02f, 0.05f), (0.17f, 0.08f) })
                Block(body, new Vector3(0.06f, 0.04f, 0.06f), new Vector3(x, 0.40f, z), dark);
        }
        if (crown)
            Crown(body, 0.54f);
        // Folded back legs, short front ones.
        foreach ((string name, float x, float z, float h) in new[] { ("LegFL", -0.17f, 0.16f, 0.14f), ("LegFR", 0.17f, 0.16f, 0.14f), ("LegBL", -0.24f, -0.14f, 0.16f), ("LegBR", 0.24f, -0.14f, 0.16f) })
        {
            var leg = new Node3D { Name = name, Position = new Vector3(x, h, z) };
            model.AddChild(leg);
            Block(leg, new Vector3(0.10f, h, 0.14f), new Vector3(0, -h / 2, 0), dark);
        }
        root.AddChild(Animations(FourLegs));
        return root;
    }

    private static Node3D Crab(float size, Color shell, bool bubbles = false, bool spikes = false, bool crown = false, bool hermitShell = false)
    {
        var root = new Node3D { Name = "Crab" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(size, size, size) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        Color dark = shell.Darkened(0.35f), light = shell.Lightened(0.35f);
        Block(body, new Vector3(0.58f, 0.18f, 0.42f), new Vector3(0, 0.26f, 0), shell);
        Block(body, new Vector3(0.44f, 0.06f, 0.32f), new Vector3(0, 0.37f, -0.02f), light);
        foreach (float x in new[] { -0.08f, 0.08f })
        {
            Block(body, new Vector3(0.03f, 0.12f, 0.03f), new Vector3(x, 0.40f, 0.18f), dark);
            Block(body, new Vector3(0.06f, 0.06f, 0.06f), new Vector3(x, 0.48f, 0.18f), Colors.Black);
        }
        // Two claws, held up in front.
        foreach (float x in new[] { -0.30f, 0.30f })
        {
            Block(body, new Vector3(0.08f, 0.08f, 0.18f), new Vector3(x, 0.30f, 0.20f), shell);
            Block(body, new Vector3(0.16f, 0.14f, 0.14f), new Vector3(x, 0.36f, 0.34f), dark);
        }
        if (bubbles)
        {
            foreach ((float x, float y) in new[] { (-0.05f, 0.60f), (0.06f, 0.70f), (0f, 0.80f) })
                Block(body, new Vector3(0.06f, 0.06f, 0.06f), new Vector3(x, y, 0.22f), new Color("d6f0ff"));
        }
        if (spikes)
        {
            foreach (float x in new[] { -0.18f, 0f, 0.18f })
                Block(body, new Vector3(0.05f, 0.10f, 0.05f), new Vector3(x, 0.44f, -0.04f), dark);
        }
        if (hermitShell)
            Block(body, new Vector3(0.30f, 0.26f, 0.30f), new Vector3(0, 0.50f, -0.06f), new Color("d9c7a3"));
        if (crown)
            Crown(body, 0.42f);
        foreach ((string name, float x, float z) in new[] { ("LegFL", -0.24f, 0.08f), ("LegFR", 0.24f, 0.08f), ("LegBL", -0.24f, -0.12f), ("LegBR", 0.24f, -0.12f) })
        {
            var leg = new Node3D { Name = name, Position = new Vector3(x, 0.18f, z) };
            model.AddChild(leg);
            Block(leg, new Vector3(0.18f, 0.18f, 0.05f), new Vector3(x < 0 ? -0.06f : 0.06f, -0.09f, 0), dark);
        }
        root.AddChild(Animations(FourLegs));
        return root;
    }

    private static Node3D Bee(float size, Color yellow, bool fat = false, bool longSting = false, bool crown = false)
    {
        var root = new Node3D { Name = "Bee" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(size, size, size) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        Color black = new("1c1c1c"), wing = new(0.92f, 0.96f, 1f);
        float w = fat ? 0.36f : 0.28f;
        // Head, thorax, striped abdomen, raised off the ground: a bee hovers.
        Block(body, new Vector3(0.22f, 0.22f, 0.20f), new Vector3(0, 0.56f, 0.28f), black);
        Block(body, new Vector3(0.24f, 0.24f, 0.20f), new Vector3(0, 0.56f, 0.10f), yellow);
        for (int i = 0; i < 3; i++)
            Block(body, new Vector3(w, w, 0.10f), new Vector3(0, 0.54f, -0.06f - i * 0.10f), i % 2 == 0 ? black : yellow);
        Block(body, new Vector3(0.04f, 0.04f, longSting ? 0.18f : 0.10f), new Vector3(0, 0.52f, longSting ? -0.42f : -0.38f), black);
        foreach (float x in new[] { -0.07f, 0.07f })
        {
            Block(body, new Vector3(0.06f, 0.06f, 0.02f), new Vector3(x, 0.60f, 0.385f), new Color("5dade2"));
            Block(body, new Vector3(0.02f, 0.14f, 0.02f), new Vector3(x, 0.72f, 0.32f), black);
        }
        foreach (float x in new[] { -0.22f, 0.22f })
            body.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.28f, 0.02f, 0.18f) }, Position = new Vector3(x, 0.72f, 0.06f), RotationDegrees = new Vector3(0, 0, x < 0 ? 20 : -20), MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(wing, 0.7f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha } });
        if (crown)
            Crown(body, 0.70f);
        foreach ((string name, float x, float z) in new[] { ("LegFL", -0.08f, 0.14f), ("LegFR", 0.08f, 0.14f), ("LegBL", -0.08f, 0.02f), ("LegBR", 0.08f, 0.02f) })
        {
            var leg = new Node3D { Name = name, Position = new Vector3(x, 0.44f, z) };
            model.AddChild(leg);
            Block(leg, new Vector3(0.03f, 0.22f, 0.03f), new Vector3(0, -0.11f, 0), black);
        }
        root.AddChild(Animations(FourLegs));
        return root;
    }

    private static Node3D Mole(float size, Color fur, bool clod = false, bool helmet = false, bool apron = false, bool crown = false)
    {
        var root = new Node3D { Name = "Mole" };
        var model = new Node3D { Name = "Model", Scale = new Vector3(size, size, size) };
        root.AddChild(model);
        var body = new Node3D { Name = "Body" };
        model.AddChild(body);
        Color pink = new("f1948a"), dark = fur.Darkened(0.3f);
        Block(body, new Vector3(0.40f, 0.30f, 0.52f), new Vector3(0, 0.28f, 0), fur);
        Block(body, new Vector3(0.28f, 0.22f, 0.20f), new Vector3(0, 0.30f, 0.32f), fur);
        Block(body, new Vector3(0.12f, 0.10f, 0.10f), new Vector3(0, 0.28f, 0.46f), pink);
        Block(body, new Vector3(0.03f, 0.03f, 0.02f), new Vector3(-0.07f, 0.37f, 0.42f), Colors.Black);
        Block(body, new Vector3(0.03f, 0.03f, 0.02f), new Vector3(0.07f, 0.37f, 0.42f), Colors.Black);
        // Wide digging hands, pink, turned outwards.
        foreach (float x in new[] { -0.25f, 0.25f })
            Block(body, new Vector3(0.14f, 0.05f, 0.14f), new Vector3(x, 0.18f, 0.26f), pink);
        if (clod)
            Block(body, new Vector3(0.12f, 0.12f, 0.12f), new Vector3(0.25f, 0.27f, 0.26f), new Color("6e4b2a"));
        if (helmet)
            Block(body, new Vector3(0.30f, 0.08f, 0.22f), new Vector3(0, 0.44f, 0.32f), new Color("b7950b"));
        if (apron)
            Block(body, new Vector3(0.30f, 0.20f, 0.02f), new Vector3(0, 0.28f, 0.265f), new Color("ecf0f1"));
        if (crown)
            Crown(body, 0.48f);
        foreach ((string name, float x, float z) in new[] { ("LegFL", -0.13f, 0.16f), ("LegFR", 0.13f, 0.16f), ("LegBL", -0.13f, -0.18f), ("LegBR", 0.13f, -0.18f) })
        {
            var leg = new Node3D { Name = name, Position = new Vector3(x, 0.14f, z) };
            model.AddChild(leg);
            Block(leg, new Vector3(0.09f, 0.14f, 0.09f), new Vector3(0, -0.07f, 0), dark);
        }
        root.AddChild(Animations(FourLegs));
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
