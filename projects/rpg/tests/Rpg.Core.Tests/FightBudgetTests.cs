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
}
