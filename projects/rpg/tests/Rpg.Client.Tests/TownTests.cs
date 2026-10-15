using Rpg.Core;

namespace Rpg.Client.Tests;

public class TownTests
{
    private static TownController Clairval(Cell? at = null) => new(GameData.Embedded, "clairval", at);

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
        // A house, the fountain, water: nothing happens, the player stays.
        Assert.Null(town.Click(new Cell(4, 0)));
        Assert.Null(town.Click(new Cell(8, 5)));
        Assert.Null(town.Click(new Cell(3, 10)));
        Assert.Equal(to, town.Position);
    }

    [Fact]
    public void AClickOnSomeone_WalksUpToThem_AndOpensTheTalk()
    {
        TownController town = Clairval();
        Npc rose = town.Town.Npcs.Single(n => n.Id == "rose");
        TownStep step = town.Click(rose.At)!;
        Assert.Equal(rose, step.TalkTo);
        Assert.True(Math.Abs(town.Position.X - rose.At.X) <= 1 && Math.Abs(town.Position.Y - rose.At.Y) <= 1);
        Assert.Equal("hello", town.Talk!.LineId);
        // While they talk, the town waits.
        Assert.Null(town.Click(town.Town.Spawn));
        town.Answer(0);
        Assert.Equal("wares", town.Talk!.LineId);
        town.Answer(0);
        Assert.Null(town.Talk);
        Assert.Null(town.Fight);
        Assert.NotNull(town.Click(town.Town.Spawn));
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
    public void TheTrainer_AndTheEastGate_LeadToTheTrainingFight()
    {
        TownController town = Clairval();
        town.Click(town.Town.Npcs.Single(n => n.Id == "garance").At);
        town.Answer(0);
        Assert.Equal("training", town.Fight);
        Assert.Null(town.PathTo(town.Town.Spawn));

        TownController gate = Clairval();
        TownStep step = gate.Click(new Cell(15, 5))!;
        Assert.Equal("training", step.Exit!.Scenario);
        Assert.Equal("training", gate.Fight);
    }

    [Fact]
    public void ThePlayerComesBackWhereTheyWere_OrToTheArrival()
    {
        Assert.Equal(new Cell(1, 6), Clairval(new Cell(1, 6)).Position);
        Assert.Equal(new Cell(7, 11), Clairval(new Cell(4, 0)).Position);
        Assert.Equal(new Cell(7, 11), Clairval(new Cell(8, 9)).Position);
        Assert.Equal(new Cell(7, 11), Clairval(new Cell(40, 40)).Position);
        Assert.Equal(new Cell(7, 11), Clairval(new Cell(7, 11)).Position);
        // A tree at the corner of the village.
        Assert.Equal(new Cell(7, 11), Clairval(new Cell(0, 11)).Position);
    }

    [Fact]
    public void TheTour_TalksToEveryone_ReadsEveryLine_AndLeavesByTheGate()
    {
        TownController town = Clairval();
        var talkedTo = new List<string>();
        var read = new HashSet<(string, string)>();
        int lines = TownTour.Run(town, step =>
        {
            if (step.TalkTo is Npc n)
                talkedTo.Add(n.Id);
        }, talk => read.Add((talk.Dialogue.Id, talk.LineId)));
        Assert.Equal(["aubin", "rose", "garance", "ysaline"], talkedTo);
        Assert.Equal(town.Town.Npcs.Select(n => GameData.Embedded.Dialogues[n.Dialogue]).Distinct().Sum(d => d.Lines.Count), read.Count);
        Assert.True(lines >= read.Count);
        Assert.Equal("training", town.Fight);
        Assert.Equal(new Cell(15, 5), town.Position);
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
        TownController town = Clairval();
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
}
