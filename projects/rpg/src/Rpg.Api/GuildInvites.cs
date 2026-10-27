using Rpg.Core;

namespace Rpg.Api;

/// <summary>
/// The invitations into a guild under way (sprint 145, D79): one a character, from one member, into its guild, for a
/// minute. Kept in memory like the parties' (a restart forgets them; the guilds themselves are in the database).
/// </summary>
public sealed class GuildInvites(TimeProvider clock)
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, (Guid From, Guid Guild, DateTimeOffset At)> _invites = [];

    /// <summary>Invites <paramref name="to"/> into the guild <paramref name="guild"/> of <paramref name="from"/>; a new invitation replaces the one before.</summary>
    public void Invite(Guid from, Guid to, Guid guild)
    {
        lock (_lock)
            _invites[to] = (from, guild, clock.GetUtcNow());
    }

    /// <summary>The guild <paramref name="from"/> invited <paramref name="to"/> into within the minute, the invitation used up; null if none.</summary>
    public Guid? Take(Guid to, Guid from)
    {
        lock (_lock)
        {
            if (!_invites.TryGetValue(to, out var invite) || invite.From != from || clock.GetUtcNow() - invite.At > GuildRules.InviteFor)
                return null;
            _invites.Remove(to);
            return invite.Guild;
        }
    }
}
