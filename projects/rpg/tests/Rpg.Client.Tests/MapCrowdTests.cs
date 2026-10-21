using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The map as the other players share it (sprint 118, D74): who is here and where, the groups away, the spots taken.</summary>
public class MapCrowdTests
{
    private static readonly Guid Me = Guid.NewGuid(), Plop = Guid.NewGuid();
    private static readonly Hero Look = new("Plop", "male-a", "guard");
    private static readonly DateTimeOffset T0 = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AnotherPlayer_Arrives_Walks_AndLeaves_AndOneselfIsLeftOut()
    {
        var crowd = new MapCrowd();
        crowd.Apply(new MapNews(MapNewsKind.Here, Me, "Moi", Look, new Cell(1, 1)), Me);
        Assert.Empty(crowd.Others);
        crowd.Apply(new MapNews(MapNewsKind.Here, Plop, "Plop", Look, new Cell(2, 3)), Me);
        Assert.Equal([new OtherPlayer(Plop, "Plop", Look, new Cell(2, 3))], crowd.Others);
        crowd.Apply(new MapNews(MapNewsKind.Walk, Plop, At: new Cell(5, 6)), Me);
        Assert.Equal(new Cell(5, 6), crowd.Others.Single().At);
        // A walk of someone unknown: nobody to move.
        crowd.Apply(new MapNews(MapNewsKind.Walk, Guid.NewGuid(), At: new Cell(1, 1)), Me);
        Assert.Single(crowd.Others);
        crowd.Apply(new MapNews(MapNewsKind.Gone, Plop), Me);
        Assert.Empty(crowd.Others);
    }

    [Fact]
    public void AGroup_FoughtOrBeaten_IsAway_ThenBack()
    {
        var crowd = new MapCrowd();
        crowd.Apply(new MapNews(MapNewsKind.Fighting, Group: "woods-2-2.0"), Me);
        Assert.True(crowd.IsAway("woods-2-2.0", T0));
        Assert.Equal(["woods-2-2.0"], crowd.Away(T0));
        crowd.Apply(new MapNews(MapNewsKind.Beaten, Group: "woods-2-2.0", Until: T0 + Roam.Respawn), Me);
        Assert.True(crowd.IsAway("woods-2-2.0", T0 + TimeSpan.FromMinutes(1)));
        Assert.False(crowd.IsAway("woods-2-2.0", T0 + Roam.Respawn));
        crowd.Apply(new MapNews(MapNewsKind.Fighting, Group: "woods-2-2.1"), Me);
        crowd.Apply(new MapNews(MapNewsKind.Back, Group: "woods-2-2.1"), Me);
        Assert.Empty(crowd.Away(T0 + Roam.Respawn));
        crowd.Apply(new MapNews(MapNewsKind.Fighting, Group: "woods-2-2.1"), Me);
        crowd.Clear();
        Assert.Empty(crowd.Away(T0));
    }

    [Fact]
    public void ASpotTakenBySomeoneElse_GrowsBackUntilTheServerSays()
    {
        var crowd = new MapCrowd();
        var spots = new GrowingSpots();
        crowd.Apply(new MapNews(MapNewsKind.Taken, Plop, Spot: "oak-1", Until: T0 + TimeSpan.FromMinutes(3)), Me, spots);
        Assert.True(spots.Contains("oak-1"));
        Assert.Empty(spots.Ready(T0 + TimeSpan.FromMinutes(2)));
        Assert.Equal(["oak-1"], spots.Ready(T0 + TimeSpan.FromMinutes(3)));
    }

    [Fact]
    public void TheMapsNews_ComeOnTheChatsSocket_AndStayOutOfItsLines()
    {
        var log = new ChatLog(new Texts("fr"));
        log.Add(new ChatEvent(null, null, new MapNews(MapNewsKind.Gone, Plop)));
        Assert.Empty(log.Lines);
    }
}
