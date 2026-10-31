using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The colours of the fight's numbers and effects: one per element, so that a glance tells which
/// element hit; healing and shields have theirs. Hexadecimal, read by the Godot client.
/// </summary>
public static class ElementStyle
{
    // Vitality and healing in pink, the elements in the colours Charles chose (D62): Earth brown, Fire red,
    // Water blue, Air green; light enough to read on the dark interface.
    public const string Heal = "#ff7eb6";
    public const string Shield = "#a9c7e8";

    public static string Colour(Element element) => element switch
    {
        Element.Earth => "#b07a45",
        Element.Fire => "#ef4b4b",
        Element.Water => "#3d9cff",
        Element.Air => "#52c45a",
        // The compounds (sprint 138): Lightning in yellow, between the Fire's red and the Air's green.
        Element.Lightning => "#f2d03b",
        _ => "#e0e0e0",
    };

    /// <summary>
    /// The talent tree's colours (sprint 149, D82, L4 a), each also a token of <c>src/assets/tokens.css</c> (<c>--osmose-*</c>):
    /// the five states of a node, the links, the couples' brace, the window's ground, and a colour a style.
    /// </summary>
    public const string Taken = "#bef374", TakenFill = "#33421f", Open = "#e0e0e0", OpenFill = "#262626", Excluded = "#e07060",
        ExcludedFill = "#2a1f1f", Locked = "#8a8a8a", LockedFill = "#202020", OutOfLevel = "#5c5c5c", OutOfLevelFill = "#1d1d1d",
        Coming = "#6a6a6a", Losing = "#f08070", LosingFill = "#3a2424", Brace = "#5a3a22", Ground = "#1b1b1b", Cost = "#f2d36b";

    /// <summary>The tree's wood (sprint 177, s164-arbre-maquette.md §3.3): its body, its highlight, its edge.</summary>
    public const string Wood = "#7a5434", WoodLight = "#a8794f", WoodDark = "#3a2616";

    public static string StyleColour(Branch style) => style switch
    {
        Branch.Melee => "#e8a33d",
        Branch.Distance => "#6bb8f2",
        Branch.Support => "#ff7eb6",
        Branch.Terrain => "#c9a27f",
        Branch.Tempo => "#b48cff",
        Branch.Alchemy => "#4fd1c5",
        _ => "#bef374",
    };
}
