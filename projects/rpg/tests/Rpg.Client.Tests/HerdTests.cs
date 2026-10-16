using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The village's beasts wandering (sprint 104, D75): each in its pasture, never on a road.</summary>
public class HerdTests
{
    private static readonly GameData Data = GameData.Embedded;

    [Fact]
    public void TheHorses_StayInTheirPaddock_AndTheSheepInTheirMeadow()
    {
        foreach (string map in new[] { "clairval-0-2", "clairval-3-2" })
        {
            Town town = Data.Towns[map];
            var herd = new Herd(town);
            Assert.NotEmpty(herd.Animals);
            // Fenced: the player cannot walk in, the beasts cannot walk out.
            HashSet<Cell> walked = [.. Pathfinding.Reachable(town.Board(), town.Spawn, 48 * 32, c => town.IsFree(town.Board(), c)).Keys];
            var rng = new Rng(7);
            for (int i = 0; i < herd.Animals.Count; i++)
            {
                Assert.DoesNotContain(herd.At(i), walked);
                Assert.True(herd.Pasture(i).Count >= 4, $"{map} {i}");
                for (int k = 0; k < 40; k++)
                {
                    IReadOnlyList<Cell>? path = herd.Roam(i, rng);
                    Assert.All(path ?? [], c => Assert.Contains(c, herd.Pasture(i)));
                    Assert.All(path ?? [], c => Assert.DoesNotContain(c, walked));
                }
            }
        }
    }

    [Fact]
    public void AHenWanders_OffTheRoads_NearWhereItStands_AndTheSameDrawsWalkTheSameWay()
    {
        Town town = Data.Towns["clairval-1-2"];
        var herd = new Herd(town);
        // Pétronille, out of the yard by Léonie (the hen the player can walk up to).
        HashSet<Cell> walked = [.. Pathfinding.Reachable(town.Board(), town.Spawn, 48 * 32, c => town.IsFree(town.Board(), c)).Keys];
        int loose = Enumerable.Range(0, herd.Animals.Count).Single(i => herd.Animals[i].Kind == "hen" && walked.Contains(herd.At(i)));
        Assert.All(herd.Pasture(loose), c => Assert.True(town[c] != '=' && c.DistanceTo(herd.Animals[loose].At) <= Herd.Reach && town.NpcAt(c) is null));
        var again = new Herd(town);
        var a = new Rng(3);
        var b = new Rng(3);
        bool moved = false;
        for (int k = 0; k < 20; k++)
        {
            IReadOnlyList<Cell>? first = herd.Roam(loose, a), second = again.Roam(loose, b);
            Assert.Equal(first ?? [], second ?? []);
            Assert.True((first?.Count ?? 0) <= Herd.Stroll * 2);
            moved |= first is { Count: > 0 };
        }
        Assert.True(moved);
        Assert.Equal(herd.At(loose), again.At(loose));
        // A map without beasts has none to move.
        Assert.Empty(new Herd(Data.Towns["clairval"]).Animals);
    }
}
