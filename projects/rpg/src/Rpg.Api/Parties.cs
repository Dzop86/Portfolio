using Rpg.Core;

namespace Rpg.Api;

/// <summary>
/// The parties (sprint 119, D74): four players at most, a leader who invites, an invitation that waits a minute.
/// Kept in memory, in one server process like the chat: a restart breaks the parties up, which the players form again.
/// </summary>
public sealed class Parties(TimeProvider clock)
{
    private sealed class Party
    {
        public Guid Leader { get; set; }
        public List<Guid> Members { get; } = [];
    }

    private readonly object _lock = new();
    private readonly Dictionary<Guid, Party> _of = [];
    private readonly Dictionary<Guid, (Guid From, DateTimeOffset At)> _invites = [];

    /// <summary>The character's party: its leader and its members (the leader first), or null.</summary>
    public (Guid Leader, IReadOnlyList<Guid> Members)? Of(Guid character)
    {
        lock (_lock)
            return _of.TryGetValue(character, out Party? p) ? (p.Leader, [.. p.Members]) : null;
    }

    /// <summary>Invites a character: why not (oneself, already in a party, not the leader, the party full), or null.</summary>
    public string? Invite(Guid from, Guid to)
    {
        lock (_lock)
        {
            string? why = from == to ? "Not oneself."
                : _of.ContainsKey(to) ? "Already in a party."
                : _of.TryGetValue(from, out Party? p) && p.Leader != from ? "Only the party's leader invites."
                : p is not null && p.Members.Count >= PartyRules.Most ? $"The party is full ({PartyRules.Most})."
                : null;
            if (why is null)
                _invites[to] = (from, clock.GetUtcNow());
            return why;
        }
    }

    /// <summary>Joins the party of who invited: why not (no such invitation, too late, already in one, full), or null.</summary>
    public string? Accept(Guid to, Guid from)
    {
        lock (_lock)
        {
            if (!_invites.TryGetValue(to, out var invite) || invite.From != from || clock.GetUtcNow() - invite.At > PartyRules.InviteFor)
                return "No invitation from them, or too late.";
            if (_of.ContainsKey(to))
                return "Already in a party.";
            if (!_of.TryGetValue(from, out Party? party))
            {
                party = new Party { Leader = from };
                party.Members.Add(from);
                _of[from] = party;
            }
            if (party.Members.Count >= PartyRules.Most)
                return $"The party is full ({PartyRules.Most}).";
            _invites.Remove(to);
            party.Members.Add(to);
            _of[to] = party;
            return null;
        }
    }

    /// <summary>
    /// Leaves the party: the next member leads if the leader left; one left alone has no party any more. The members
    /// before, to tell them (empty when not in one).
    /// </summary>
    public IReadOnlyList<Guid> Leave(Guid character)
    {
        lock (_lock)
        {
            if (!_of.Remove(character, out Party? party))
                return [];
            List<Guid> before = [.. party.Members];
            party.Members.Remove(character);
            if (party.Leader == character && party.Members.Count > 0)
                party.Leader = party.Members[0];
            if (party.Members.Count == 1)
            {
                _of.Remove(party.Members[0]);
                party.Members.Clear();
            }
            return before;
        }
    }
}
