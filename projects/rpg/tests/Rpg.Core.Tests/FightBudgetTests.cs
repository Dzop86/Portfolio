namespace Rpg.Core.Tests;

public class FightBudgetTests
{
    [Fact]
    public void AFightPlayedOut_CountsAgainstTheBudget()
    {
        long before = FightBudget.Played;
        Ai.PlayOut(new Fight(GameData.Embedded, "training", 1));
        Assert.True(FightBudget.Played > before);
    }

    [Fact]
    public void AFightPlayedOut_CountsAgainstTheTestThatPlayedIt()
    {
        string me = $"{GetType().FullName}.{nameof(AFightPlayedOut_CountsAgainstTheTestThatPlayedIt)}";
        Parallel.For(0, 3, seed => Ai.PlayOut(new Fight(GameData.Embedded, "training", (ulong)seed)));
        Assert.Equal(3, FightBudget.Costs.Single(c => c.Test == me).Fights);
    }
}
