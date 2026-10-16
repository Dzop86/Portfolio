namespace Rpg.Core;

/// <summary>
/// A character's life between fights (sprint 101, D73, docs/suite-apres-d69.md §5 bis): the hit points it ends a
/// fight with are kept, and grow back out of a fight, 1 % of its maximum a second and at least 1 (twice that in the
/// village), never in a fight; computed from the last value set and its time, by the server and the game alike.
/// A fight taken and never reported counts as lost after <see cref="Abandoned"/>.
/// </summary>
public static class Life
{
    /// <summary>A fight's ticket not reported after this long: lost, the character at 1 hit point.</summary>
    public static readonly TimeSpan Abandoned = TimeSpan.FromMinutes(30);

    /// <summary>Below this share of its maximum, attacking asks first.</summary>
    public const double Warn = 0.3;

    /// <summary>The hit points a second: one hundredth of the maximum, one at least.</summary>
    public static double PerSecond(int maxHp) => Math.Max(1, maxHp / 100.0);

    /// <summary>The village heals twice as fast.</summary>
    public static int Factor(string? map) => Shop.InVillage(map) ? 2 : 1;

    /// <summary>A hero's maximum hit points out of a fight: its class's at its level and its Vitality, what it wears included.</summary>
    public static int MaxHp(Hero hero, GameData data) =>
        Fight.HeroTotals(hero ?? throw new ArgumentNullException(nameof(hero)), data) is HeroNumbers n ? n.Hp + n.Stats.Vitality : 1;

    /// <summary>
    /// The hit points now: <paramref name="hp"/> set at <paramref name="at"/> (null: all of them), grown back since,
    /// at the rate of <paramref name="map"/>; frozen in a fight, then 1 if the fight was abandoned.
    /// </summary>
    public static int Now(int maxHp, int? hp, DateTimeOffset? at, bool inFight, string? map, DateTimeOffset now)
    {
        if (at is not DateTimeOffset from || (hp is null && !inFight))
            return maxHp;
        int start = Math.Clamp(hp ?? maxHp, 1, maxHp);
        if (inFight)
        {
            if (now - from < Abandoned)
                return start;
            (start, from) = (1, from + Abandoned);
        }
        double grown = Math.Max(0, (now - from).TotalSeconds) * PerSecond(maxHp) * Factor(map);
        return (int)Math.Min(maxHp, start + Math.Floor(grown));
    }

    /// <summary>Whether attacking at these hit points asks first.</summary>
    public static bool Low(int hp, int maxHp) => hp < maxHp * Warn;
}
