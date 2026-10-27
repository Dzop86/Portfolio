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
    /// then any node the rules allow and the points pay for, until none is left. Without <paramref name="braid"/> (sprint 152,
    /// D83), no node of a braid: a pair's reaction measured apart from its braid.
    /// </summary>
    public static IReadOnlyList<string> Random(string door, int level, Random rng, GameData data, bool braid = true)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(data);
        var taken = new List<string> { door };
        string[] Open() => [.. data.Talents.Keys.Order(StringComparer.Ordinal).Where(id => !taken.Contains(id) && (braid || data.Talents[id].Branch != Branch.Braids)
            && TalentReasons.WhyNot(taken, id, level, data) is null)];
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
    /// The talents that fewer than <paramref name="percent"/> % of the builds aiming at their branch take, with their share
    /// (sprint 152, D83, the game designer's 20 % a style): a node counted among the builds that hold a node of its branch, not
    /// among them all, where a key or a braid stays rare by the draw's arithmetic alone. The doors (the pairs and the Hearts,
    /// which the measure imposes) are left out.
    /// </summary>
    public static IReadOnlyList<(string Talent, double Share)> Rare(IReadOnlyList<IReadOnlyList<string>> builds, GameData data, double percent = 20)
    {
        ArgumentNullException.ThrowIfNull(builds);
        ArgumentNullException.ThrowIfNull(data);
        var rare = new List<(string, double)>();
        foreach (string id in data.Talents.Keys.Order(StringComparer.Ordinal))
        {
            Branch branch = data.Talents[id].Branch;
            if (branch is Branch.Fork or Branch.Pure)
                continue;
            IReadOnlyList<string>[] aiming = [.. builds.Where(b => b.Any(t => data.Talents.TryGetValue(t, out TalentSpec? x) && x.Branch == branch))];
            double share = 100.0 * aiming.Count(b => b.Contains(id)) / Math.Max(1, aiming.Length);
            if (share < percent)
                rare.Add((id, share));
        }
        return rare;
    }
}
