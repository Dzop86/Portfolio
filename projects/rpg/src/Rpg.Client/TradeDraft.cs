using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The offer a player puts together in the trade's window (sprint 119, D74): items added one by one from what it owns
/// and does not wear, taken back, Éclats within what it has; it becomes the <see cref="TradeOffer"/> sent.
/// </summary>
public sealed class TradeDraft(IReadOnlyList<ItemCount> owned, IReadOnlyDictionary<Slot, string> worn, long shards)
{
    private readonly Dictionary<string, int> _items = new(StringComparer.Ordinal);
    private readonly List<string> _order = [];

    public long Shards { get; private set; }

    /// <summary>How many of an item can still go on the table: owned, less those worn, less those already on it.</summary>
    public int Left(string item) =>
        owned.Where(o => o.Item == item).Sum(o => o.Count) - worn.Values.Count(w => w == item) - _items.GetValueOrDefault(item);

    /// <summary>One more of an item on the table; false when none is left, or the table holds all the kinds it can.</summary>
    public bool Add(string item)
    {
        if (Left(item) < 1 || (!_items.ContainsKey(item) && _items.Count >= Trading.MostLines))
            return false;
        if (!_items.ContainsKey(item))
            _order.Add(item);
        _items[item] = _items.GetValueOrDefault(item) + 1;
        return true;
    }

    /// <summary>One of an item taken back off the table.</summary>
    public bool Remove(string item)
    {
        if (!_items.TryGetValue(item, out int n))
            return false;
        if (n == 1)
        {
            _items.Remove(item);
            _order.Remove(item);
        }
        else
            _items[item] = n - 1;
        return true;
    }

    /// <summary>The Éclats put on the table, kept within 0 and what the player has.</summary>
    public void SetShards(long value) => Shards = Math.Clamp(value, 0, shards);

    /// <summary>The offer to send, its items in the order they were added.</summary>
    public TradeOffer Offer => new([.. _order.Select(i => new ItemCount(i, _items[i]))], Shards);
}
