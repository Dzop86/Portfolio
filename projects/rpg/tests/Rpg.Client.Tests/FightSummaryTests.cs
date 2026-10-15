using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The end-of-fight window without an engine (sprint 87): who won, who lost, what the player earned, the statistics.</summary>
public class FightSummaryTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static Fight Played(ulong seed, int level = 3)
    {
        var fight = new Fight(Data, "training", seed, new Hero("Ronce", "female-c", "mage", Level: level));
        Ai.PlayOut(fight);
        return fight;
    }

    private static Fight AWin()
    {
        for (ulong seed = 1; ; seed++)
        {
            Fight f = Played(seed);
            if (f.WinningTeam == 0)
                return f;
        }
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void AWin_WithTheServersWord_ShowsThePlayersRewards_AndItsBar(string lang)
    {
        var texts = new Texts(lang);
        Fight fight = AWin();
        long total = Progression.XpToReach(4) + 150;
        var result = new FightResult(120, total, 4, "first-lesson", [new ItemCount("bread", 2)], Shards: 6, TotalShards: 40);
        var summary = new FightSummary(fight, 0, result, texts);
        Assert.Equal(texts["victory"], summary.Title);
        Assert.True(summary.Won);
        Assert.All(summary.Winners, r => Assert.Equal(0, fight.Fighters[r.Fighter].Team));
        Assert.All(summary.Losers, r => Assert.Equal((1, false, (long?)null), (fight.Fighters[r.Fighter].Team, r.IsPlayer, r.Xp)));
        Assert.Equal(fight.Fighters.Count(f => !f.IsSummon), summary.Winners.Count + summary.Losers.Count);
        SummaryRow me = summary.Player!;
        Assert.Equal(("Ronce", 4, 120L, 6L), (me.Name, me.Level, me.Xp!.Value, me.Shards!.Value));
        Assert.Equal([new ItemCount("bread", 2)], me.Loot!);
        // 150 of the way from level 4 (600) to level 5 (1,000).
        Assert.Equal(150.0 / (Progression.XpToReach(5) - Progression.XpToReach(4)), me.XpBar!.Value, 6);
        Assert.Equal(texts.XpLine(4, total), me.XpText);
        // The quest and the new level, said under the lines.
        Assert.Contains(texts["result.level", 4], summary.Notes, StringComparison.Ordinal);
        Assert.Contains(Data.Quests["first-lesson"].Name.In(lang), summary.Notes, StringComparison.Ordinal);
        Assert.Equal(FightStats.Of(fight), summary.Stats.Select(s => s.Stats));
        Assert.Equal(texts.Name(fight.Fighters[0]), summary.Stats[0].Name);
    }

    [Fact]
    public void Offline_TheRulesSayWhatItWouldGive_AndThatNothingIsKept()
    {
        var texts = new Texts("fr");
        Fight fight = AWin();
        var summary = new FightSummary(fight, 0, null, texts, offline: true);
        SummaryRow me = summary.Player!;
        Assert.Equal((Progression.FightXp(fight), Progression.FightShards(fight), (double?)null), (me.Xp!.Value, me.Shards!.Value, me.XpBar));
        Assert.Equal(Equipment.Loot(fight), me.Loot!);
        Assert.Equal(texts["result.offline"], summary.Notes);
        // Waiting for the server: the same, without that word.
        Assert.Equal("", new FightSummary(fight, 0, null, texts).Notes);
    }

    [Fact]
    public void ADefeat_PutsTheMonstersFirst_AndTheyHaveNoRewards()
    {
        var texts = new Texts("en");
        Fight lost = new(Data, "training", 1, new Hero("Ronce", "female-c", "mage"));
        while (!lost.IsOver)
            lost.Apply(lost.Current.Team == 0 ? new EndTurnAction() : Ai.Decide(lost));
        var summary = new FightSummary(lost, 0, new FightResult(0, 0, 1), texts);
        Assert.Equal(texts["defeat"], summary.Title);
        Assert.All(summary.Winners, r => Assert.False(r.IsPlayer));
        Assert.Equal((0L, 0L), (summary.Player!.Xp!.Value, summary.Player.Shards!.Value));
        Assert.Contains(summary.Losers, r => r.IsPlayer);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheWindowsWords_AreInBothLanguages(string lang)
    {
        var texts = new Texts(lang);
        foreach (string key in new[] { "end.title", "end.results", "end.stats", "end.winners", "end.losers", "end.close", "end.name", "end.level", "end.xp", "end.shards", "end.loot", "end.dealt", "end.taken", "end.healing", "end.shielding", "end.kills" })
            Assert.NotEqual(key, texts[key]);
        Assert.Contains("12", texts.Result(new FightResult(5, 5, 1, Shards: 12), 1), StringComparison.Ordinal);
    }

    [Fact]
    public void TheEndWindow_SaysTheLifeKept_1IfLost()
    {
        // Sprint 101 (D73): the hit points the hero keeps for the next fight.
        var hero = Progression.Built(new Hero("Élise", "female-c", "guard", Level: 5), GameData.Embedded);
        var fight = new Fight(GameData.Embedded, "training", 1, hero);
        Ai.PlayOut(fight);
        SummaryRow row = new FightSummary(fight, 0, null, new Texts("fr")).Winners.Concat(new FightSummary(fight, 0, null, new Texts("fr")).Losers).Single(r => r.IsPlayer);
        int max = Life.MaxHp(hero, GameData.Embedded);
        Assert.Equal($"Vie : {(fight.WinningTeam == 0 ? Math.Min(fight.HeroFighter!.Hp, max) : 1)} / {max}", row.Life);
    }
}
