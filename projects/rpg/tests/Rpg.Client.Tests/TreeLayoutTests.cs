using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The tree's window without any engine (sprint 148, D82, K1 d): at 1280 × 720 no node leaves the area and none overlaps
/// another, nodes of 56 and 72 px, a neighbour for the keyboard each way there is one, the couples side by side; the five
/// states on a tree of each; the cascade shown before it is done, the draft undone, what a save would gain and lose.
/// </summary>
public class TreeLayoutTests
{
    private static readonly GameData Data = GameData.Embedded;

    // The tree's area in a window of 1250 × 700 inside a game of 1280 × 720, the side panel taken off.
    private const float W = 1250 - 280 - 12 - 24, H = 700 - 24 - 34;

    public static TheoryData<Branch> Styles => [.. TreeLayout.Styles];

    [Theory]
    [MemberData(nameof(Styles))]
    public void EachStyleDeployed_FitsTheArea_WithoutOverlap(Branch focus)
    {
        var layout = new TreeLayout(Data, focus, W, H);
        Assert.All(layout.Nodes, n => Assert.True(n.Box.Inside(W, H), $"{n.Id} at {n.Box}"));
        // Each node with the name under it: no two of them overlap.
        Box Named(TreeNode n) => n.Box with { H = n.Box.H + TreeLayout.Label };
        foreach (TreeNode a in layout.Nodes)
            Assert.Null(layout.Nodes.FirstOrDefault(b => b != a && Named(a).Overlaps(Named(b)))?.Id);
        Assert.All(layout.Nodes, n => Assert.True(n.Box.W >= 44 && n.Box.H >= 44, n.Id));
        Assert.All(layout.Nodes, n => Assert.Equal(n.Shape is NodeShape.Key || (n.Shape is NodeShape.Door && n.Branch == Branch.Fork) ? TreeLayout.Large : TreeLayout.Small, n.Box.W));
        // The deployed style shows each of its talents, the others one node a floor.
        Assert.Equal(Data.Talents.Values.Where(t => t.Branch == focus).Select(t => t.Id).Order(StringComparer.Ordinal),
            layout.Nodes.Where(n => n.IsTalent && n.Branch == focus).Select(n => n.Id).Order(StringComparer.Ordinal));
        Assert.All(TreeLayout.Styles.Where(s => s != focus), s => Assert.DoesNotContain(layout.Nodes, n => n.IsTalent && n.Branch == s));
        // The fork, the Hearts, the braid and the trunk are always there.
        Assert.All(Data.Talents.Values.Where(t => t.IsFork || t.Branch == Branch.Braids), t => Assert.NotNull(layout[t.Id]));
        Assert.Equal(3, layout.Nodes.Count(n => n.IsTrunk));
    }

    [Fact]
    public void TheTree_ReadsBottomUp_TrunkForkFloors()
    {
        var layout = new TreeLayout(Data, Branch.Terrain, W, H);
        float trunk = layout.Nodes.Where(n => n.IsTrunk).Min(n => n.Box.Y);
        float fork = layout["bogging"]!.Box.Y;
        float first = layout["side-step"]!.Box.Y, second = layout["seal"]!.Box.Y, key = layout["nomad"]!.Box.Y;
        Assert.True(trunk > fork && fork > first && first > second && second > key);
        // The braid over its pair; the other pairs' braids, still to come, no longer drawn (sprint 164, É1).
        Assert.True(layout["transmute-fire-lightning"]!.Box.Y < fork);
        Assert.DoesNotContain(layout.Nodes, n => n.Id.StartsWith("coming:", StringComparison.Ordinal));
    }

    [Fact]
    public void TheCouples_AreSideBySide_ThenPureElement()
    {
        var layout = new TreeLayout(Data, Branch.Melee, W, H);
        TreeNode[] row = [.. layout.Nodes.Where(n => n.Branch is Branch.Fork or Branch.Pure).OrderBy(n => n.Box.X)];
        Assert.Equal(["electrifying", "bogging", "chilling", "cracking", "fogging", "blinding"], row.Take(6).Select(n => n.Id));
        Assert.All(row.Skip(6), n => Assert.Equal(Branch.Pure, n.Branch));
        // Inside a couple, closer than between two couples (C7).
        float Gap(int i) => row[i + 1].Box.X - (row[i].Box.X + row[i].Box.W);
        Assert.True(Gap(0) < Gap(1) && Gap(2) < Gap(3) && Gap(4) < Gap(5));
        foreach ((string a, string b) in new[] { ("electrifying", "bogging"), ("chilling", "cracking"), ("fogging", "blinding") })
            Assert.Null(Talents.Problem([a, b], 35, Data));
    }

    [Theory]
    [MemberData(nameof(Styles))]
    public void TheKeyboard_FindsANeighbour_EachWayThereIsOne(Branch focus)
    {
        var layout = new TreeLayout(Data, focus, W, H);
        TreeNode[] nodes = [.. layout.Nodes];
        foreach (TreeNode n in nodes)
        {
            foreach (Direction d in Enum.GetValues<Direction>())
            {
                bool any = nodes.Any(o => o != n && Ahead(n, o, d));
                TreeNode? next = layout.Neighbour(n.Id, d);
                Assert.Equal(any, next is not null);
                if (next is not null)
                    Assert.True(Ahead(n, next, d), $"{n.Id} {d} → {next.Id}");
            }
        }
        // What is to come is never reached.
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
        // The trunk, lit from its level on; a folded floor, the best of its nodes.
        Assert.Equal(NodeState.Level, TreeLayout.TrunkState(35, 34));
        Assert.Equal(NodeState.Taken, TreeLayout.TrunkState(35, 35));
        Assert.Equal(NodeState.Taken, TreeLayout.FoldState(taken, Branch.Terrain, Tier.First, 40, Data));
        Assert.Equal(NodeState.Open, TreeLayout.FoldState(taken, Branch.Terrain, Tier.Second, 40, Data));
        Assert.Equal(NodeState.Open, TreeLayout.FoldState(taken, Branch.Melee, Tier.First, 40, Data));
        Assert.Equal(NodeState.Locked, TreeLayout.FoldState(taken, Branch.Melee, Tier.Second, 40, Data));
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
    public void TheWindow_OpensOnTheStyleAndThePairTaken_ElseTheTrunk()
    {
        var layout = new TreeLayout(Data, Branch.Melee, W, H);
        Assert.Equal(Branch.Melee, TreeLayout.FocusOf([], Data));
        Assert.Equal(Branch.Terrain, TreeLayout.FocusOf(Progression.Builds["mage-pure"], Data));
        Assert.Equal("trunk:30", layout.Start([], Data));
        Assert.Equal("heart-fire", layout.Start(Progression.Builds["mage-pure"], Data));
        // With two pairs, the one whose braid has the most nodes (K4 a).
        Assert.Equal("electrifying", layout.Start(["bogging", "electrifying", "transmute-fire-lightning"], Data));
        Assert.Equal("bogging", layout.Start(["bogging", "electrifying"], Data));
    }

    [Fact]
    public void TheLinks_GoFromTheDoorsTaken_AndUpTheFloors()
    {
        string[] taken = ["bogging", "side-step"];
        var layout = new TreeLayout(Data, Branch.Terrain, W, H);
        var links = layout.Links(taken, Data).ToList();
        Assert.Contains(("bogging", "side-step"), links);
        Assert.Contains(("side-step", "seal"), links);
        Assert.Contains(("electrifying", "transmute-fire-lightning"), links);
        // A door not taken links to no style; a folded style links its floors.
        Assert.DoesNotContain(links, l => l.From == "chilling");
        Assert.Contains(("bogging", "fold:Melee:First"), links);
        Assert.Contains(("fold:Melee:First", "fold:Melee:Second"), links);
        Assert.Contains(("fold:Alchemy:First", "fold:Alchemy:Key"), links);
        // A Heart opens no Alchemy.
        Assert.DoesNotContain(("heart-fire", "fold:Alchemy:First"), new TreeLayout(Data, Branch.Terrain, W, H).Links(["heart-fire"], Data));
        Assert.All(links, l => Assert.True(layout[l.From] is not null && layout[l.To] is not null));
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
