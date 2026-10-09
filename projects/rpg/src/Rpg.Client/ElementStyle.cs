using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The colours of the fight's numbers and effects: one per element, so that a glance tells which
/// element hit; healing and shields have theirs. Hexadecimal, read by the Godot client.
/// </summary>
public static class ElementStyle
{
    public const string Heal = "#7be07b";
    public const string Shield = "#a9c7e8";

    public static string Colour(Element element) => element switch
    {
        Element.Earth => "#d9a55b",
        Element.Fire => "#ff6b3d",
        Element.Water => "#3d9cff",
        Element.Air => "#b98cff",
        _ => "#e0e0e0",
    };
}
