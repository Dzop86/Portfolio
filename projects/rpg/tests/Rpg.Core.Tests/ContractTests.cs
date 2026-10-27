using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The notice board's contracts (sprint 153, D83): the same board for a character on a day, always something it can do,
/// one in three at most for a group, three more a day up to nine, the changes and their price, what a fight counts.
/// </summary>
public class ContractTests
{
    private static readonly Contracts.Reach Village = new(["clairval", "woods"], new Dictionary<string, int>());

    private static Contracts.Reach Everywhere(int job) =>
        new(Contracts.Zones(Real, [.. Real.Quests.Keys]), Real.Jobs.Keys.ToDictionary(j => j, _ => job));

    [Fact]
    public void ACharacter_GetsTheSameBoard_OnTheSameDay_AndAnotherTheNext()
    {
        Guid aube = Guid.NewGuid();
        Assert.Equal(Contracts.Draw(Real, aube, 100, 0, 30, Everywhere(30)), Contracts.Draw(Real, aube, 100, 0, 30, Everywhere(30)));
        Assert.NotEqual(Contracts.Draw(Real, aube, 100, 0, 30, Everywhere(30)), Contracts.Draw(Real, aube, 101, 0, 30, Everywhere(30)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(40)]
    [InlineData(45)]
    public void AThousandBoards_HoldOnlyContractsTheCharacterCanDo(int level)
    {
        // The game designer's criterion: 100 % feasible in the zones open to the character, one in three for a group at most.
        Contracts.Reach reach = Everywhere(level);
        string[] families = [.. Contracts.Families(Real, level, reach.Zones)];
        Assert.NotEmpty(families);
        for (int i = 0; i < 1000; i++)
        {
            IReadOnlyList<Contract> board = Contracts.Draw(Real, new Guid(i, 0, 0, new byte[8]), i, 0, level, reach);
            Assert.Equal(Contracts.PerDay, board.Count);
            Assert.True(board.Count(c => c.IsGroup) <= 1);
            foreach (Contract c in board)
            {
                Assert.True(c.Kind switch
                {
                    ContractKind.Hunt => families.Contains(c.Target),
                    ContractKind.Horde => families.Contains(c.Target) && Contracts.Hordes(Real, c.Target).Any(),
                    ContractKind.Dungeon or ContractKind.Boss => families.Contains(Real.Dungeons[c.Target].Family),
                    ContractKind.Gather => Real.Jobs.Values.Any(j => j.Gathers && j.Harvests!.Any(h => h.Item == c.Target && h.Level <= level)),
                    _ => Real.Jobs.Values.Any(j => j.Recipes.Any(r => r.Item == c.Target && r.Level <= level)),
                }, $"{c} at level {level}");
                Assert.Equal(level, c.Level);
            }
        }
    }

    [Fact]
    public void TheFamilies_AreThoseOfTheLevel_TenBelowAtMost_InTheZonesReached()
    {
        Assert.Equal(["boar", "wolf"], Contracts.Families(Real, 5, Village.Zones));
        // The Forest beyond the east gate: none of its families before "Four Trails".
        Assert.DoesNotContain("stag", Contracts.Families(Real, 40, Contracts.Zones(Real, [])));
        Assert.Contains("stag", Contracts.Families(Real, 40, Everywhere(40).Zones));
        Assert.DoesNotContain("boar", Contracts.Families(Real, 30, Everywhere(30).Zones));
    }

    [Fact]
    public void TheBoard_GetsThreeADay_UpToNine_AndItsChangesBackToNothing()
    {
        Guid aube = Guid.NewGuid();
        ContractBoard first = Contracts.Renew(ContractBoard.Empty, 10, aube, 20, Village, Real);
        Assert.Equal((10, 3), (first.Day, first.Offered.Count));
        Assert.Same(first, Contracts.Renew(first, 10, aube, 20, Village, Real));
        Assert.Equal(6, Contracts.Renew(first, 11, aube, 20, Village, Real).Offered.Count);
        // Three days away lose nothing, a week no more than nine.
        Assert.Equal(Contracts.Most, Contracts.Renew(first, 13, aube, 20, Village, Real).Offered.Count);
        Assert.Equal(Contracts.Most, Contracts.Renew(first, 30, aube, 20, Village, Real).Offered.Count);
        Assert.Equal(0, Contracts.Renew(first with { Changes = 2 }, 11, aube, 20, Village, Real).Changes);
    }

    [Fact]
    public void AChange_IsFreeOnceADay_ThenFiveTimesTheLevel_AGroupsAlwaysFree()
    {
        Guid aube = Guid.NewGuid();
        var hunt = new Contract(ContractKind.Hunt, "boar", 6, 20);
        var horde = new Contract(ContractKind.Horde, "boar", 1, 20);
        var board = new ContractBoard(10, 0, [hunt, horde]);
        Assert.Equal(0, Contracts.ChangePrice(board, hunt, 20));
        ContractBoard changed = Contracts.Change(board, 0, aube, 20, Village, Real);
        Assert.Equal((1, 1), (changed.Changes, changed.Swaps));
        Assert.False(changed.Offered[0].IsGroup);
        Assert.Equal(100, Contracts.ChangePrice(changed, changed.Offered[0], 20));
        Assert.Equal(0, Contracts.ChangePrice(changed, horde, 20));
        // A group's change is not counted among the day's, and draws anew all the same.
        ContractBoard again = Contracts.Change(changed, 1, aube, 20, Village, Real);
        Assert.Equal((1, 2), (again.Changes, again.Swaps));
    }

    [Fact]
    public void AHunt_CountsItsFamily_AGatheringTheItemsOwned_TheRewardsByLevel()
    {
        MonsterGroup boars = Real.Groups.Values.First(g => g.Family == "boar" && g.Fight is null && g.Monsters.Count == 2);
        var fight = new Fight(Real, boars.Scenario, 1, Progression.Built(new Hero("Ronce", "female-c", "guard", Level: 60), Real));
        Ai.PlayOut(fight);
        Contract hunt = new(ContractKind.Hunt, "boar", 6, 30);
        hunt = Contracts.AfterFight(hunt, Real, fight, null);
        Assert.Equal(2, Contracts.Reached(hunt, []));
        Assert.Equal(0, Contracts.AfterFight(new Contract(ContractKind.Hunt, "wolf", 6, 30), Real, fight, null).Done);
        // Not a horde: two boars.
        Assert.Equal(0, Contracts.AfterFight(new Contract(ContractKind.Horde, "boar", 1, 30), Real, fight, null).Done);
        Contract gather = new(ContractKind.Gather, "oak-log", 10, 30);
        Assert.False(Contracts.IsComplete(gather, [new ItemCount("oak-log", 9)]));
        Assert.True(Contracts.IsComplete(gather, [new ItemCount("oak-log", 4), new ItemCount("oak-log", 6)]));
        Assert.Equal([new ItemCount("oak-log", 10)], gather.Taken);
        // A quarter of a level and 5 × level Éclats at 30 (750 and 150); twice for a group.
        Assert.Equal((750, 150), (hunt.Xp, hunt.Shards));
        Assert.Equal((1500, 300), (new Contract(ContractKind.Boss, "wolf-den", 1, 30).Xp, new Contract(ContractKind.Boss, "wolf-den", 1, 30).Shards));
    }

    [Fact]
    public void ADungeon_CountsFromItsSecondRoom_AChief_ItsLastOnly()
    {
        var fight = new Fight(Real, Real.Groups.Values.First(g => g.Family == "boar" && g.Fight is null).Scenario, 1, Progression.Built(new Hero("Ronce", "female-c", "guard", Level: 60), Real));
        Ai.PlayOut(fight);
        Contract scout = new(ContractKind.Dungeon, "boar-wallow", 1, 10), boss = new(ContractKind.Boss, "boar-wallow", 1, 10);
        Assert.Equal(0, Contracts.AfterFight(scout, Real, fight, ("boar-wallow", 1)).Done);
        Assert.Equal(1, Contracts.AfterFight(scout, Real, fight, ("boar-wallow", 2)).Done);
        Assert.Equal(0, Contracts.AfterFight(boss, Real, fight, ("boar-wallow", 2)).Done);
        Assert.Equal(1, Contracts.AfterFight(boss, Real, fight, ("boar-wallow", Dungeons.RoomCount)).Done);
        Assert.Equal(0, Contracts.AfterFight(scout, Real, fight, ("wolf-den", 2)).Done);
    }

    [Fact]
    public void TheBoard_IsWrittenAndReadBack_AndNothingReadIsAnEmptyBoard()
    {
        var board = new ContractBoard(7, 1, [new Contract(ContractKind.Craft, "copper-ring", 1, 12, 0), new Contract(ContractKind.Horde, "wolf", 1, 12, 1)], 3);
        ContractBoard back = Contracts.Read(Contracts.Write(board));
        Assert.Equal((board.Day, board.Changes, board.Swaps), (back.Day, back.Changes, back.Swaps));
        Assert.Equal(board.Offered, back.Offered);
        Assert.Same(ContractBoard.Empty, Contracts.Read(""));
        Assert.Same(ContractBoard.Empty, Contracts.Read("{not json"));
    }

    [Fact]
    public void TheDay_TurnsAtDawn()
    {
        var before = new DateTimeOffset(2026, 10, 27, 3, 59, 0, TimeSpan.Zero);
        Assert.Equal(Contracts.Day(before) + 1, Contracts.Day(before.AddMinutes(1)));
        Assert.Equal(Contracts.Day(before.AddMinutes(1)), Contracts.Day(before.AddHours(23)));
    }
}
