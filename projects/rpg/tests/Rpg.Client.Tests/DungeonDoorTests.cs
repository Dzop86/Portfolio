using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The door of a dungeon on its map (sprint 80): walked onto, it asks; what it says.</summary>
public class DungeonDoorTests
{
    private static readonly GameData Data = GameData.Embedded;

    [Fact]
    public void WalkingOntoTheDoor_AsksToGoIn_AndStayingOutside_LetsThePlayerWalkOn()
    {
        Dungeon wallow = Data.Dungeons["boar-wallow"];
        var town = new TownController(Data, wallow.Map);
        TownStep step = town.Click(wallow.Door)!;
        Assert.Equal(new DungeonDoor(wallow.Door, wallow.Id), step.Door);
        Assert.Equal(new DungeonDoor(wallow.Door, wallow.Id), town.Door);
        Assert.Equal(wallow.Door, town.Position);
        Assert.Null(town.Fight);
        // Asking, the player does not walk away.
        Assert.Null(town.Click(town.Town.Spawn));
        town.CloseDoor();
        Assert.Null(town.Door);
        Assert.NotNull(town.Click(town.Town.Spawn));
        // Passing over it without stopping there asks nothing.
        Assert.Null(town.Door);
    }

    [Fact]
    public void ACutWalk_DoesNotAskEither()
    {
        Dungeon den = Data.Dungeons["wolf-den"];
        var town = new TownController(Data, den.Map);
        Assert.NotNull(town.Click(den.Door));
        town.Interrupt(town.Town.Spawn);
        Assert.Null(town.Door);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheDoor_SaysWhatOpensIt_OrWhatIsMissing(string lang)
    {
        var texts = new Texts(lang);
        Dungeon wallow = Data.Dungeons["boar-wallow"];
        string name = wallow.Name.In(lang), key = Data.Items[wallow.Key].Name.In(lang);
        (string shut, bool canShut) = texts.DoorText(wallow, [], null);
        Assert.False(canShut);
        Assert.Contains(name, shut);
        Assert.Contains(key, shut);
        (string withKey, bool canKey) = texts.DoorText(wallow, [new ItemCount(wallow.Key, 1)], null);
        Assert.True(canKey);
        Assert.Equal(texts["dungeon.enter", name] + "\n" + texts["dungeon.key-used", key], withKey);
        Assert.Equal((texts["dungeon.enter", name] + "\n" + texts["dungeon.keyring"], true), texts.DoorText(wallow, [new ItemCount(Dungeons.Keyring, 1)], null));
        Assert.Equal((texts["dungeon.resume", name, 2], true), texts.DoorText(wallow, [], new DungeonRun(wallow.Id, 2)));
        // In another dungeon: this door still wants its key.
        Assert.False(texts.DoorText(wallow, [], new DungeonRun("wolf-den", 2)).CanEnter);
        Assert.Equal((texts["dungeon.enter", name], true), texts.DoorText(wallow, null, null));
    }

    [Fact]
    public void ADungeonCleared_SaysSo_FirstOnTheEndScreen()
    {
        var texts = new Texts("fr");
        string result = texts.Result(new FightResult(120, 5000, 20, DungeonDone: true), 20);
        Assert.StartsWith(texts["dungeon.done"] + " · ", result);
        Assert.DoesNotContain(texts["dungeon.done"], texts.Result(new FightResult(60, 5000, 20, Run: new DungeonRun("boar-wallow", 2)), 20));
    }
}
