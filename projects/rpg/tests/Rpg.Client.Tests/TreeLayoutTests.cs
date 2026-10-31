using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The tree's window without any engine (sprint 148, D82; the espalier since sprint 176, D87, <c>docs/arbre-lisible.md</c>
/// §4.2 and §7): every node on its line, in its lane, above its parents, none overlapping another, at its size; the lanes in
/// their order; on a corpus of drafts, wood that never crosses and lives only where it leads to a node taken; the window
/// framed on the build; a neighbour for the keyboard each way there is one; the five states on a tree of each; the draft
/// undone, what a save would gain and lose.
/// </summary>
public class TreeLayoutTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly TreeLayout Layout = new(Data);

    /// <summary>
    /// The corpus (docs/arbre-lisible.md §4.2): the empty tree at 30, the reference builds at 45, 60 and 100, and 300 drafts
    /// drawn from every door at 30, 45, 60 and 100.
    /// </summary>
    private static IEnumerable<(IReadOnlyList<string> Taken, int Level)> Corpus()
    {
        yield return ([], 30);
        foreach (IReadOnlyList<string> build in Progression.Builds.Values)
            foreach (int level in new[] { 45, 60, 100 })
                yield return (build, level);
        var rng = new Random(176);
        string[] doors = [.. Data.Talents.Values.Where(t => t.IsFork).Select(t => t.Id)];
        int[] levels = [30, 45, 60, 100];
        for (int i = 0; i < 300; i++)
        {
            int level = levels[i % levels.Length];
            yield return (TreeDraws.Random(doors[i % doors.Length], level, rng, Data), level);
        }
    }

    private static Func<TreeNode, NodeState?> StateOf(IReadOnlyList<string> taken, int level) => n =>
        n.IsTrunk ? TreeLayout.TrunkState(int.Parse(n.Id["trunk:".Length..], System.Globalization.CultureInfo.InvariantCulture), level)
        : TreeLayout.State(taken, n.Id, level, Data);

    [Fact]
    public void EveryNode_StandsOnItsLine_InItsLane_AboveItsParents()
    {
        // The whole tree, deployed: each talent once, the three rings (G13: nothing folded nor still to come).
        Assert.Equal(Data.Talents.Keys.Order(StringComparer.Ordinal), Layout.Nodes.Where(n => n.IsTalent).Select(n => n.Id).Order(StringComparer.Ordinal));
        Assert.Equal(["trunk:30", "trunk:35", "trunk:40"], Layout.Nodes.Where(n => n.IsTrunk).Select(n => n.Id).Order(StringComparer.Ordinal));
        Assert.All(Layout.Nodes, n => Assert.DoesNotContain(':', n.IsTrunk ? "" : n.Id));
        foreach (TreeNode n in Layout.Nodes.Where(n => n.IsTalent))
        {
            TalentSpec t = Data.Talents[n.Id];
            // G1: on its line; G2: in its branch's lane.
            Assert.Equal(TreeLayout.RowY(TreeLayout.RowOf(t)), n.Box.CenterY);
            if (TreeLayout.Branches.Contains(t.Branch))
                Assert.True(n.Box.X >= Layout.Columns[t.Branch].X && n.Box.X + n.Box.W <= Layout.Columns[t.Branch].X + Layout.Columns[t.Branch].W, n.Id);
            // G3: above every parent.
            foreach (string parent in (t.Requires ?? []).Concat(t.RequiresAny ?? []))
                Assert.True(n.Box.CenterY < Layout[parent]!.Box.CenterY, $"{n.Id} under {parent}");
        }
        // The Fusion over Lightning; the Conversion on the first floor with the Transmutations it excludes.
        Assert.InRange(Math.Abs(Layout.Columns[Branch.Braids].CenterX - Layout["electrifying"]!.Box.CenterX), 0, 60);
        Assert.Equal(TreeRow.First, TreeLayout.RowOf(Data.Talents["convert-lightning"]));
        Assert.Equal(TreeRow.Second, TreeLayout.RowOf(Data.Talents["prism"]));
        // Read bottom up: the trunk, the doors, the floors, the keys.
        Assert.True(Layout["trunk:40"]!.Box.Y > Layout["bogging"]!.Box.Y && Layout["bogging"]!.Box.Y > Layout["side-step"]!.Box.Y
            && Layout["side-step"]!.Box.Y > Layout["seal"]!.Box.Y && Layout["seal"]!.Box.Y > Layout["nomad"]!.Box.Y);
        Assert.True(Layout["trunk:30"]!.Box.Y > Layout["trunk:40"]!.Box.Y);
    }

    [Fact]
    public void ThePairsOfEachBranch_AreTheNonEmptyLineUnderIt()
    {
        // G4: the data's shape the wood relies on: a node asks for any node of the non-empty line under it in its branch
        // (the doors for a first floor; Lightning for the Fusion's first, which asks it outright).
        foreach (TalentSpec t in Data.Talents.Values.Where(t => !t.IsFork))
        {
            TreeRow row = TreeLayout.RowOf(t);
            string[] parents = [.. (t.Requires ?? []).Concat(t.RequiresAny ?? []).Order(StringComparer.Ordinal)];
            TreeRow under = Enum.GetValues<TreeRow>().Where(r => r < row && r > TreeRow.Doors && Data.Talents.Values.Any(o => o.Branch == t.Branch && TreeLayout.RowOf(o) == r)).DefaultIfEmpty(TreeRow.Doors).Max();
            if (under == TreeRow.Doors)
                Assert.All(parents, p => Assert.True(Data.Talents[p].IsFork, $"{t.Id} asks {p}"));
            else
                Assert.Equal(Data.Talents.Values.Where(o => o.Branch == t.Branch && TreeLayout.RowOf(o) == under).Select(o => o.Id).Order(StringComparer.Ordinal), parents);
        }
    }

    [Fact]
    public void NothingOverlaps_AndEverythingFitsTheWorld_AtItsSizes()
    {
        // G6: each node with its names under it (a ring's at its right), the branches' names; G8: the sizes.
        Box Named(TreeNode n) => n.IsTrunk ? n.Box with { W = n.Box.W + 158 } : new Box(n.Box.CenterX - ((n.Box.W + TreeLayout.NodeGap) / 2) + 0.01f, n.Box.Y, n.Box.W + TreeLayout.NodeGap - 0.02f, n.Box.H + TreeLayout.Label);
        Box[] boxes = [.. Layout.Nodes.Select(Named), .. Layout.Columns.Values.Select(c => c with { H = 20 })];
        for (int i = 0; i < boxes.Length; i++)
            for (int j = i + 1; j < boxes.Length; j++)
                Assert.False(boxes[i].Overlaps(boxes[j]), $"{boxes[i]} and {boxes[j]}");
        Assert.All(boxes, b => Assert.True(b.Inside(Layout.Width, Layout.Height), b.ToString()));
        Assert.All(Layout.Nodes, n => Assert.True(n.Box.W >= 44 && n.Box.H >= 44, n.Id));
        Assert.All(Layout.Nodes, n => Assert.Equal(n.IsTrunk ? TreeLayout.Ring : TreeLayout.SizeOf(Data.Talents[n.Id]), n.Box.W));
        Assert.Equal(TreeLayout.Heart, Layout["heart-fire"]!.Box.W);
        Assert.Equal(TreeLayout.Large, Layout["nomad"]!.Box.W);
        // About 1 760 × 780 (§7.4).
        Assert.InRange(Layout.Width, 1500, 2000);
        Assert.InRange(Layout.Height, 700, 820);
    }

    [Fact]
    public void TheLanes_KeepTheirOrder_AndTheCouplesStandSideBySide()
    {
        // G9: the seven lanes left to right, nothing reordered by what is taken (the layout knows nothing of it).
        Assert.Equal(TreeLayout.Branches, Layout.Columns.OrderBy(c => c.Value.X).Select(c => c.Key));
        Assert.Equal(Layout.Nodes, new TreeLayout(Data).Nodes);
        TreeNode[] row = [.. Layout.Nodes.Where(n => n.Branch is Branch.Fork or Branch.Pure).OrderBy(n => n.Box.X)];
        Assert.Equal(TreeLayout.Pairs, row.Take(6).Select(n => n.Id));
        Assert.All(row.Skip(6), n => Assert.Equal(Branch.Pure, n.Branch));
        // Inside a couple, closer than between two couples (C7).
        float Gap(int i) => row[i + 1].Box.X - (row[i].Box.X + row[i].Box.W);
        Assert.True(Gap(0) < Gap(1) && Gap(2) < Gap(3) && Gap(4) < Gap(5));
        foreach ((string a, string b) in new[] { ("electrifying", "bogging"), ("chilling", "cracking"), ("fogging", "blinding") })
            Assert.Null(Talents.Problem([a, b], 35, Data));
    }

    [Fact]
    public void TheWood_NeverCrosses_OnTheWholeCorpus()
    {
        // G5: two segments of the wood meet at a shared end at most, whatever is taken.
        foreach ((IReadOnlyList<string> taken, int level) in Corpus())
        {
            (TreePoint A, TreePoint B)[] segments = [.. Layout.Wood(taken, StateOf(taken, level)).SelectMany(p => p.Points.Zip(p.Points.Skip(1)))];
            for (int i = 0; i < segments.Length; i++)
                for (int j = i + 1; j < segments.Length; j++)
                    Assert.False(Cross(segments[i], segments[j]), $"{string.Join(",", taken)}: {segments[i]} × {segments[j]}");
        }
    }

    // Whether two segments touch anywhere but at an end they share.
    private static bool Cross((TreePoint A, TreePoint B) s, (TreePoint A, TreePoint B) t)
    {
        static float Side(TreePoint a, TreePoint b, TreePoint c) => ((b.X - a.X) * (c.Y - a.Y)) - ((b.Y - a.Y) * (c.X - a.X));
        static bool On(TreePoint a, TreePoint b, TreePoint c) => Math.Abs(Side(a, b, c)) < 0.01f
            && c.X >= Math.Min(a.X, b.X) - 0.01f && c.X <= Math.Max(a.X, b.X) + 0.01f && c.Y >= Math.Min(a.Y, b.Y) - 0.01f && c.Y <= Math.Max(a.Y, b.Y) + 0.01f;
        TreePoint[] shared = [.. new[] { s.A, s.B }.Where(p => p == t.A || p == t.B)];
        if (shared.Length > 0)
        {
            // Sharing an end, they cross only if they run along each other.
            TreePoint other = s.A == shared[0] ? s.B : s.A, tOther = t.A == shared[0] ? t.B : t.A;
            return shared.Length == 2 || (Math.Abs(Side(s.A, s.B, tOther)) < 0.01f && Vector(shared[0], other, tOther));
        }
        float d1 = Side(t.A, t.B, s.A), d2 = Side(t.A, t.B, s.B), d3 = Side(s.A, s.B, t.A), d4 = Side(s.A, s.B, t.B);
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;
        return On(t.A, t.B, s.A) || On(t.A, t.B, s.B) || On(s.A, s.B, t.A) || On(s.A, s.B, t.B);
    }

    // Whether two segments from the same point go the same way.
    private static bool Vector(TreePoint from, TreePoint a, TreePoint b) => ((a.X - from.X) * (b.X - from.X)) + ((a.Y - from.Y) * (b.Y - from.Y)) > 0;

    [Fact]
    public void TheWood_IsAlive_OnlyWhereItLeadsToANodeTaken()
    {
        // G11: an empty tree has no living wood but its trunk; each door taken its stem; an axis alive up to its last line taken.
        Assert.All(Layout.Wood([], StateOf([], 30)), p => Assert.True(p.Kind == WoodKind.Trunk || p.Life != WoodLife.Alive, p.ToString()));
        Assert.DoesNotContain(Layout.Wood([], StateOf([], 30)), p => p.Kind == WoodKind.Stem);
        foreach ((IReadOnlyList<string> taken, int level) in Corpus())
        {
            Func<TreeNode, NodeState?> state = StateOf(taken, level);
            IReadOnlyList<WoodPiece> wood = Layout.Wood(taken, state);
            Assert.All(wood, p => Assert.Equal(p.Leads.Any(taken.Contains) || (p.Kind == WoodKind.Trunk && p.Leads.Any(id => state(Layout[id]!) == NodeState.Taken)), p.Life == WoodLife.Alive));
            Assert.Equal(taken.Count(id => Data.Talents[id].IsFork), wood.Count(p => p.Kind == WoodKind.Stem));
        }
        string[] seal = ["bogging", "side-step", "seal"];
        IReadOnlyList<WoodPiece> terrain = Layout.Wood(seal, StateOf(seal, 45));
        WoodPiece Axis(TreeRow row) => terrain.Single(p => p.Kind == WoodKind.Axis && p.Points[^1].X == Layout.AxisX(Branch.Terrain) && p.Points[^1].Y == TreeLayout.RowY(row));
        Assert.Equal(WoodLife.Alive, Axis(TreeRow.First).Life);
        Assert.Equal(WoodLife.Alive, Axis(TreeRow.Second).Life);
        Assert.Equal(WoodLife.Reachable, Axis(TreeRow.Key).Life);
        // A style nothing of which is taken is reachable from a door taken, bare without one (Alchemy asks a pair).
        Assert.Equal(WoodLife.Reachable, terrain.Single(p => p.Kind == WoodKind.Limb && p.Points[^1].X == Layout.AxisX(Branch.Melee)).Life);
        string[] heart = ["heart-fire"];
        Assert.Equal(WoodLife.Bare, Layout.Wood(heart, StateOf(heart, 45)).Single(p => p.Kind == WoodKind.Limb && p.Points[^1].X == Layout.AxisX(Branch.Alchemy)).Life);
    }

    [Fact]
    public void TheSap_RunsUpThePathToTheNodeUnderTheMouse_AndRedOnlyWhenGivingBack()
    {
        // Sprint 177 (A5): from an empty tree, the path to Nomad goes through a door, Terrain's first and second floors.
        IReadOnlyList<string> path = TreePlan.Path([], "nomad", Data)!;
        IReadOnlyList<WoodPiece> wood = Layout.Wood([], StateOf([], 45), path);
        string door = path.Single(id => Data.Talents[id].IsFork);
        Assert.Contains(wood, p => p.Kind == WoodKind.Stem && p.Sap && p.Leads.Contains(door));
        Assert.Contains(wood, p => p.Kind == WoodKind.Root && p.Sap && p.Leads.Contains(door));
        Assert.Contains(wood, p => p.Kind == WoodKind.Limb && p.Sap && p.Leads.Contains("nomad"));
        Assert.Equal(3, wood.Count(p => p.Kind == WoodKind.Axis && p.Sap));
        // Nothing of another branch, nor the twigs right of Nomad on its line.
        Assert.DoesNotContain(wood, p => p.Sap && p.Leads.All(id => Layout[id]!.Branch is Branch.Melee or Branch.Braids));
        Assert.All(wood.Where(p => p.Kind == WoodKind.Twig && p.Sap), p => Assert.Contains(p.Leads, path.Contains));
        // Without a path, no sap; without a giving back asked, nothing lost; asked, the wood of what goes.
        Assert.DoesNotContain(Layout.Wood([], StateOf([], 45)), p => p.Sap);
        string[] seal = ["bogging", "side-step", "seal"];
        IReadOnlyList<WoodPiece> built = Layout.Wood(seal, StateOf(seal, 45));
        Assert.DoesNotContain(built, p => TreeLayout.Lost(p, seal, []));
        WoodPiece[] lost = [.. built.Where(p => TreeLayout.Lost(p, seal, ["seal"]))];
        Assert.NotEmpty(lost);
        Assert.All(lost, p => Assert.DoesNotContain("side-step", p.Leads));
    }

    [Fact]
    public void Hovering_SaysWhatTakingANodeWouldClose()
    {
        // Sprint 177 (A6): a pair closes the pairs sharing one of its elements, a Heart the other Hearts; a taken node nothing.
        IReadOnlyList<string> door = TreePlan.Closes([], "bogging", Data);
        Assert.Contains("chilling", door);
        Assert.DoesNotContain("bogging", door);
        Assert.DoesNotContain("heart-fire", door);
        Assert.Equal(["heart-air", "heart-earth", "heart-water"], TreePlan.Closes([], "heart-fire", Data).Where(id => Data.Talents[id].Branch == Branch.Pure));
        Assert.Empty(TreePlan.Closes(["bogging"], "bogging", Data));
        Assert.All(door, id => Assert.NotEqual(NodeState.Excluded, TreeLayout.State([], id, Hero.MaxLevel, Data)));
        // Each closed node is closed once the path is taken.
        string[] seal = ["bogging", "side-step"];
        IReadOnlyList<string> key = TreePlan.Closes(seal, "nomad", Data);
        string[] after = [.. seal, .. TreePlan.Path(seal, "nomad", Data)!];
        Assert.All(key, id => Assert.Equal(NodeState.Excluded, TreeLayout.State(after, id, Hero.MaxLevel, Data)));
    }

    [Fact]
    public void TheCurves_LeaveAndReachTheirEndsUpright()
    {
        TreePoint[] curve = TreeLayout.Curve(new(100, 600), new(400, 560));
        Assert.Equal(TreeLayout.CurveSegments + 1, curve.Length);
        Assert.Equal((new TreePoint(100, 600), new TreePoint(400, 560)), (curve[0], curve[^1]));
        // Never going back down; the same heights for any two curves between the same heights.
        Assert.All(curve.Zip(curve.Skip(1)), p => Assert.True(p.Second.Y <= p.First.Y && p.Second.X >= p.First.X));
        Assert.Equal(curve.Select(p => p.Y), TreeLayout.Curve(new(100, 600), new(900, 560)).Select(p => p.Y));
    }

    [Fact]
    public void TheWindow_OpensOnTheBuild_ElseOnTheDoorsTrunkAndFirstFloor()
    {
        IReadOnlyList<Box> start = Layout.Build([], StateOf([], 30));
        Assert.Contains(Layout["bogging"]!.Box, start);
        Assert.Contains(Layout["trunk:30"]!.Box, start);
        Assert.Contains(Layout["side-step"]!.Box, start);
        Assert.DoesNotContain(Layout["seal"]!.Box, start);
        string[] seal = ["bogging", "side-step", "seal"];
        IReadOnlyList<Box> built = Layout.Build(seal, StateOf(seal, 45));
        Assert.All(seal, id => Assert.Contains(Layout[id]!.Box, built));
        Assert.Contains(Layout["nomad"]!.Box, built);
        Assert.DoesNotContain(Layout["chilling"]!.Box, built);
        // The other styles' first floors, open from the door, are not the build; a door alone frames like an empty tree.
        Assert.DoesNotContain(Layout["up-close"]!.Box, built);
        Assert.Equal(start.Count, Layout.Build(["bogging"], StateOf(["bogging"], 45)).Count);
    }

    [Fact]
    public void TheKeyboard_FindsANeighbour_EachWayThereIsOne()
    {
        TreeNode[] nodes = [.. Layout.Nodes];
        foreach (TreeNode n in nodes)
        {
            foreach (Direction d in Enum.GetValues<Direction>())
            {
                bool any = nodes.Any(o => o != n && Ahead(n, o, d));
                TreeNode? next = Layout.Neighbour(n.Id, d);
                Assert.Equal(any, next is not null);
                if (next is not null)
                    Assert.True(Ahead(n, next, d), $"{n.Id} {d} → {next.Id}");
            }
        }
    }

    private static bool Ahead(TreeNode from, TreeNode to, Direction d)
    {
        float dx = to.Box.CenterX - from.Box.CenterX, dy = to.Box.CenterY - from.Box.CenterY;
        (float along, float across) = d switch
        {
            Direction.Up => (-dy, Math.Abs(dx)),
            Direction.Down => (dy, Math.Abs(dx)),
            Direction.Left => (-dx, Math.Abs(dy)),
            _ => (dx, Math.Abs(dy)),
        };
        return along > 0 && across <= along;
    }

    [Fact]
    public void EachState_OnATreeOfIt()
    {
        string[] taken = ["bogging", "side-step"];
        Assert.Equal(NodeState.Taken, TreeLayout.State(taken, "bogging", 40, Data));
        Assert.Equal(NodeState.Open, TreeLayout.State(taken, "seal", 40, Data));
        // A parent missing, a door missing: locked.
        Assert.Equal(NodeState.Locked, TreeLayout.State(taken, "heavy-push", 40, Data));
        Assert.Equal(NodeState.Locked, TreeLayout.State([], "up-close", 40, Data));
        // A pair sharing the Water of Mud, a Heart besides one: excluded.
        Assert.Equal(NodeState.Excluded, TreeLayout.State(taken, "chilling", 40, Data));
        Assert.Equal(NodeState.Excluded, TreeLayout.State(["heart-fire"], "heart-air", 40, Data));
        // A key before 45, the tree before 30: out of level.
        Assert.Equal(NodeState.Level, TreeLayout.State(["bogging", "side-step", "seal"], "nomad", 44, Data));
        Assert.Equal(NodeState.Level, TreeLayout.State([], "bogging", 29, Data));
        // The trunk, lit from its level on.
        Assert.Equal(NodeState.Level, TreeLayout.TrunkState(35, 34));
        Assert.Equal(NodeState.Taken, TreeLayout.TrunkState(35, 35));
    }

    [Fact]
    public void FourLooks_FromTheStates_AndANodeShortOfPointsStaysOpen()
    {
        // Sprint 164, É1 (docs/arbre-lisible.md §2.6): locked and out of level both "not yet", excluded "closed".
        Assert.Equal([NodeLook.Taken, NodeLook.Open, NodeLook.NotYet, NodeLook.Closed, NodeLook.NotYet], Enum.GetValues<NodeState>().Select(TreeLayout.Look));
        // The two points of 30 spent on two doors: the floor over them, refused for points only, stays open (the tree no
        // longer goes dark once everything is spent; the counter and a click say the points are missing).
        Assert.Equal(RefusalKind.Points, TalentReasons.WhyNot(["bogging", "heart-earth"], "side-step", 30, Data)?.Kind);
        Assert.Equal(NodeState.Open, TreeLayout.State(["bogging", "heart-earth"], "side-step", 30, Data));
    }

    [Fact]
    public void TheKeyboard_StartsOnThePairTaken_ElseTheTrunk()
    {
        Assert.Equal("trunk:30", Layout.Start([], Data));
        Assert.Equal("heart-fire", Layout.Start(Progression.Builds["mage-pure"], Data));
        // With two pairs, the one whose braid has the most nodes (K4 a).
        Assert.Equal("electrifying", Layout.Start(["bogging", "electrifying", "transmute-fire-lightning"], Data));
        Assert.Equal("bogging", Layout.Start(["bogging", "electrifying"], Data));
    }

    [Fact]
    public void TheDraft_IsUndone_AndWhatASaveChangesIsSaid()
    {
        var hero = new Hero("Ondine", "female-a", "sentinel", Level: 45, Talents: ["bogging", "side-step", "seal"]);
        var editor = new PointsEditor(hero, Data);
        Assert.False(editor.TalentsChanged);
        Assert.Empty(new Texts("fr").TreeDiff(editor));
        editor.Take(Data.Talents["remanence"]);
        editor.Drop(Data.Talents["seal"]);
        Assert.True(editor.TalentsChanged);
        Assert.Equal(["remanence"], editor.TalentsDiff.Gained);
        Assert.Equal(["seal"], editor.TalentsDiff.Lost);
        Assert.Equal(["Gagnés : Rémanence", "Perdus : Sceau"], new Texts("fr").TreeDiff(editor));
        Assert.Equal(["Gained: Remanence", "Lost: Seal"], new Texts("en").TreeDiff(editor));
        editor.UndoTalents();
        Assert.False(editor.TalentsChanged);
        Assert.Equal(hero.Talents, editor.TalentsTaken);
        // The undone draft is what would be sent: the saved talents.
        Assert.Equal(hero.Talents, editor.ToPoints().Talents);
    }
}
