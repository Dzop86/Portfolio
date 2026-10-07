namespace Rogue.Core.Tests;

public class FieldOfViewTests
{
    [Fact]
    public void WallsBlockTheView_AndAreSeen()
    {
        Game game = Floors.Play(1,
            "###########",
            "#@...#...r#",
            "#....#....#",
            "###########");
        Assert.True(game.IsVisible(new Point(4, 2)));
        Assert.True(game.IsVisible(new Point(5, 1)), "the wall itself is seen");
        Assert.False(game.IsVisible(new Point(6, 1)), "behind the wall");
        Assert.False(game.IsVisible(new Point(9, 1)));
        Assert.False(game.Monsters[0].Awake, "a monster out of sight sleeps");
        Assert.True(game.Map.IsExplored(new Point(1, 2)));
        Assert.False(game.Map.IsExplored(new Point(7, 2)));
    }

    [Fact]
    public void SightEndsAtTheRadius()
    {
        Game game = Floors.Play(1,
            "##########",
            "#@.......#",
            "##########");
        Assert.True(game.IsVisible(new Point(1 + FieldOfView.Radius, 1)));
        Assert.False(game.IsVisible(new Point(2 + FieldOfView.Radius, 1)));
    }

    [Fact]
    public void ExploredTilesAreRemembered()
    {
        Game game = Floors.Play(1,
            "##########",
            "#@.......#",
            "##########");
        for (int i = 0; i < 2; i++)
            game.Apply(GameAction.East);
        for (int i = 0; i < 2; i++)
            game.Apply(GameAction.West);
        Assert.False(game.IsVisible(new Point(8, 1)));
        Assert.True(game.Map.IsExplored(new Point(8, 1)));
    }
}
