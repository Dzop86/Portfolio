namespace Rpg.Client.Tests;

public class MenuTests
{
    [Fact]
    public void TheSoundSettings_AreReadBack_AndKeptWithinRange()
    {
        var s = new AudioSettings(Master: 70, Music: 0, Effects: 100, Muted: true);
        Assert.Equal(s, AudioSettings.Parse(s.ToText()));
        Assert.Equal(new AudioSettings(Master: 100, Music: 0), AudioSettings.Parse("master=250\nmusic=-4\nnoise=3\neffects=x"));
        Assert.Equal(new AudioSettings(), AudioSettings.Parse(null));
    }

    [Fact]
    public void AVolume_GoesToDecibels_SilentAtZero_AndMuteSilencesEverything()
    {
        Assert.Equal(0, AudioSettings.Decibels(100), 6);
        Assert.Equal(-6.02, AudioSettings.Decibels(50), 2);
        Assert.Equal(-80, AudioSettings.Decibels(0));
        Assert.Equal(-80, new AudioSettings(Master: 100, Muted: true).MasterDb);
        Assert.True(AudioSettings.Decibels(30) < AudioSettings.Decibels(60));
    }

    [Fact]
    public void EachKey_OpensItsWindow_AndClosesIt_OneWindowAtATime()
    {
        Assert.Equal(GameWindow.Characteristics, WindowState.ForKey('c'));
        Assert.Equal(GameWindow.Spells, WindowState.ForKey('S'));
        Assert.Equal(GameWindow.Inventory, WindowState.ForKey('i'));
        Assert.Equal(GameWindow.Map, WindowState.ForKey('M'));
        Assert.Equal(GameWindow.Journal, WindowState.ForKey('q'));
        Assert.Null(WindowState.ForKey('X'));
        var w = new WindowState();
        w.Toggle(GameWindow.Spells);
        Assert.Equal(GameWindow.Spells, w.Open);
        w.Toggle(GameWindow.Inventory);
        Assert.Equal(GameWindow.Inventory, w.Open);
        w.Toggle(GameWindow.Inventory);
        Assert.Null(w.Open);
    }

    [Fact]
    public void Escape_ClosesTheWindow_ThenOpensAndClosesTheMenu()
    {
        var w = new WindowState();
        w.Toggle(GameWindow.Map);
        w.Escape();
        Assert.Null(w.Open);
        Assert.False(w.MenuOpen);
        w.Escape();
        Assert.True(w.MenuOpen);
        // Behind the menu, the keys do nothing.
        w.Toggle(GameWindow.Spells);
        Assert.Null(w.Open);
        w.Escape();
        Assert.False(w.MenuOpen);
    }

    [Fact]
    public void TheMenuIcon_OpensTheMenuOverTheWindow_WhichIsStillThereAfter()
    {
        var w = new WindowState();
        w.Toggle(GameWindow.Inventory);
        w.ToggleMenu();
        Assert.True(w.MenuOpen);
        Assert.Equal(GameWindow.Inventory, w.Open);
        w.Escape();
        Assert.False(w.MenuOpen);
        Assert.Equal(GameWindow.Inventory, w.Open);
        w.ToggleMenu();
        w.ToggleMenu();
        Assert.False(w.MenuOpen);
    }

    [Fact]
    public void InAFight_OnlyTheCharacteristicsAndTheInventory_Open()
    {
        Assert.Equal([GameWindow.Characteristics, GameWindow.Inventory], WindowState.Bar.Select(b => b.Window).Where(WindowState.OpensInFight));
    }
}
