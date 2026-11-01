using System.Numerics;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The pet that follows (sprint 181, D92, Charles : « je veux que le familier me suive en courant à côté de moi, que je sois
/// sur la monture ou pas »): the pet worn in its slot keeps a spot at the hero's side, a little behind, on the ground plane
/// (x, z in cells); it runs there, faster than its master when it lags, and appears there at once when too far (a map
/// changed, a waystone). It is drawn only: no cell is taken, nothing blocks it. In a fight it keeps its characteristics only.
/// </summary>
public static class Pets
{
    /// <summary>How far to the side of the hero the pet keeps, and how far behind, in cells.</summary>
    public const float Side = 1.0f, Behind = 0.35f;

    /// <summary>Close enough to its spot: it stands still.</summary>
    public const float Settled = 0.1f;

    /// <summary>Farther than this, it does not run: it appears at its spot.</summary>
    public const float Far = 6f;

    /// <summary>Lagging by more than this, it runs half as fast again as its master; otherwise a little faster.</summary>
    public const float Lagging = 1.5f;

    /// <summary>The pet the hero wears, or null.</summary>
    public static string? Worn(Hero? hero) =>
        hero?.Worn is { } worn && worn.TryGetValue(Slot.Pet, out string? id) && !string.IsNullOrEmpty(id) ? id : null;

    /// <summary>
    /// The pet's spot by a hero standing at <paramref name="hero"/> and facing <paramref name="facing"/> (radians about
    /// the vertical, 0 along +z as the models look): at its right hand, a little behind.
    /// </summary>
    public static Vector2 Spot(Vector2 hero, float facing)
    {
        var forward = new Vector2(MathF.Sin(facing), MathF.Cos(facing));
        var right = new Vector2(-forward.Y, forward.X);
        return hero + Side * right - Behind * forward;
    }

    /// <summary>
    /// Where the pet is after <paramref name="seconds"/>, its master going at <paramref name="speed"/> cells a second (or
    /// standing), and whether it is running.
    /// </summary>
    public static (Vector2 At, bool Running) Follow(Vector2 pet, Vector2 hero, float facing, float speed, float seconds)
    {
        Vector2 spot = Spot(hero, facing);
        float away = Vector2.Distance(pet, spot);
        if (away > Far || float.IsNaN(away))
            return (spot, false);
        if (away <= Settled)
            return (pet, false);
        float pace = MathF.Max(speed, 2f) * (away > Lagging ? 1.5f : 1.15f) * MathF.Max(seconds, 0);
        return (pace >= away ? spot : pet + (spot - pet) / away * pace, true);
    }
}
