namespace Rpg.Core.Tests;

/// <summary>The guild that serves (sprint 156, D83): its level and week, the chest's rights and journal, the crest.</summary>
public class GuildHallTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(149, 1)]
    [InlineData(150, 2)]
    [InlineData(449, 2)]
    [InlineData(450, 3)]
    [InlineData(6749, 9)]
    [InlineData(6750, 10)]
    [InlineData(100_000, 10)]
    public void FromLevelNToTheNext_Costs150TimesN_UpToTen(long points, int level) => Assert.Equal(level, GuildRules.LevelFor(points));

    [Fact]
    public void TheProgress_SaysThePointsIntoTheLevel_AndNothingLeftAtTheTop()
    {
        Assert.Equal((2, 50L, 300L), GuildRules.Progress(200));
        Assert.Equal((GuildRules.TopLevel, 0L, 0L), GuildRules.Progress(7000));
    }

    [Fact]
    public void FiveActivePlayers_MakeLevelTwoInAnEvening()
    {
        // Each hands back the day's three contracts, one of them a group's, and earns two achievements; together, a dungeon.
        long evening = 5 * (2 * GuildRules.ContractPoints(false) + GuildRules.ContractPoints(true) + 2 * GuildRules.AchievementPoints) + GuildRules.DungeonPoints;
        Assert.Equal(2, GuildRules.LevelFor(evening));
    }

    [Fact]
    public void TheWeek_StartsOnMondaysDawn()
    {
        var beforeDawn = new DateTimeOffset(2026, 11, 2, 3, 59, 0, TimeSpan.Zero);
        Assert.Equal(DayOfWeek.Monday, beforeDawn.DayOfWeek);
        Assert.Equal(GuildRules.Week(Now), GuildRules.Week(beforeDawn));
        Assert.Equal(GuildRules.Week(Now) + 1, GuildRules.Week(beforeDawn.AddMinutes(1)));
    }

    [Fact]
    public void ALastWeeksPoints_CountForNothingThisWeek()
    {
        GuildStanding last = GuildRules.Standing(500, 85, 10, 11, GuildRules.DefaultGoal, null, GuildCrests.Default);
        GuildStanding now = GuildRules.Standing(500, 85, 11, 11, GuildRules.DefaultGoal, null, GuildCrests.Default);
        Assert.Equal(0, last.Week);
        Assert.Equal(85, now.Week);
        Assert.Equal(3, now.Level);
    }

    [Fact]
    public void OnlyTheLeader_WritesTheMessageAndTheGoal()
    {
        Assert.Null(GuildRules.WhyNotNotice(GuildRank.Leader, "Ce soir, la Combe !", 120));
        Assert.NotNull(GuildRules.WhyNotNotice(GuildRank.Officer, "Ce soir, la Combe !", 120));
        Assert.NotNull(GuildRules.WhyNotNotice(GuildRank.Leader, new string('a', GuildRules.MessageLength + 1), 120));
        Assert.NotNull(GuildRules.WhyNotNotice(GuildRank.Leader, null, 0));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(6, 3)]
    [InlineData(8, 3)]
    [InlineData(9, 4)]
    [InlineData(10, 4)]
    public void TheChestsTabs_GrowWithTheLevel(int level, int tabs) => Assert.Equal(tabs, GuildChests.TabsAt(level));

    [Fact]
    public void AChestKept_GainsItsNewTabs_ClosedAndEmpty()
    {
        GuildChest chest = GuildChests.Read(null, 1);
        Assert.Single(chest.Tabs);
        GuildChest grown = GuildChests.Read(GuildChests.Write(chest), 6);
        Assert.Equal(3, grown.Tabs.Count);
        Assert.All(grown.Tabs, t => Assert.False(t.Open));
    }

    [Fact]
    public void EveryoneDeposits_TheLeaderAndOfficersTakeOut_MembersFromAnOpenTab()
    {
        GameData data = GameData.Embedded;
        string granite = data.Items.Keys.First();
        GuildChest chest = GuildChests.Read(null, 3);
        var put = new ChestOrder(granite, 5, 0);
        Assert.Null(GuildChests.WhyNotDeposit(chest, put, 5, data));
        Assert.NotNull(GuildChests.WhyNotDeposit(chest, put, 4, data));
        Assert.NotNull(GuildChests.WhyNotDeposit(chest, put with { Tab = 2 }, 5, data));
        Assert.NotNull(GuildChests.WhyNotDeposit(chest, put with { Item = "no-such-thing" }, 5, data));
        chest = GuildChests.Move(chest, 0, granite, 5, "Ondine", Now);

        var take = new ChestOrder(granite, 2, 0);
        Assert.Null(GuildChests.WhyNotWithdraw(chest, take, GuildRank.Leader));
        Assert.Null(GuildChests.WhyNotWithdraw(chest, take, GuildRank.Officer));
        Assert.NotNull(GuildChests.WhyNotWithdraw(chest, take, GuildRank.Member));
        Assert.NotNull(GuildChests.WhyNotWithdraw(chest, take with { Count = 6 }, GuildRank.Leader));

        Assert.NotNull(GuildChests.WhyNotOpen(chest, new ChestTabOrder(0, true), GuildRank.Officer));
        Assert.Null(GuildChests.WhyNotOpen(chest, new ChestTabOrder(0, true), GuildRank.Leader));
        chest = GuildChests.Open(chest, 0, true);
        Assert.Null(GuildChests.WhyNotWithdraw(chest, take, GuildRank.Member));
    }

    [Fact]
    public void ATab_HoldsFortyStacks_ButAStackGrowsWithoutLimit()
    {
        GameData data = GameData.Embedded;
        string[] items = [.. data.Items.Keys.Take(GuildChests.TabSize + 1)];
        GuildChest chest = GuildChests.Read(null, 1);
        foreach (string item in items.Take(GuildChests.TabSize))
            chest = GuildChests.Move(chest, 0, item, 1, "Ondine", Now);
        Assert.NotNull(GuildChests.WhyNotDeposit(chest, new ChestOrder(items[^1], 1, 0), 1, data));
        Assert.Null(GuildChests.WhyNotDeposit(chest, new ChestOrder(items[0], 99, 0), 99, data));
    }

    [Fact]
    public void TheJournal_KeepsTheHundredLastMovements_TheLatestFirst_AndAStackTakenOutGoes()
    {
        string item = GameData.Embedded.Items.Keys.First();
        GuildChest chest = GuildChests.Read(null, 1);
        for (int i = 0; i < GuildChests.JournalSize + 5; i++)
            chest = GuildChests.Move(chest, 0, item, 1, $"N{i}", Now.AddMinutes(i));
        Assert.Equal(GuildChests.JournalSize, chest.Journal.Count);
        Assert.Equal($"N{GuildChests.JournalSize + 4}", chest.Journal[0].Who);
        chest = GuildChests.Move(chest, 0, item, -(GuildChests.JournalSize + 5), "Kesh", Now.AddHours(3));
        Assert.Empty(chest.Tabs[0].Items);
        Assert.Equal(-(GuildChests.JournalSize + 5), chest.Journal[0].Count);
    }

    [Fact]
    public void ACrest_IsAShape_APictogram_AndTwoColoursOfTheCharter()
    {
        Assert.Equal(6, GuildCrests.Shapes.Count);
        Assert.Equal(30, GuildCrests.Icons.Distinct().Count());
        Assert.Null(GuildCrests.WhyNot(GuildCrests.Default));
        Assert.NotNull(GuildCrests.WhyNot(GuildCrests.Default with { Charge = GuildCrests.Default.Field }));
        Assert.NotNull(GuildCrests.WhyNot(GuildCrests.Default with { Icon = "tank" }));
        Assert.NotNull(GuildCrests.WhyNot(GuildCrests.Default with { Field = "#ff00ff" }));
        Assert.NotNull(GuildRules.WhyNotCrest(GuildRank.Officer, GuildCrests.Default));
    }

    [Fact]
    public void ACrestKept_ReadsBack_AndOneNoLongerValid_IsTheDefault()
    {
        var crest = new GuildCrest("banner", "wolf-head", "gris-fond", "osmose-cost");
        Assert.Equal(crest, GuildCrests.Read(GuildCrests.Write(crest)));
        Assert.Equal(GuildCrests.Default, GuildCrests.Read("banner/withdrawn/gris-fond/osmose-cost"));
        Assert.Equal(GuildCrests.Default, GuildCrests.Read(null));
    }
}
