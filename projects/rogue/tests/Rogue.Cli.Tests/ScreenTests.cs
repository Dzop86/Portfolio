using Rogue.Core;

namespace Rogue.Cli.Tests;

public class ScreenTests
{
    [Fact]
    public void TheFirstScreen_ShowsThePlayerInALitRoom()
    {
        var game = new Game(42);
        (string[] rows, bool[,] remembered) = Screen.Draw(game);
        Assert.Equal(Map.DefaultHeight, rows.Length);
        Assert.All(rows, r => Assert.Equal(Map.DefaultWidth, r.Length));
        Point p = game.Player.Position;
        Assert.Equal('@', rows[p.Y][p.X]);
        Assert.Single(string.Concat(rows), c => c == '@');
        Assert.False(remembered[p.Y, p.X]);
    }

    [Fact]
    public void TilesOutOfSight_AreRemembered_WithoutTheirMonsters()
    {
        var game = new Game(7);
        while (game.State == GameState.Playing && game.Depth == 1 && game.Turn < 150)
            game.Apply(Autopilot.Choose(game));
        (string[] rows, bool[,] remembered) = Screen.Draw(game);
        bool anyRemembered = false;
        foreach (Point p in game.Map.Points())
        {
            char c = rows[p.Y][p.X];
            if (!game.Map.IsExplored(p))
                Assert.Equal(' ', c);
            else if (!game.IsVisible(p))
            {
                anyRemembered = true;
                Assert.True(remembered[p.Y, p.X]);
                Assert.DoesNotContain(c, "@rgoT");
            }
        }
        Assert.True(anyRemembered);
    }
}
