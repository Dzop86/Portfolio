using Godot;
using Rpg.Core;

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

    /// <summary>The hair colours a player may choose (index 1 to 7; 0 keeps the look's own): palette cells.</summary>
    public static readonly Vector2[] HairCells =
        [new(-1, -1), new(0, 3), new(6, 3), new(3, 2), new(2, 2), new(4, 3), new(4, 2), new(5, 2)];

    /// <summary>The skin tones (index 1 to 4; 0 keeps the look's own): palette cells, light to dark.</summary>
    public static readonly Vector2[] SkinCells = [new(-1, -1), new(0, 2), new(7, 3), new(5, 3), new(6, 3)];

    private static readonly Dictionary<string, (Vector2 Hair, Vector2 Skin)> Cells = [];
    private static Image? _palette;

    /// <summary>The colour in the middle of a palette cell, for the swatches.</summary>
    public static Color CellColour(Vector2 cell)
    {
        _palette ??= GD.Load<Texture2D>(Root + "characters/Textures/colormap.png").GetImage();
        return _palette.GetPixel((int)((cell.X + 0.5f) * _palette.GetWidth() / 8), (int)((cell.Y + 0.5f) * _palette.GetHeight() / 4));
    }

    /// <summary>
    /// The hair and skin cells of a look, read from its head's texture coordinates: the skin is the
    /// face (the light-to-dark cells 5 to 7 of the last row, about 220 vertices on every look), the
    /// hair the head's largest other group.
    /// </summary>
    public static (Vector2 Hair, Vector2 Skin) CellsOf(string look)
    {
        if (Cells.TryGetValue(look, out var known))
            return known;
        var counts = new Dictionary<Vector2, int>();
        Node3D model = Fighter(look).Instantiate<Node3D>();
        foreach (MeshInstance3D mesh in model.FindChildren("*head*", nameof(MeshInstance3D), true, false).OfType<MeshInstance3D>())
        {
            for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                foreach (Vector2 uv in mesh.Mesh.SurfaceGetArrays(i)[(int)Mesh.ArrayType.TexUV].AsVector2Array())
                {
                    var cell = new Vector2(Mathf.Floor(uv.X * 8), Mathf.Floor(uv.Y * 4));
                    counts[cell] = counts.GetValueOrDefault(cell) + 1;
                }
            }
        }
        model.Free();
        Vector2 skin = counts.Keys.Where(c => c.Y == 3 && c.X >= 5).OrderBy(c => Math.Abs(counts[c] - 220)).DefaultIfEmpty(new Vector2(-1, -1)).First();
        Vector2 hair = counts.Where(c => c.Key != skin).OrderByDescending(c => c.Value).Select(c => c.Key).DefaultIfEmpty(new Vector2(-1, -1)).First();
        Cells[look] = (hair, skin);
        return (hair, skin);
    }

    /// <summary>Turns the outfit colours of a model by <paramref name="colour"/> steps (0 leaves it as drawn).</summary>
    public static void Paint(Node3D model, int colour) => Paint(model, new Hero("", "", null, colour));

    /// <summary>
    /// Dresses a character model as <paramref name="hero"/> says: outfit colour, hair colour, skin
    /// tone (see shaders/outfit.gdshader), height and build. Monsters are left alone.
    /// </summary>
    public static void Paint(Node3D model, Hero hero)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(hero);
        (Vector2 hair, Vector2 skin) = hero.Look.Length > 0 && (hero.Hair > 0 || hero.Skin > 0) ? CellsOf(hero.Look) : (new Vector2(-1, -1), new Vector2(-1, -1));
        foreach (MeshInstance3D mesh in model.FindChildren("*", nameof(MeshInstance3D), true, false).OfType<MeshInstance3D>())
        {
            bool head = mesh.Name.ToString().Contains("head", StringComparison.Ordinal);
            for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                if (mesh.Mesh.SurfaceGetMaterial(i) is not BaseMaterial3D { AlbedoTexture: Texture2D texture })
                    continue;
                var material = new ShaderMaterial { Shader = Outfit };
                material.SetShaderParameter("albedo_texture", texture);
                material.SetShaderParameter("shift", hero.Colour);
                if (head && hero.Hair > 0)
                {
                    material.SetShaderParameter("hair_from", hair);
                    material.SetShaderParameter("hair_to", HairCells[hero.Hair]);
                }
                if (hero.Skin > 0)
                {
                    material.SetShaderParameter("skin_from", skin);
                    material.SetShaderParameter("skin_to", SkinCells[hero.Skin]);
                }
                mesh.SetSurfaceOverrideMaterial(i, material);
            }
        }
        // Height stretches, build widens: 7 and 6 % a step, on the model inside its node.
        model.Scale = new Vector3(1 + 0.06f * hero.Build, 1 + 0.07f * hero.Height, 1 + 0.06f * hero.Build);
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
        // Weighted by the area of the triangles, not their number: what the eye sees most.
        var area = new double[8];
        Node3D model = Fighter(look).Instantiate<Node3D>();
        foreach (MeshInstance3D mesh in model.FindChildren("*", nameof(MeshInstance3D), true, false).OfType<MeshInstance3D>())
        {
            for (int s = 0; s < mesh.Mesh.GetSurfaceCount(); s++)
            {
                Godot.Collections.Array arrays = mesh.Mesh.SurfaceGetArrays(s);
                Vector3[] points = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                Vector2[] uvs = arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();
                int[] index = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                for (int t = 0; t + 2 < index.Length; t += 3)
                {
                    Vector2 uv = uvs[index[t]];
                    if (uv.Y < 0.5f || uv.Y >= 0.75f || uv.X < 0.125f)
                        continue;
                    Vector3 a = points[index[t]], b = points[index[t + 1]], c = points[index[t + 2]];
                    area[Math.Min(7, (int)(uv.X * 8))] += (b - a).Cross(c - a).Length() / 2;
                }
            }
        }
        model.Free();
        int main = Enumerable.Range(1, 7).MaxBy(c => area[c]);
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
