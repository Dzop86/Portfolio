using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// Balance measured by simulation (lesson of the roguelike's sprint 23): a change of data that makes
/// a scenario trivial or hopeless fails here, not in a player's hands.
/// </summary>
public class BalanceTests
{
    private static int[] Results(string scenario, int fights, Hero? hero = null, Hero? rival = null)
    {
        int[] r = new int[3];
        for (ulong seed = 0; seed < (ulong)fights; seed++)
        {
            var f = new Fight(Real, scenario, seed, hero, rival);
            Ai.PlayOut(f);
            r[f.WinningTeam ?? 2]++;
        }
        return r;
    }

    [Fact]
    public void Training_TheHeroPlayedByTheAi_WinsMostFights_ButNotAll()
    {
        // 66 % since the training's enemies were made sturdier for the classes of sprint 67 (the heroine
        // went from 36 to 80 hit points with them); a human player chooses better than the AI.
        int[] r = Results("training", 1000);
        Assert.InRange(r[0], 600, 780);
        Assert.Equal(0, r[2]);
    }

    [Fact]
    public void Duel_NeitherSideAlwaysWins()
    {
        // About one in two since the AI plans its turns (sprint 56); the second to play won two in three before.
        int[] r = Results("duel", 1000);
        Assert.InRange(r[0], 400, 600);
        Assert.Equal(0, r[2]);
    }

    [Theory]
    [InlineData("sentinel")]
    [InlineData("guard")]
    [InlineData("mage")]
    public void Training_EveryClass_WinsMostFights_ButNotAll(string id)
    {
        // Measured on 1,000 fights after the classes were redone (sprint 67, T34): sentinel 83 %, guard 91 %,
        // mage 65 %. The guard fights up close and the training's enemies come to it: tuned down for it, the
        // other two fall off a cliff (README, limits).
        int[] r = Results("training", 1000, new Hero("Essai", "female-d", id));
        Assert.InRange(r[0], 600, 930);
        Assert.Equal(0, r[2]);
    }

    [Theory]
    [InlineData("sentinel", "guard")]
    [InlineData("sentinel", "mage")]
    [InlineData("guard", "mage")]
    public void AtLevel1_EveryClassDuel_IsWonByEitherSide(string a, string b)
    {
        // Both orders of play, 300 fights each; measured on 400 (sprint 56): 52 %, 38 %, 54 %.
        int[] ab = Results("duel", 300, new Hero("Alpha", "female-a", a), new Hero("Bravo", "male-a", b));
        int[] ba = Results("duel", 300, new Hero("Alpha", "female-a", b), new Hero("Bravo", "male-a", a));
        Assert.InRange((ab[0] + ba[1]) / 6.0, 30, 70);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    public void AtHighLevels_ClassDuels_AlwaysEnd_HealingNeverOutlastsDamage(int level)
    {
        // Erosion takes back a tenth of each blow from the maximum: no duel reaches the round limit,
        // with points spent (sprint 58) or not.
        string[] classes = ["sentinel", "guard", "mage"];
        foreach (string a in classes)
        {
            foreach (string b in classes.Where(c => c != a))
            {
                var alpha = new Hero("Alpha", "female-a", a, Level: level);
                var bravo = new Hero("Bravo", "male-a", b, Level: level);
                Assert.Equal(0, Results("duel", 20, alpha, bravo)[2]);
                Assert.Equal(0, Results("duel", 20, Progression.Built(alpha, Real), Progression.Built(bravo, Real))[2]);
            }
        }
    }

    private static int Wins(string group, string id, int level, int fights) =>
        Results(MonsterGroup.Prefix + group, fights, Progression.Built(new Hero("Essai", "female-d", id, Level: level), Real))[0];

    [Theory]
    [InlineData("woods", 3)]
    [InlineData("mouldy", 10)]
    [InlineData("lake", 18)]
    [InlineData("fields", 26)]
    public void AZone_GroupsOfOneToThree_AreForOnePlayerAtItsLevel_OfSixAndMore_ForSeveral(string zone, int level)
    {
        // Charles's rule (D63): a player alone beats groups of one to three easily, at the zone's level;
        // groups of six to ten are for several players, and a player alone often loses there. Measured on
        // 30 fights per group and class (sprints 72 to 74, T41): the woods at level 3, 98 to 100 % on
        // average, three cases under 80 % (17 to 22 of 30); the mouldy woods at level 10, 98 to 100 %,
        // four cases (16 to 22 of 30); the lake at level 18 (sprint 75, T42), 96 to 100 %, four cases,
        // one under 18 of 30 (13); the fields at level 26 (sprint 76, T43), 100 %; the hordes and the chiefs, 0 to 2 %.
        MonsterGroup[] here = [.. Real.Groups.Values.Where(g => Real.Towns[g.Map].Zone == zone)];
        foreach (string id in new[] { "sentinel", "guard", "mage" })
        {
            int[] small = [.. here.Where(g => g.Monsters.Count <= 3).AsParallel().Select(g => Wins(g.Id, id, level, 30))];
            Assert.True(small.Average() >= 27, $"{id}: {small.Average():0.0} of 30 against the small groups of {zone}");
            Assert.InRange(small.Count(w => w < 18), 0, 2);
            int[] large = [.. here.Where(g => g.Monsters.Count >= 6).AsParallel().Select(g => Wins(g.Id, id, level, 10))];
            Assert.NotEmpty(large);
            Assert.True(large.Average() <= 3, $"{id}: {large.Average():0.0} of 10 against the hordes of {zone}");
        }
    }
}
