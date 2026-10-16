using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>Following a quest (sprint 106, D75): where its next objective is, and the way there from any map.</summary>
public class QuestTrailTests
{
    private static readonly GameData Data = GameData.Embedded;

    [Fact]
    public void TheWhisperingStone_LeadsToTheStone_ThenToOdalric_ThenBackToYsaline()
    {
        QuestProgress stone = Quests.Start(Data.Quests["whispering-stone"]);
        var book = new QuestBook(Data, 1, [], [stone], []);
        Assert.Equal(new TrailGoal("clairval", Data.FindProp("old-stone")!.Value.Prop.At), QuestTrail.Goal(book, stone));
        Assert.Equal(new TrailGoal("clairval-2-2", Data.FindNpc("clairval/odalric")!.Value.Npc.At), QuestTrail.Goal(book, stone with { Counts = [1, 1, 0] }));
        Assert.Equal(new TrailGoal("clairval", Data.FindNpc("clairval/ysaline")!.Value.Npc.At), QuestTrail.Goal(book, stone with { Counts = [1, 1, 1] }));
        // On the goal's map, the arrow points at the goal itself.
        Assert.Equal(Data.FindProp("old-stone")!.Value.Prop.At, QuestTrail.Toward(Data, Data.Towns["clairval"], QuestTrail.Goal(book, stone)!));
        // An item to wear has no place: the inventory.
        QuestProgress kit = Quests.Start(Data.Quests["well-equipped"]);
        Assert.Null(QuestTrail.Goal(new QuestBook(Data, 1, ["first-lesson"], [kit], []), kit));
        // A dungeon: its door.
        QuestProgress wallow = Quests.Start(Data.Quests["rumble-under-the-wallow"]);
        Dungeon d = Data.Dungeons["boar-wallow"];
        Assert.Equal(new TrailGoal(d.Map, d.Door), QuestTrail.Goal(new QuestBook(Data, 5, [], [wallow], []), wallow));
    }

    [Theory]
    [InlineData("clairval-3-2", "clairval-2-2")]
    [InlineData("woods-0-0", "clairval")]
    [InlineData("mouldy-0-4", "clairval-0-2")]
    [InlineData("clairval-3-0", "fields-2-4")]
    [InlineData("lake-0-0", "mouldy-2-2")]
    public void FollowingTheArrow_MapAfterMap_ReachesTheGoalsMap(string from, string to)
    {
        var goal = new TrailGoal(to, Data.Towns[to].Spawn);
        string here = from;
        for (int step = 0; step < 40 && here != to; step++)
        {
            Cell way = QuestTrail.Toward(Data, Data.Towns[here], goal) ?? throw new InvalidOperationException($"no way on from {here}");
            ZoneLink link = Data.Towns[here].LinkAt(way) ?? throw new InvalidOperationException($"{way} on {here} is not a way");
            // A way the player can walk to.
            Assert.True(new TownController(Data, here).PathTo(way) is not null, $"{here} {way}");
            here = link.To;
        }
        Assert.Equal(to, here);
        Assert.Equal(goal.At, QuestTrail.Toward(Data, Data.Towns[to], goal));
    }

    [Fact]
    public void ADungeonsHall_HasNoArrow()
    {
        Town hall = Data.Towns[Data.Dungeons["boar-wallow"].Halls![0].Map];
        Assert.Null(QuestTrail.Toward(Data, hall, new TrailGoal("clairval", Data.Towns["clairval"].Spawn)));
    }

    [Fact]
    public void TheLoftsRats_AreFoundAtTheHatch_WhichStartsTheirFight_WhileFirminsQuestWantsIt()
    {
        QuestProgress rats = Quests.Start(Data.Quests["cellar-rats"]);
        var book = new QuestBook(Data, 2, ["well-equipped"], [rats], []);
        (Town granaries, Prop hatch) = Data.FindProp("loft-hatch")!.Value;
        Assert.Equal(new TrailGoal(granaries.Id, hatch.At), QuestTrail.Goal(book, rats));
        Assert.Equal(Data.Quests["cellar-rats"], book.FightFor(hatch));
        // Won: the hatch is a hatch again.
        Assert.Null(new QuestBook(Data, 2, [], [rats with { Counts = [1, 0] }], []).FightFor(hatch));
        Assert.Null(new QuestBook(Data, 2, [], [], []).FightFor(hatch));
        TownController town = new(Data, granaries.Id) { Book = book };
        town.Click(hatch.At);
        Assert.Equal(hatch, town.Examining);
        // The tails: the rats leave them, so the trail leads back to the hatch's fight.
        Assert.Equal(new TrailGoal(granaries.Id, hatch.At), QuestTrail.Goal(book, rats with { Counts = [1, 0] }));
        // A stake: at the locksmith's bench that shapes it; a loaf: at Rose's stall.
        Workshop bench = Data.Towns["clairval-3-1"].Workshops!.Single();
        Assert.Equal(new TrailGoal("clairval-3-1", bench.At), QuestTrail.Source(Data, "palisade-stake"));
        Assert.Equal(new TrailGoal("clairval-2-1", Data.FindNpc("clairval/rose")!.Value.Npc.At), QuestTrail.Source(Data, "bread"));
    }
}
