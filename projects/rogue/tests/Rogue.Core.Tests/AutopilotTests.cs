namespace Rogue.Core.Tests;

public class AutopilotTests
{
    [Fact]
    public void EveryRunEnds_AndSomeEscape()
    {
        Game[] games = Enumerable.Range(0, 100).Select(s => Floors.AutoPlay((ulong)s)).ToArray();
        Assert.All(games, g => Assert.NotEqual(GameState.Playing, g.State));
        Assert.Contains(games, g => g.State == GameState.Escaped);
        Assert.Contains(games, g => g.State == GameState.Dead);
    }

    [Fact]
    public void Autopilot_OnlyChoosesPossibleActions()
    {
        var game = new Game(99);
        while (game.State == GameState.Playing && game.Turn < 5_000)
        {
            GameAction action = Autopilot.Choose(game);
            Assert.True(game.CanApply(action), $"turn {game.Turn}: {action}");
            game.Apply(action);
        }
    }

    [Fact]
    public void Autopilot_DrinksWhenBadlyHurt_AndFightsWhatIsNextToIt()
    {
        Game game = Floors.Play(1, "#####", "#@g.#", "#####");
        Assert.Equal(GameAction.East, Autopilot.Choose(game));
        game.Player.Potions = 1;
        game.Player.Hp = 5;
        Assert.Equal(GameAction.Drink, Autopilot.Choose(game));
    }
}
