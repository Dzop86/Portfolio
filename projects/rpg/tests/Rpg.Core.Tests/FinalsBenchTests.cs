namespace Rpg.Core.Tests;

/// <summary>
/// The classes of four ranks at 100 on the bench (sprint 197, D96, docs/classes-guerrier.md §7; the Archer since sprint 198, docs/classes-archer.md §5; the Mage since sprint 200, docs/classes-mage-2.md §5): their final classes
/// against their lineages alone, alone against the groups of 4 and 5 of a family made from the templates at 100. Off the CI (N2), at the jalons; its bounds are indicative
/// while no zone goes past 60.
/// </summary>
public class FinalsBenchTests
{
    /// <summary>The fights of each row on each group.</summary>
    public const int Fights = 30;

    [Fact(Explicit = true)] // The bench (N2, D97): off the CI, run with -- --explicit only
    public void AtHundred_TheWarriorsFinals_KeepTheirBounds_WhereTheBenchCanJudgeThem() =>
        // The Warden's and the Champion's sisters are judged in a party (F3).
        Check("warrior", ["rampart", "bastion"]);

    [Fact(Explicit = true)] // The bench (N2, D97): off the CI, run with -- --explicit only
    public void AtHundred_TheArchersFinals_KeepTheirBounds_WhereTheBenchCanJudgeThem() => Check("archer", []);

    [Fact(Explicit = true)] // The bench (N2, D97): off the CI, run with -- --explicit only
    public void AtHundred_TheMagesFinals_KeepTheirBounds_WhereTheBenchCanJudgeThem() =>
        // The Time Keeper saves an ally: judged in a party (F3), as the Warden.
        Check("mage", ["time-weaver"]);

    [Fact(Explicit = true)] // The bench (N2, D97): off the CI, run with -- --explicit only
    public void AtHundred_ThePriestsFinals_KeepTheirBounds_WhereTheBenchCanJudgeThem() =>
        // The Life Warden saves an ally and its lineage raises them: judged in a party (F3).
        Check("priest", ["sacred-medic"]);

    private static void Check(string cls, string[] partyJudged)
    {
        BenchReport report = Bench.Run(new SyntheticFamily(Specs.Finals), cls, Fights);
        SpecFork[] forks = [.. GameData.Embedded.Specs.Values.Where(s => s.Class == cls).SelectMany(s => s.Forks)];
        BenchRow Row(string path) => report.Paths.Single(p => p.Path == path);
        var problems = new List<string>();
        var lines = new List<string> { FormattableString.Invariant($"{cls} plain {report.Plain.Points:0.0}") };
        double mean = forks.SelectMany(f => f.Finals!).Average(x => Row(x.Id).Points);
        foreach (SpecFork fork in forks)
        {
            BenchRow alone = Row(fork.Id);
            BenchRow[] finals = [.. fork.Finals!.Select(x => Row(x.Id))];
            lines.Add(FormattableString.Invariant($"{fork.Id} {alone.Points:0.0}: ") + string.Join(", ", finals.Select(f =>
                FormattableString.Invariant($"{f.Path} {f.Points:0.0} ({f.Points - alone.Points:+0.0;-0.0}){(f.Conclusive ? "" : " not conclusive")}"))));
            // F1: a final class against its lineage alone, from -3 to +10.
            foreach (BenchRow f in finals.Where(f => f.Conclusive && alone.Conclusive))
            {
                if (f.Points - alone.Points is < -3 or > 10)
                    problems.Add(FormattableString.Invariant($"{f.Path} {f.Points - alone.Points:+0.0;-0.0} on {fork.Id}"));
            }
            // F2: two sisters within 8 points, but those a party judges.
            if (!partyJudged.Contains(fork.Id) && finals.All(f => f.Conclusive) && Math.Abs(finals[0].Points - finals[1].Points) > 8)
                problems.Add(FormattableString.Invariant($"{fork.Id}: sisters {Math.Abs(finals[0].Points - finals[1].Points):0.0} apart"));
            // F4: each lineage's final classes within 8 of the class's mean.
            if (finals.All(f => f.Conclusive) && Math.Abs(finals.Average(f => f.Points) - mean) > 8)
                problems.Add(FormattableString.Invariant($"{fork.Id}: {finals.Average(f => f.Points) - mean:+0.0;-0.0} from the mean"));
        }
        TestContext.Current.SendDiagnosticMessage(string.Join("\n", lines));
        Assert.True(problems.Count == 0, string.Join("; ", problems) + " | " + string.Join("; ", lines));
    }
}
