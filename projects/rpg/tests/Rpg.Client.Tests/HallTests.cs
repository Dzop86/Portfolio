using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>A dungeon's halls (sprint 86): its group guards the far door, the fountain heals, the way out leads under the keep.</summary>
public class HallTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly Dungeon Wallow = Data.Dungeons["boar-wallow"];

    private static Town Hall(int room) => Data.Towns[Wallow.Halls![room - 1].Map];

    private static ZoneLink FarDoor(Town hall) => hall.Links!.First(l => Data.Towns[l.To].IsHall);

    [Fact]
    public void AtItsRoom_TheGroupStands_AndTheFarDoorStaysShut_UntilTheRoomIsBehind()
    {
        Town hall = Hall(1);
        var here = new TownController(Data, hall.Id, null, null, new DungeonRun(Wallow.Id, 1));
        Assert.Equal(Wallow.Halls![0].Group, here.Groups[Wallow.GroupOf(1)]);
        Assert.All(here.ShutWays, l => Assert.Equal(Hall(2).Id, l.To));
        Assert.Equal(3, here.ShutWays.Count);
        // Walked onto, it says why and leads nowhere; the player may walk on once told.
        ZoneLink far = FarDoor(hall);
        TownStep step = here.Click(far.At)!;
        Assert.Equal((far, (ZoneLink?)null, (ZoneLink?)null), (here.Shut!, here.Travel, step.Link));
        Assert.Null(here.Click(hall.Spawn));
        here.CloseDoor();
        Assert.NotNull(here.Click(hall.Spawn));
        // The room behind: no group, the far door leads to the next hall, in the same run.
        var behind = new TownController(Data, hall.Id, null, null, new DungeonRun(Wallow.Id, 2, 30));
        Assert.Empty(behind.Groups);
        Assert.Empty(behind.ShutWays);
        Assert.NotNull(behind.Click(far.At));
        Assert.Equal(far, behind.Travel);
        TownController next = behind.Enter(behind.Travel!);
        Assert.Equal((Hall(2).Id, new DungeonRun(Wallow.Id, 2, 30)), (next.Town.Id, next.Run));
        Assert.True(next.Groups.ContainsKey(Wallow.GroupOf(2)));
    }

    [Fact]
    public void TheRoomsGroup_IsFoughtByARightClick_ItsFightTheRoomsOwn()
    {
        var here = new TownController(Data, Hall(2).Id, null, null, new DungeonRun(Wallow.Id, 2));
        Cell guard = here.Groups[Wallow.GroupOf(2)];
        Assert.NotNull(here.Click(guard));
        Assert.Null(here.Fight);
        here.Interrupt(here.Position);
        Assert.NotNull(here.Attack(guard));
        Assert.Equal(Wallow.Scenario(2), here.Fight);
    }

    [Fact]
    public void AClickOnTheFountain_WalksUpToIt_ToHeal_InAHallOnly()
    {
        Town hall = Hall(3);
        var here = new TownController(Data, hall.Id, null, null, new DungeonRun(Wallow.Id, 3, 12));
        Cell fountain = hall.Rows.Select((r, y) => new Cell(r.IndexOf('f', StringComparison.Ordinal), y)).First(c => c.X >= 0);
        Assert.True(here.IsSpring(fountain));
        TownStep step = here.Click(fountain)!;
        Assert.True(here.Spring);
        Assert.Equal(1, here.Position.DistanceTo(fountain));
        Assert.NotEmpty(step.Path);
        // Being healed, nothing else; then free again.
        Assert.Null(here.Click(hall.Spawn));
        here.CloseDoor();
        Assert.False(here.Spring);
        // The last hall has no far door: only the way out, under the keep.
        Assert.Empty(here.ShutWays);
        Assert.All(hall.Links!, l => Assert.Equal((Wallow.Map, Wallow.Door + new Cell(0, 1)), (l.To, l.Arrival)));
        // The village's fountain heals nobody.
        var village = new TownController(Data, "clairval");
        Cell square = village.Town.Rows.Select((r, y) => new Cell(r.IndexOf('F', StringComparison.Ordinal), y)).First(c => c.X >= 0);
        Assert.False(village.IsSpring(square));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheHallsWords_AreInBothLanguages(string lang)
    {
        var texts = new Texts(lang);
        Assert.Contains("25", texts["hall.hp", 25, 80], StringComparison.Ordinal);
        Assert.Contains("80", texts["hall.rested", 80], StringComparison.Ordinal);
        Assert.NotEmpty(texts["hall.shut"]);
    }
}
