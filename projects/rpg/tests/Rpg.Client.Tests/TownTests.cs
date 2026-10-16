using Rpg.Core;

namespace Rpg.Client.Tests;

public class TownTests
{
    private static TownController Clairval(Cell? at = null) => new(GameData.Embedded, "clairval", at);

    // Rose's market and Garance's west gate, maps of the village since sprint 102.
    private static TownController Market() => new(GameData.Embedded, "clairval-2-1");

    [Fact]
    public void AClick_WalksTheShortestWay_AroundHousesAndWater()
    {
        TownController town = Clairval();
        // Three cells in from the west edge: nearer would lead to the woods (sprints 83 and 85).
        Cell to = new(3, 6);
        TownStep step = town.Click(to)!;
        Assert.Equal(to, town.Position);
        Assert.Equal(Wander.FindPath(town.Board, town.Town.Spawn, to, c => town.Town.IsFree(town.Board, c))!.Count, step.Path.Count);
        Assert.True(step.Path.Count < town.Town.Spawn.DistanceTo(to));
        Assert.Equal(to, step.Waypoints[^1]);
        Assert.All(step.Path, c => Assert.True(town.Town.IsFree(town.Board, c)));
        // A house, the fountain, a tree: nothing happens, the player stays.
        Assert.Null(town.Click(new Cell(5, 4)));
        Assert.Null(town.Click(new Cell(22, 13)));
        Assert.Null(town.Click(new Cell(43, 21)));
        Assert.Equal(to, town.Position);
    }

    [Fact]
    public void AClickOnSomeone_WalksUpToThem_AndOpensTheTalk()
    {
        TownController town = Market();
        Npc rose = town.Town.Npcs.Single(n => n.Id == "rose");
        TownStep step = town.Click(rose.At)!;
        Assert.Equal(rose, step.TalkTo);
        Assert.True(Math.Abs(town.Position.X - rose.At.X) <= 1 && Math.Abs(town.Position.Y - rose.At.Y) <= 1);
        Assert.Equal("hello", town.Talk!.LineId);
        // While they talk, the town waits.
        Assert.Null(town.Click(town.Town.Spawn));
        town.Answer(1);
        Assert.Equal("wares", town.Talk!.LineId);
        town.Answer(1);
        Assert.Null(town.Talk);
        Assert.Null(town.Fight);
        Assert.False(town.ShopOpened);
        Assert.NotNull(town.Click(town.Town.Spawn));
        // Her first answer opens her stall (sprint 97); the game says it is seen to.
        town.Click(rose.At);
        town.Answer(0);
        Assert.True(town.ShopOpened);
        town.ShopSeenTo();
        Assert.False(town.ShopOpened);
    }

    [Fact]
    public void TheWalkUpToSomeone_EndsOnTheirNearestSide()
    {
        TownController town = Clairval();
        // Aubin stands two steps from the arrival on two sides: one step on the diagonal.
        TownStep step = town.Click(town.Town.Npcs.Single(n => n.Id == "aubin").At)!;
        Assert.Single(step.Path);
    }

    [Fact]
    public void TheTrainer_LeadsToTheTrainingFight_NoGateDoesAnyMore()
    {
        TownController town = new(GameData.Embedded, "clairval-0-1");
        town.Click(town.Town.Npcs.Single(n => n.Id == "garance").At);
        town.Answer(0);
        Assert.Equal("training", town.Fight);
        Assert.Null(town.PathTo(town.Town.Spawn));
        // The east gate to it is gone (sprint 102): Garance offers it in her talk alone.
        Assert.All(GameData.Embedded.Towns.Values.Where(t => Shop.InVillage(t.Id)), t => Assert.Empty(t.Exits));
    }

    [Fact]
    public void ThePlayerComesBackWhereTheyWere_OrToTheArrival()
    {
        Cell arrival = new(24, 16);
        Assert.Equal(new Cell(1, 6), Clairval(new Cell(1, 6)).Position);
        // A house, Aubin's cell, out of the map, the arrival itself.
        Assert.Equal(arrival, Clairval(new Cell(5, 4)).Position);
        Assert.Equal(arrival, Clairval(new Cell(26, 18)).Position);
        Assert.Equal(arrival, Clairval(new Cell(60, 40)).Position);
        Assert.Equal(arrival, Clairval(arrival).Position);
        // A hedge on the square's edge.
        Assert.Equal(arrival, Clairval(new Cell(0, 11)).Position);
    }

    [Fact]
    public void TheTour_TalksToEveryoneOfTheMap_AndReadsEveryLine()
    {
        TownController town = Clairval();
        var talkedTo = new List<string>();
        var read = new HashSet<(string, string)>();
        int lines = TownTour.Run(town, step =>
        {
            if (step.TalkTo is Npc n)
                talkedTo.Add(n.Id);
        }, talk => read.Add((talk.Dialogue.Id, talk.LineId)));
        // The square's people (sprint 102: the others live on the village's other maps).
        Assert.Equal(["aubin", "ysaline"], talkedTo);
        Assert.Equal(town.Town.Npcs.Select(n => GameData.Embedded.Dialogues[n.Dialogue]).Distinct().Sum(d => d.Lines.Count), read.Count);
        Assert.True(lines >= read.Count);
        Assert.Null(town.Fight);
        Assert.Null(town.Travel);
    }

    [Fact]
    public void EveryTownText_ExistsInBothLanguages()
    {
        foreach (string key in new[] { "town.help", "town.back", "town.exit", "town.saved" })
        {
            Assert.True(Texts.Fr.ContainsKey(key), key);
            Assert.True(Texts.En.ContainsKey(key), key);
        }
    }

    [Fact]
    public void ANewClickDuringAWalk_StopsItWhereThePlayerIs_AndWhatItLedToIsForgotten()
    {
        TownController town = Market();
        Npc rose = town.Town.Npcs.Single(n => n.Id == "rose");
        TownStep step = town.Click(rose.At)!;
        Assert.NotNull(town.Talk);
        Cell halfway = step.Path.Count > 1 ? step.Path[0] : town.Town.Spawn;
        town.Interrupt(halfway);
        Assert.Equal(halfway, town.Position);
        Assert.Null(town.Talk);
        Assert.NotNull(town.Click(town.Town.Spawn));
        Assert.Throws<ArgumentException>(() => town.Interrupt(rose.At));
    }

    [Fact]
    public void AClickOnAGatheringSpot_WalksNextToIt_AndAsksForTheGathering_AWalkCancelsIt()
    {
        // Sprint 99: the first spot of a map of the woods.
        HarvestNode node = GameData.Embedded.Nodes.Values.First(n => n.Map.StartsWith("woods-", StringComparison.Ordinal) && n.Kind == NodeKind.Tree);
        var town = new TownController(GameData.Embedded, node.Map);
        Assert.Contains(node, town.Nodes);
        Assert.NotNull(town.Click(node.At));
        Assert.Equal(node, town.Gathering);
        Assert.True(Harvesting.Beside(town.Position, node.At) && town.Position != node.At);
        town.Interrupt(town.Position);
        Assert.Null(town.Gathering);
        town.Click(node.At);
        town.GatheringSeenTo();
        Assert.Null(town.Gathering);
    }
}
