using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>Clairval's east gate (sprint 129, D79): shut until "Four Trails" is done, then the way to Quatre-Chemins.</summary>
public class EastGateTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static QuestBook Book(params string[] done) => new(Data, 30, done, [], []);

    private static ZoneLink Gate => Data.Towns["clairval-3-1"].Links!.First(l => l.To == Shop.Hamlet);

    [Fact]
    public void BeforeFourTrails_TheGateSaysWhyAndLeadsNowhere_AfterIt_ToQuatreChemins()
    {
        var before = new TownController(Data, "clairval-3-1") { Book = Book("the-disjoint") };
        Assert.Equal(3, before.ShutWays.Count);
        Assert.All(before.ShutWays, l => Assert.Equal(Shop.Hamlet, l.To));
        TownStep step = before.Click(Gate.At)!;
        Assert.Equal((Gate, (ZoneLink?)null, (ZoneLink?)null), (before.Shut!, before.Travel, step.Link));
        before.CloseDoor();
        // A hero without a character (no book): shut too.
        Assert.Equal(3, new TownController(Data, "clairval-3-1").ShutWays.Count);

        var after = new TownController(Data, "clairval-3-1") { Book = Book("the-disjoint", "four-trails") };
        Assert.Empty(after.ShutWays);
        Assert.NotNull(after.Click(Gate.At));
        Assert.Equal(Gate, after.Travel);
        TownController there = after.Enter(after.Travel!);
        Assert.Equal((Shop.Hamlet, Gate.Arrival), (there.Town.Id, there.Position));
    }

    [Fact]
    public void TheShutGate_IsSaidInBothLanguages()
    {
        Assert.Contains("porte de l'Est", new Texts("fr")["way.shut"]);
        Assert.Contains("East Gate", new Texts("en")["way.shut"]);
    }
}
