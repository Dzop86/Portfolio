using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// Balance measured by simulation (lesson of the roguelike's sprint 23): a change of data that makes
/// a scenario trivial or hopeless fails here, not in a player's hands.
/// </summary>
public class BalanceTests
{
    private static int[] Results(string scenario, int fights)
    {
        int[] r = new int[3];
        for (ulong seed = 0; seed < (ulong)fights; seed++)
        {
            var f = new Fight(Real, scenario, seed);
            Ai.PlayOut(f);
            r[f.WinningTeam ?? 2]++;
        }
        return r;
    }

    [Fact]
    public void Training_TheHeroPlayedByTheAi_WinsMostFights_ButNotAll()
    {
        // 69 % over 5,000 fights when it was tuned; a human player chooses better than the AI.
        int[] r = Results("training", 1000);
        Assert.InRange(r[0], 600, 780);
        Assert.Equal(0, r[2]);
    }

    [Fact]
    public void Duel_NeitherSideAlwaysWins()
    {
        // The second to play wins about two fights in three (README, limits).
        int[] r = Results("duel", 1000);
        Assert.InRange(r[0], 250, 450);
        Assert.Equal(0, r[2]);
    }
}
