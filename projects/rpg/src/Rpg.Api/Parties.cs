using Rpg.Core;

namespace Rpg.Api;

/// <summary>
/// The parties (sprint 119, D74): four players at most, a leader who invites, an invitation that waits a minute.
/// Kept in memory, in one server process like the chat: a restart breaks the parties up, which the players form again.
/// Since sprint 124, the leader sends a member out, and a member whose game has been closed for ten minutes leaves.
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
    private readonly Dictionary<Guid, DateTimeOffset> _gone = [];

    /// <summary>The character's game closed its connection: out of play from now (sprint 124).</summary>
    public void Gone(Guid character)
    {
        lock (_lock)
            _gone[character] = clock.GetUtcNow();
    }

    /// <summary>The character's game is connected again.</summary>
    public void Back(Guid character)
    {
        lock (_lock)
            _gone.Remove(character);
    }

    /// <summary>The members of a party out of play for <see cref="PartyRules.OfflineFor"/> or more: to send out.</summary>
    public IReadOnlyList<Guid> Stale()
    {
        DateTimeOffset now = clock.GetUtcNow();
        lock (_lock)
        {
            // Out of play and in no party: nothing to keep.
            foreach (Guid alone in _gone.Keys.Where(k => !_of.ContainsKey(k)).ToList())
                _gone.Remove(alone);
            return [.. _gone.Where(g => now - g.Value >= PartyRules.OfflineFor).Select(g => g.Key)];
        }
    }

    /// <summary>The leader sends a member out (sprint 124): why not, or null and the members before, to tell.</summary>
    public (string? Why, IReadOnlyList<Guid> Before) Kick(Guid by, Guid member)
    {
        lock (_lock)
        {
            if (!_of.TryGetValue(by, out Party? party))
                return ("Not in a party.", []);
            if (PartyRules.WhyNotKick(by, member, party.Leader, party.Members) is string why)
                return (why, []);
            return (null, Leave(member));
        }
    }

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
            _gone.Remove(character);
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
