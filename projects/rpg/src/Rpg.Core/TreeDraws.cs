namespace Rpg.Core;

/// <summary>
/// Builds drawn at random for the tree's measure out of CI (sprint 151, D82, J5 b, J6 a): node after node among those the
/// rules allow and the points pay for; and the talents too few of them take, false decisions to look at. Since sprint 182
/// (D93) the tree has no doors: a draw starts empty.
/// </summary>
public static class TreeDraws
{
    /// <summary>
    /// A build drawn at <paramref name="level"/> the way a player builds one: a branch aimed at, drawn among those open,
    /// climbed node after node while it has one to take, then any node the rules allow and the points pay for, until none
    /// is left.
    /// </summary>
    public static IReadOnlyList<string> Random(int level, Random rng, GameData data)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(data);
        var taken = new List<string>();
        string[] Open() => [.. data.Talents.Keys.Order(StringComparer.Ordinal).Where(id => !taken.Contains(id) && TalentReasons.WhyNot(taken, id, level, data) is null)];
        Branch[] aims = [.. Open().Select(id => data.Talents[id].Branch).Distinct().Order()];
        Branch? aim = aims.Length == 0 ? null : aims[rng.Next(aims.Length)];
        while (true)
        {
            string[] open = Open();
            if (open.Length == 0)
                return taken;
            string[] aimed = [.. open.Where(id => data.Talents[id].Branch == aim)];
            string[] from = aimed.Length > 0 ? aimed : open;
            taken.Add(from[rng.Next(from.Length)]);
        }
    }

    /// <summary>
    /// The talents that fewer than <paramref name="percent"/> % of the builds aiming at their branch take, with their share
    /// (sprint 152, D83, the game designer's 20 % a style): a node counted among the builds that hold a node of its branch, not
    /// among them all, where a key stays rare by the draw's arithmetic alone.
    /// </summary>
    public static IReadOnlyList<(string Talent, double Share)> Rare(IReadOnlyList<IReadOnlyList<string>> builds, GameData data, double percent = 20)
    {
        ArgumentNullException.ThrowIfNull(builds);
        ArgumentNullException.ThrowIfNull(data);
        var rare = new List<(string, double)>();
        foreach (string id in data.Talents.Keys.Order(StringComparer.Ordinal))
        {
            Branch branch = data.Talents[id].Branch;
            IReadOnlyList<string>[] aiming = [.. builds.Where(b => b.Any(t => data.Talents.TryGetValue(t, out TalentSpec? x) && x.Branch == branch))];
            double share = 100.0 * aiming.Count(b => b.Contains(id)) / Math.Max(1, aiming.Length);
            if (share < percent)
                rare.Add((id, share));
        }
        return rare;
    }
}
