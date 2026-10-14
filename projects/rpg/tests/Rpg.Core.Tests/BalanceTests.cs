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
    [InlineData("sentinel")]
    [InlineData("guard")]
    [InlineData("mage")]
    public void TheBoars_AloneAreForBeginners_InGroupsForAFifthLevel_TheirKingForASeventhToNinth(string id)
    {
        // Measured on 30 fights per group (sprint 72, T39): every lone boar at level 1, 30 of 30; every
        // group of two or three at level 5, 28 to 30, but for the guard against the loner, the charger
        // and the piglet of the north-west corner (1: it cannot reach the piglet behind the loner); the
        // king and his escort at level 5, 0 to 14, at level 9, 30.
        foreach (MonsterGroup lone in Real.Groups.Values.Where(g => g.Monsters.Count == 1))
            Assert.InRange(Wins(lone.Id, id, 1, 30), 27, 30);
        MonsterGroup[] groups = [.. Real.Groups.Values.Where(g => g.Monsters.Count > 1 && !g.Monsters.Contains("boar-king"))];
        Assert.InRange(groups.Count(g => Wins(g.Id, id, 5, 30) < 20), 0, 1);
        MonsterGroup king = Real.Groups.Values.Single(g => g.Monsters.Contains("boar-king"));
        Assert.InRange(Wins(king.Id, id, 5, 30), 0, 18);
        Assert.InRange(Wins(king.Id, id, 9, 30), 25, 30);
    }
}
