using Rpg.Core;

namespace Rpg.Client;

/// <summary>A rectangle of the tree's window, in pixels, without any engine.</summary>
public readonly record struct Box(float X, float Y, float W, float H)
{
    public float CenterX => X + (W / 2);

    public float CenterY => Y + (H / 2);

    public bool Overlaps(Box o) => X < o.X + o.W && o.X < X + W && Y < o.Y + o.H && o.Y < Y + H;

    public bool Inside(float width, float height) => X >= 0 && Y >= 0 && X + W <= width && Y + H <= height;
}

/// <summary>The five states a node shows before the click (sprint 148, D82, L5 b).</summary>
public enum NodeState
{
    /// <summary>In the draft.</summary>
    Taken,

    /// <summary>It can be taken now.</summary>
    Open,

    /// <summary>A parent, a door or points are missing.</summary>
    Locked,

    /// <summary>A rule of the tree shuts it out (an exclusion, a pair, a key, the switch, the elements).</summary>
    Excluded,

    /// <summary>Its level is not reached (the tree's 30, a key's 45).</summary>
    Level,
}

/// <summary>How a node looks (sprint 164, É1): taken (yours), open (a click), not yet (a node or a level missing), closed (a choice of yours shuts it).</summary>
public enum NodeLook
{
    Taken,
    Open,
    NotYet,
    Closed,
}

/// <summary>A node's shape, one per kind (L1 a): the trunk's, a door of the fork, a node of a floor, a key.</summary>
public enum NodeShape
{
    Trunk,
    Door,
    Node,
    Key,
}

/// <summary>The espalier's lines, bottom up (sprint 176, D87, <c>docs/arbre-lisible.md</c> §2.2): the trunk, the doors, the first and second floors, the keys.</summary>
public enum TreeRow
{
    Trunk,
    Doors,
    First,
    Second,
    Key,
}

/// <summary>
/// A node as the window places it: a talent (its id) or a ring of the trunk (<c>trunk:30</c>). Since sprint 176 (D87) every
/// branch is deployed whole: no more folded floors nor braids to come.
/// </summary>
public sealed record TreeNode(string Id, Box Box, NodeShape Shape, Branch? Branch = null, Tier? Tier = null)
{
    public bool IsTalent => !Id.Contains(':', StringComparison.Ordinal);

    public bool IsTrunk => Id.StartsWith("trunk:", StringComparison.Ordinal);
}

/// <summary>A point of the tree's world, in pixels at 100 %.</summary>
public readonly record struct TreePoint(float X, float Y);

/// <summary>
/// The pieces of the tree's wood, from the thickest (sprint 176, D87, <c>docs/arbre-lisible.md</c> §2.7): the trunk, the fork's
/// roots to the doors, a door taken's stem to the crown, the crown's collar, the limbs from it to each style's foot, the
/// arrow from Lightning to the Fusion, a branch's axis from floor to floor, the twigs along a floor to its nodes.
/// </summary>
public enum WoodKind
{
    Trunk,
    Root,
    Stem,
    Collar,
    Limb,
    Arrow,
    Axis,
    Twig,
}

/// <summary>A piece's life (§2.7): alive when it leads to a node taken, reachable when to a node open, bare otherwise.</summary>
public enum WoodLife
{
    Alive,
    Reachable,
    Bare,
}

/// <summary>A piece of wood: its kind, its points (straight segments from one to the next), the nodes it leads to, its life.</summary>
public sealed record WoodPiece(WoodKind Kind, IReadOnlyList<TreePoint> Points, IReadOnlyList<string> Leads, WoodLife Life);

/// <summary>A direction the keyboard moves from node to node.</summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

/// <summary>
/// The talent tree laid out as an espalier, without any engine (sprint 176, D87, <c>docs/arbre-lisible.md</c> §2 and §7):
/// in the coordinates of a world of its own, read from the bottom up, the trunk standing under the doors' middle, the doors
/// in couples then the Hearts, the crown over them, then the seven branches side by side in a fixed order, each in its own lane and
/// deployed whole (the Fusion over Lightning, its Transmutations and Conversion on its first floor). The camera
/// (<see cref="TreeCamera"/>) shows what the window can hold. No link goes from node to node: the wood
/// (<see cref="Wood"/>) is made of pieces that never cross.
/// </summary>
public sealed class TreeLayout
{
    /// <summary>The nodes' sizes at 100 % (G8): a node, a pair or a key, a Heart, a ring of the trunk; a node's names under it, two lines.</summary>
    public const float Small = 56, Large = 64, Heart = 48, Ring = 44, Label = 32;

    /// <summary>The gaps: between two nodes of a floor, between two branches; the margins and the gutter that names the lines.</summary>
    public const float NodeGap = 16, ColumnGap = 48, Margin = 24, Gutter = 72;

    /// <summary>The styles (the order the guide breaks a tie in; Melee and Distance side by side, their exclusions read at a glance).</summary>
    public static readonly IReadOnlyList<Branch> Styles = [Branch.Melee, Branch.Distance, Branch.Support, Branch.Terrain, Branch.Tempo, Branch.Alchemy];

    /// <summary>
    /// The branches from left to right, whatever is taken (G9): the Fusion over its door, Alchemy which lives on the pairs
    /// next to it, Melee and Distance side by side, Tempo, Terrain, and Support, the group's style, at the edge.
    /// </summary>
    public static readonly IReadOnlyList<Branch> Branches = [Branch.Braids, Branch.Alchemy, Branch.Melee, Branch.Distance, Branch.Tempo, Branch.Terrain, Branch.Support];

    /// <summary>The fork's pairs, the complementary couples side by side (C7): Lightning and Mud, Ice and Magma, Steam and Sand.</summary>
    public static readonly IReadOnlyList<string> Pairs = ["electrifying", "bogging", "chilling", "cracking", "fogging", "blinding"];

    /// <summary>The trunk's levels (B1 a), bottom up.</summary>
    public static readonly IReadOnlyList<int> TrunkLevels = [30, 35, 40];

    /// <summary>The lines' middles, top down: the keys, the second floor, the first, the doors; then the branches' feet, the crown and its collar.</summary>
    public const float KeyY = 88, SecondY = 200, FirstY = 304, Foot = 376, CollarTop = 428, CrownY = 448, DoorsY = 512, RootEnd = 584, TrunkTop = 628;

    private const float CoupleGap = NodeGap, CouplesGap = 36, StemRise = 12;
    private readonly Dictionary<string, TreeNode> _byId;
    private readonly GameData _data;

    public TreeLayout(GameData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        var nodes = new List<TreeNode>();
        // The branches' lanes: as wide as their widest line, from the gutter on.
        float x = Margin + Gutter;
        foreach (Branch branch in Branches)
        {
            float w = Math.Max(Small, Enum.GetValues<TreeRow>().Where(r => r > TreeRow.Doors).Max(r => RowWidth(Of(branch, r))));
            Columns[branch] = new Box(x, Margin, w, Foot - Margin);
            foreach (TreeRow row in new[] { TreeRow.First, TreeRow.Second, TreeRow.Key })
            {
                TalentSpec[] line = Of(branch, row);
                float at = x + ((w - RowWidth(line)) / 2);
                foreach (TalentSpec t in line)
                {
                    float size = SizeOf(t);
                    nodes.Add(new(t.Id, new Box(at, RowY(row) - (size / 2), size, size), t.Tier == Tier.Key ? NodeShape.Key : NodeShape.Node, branch, t.Tier));
                    at += size + NodeGap;
                }
            }
            x += w + ColumnGap;
        }
        Width = x - ColumnGap + Margin;
        // The doors: Lightning under the Fusion's middle, the couples side by side, then the Hearts.
        x = Columns[Branch.Braids].CenterX - (Large / 2);
        for (int i = 0; i < Pairs.Count; i++)
        {
            nodes.Add(new(Pairs[i], new Box(x, DoorsY - (Large / 2), Large, Large), NodeShape.Door, Branch.Fork, Tier.First));
            x += Large + (i % 2 == 0 ? CoupleGap : CouplesGap);
        }
        foreach (TalentSpec heart in data.Talents.Values.Where(t => t.Branch == Branch.Pure).OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            nodes.Add(new(heart.Id, new Box(x, DoorsY - (Heart / 2), Heart, Heart), NodeShape.Door, Branch.Pure, Tier.First));
            x += Heart + CoupleGap;
        }
        // The crown over the doors' middle, the trunk under it: the doors' stems and roots short, the limbs fanning out to the styles.
        TreeNode[] doors = [.. nodes.Where(n => n.Branch is Branch.Fork or Branch.Pure)];
        Crown = new TreePoint((doors.Min(n => n.Box.X) + doors.Max(n => n.Box.X + n.Box.W)) / 2, CrownY);
        // The trunk's rings, 40 at the top, 30 at the bottom.
        for (int i = 0; i < TrunkLevels.Count; i++)
            nodes.Add(new($"trunk:{TrunkLevels[i]}", new Box(Crown.X - (Ring / 2), TrunkTop + ((TrunkLevels.Count - 1 - i) * (Ring + 8)), Ring, Ring), NodeShape.Trunk));
        Height = nodes.Max(n => n.Box.Y + n.Box.H) + Margin;
        Nodes = nodes;
        _byId = nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
    }

    /// <summary>The world's size, at 100 %.</summary>
    public float Width { get; }

    public float Height { get; }

    /// <summary>Where the doors taken feed the styles: the point under the collar.</summary>
    public TreePoint Crown { get; }

    /// <summary>Each branch's lane, from its name down to its foot.</summary>
    public Dictionary<Branch, Box> Columns { get; } = [];

    public IReadOnlyList<TreeNode> Nodes { get; }

    public TreeNode? this[string id] => _byId.GetValueOrDefault(id);

    /// <summary>The middle of a line (the gutter names it there); the trunk's, its middle ring's.</summary>
    public static float RowY(TreeRow row) => row switch
    {
        TreeRow.Key => KeyY,
        TreeRow.Second => SecondY,
        TreeRow.First => FirstY,
        TreeRow.Doors => DoorsY,
        _ => TrunkTop + Ring + 8 + (Ring / 2),
    };

    /// <summary>
    /// The line a talent stands on (§2.4): the fork and the Hearts on the doors'; its tier's otherwise, but the Fusion's
    /// Conversion on the first floor with the two Transmutations it excludes (one choice, "how much Lightning").
    /// </summary>
    public static TreeRow RowOf(TalentSpec t)
    {
        ArgumentNullException.ThrowIfNull(t);
        return t.IsFork ? TreeRow.Doors
            : t.Branch == Branch.Braids && t.Tier == Tier.Key ? TreeRow.First
            : t.Tier switch { Tier.First => TreeRow.First, Tier.Second => TreeRow.Second, _ => TreeRow.Key };
    }

    /// <summary>A talent's size: a key 64, a pair 64, a Heart 48, any other node 56.</summary>
    public static float SizeOf(TalentSpec t) =>
        (t ?? throw new ArgumentNullException(nameof(t))).Tier == Tier.Key || t.Branch == Branch.Fork ? Large : t.Branch == Branch.Pure ? Heart : Small;

    // A branch's talents on a line, the keys last, then by id.
    private TalentSpec[] Of(Branch branch, TreeRow row) =>
        [.. _data.Talents.Values.Where(t => t.Branch == branch && RowOf(t) == row).OrderBy(t => t.Tier == Tier.Key).ThenBy(t => t.Id, StringComparer.Ordinal)];

    private static float RowWidth(TalentSpec[] line) => line.Length == 0 ? 0 : line.Sum(SizeOf) + ((line.Length - 1) * NodeGap);

    /// <summary>A branch's axis, left of its lane: its foot, then up through each of its lines.</summary>
    public float AxisX(Branch branch) => Columns[branch].X - 12;

    /// <summary>
    /// The tree's wood for a draft (§2.7, straight segments until the sap of the next sprint): the trunk from ring to ring,
    /// a root from the trunk's top to each door (under its name), a stem from each door taken up to the crown, the collar,
    /// a limb from the collar to each style's foot, the arrow from Lightning to the Fusion's foot, each branch's axis from
    /// line to line and its twigs along each line from node to node. Each piece is alive if it leads to a node taken,
    /// reachable if to a node open, bare otherwise (<paramref name="stateOf"/> gives a node's state).
    /// </summary>
    public IReadOnlyList<WoodPiece> Wood(IReadOnlyList<string> taken, Func<TreeNode, NodeState?> stateOf)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(stateOf);
        var pieces = new List<WoodPiece>();
        WoodLife Life(IEnumerable<string> leads)
        {
            NodeState?[] states = [.. leads.Select(id => _byId.TryGetValue(id, out TreeNode? n) ? stateOf(n) : null)];
            return states.Contains(NodeState.Taken) ? WoodLife.Alive : states.Contains(NodeState.Open) ? WoodLife.Reachable : WoodLife.Bare;
        }
        void Add(WoodKind kind, string[] leads, params TreePoint[] points) => pieces.Add(new(kind, points, leads, Life(leads)));
        static TreePoint P(float x, float y) => new(x, y);

        // The trunk, from ring to ring, up to the fork; alive as far as the rings are reached.
        TreeNode[] rings = [.. Nodes.Where(n => n.IsTrunk).OrderByDescending(n => n.Box.Y)];
        for (int i = 0; i < rings.Length; i++)
            Add(WoodKind.Trunk, [rings[i].Id], P(Crown.X, rings[i].Box.CenterY + (i == 0 ? Ring / 2 : 0)), P(Crown.X, i + 1 < rings.Length ? rings[i + 1].Box.CenterY : TrunkTop));
        // A root to each door, under its name.
        TreeNode[] doors = [.. Nodes.Where(n => n.Branch is Branch.Fork or Branch.Pure)];
        foreach (TreeNode d in doors)
            Add(WoodKind.Root, [d.Id], P(Crown.X, TrunkTop), P(d.Box.CenterX, RootEnd));
        // The doors taken up to the crown: straight up a little, then across over the others.
        foreach (TreeNode d in doors.Where(d => taken.Contains(d.Id)))
            Add(WoodKind.Stem, [d.Id], P(d.Box.CenterX, d.Box.Y), P(d.Box.CenterX, DoorsY - (Large / 2) - StemRise), P(Crown.X, Crown.Y));
        string[] Lane(Branch b) => [.. Nodes.Where(n => n.Branch == b).Select(n => n.Id)];
        string[] styles = [.. Branches.Skip(1).SelectMany(Lane)];
        Add(WoodKind.Collar, styles, P(Crown.X, Crown.Y), P(Crown.X, CollarTop));
        foreach (Branch b in Branches.Skip(1))
            Add(WoodKind.Limb, Lane(b), P(Crown.X, CollarTop), P(AxisX(b), Foot));
        TreeNode lightning = _byId[Pairs[0]];
        Add(WoodKind.Arrow, Lane(Branch.Braids), P(lightning.Box.CenterX, lightning.Box.Y), P(AxisX(Branch.Braids), Foot));
        // Each branch: its axis up from line to line, the twigs along each line, behind its nodes.
        foreach (Branch b in Branches)
        {
            float x = AxisX(b), from = Foot;
            foreach (TreeRow row in new[] { TreeRow.First, TreeRow.Second, TreeRow.Key })
            {
                TreeNode[] line = [.. Nodes.Where(n => n.Branch == b && n.IsTalent && RowOf(_data.Talents[n.Id]) == row).OrderBy(n => n.Box.X)];
                if (line.Length == 0)
                    continue;
                string[] above = [.. Nodes.Where(n => n.Branch == b && n.IsTalent && RowOf(_data.Talents[n.Id]) >= row).Select(n => n.Id)];
                Add(WoodKind.Axis, above, P(x, from), P(x, RowY(row)));
                float left = x;
                for (int i = 0; i < line.Length; i++)
                {
                    Add(WoodKind.Twig, [.. line.Skip(i).Select(n => n.Id)], P(left, RowY(row)), P(line[i].Box.CenterX, RowY(row)));
                    left = line[i].Box.CenterX;
                }
                from = RowY(row);
            }
        }
        return pieces;
    }

    /// <summary>
    /// The boxes the window opens framed on (§7.4, Charles's answer): the nodes taken, and those open in the branches where
    /// one is (a door taken opens every style's first floor: the whole tree is not the build); with no branch begun, the
    /// doors, the trunk and the first floor.
    /// </summary>
    public IReadOnlyList<Box> Build(IReadOnlyList<string> taken, Func<TreeNode, NodeState?> stateOf)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(stateOf);
        HashSet<Branch> begun = [.. Nodes.Where(n => n.IsTalent && taken.Contains(n.Id) && Branches.Contains(n.Branch!.Value)).Select(n => n.Branch!.Value)];
        return begun.Count > 0
            ? [.. Nodes.Where(n => n.IsTalent && (taken.Contains(n.Id) || (begun.Contains(n.Branch!.Value) && stateOf(n) == NodeState.Open))).Select(n => n.Box)]
            : [.. Nodes.Where(n => !n.IsTalent || n.Branch is Branch.Fork or Branch.Pure || RowOf(_data.Talents[n.Id]) == TreeRow.First).Select(n => n.Box)];
    }

    /// <summary>
    /// The nearest node the keyboard reaches from <paramref name="from"/> in a direction: its centre further that way than
    /// across (a cone of 90°), the nearest first; null when there is none.
    /// </summary>
    public TreeNode? Neighbour(string from, Direction direction)
    {
        TreeNode start = _byId[from];
        TreeNode? best = null;
        float bestScore = float.MaxValue;
        foreach (TreeNode n in Nodes.Where(n => n.Id != from))
        {
            float dx = n.Box.CenterX - start.Box.CenterX, dy = n.Box.CenterY - start.Box.CenterY;
            (float along, float across) = direction switch
            {
                Direction.Up => (-dy, Math.Abs(dx)),
                Direction.Down => (dy, Math.Abs(dx)),
                Direction.Left => (-dx, Math.Abs(dy)),
                _ => (dx, Math.Abs(dy)),
            };
            if (along <= 0 || across > along)
                continue;
            // Straight ahead counts more than across.
            float score = along + (2 * across);
            if (score < bestScore)
                (best, bestScore) = (n, score);
        }
        return best;
    }

    /// <summary>
    /// The node the keyboard starts on (K4 a): the pair taken whose braid holds the most nodes, else the first pair taken,
    /// else a Heart taken, else the trunk.
    /// </summary>
    public string Start(IReadOnlyList<string> taken, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        int Braided(string pair) => taken.Count(id => data.Talents[id].Branch == Branch.Braids && (data.Talents[id].Requires ?? []).Contains(pair));
        string? pair = taken.Where(id => data.Talents[id].IsPair).OrderByDescending(Braided).FirstOrDefault();
        return pair ?? taken.FirstOrDefault(id => data.Talents[id].Branch == Branch.Pure) ?? $"trunk:{TrunkLevels[0]}";
    }

    /// <summary>The state a talent shows (L5 b), from the reason it would be refused (<see cref="TalentReasons.WhyNot"/>).</summary>
    public static NodeState State(IReadOnlyList<string> taken, string id, int level, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        if (taken.Contains(id))
            return NodeState.Taken;
        return TalentReasons.WhyNot(taken, id, level, data)?.Kind switch
        {
            null => NodeState.Open,
            RefusalKind.Opens or RefusalKind.Level => NodeState.Level,
            // Points are tested last: a node refused for them passes every other rule, it is reachable (docs/arbre-lisible.md
            // §2.6): open, the lack of points said once by the counter and by a click.
            RefusalKind.Points => NodeState.Open,
            RefusalKind.Door or RefusalKind.Parent => NodeState.Locked,
            _ => NodeState.Excluded,
        };
    }

    /// <summary>
    /// The look a state shows (sprint 164, É1, <c>docs/arbre-lisible.md</c> §2.6): four, from the most to the least salient;
    /// locked and out of level are the same answer for the player ("not yet"), the level's badge tells them apart.
    /// </summary>
    public static NodeLook Look(NodeState state) => state switch
    {
        NodeState.Taken => NodeLook.Taken,
        NodeState.Open => NodeLook.Open,
        NodeState.Excluded => NodeLook.Closed,
        _ => NodeLook.NotYet,
    };

    /// <summary>A node of the trunk: lit from its level on (B10 a: shown, greyed, before).</summary>
    public static NodeState TrunkState(int trunkLevel, int level) => level >= trunkLevel ? NodeState.Taken : NodeState.Level;
}
