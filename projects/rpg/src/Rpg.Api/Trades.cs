using Rpg.Core;

namespace Rpg.Api;

/// <summary>
/// The trades under way (sprint 119, D74): one asks, the other accepts, each puts items and Éclats on the table, and the
/// trade happens once both accept the offers as they stand; any change asks both to accept again. Kept in memory;
/// the endpoints check the offers against the inventories and make the trade in one save, all of it or nothing.
/// </summary>
public sealed class Trades(TimeProvider clock)
{
    private sealed class Deal(Guid a, Guid b)
    {
        public Guid A { get; } = a;
        public Guid B { get; } = b;
        public TradeOffer OfferA { get; set; } = TradeOffer.Nothing;
        public TradeOffer OfferB { get; set; } = TradeOffer.Nothing;
        public bool ReadyA { get; set; }
        public bool ReadyB { get; set; }
        public Guid Other(Guid me) => me == A ? B : A;
    }

    private readonly object _lock = new();
    private readonly Dictionary<Guid, Deal> _deals = [];
    private readonly Dictionary<Guid, (Guid From, DateTimeOffset At)> _asks = [];

    /// <summary>Asks someone to trade: why not (oneself, either already trading), or null.</summary>
    public string? Ask(Guid from, Guid to)
    {
        lock (_lock)
        {
            string? why = from == to ? "Not oneself." : _deals.ContainsKey(from) || _deals.ContainsKey(to) ? "Already trading." : null;
            if (why is null)
                _asks[to] = (from, clock.GetUtcNow());
            return why;
        }
    }

    /// <summary>Opens the trade someone asked for: why not, or null.</summary>
    public string? Accept(Guid to, Guid from)
    {
        lock (_lock)
        {
            if (!_asks.TryGetValue(to, out var ask) || ask.From != from || clock.GetUtcNow() - ask.At > PartyRules.InviteFor)
                return "No trade asked by them, or too late.";
            if (_deals.ContainsKey(from) || _deals.ContainsKey(to))
                return "Already trading.";
            _asks.Remove(to);
            var deal = new Deal(from, to);
            _deals[from] = deal;
            _deals[to] = deal;
            return null;
        }
    }

    /// <summary>Whom the character trades with, if it does.</summary>
    public Guid? With(Guid character)
    {
        lock (_lock)
            return _deals.TryGetValue(character, out Deal? d) ? d.Other(character) : null;
    }

    /// <summary>The trade as the character sees it (its offer, the other's, who accepted), or null.</summary>
    public (Guid With, TradeOffer Mine, TradeOffer Theirs, bool MineReady, bool TheirsReady)? View(Guid character)
    {
        lock (_lock)
        {
            if (!_deals.TryGetValue(character, out Deal? d))
                return null;
            bool a = d.A == character;
            return (d.Other(character), a ? d.OfferA : d.OfferB, a ? d.OfferB : d.OfferA, a ? d.ReadyA : d.ReadyB, a ? d.ReadyB : d.ReadyA);
        }
    }

    /// <summary>The character's offer (already checked): both have to accept again. False when it is not trading.</summary>
    public bool Offer(Guid character, TradeOffer offer)
    {
        lock (_lock)
        {
            if (!_deals.TryGetValue(character, out Deal? d))
                return false;
            if (d.A == character)
                d.OfferA = offer;
            else
                d.OfferB = offer;
            (d.ReadyA, d.ReadyB) = (false, false);
            return true;
        }
    }

    /// <summary>The character accepts the offers as they stand; true when the other has too: the trade is to be made.</summary>
    public bool Ready(Guid character)
    {
        lock (_lock)
        {
            if (!_deals.TryGetValue(character, out Deal? d))
                return false;
            if (d.A == character)
                d.ReadyA = true;
            else
                d.ReadyB = true;
            return d.ReadyA && d.ReadyB;
        }
    }

    /// <summary>The trade ends (made, or called off): the other one, to tell, or null when there was none.</summary>
    public Guid? End(Guid character)
    {
        lock (_lock)
        {
            if (!_deals.Remove(character, out Deal? d))
                return null;
            Guid other = d.Other(character);
            _deals.Remove(other);
            return other;
        }
    }
}
