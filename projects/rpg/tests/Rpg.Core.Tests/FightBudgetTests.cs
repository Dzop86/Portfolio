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

    [Fact]
    public void TheBench_IsTheTestsMarkedExplicit_AndOnlyThese()
    {
        // Sprint 195 (D97): what was taken out of the CI, on purpose; a test that plays thousands of fights joins it here.
        string[] explicitTests = [.. typeof(FightBudgetTests).Assembly.GetTypes().SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(FactAttribute), true).Cast<FactAttribute>().Any(f => f.Explicit))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}").Order(StringComparer.Ordinal)];
        Assert.Equal(
        [
            "BalanceTests.AZone_GroupsOfOneToThree_AreForOnePlayerAtItsLevel_OfSixAndMore_ForSeveral",
            "BalanceTests.AtLevel1_EveryClassDuel_IsWonByEitherSide",
            "BalanceTests.Duel_NeitherSideAlwaysWins",
            "FinalsBenchTests.AtHundred_TheArchersFinals_KeepTheirBounds_WhereTheBenchCanJudgeThem",
            "FinalsBenchTests.AtHundred_TheWarriorsFinals_KeepTheirBounds_WhereTheBenchCanJudgeThem",
            "TemplateTests.AFamilyMadeFromTheTemplates_KeepsT41_SmallGroupsForOnePlayer_HordesForSeveral",
            "TemplateTests.AHeroOfOneElement_AtTheMonstersLevel_BeatsNoMoreHordesThanT41Allows",
            "TemplateTests.AtFortyFive_ThePairs_AreWorthAlike",
            "TemplateTests.AtFortyFive_TheStyles_AreWorthAlike",
            "TemplateTests.TheReferenceBuilds_KeepT41_TheTreeWorthAtMostTenPercent_TheFamiliesEven",
            "TemplateTests.TheTalentsOfTheSlice_KeepT41AtThirtyFive_AndGiveTheSentinelAtMostTenPercentMore",
        ], explicitTests);
    }
}
