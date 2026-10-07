namespace Rogue.Core.Tests;

public class ReplayTests
{
    public static TheoryData<ulong> Seeds()
    {
        var data = new TheoryData<ulong>();
        for (ulong seed = 0; seed < 100; seed++)
            data.Add(seed * 1_000_003 + 17);
        return data;
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void ReplayingARun_GivesTheSameOutcome(ulong seed)
    {
        Game game = Floors.AutoPlay(seed);
        string json = RunRecord.From(game).ToJson();

        ReplayResult result = Replay.Run(json);

        Assert.True(result.IsValid, result.Error.ToString());
        Assert.Equal((game.State, game.Score, game.Depth, game.Turn, game.Kills), (result.State, result.Score, result.Depth, result.Turns, result.Kills));
    }

    /// <summary>
    /// Outcomes written down once: the CI runs them on Linux, Windows and macOS, so the same run
    /// gives the same score on every system (and a change to the rules shows up here).
    /// </summary>
    [Theory]
    [InlineData(0UL, GameState.Escaped, 5, 1496, 2071)]
    [InlineData(7UL, GameState.Dead, 4, 1048, 948)]
    [InlineData(9UL, GameState.Escaped, 5, 1083, 1926)]
    [InlineData(18446744073709551615UL, GameState.Dead, 5, 1224, 1313)]
    public void KnownRuns_EndAsRecorded(ulong seed, GameState state, int depth, int turns, int score)
    {
        Game game = Floors.AutoPlay(seed);
        Assert.Equal((state, depth, turns, score), (game.State, game.Depth, game.Turn, game.Score));
    }

    [Fact]
    public void Json_RoundTrips_AndKeepsLargeSeedsExact()
    {
        var run = new RunRecord(ulong.MaxValue, "nse.>p");
        Assert.Equal("""{"format":"rogue-run","version":1,"seed":"18446744073709551615","actions":"nse.>p"}""", run.ToJson());
        Assert.Equal(run, RunRecord.Parse(run.ToJson()));
    }

    [Theory]
    [InlineData("not json", ReplayError.BadFormat)]
    [InlineData("[]", ReplayError.BadFormat)]
    [InlineData("""{"format":"other","version":1,"seed":"1","actions":""}""", ReplayError.BadFormat)]
    [InlineData("""{"format":"rogue-run","version":2,"seed":"1","actions":""}""", ReplayError.UnsupportedVersion)]
    [InlineData("""{"format":"rogue-run","seed":"1","actions":""}""", ReplayError.UnsupportedVersion)]
    [InlineData("""{"format":"rogue-run","version":1,"seed":1,"actions":""}""", ReplayError.BadFormat)]
    [InlineData("""{"format":"rogue-run","version":1,"seed":"-1","actions":""}""", ReplayError.BadSeed)]
    [InlineData("""{"format":"rogue-run","version":1,"seed":"01","actions":""}""", ReplayError.BadSeed)]
    [InlineData("""{"format":"rogue-run","version":1,"seed":"18446744073709551616","actions":""}""", ReplayError.BadSeed)]
    [InlineData("""{"format":"rogue-run","version":1,"seed":"","actions":""}""", ReplayError.BadSeed)]
    [InlineData("""{"format":"rogue-run","version":1,"seed":"1"}""", ReplayError.BadFormat)]
    [InlineData("""{"format":"rogue-run","version":1,"seed":"1","actions":"","score":9999}""", ReplayError.BadFormat)]
    [InlineData("""{"format":"rogue-run","version":1,"seed":"1","seed":"2","actions":""}""", ReplayError.BadFormat)]
    public void MalformedRuns_AreRejected(string json, ReplayError expected)
    {
        ReplayResult result = Replay.Run(json);
        Assert.False(result.IsValid);
        Assert.Equal(expected, result.Error);
        Assert.Null(result.ActionIndex);
    }

    [Fact]
    public void AnEmptyRun_IsValidButUnfinished()
    {
        ReplayResult result = Replay.Run(new RunRecord(3, ""));
        Assert.True(result.IsValid);
        Assert.Equal((GameState.Playing, 0, 1, 0), (result.State, result.Score, result.Depth, result.Turns));
    }

    [Fact]
    public void UnknownActions_AreRejectedWhereTheyAre()
    {
        ReplayResult result = Replay.Run(new RunRecord(3, "..x."));
        Assert.Equal((ReplayError.UnknownAction, 2), (result.Error, result.ActionIndex));
    }

    [Fact]
    public void ImpossibleActions_AreRejectedWhereTheyAre()
    {
        // Nobody starts on the stairs or with a potion.
        Assert.Equal((ReplayError.IllegalAction, 1), Outcome(new RunRecord(3, ".>")));
        Assert.Equal((ReplayError.IllegalAction, 0), Outcome(new RunRecord(3, "p")));

        // A move into a wall, slipped into a genuine run.
        var game = new Game(11);
        for (int i = 0; i < 40; i++)
            game.Apply(Autopilot.Choose(game));
        Direction blocked = Directions.All.First(d => !game.Map.IsWalkable(game.Player.Position.Step(d)));
        string tampered = ActionCodec.Encode(game.History) + ActionCodec.ToChar(ActionCodec.Move(blocked));
        Assert.Equal((ReplayError.IllegalAction, 40), Outcome(new RunRecord(11, tampered)));
    }

    [Fact]
    public void ActionsAfterTheEnd_AreRejected()
    {
        Game game = Floors.AutoPlay(17);
        Assert.NotEqual(GameState.Playing, game.State);
        string actions = ActionCodec.Encode(game.History);
        Assert.Equal((ReplayError.ActionAfterEnd, actions.Length), Outcome(new RunRecord(17, actions + ".")));
    }

    [Fact]
    public void ARunCannotClaimAnotherSeed()
    {
        Game game = Floors.AutoPlay(23);
        ReplayResult elsewhere = Replay.Run(new RunRecord(24, ActionCodec.Encode(game.History)));
        Assert.False(elsewhere.IsValid && elsewhere.Score == game.Score && elsewhere.Turns == game.Turn);
    }

    [Fact]
    public void OverlongRuns_AreRejectedBeforeBeingPlayed()
    {
        ReplayResult result = Replay.Run(new RunRecord(3, new string('.', RunRecord.MaxActions + 1)));
        Assert.Equal(ReplayError.TooLong, result.Error);
        Assert.Equal(0, result.Turns);
    }

    private static (ReplayError, int?) Outcome(RunRecord run)
    {
        ReplayResult result = Replay.Run(run);
        return (result.Error, result.ActionIndex);
    }
}
