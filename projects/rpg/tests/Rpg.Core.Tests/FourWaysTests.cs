using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// Quatre-Chemins (sprint 129, D79, T97): 2 × 2 maps east of Clairval past its east gate, shut until "Four Trails" is
/// done; the roads leaving the village part there, three shut in the world, the Earth's open.
/// </summary>
public class FourWaysTests
{
    private static readonly string[] Maps = ["four-ways", "four-ways-0-0", "four-ways-1-0", "four-ways-1-1"];

    [Fact]
    public void ClairvalsEastGate_LeadsToQuatreChemins_AndWaitsForFourTrails()
    {
        ZoneLink[] gate = [.. Real.Towns["clairval-3-1"].Links!.Where(l => l.To == Shop.Hamlet)];
        Assert.Equal(3, gate.Length);
        Assert.All(gate, l => Assert.Equal("four-trails", l.Quest));
        Assert.False(gate[0].OpenFor(["the-disjoint"]));
        Assert.True(gate[0].OpenFor(["the-disjoint", "four-trails"]));
        // Every other way of the world is open, and the way back from Quatre-Chemins too.
        Assert.All(Real.Towns.Values.SelectMany(t => t.Links ?? []).Where(l => !gate.Contains(l)), l => Assert.Null(l.Quest));
        Assert.Contains(Real.Towns[Shop.Hamlet].Links!, l => l.To == "clairval-3-1" && l.Quest is null);
    }

    [Fact]
    public void BeingOnAMapOfQuatreChemins_NeedsFourTrails_TheRestOfTheWorldNothing()
    {
        Assert.All(Maps, m => Assert.Equal("four-trails", Real.QuestToReach(m)));
        // The Forest of the Ancestors lies past Quatre-Chemins (sprint 131): its maps too.
        Assert.All(Real.Towns.Values.Where(t => !Maps.Contains(t.Id) && t.Zone != "ancestors"), t => Assert.Null(Real.QuestToReach(t.Id)));
    }

    [Fact]
    public void TheFourMaps_AreAVillageOfTwoByTwo_EastOfClairval_AtLevelThirty()
    {
        Town[] here = [.. Maps.Select(m => Real.Towns[m])];
        Assert.Equal([(4, -1), (4, 0), (5, -1), (5, 0)], here.Select(t => (t.MapAt.X, t.MapAt.Y)).Order());
        Assert.All(here, t => Assert.Equal(("four-ways", 30), (t.Zone, t.Level)));
        Assert.Equal((3, 0), (Real.Towns["clairval-3-1"].MapAt.X, Real.Towns["clairval-3-1"].MapAt.Y));
        Assert.All(here, t => Assert.True(Shop.InVillage(t.Id) && Shop.VillageOf(t.Id) == Shop.Hamlet, t.Id));
        // A village: no monsters, the stall open, life back twice as fast, friends told "Quatre-Chemins".
        Assert.All(here, t => Assert.Empty(Real.GroupsOn(t.Id)));
        Assert.Null(Shop.WhyNotBuy(Real.Items[Waystones.Recall], 1, 100, "four-ways-1-1", Real));
        Assert.Equal(2, Life.Factor("four-ways-0-0"));
        Assert.Equal(Shop.Hamlet, Contacts.ZoneOf("four-ways-1-0", Real));
        Assert.Equal(Shop.Village, Contacts.ZoneOf("clairval-3-1", Real));
        // From the square, every map of the village by its own ways.
        var seen = new HashSet<string> { Shop.Hamlet };
        var todo = new Queue<string>([Shop.Hamlet]);
        while (todo.TryDequeue(out string? m))
        {
            foreach (ZoneLink l in Real.Towns[m].Links!.Where(l => Maps.Contains(l.To) && seen.Add(l.To)))
                todo.Enqueue(l.To);
        }
        Assert.Equal(Maps.Order(), seen.Order());
    }

    [Theory]
    [InlineData("four-ways-0-0", "rockfall", 0, -1)]
    [InlineData("four-ways-1-0", "blaze", 1, 0)]
    [InlineData("four-ways", "ford", 0, 1)]
    public void AShutRoad_EndsAtWhatShutsIt_AndLeadsNowhere(string map, string look, int dx, int dy)
    {
        Town t = Real.Towns[map];
        Prop shut = Assert.Single(t.Props!, p => p.Look == look);
        // The thing to examine stands by the road's end, which one reaches; the edge beyond it, nobody.
        HashSet<Cell> reach = Reach(t);
        Assert.Contains(new[] { new Cell(1, 0), new Cell(-1, 0), new Cell(0, 1), new Cell(0, -1) }, d => reach.Contains(shut.At + d));
        Cell[] edge = [.. Enumerable.Range(0, t.Width).SelectMany(x => Enumerable.Range(0, t.Rows.Count).Select(y => new Cell(x, y)))
            .Where(c => t[c] == '=' && (dx == 1 ? c.X == t.Width - 1 : dy == -1 ? c.Y == 0 : c.Y == t.Rows.Count - 1))];
        Assert.NotEmpty(edge);
        Assert.All(edge, c => Assert.DoesNotContain(c, reach));
        Assert.DoesNotContain(t.Links!, l => edge.Contains(l.At));
        Assert.NotEmpty(shut.Name.Fr);
        Assert.NotEmpty(shut.Text.En);
    }

    [Fact]
    public void TheSouthRoad_TheEarths_GoesThroughThePalisade_IntoTheForest()
    {
        // A wall until sprint 131; the forest's three ways since (ForestTests), each reached from the arrival.
        Town t = Real.Towns["four-ways-1-1"];
        HashSet<Cell> reach = Reach(t);
        ZoneLink[] south = [.. t.Links!.Where(l => l.At.Y == t.Rows.Count - 1)];
        Assert.Equal(3, south.Length);
        Assert.All(south, l => Assert.Equal("ancestors-1-0", l.To));
        Assert.All(south, l => Assert.Contains(l.At, reach));
    }

    [Fact]
    public void QuatreChemins_HasAWaystoneOnItsSquare_ForSeventyÉclats()
    {
        (string id, string map, Cell at) = Waystones.All(Real).Single(w => w.Map == Shop.Hamlet);
        Assert.Equal("waystone-four-ways", id);
        Assert.InRange(at.DistanceTo(Real.Towns[map].Spawn), 1, 2);
        Assert.Equal(70, Waystones.Cost(id, Real));
    }

    [Fact]
    public void AWayWaitingForAnUnknownQuest_IsRefused()
    {
        Town gate = Real.Towns["clairval-3-1"];
        Town wrong = gate with { Links = [.. gate.Links!.Select(l => l.Quest is null ? l : l with { Quest = "no-such-quest" })] };
        var e = Assert.Throws<InvalidDataException>(() => new GameData(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes,
            Real.Towns.Values.Select(t => t.Id == gate.Id ? wrong : t), Real.Dialogues.Values, Real.Summons.Values, Real.Quests.Values, Real.Items.Values,
            Real.Sets.Values, Real.Families.Values, Real.Dungeons.Values, Real.Jobs.Values, Real.Passives.Values));
        Assert.Contains("no-such-quest", e.Message);
    }

    private static HashSet<Cell> Reach(Town t)
    {
        Board board = t.Board();
        var seen = new HashSet<Cell> { t.Spawn };
        var todo = new Queue<Cell>([t.Spawn]);
        while (todo.TryDequeue(out Cell c))
        {
            foreach (Cell n in new[] { c + new Cell(1, 0), c + new Cell(-1, 0), c + new Cell(0, 1), c + new Cell(0, -1) })
            {
                if (board.Contains(n) && t.CanStand(n) && seen.Add(n))
                    todo.Enqueue(n);
            }
        }
        return seen;
    }
}
