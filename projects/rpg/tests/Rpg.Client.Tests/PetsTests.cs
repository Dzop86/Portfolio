using System.Numerics;
using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The pet that follows its master, on foot or riding (sprint 181, D92).</summary>
public class PetsTests
{
    [Fact]
    public void ThePetWorn_IsTheOneInItsSlot()
    {
        Assert.Equal("kitten", Pets.Worn(new Hero("Élise", "female-c", "warrior", Worn: new Dictionary<Slot, string> { [Slot.Pet] = "kitten", [Slot.Mount] = "donkey" })));
        Assert.Null(Pets.Worn(new Hero("Élise", "female-c", "warrior")));
        Assert.Null(Pets.Worn(null));
    }

    [Fact]
    public void ItsSpot_IsAtTheHerosSide_ALittleBehind_TurningWithIt()
    {
        // Facing +z: the right hand is −x.
        Vector2 spot = Pets.Spot(new Vector2(10, 10), 0);
        Assert.Equal(10 - Pets.Side, spot.X, 3);
        Assert.Equal(10 - Pets.Behind, spot.Y, 3);
        // Turned a quarter (facing +x), the spot turns too, as far from the hero.
        Vector2 turned = Pets.Spot(new Vector2(10, 10), MathF.PI / 2);
        Assert.Equal(10 - Pets.Behind, turned.X, 3);
        Assert.Equal(10 + Pets.Side, turned.Y, 3);
        Assert.Equal(Vector2.Distance(spot, new Vector2(10, 10)), Vector2.Distance(turned, new Vector2(10, 10)), 3);
    }

    [Fact]
    public void ItRuns_FasterThanItsMaster_WhenItLags_AndStandsStillOnceThere()
    {
        Vector2 hero = new(10, 10), spot = Pets.Spot(hero, 0);
        // Three cells behind its spot: it runs, half as fast again as its master.
        Vector2 pet = spot - new Vector2(0, 3);
        (Vector2 at, bool running) = Pets.Follow(pet, hero, 0, 4, 0.1f);
        Assert.True(running);
        Assert.Equal(4 * 1.5f * 0.1f, Vector2.Distance(pet, at), 3);
        // Close by, a little faster than its master, never past its spot.
        (at, running) = Pets.Follow(spot - new Vector2(0, 0.3f), hero, 0, 4, 1);
        Assert.True(running);
        Assert.Equal(spot, at);
        // There: it stands.
        (at, running) = Pets.Follow(spot, hero, 0, 4, 0.1f);
        Assert.False(running);
        Assert.Equal(spot, at);
        // Its master standing still, it still comes to its side.
        Assert.True(Pets.Follow(spot - new Vector2(1, 0), hero, 0, 0, 0.1f).Running);
    }

    [Fact]
    public void FarAway_ItAppearsAtItsSpot_OnAMountToo()
    {
        Vector2 hero = new(30, 12);
        (Vector2 at, bool running) = Pets.Follow(new Vector2(2, 2), hero, 1, 4 * (1 + (float)Mounts.SpeedBonus), 0.016f);
        Assert.False(running);
        Assert.Equal(Pets.Spot(hero, 1), at);
        // Riding, it keeps up: in a second at the mount's pace it closes a lag of five cells and is at its spot.
        Vector2 pet = Pets.Spot(hero, 0) - new Vector2(0, 5);
        Vector2 now = pet;
        for (int i = 0; i < 60; i++)
            now = Pets.Follow(now, hero, 0, 4 * (1 + (float)Mounts.SpeedBonus), 1 / 60f).At;
        Assert.True(Vector2.Distance(now, Pets.Spot(hero, 0)) <= Pets.Settled);
    }
}
