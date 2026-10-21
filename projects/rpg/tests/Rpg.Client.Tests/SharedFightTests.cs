using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// A fight with the party on the game's side (sprint 121, D74): the same fight as the server's from its start, the
/// player's own hero only, the actions told applied once and in order, the end with a line for each hero.
/// </summary>
public class SharedFightTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly MonsterGroup Group = Data.Groups.Values.First(g => Data.Towns[g.Map].Dungeon is null);
    private static readonly Guid Aube = Guid.NewGuid(), Brume = Guid.NewGuid();

    private static SharedFightStart Start() => new(Guid.NewGuid(), Group.Scenario, 5, Progression.Built(new Hero("Aube", "female-c", "guard", Level: 20), Data), null,
        [Progression.Built(new Hero("Brume", "female-c", "mage", Level: 20), Data)], [null], [Aube, Brume]);

    [Fact]
    public void EachGame_PlaysItsOwnHero_AndTheActionsTold_OnceAndInOrder()
    {
        SharedFightStart start = Start();
        var server = start.Build(Data);
        var mine = new SharedFightSync(start, Brume, Data);
        Assert.Equal(1, mine.Me);
        Assert.Equal("Brume", mine.Hero.Spec.Name.In("fr"));
        var controller = new FightController(mine);
        Assert.Null(controller.PlayAiStep());
        for (int i = 0; i < 400 && !server.IsOver; i++)
        {
            FightAction action = Ai.Decide(server);
            if (server.Apply(action) != ActionError.None)
            {
                action = new EndTurnAction();
                server.Apply(action);
            }
            // Told once; told again (one's own action coming back from the server): passed over.
            mine.Receive(server.History.Count - 1, action);
            mine.Receive(server.History.Count - 1, action);
            Assert.Equal(mine.Fight.Current == mine.Hero && !mine.Fight.IsOver, controller.IsPlayerTurn);
        }
        Assert.False(mine.OutOfStep);
        Assert.Equal(server.History, mine.Fight.History);
        Assert.Equal(server.WinningTeam, mine.Fight.WinningTeam);
        // A gap, or another action under a number already played: out of step.
        var lost = new SharedFightSync(start, Aube, Data);
        lost.Receive(3, new EndTurnAction());
        Assert.True(lost.OutOfStep);
        Assert.Throws<ArgumentException>(() => new SharedFightSync(start, Guid.NewGuid(), Data));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheEnd_HasALineOfRewardsForEachHero_ThePlayersMarked(string lang)
    {
        SharedFightStart start = Start();
        var sync = new SharedFightSync(start, Brume, Data);
        IReadOnlyList<SharedFightResult> results = [new(Aube, "Aube", 120, 9, [new ItemCount("bread", 1)], 20, null), new(Brume, "Brume", 132, 9, [], 21, null)];
        var summary = new FightSummary(sync.Fight, 0, null, new Texts(lang), party: (results, sync.Me));
        SummaryRow[] heroes = [.. summary.Winners.Concat(summary.Losers).Where(r => r.Xp is not null)];
        Assert.Equal(["Aube", "Brume"], heroes.Select(r => r.Name));
        Assert.Equal([120L, 132L], heroes.Select(r => r.Xp!.Value));
        Assert.Equal("Brume", summary.Player?.Name);
        Assert.Equal(21, summary.Player?.Level);
    }
}
