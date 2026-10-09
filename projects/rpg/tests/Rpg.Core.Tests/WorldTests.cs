using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The zones of the world and the ways between them (sprint 60).</summary>
public class WorldTests
{
    [Fact]
    public void EveryZone_IsReachedFromClairval_AndEveryWayHasAWayBack()
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
                // The way back leads next to where this way starts.
                ZoneLink back = Assert.Single(Real.Towns[l.To].Links!, b => b.To == t.Id);
                Assert.True(back.Arrival.DistanceTo(l.At) <= 1, $"{t.Id} → {l.To}");
                Assert.True(l.Arrival.DistanceTo(back.At) <= 1, $"{l.To} → {t.Id}");
            }
        }
    }

    [Fact]
    public void TheZones_AreLargeOnes_OfRisingLevels_InTheirOwnPlaceOnTheMap()
    {
        Assert.Equal(["clairval", "clairval-woods", "misty-heath"], Real.Towns.Keys.Order(StringComparer.Ordinal));
        Assert.Equal((1, 3, 10), (Real.Towns["clairval"].Level, Real.Towns["clairval-woods"].Level, Real.Towns["misty-heath"].Level));
        Assert.All(Real.Towns.Values.Where(t => t.Id != "clairval"), t => Assert.True(t.Rows.Count * t.Rows[0].Length >= 600, t.Id));
        Assert.Equal(Real.Towns.Count, Real.Towns.Values.Select(t => t.MapAt).Distinct().Count());
        Assert.All(Real.Towns.Values.SelectMany(t => t.Links ?? []), l => Assert.False(string.IsNullOrWhiteSpace(l.Name.Fr) || string.IsNullOrWhiteSpace(l.Name.En)));
    }

    [Fact]
    public void AWayToNowhere_OrOntoATree_IsRefused()
    {
        Town a = new("a", Text, ["...", "..."], new Cell(0, 0), [], [], [new ZoneLink(new Cell(2, 0), "b", new Cell(0, 0), Text)]);
        Town b = new("b", Text, ["T..", "..."], new Cell(1, 0), [], [], [new ZoneLink(new Cell(2, 1), "a", new Cell(1, 0), Text)]);
        GameData With(params Town[] towns) => new(Spells, [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])], null, towns, []);
        Assert.Throws<InvalidDataException>(() => With(a, b));
        Assert.NotNull(With(a, b with { Rows = ["...", "..."] }).Towns["b"]);
        Assert.Throws<InvalidDataException>(() => With(a));
        Assert.Throws<InvalidDataException>(() => With(a with { Level = 0 }, b with { Rows = ["...", "..."] }));
    }
}
