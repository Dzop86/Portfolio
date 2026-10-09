using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>Going from zone to zone, the world tour and the camera (sprint 60).</summary>
public class WorldTests
{
    [Fact]
    public void WalkingOntoAWay_LeadsToTheNextZone_OnItsArrival()
    {
        var town = new TownController(GameData.Embedded, "clairval");
        TownStep step = town.Click(new Cell(0, 5))!;
        Assert.Equal("clairval-woods", step.Link!.To);
        Assert.Equal(step.Link, town.Travel);
        // Nothing else to do there: the zone changes.
        Assert.Null(town.Click(new Cell(1, 6)));
        TownController woods = town.Enter(town.Travel!);
        Assert.Equal(("clairval-woods", new Cell(28, 11), (ZoneLink?)null), (woods.Town.Id, woods.Position, woods.Travel));
        // Walking past a way does not take it: only stopping on it.
        Assert.Null(woods.Click(new Cell(26, 11))!.Link);
        Assert.Null(woods.Travel);
    }

    [Fact]
    public void TheWorldTour_EntersEveryZone()
    {
        var steps = 0;
        IReadOnlyList<string> zones = TownTour.World(new TownController(GameData.Embedded, "clairval"), (_, s) =>
        {
            Assert.NotNull(s.Link);
            steps++;
        });
        Assert.Equal(["clairval", "clairval-woods", "misty-heath"], zones);
        Assert.Equal(2, steps);
    }

    [Fact]
    public void TheCamera_Follows_Zooms_AndTurnsAQuarterAtATime()
    {
        var cam = new ExploreCamera(0, 0);
        cam.Follow(10, 4, 0.05);
        Assert.Equal((3.0, 1.2), (Math.Round(cam.X, 6), Math.Round(cam.Z, 6)));
        cam.Follow(10, 4, 5);
        Assert.Equal((10.0, 4.0), (cam.X, cam.Z));
        cam.ZoomBy(1);
        Assert.Equal(13.5 * 0.85, cam.Zoom, 6);
        cam.ZoomBy(50);
        Assert.Equal(ExploreCamera.MinZoom, cam.Zoom);
        cam.ZoomBy(-50);
        Assert.Equal(ExploreCamera.MaxZoom, cam.Zoom);
        cam.Turn(1);
        Assert.Equal(135, cam.Yaw);
        cam.Turn(-2);
        Assert.Equal((3, 315.0), (cam.Quarter, cam.Yaw));
        cam.Jump(2, 2);
        Assert.Equal((2.0, 2.0), (cam.X, cam.Z));
    }
}
