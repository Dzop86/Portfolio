using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>What the game says of a tier passed (sprint 186, D93, §v3.5), and the fight's summary that says it.</summary>
public class PathNewsTests
{
    private static readonly GameData Data = GameData.Embedded;

    [Theory]
    [InlineData("fr", "Te voici : Manipulateur arcanique")]
    [InlineData("en", "You are now: Arcane Shaper")]
    public void ATier_SaysItsWord_AndTheEvolution_OfWhoHasAPath(string lang, string evolution)
    {
        var texts = new Texts(lang);
        var mage = new Hero("Ronce", "female-c", "mage", Level: 50, Spec: "arcanist", Fork: "rune-weaver");
        string news = PathNews.Of(mage, 49, Data, texts)!;
        Assert.StartsWith(texts["path.tier.50"], news, StringComparison.Ordinal);
        Assert.Contains(evolution, news, StringComparison.Ordinal);
        Assert.Equal(texts["path.tier.50"], PathNews.Of(mage with { Spec = null }, 49, Data, texts));
        Assert.Equal(texts["path.tier.30"], PathNews.Of(mage with { Spec = null, Level = 30 }, 29, Data, texts));
        Assert.Null(PathNews.Of(mage with { Level = 51 }, 50, Data, texts));
    }

    [Fact]
    public void TheFightsSummary_SaysTheTierPassed()
    {
        var texts = new Texts("fr");
        for (ulong seed = 1; ; seed++)
        {
            var fight = new Fight(Data, "training", seed, new Hero("Ronce", "female-c", "mage", Level: 49, Spec: "arcanist", Fork: null));
            Ai.PlayOut(fight);
            if (fight.WinningTeam != 0)
                continue;
            var summary = new FightSummary(fight, 0, new FightResult(10, Progression.XpToReach(50), 50), texts);
            Assert.Contains(texts["path.tier.50"], summary.Notes, StringComparison.Ordinal);
            Assert.DoesNotContain(texts["path.tier.50"], new FightSummary(fight, 0, new FightResult(10, Progression.XpToReach(49), 49), texts).Notes, StringComparison.Ordinal);
            return;
        }
    }
}
