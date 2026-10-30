using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The monsters' portraits (sprint 172, D88): one per monster and its silhouette, up to date, the silhouette without colour.</summary>
public class MonsterPortraitsTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static string Portraits([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "godot", MonsterPortraits.Folder));

    [Fact]
    public void EachMonster_HasItsPortraitAndItsSilhouette_ShowingItsLookAsItIsNow()
    {
        string index = File.ReadAllText(Path.Combine(Portraits(), MonsterPortraits.Index));
        // A look changed since its portrait: run the game with --render-portraits (see godot/MonsterPhotographer.cs).
        Assert.Empty(MonsterPortraits.Stale(Data, index));
        Assert.Equal(Data.Monsters.Count, MonsterPortraits.All(Data).Count());
        Assert.All(MonsterPortraits.All(Data), m =>
        {
            Assert.True(File.Exists(Path.Combine(Portraits(), MonsterPortraits.File(m.Id))), m.Id);
            Assert.True(File.Exists(Path.Combine(Portraits(), MonsterPortraits.Shadow(m.Id))), m.Id);
        });
    }

    [Fact]
    public void AFingerprint_ChangesWithTheLook_AndAStaleIndexSaysWhich()
    {
        MonsterSpec wolf = Data.Monsters.Values.First(m => m.Look == "wolf-melee");
        Assert.Equal(MonsterPortraits.Fingerprint(wolf), MonsterPortraits.Fingerprint(wolf with { Hp = wolf.Hp + 1 }));
        Assert.NotEqual(MonsterPortraits.Fingerprint(wolf), MonsterPortraits.Fingerprint(wolf with { Look = "wolf-boss" }));
        Assert.Equal(Data.Monsters.Count, MonsterPortraits.Stale(Data, null).Count);
        string index = MonsterPortraits.WriteIndex(Data);
        Assert.Empty(MonsterPortraits.Stale(Data, index));
        string changed = index.Replace($"\"{wolf.Id}\": \"{MonsterPortraits.Fingerprint(wolf)}\"", $"\"{wolf.Id}\": \"0000000000000000\"", StringComparison.Ordinal);
        Assert.Equal([wolf.Id], MonsterPortraits.Stale(Data, changed));
    }

    [Fact]
    public void TheBestiary_ShowsTheSilhouette_UntilTheMonsterIsMet()
    {
        Assert.Equal("assets/bestiary/portraits/grey-wolf-unknown.webp", MonsterPortraits.Picture("grey-wolf", Knowledge.Unknown));
        Assert.Equal("assets/bestiary/portraits/grey-wolf.webp", MonsterPortraits.Picture("grey-wolf", Knowledge.Met));
        Assert.Equal("assets/bestiary/portraits/grey-wolf.webp", MonsterPortraits.Picture("grey-wolf", Knowledge.Mastered));
    }

    [Fact]
    public void ASilhouette_KeepsTheShape_InBlack_EdgedWithLight_WithoutAnyColour()
    {
        // A red square of 4 × 4 in the middle of an empty picture of 10 × 10.
        const int side = 10;
        var picture = new byte[side * side * 4];
        for (int y = 3; y < 7; y++)
            for (int x = 3; x < 7; x++)
                (picture[(y * side + x) * 4], picture[(y * side + x) * 4 + 3]) = (230, 255);
        byte[] shadow = MonsterPortraits.Silhouette(picture, side, side);
        (byte, byte, byte, byte) At(int x, int y) => (shadow[(y * side + x) * 4], shadow[(y * side + x) * 4 + 1], shadow[(y * side + x) * 4 + 2], shadow[(y * side + x) * 4 + 3]);
        // The shape in black, whole.
        Assert.Equal((0x0d, 0x0d, 0x0d, 255), At(4, 4));
        // Its edge of light, two pixels around it (one beside, two straight above), and nothing farther.
        Assert.Equal((0x3a, 0x3a, 0x3d, 255), At(2, 4));
        Assert.Equal((0x3a, 0x3a, 0x3d, 255), At(4, 1));
        Assert.Equal(0, At(0, 4).Item4);
        Assert.Equal(0, At(0, 0).Item4);
        // No colour anywhere: each pixel is grey (its channels within the edge's tint).
        for (int i = 0; i < shadow.Length; i += 4)
            Assert.InRange(Math.Max(shadow[i], Math.Max(shadow[i + 1], shadow[i + 2])) - Math.Min(shadow[i], Math.Min(shadow[i + 1], shadow[i + 2])), 0, 3);
        Assert.Throws<ArgumentException>(() => MonsterPortraits.Silhouette(picture, side, side + 1));
    }

    [Fact]
    public void ATurningPortrait_LeavesRoomForTheModel_WhicheverWayItTurns()
    {
        // A wolf's box seen from above: long along x, narrow along z, its head farther from the axis than its tail.
        (double minX, double maxX, double minZ, double maxZ) = (-0.6, 0.9, -0.2, 0.25);
        double radius = MonsterPortraits.TurningRadius(minX, maxX, minZ, maxZ);
        for (int degrees = 0; degrees < 360; degrees += 5)
        {
            double a = degrees * Math.PI / 180;
            foreach (double x in new[] { minX, maxX })
                foreach (double z in new[] { minZ, maxZ })
                {
                    (double tx, double tz) = (x * Math.Cos(a) + z * Math.Sin(a), -x * Math.Sin(a) + z * Math.Cos(a));
                    Assert.True(Math.Sqrt(tx * tx + tz * tz) <= radius + 1e-9, $"{degrees}°");
                }
        }
        // And no more: the farthest corner touches it.
        Assert.Equal(Math.Sqrt(0.9 * 0.9 + 0.25 * 0.25), radius, 9);
    }
}
