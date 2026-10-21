using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The offer put together in the trade's window (sprint 119): owned and not worn, one by one, Éclats within what one has.</summary>
public class TradeDraftTests
{
    [Fact]
    public void Items_GoOnTheTableOneByOne_WithinWhatIsOwnedAndNotWorn_AndComeBack()
    {
        var draft = new TradeDraft([new("bread", 2), new("copper-ring", 2)], new Dictionary<Slot, string> { [Slot.Ring1] = "copper-ring" }, 30);
        Assert.Equal(1, draft.Left("copper-ring"));
        Assert.True(draft.Add("copper-ring"));
        Assert.False(draft.Add("copper-ring"));
        Assert.True(draft.Add("bread"));
        Assert.True(draft.Add("bread"));
        Assert.False(draft.Add("bread"));
        Assert.False(draft.Add("boar-bristle"));
        draft.SetShards(50);
        Assert.Equal(30, draft.Offer.Shards);
        Assert.Equal([new ItemCount("copper-ring", 1), new ItemCount("bread", 2)], draft.Offer.Items);
        Assert.True(draft.Remove("copper-ring"));
        Assert.False(draft.Remove("copper-ring"));
        Assert.True(draft.Remove("bread"));
        Assert.Equal([new ItemCount("bread", 1)], draft.Offer.Items);
        draft.SetShards(-5);
        Assert.Equal(0, draft.Offer.Shards);
    }
}
