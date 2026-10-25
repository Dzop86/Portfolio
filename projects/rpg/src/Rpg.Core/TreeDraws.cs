namespace Rpg.Core;

/// <summary>
/// Builds drawn at random for the tree's measure out of CI (sprint 151, D82, J5 b, J6 a): from a door, node after node among
/// those the rules allow and the points pay for; and the talents too few of them take, false decisions to look at.
/// </summary>
public static class TreeDraws
{
    /// <summary>
    /// A build drawn from <paramref name="door"/> at <paramref name="level"/> the way a player builds one: a branch aimed at,
    /// drawn among those the door opens (a style, the braid of its pair), climbed node after node while it has one to take,
    /// then any node the rules allow and the points pay for, until none is left.
    /// </summary>
    public static IReadOnlyList<string> Random(string door, int level, Random rng, GameData data)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(data);
        var taken = new List<string> { door };
        string[] Open() => [.. data.Talents.Keys.Order(StringComparer.Ordinal).Where(id => !taken.Contains(id) && TalentReasons.WhyNot(taken, id, level, data) is null)];
        Branch[] aims = [.. Open().Select(id => data.Talents[id].Branch).Where(b => b is not Branch.Fork and not Branch.Pure).Distinct().Order()];
        Branch? aim = aims.Length == 0 ? null : aims[rng.Next(aims.Length)];
        while (true)
        {
            string[] open = Open();
            if (open.Length == 0)
                return taken;
            string[] aimed = [.. open.Where(id => data.Talents[id].Branch == aim)];
            string[] from = aimed.Length > 0 ? aimed : open;
            taken = Talents.With(taken, from[rng.Next(from.Length)], data);
        }
    }

    /// <summary>
    /// The talents, each half of a choice included (the Transmutations, the Hearts), that fewer than
    /// <paramref name="percent"/> % of the builds take, with their share.
    /// </summary>
    public static IReadOnlyList<(string Talent, double Share)> Rare(IReadOnlyList<IReadOnlyList<string>> builds, GameData data, double percent = 5)
    {
        ArgumentNullException.ThrowIfNull(builds);
        ArgumentNullException.ThrowIfNull(data);
        return [.. data.Talents.Keys.Order(StringComparer.Ordinal)
            .Select(id => (id, 100.0 * builds.Count(b => b.Contains(id)) / Math.Max(1, builds.Count)))
            .Where(x => x.Item2 < percent)];
    }
}
