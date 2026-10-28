using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The achievements as the game shows them (sprint 155, D83): categories, progress, titles, the word for a new one.</summary>
public class AchievementsPageTests
{
    private static AchievementView View(params (string Id, int Reached)[] reached)
    {
        var by = reached.ToDictionary(r => r.Id, r => r.Reached);
        return new AchievementView([.. Achievements.All.Select(a => new AchievementProgress(a.Id, by.GetValueOrDefault(a.Id), a.Count))], 0, null);
    }

    [Fact]
    public void TheCategories_KeepTheDatasOrder_AndCountWhatIsEarned()
    {
        IReadOnlyList<AchievementCategory> page = AchievementsPage.Categories(View(("waystones-1", 1), ("waystones-3", 2)), new Texts("fr"));
        Assert.Equal(7, page.Count);
        Assert.Equal(("Exploration", 1), (page[0].Name, page[0].Earned));
        AchievementLine second = page[0].Lines[1];
        Assert.Equal(("2 / 3", false), (second.Progress, second.Earned));
        Assert.Equal("Chasse", page.Single(c => c.Id == "Hunt").Name);
    }

    [Fact]
    public void TheOverview_GivesEachCategoryAndGroup_ItsShare_InTheDatasOrder()
    {
        AchievementOverview o = AchievementsPage.Overview(View(("waystones-1", 1), ("waystone-woods", 1), ("dungeon-boar-wallow", 1)), new Texts("fr"));
        Assert.Equal((3, Achievements.All.Count), (o.Share.Earned, o.Share.Count));
        Assert.Equal(3 * 100 / Achievements.All.Count, o.Share.Percent);
        AchievementBranch exploration = o.Categories[0];
        Assert.Equal(("Exploration", "map", 2, 9, 22), (exploration.Name, exploration.Icon, exploration.Share.Earned, exploration.Share.Count, exploration.Share.Percent));
        Assert.Equal(["Général", "Les pierres"], exploration.Groups.Select(g => g.Name));
        AchievementBranch dungeons = o.Categories.Single(c => c.Id == "Dungeons");
        Assert.Equal("dungeons", dungeons.Icon);
        Assert.Equal(["general", "levels-1-15", "levels-16-30", "levels-31-45"], dungeons.Groups.Select(g => g.Id).Order(StringComparer.Ordinal));
        Assert.Equal((1, 4, 25), (dungeons.Groups.Single(g => g.Id == "levels-1-15").Share.Earned, dungeons.Groups.Single(g => g.Id == "levels-1-15").Share.Count, dungeons.Groups.Single(g => g.Id == "levels-1-15").Share.Percent));
        // The tree and the trust have no groups; 99 % until the last one.
        Assert.Empty(o.Categories.Single(c => c.Id == "Tree").Groups);
        Assert.Equal(99, new AchievementShare(199, 200).Percent);
    }

    [Fact]
    public void EachCategoryWithGroups_PutsEveryAchievementInOne_NamedInBothLanguages()
    {
        foreach (IGrouping<string, AchievementSpec> c in Achievements.All.GroupBy(a => a.Category))
        {
            if (c.Any(a => a.Group is not null))
                Assert.All(c, a => Assert.NotNull(a.Group));
            foreach (string g in c.Select(a => a.Group).OfType<string>().Distinct())
                Assert.All(new[] { Texts.Fr, Texts.En }, table => Assert.True(table.ContainsKey("achievements.group." + g), g));
        }
    }

    [Fact]
    public void TheLines_AreThoseOfACategoryOrGroup_OrThoseFound_LessThoseEarnedWhenHidden()
    {
        var texts = new Texts("fr");
        AchievementView view = View(("dungeon-boar-wallow", 1));
        Assert.Equal(11, AchievementsPage.Lines(view, texts, "Dungeons").Count);
        IReadOnlyList<AchievementLine> low = AchievementsPage.Lines(view, texts, "Dungeons", "levels-1-15");
        Assert.Equal(4, low.Count);
        Assert.All(low, l => Assert.Equal(("Dungeons", "levels-1-15", "dungeons"), (l.Category, l.Group, l.Icon)));
        Assert.Equal(3, AchievementsPage.Lines(view, texts, "Dungeons", "levels-1-15", hideEarned: true).Count);
        // Found in every category, accents and capitals aside.
        Assert.Equal(["dungeon-mother-tree-roots", "chapter-2-slice"], AchievementsPage.Lines(view, texts, null, search: "ARBRE-MERE").Select(l => l.Id));
        Assert.Empty(AchievementsPage.Lines(view, texts, "Dungeons", search: "zzz"));
        Assert.Equal("eleve", AchievementsPage.Plain(" Élève "));
        // The gold band's total, grouped by thousands without any culture.
        Assert.Equal(("14\u202f861", "14,861", "320", "1\u202f000\u202f000"), (AchievementsPage.Grouped(14861, "fr"), AchievementsPage.Grouped(14861, "en"), AchievementsPage.Grouped(320, "fr"), AchievementsPage.Grouped(1_000_000, "fr")));
    }

    [Fact]
    public void TheTitles_AreThoseEarned_InThePlayersLanguage()
    {
        AchievementSpec titled = Achievements.Titled.First();
        Assert.Empty(AchievementsPage.Titles(View(), new Texts("en")));
        Assert.Equal([(titled.Id, titled.Title!.En)], AchievementsPage.Titles(View((titled.Id, titled.Count)), new Texts("en")));
        Assert.Equal(titled.Title!.Fr, AchievementsPage.TitleOf(titled.Id, "fr"));
        Assert.Null(AchievementsPage.TitleOf(null, "fr"));
        Assert.Null(AchievementsPage.TitleOf("no-such", "fr"));
    }

    [Fact]
    public void ANewAchievement_IsTold_OnceAndNotAtTheFirstLook()
    {
        AchievementView before = View(("waystones-1", 1)), after = View(("waystones-1", 1), ("first-victory", 1));
        Assert.Empty(AchievementsPage.NewlyEarned(null, after));
        Assert.Equal(["first-victory"], AchievementsPage.NewlyEarned(before, after));
        Assert.Empty(AchievementsPage.NewlyEarned(after, after));
    }

    [Fact]
    public void AnotherPlayersTitle_ComesWithTheMapsNews()
    {
        var crowd = new MapCrowd();
        Guid plop = Guid.NewGuid();
        crowd.Apply(new MapNews(MapNewsKind.Here, plop, "Plop", new Hero("Plop", "male-a", "guard"), new Cell(3, 3), Title: "dungeons-all"), Guid.NewGuid());
        Assert.Equal("dungeons-all", Assert.Single(crowd.Others).Title);
    }

    [Fact]
    public void ALine_SaysItsReward_WhetherClaimed_ItsQuests_AndACrown_WhatItAsks()
    {
        var texts = new Texts("fr");
        AchievementView view = View(("woods-main", 5), ("woods-all", 1), ("dungeon-boar-wallow", 1)) with { Claimed = ["woods-main"], Quests = ["edge-of-the-woods"] };
        AchievementLine main = AchievementsPage.LineOf(view, texts, "woods-main")!;
        Assert.Equal((true, true, false), (main.Earned, main.Claimed, main.Claimable));
        Assert.Equal(Achievements.Reward(Achievements.All.Single(a => a.Id == "woods-main")), main.Reward);
        Assert.Equal(5, main.Quests.Count);
        Assert.Equal("La lisière", main.Quests[0].Name);
        AchievementLine side = AchievementsPage.LineOf(view, texts, "woods-side")!;
        Assert.DoesNotContain(side.Quests, q => q.Done);
        AchievementLine crown = AchievementsPage.LineOf(view, texts, "woods-all")!;
        Assert.Equal(("achievements", 4), (crown.Icon, crown.Required.Count));
        Assert.Equal([true, false, true, false], crown.Required.Select(r => r.Earned));
        // The badge: earned and not claimed (the boar's dungeon; the woods' crown is not earned).
        Assert.Equal(1, AchievementsPage.ToClaim(view));
        Assert.Equal(1, AchievementsPage.Overview(view, texts).ToClaim);
    }
}
