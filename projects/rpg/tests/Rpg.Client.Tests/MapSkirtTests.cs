using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The neighbouring map seen past the edge, faded (sprint 178, D87 §7.2 B, Q2).</summary>
public class MapSkirtTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static readonly Dictionary<Cell, Town> World = MapPhotos.World(Data).GroupBy(t => t.MapAt).ToDictionary(g => g.Key, g => g.First());

    private static readonly Cell[] Sides = [new(-1, 0), new(0, -1), new(1, 0), new(0, 1)];

    [Fact]
    public void TheSkirt_IsSixCellsDeep_AllAroundTheMap_CornersIncluded_NeverOnIt()
    {
        Town map = Data.Towns["woods-2-2"];
        IReadOnlyList<SkirtCell> skirt = MapSkirt.Around(Data, map);
        int w = map.Width, h = map.Rows.Count, d = MapSkirt.Depth;
        Assert.Equal(6, d);
        Assert.Equal((w + 2 * d) * (h + 2 * d) - w * h, skirt.Count);
        Assert.Equal(skirt.Count, skirt.Select(s => s.At).Distinct().Count());
        Assert.All(skirt, s => Assert.False(s.At.X >= 0 && s.At.X < w && s.At.Y >= 0 && s.At.Y < h));
        Assert.All(skirt, s => Assert.InRange(s.Depth, 1, d));
        foreach (Cell side in Sides)
            Assert.Equal((side.X == 0 ? w : h) * d, MapSkirt.Side(skirt, map, side).Count());
    }

    [Fact]
    public void PastAnEdgeWithANeighbour_TheSkirtIsTheNeighboursFirstCells_InOrder_AndInPlace()
    {
        int checkedSides = 0;
        foreach (Town map in World.Values.Where(t => t.Zone == "woods"))
        {
            IReadOnlyList<SkirtCell> skirt = MapSkirt.Around(Data, map);
            Dictionary<Cell, SkirtCell> at = skirt.ToDictionary(s => s.At);
            int w = map.Width, h = map.Rows.Count;
            foreach (Cell side in Sides)
            {
                if (!World.TryGetValue(map.MapAt + side, out Town? next))
                    continue;
                checkedSides++;
                for (int k = 0; k < MapSkirt.Depth; k++)
                {
                    for (int i = 0; i < (side.X == 0 ? w : h); i++)
                    {
                        // Our cell past the edge, and the neighbour's cell it shows.
                        (Cell ours, Cell theirs) = side switch
                        {
                            { X: 1 } => (new Cell(w + k, i), new Cell(k, i)),
                            { X: -1 } => (new Cell(-1 - k, i), new Cell(next.Width - 1 - k, i)),
                            { Y: 1 } => (new Cell(i, h + k), new Cell(i, k)),
                            _ => (new Cell(i, -1 - k), new Cell(i, next.Rows.Count - 1 - k)),
                        };
                        SkirtCell s = at[ours];
                        Assert.Equal(next.Id, s.From);
                        Assert.Equal(k + 1, s.Depth);
                        char kind = next[theirs];
                        Assert.Equal(".=,~TRBMH".Contains(kind, StringComparison.Ordinal) ? kind : '.', s.Kind);
                    }
                }
            }
        }
        Assert.True(checkedSides > 20, $"{checkedSides}");
    }

    [Fact]
    public void WhereTheWorldEnds_TheSkirtIsMadeUp_InTheZonesStyle_ThickeningOutwards()
    {
        // A map of the woods' style (the woods, the Ancestors, the village) at the world's end.
        (Town edge, Cell side) = World.Values.Where(t => t.ZoneId is "woods" or "ancestors" or "clairval")
            .SelectMany(t => Sides.Select(s => (t, s))).First(p => !World.ContainsKey(p.t.MapAt + p.s));
        SkirtCell[] top = [.. MapSkirt.Side(MapSkirt.Around(Data, edge), edge, side)];
        Assert.All(top, s => Assert.Null(s.From));
        Assert.All(top, s => Assert.Contains(s.Kind, ".TRB"));
        double Thick(int depth) => top.Where(s => s.Depth == depth).Average(s => s.Kind == '.' ? 0 : 1);
        Assert.True(Thick(1) < Thick(MapSkirt.Depth), $"{Thick(1)} {Thick(MapSkirt.Depth)}");
        Assert.InRange(Thick(1), 0.1, 0.5);
        Assert.True(Thick(MapSkirt.Depth) > 0.7);
        // The fields' skirt is mostly hedges, the lake's has water.
        Assert.Contains(Enumerable.Range(0, 400).Select(i => MapSkirt.MadeUp("fields", new Cell(i, -6), 6)), k => k == 'B');
        Assert.Contains(Enumerable.Range(0, 400).Select(i => MapSkirt.MadeUp("lake", new Cell(i, -6), 6)), k => k == '~');
    }

    [Fact]
    public void TheSkirt_FadesOutwards_AndADungeonsHallHasNone()
    {
        // Nothing at the edge (no step), rising all the way, nearly the background at the far edge.
        Assert.Equal(0, MapSkirt.Fade(0));
        for (float d = 0; d < MapSkirt.Depth; d += 0.5f)
            Assert.True(MapSkirt.Fade(d) < MapSkirt.Fade(d + 0.5f));
        Assert.InRange(MapSkirt.Fade(0.5f), 0.05f, 0.2f);
        Assert.InRange(MapSkirt.Fade(MapSkirt.Depth), 0.9f, 0.95f);
        Town hall = Data.Towns.Values.First(t => t.IsHall);
        Assert.Empty(MapSkirt.Around(Data, hall));
    }

    [Fact]
    public void NoSkirtCell_CanBeWalkedTo()
    {
        var controller = new TownController(Data, "woods-2-2");
        foreach (SkirtCell s in MapSkirt.Around(Data, controller.Town).Where((_, i) => i % 37 == 0))
        {
            Assert.False(controller.IsFree(s.At));
            Assert.Null(controller.PathTo(s.At));
        }
    }
}
