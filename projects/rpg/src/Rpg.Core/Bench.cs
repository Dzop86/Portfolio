namespace Rpg.Core;

/// <summary>What one path of a class is worth on the bench: its points, its gain over the hero without a path, and whether its spells were played.</summary>
/// <param name="Path">The branch's id, or from level 50 its lineage's; null for the hero without a path.</param>
/// <param name="Points">Fights won, in percent.</param>
/// <param name="Casts">Each of the path's spells, cast this many times per fight.</param>
public sealed record BenchRow(string? Path, double Points, IReadOnlyDictionary<string, double> Casts)
{
    /// <summary>
    /// A spell of the path played less than this per fight says nothing of the spell (T152, the Leap at 0.1): a low score is then the AI's as
    /// much as the path's, and reads "not conclusive". Mend the AI, do not tune the spell.
    /// </summary>
    public const double LeastCasts = 0.5;

    /// <summary>The path's spells the AI hardly plays.</summary>
    public IEnumerable<string> Unplayed => Casts.Where(c => c.Value < LeastCasts).Select(c => c.Key);

    public bool Conclusive => !Unplayed.Any();
}

/// <summary>The bench of a class at a level: the hero without a path, then each path.</summary>
public sealed record BenchReport(string Class, int Level, int Groups, int FightsPerGroup, BenchRow Plain, IReadOnlyList<BenchRow> Paths)
{
    public double Mean => Paths.Count == 0 ? 0 : Paths.Average(p => p.Points);

    /// <summary>The widest distance of a path from the mean of its class.</summary>
    public double Spread => Paths.Count == 0 ? 0 : Paths.Max(p => Math.Abs(p.Points - Mean));
}

/// <summary>
/// The measuring bench (D96, `docs/classes-conception.md` §7): a class's paths against the groups of 4 and 5 monsters of a family made from the
/// templates at the level, bare, on the same seeds. At jalons and off the CI with many fights; in the CI with a sample of the groups and few.
/// It measures the AI and its decks, not what a path is worth in good hands.
/// </summary>
public static class Bench
{
    /// <summary>The groups of 4 and 5 monsters, or an even sample of them.</summary>
    public static IReadOnlyList<string> MiddleGroups(SyntheticFamily family, int? sample = null)
    {
        ArgumentNullException.ThrowIfNull(family);
        string[] groups = [.. family.Groups.Where(g => g.Size is 4 or 5).Select(g => g.Scenario)];
        if (sample is not int n || n >= groups.Length)
            return groups;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);
        return [.. Enumerable.Range(0, n).Select(i => groups[i * groups.Length / n])];
    }

    /// <summary>The paths of a class at a level: its branches, from <see cref="Specs.Forks"/> their lineages.</summary>
    public static IReadOnlyList<(string Spec, string? Fork)> PathsOf(string cls, int level, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        IEnumerable<Specialisation> specs = data.Specs.Values.Where(s => s.Class == cls);
        return level < Specs.Forks
            ? [.. specs.Select(s => (s.Id, (string?)null))]
            : [.. specs.SelectMany(s => s.Forks.Select(f => (s.Id, (string?)f.Id)))];
    }

    /// <summary>One class at the family's level. <paramref name="deck"/>, a reference deck in order of preference, replaces the automatic deck: the path's spells, then its spells the level has unlocked, up to the bar's size.</summary>
    public static BenchReport Run(SyntheticFamily family, string cls, int fights, int? sample = null, IReadOnlyList<string>? deck = null)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fights);
        IReadOnlyList<string> groups = MiddleGroups(family, sample);
        Hero bare = family.Hero(cls, "none");
        BenchRow Row(string? path, Hero hero, IReadOnlyCollection<string> spells)
        {
            (int Wins, Dictionary<string, int> Casts)[] results = [.. groups.AsParallel().AsOrdered().Select(g => Play(family, g, hero, fights, spells))];
            double played = groups.Count * fights;
            return new BenchRow(path, results.Sum(r => r.Wins) * 100 / played,
                spells.ToDictionary(s => s, s => results.Sum(r => r.Casts.GetValueOrDefault(s)) / played, StringComparer.Ordinal));
        }
        Hero With(Hero h)
        {
            if (deck is null)
                return h;
            IReadOnlyList<string> known = Talents.GrimoireOf(h, family.Data).Spells;
            return h with { Deck = [.. Specs.SpellsOf(h, family.Data).Concat(deck).Distinct(StringComparer.Ordinal).Where(known.Contains).Take(Hero.DeckSize)] };
        }
        var rows = new List<BenchRow>();
        foreach ((string spec, string? fork) in PathsOf(cls, family.Level, family.Data))
        {
            Hero hero = With(bare with { Spec = spec, Fork = fork });
            // A swap with an ally cannot be played alone: its absence says nothing here; the party measures judge it.
            rows.Add(Row(fork ?? spec, hero, [.. Specs.SpellsOf(hero, family.Data).Intersect(hero.DeckOf(family.Data), StringComparer.Ordinal)
                .Where(s => family.Data.Spells[s].Aims is not null || !family.Data.Spells[s].AllEffects.Any(e => e is SwapEffect))]));
        }
        return new BenchReport(cls, family.Level, groups.Count, fights, Row(null, With(bare), []), rows);
    }

    private static (int Wins, Dictionary<string, int> Casts) Play(SyntheticFamily family, string group, Hero hero, int fights, IReadOnlyCollection<string> spells)
    {
        int wins = 0;
        var casts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (ulong seed = 0; seed < (ulong)fights; seed++)
        {
            var fight = new Fight(family.Data, group, seed, hero);
            Ai.PlayOut(fight);
            wins += fight.WinningTeam == 0 ? 1 : 0;
            foreach (CastAction cast in fight.History.OfType<CastAction>().Where(c => spells.Contains(c.Spell)))
                casts[cast.Spell] = casts.GetValueOrDefault(cast.Spell) + 1;
        }
        return (wins, casts);
    }
}
