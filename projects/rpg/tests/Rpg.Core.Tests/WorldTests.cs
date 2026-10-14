using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The maps of the world and the ways between them (sprint 60; zones of 5 × 5 maps since sprint 70).</summary>
public class WorldTests
{
    [Fact]
    public void EveryMap_IsReachedFromClairval_AndEveryWayHasAWayBack()
    {
        var seen = new HashSet<string> { "clairval" };
        var todo = new Queue<string>(["clairval"]);
        while (todo.Count > 0)
        {
            foreach (ZoneLink l in Real.Towns[todo.Dequeue()].Links ?? [])
            {
                if (seen.Add(l.To))
                    todo.Enqueue(l.To);
            }
        }
        Assert.Equal(Real.Towns.Keys.Order(StringComparer.Ordinal), seen.Order(StringComparer.Ordinal));
        foreach (Town t in Real.Towns.Values)
        {
            foreach (ZoneLink l in t.Links ?? [])
            {
                // The way back starts next to where this way arrives, and arrives next to where it starts.
                Assert.Contains(Real.Towns[l.To].Links!, b => b.To == t.Id && b.At.DistanceTo(l.Arrival) <= 1 && b.Arrival.DistanceTo(l.At) <= 1);
            }
        }
    }

    [Fact]
    public void TheWoods_AreFiveByFiveMaps_Of48By32_WestOfTheVillage_EachOpenOnItsNeighbours()
    {
        Town village = Real.Towns["clairval"];
        Town[] woods = [.. Real.Towns.Values.Where(t => t.Zone == "woods")];
        Assert.Equal(25, woods.Length);
        Assert.Equal((null, new Cell(0, 0)), (village.Zone, village.MapAt));
        Assert.All(woods, t => Assert.Equal((48, 32, 3, "Bois de Clairval", "Clairval Woods"), (t.Width, t.Rows.Count, t.Level, t.Name.Fr, t.Name.En)));
        Assert.Equal([.. Enumerable.Range(-5, 5).SelectMany(x => Enumerable.Range(-2, 5).Select(y => new Cell(x, y)))],
            woods.Select(t => t.MapAt).OrderBy(c => c.X).ThenBy(c => c.Y));
        foreach (Town a in woods)
        {
            foreach (Town b in woods.Where(b => (b.MapAt - a.MapAt) is { X: 1, Y: 0 } or { X: 0, Y: 1 }))
            {
                // Neighbours open on each other by three cells, both ways.
                Assert.Equal(3, a.Links!.Count(l => l.To == b.Id));
                Assert.Equal(3, b.Links!.Count(l => l.To == a.Id));
            }
        }
        // The village's west road opens on the middle of the woods' east side.
        Assert.All(village.Links!, l => Assert.Equal("woods-4-2", l.To));
        Assert.Equal(new Cell(-1, 0), Real.Towns["woods-4-2"].MapAt);
        // A way to another zone is named after it alone (D64: no compass points); inside a zone, no name.
        foreach (Town t in Real.Towns.Values)
        {
            foreach (ZoneLink l in t.Links ?? [])
            {
                Town to = Real.Towns[l.To];
                Assert.Equal(to.ZoneId == t.ZoneId ? null : to.Name, l.Name);
            }
        }
    }

    [Fact]
    public void TheMouldyWoods_AreFiveByFiveMaps_WestOfTheWoods_OpenOnThemByTwoMaps()
    {
        Town[] mouldy = [.. Real.Towns.Values.Where(t => t.Zone == "mouldy")];
        Assert.Equal(25, mouldy.Length);
        Assert.All(mouldy, t => Assert.Equal((48, 32, 10, "Bois moisi", "Mouldy Woods"), (t.Width, t.Rows.Count, t.Level, t.Name.Fr, t.Name.En)));
        Assert.Equal([.. Enumerable.Range(-10, 5).SelectMany(x => Enumerable.Range(-2, 5).Select(y => new Cell(x, y)))],
            mouldy.Select(t => t.MapAt).OrderBy(c => c.X).ThenBy(c => c.Y));
        // Its giant mushrooms, and the woods' ways to it, three cells each, both ways.
        Assert.Contains(mouldy, t => t.Rows.Any(r => r.Contains('M')));
        foreach (int row in new[] { 1, 3 })
        {
            Assert.Equal(3, Real.Towns[$"woods-0-{row}"].Links!.Count(l => l.To == $"mouldy-4-{row}"));
            Assert.Equal(3, Real.Towns[$"mouldy-4-{row}"].Links!.Count(l => l.To == $"woods-0-{row}"));
        }
        // Nowhere else.
        Assert.Equal(6, Real.Towns.Values.Where(t => t.Zone == "woods").Sum(t => t.Links!.Count(l => l.To.StartsWith("mouldy", StringComparison.Ordinal))));
    }

    [Fact]
    public void TheLake_IsFiveByFiveMaps_NorthOfTheWoods_ItsWaterCrossedByPontoons()
    {
        Town[] lake = [.. Real.Towns.Values.Where(t => t.Zone == "lake")];
        Assert.Equal(25, lake.Length);
        Assert.All(lake, t => Assert.Equal((48, 32, 18, "Lac des Joncs", "Reed Lake"), (t.Width, t.Rows.Count, t.Level, t.Name.Fr, t.Name.En)));
        Assert.Equal([.. Enumerable.Range(-5, 5).SelectMany(x => Enumerable.Range(-7, 5).Select(y => new Cell(x, y)))],
            lake.Select(t => t.MapAt).OrderBy(c => c.X).ThenBy(c => c.Y));
        // The middle map: water crossed by pontoons, an islet in its middle; the shores have their bushes.
        Town middle = Real.Towns["lake-2-2"];
        Assert.True(middle.Rows.Sum(r => r.Count(c => c == '~')) > 500);
        Assert.True(middle.Rows.Sum(r => r.Count(c => c == '=')) > 30);
        Assert.Equal('.', middle[middle.Spawn + new Cell(-2, 1)]);
        Assert.Contains(lake, t => t.Rows.Any(r => r.Contains('B')));
        foreach (int col in new[] { 1, 3 })
        {
            Assert.Equal(3, Real.Towns[$"woods-{col}-0"].Links!.Count(l => l.To == $"lake-{col}-4"));
            Assert.Equal(3, Real.Towns[$"lake-{col}-4"].Links!.Count(l => l.To == $"woods-{col}-0"));
        }
    }

    [Fact]
    public void TheFields_AreFiveByFiveMaps_SouthOfTheWoods_InFlowersAndHedges()
    {
        Town[] fields = [.. Real.Towns.Values.Where(t => t.Zone == "fields")];
        Assert.Equal(25, fields.Length);
        Assert.All(fields, t => Assert.Equal((48, 32, 26, "Champs de Clairval", "Clairval Fields"), (t.Width, t.Rows.Count, t.Level, t.Name.Fr, t.Name.En)));
        Assert.Equal([.. Enumerable.Range(-5, 5).SelectMany(x => Enumerable.Range(3, 5).Select(y => new Cell(x, y)))],
            fields.Select(t => t.MapAt).OrderBy(c => c.X).ThenBy(c => c.Y));
        // Flowers one walks on, hedges one does not.
        Town middle = Real.Towns["fields-2-2"];
        Cell flowers = middle.Board().Cells().First(c => middle[c] == ',');
        Assert.True(middle.Board().IsFloor(flowers));
        Assert.All(fields, t => Assert.Equal('B', t[new Cell(0, 0)]));
        foreach (int col in new[] { 1, 3 })
        {
            Assert.Equal(3, Real.Towns[$"woods-{col}-4"].Links!.Count(l => l.To == $"fields-{col}-0"));
            Assert.Equal(3, Real.Towns[$"fields-{col}-0"].Links!.Count(l => l.To == $"woods-{col}-4"));
        }
        // Four zones and the village: 101 maps.
        Assert.Equal(101, Real.Towns.Count);
    }

    // Two maps side by side, three cells wide: a to the west of b.
    private static readonly Town West = new("a", Text, ["...", "..."], new Cell(0, 0), [], [], [new ZoneLink(new Cell(2, 0), "b", new Cell(0, 0), Text)]);
    private static readonly Town East = new("b", Text, ["...", "..."], new Cell(1, 1), [], [], [new ZoneLink(new Cell(0, 1), "a", new Cell(2, 1), Text)], MapAt: new Cell(1, 0));

    private static GameData With(params Town[] towns) => new(Spells, [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])], null, towns, []);

    [Fact]
    public void AWayToNowhere_OrOntoATree_IsRefused()
    {
        Assert.NotNull(With(West, East).Towns["b"]);
        Assert.Throws<InvalidDataException>(() => With(West, East with { Rows = ["T..", "..."] }));
        Assert.Throws<InvalidDataException>(() => With(West));
        Assert.Throws<InvalidDataException>(() => With(West with { Level = 0 }, East));
    }

    [Fact]
    public void AWay_LiesOnTheEdgeFacingItsNeighbour_AndEachMapHasItsOwnPlace()
    {
        // Not on the east edge, though b is east.
        Assert.Throws<InvalidDataException>(() => With(West with { Links = [new ZoneLink(new Cell(1, 0), "b", new Cell(0, 0), Text)] }, East));
        // Arriving in the middle of b, not on its west edge.
        Assert.Throws<InvalidDataException>(() => With(West with { Links = [new ZoneLink(new Cell(2, 0), "b", new Cell(2, 0), Text)] }, East));
        // b two maps away, or on a's own place.
        Assert.Throws<InvalidDataException>(() => With(West, East with { MapAt = new Cell(2, 0) }));
        Assert.Throws<InvalidDataException>(() => With(West, East with { MapAt = new Cell(0, 0) }));
        // A way north: on the top row, arriving on the bottom one.
        Town north = new("n", Text, ["...", "..."], new Cell(1, 0), [], [], [new ZoneLink(new Cell(1, 1), "a", new Cell(1, 0), Text)], MapAt: new Cell(0, -1));
        Assert.NotNull(With(West with { Links = [new ZoneLink(new Cell(1, 0), "n", new Cell(1, 1), Text)] }, north).Towns["n"]);
    }
}
