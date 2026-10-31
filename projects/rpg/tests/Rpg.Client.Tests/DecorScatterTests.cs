using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The world without its chequerboard (sprint 178, D87 §7.2 A): decor scattered, ground in patches.</summary>
public class DecorScatterTests
{
    private static readonly GameData Data = GameData.Embedded;

    [Fact]
    public void TheSameCell_AlwaysStandsTheSameWay()
    {
        var c = new Cell(17, 9);
        Assert.Equal(DecorScatter.For(c, 'T', 5), DecorScatter.For(c, 'T', 5));
        Assert.Equal(DecorScatter.Tone(c), DecorScatter.Tone(c));
        Assert.Equal(DecorScatter.Rim(Data.Towns["woods-2-2"], 3), DecorScatter.Rim(Data.Towns["woods-2-2"], 3));
    }

    [Fact]
    public void EveryPiece_StaysNearItsMiddle_TurnedAndScaledWithinBounds_ButNeverExactlyCentred()
    {
        for (int x = -10; x < 60; x++)
        {
            for (int y = -10; y < 40; y++)
            {
                Scatter s = DecorScatter.For(new Cell(x, y), 'T', 4);
                Assert.InRange(s.Dx, -DecorScatter.MaxShift, DecorScatter.MaxShift);
                Assert.InRange(s.Dz, -DecorScatter.MaxShift, DecorScatter.MaxShift);
                Assert.InRange(s.Turn, 0, 360);
                Assert.InRange(s.Scale, DecorScatter.MinScale, DecorScatter.MaxScale);
                Assert.InRange(s.Variant, 0, 3);
                Assert.False(s.Dx == 0 && s.Dz == 0);
            }
        }
    }

    [Fact]
    public void AWoodsMap_UsesAtLeastThreeTreeModels_AndNeighboursDoNotAlternateByParity()
    {
        Town woods = Data.Towns["woods-2-2"];
        Cell[] trees = [.. Cells(woods).Where(c => woods[c] == 'T')];
        Assert.True(trees.Select(c => DecorScatter.For(c, 'T', 4).Variant).Distinct().Count() >= 3);
        // Parity would give every pair of horizontal neighbours different models; the hash, often the same.
        int same = trees.Count(c => woods[c + new Cell(1, 0)] == 'T' && DecorScatter.For(c, 'T', 4).Variant == DecorScatter.For(c + new Cell(1, 0), 'T', 4).Variant);
        Assert.True(same > 0);
    }

    [Fact]
    public void TheGround_ChangesSlowly_InPatches_UsingEveryTone()
    {
        int jumps = 0, pairs = 0;
        HashSet<int> tones = [];
        for (int x = 0; x < 48; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                int t = DecorScatter.Tone(new Cell(x, y));
                tones.Add(t);
                int right = DecorScatter.Tone(new Cell(x + 1, y));
                pairs++;
                // A neighbour is never more than one tone away: no checks, no hard edge.
                Assert.InRange(Math.Abs(t - right), 0, 1);
                if (t != right)
                    jumps++;
            }
        }
        Assert.True(tones.Count >= DecorScatter.Tones - 1, string.Join(",", tones));
        // The chequerboard changed tone between every pair; the patches, rarely.
        Assert.True(jumps < pairs / 3, $"{jumps} of {pairs}");
    }

    [Fact]
    public void TheGrass_IsDryAlongTheRoads_Only()
    {
        Town village = Data.Towns["clairval"];
        Cell[] grass = [.. Cells(village).Where(c => village[c] == '.')];
        Cell[] dry = [.. grass.Where(c => DecorScatter.Dry(village, c))];
        Assert.NotEmpty(dry);
        Assert.All(dry, c => Assert.Contains(Around(c), n => village[n] == '='));
        Assert.All(Cells(village).Where(c => village[c] == '='), c => Assert.False(DecorScatter.Dry(village, c)));
    }

    [Fact]
    public void TheTufts_GrowOnOneGrassCellInEight_NeverOnAWayADoorSomeoneOrASpot()
    {
        foreach (string id in new[] { "woods-2-2", "fields-1-3", "clairval" })
        {
            Town town = Data.Towns[id];
            var controller = new TownController(Data, id);
            HashSet<Cell> taken = [.. Reserved(town, controller)];
            IReadOnlyList<Sprig> tufts = DecorScatter.Tufts(town, taken.Contains, 3);
            int grass = Cells(town).Count(c => town[c] == '.');
            Assert.InRange(tufts.Count, grass / 16, grass / 4);
            Assert.All(tufts, t => Assert.Equal('.', town[t.At]));
            Assert.All(tufts, t => Assert.DoesNotContain(t.At, taken));
            Assert.All(tufts, t => Assert.NotEqual(town.Spawn, t.At));
            Assert.Equal(tufts.Count, tufts.Select(t => t.At).Distinct().Count());
        }
    }

    [Fact]
    public void TheGroves_HaveBushesOnTheirRim_OnTheTreesOwnCells_LeaningToTheGrass()
    {
        Town woods = Data.Towns["woods-2-2"];
        IReadOnlyList<Sprig> rim = DecorScatter.Rim(woods, 3);
        Assert.NotEmpty(rim);
        foreach (Sprig b in rim)
        {
            Assert.Equal('T', woods[b.At]);
            // On its own cell (never past its edge), pushed towards a walkable neighbour.
            Assert.InRange(b.Dx, -0.49f, 0.49f);
            Assert.InRange(b.Dz, -0.49f, 0.49f);
            Cell towards = Math.Abs(b.Dx) > Math.Abs(b.Dz) ? new(Math.Sign(b.Dx), 0) : new(0, Math.Sign(b.Dz));
            Assert.True(woods[b.At + towards] is '.' or ',');
        }
        // One to three a tree.
        Assert.All(rim.GroupBy(b => b.At), g => Assert.InRange(g.Count(), 1, 3));
    }

    private static IEnumerable<Cell> Reserved(Town town, TownController controller) =>
        town.Npcs.Select(n => n.At)
            .Concat(town.Exits.Select(e => e.At))
            .Concat((town.Links ?? []).Select(l => l.At))
            .Concat(town.Doors.Select(d => d.At))
            .Concat((town.Props ?? []).Select(p => p.At))
            .Concat((town.Workshops ?? []).Select(w => w.At))
            .Concat(controller.Nodes.Select(n => n.At))
            .Concat(controller.Groups.Values);

    private static IEnumerable<Cell> Cells(Town town) =>
        Enumerable.Range(0, town.Rows.Count).SelectMany(y => Enumerable.Range(0, town.Rows[y].Length).Select(x => new Cell(x, y)));

    private static IEnumerable<Cell> Around(Cell c) =>
        Enumerable.Range(-1, 3).SelectMany(dy => Enumerable.Range(-1, 3).Select(dx => new Cell(c.X + dx, c.Y + dy))).Where(n => n != c);
}
