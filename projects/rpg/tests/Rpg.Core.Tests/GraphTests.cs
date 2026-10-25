using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The tree as a graph (sprint 147, D82, <c>docs/talents-arbre-questions.md</c>): the fork's doors (six pairs and the Hearts
/// of Pure element), six styles shared by every pair, a node asking for one of the floor below it, the keys at 45 and one a
/// style, one big switch, the cascade of what is given back; each rule a case accepted and a case refused with its reason,
/// then ten thousand trees drawn at random against the rules written again here.
/// </summary>
public class GraphTests
{
    private static string? P(int level, params string[] ids) => Talents.Problem(ids, level, Real);

    private static TalentRefusal? Why(int level, string id, params string[] taken) => TalentReasons.WhyNot(taken, id, level, Real);

    [Fact]
    public void TheFork_HasSixPairsAndPureElement_TheStylesTheirSix()
    {
        Assert.Equal(["blinding", "bogging", "chilling", "cracking", "electrifying", "fogging"],
            Real.Talents.Values.Where(t => t.Branch == Branch.Fork).Select(t => t.Id).Order(StringComparer.Ordinal));
        Assert.Equal(["heart-air", "heart-earth", "heart-fire", "heart-water"], Real.Talents.Values.Where(t => t.Branch == Branch.Pure).Select(t => t.Id).Order(StringComparer.Ordinal));
        Assert.All(Real.Talents.Values.Where(t => t.IsFork), t => Assert.False(t.HasParent));
        Assert.Equal([Branch.Alchemy, Branch.Melee, Branch.Distance, Branch.Terrain, Branch.Tempo, Branch.Support],
            Real.Talents.Values.Select(t => t.Branch).Where(b => b > Branch.Braids).Distinct().Order());
        // Every node of a style asks for one of the floor below it, the first floor for a door; the keys at 45.
        foreach (TalentSpec t in Real.Talents.Values.Where(t => t.Branch > Branch.Braids))
        {
            Assert.True(t.HasParent, t.Id);
            if (t.Tier == Tier.Key)
                Assert.Equal(45, t.Level);
        }
    }

    [Fact]
    public void AStyle_AsksForADoor_APairOrAHeart_AlchemyAPair()
    {
        Assert.Equal(new TalentRefusal(RefusalKind.Door), Why(30, "up-close"));
        Assert.Null(P(30, "bogging", "up-close"));
        Assert.Null(P(30, "heart-fire", "up-close"));
        // Alchemy lives on reactions: a Heart is no door to it.
        Assert.NotNull(P(30, "heart-fire", "lasting-mark"));
        Assert.Null(P(30, "fogging", "lasting-mark"));
    }

    [Fact]
    public void ANode_AsksForOneOfTheFloorBelow_OneIsEnough()
    {
        TalentRefusal parent = Why(33, "seal", "bogging")!;
        Assert.Equal(RefusalKind.Parent, parent.Kind);
        Assert.Equal(["side-step", "backed", "relay-talent"], parent.Any!);
        Assert.Null(P(33, "bogging", "backed", "seal"));
    }

    [Fact]
    public void TheKeys_OpenAtFortyFive_OneAStyle()
    {
        Assert.Equal(new TalentRefusal(RefusalKind.Level, Number: 45), Why(44, "nomad", "bogging", "side-step", "seal"));
        Assert.Null(P(45, "bogging", "side-step", "seal", "nomad"));
        Assert.Equal(new TalentRefusal(RefusalKind.KeyPerStyle, "nomad"), Why(60, "echo", "bogging", "side-step", "seal", "nomad"));
        // Two keys of two styles.
        Assert.Null(P(60, "bogging", "side-step", "seal", "nomad", "up-close", "heavy-push", "close-quarters"));
    }

    [Fact]
    public void TheHearts_AreAChoice_AndTakeNoElement()
    {
        Assert.Equal(new TalentRefusal(RefusalKind.Excludes, "heart-fire"), Why(30, "heart-air", "heart-fire"));
        // With Lightning and Mud: Pure element takes no element.
        Assert.Null(P(33, "electrifying", "bogging", "heart-fire"));
    }

    [Fact]
    public void ThePairs_AndTheSwitch_AndThePoints_SayWhy()
    {
        Assert.Equal(new TalentRefusal(RefusalKind.Pair, "bogging", Element.Water), Why(35, "chilling", "bogging"));
        Assert.Equal(new TalentRefusal(RefusalKind.Points, Number: 1), Why(30, "electrifying", "bogging", "up-close"));
        Assert.Equal(new TalentRefusal(RefusalKind.Opens, Number: 30), Why(29, "bogging"));
        Assert.Null(Why(30, "bogging"));
    }

    [Fact]
    public void GivingBack_GivesBackWhatHangsOnIt_NotWhatHasAnotherParent()
    {
        // I1 b: giving Electrifying back gives back the Transmutation, Prism and the Signature; the style stays on Mud.
        string[] tree = ["electrifying", "bogging", "transmute-fire-lightning", "prism", "strong-signature", "up-close", "heavy-push"];
        Assert.Null(P(45, tree));
        Assert.Equal(["bogging", "up-close", "heavy-push"], Talents.Without(tree, "electrifying", Real));
        // Mud given back too: nothing holds the style.
        Assert.Empty(Talents.Without(["bogging", "up-close", "heavy-push"], "bogging", Real));
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
                && taken.Where(t => t.Tier == Tier.Key).GroupBy(t => t.Branch).All(g => g.Count() == 1)
                && taken.Count(t => t.IsSwitch) <= 1
                && Talents.ElementsOpened(taken) <= Talents.MaxOpened
                && taken.Where(t => t.IsPair).SelectMany(t => t.Rules.OfType<ReactionRule>()).Select(r => Real.Reactions[r.Reaction])
                    .SelectMany(r => new[] { r.First, r.Second }).GroupBy(e => e).All(g => g.Count() == 1);
            Assert.True(holds == (Talents.Problem(ids, level, Real) is null), $"level {level}: {string.Join(", ", ids)}");
            if (holds)
                accepted++;
        }
        // Enough legal trees among them for the test to mean something.
        Assert.True(accepted > 300, $"{accepted} accepted");
    }
}
