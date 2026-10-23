using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The balance with the party (sprint 123, T91, §11.2 of the designer's v7): a fixed sample of each zone, played by the
/// heroes' team AI at the level of the map with the zone's kit, 30 fights going through every party of classes in turn.
/// Only floors are checked, 10 points under the bands: a group easier than its band is accepted (§11.2.2, lever 4).
/// The full measure (100 fights for each party) is T90's and T91's, out of the CI.
/// </summary>
public class PartyBalanceTests
{
    private const int Fights = 30;

    private static readonly string[] Names = ["Aube", "Brume", "Cendre", "Dune"];

    /// <summary>The share of fights won by <paramref name="heroes"/> heroes, in percent.</summary>
    private static int Won(string scenario, int heroes)
    {
        Scenario s = Real.FindScenario(scenario)!;
        int level = s.Fighters.Where(f => f.Team == 1).Max(f => f.Level);
        string family = Real.Groups.TryGetValue(scenario.Replace(MonsterGroup.Prefix, "", StringComparison.Ordinal), out MonsterGroup? g)
            ? g.Family : Real.Dungeons[Dungeons.Parse(scenario)!.Value.Item1].Family;
        IReadOnlyDictionary<Slot, string> worn = Progression.Kit("zone", Real.Families[family].Zone, level, Real);
        string[] classes = [.. Real.Classes.Select(c => c.Id)];
        IEnumerable<string[]> Parties(int size, int first) => size == 0 ? [[]]
            : Enumerable.Range(first, classes.Length - first).SelectMany(i => Parties(size - 1, i).Select(r => (string[])[classes[i], .. r]));
        string[][] parties = [.. Parties(heroes, 0)];
        int won = 0;
        Parallel.For(0, Fights, i =>
        {
            Hero[] party = [.. parties[i % parties.Length].Select((c, k) => Progression.Built(new Hero(Names[k], "female-c", c, Level: level), Real, worn))];
            var fight = new Fight(Real, scenario, 1 + (ulong)i, party[0], null, null, party[1..]);
            Ai.PlayOut(fight);
            if (fight.WinningTeam == 0)
                Interlocked.Increment(ref won);
        });
        return 100 * won / Fights;
    }

    [Theory]
    // One horde of each size band in each zone: 6, then 7 and 8, then 9 and 10 when the zone has one.
    [InlineData("group:woods-0-3.1", 6)]
    [InlineData("group:woods-0-2.1", 7)]
    [InlineData("group:woods-0-4.2", 9)]
    [InlineData("group:mouldy-2-0.0", 6)]
    [InlineData("group:mouldy-0-1.2", 8)]
    [InlineData("group:mouldy-0-1.0", 10)]
    [InlineData("group:lake-0-3.0", 6)]
    [InlineData("group:lake-2-1.1", 8)]
    [InlineData("group:fields-1-2.3", 6)]
    [InlineData("group:fields-4-4.2", 8)]
    // The Forest of the Ancestors' stags (sprint 132) and keepers (sprint 133).
    [InlineData("group:ancestors-3-2.1", 6)]
    [InlineData("group:ancestors-4-2.0", 7)]
    [InlineData("group:ancestors-2-4.0", 10)]
    [InlineData("group:ancestors-3-2.0", 10)]
    public void AHorde_IsAGambleForTwo_AndPassesForThree(string group, int size)
    {
        Assert.Equal(size, Real.FindScenario(group)!.Fighters.Count(f => f.Team == 1));
        // §11.2.2: for two, 50 % at least for 6, 20 % for 7 and 8, nothing asked for 9 and 10; for three, 90, 80 and 60 %.
        (int two, int three) = size switch { 6 => (50, 90), <= 8 => (20, 80), _ => (0, 60) };
        Assert.InRange(Won(group, 2), Math.Max(0, two - 10), 100);
        Assert.InRange(Won(group, 3), three - 10, 100);
    }

    [Theory]
    [InlineData("group:fields-0-4.0")]
    [InlineData("group:mouldy-2-4.0")]
    [InlineData("dungeon:royal-hive:3")]
    [InlineData("dungeon:mole-burrows:3")]
    [InlineData("group:ancestors-0-4.0")]
    [InlineData("group:ancestors-4-4.0")]
    public void AChief_OrADungeonsLastRoom_FallsToThree_AndToFour(string scenario)
    {
        // §6.4 and §11.2.1: 70 % at least for three, 90 % for four.
        Assert.InRange(Won(scenario, 3), 60, 100);
        Assert.InRange(Won(scenario, 4), 80, 100);
    }
}
