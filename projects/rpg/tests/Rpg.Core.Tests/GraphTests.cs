using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The tree as a graph (sprint 147, D82, <c>docs/talents-arbre-questions.md</c>): six styles, a node asking for one of the
/// floor below it, the keys at 45 and one a style, the cascade of what is given back; each rule a case accepted and a case
/// refused with its reason, then ten thousand trees drawn at random against the rules written again here. Since sprint 182
/// (D93), no door: the pairs, the Hearts and the braid left for the Elements page, a style's first floor opens alone.
/// </summary>
public class GraphTests
{
    private static string? P(int level, params string[] ids) => Talents.Problem(ids, level, Real);

    private static TalentRefusal? Why(int level, string id, params string[] taken) => TalentReasons.WhyNot(taken, id, level, Real);

    [Fact]
    public void TheTree_HasSixStyles_EachNodeAboveTheFirstFloorAskingForOneBelow()
    {
        Assert.Equal([Branch.Alchemy, Branch.Melee, Branch.Distance, Branch.Terrain, Branch.Tempo, Branch.Support],
            Real.Talents.Values.Select(t => t.Branch).Distinct().Order());
        // A first floor asks for nothing; every other node for one of the floor below it; the keys at 45.
        foreach (TalentSpec t in Real.Talents.Values)
        {
            Assert.Equal(t.Tier != Tier.First, t.HasParent);
            if (t.Tier == Tier.Key)
                Assert.Equal(45, t.Level);
        }
    }

    [Fact]
    public void AStyle_AsksForNoDoor_AlchemyNoPair()
    {
        Assert.Null(Why(30, "up-close"));
        Assert.Null(P(30, "lasting-mark"));
        Assert.Null(P(30, "helping-hand"));
    }

    [Fact]
    public void ANode_AsksForOneOfTheFloorBelow_OneIsEnough()
    {
        TalentRefusal parent = Why(33, "seal")!;
        Assert.Equal(RefusalKind.Parent, parent.Kind);
        Assert.Equal(["side-step", "backed", "relay-talent"], parent.Any!);
        Assert.Null(P(33, "backed", "seal"));
    }

    [Fact]
    public void TheKeys_OpenAtFortyFive_OneAStyle()
    {
        Assert.Equal(new TalentRefusal(RefusalKind.Level, Number: 45), Why(44, "nomad", "side-step", "seal"));
        Assert.Null(P(45, "side-step", "seal", "nomad"));
        Assert.Equal(new TalentRefusal(RefusalKind.KeyPerStyle, "nomad"), Why(60, "echo", "side-step", "seal", "nomad"));
        // Two keys of two styles.
        Assert.Null(P(60, "side-step", "seal", "nomad", "up-close", "heavy-push", "close-quarters"));
    }

    [Fact]
    public void TheExclusions_AndThePoints_AndTheLevel_SayWhy()
    {
        Assert.Equal(new TalentRefusal(RefusalKind.Excludes, "heavy-blows"), Why(33, "light-blows", "heavy-blows"));
        Assert.Equal(new TalentRefusal(RefusalKind.Points, Number: 1), Why(30, "side-step", "up-close"));
        Assert.Equal(new TalentRefusal(RefusalKind.Opens, Number: 30), Why(29, "up-close"));
    }

    [Fact]
    public void GivingBack_GivesBackWhatHangsOnIt_NotWhatHasAnotherParent()
    {
        // I1 b: giving Side Step back keeps Seal, which Backed still holds up; giving both back, Seal falls with them.
        string[] tree = ["side-step", "backed", "seal", "up-close", "heavy-push"];
        Assert.Null(P(45, tree));
        Assert.Equal(["backed", "seal", "up-close", "heavy-push"], Talents.Without(tree, "side-step", Real));
        Assert.Equal(["up-close", "heavy-push"], Talents.Without(["backed", "seal", "up-close", "heavy-push"], "backed", Real));
    }

    [Fact]
    public void TenThousandTreesAtRandom_AcceptedOnlyWhenEveryRuleHolds()
    {
        // The rules written again, apart from Talents.Problem: what it accepts must hold them, what breaks one it refuses.
        var random = new Random(147);
        TalentSpec[] all = [.. Real.Talents.Values];
        int accepted = 0;
        for (int n = 0; n < 10_000; n++)
        {
            int level = random.Next(28, 101);
            string[] ids = [.. all.OrderBy(_ => random.Next()).Take(random.Next(1, 9)).Select(t => t.Id)];
            TalentSpec[] taken = [.. ids.Select(id => Real.Talents[id])];
            bool holds = level >= Talents.Opens
                && taken.Sum(t => t.Cost) <= Talents.Points(level)
                && taken.All(t => level >= t.Level)
                && taken.All(t => (t.Requires ?? []).All(ids.Contains) && (t.RequiresAny is not { Count: > 0 } any || any.Any(ids.Contains)))
                && !taken.Any(t => taken.Any(o => o != t && (t.Excludes ?? []).Contains(o.Id)))
                && taken.Where(t => t.Tier == Tier.Key).GroupBy(t => t.Branch).All(g => g.Count() == 1);
            Assert.True(holds == (Talents.Problem(ids, level, Real) is null), $"level {level}: {string.Join(", ", ids)}");
            if (holds)
                accepted++;
        }
        // Enough legal trees among them for the test to mean something.
        Assert.True(accepted > 300, $"{accepted} accepted");
    }
}
