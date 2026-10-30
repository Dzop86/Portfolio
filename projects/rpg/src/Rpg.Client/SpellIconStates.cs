namespace Rpg.Client;

/// <summary>The ring around a spell's tile: none, the pointer over it, or the spell chosen.</summary>
public enum SpellRing
{
    None,
    Hover,
    Selected,
}

/// <summary>
/// How a spell's tile is drawn: which of its three drawings (<c>""</c>, <c>"-locked"</c>, <c>"-small"</c>), its ring, and
/// how many pixels it moves down (up when negative).
/// </summary>
public readonly record struct SpellLook(string Variant, SpellRing Ring, int Lift);

/// <summary>
/// The states of a spell's tile (sprint 163, <c>docs/graphisme/s163-icones-arbre.md</c> §1.6), the same in the book, the
/// deck and the combat bar: flat when it is not unlocked or not castable now (one convention: flat = not now), without its
/// marks under 32 pixels, ringed in chocolate under the pointer and in pistachio when chosen, raised by a pixel under the
/// pointer and pressed down by one.
/// </summary>
public static class SpellIconStates
{
    /// <summary>Below this size (pixels), the tile drops its marks (a twin's dot, a Seal's accent).</summary>
    public const int SmallBelow = 32;

    /// <summary>The ring's colours: the hover's light chocolate, the chosen one's pistachio (and its halo at 35 %).</summary>
    public const string HoverRing = UiStyle.ChocolateLight, SelectedRing = UiStyle.Pistachio;

    public static SpellLook Look(bool faint, float size, bool hovered = false, bool selected = false, bool pressed = false) => new(
        faint ? "-locked" : size < SmallBelow ? "-small" : "",
        selected ? SpellRing.Selected : hovered ? SpellRing.Hover : SpellRing.None,
        pressed ? 1 : hovered ? -1 : 0);
}
