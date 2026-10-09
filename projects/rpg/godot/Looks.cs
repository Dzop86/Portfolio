using Godot;

namespace Rpg.Desktop;

/// <summary>The 3D models (Kenney, CC0) behind the "look" of the data files and the board's terrain.</summary>
public static class Looks
{
    private const string Root = "res://assets/kenney/";

    /// <summary>"female-a" to "female-f", "male-a" to "male-f" (Mini Characters), "orc" (Mini Dungeon).</summary>
    public static PackedScene Fighter(string look) =>
        GD.Load<PackedScene>(look == "orc" ? Root + "dungeon/character-orc.glb" : $"{Root}characters/character-{look}.glb")
        ?? throw new KeyNotFoundException($"No model for the look '{look}'.");

    private static readonly Shader Outfit = GD.Load<Shader>("res://shaders/outfit.gdshader");

    /// <summary>
    /// Turns the outfit colours of a character model by <paramref name="colour"/> steps (0 leaves it
    /// as drawn; see shaders/outfit.gdshader). Monsters are left alone.
    /// </summary>
    public static void Paint(Node3D model, int colour)
    {
        ArgumentNullException.ThrowIfNull(model);
        foreach (MeshInstance3D mesh in model.FindChildren("*", nameof(MeshInstance3D), true, false).OfType<MeshInstance3D>())
        {
            for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                if (mesh.Mesh.SurfaceGetMaterial(i) is not BaseMaterial3D { AlbedoTexture: Texture2D texture })
                    continue;
                var material = new ShaderMaterial { Shader = Outfit };
                material.SetShaderParameter("albedo_texture", texture);
                material.SetShaderParameter("shift", colour);
                mesh.SetSurfaceOverrideMaterial(i, material);
            }
        }
    }

    private static readonly Dictionary<string, int> MainColumns = [];

    /// <summary>
    /// The coloured palette column (1 green to 7 purple) most of a look's outfit uses, read from the
    /// texture coordinates of its meshes: shifted by k, the outfit takes column 1 + (main - 1 + k) mod 7.
    /// </summary>
    public static int MainColumn(string look)
    {
        if (MainColumns.TryGetValue(look, out int known))
            return known;
        var counts = new int[8];
        Node3D model = Fighter(look).Instantiate<Node3D>();
        foreach (MeshInstance3D mesh in model.FindChildren("*", nameof(MeshInstance3D), true, false).OfType<MeshInstance3D>())
        {
            for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                foreach (Vector2 uv in mesh.Mesh.SurfaceGetArrays(i)[(int)Mesh.ArrayType.TexUV].AsVector2Array())
                {
                    if (uv.Y >= 0.5f && uv.Y < 0.75f && uv.X >= 0.125f)
                        counts[Math.Min(7, (int)(uv.X * 8))]++;
                }
            }
        }
        model.Free();
        int main = Enumerable.Range(1, 7).MaxBy(c => counts[c]);
        MainColumns[look] = main;
        return main;
    }

    /// <summary>The shift a model was painted with (for the self-test), or null if it was not.</summary>
    public static int? PaintedWith(Node3D model) =>
        model?.FindChildren("*", nameof(MeshInstance3D), true, false).OfType<MeshInstance3D>()
            .Select(m => m.GetSurfaceOverrideMaterial(0) as ShaderMaterial)
            .FirstOrDefault(m => m is not null)?.GetShaderParameter("shift").AsInt32();

    public static PackedScene Nature(string name) => GD.Load<PackedScene>($"{Root}nature/{name}.glb");

    /// <summary>Obstacles alternate between trees and rocks, the same for a given cell every time.</summary>
    public static string Obstacle(int x, int y) => ((x * 31 + y * 17) % 4) switch
    {
        0 => "tree_default",
        1 => "tree_pineDefaultA",
        2 => "rock_largeA",
        _ => "rock_largeB",
    };

    /// <summary>A few flowers and bushes on the floor, for decoration only; null for a bare cell.</summary>
    public static string? Decoration(int x, int y) => ((x * 7 + y * 13) % 9) switch
    {
        0 => "flower_redA",
        3 => "plant_bushSmall",
        5 => "flower_yellowA",
        7 => "mushroom_tan",
        _ => null,
    };
}
