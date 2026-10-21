using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The groups of monsters wander the same for every player (sprint 118, D74): drawn from the clock's slot, near their
/// homes, on free cells, never two on one, never cutting a map in two.
/// </summary>
public class RoamTests
{
    [Fact]
    public void ASlot_IsAWholePeriodSince1970()
    {
        var t = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(t.ToUnixTimeMilliseconds() / 8000, Roam.Slot(t));
        Assert.Equal(Roam.Slot(t), Roam.Slot(t + Roam.Period - TimeSpan.FromMilliseconds(1) - TimeSpan.FromMilliseconds(t.ToUnixTimeMilliseconds() % 8000)));
        Assert.Equal(Roam.Slot(t) + 1, Roam.Slot(t + Roam.Period));
    }

    [Fact]
    public void EveryMapsGroups_StandNearTheirHomes_OnFreeCells_TheSameOnEveryComputer()
    {
        foreach (Town map in Real.Towns.Values.Where(t => Real.GroupsOn(t.Id).Count > 0))
        {
            Board board = map.Board();
            foreach (long slot in new long[] { 0, 1, 7, 223_456_789 })
            {
                IReadOnlyDictionary<string, Cell> places = Roam.Places(Real, map, board, slot);
                Assert.Equal(Real.GroupsOn(map.Id).Select(g => g.Id).Order(StringComparer.Ordinal), places.Keys.Order(StringComparer.Ordinal));
                Assert.Equal(places.Count, places.Values.Distinct().Count());
                foreach ((string id, Cell at) in places)
                {
                    Assert.True(at.DistanceTo(Real.Groups[id].Home) <= Roam.Radius, $"{id} at {at}");
                    Assert.True(at == Real.Groups[id].Home || (map.IsFree(board, at) && map.LinkAt(at) is null && map.ExitAt(at) is null), $"{id} at {at}");
                }
                Assert.True(MonsterGroups.KeepsWaysOpen(map, board, [.. places.Values]), map.Id);
                // Computed again (another player's game): the same.
                Assert.Equal(places, Roam.Places(Real, map, board, slot));
            }
        }
    }

    [Fact]
    public void TheGroups_Move_FromOneSlotToTheNext()
    {
        MonsterGroup g = Real.Groups.Values.First(x => Real.Towns[x.Map].Dungeon is null);
        Town map = Real.Towns[g.Map];
        Board board = map.Board();
        Assert.True(Enumerable.Range(0, 20).Select(s => Roam.Places(Real, map, board, s)[g.Id]).Distinct().Count() > 3);
    }
}
