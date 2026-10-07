namespace Rogue.Core.Tests;

public class GameTests
{
    [Fact]
    public void NewGame_StartsOnTheFirstFloor()
    {
        var game = new Game(5);
        Assert.Equal(1, game.Depth);
        Assert.Equal(GameState.Playing, game.State);
        Assert.Equal(Player.StartHp, game.Player.Hp);
        Assert.Equal(0, game.Score);
        Assert.Equal(0, game.Turn);
        Assert.True(game.IsVisible(game.Player.Position));
    }

    [Fact]
    public void Moves_IntoWallsAreRefused_AndCostNothing()
    {
        Game game = Floors.Play(1,
            "####",
            "#@.#",
            "####");
        Assert.False(game.CanApply(GameAction.North));
        Assert.False(game.CanApply(GameAction.West));
        Assert.True(game.CanApply(GameAction.East));
        Assert.Throws<InvalidOperationException>(() => game.Apply(GameAction.North));
        Assert.Equal(0, game.Turn);
        Assert.Empty(game.History);
        game.Apply(GameAction.East);
        Assert.Equal(new Point(2, 1), game.Player.Position);
        Assert.Equal([GameAction.East], game.History);
    }

    [Fact]
    public void StairsAndPotions_AreOnlyUsableWhenThere()
    {
        Game game = Floors.Play(1,
            "#####",
            "#@!>#",
            "#####");
        Assert.False(game.CanApply(GameAction.Descend));
        Assert.False(game.CanApply(GameAction.Drink));
        Assert.Equal([new PotionPicked()], game.Apply(GameAction.East));
        Assert.Equal(1, game.Player.Potions);
        Assert.True(game.CanApply(GameAction.Drink));
        Assert.Equal([new PotionDrunk(0)], game.Apply(GameAction.Drink));
        Assert.Equal(0, game.Player.Potions);
        game.Apply(GameAction.East);
        Assert.True(game.CanApply(GameAction.Descend));
    }

    [Fact]
    public void Potion_HealsUpToTheMaximum()
    {
        Game game = Floors.Play(1, "####", "#@!#", "####");
        game.Apply(GameAction.East);
        game.Player.Hp = 10;
        game.Apply(GameAction.Drink);
        Assert.Equal(10 + Game.PotionHeal, game.Player.Hp);
        game.Player.Potions = 1;
        game.Player.Hp = game.Player.MaxHp - 3;
        Assert.Equal([new PotionDrunk(3)], game.Apply(GameAction.Drink));
        Assert.Equal(game.Player.MaxHp, game.Player.Hp);
    }

    [Fact]
    public void Gold_IsPickedUpAndScored()
    {
        Game game = Floors.Play(1, "####", "#@$#", "####");
        Assert.Equal([new GoldPicked(10)], game.Apply(GameAction.East));
        Assert.Equal(10, game.Gold);
        Assert.Equal(10, game.Score);
        Assert.Empty(game.Items);
    }

    [Fact]
    public void Descending_GeneratesTheNextFloor_AndTheLastStairsEndTheRun()
    {
        Game game = Floors.Play(1, "####", "#@>#", "####");
        game.Apply(GameAction.East);
        Assert.Equal([new FloorReached(2)], game.Apply(GameAction.Descend));
        Assert.Equal(2, game.Depth);
        Assert.Equal(Map.DefaultWidth, game.Map.Width);
        Assert.Equal(Game.PointsPerFloor, game.Score);

        Game last = Floors.Play(Game.MaxDepth, "####", "#@>#", "####");
        last.Apply(GameAction.East);
        Assert.Equal([new DungeonEscaped()], last.Apply(GameAction.Descend));
        Assert.Equal(GameState.Escaped, last.State);
        Assert.Equal(Game.PointsPerFloor * (Game.MaxDepth - 1) + Game.EscapeBonus, last.Score);
        Assert.False(last.CanApply(GameAction.Wait), "nothing after the end");
    }

    [Fact]
    public void Fighting_KillsTheMonster_ScoresItAndGivesExperience()
    {
        Game game = Floors.Play(1, "#####", "#@r.#", "#####");
        Monster rat = game.Monsters[0];
        Assert.True(rat.Awake);
        var events = new List<GameEvent>();
        while (game.Monsters.Count > 0)
            events.AddRange(game.Apply(GameAction.East));

        Assert.Equal(new Point(1, 1), game.Player.Position);
        Assert.Contains(new MonsterKilled(MonsterKind.Rat, 5), events);
        int dealt = events.OfType<PlayerAttacked>().Sum(e => e.Damage);
        Assert.Equal(rat.MaxHp - dealt, rat.Hp);
        Assert.True(rat.Hp <= 0);
        Assert.Equal(1, game.Kills);
        Assert.Equal(5, game.Score);
        Assert.Equal(5, game.Player.Xp);
    }

    [Fact]
    public void Experience_RaisesTheLevel()
    {
        Game game = Floors.Play(1, "#####", "#@r.#", "#####");
        game.Player.Xp = game.Player.XpToNextLevel - 1;
        game.Player.Hp = 20;
        var events = new List<GameEvent>();
        while (game.Monsters.Count > 0 && game.State == GameState.Playing)
            events.AddRange(game.Apply(GameAction.East));
        Assert.Contains(new LevelGained(2), events);
        Assert.Equal(2, game.Player.Level);
        Assert.Equal(4, game.Player.Xp);
        Assert.Equal(Player.StartHp + Game.LevelHp, game.Player.MaxHp);
        int taken = events.OfType<MonsterAttacked>().Sum(e => e.Damage);
        Assert.Equal(20 - taken + Game.LevelHp, game.Player.Hp);
        Assert.Equal(5, game.Player.Attack);
        Assert.Equal(1, game.Player.Defense);
    }

    [Fact]
    public void AwakeMonsters_ChaseThePlayer_AlongAShortestPath()
    {
        Game game = Floors.Play(1,
            "#########",
            "#@......#",
            "#.#####.#",
            "#......o#",
            "#########");
        Monster orc = game.Monsters[0];
        Assert.False(orc.Awake, "out of sight");
        orc.Awake = true;
        game.Apply(GameAction.Wait);
        // From (7,3) the orc is 8 steps away either way; north comes first in the tie-break.
        Assert.Equal(new Point(7, 2), orc.Position);
        for (int i = 0; i < 6; i++)
            game.Apply(GameAction.Wait);
        Assert.Equal(new Point(2, 1), orc.Position);
        Assert.True(orc.Position.IsAdjacentTo(game.Player.Position));
    }

    [Fact]
    public void SleepingMonsters_DoNotMove()
    {
        Game game = Floors.Play(1,
            "#############",
            "#@#########r#",
            "#############");
        game.Apply(GameAction.Wait);
        Assert.False(game.Monsters[0].Awake);
        Assert.Equal(new Point(11, 1), game.Monsters[0].Position);
    }

    [Fact]
    public void Monsters_KillThePlayer_AndEndTheRun()
    {
        Game game = Floors.Play(1, "#####", "#@T.#", "#####");
        var events = new List<GameEvent>();
        while (game.State == GameState.Playing)
            events.AddRange(game.Apply(GameAction.Wait));
        Assert.Equal(GameState.Dead, game.State);
        Assert.Equal(0, game.Player.Hp);
        Assert.Equal(new PlayerDied(MonsterKind.Troll), events[^1]);
        // The last blow may take more than the hit points left.
        Assert.True(events.OfType<MonsterAttacked>().Sum(e => e.Damage) >= Player.StartHp);
        Assert.All(new[] { GameAction.Wait, GameAction.East }, a => Assert.False(game.CanApply(a)));
    }

    [Fact]
    public void Resting_RegainsOneHitPointEveryFewTurns()
    {
        Game game = Floors.Play(1, "####", "#@.#", "####");
        game.Player.Hp = 10;
        for (int i = 0; i < Game.RegenerationPeriod * 3; i++)
            game.Apply(GameAction.Wait);
        Assert.Equal(13, game.Player.Hp);
    }
}
