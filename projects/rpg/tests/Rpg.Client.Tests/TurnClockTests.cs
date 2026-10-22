using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The turn's clock and the fights to join, on the game's side (sprint 124, D79).</summary>
public class TurnClockTests
{
    private static readonly DateTimeOffset Local = new(2026, 10, 22, 20, 0, 0, TimeSpan.Zero);

    private static SharedFightNews Turn(int fighter, DateTimeOffset serverNow, int seconds, params int[] absent) =>
        new(SharedFightKind.Turn, Guid.Empty, TurnEnds: serverNow.AddSeconds(seconds), Fighter: fighter, Absent: absent, ServerNow: serverNow);

    [Fact]
    public void TheSecondsLeft_FollowTheServersClock_EvenWithTheGamesAheadOrBehind()
    {
        // The server's clock is 7 minutes ahead of the game's: what counts is the 30 seconds it gives.
        foreach (TimeSpan skew in new[] { TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(-3), TimeSpan.Zero })
        {
            var clock = new TurnClock();
            Assert.Null(clock.SecondsLeft(Local));
            clock.Receive(Turn(2, Local + skew, 30), Local);
            Assert.Equal(2, clock.Fighter);
            Assert.Equal(30, clock.SecondsLeft(Local));
            Assert.Equal(21, clock.SecondsLeft(Local.AddSeconds(9.5)));
            Assert.Equal(0, clock.SecondsLeft(Local.AddSeconds(45)));
        }
    }

    [Fact]
    public void TheLastTenSeconds_AreInAlert_AndASoundPlaysOnceAtFive()
    {
        var clock = new TurnClock();
        clock.Receive(Turn(1, Local, 30, 3), Local);
        Assert.Equal([3], clock.Absent);
        Assert.False(clock.Alert(Local.AddSeconds(19)));
        Assert.True(clock.Alert(Local.AddSeconds(20)));
        Assert.False(clock.Sound(Local.AddSeconds(24)));
        Assert.True(clock.Sound(Local.AddSeconds(25)));
        Assert.False(clock.Sound(Local.AddSeconds(26)));
        // The next turn sounds again; the fight over, nothing is counted.
        Assert.Equal(30, clock.TotalSeconds);
        clock.Receive(Turn(3, Local.AddSeconds(30), 10), Local.AddSeconds(30));
        Assert.Equal(10, clock.TotalSeconds);
        Assert.True(clock.Alert(Local.AddSeconds(30)));
        Assert.True(clock.Sound(Local.AddSeconds(35)));
        clock.Stop();
        Assert.Null(clock.SecondsLeft(Local));
        Assert.False(clock.Alert(Local));
        Assert.Empty(clock.Absent);
    }

    [Fact]
    public void AFightToJoin_IsKeptForItsFifteenSeconds_WithItsGroup_UntilJoinedStartedOrOver()
    {
        var offers = new JoinOffers();
        Guid one = Guid.NewGuid(), two = Guid.NewGuid();
        // The server's clock 2 minutes behind: 12 seconds left on it, 12 on the game's.
        DateTimeOffset server = Local.AddMinutes(-2);
        offers.Receive(new SharedFightNews(SharedFightKind.Opened, one, "Rosalie", "group:woods-0-2.1", TurnEnds: server.AddSeconds(12), ServerNow: server), Local);
        offers.Receive(new SharedFightNews(SharedFightKind.Opened, two, "Plop", "dungeon:wolf-den:1"), Local);
        IReadOnlyList<JoinOffer> open = offers.Open(Local);
        Assert.Equal([two, one], open.Select(o => o.Fight));
        Assert.Equal(("woods-0-2.1", 12), (open[1].Group, JoinOffers.SecondsLeft(open[1], Local)));
        Assert.Null(open[0].Group);
        Assert.Equal(SharedRules.JoinFor.TotalSeconds, JoinOffers.SecondsLeft(open[0], Local));
        // Twelve seconds later, Rosalie's is gone; Plop's started.
        Assert.Equal([two], offers.Open(Local.AddSeconds(12)).Select(o => o.Fight));
        offers.Receive(new SharedFightNews(SharedFightKind.Started, two), Local);
        Assert.Empty(offers.Open(Local));
        offers.Receive(new SharedFightNews(SharedFightKind.Opened, one, "Rosalie", "group:woods-0-2.1"), Local);
        offers.Remove(one);
        Assert.Empty(offers.Open(Local));
    }
}
