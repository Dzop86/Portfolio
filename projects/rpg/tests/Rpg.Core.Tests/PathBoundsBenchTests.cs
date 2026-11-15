namespace Rpg.Core.Tests;

/// <summary>
/// B5 and B6 on the bench (docs/classes-conception.md §7.3; sprint 220, D101, Q1 of docs/jalon-3.md): a class's branches at 35 and 45, and its lineages at 50
/// and 60, alone against the groups of 4 and 5 of the synthetic family, the same seeds as <c>Rpg.Sim --matrix --simulate 30</c>. Off the CI (N2): a class takes
/// about four minutes on twelve cores. The classes listed are those balanced on the bench; the others join as their sprints come.
/// </summary>
public class PathBoundsBenchTests
{
    /// <summary>The fights of each row on each group, as the jalons' matrix.</summary>
    public const int Fights = 30;

    /// <summary>How far a path may stand from its class's mean, and two lineages of a branch from each other, in the bench's points.</summary>
    public const double Bound = 8;

    [Theory(Explicit = true)] // The bench (N2, D101): off the CI, run with -- --explicit only
    [InlineData("warrior")]
    [InlineData("archer")]
    public void TheBranchesAndTheLineages_StayWithinEightOfTheirClass_AndOfTheirSisters(string cls)
    {
        var problems = new List<string>();
        var lines = new List<string>();
        foreach (int level in new[] { 35, 45, Specs.Forks, 60 })
        {
            BenchReport report = Bench.Run(new SyntheticFamily(level), cls, Fights);
            lines.Add(FormattableString.Invariant($"{level}: mean {report.Mean:0.0}, ") + string.Join(", ", report.Paths.Select(p => FormattableString.Invariant($"{p.Path} {p.Points:0.0}"))));
            // B5 (35, 45) and B6 (50, 60): each path within 8 of the class's mean at its level.
            foreach (BenchRow row in report.Paths.Where(p => Math.Abs(p.Points - report.Mean) > Bound))
                problems.Add(FormattableString.Invariant($"{row.Path} at {level}: {row.Points - report.Mean:+0.0;-0.0} from the mean"));
            if (level < Specs.Forks)
                continue;
            // B6: the two lineages of a branch within 8 of each other.
            foreach (Specialisation spec in GameData.Embedded.Specs.Values.Where(s => s.Class == cls))
            {
                double[] sisters = [.. spec.Forks.Select(f => report.Paths.Single(p => p.Path == f.Id).Points)];
                if (sisters.Max() - sisters.Min() > Bound)
                    problems.Add(FormattableString.Invariant($"{spec.Id} at {level}: lineages {sisters.Max() - sisters.Min():0.0} apart"));
            }
        }
        TestContext.Current.SendDiagnosticMessage(string.Join("\n", lines));
        Assert.True(problems.Count == 0, string.Join("; ", problems) + " | " + string.Join("; ", lines));
    }
}
