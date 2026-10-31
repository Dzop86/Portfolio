using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// Riding (sprint 179, Charles : « quand j'équipe une monture, il faudrait que visuellement je sois sur la monture et
/// que ça augmente ma vitesse de déplacement de 30 % »): the mount worn in its slot carries the hero, seen on its
/// back, and walks it out of a fight 30 % faster. In a fight the mount keeps its characteristics only.
/// </summary>
public static class Mounts
{
    /// <summary>How much faster a hero on a mount goes out of a fight.</summary>
    public const double SpeedBonus = 0.30;

    /// <summary>The mount the hero wears, or null.</summary>
    public static string? Ridden(Hero? hero) =>
        hero?.Worn is { } worn && worn.TryGetValue(Slot.Mount, out string? id) && !string.IsNullOrEmpty(id) ? id : null;

    /// <summary>How fast the hero goes out of a fight, in cells a second, from the speed on foot.</summary>
    public static double Speed(Hero? hero, double walking) => Ridden(hero) is null ? walking : walking * (1 + SpeedBonus);
}
