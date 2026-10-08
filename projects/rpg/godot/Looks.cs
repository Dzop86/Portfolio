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
