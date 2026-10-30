namespace Rpg.Client.Tests;

/// <summary>The folding panels on the map (sprint 165, D87): the tracker and the menus, folded or not, kept between two games.</summary>
public class HudSettingsTests
{
    private static JournalQuest Quest(params bool[] reached) =>
        new("frogs", "Le chant des grenouilles", "Ondine", "", reached.Select((r, i) => ($"objectif {i}", r)).ToList(), false, false);

    [Fact]
    public void TheSettings_AreKeptAsText_UnfoldedByDefault()
    {
        Assert.Equal(new HudSettings(), HudSettings.Parse(null));
        Assert.Equal(new HudSettings(), HudSettings.Parse(""));
        foreach (HudSettings s in new[] { new HudSettings(), new HudSettings(true), new HudSettings(MenusFolded: true), new HudSettings(true, true) })
            Assert.Equal(s, HudSettings.Parse(s.ToText()));
        // What is unreadable keeps its default, what is unknown is skipped.
        Assert.Equal(new HudSettings(MenusFolded: true), HudSettings.Parse("tracker=x\nmenus=1\nsound=3\nnoise"));
    }

    [Fact]
    public void TheMenus_Unfolded_TakeTheirRows_Folded_TheirHeaderAlone()
    {
        // A hero's ten windows, the mini-map and the menu: five rows of three, 44 + 4 + 5 × 48 + 4 × 4 + 8.
        Assert.Equal(312, HudLayout.MenusHeight(13, 3, folded: false));
        Assert.Equal(44, HudLayout.MenusHeight(13, 3, folded: true));
        // Four a row in a small window: four rows.
        Assert.Equal(44 + 4 + 4 * 48 + 3 * 4 + 8, HudLayout.MenusHeight(13, 4, folded: false));
        // The classless hero of the self-tests: the map, the mini-map and the menu, one row.
        Assert.Equal(44 + 4 + 48 + 8, HudLayout.MenusHeight(3, 3, folded: false));
        Assert.Equal(HudLayout.Header, HudLayout.MenusHeight(0, 3, folded: false));
        Assert.Throws<ArgumentOutOfRangeException>(() => HudLayout.MenusHeight(3, 0, false));
    }

    [Fact]
    public void TheFoldedMenus_CountEveryBadge()
    {
        Assert.Equal(0, HudLayout.Badges([]));
        Assert.Equal(0, HudLayout.Badges([0, 0]));
        Assert.Equal(3, HudLayout.Badges([3, 0]));
        Assert.Equal(5, HudLayout.Badges([3, 2, -1]));
    }

    [Fact]
    public void TheFoldedTracker_SaysTheObjectivesReachedOutOfAll()
    {
        Assert.Equal("1/2", HudLayout.Reached(Quest(true, false)));
        Assert.Equal("3/3", HudLayout.Reached(Quest(true, true, true)));
        Assert.Equal("0/1", HudLayout.Reached(Quest(false)));
    }
}
