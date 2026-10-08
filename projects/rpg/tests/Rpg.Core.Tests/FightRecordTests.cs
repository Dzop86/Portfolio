using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

public class FightRecordTests
{
    [Fact]
    public void ARecordedFight_ReplaysIdentically_ThroughJson()
    {
        foreach (string scenario in Real.Scenarios.Keys)
        {
            for (ulong seed = 0; seed < 200; seed++)
            {
                var f = new Fight(Real, scenario, seed);
                Ai.PlayOut(f);
                string json = FightRecord.Of(f).ToJson();
                Fight again = FightRecord.FromJson(json).Replay(Real);
                Assert.Equal(Fingerprint(f), Fingerprint(again));
                Assert.Equal((f.WinningTeam, f.Round), (again.WinningTeam, again.Round));
            }
        }
    }

    [Fact]
    public void TheSeedDecidesTheRolls()
    {
        var a = new Fight(Real, "duel", 5);
        var b = new Fight(Real, "duel", 5);
        Ai.PlayOut(a);
        Ai.PlayOut(b);
        Assert.Equal(Fingerprint(a), Fingerprint(b));
        var fingerprints = new HashSet<string>();
        for (ulong seed = 0; seed < 20; seed++)
        {
            var f = new Fight(Real, "duel", seed);
            Ai.PlayOut(f);
            fingerprints.Add(Fingerprint(f));
        }
        Assert.True(fingerprints.Count > 10);
    }

    [Fact]
    public void TheSeedIsWrittenAsAString_SoThatJavaScriptKeepsAll64Bits()
    {
        var f = new Fight(Real, "duel", ulong.MaxValue);
        string json = FightRecord.Of(f).ToJson();
        Assert.Contains("\"seed\":\"18446744073709551615\"", json, StringComparison.Ordinal);
        Assert.Equal(ulong.MaxValue, FightRecord.FromJson(json).Seed);
    }

    [Fact]
    public void ARecordIsSmall()
    {
        var f = new Fight(Real, "training", 7);
        Ai.PlayOut(f);
        Assert.InRange(FightRecord.Of(f).ToJson().Length, 1, 60 * f.History.Count);
    }

    [Fact]
    public void ARefusedAction_MakesTheRecordInvalid()
    {
        var f = new Fight(Real, "training", 3);
        Ai.PlayOut(f);
        FightRecord r = FightRecord.Of(f);
        var cheated = r with { Actions = [new MoveAction(new Cell(10, 10)), .. r.Actions] };
        var e = Assert.Throws<InvalidFightRecordException>(() => cheated.Replay(Real));
        Assert.Contains("Action 0", e.Message, StringComparison.Ordinal);
        var tooLong = r with { Actions = [.. r.Actions, new EndTurnAction()] };
        Assert.Contains("FightOver", Assert.Throws<InvalidFightRecordException>(() => tooLong.Replay(Real)).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"version":2,"scenario":"duel","seed":1,"actions":[]}""")]
    [InlineData("""{"version":1,"scenario":"nowhere","seed":1,"actions":[]}""")]
    [InlineData("""{"version":1,"scenario":"duel","seed":1,"actions":[{"type":"fly"}]}""")]
    [InlineData("""{"version":1,"scenario":"duel","seed":1}""")]
    [InlineData("""{"version":1,"scenario":"duel","seed":1,"actions":[],"score":99}""")]
    [InlineData("not json")]
    public void UnknownOrBrokenRecords_AreRefused(string json) =>
        Assert.Throws<InvalidFightRecordException>(() => FightRecord.FromJson(json).Replay(Real));
}
