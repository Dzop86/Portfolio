using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>Riding (sprint 179): the mount worn carries the hero, 30 % faster.</summary>
public class MountsTests
{
    private static readonly GameData Data = GameData.Embedded;

    [Fact]
    public void AHeroWithAMount_Rides_ThirtyPercentFaster()
    {
        var rider = new Hero("Aubépine", "female-b", "warrior", Level: 20, Worn: new Dictionary<Slot, string> { [Slot.Mount] = "donkey" });
        Assert.Equal("donkey", Mounts.Ridden(rider));
        Assert.Equal(5.544 * 1.3, Mounts.Speed(rider, 5.544), 6);
    }

    [Fact]
    public void AHeroOnFoot_WalksAtItsOwnPace()
    {
        var walker = new Hero("Aubépine", "female-b", "warrior", Level: 20, Worn: new Dictionary<Slot, string> { [Slot.Pet] = "kitten" });
        Assert.Null(Mounts.Ridden(walker));
        Assert.Null(Mounts.Ridden(new Hero("Aubépine", "female-b")));
        Assert.Null(Mounts.Ridden(null));
        Assert.Equal(5.544, Mounts.Speed(walker, 5.544));
    }

    [Fact]
    public void EveryMountOfTheData_GoesInTheMountSlot_AndCanBeWorn()
    {
        Item[] mounts = [.. Data.Items.Values.Where(i => i.Slots.Contains(Slot.Mount))];
        Assert.NotEmpty(mounts);
        foreach (Item mount in mounts)
        {
            var worn = new Dictionary<Slot, string> { [Slot.Mount] = mount.Id };
            Assert.Null(Equipment.Problem(worn, mount.Level, Data));
            Assert.Equal(mount.Id, Mounts.Ridden(new Hero("", "female-b", Level: mount.Level, Worn: worn)));
        }
    }
}
