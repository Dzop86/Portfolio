using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The turn's clock in a fight with the party (sprint 124, D79), without the engine: whose turn it is and when it ends by
/// the server's clock, set by what the server says (its time with each news, so that a game's clock ahead or behind
/// does not matter); the seconds left, the last ten in alert, one sound at five; the heroes absent.
/// </summary>
public sealed class TurnClock
{
    /// <summary>Seconds left from which the clock is shown in alert.</summary>
    public const int AlertFrom = 10;

    /// <summary>Seconds left at which the sound plays, once a turn.</summary>
    public const int SoundAt = 5;

    private TimeSpan _offset;
    private DateTimeOffset? _ends;
    private bool _sounded;

    /// <summary>The fighter whose turn is counted, if any.</summary>
    public int? Fighter { get; private set; }

    /// <summary>The whole length of the turn counted, in seconds (30, or 10 for an absent hero).</summary>
    public int TotalSeconds { get; private set; } = (int)SharedRules.Turn.TotalSeconds;

    /// <summary>The heroes absent (their turns passed twice in a row by the clock).</summary>
    public IReadOnlyList<int> Absent { get; private set; } = [];

    /// <summary>A news of the server: its time sets the game's clock; a turn's start, its fighter and end.</summary>
    public void Receive(SharedFightNews news, DateTimeOffset localNow)
    {
        ArgumentNullException.ThrowIfNull(news);
        if (news.ServerNow is DateTimeOffset serverNow)
            _offset = serverNow - localNow;
        if (news.Fighter is int fighter && news.TurnEnds is DateTimeOffset ends && news.Shared is SharedFightKind.Turn or SharedFightKind.Started)
        {
            if (fighter != Fighter || ends != _ends)
                _sounded = false;
            (Fighter, _ends) = (fighter, ends);
            if (news.ServerNow is DateTimeOffset at)
                TotalSeconds = Math.Max(1, (int)Math.Round((ends - at).TotalSeconds));
            Absent = news.Absent ?? [];
        }
    }

    /// <summary>Whole seconds left in the turn (rounded up, never below 0), or null when no turn is counted.</summary>
    public int? SecondsLeft(DateTimeOffset localNow) =>
        _ends is DateTimeOffset ends ? Math.Max(0, (int)Math.Ceiling((ends - (localNow + _offset)).TotalSeconds)) : null;

    /// <summary>Whether the seconds left are shown in alert (the last ten).</summary>
    public bool Alert(DateTimeOffset localNow) => SecondsLeft(localNow) is int s && s <= AlertFrom;

    /// <summary>True once a turn, the first time five seconds or less are left: the sound to play.</summary>
    public bool Sound(DateTimeOffset localNow)
    {
        if (_sounded || SecondsLeft(localNow) is not int s || s > SoundAt || s == 0)
            return false;
        _sounded = true;
        return true;
    }

    /// <summary>The fight is over: nothing counted any more.</summary>
    public void Stop() => (Fighter, _ends, Absent) = (null, null, []);
}

/// <summary>A fight a party member opened, to join until <see cref="Until"/> (the game's clock): who, on which group.</summary>
public sealed record JoinOffer(Guid Fight, string Name, string? Group, DateTimeOffset Until);

/// <summary>
/// The fights one may still join (sprint 124, D79): told when a party member opens one, kept until their fifteen seconds
/// end, the question answered or not; each with its seconds left, and the group of monsters it is on, to mark it on the map.
/// </summary>
public sealed class JoinOffers
{
    private readonly List<JoinOffer> _offers = [];

    /// <summary>An opened fight as the server tells it, its end set on the game's clock by the server's time.</summary>
    public void Receive(SharedFightNews news, DateTimeOffset localNow)
    {
        ArgumentNullException.ThrowIfNull(news);
        if (news is { Shared: SharedFightKind.Opened, Name: string name })
        {
            DateTimeOffset until = news.TurnEnds is DateTimeOffset ends && news.ServerNow is DateTimeOffset serverNow
                ? localNow + (ends - serverNow) : localNow + SharedRules.JoinFor;
            string? group = news.Scenario is string sc && sc.StartsWith(MonsterGroup.Prefix, StringComparison.Ordinal) ? sc[MonsterGroup.Prefix.Length..] : null;
            _offers.RemoveAll(o => o.Fight == news.Fight);
            _offers.Add(new JoinOffer(news.Fight, name, group, until));
        }
        else if (news.Shared is SharedFightKind.Started or SharedFightKind.Over)
        {
            _offers.RemoveAll(o => o.Fight == news.Fight);
        }
    }

    /// <summary>Joined, or refused: no longer offered.</summary>
    public void Remove(Guid fight) => _offers.RemoveAll(o => o.Fight == fight);

    /// <summary>The fights still open to join, the latest first; the others are forgotten.</summary>
    public IReadOnlyList<JoinOffer> Open(DateTimeOffset localNow)
    {
        _offers.RemoveAll(o => o.Until <= localNow);
        return [.. Enumerable.Reverse(_offers)];
    }

    /// <summary>Whole seconds left to join, rounded up.</summary>
    public static int SecondsLeft(JoinOffer offer, DateTimeOffset localNow)
    {
        ArgumentNullException.ThrowIfNull(offer);
        return Math.Max(0, (int)Math.Ceiling((offer.Until - localNow).TotalSeconds));
    }
}
