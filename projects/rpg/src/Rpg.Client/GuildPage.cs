using Rpg.Core;

namespace Rpg.Client;

/// <summary>What one may do to a member of one's guild, from the guild's tab (sprint 145).</summary>
public sealed record GuildActions(bool Promote, bool Demote, bool Kick, bool HandOver);

/// <summary>
/// The guild's tab as it lays itself out, without any engine (sprint 145, D79): one's own rank, whether one may invite,
/// what one may do to each member (<see cref="GuildRules"/>), and when a member offline was last seen, in whole days,
/// hours or minutes.
/// </summary>
public static class GuildPage
{
    /// <summary>One's rank in the guild, or null when not in it.</summary>
    public static GuildRank? RankOf(GuildView guild, Guid me)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return guild.Members.FirstOrDefault(m => m.Id == me)?.Rank;
    }

    /// <summary>Whether one may invite: its leader and officers, while there is room.</summary>
    public static bool CanInvite(GuildView guild, Guid me) =>
        RankOf(guild, me) is GuildRank rank && GuildRules.WhyNotInvite(rank, guild.Members.Count, themInGuild: false) is null;

    /// <summary>What one may do to a member: name it officer or member, send it out, hand it the title.</summary>
    public static GuildActions Actions(GuildView guild, Guid me, GuildMember them)
    {
        ArgumentNullException.ThrowIfNull(them);
        if (RankOf(guild, me) is not GuildRank by)
            return new GuildActions(false, false, false, false);
        bool oneself = them.Id == me;
        return new GuildActions(
            GuildRules.WhyNotRank(by, them.Rank, GuildRank.Officer, oneself) is null && them.Rank == GuildRank.Member,
            GuildRules.WhyNotRank(by, them.Rank, GuildRank.Member, oneself) is null,
            GuildRules.WhyNotKick(by, them.Rank, oneself) is null,
            GuildRules.WhyNotRank(by, them.Rank, GuildRank.Leader, oneself) is null);
    }

    /// <summary>How long ago a member was last seen: whole days, else hours, else minutes (at least 1); null if never.</summary>
    public static (int Count, string Unit)? Ago(DateTimeOffset? lastSeen, DateTimeOffset now)
    {
        if (lastSeen is not DateTimeOffset seen)
            return null;
        TimeSpan gone = now - seen;
        return gone.TotalDays >= 1 ? ((int)gone.TotalDays, "days")
            : gone.TotalHours >= 1 ? ((int)gone.TotalHours, "hours")
            : (Math.Max(1, (int)gone.TotalMinutes), "minutes");
    }

    /// <summary>Whether one writes the message, the goal and the crest: the leader (sprint 156).</summary>
    public static bool CanLead(GuildView guild, Guid me) => RankOf(guild, me) == GuildRank.Leader;

    /// <summary>How far toward the next level, from 0 to 1 (1 at the top).</summary>
    public static double Toward(GuildStanding standing)
    {
        ArgumentNullException.ThrowIfNull(standing);
        return standing.ToNext == 0 ? 1 : (double)standing.Into / standing.ToNext;
    }

    /// <summary>How far toward the week's goal, from 0 to 1.</summary>
    public static double TowardGoal(GuildStanding standing)
    {
        ArgumentNullException.ThrowIfNull(standing);
        return Math.Clamp((double)standing.Week / Math.Max(1, standing.Goal), 0, 1);
    }

    /// <summary>The level a guild needs for its tab <paramref name="tab"/> (from 0): 1, 3, 6, 9.</summary>
    public static int TabLevel(int tab) => Enumerable.Range(1, GuildRules.TopLevel).First(l => GuildChests.TabsAt(l) > tab);

    /// <summary>Whether one may take out of a tab: the leader and the officers, a member from a tab opened to it.</summary>
    public static bool CanTake(ChestView chest, int tab)
    {
        ArgumentNullException.ThrowIfNull(chest);
        return GuildChests.MayTake(chest.Chest, tab, chest.Rank);
    }

    /// <summary>The colour of a crest's part, as its token's value ("#bef374").</summary>
    public static string Colour(string token) => GuildCrests.Colours.TryGetValue(token, out string? hex) ? hex : GuildCrests.Colours[GuildCrests.Default.Field];

    /// <summary>The crest the map's players carry ("shape/icon/field/charge"), or null.</summary>
    public static GuildCrest? CrestOf(string? kept) => kept is null ? null : GuildCrests.Read(kept);

    /// <summary>How many members, against the most a guild may have.</summary>
    public static (int Count, int Most) Room(GuildView guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return (guild.Members.Count, GuildRules.Most);
    }
}
