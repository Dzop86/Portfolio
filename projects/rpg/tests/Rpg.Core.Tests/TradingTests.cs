using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The trade's rules (sprint 119, D74): only what one owns and does not wear, Éclats within what one has.</summary>
public class TradingTests
{
    private static readonly ItemCount[] Owned = [new("bread", 3), new("copper-ring", 2), new("boar-bristle", 5)];
    private static readonly Dictionary<Slot, string> Worn = new() { [Slot.Ring1] = "copper-ring" };

    [Fact]
    public void AnOffer_GivesWhatIsOwnedAndNotWorn_AndÉclatsWithinWhatOneHas()
    {
        Assert.Null(Trading.WhyNot(TradeOffer.Nothing, [], new Dictionary<Slot, string>(), 0, Real));
        Assert.Null(Trading.WhyNot(new TradeOffer([new("bread", 3), new("copper-ring", 1)], 40), Owned, Worn, 40, Real));
        // One ring worn: only the other can go.
        Assert.NotNull(Trading.WhyNot(new TradeOffer([new("copper-ring", 2)]), Owned, Worn, 0, Real));
        Assert.NotNull(Trading.WhyNot(new TradeOffer([new("bread", 4)]), Owned, Worn, 0, Real));
        Assert.NotNull(Trading.WhyNot(new TradeOffer([new("bread", 0)]), Owned, Worn, 0, Real));
        Assert.NotNull(Trading.WhyNot(new TradeOffer([new("no-such-thing", 1)]), Owned, Worn, 0, Real));
        Assert.NotNull(Trading.WhyNot(new TradeOffer([new("bread", 1), new("bread", 1)]), Owned, Worn, 0, Real));
        Assert.NotNull(Trading.WhyNot(new TradeOffer([], 41), Owned, Worn, 40, Real));
        Assert.NotNull(Trading.WhyNot(new TradeOffer([], -1), Owned, Worn, 40, Real));
        // Twelve kinds at most.
        ItemCount[] many = [.. Real.Items.Keys.Take(Trading.MostLines + 1).Select(i => new ItemCount(i, 1))];
        Assert.NotNull(Trading.WhyNot(new TradeOffer(many), many, new Dictionary<Slot, string>(), 0, Real));
        Assert.Null(Trading.WhyNot(new TradeOffer(many[..Trading.MostLines]), many, new Dictionary<Slot, string>(), 0, Real));
    }
}
