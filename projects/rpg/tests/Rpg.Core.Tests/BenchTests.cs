using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

public class BenchTests
{
    private static readonly SyntheticFamily At35 = new(35), At50 = new(50);

    [Fact]
    public void ThePaths_AreTheBranchesBeforeFifty_AndTheLineagesFromIt()
    {
        Assert.Equal(3, Bench.PathsOf("warrior", 35, Real).Count);
        Assert.All(Bench.PathsOf("warrior", 35, Real), p => Assert.Null(p.Fork));
        Assert.Equal(6, Bench.PathsOf("warrior", 50, Real).Count);
        Assert.All(Bench.PathsOf("warrior", 50, Real), p => Assert.NotNull(p.Fork));
        // From 100 (sprint 197), each lineage of the Warrior without its final class, then with each of its two; the Archer's too since sprint 198, the Mage's since sprint 200.
        Assert.Equal(18, Bench.PathsOf("warrior", 100, Real).Count);
        Assert.Equal(["rampart", "crusader", "sentry"], Bench.PathsOf("warrior", 100, Real).Take(3).Select(p => p.Final ?? p.Fork));
        Assert.Equal(18, Bench.PathsOf("archer", 100, Real).Count);
        Assert.Equal(18, Bench.PathsOf("mage", 100, Real).Count);
        Assert.All(Bench.PathsOf("warrior", 99, Real), p => Assert.Null(p.Final));
    }

    [Fact]
    public void AClass_IsMeasured_ThePlainHeroAndEachPath_TheSameFromOneRunToTheNext()
    {
        BenchReport report = Bench.Run(At35, "warrior", 2, sample: 2);
        Assert.Equal(2, report.Groups);
        Assert.Null(report.Plain.Path);
        Assert.Equal(3, report.Paths.Count);
        Assert.All(report.Paths, p => Assert.InRange(p.Points, 0, 100));
        BenchReport again = Bench.Run(At35, "warrior", 2, sample: 2);
        Assert.Equal(report.Paths.Select(p => p.Points), again.Paths.Select(p => p.Points));
        Assert.Equal(report.Paths.Select(p => p.Casts.Values.Sum()), again.Paths.Select(p => p.Casts.Values.Sum()));
    }

    [Fact]
    public void APathsHero_SpendsItsSpellPointsKnowingItsPath_OnTheBench()
    {
        // Sprint 210 (the second milestone, docs/jalon-2.md §1): built bare then given a path, a hero's path spells stay at rank 1, under the kit's ranks.
        Hero bare = At35.Hero("archer", "none");
        Hero onPath = bare with { Spec = "sightline" };
        string[] path = [.. Specs.SpellsOf(onPath, At35.Data).Where(s => At35.Data.Spells[s].DamageMax > 0)];
        Assert.NotEmpty(path);
        Assert.All(path, s => Assert.Equal(1, onPath.Ranks?.GetValueOrDefault(s, 1) ?? 1));
        Hero ranked = Progression.Built(onPath, At35.Data);
        Assert.Contains(path, s => ranked.Ranks!.GetValueOrDefault(s, 1) > 1);
        // The bench plays the ranked hero (D98), the bare hero's ranks on demand, the hero without a path untouched.
        BenchReport plain = Bench.Run(At35, "warrior", 2, sample: 2, rankedPaths: false), rankedReport = Bench.Run(At35, "warrior", 2, sample: 2);
        Assert.Equal(plain.Plain.Points, rankedReport.Plain.Points);
        Assert.NotEqual(plain.Paths.Select(p => p.Casts.Values.Sum()), rankedReport.Paths.Select(p => p.Casts.Values.Sum()));
    }

    [Fact]
    public void AGroupSample_IsEven_AndNeverMoreThanTheGroups()
    {
        IReadOnlyList<string> all = Bench.MiddleGroups(At50);
        Assert.Equal(all, Bench.MiddleGroups(At50, all.Count + 5));
        IReadOnlyList<string> some = Bench.MiddleGroups(At50, 4);
        Assert.Equal(4, some.Count);
        Assert.Equal(4, some.Distinct().Count());
        Assert.Equal(all[0], some[0]);
    }

    [Fact]
    public void ASpellHardlyPlayed_MakesItsPathNotConclusive()
    {
        var played = new BenchRow("a", 40, new Dictionary<string, double> { ["x"] = 1.2, ["y"] = 0.5 });
        Assert.True(played.Conclusive);
        var unplayed = new BenchRow("b", 10, new Dictionary<string, double> { ["x"] = 1.2, ["y"] = 0.1 });
        Assert.False(unplayed.Conclusive);
        Assert.Equal(["y"], unplayed.Unplayed);
    }

    [Fact]
    public void TheSpread_IsTheWidestDistanceFromTheMean()
    {
        var empty = new Dictionary<string, double>();
        BenchRow Row(double points) => new("p", points, empty);
        var report = new BenchReport("warrior", 35, 1, 1, Row(20), [Row(10), Row(20), Row(60)]);
        Assert.Equal(30, report.Mean);
        Assert.Equal(30, report.Spread);
    }

    [Fact]
    public void AReferenceDeck_ReplacesTheAutomaticOne_ThePathsSpellsAdded()
    {
        string[] deck = [.. Real.Class("warrior")!.Spells.Where(s => Real.Spells[s].Level <= 10).Take(4)];
        BenchReport report = Bench.Run(At35, "warrior", 1, sample: 1, deck: deck);
        Assert.All(report.Paths, p => Assert.NotEmpty(p.Casts));
    }

    [Fact]
    public void AReferenceDeck_LeavesOutWhatTheLevelHasNotUnlocked_AndStillPlays()
    {
        // Titan Blow comes at 60: the same list serves at every level of the bench, instead of a refused hero.
        string[] deck = ["titan-blow", .. Real.Class("warrior")!.Spells.Where(s => Real.Spells[s].Level <= 10)];
        BenchReport report = Bench.Run(At35, "warrior", 1, sample: 1, deck: deck);
        Assert.All(report.Paths, p => Assert.DoesNotContain("titan-blow", p.Casts.Keys));
        Assert.InRange(report.Plain.Points, 0, 100);
    }
}
