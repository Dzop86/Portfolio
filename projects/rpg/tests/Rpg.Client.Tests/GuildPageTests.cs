using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The guild's tab (sprint 145): what each rank may do to each member, who invites, when a member was last seen.</summary>
public class GuildPageTests
{
    private static readonly GuildMember Odon = M("Odon", GuildRank.Leader), Brune = M("Brune", GuildRank.Officer), Anne = M("Anne", GuildRank.Member);
    private static readonly GuildView Guild = new(Guid.NewGuid(), "Les Veilleurs", [Odon, Brune, Anne]);

    private static GuildMember M(string name, GuildRank rank) => new(Guid.NewGuid(), name, rank, 30, "guard", null, null);

    [Fact]
    public void TheLeader_NamesOfficers_SendsOut_AndHandsTheTitleOver_NeverToItself()
    {
        Assert.Equal(new GuildActions(true, false, true, true), GuildPage.Actions(Guild, Odon.Id, Anne));
        Assert.Equal(new GuildActions(false, true, true, true), GuildPage.Actions(Guild, Odon.Id, Brune));
        Assert.Equal(new GuildActions(false, false, false, false), GuildPage.Actions(Guild, Odon.Id, Odon));
    }

    [Fact]
    public void AnOfficer_SendsMembersOut_Only_AndAMember_DoesNothing()
    {
        Assert.Equal(new GuildActions(false, false, true, false), GuildPage.Actions(Guild, Brune.Id, Anne));
        Assert.Equal(new GuildActions(false, false, false, false), GuildPage.Actions(Guild, Brune.Id, Odon));
        Assert.Equal(new GuildActions(false, false, false, false), GuildPage.Actions(Guild, Anne.Id, Brune));
        // Someone out of the guild may do nothing at all.
        Assert.Equal(new GuildActions(false, false, false, false), GuildPage.Actions(Guild, Guid.NewGuid(), Anne));
    }

    [Fact]
    public void TheLeaderAndTheOfficers_Invite_WhileThereIsRoom()
    {
        Assert.True(GuildPage.CanInvite(Guild, Odon.Id));
        Assert.True(GuildPage.CanInvite(Guild, Brune.Id));
        Assert.False(GuildPage.CanInvite(Guild, Anne.Id));
        var full = Guild with { Members = [Odon, .. Enumerable.Range(0, GuildRules.Most - 1).Select(i => M("Membre" + i, GuildRank.Member))] };
        Assert.False(GuildPage.CanInvite(full, Odon.Id));
        Assert.Equal((GuildRules.Most, 50), GuildPage.Room(full));
    }

    [Fact]
    public void LastSeen_IsInDays_ElseHours_ElseMinutes()
    {
        DateTimeOffset now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal((3, "days"), GuildPage.Ago(now.AddDays(-3).AddHours(-5), now));
        Assert.Equal((5, "hours"), GuildPage.Ago(now.AddHours(-5).AddMinutes(-20), now));
        Assert.Equal((20, "minutes"), GuildPage.Ago(now.AddMinutes(-20), now));
        Assert.Equal((1, "minutes"), GuildPage.Ago(now.AddSeconds(-10), now));
        Assert.Null(GuildPage.Ago(null, now));
    }
}
