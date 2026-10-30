using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The world map's photographs (sprint 169, D88): one per map of the world, up to date, fading towards the sea.</summary>
public class MapPhotosTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static string Photos([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "godot", MapPhotos.Folder));

    [Fact]
    public void EachMapOfTheWorld_HasItsPhotograph_ShowingItAsItIsNow()
    {
        string index = File.ReadAllText(Path.Combine(Photos(), MapPhotos.Index));
        // A map changed since its photograph: run the game with --render-maps (see godot/MapPhotographer.cs).
        Assert.Empty(MapPhotos.Stale(Data, index));
        Assert.All(MapPhotos.World(Data), t => Assert.True(File.Exists(Path.Combine(Photos(), MapPhotos.File(t.Id))), t.Id));
        Assert.DoesNotContain(MapPhotos.World(Data), t => t.IsHall);
    }

    [Fact]
    public void AFingerprint_ChangesWithTheMap_AndAStaleIndexSaysWhich()
    {
        Town map = Data.Towns["woods-2-2"];
        Assert.Equal(MapPhotos.Fingerprint(map), MapPhotos.Fingerprint(map with { }));
        Assert.NotEqual(MapPhotos.Fingerprint(map), MapPhotos.Fingerprint(map with { Level = map.Level + 1 }));
        Assert.Equal(MapPhotos.World(Data).Count(), MapPhotos.Stale(Data, null).Count);
        string index = MapPhotos.WriteIndex(Data);
        Assert.Empty(MapPhotos.Stale(Data, index));
        Assert.Equal(["woods-2-2"], MapPhotos.Stale(Data, index.Replace(MapPhotos.Fingerprint(map), "0000000000000000", StringComparison.Ordinal)));
    }

    [Fact]
    public void APhotograph_FadesOnlyTowardsASideWithoutANeighbour()
    {
        // The lake's top row of maps: nothing above.
        Town top = MapPhotos.World(Data).Where(t => t.Zone == "lake").MinBy(t => t.MapAt.Y)!;
        Assert.True(MapPhotos.Open(Data, top).Top);
        Town inside = Data.Towns["woods-2-2"];
        Assert.Equal((false, false, false, false), MapPhotos.Open(Data, inside));
        var open = (Left: true, Top: false, Right: false, Bottom: false);
        Assert.Equal(0, MapPhotos.Alpha(open, 0, 50), 6);
        Assert.Equal(1, MapPhotos.Alpha(open, MapPhotos.Fade, 50), 6);
        Assert.InRange(MapPhotos.Alpha(open, MapPhotos.Fade / 2, 50), 0.3, 0.7);
        // The other sides stay whole.
        Assert.Equal(1, MapPhotos.Alpha(open, 100, 0), 6);
        Assert.Equal(1, MapPhotos.Alpha((false, false, false, false), 0, 0), 6);
    }
}
