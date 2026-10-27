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
}
