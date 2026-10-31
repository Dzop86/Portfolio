namespace Rpg.Client.Tests;

/// <summary>The words over a player (sprint 173, D89): its guild, then its name just under it, then its title; on hovering it only.</summary>
public class NameTagsTests
{
    [Fact]
    public void TheGuild_StandsAboveTheName_AndTheTitleUnderIt()
    {
        IReadOnlyList<(NameTagLine Line, double At)> all = NameTags.Layout(guild: true, name: true, title: true);
        Assert.Equal([NameTagLine.Guild, NameTagLine.Name, NameTagLine.Title], all.Select(l => l.Line));
        // From the top, each line one spacing under the one before, the last at the lowest height.
        Assert.Equal(NameTags.Lowest + (2 * NameTags.Spacing), all[0].At, 9);
        Assert.Equal(NameTags.Lowest + NameTags.Spacing, all[1].At, 9);
        Assert.Equal(NameTags.Lowest, all[2].At, 9);
        // Out of a guild, without a title: the name alone, at the lowest height.
        Assert.Equal([(NameTagLine.Name, NameTags.Lowest)], NameTags.Layout(guild: false, name: true, title: false));
        IReadOnlyList<(NameTagLine Line, double At)> two = NameTags.Layout(guild: true, name: true, title: false);
        Assert.Equal([NameTagLine.Guild, NameTagLine.Name], two.Select(l => l.Line));
        Assert.True(two[0].At > two[1].At);
    }

    [Fact]
    public void TheMouse_IsOverAPlayer_OnlyWithinItsFigure()
    {
        // Feet at (400, 300), head at 200: 100 pixels tall, 60 wide.
        Assert.True(NameTags.Over(400, 300, 200, 400, 250));
        Assert.True(NameTags.Over(400, 300, 200, 429, 210));
        Assert.False(NameTags.Over(400, 300, 200, 431, 250));
        Assert.False(NameTags.Over(400, 300, 200, 400, 190));
        Assert.False(NameTags.Over(400, 300, 200, 400, 310));
        // Behind the camera or flattened: never over it.
        Assert.False(NameTags.Over(400, 200, 300, 400, 250));
    }
}
