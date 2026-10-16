using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The spots growing back and the gathering's bar (sprint 103, Charles).</summary>
public class GrowingSpotsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AGatheredSpot_VanishesForAMinute_ThenComesBack()
    {
        var spots = new GrowingSpots();
        spots.Taken("oak", Now);
        Assert.True(spots.Contains("oak"));
        Assert.Empty(spots.Ready(Now + TimeSpan.FromSeconds(59)));
        Assert.Equal(["oak"], spots.Ready(Now + TimeSpan.FromMinutes(1)));
        Assert.False(spots.Contains("oak"));
        Assert.Empty(spots.Ids);
    }

    [Fact]
    public void TheServersSpots_ReplaceTheOthers_EachBackInItsTime()
    {
        var spots = new GrowingSpots();
        spots.Taken("elsewhere", Now);
        spots.Reset(new Dictionary<string, DateTimeOffset> { ["b"] = Now + TimeSpan.FromSeconds(20), ["a"] = Now + TimeSpan.FromSeconds(10) });
        Assert.Equal(["a", "b"], spots.Ids.Order(StringComparer.Ordinal));
        Assert.Equal(["a"], spots.Ready(Now + TimeSpan.FromSeconds(15)));
        Assert.Equal(["b"], spots.Ready(Now + TimeSpan.FromSeconds(25)));
    }

    [Fact]
    public void TheBar_FillsOverTheGatheringsThreeSeconds()
    {
        Assert.Equal(0, GrowingSpots.Progress(TimeSpan.Zero));
        Assert.Equal(0.5, GrowingSpots.Progress(Harvesting.Duration / 2), 6);
        Assert.Equal(1, GrowingSpots.Progress(Harvesting.Duration * 2));
        Assert.Equal(0, GrowingSpots.Progress(TimeSpan.FromSeconds(-1)));
    }
}
