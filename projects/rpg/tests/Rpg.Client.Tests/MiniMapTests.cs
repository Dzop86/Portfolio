using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The mini-map (sprint 108, §5 ter.6): its view on the player, its zoom, its dots and their legend, the cell a click walks to.</summary>
public class MiniMapTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly WorldPicture World = new(Data);
    private static readonly Texts Fr = new("fr");

    private static MiniMap On(string map, Cell at, int zoom = MiniMap.DefaultZoom, MiniMapSize size = MiniMapSize.Medium)
    {
        var mini = new MiniMap(Data, World) { Zoom = zoom, Size = size };
        mini.Follow(map, at.X, at.Y);
        return mini;
    }

    private static MiniScene Scene(string map, IReadOnlyCollection<string>? growing = null, CharacterSummary? character = null, QuestBook? book = null, TrailGoal? goal = null) =>
        new(Data.Towns[map], new TownController(Data, map).Groups, growing ?? [], character, book, goal);

    [Fact]
    public void TheView_IsCentredOnThePlayer_AndFollowsTheirSteps()
    {
        Cell spawn = Data.Towns["clairval"].Spawn;
        MiniMap mini = On("clairval", spawn);
        (double x, double y) = mini.ViewOfCell("clairval", spawn);
        Assert.Equal(mini.Width / 2.0, x, 6);
        Assert.Equal(mini.Height / 2.0, y, 6);
        Assert.Equal(("clairval", spawn), mini.CellAt(x, y) is (Town m, Cell c) ? (m.Id, c) : default);
        // Half a cell walked: the view moves with the player, the cell under its centre a step farther.
        mini.Follow("clairval", spawn.X + 1, spawn.Y);
        Assert.Equal(mini.Width / 2.0, mini.ViewOfCell("clairval", spawn + new Cell(1, 0)).X, 6);
        Assert.Equal(spawn + new Cell(1, 0), mini.WalkTo(mini.Width / 2.0, mini.Height / 2.0));
        // To and from the view, the same pixel of the world.
        (double px, double py) = mini.FromView(17, 23);
        Assert.Equal(17, mini.ToView(px, py).X, 6);
        Assert.Equal(23, mini.ToView(px, py).Y, 6);
    }

    [Fact]
    public void TheZoom_GoesFromSixteenCellsToThreeMapsAcross_AndAWholeMapFitsTheViewAtFirst()
    {
        var mini = new MiniMap(Data, World);
        Assert.Equal(48, mini.Span);
        // A whole map, 48 × 32 cells, fills the view of each size exactly: 3 to 2.
        foreach (MiniMapSize size in Enum.GetValues<MiniMapSize>())
        {
            mini.Size = size;
            Assert.Equal(mini.Width, 48 * WorldPicture.CellPixels * mini.Scale, 6);
            Assert.Equal(mini.Height, 32 * WorldPicture.CellPixels * mini.Scale, 6);
        }
        Assert.True(mini.ZoomIn());
        Assert.True(mini.ZoomIn());
        Assert.Equal(16, mini.Span);
        Assert.False(mini.ZoomIn());
        while (mini.ZoomOut())
        {
        }
        Assert.Equal(3 * 48, mini.Span);
        mini.Zoom = 99;
        Assert.Equal(MiniMap.Spans.Count - 1, mini.Zoom);
        Assert.True(MiniMap.ViewOf(MiniMapSize.Small).Width < MiniMap.ViewOf(MiniMapSize.Medium).Width);
        Assert.True(MiniMap.ViewOf(MiniMapSize.Medium).Width < MiniMap.ViewOf(MiniMapSize.Large).Width);
    }

    [Fact]
    public void AClick_WalksToACellOfThePlayersMap_NeverOfAnother()
    {
        Town square = Data.Towns["clairval"];
        MiniMap mini = On("clairval", square.Spawn, zoom: MiniMap.Spans.Count - 1);
        // Three maps across: the map to the west is in the view, a third of the way from its left edge.
        (Town map, Cell _) = mini.CellAt(mini.Width / 6.0, mini.Height / 2.0)!.Value;
        Assert.NotEqual("clairval", map.Id);
        Assert.Null(mini.WalkTo(mini.Width / 6.0, mini.Height / 2.0));
        Assert.NotNull(mini.WalkTo(mini.Width / 2.0 + 3, mini.Height / 2.0));
    }

    [Fact]
    public void TheDots_ShowThePlayerOnTop_TheInhabitantsWithTheirMarks_AndTheGoalFollowed()
    {
        Town square = Data.Towns["clairval"];
        MiniMap mini = On("clairval", square.Spawn);
        var book = new QuestBook(Data, 1, [], [], []);
        Npc aubin = square.Npcs.First(n => book.Mark(Data.RefOf(square, n)) == QuestMark.Offer);
        var goal = new TrailGoal("clairval", aubin.At);
        IReadOnlyList<MiniMark> marks = mini.Marks(Scene("clairval", book: book, goal: goal), Fr);
        Assert.Equal(MiniMarkKind.Player, marks[^1].Kind);
        Assert.Equal((mini.Width / 2.0, mini.Height / 2.0), (marks[^1].X, marks[^1].Y));
        Assert.Equal("Vous", marks[^1].Legend);
        MiniMark offer = marks.Single(m => m.Kind == MiniMarkKind.Inhabitant && m.Legend == aubin.Name.In("fr"));
        Assert.Equal(QuestMark.Offer, offer.Quest);
        MiniMark ring = marks.Single(m => m.Kind == MiniMarkKind.Goal);
        Assert.Equal((offer.X, offer.Y), (ring.X, ring.Y));
        // Every inhabitant of the square is in a view of the whole map, and every dot inside the view.
        Assert.Equal(square.Npcs.Count, marks.Count(m => m.Kind == MiniMarkKind.Inhabitant));
        Assert.All(marks, m => Assert.True(m.X >= 0 && m.Y >= 0 && m.X < mini.Width && m.Y < mini.Height));
        // The legend under the pointer: the nearest dot, else the map's name.
        // The goal's ring is drawn over the inhabitant: its legend first.
        Assert.Equal(Fr["minimap.goal"], mini.Legend(marks, offer.X + 2, offer.Y + 1, Fr));
        Assert.Equal(aubin.Name.In("fr"), mini.Legend(mini.Marks(Scene("clairval", book: book), Fr), offer.X + 2, offer.Y + 1, Fr));
        Assert.Equal("Vous", mini.Legend(marks, mini.Width / 2.0 + 1, mini.Height / 2.0, Fr));
    }

    [Fact]
    public void TheLegend_TellsTheNearestDot_TheOneOnTopOnATie_ElseTheMapsName()
    {
        MiniMap mini = On("clairval", Data.Towns["clairval"].Spawn);
        MiniMark[] marks =
        [
            new(MiniMarkKind.Inhabitant, 50, 50, "under"),
            new(MiniMarkKind.Goal, 50, 50, "over"),
            new(MiniMarkKind.Spot, 60, 50, "spot"),
        ];
        Assert.Equal("over", mini.Legend(marks, 51, 50, Fr));
        Assert.Equal("spot", mini.Legend(marks, 58, 50, Fr));
        Assert.Equal(Fr.MapTitle(Data.Towns["clairval"]), mini.Legend(marks, 120, 20, Fr));
    }

    [Fact]
    public void TheGatheringSpots_HaveTheirTradesColour_AreGreyedWhenTheTradeIsTooLow_AndHiddenWhileGrowingBack()
    {
        HarvestNode high = Data.Nodes.Values.Where(n => !Data.Towns[n.Map].IsHall && n.Level > 1).OrderBy(n => n.Id, StringComparer.Ordinal).First();
        MiniMap mini = On(high.Map, high.At);
        var novice = new CharacterSummary(Guid.NewGuid(), "Ondine", "female-d", "fire", 0, DateTimeOffset.UnixEpoch);
        MiniMark spot = mini.Marks(Scene(high.Map, character: novice), Fr).Single(m => m.Kind == MiniMarkKind.Spot && (m.X, m.Y) == mini.ViewOfCell(high.Map, high.At));
        Assert.True(spot.Low);
        Assert.Equal(high.Kind, spot.Spot);
        Assert.Equal(Fr["minimap.spot", Data.Items[high.Item].Name.In("fr"), Data.Jobs[high.Job].Name.In("fr"), high.Level], spot.Legend);
        var master = novice with { Jobs = new Dictionary<string, long> { [high.Job] = long.MaxValue / 4 } };
        Assert.DoesNotContain(mini.Marks(Scene(high.Map, character: master), Fr), m => m.Kind == MiniMarkKind.Spot && m.Low && (m.X, m.Y) == (spot.X, spot.Y));
        Assert.DoesNotContain(mini.Marks(Scene(high.Map, growing: [high.Id]), Fr), m => m.Kind == MiniMarkKind.Spot && (m.X, m.Y) == (spot.X, spot.Y));
        Assert.NotEqual(MiniMap.SpotColour(NodeKind.Tree), MiniMap.SpotColour(NodeKind.Water));
    }

    [Fact]
    public void TheGroups_AreDots_BiggerForAHorde_CrownedForAChief()
    {
        MonsterFamily family = Data.Families.Values.OrderBy(f => f.Id, StringComparer.Ordinal).First();
        string map = family.BossMap;
        MiniMap mini = On(map, Data.Towns[map].Spawn, zoom: MiniMap.Spans.Count - 1);
        MiniScene scene = Scene(map);
        IReadOnlyList<MiniMark> marks = mini.Marks(scene, Fr);
        Assert.Equal(scene.Groups.Count, marks.Count(m => m.Kind is MiniMarkKind.Group or MiniMarkKind.Horde or MiniMarkKind.Chief));
        Assert.Single(marks, m => m.Kind == MiniMarkKind.Chief);
        foreach ((string id, Cell _) in scene.Groups)
        {
            MonsterGroup g = Data.Groups[id];
            Assert.Contains(marks, m => m.Legend == Fr.GroupLabel(g, Data));
        }
        Assert.All(marks.Where(m => m.Kind == MiniMarkKind.Horde), m => Assert.Contains(scene.Groups, g => Fr.GroupLabel(Data.Groups[g.Key], Data) == m.Legend && Data.Groups[g.Key].Monsters.Count >= MiniMap.Horde));
    }

    [Fact]
    public void TheWays_AreArrowsPointingOut_NamedWithTheZoneWhenItChanges_AndTheDoorsAndWorkshopsAreShown()
    {
        // A map of the village by the woods: its way out leads to another zone.
        Town edge = Data.Towns.Values.Where(t => !t.IsHall && Shop.InVillage(t.Id))
            .Where(t => (t.Links ?? []).Any(l => Data.Towns[l.To].ZoneId != t.ZoneId)).OrderBy(t => t.Id, StringComparer.Ordinal).First();
        // Two maps across: the whole map is in the view, wherever the player stands.
        MiniMap mini = On(edge.Id, edge.Spawn, zoom: 3);
        IReadOnlyList<MiniMark> marks = mini.Marks(Scene(edge.Id), Fr);
        MiniMark[] ways = [.. marks.Where(m => m.Kind == MiniMarkKind.Way)];
        Assert.Equal((edge.Links ?? []).Select(l => l.To).Distinct().Count(), ways.Length);
        Assert.All(ways, w => Assert.Equal(1, Math.Abs(w.Direction.X) + Math.Abs(w.Direction.Y)));
        Assert.Contains(ways, w => w.Label is not null);
        Assert.Contains(ways, w => w.Label is null);
        Assert.Equal((edge.Workshops ?? []).Count, marks.Count(m => m.Kind == MiniMarkKind.Workshop));
        // A dungeon's door, named as on the world map.
        Dungeon d = Data.Dungeons.Values.OrderBy(x => x.Id, StringComparer.Ordinal).First();
        MiniMap atDoor = On(d.Map, d.Door);
        Assert.Contains(atDoor.Marks(Scene(d.Map), Fr), m => m.Kind == MiniMarkKind.Door && m.Legend == Fr["map.dungeon", d.Name.In("fr")]);
    }

    [Fact]
    public void ADungeonsHall_HasNoDots_AndNoCellToWalkTo()
    {
        Town hall = Data.Towns[Data.Dungeons["boar-wallow"].Halls![0].Map];
        MiniMap mini = On(hall.Id, hall.Spawn);
        Assert.False(mini.InWorld);
        Assert.Empty(mini.Marks(new MiniScene(hall, new Dictionary<string, Cell>(), []), Fr));
        Assert.Null(mini.WalkTo(mini.Width / 2.0, mini.Height / 2.0));
    }

    [Fact]
    public void TheSettings_AreKeptAsText_WhatIsWrongKeepingItsDefault()
    {
        var moved = new MiniMapSettings(false, MiniMapSize.Large, 4, 120, 36);
        Assert.Equal(moved, MiniMapSettings.Parse(moved.ToText()));
        Assert.Equal(new MiniMapSettings(), MiniMapSettings.Parse(new MiniMapSettings().ToText()));
        Assert.Equal(new MiniMapSettings(), MiniMapSettings.Parse(null));
        Assert.Equal(new MiniMapSettings(Zoom: MiniMap.Spans.Count - 1), MiniMapSettings.Parse("zoom=40\nsize=9\nshown=x"));
        // A corner without both its coordinates is no corner.
        Assert.Equal(new MiniMapSettings(), MiniMapSettings.Parse("x=10"));
    }
}
