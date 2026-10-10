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
        _ => "#e0e0e0",
    };
}
