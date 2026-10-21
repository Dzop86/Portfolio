namespace Rpg.Core;

/// <summary>Someone in a party (sprint 119, D74): their name, level, class, and the map they are on (none when not in play).</summary>
public sealed record PartyMember(Guid Id, string Name, int Level, string? Class, string? Map);

/// <summary>
/// What the server tells a player of their party (sprint 119), on the map's socket: someone invites them
/// (<see cref="PartyNewsKind.Invited"/>, their id and name), the party as it now is (its leader first, its members),
/// or they are no longer in one.
/// </summary>
public sealed record PartyNews(PartyNewsKind Party, Guid? From = null, string? Name = null, Guid? Leader = null, IReadOnlyList<PartyMember>? Members = null);

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<PartyNewsKind>))]
public enum PartyNewsKind
{
    Invited,
    Changed,
    Left,
}

/// <summary>Asks a character to join one's party, by its name.</summary>
public sealed record PartyInvite(string? Name);

/// <summary>Answers yes to the invitation of a character, by its id; also asks or answers a trade.</summary>
public sealed record PartyAnswer(Guid From);

/// <summary>The party's rules (sprint 119, D74): four at most, the leader invites, an invitation waits a minute.</summary>
public static class PartyRules
{
    public const int Most = 4;
    public static readonly TimeSpan InviteFor = TimeSpan.FromMinutes(1);
}

/// <summary>What one puts on the table in a trade (sprint 119): items, Éclats.</summary>
public sealed record TradeOffer(IReadOnlyList<ItemCount>? Items, long Shards = 0)
{
    public static readonly TradeOffer Nothing = new([], 0);
}

/// <summary>A trade as one of its two players sees it: with whom, both offers, who has accepted them.</summary>
public sealed record TradeView(Guid With, string WithName, TradeOffer Mine, TradeOffer Theirs, bool MineReady, bool TheirsReady);

/// <summary>
/// What the server tells a player of a trade (sprint 119), on the map's socket: someone asks to trade, the trade
/// opened or changed (both offers, who accepted), done (both inventories changed), or called off.
/// </summary>
public sealed record TradeNews(TradeNewsKind Trade, Guid? From = null, string? Name = null, TradeView? View = null);

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<TradeNewsKind>))]
public enum TradeNewsKind
{
    Asked,
    Opened,
    Changed,
    Done,
    Cancelled,
}

/// <summary>
/// The trade's rules (sprint 119, D74), shared by the game and the server: two players of the same map each put
/// items and Éclats on the table, at most <see cref="MostLines"/> kinds of items; only what one owns and does not wear;
/// the trade happens once both accept the offers as they stand, all of it or nothing.
/// </summary>
public static class Trading
{
    public const int MostLines = 12;

    /// <summary>Why the offer cannot be made by someone owning that, wearing that, with these Éclats; null when it can.</summary>
    public static string? WhyNot(TradeOffer offer, IReadOnlyList<ItemCount> owned, IReadOnlyDictionary<Slot, string> worn, long shards, GameData data)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(owned);
        ArgumentNullException.ThrowIfNull(worn);
        ArgumentNullException.ThrowIfNull(data);
        IReadOnlyList<ItemCount> items = offer.Items ?? [];
        if (offer.Shards < 0 || offer.Shards > shards)
            return "Not that many Éclats.";
        if (items.Count > MostLines || items.Select(i => i.Item).Distinct().Count() != items.Count)
            return $"At most {MostLines} kinds of items, each once.";
        foreach (ItemCount i in items)
        {
            if (!data.Items.ContainsKey(i.Item) || i.Count < 1)
                return $"No item '{i.Item}'.";
            int free = owned.Where(o => o.Item == i.Item).Sum(o => o.Count) - worn.Values.Count(w => w == i.Item);
            if (i.Count > free)
                return $"Not that many '{i.Item}' to give (worn ones stay).";
        }
        return null;
    }
}
