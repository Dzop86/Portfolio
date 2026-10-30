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

/// <summary>
/// A node as the window places it: a talent (its id), a node of the trunk (<c>trunk:30</c>), a floor of a folded style
/// (<c>fold:Melee:Second</c>, a click deploys the style). The braids still to come are no longer drawn (sprint 164, É1).
/// </summary>
public sealed record TreeNode(string Id, Box Box, NodeShape Shape, Branch? Branch = null, Tier? Tier = null)
{
    public bool IsTalent => !Id.Contains(':', StringComparison.Ordinal);

    public bool IsFold => Id.StartsWith("fold:", StringComparison.Ordinal);

    public bool IsTrunk => Id.StartsWith("trunk:", StringComparison.Ordinal);
}

/// <summary>A way the keyboard moves from node to node.</summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

/// <summary>
/// The tree's window as it lays its nodes out, without any engine (sprint 148, D82, K1 d after the mock-up of sprint 146):
/// read from the bottom up, the trunk, the fork (the three couples of pairs side by side, C7, then the Hearts of Pure
/// element), the Lightning braid over its pair and a dotted stub over the others, then the six styles; the style looked
/// at is deployed, each floor with all its nodes, the others folded to one node a floor. Nodes are 56 px, doors and keys
/// 72 (L2 a), never under 44; none leaves the area, none overlaps another.
/// </summary>
public sealed class TreeLayout
{
    public const float Small = 56, Large = 72, Label = 20;

    /// <summary>The styles from left to right: Melee and Distance side by side, their exclusions read at a glance.</summary>
    public static readonly IReadOnlyList<Branch> Styles = [Branch.Melee, Branch.Distance, Branch.Support, Branch.Terrain, Branch.Tempo, Branch.Alchemy];

    /// <summary>The fork's pairs, the complementary couples side by side (C7): Lightning and Mud, Ice and Magma, Steam and Sand.</summary>
    public static readonly IReadOnlyList<string> Pairs = ["electrifying", "bogging", "chilling", "cracking", "fogging", "blinding"];

    /// <summary>The trunk's levels (B1 a), bottom left to right.</summary>
    public static readonly IReadOnlyList<int> TrunkLevels = [30, 35, 40];

    private static readonly Tier[] Floors = [Tier.First, Tier.Second, Tier.Key];
    private const float Gap = 12, ChoiceGap = 8, Braid = (2 * Small) + Gap;
    private readonly Dictionary<string, TreeNode> _byId;

    public TreeLayout(GameData data, Branch focus, float width, float height)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (!Styles.Contains(focus))
            throw new ArgumentException($"{focus} is not a style", nameof(focus));
        (Focus, Width, Height) = (focus, width, height);
        var nodes = new List<TreeNode>();

        // The trunk, at the bottom, three nodes in a row under the fork's middle.
        float trunkY = height - Small - Label - 4;
        float forkY = trunkY - 34 - Large - Label;
        float forkLeft = (Braid - Large) / 2;
        string[] hearts = [.. data.Talents.Values.Where(t => t.Branch == Branch.Pure).Select(t => t.Id).Order(StringComparer.Ordinal)];
        float forkWidth = (Pairs.Count * Large) + (3 * Gap) + (hearts.Length * Small) + ((hearts.Length - 1) * ChoiceGap);
        float between = Math.Clamp((width - forkLeft - forkWidth) / 3, 24, 96);
        float x = forkLeft;
        for (int i = 0; i < Pairs.Count; i++)
        {
            nodes.Add(new(Pairs[i], new Box(x, forkY, Large, Large), NodeShape.Door, Branch.Fork, Tier.First));
            x += Large + (i % 2 == 0 ? Gap : between);
        }
        float heartY = forkY + ((Large - Small) / 2);
        foreach (string heart in hearts)
        {
            nodes.Add(new(heart, new Box(x, heartY, Small, Small), NodeShape.Door, Branch.Pure, Tier.First));
            x += Small + ChoiceGap;
        }
        ForkRight = x - ChoiceGap;
        float middle = (forkLeft + ForkRight) / 2, spread = Math.Min(170, (width - Small) / 3);
        for (int i = 0; i < TrunkLevels.Count; i++)
            nodes.Add(new($"trunk:{TrunkLevels[i]}", new Box(middle + ((i - 1) * spread) - (Small / 2), trunkY, Small, Small), NodeShape.Trunk));
        ForkTop = forkY;

        // The floors over the fork, bottom up: first, second, key.
        var rowY = new Dictionary<Tier, float>();
        // Room under the first floor for the stubs of the braids to come.
        float y = forkY - 56;
        foreach (Tier tier in Floors)
        {
            float size = tier == Tier.Key ? Large : Small;
            y -= size + Label;
            rowY[tier] = y;
            // Room over each floor for the captions of the choices (the Transmutation).
            y -= 28;
        }
        Top = rowY[Tier.Key] - 26;

        // The Lightning braid over its pair: the Transmutation's choice, then the second floor, then the Conversion.
        foreach (Tier tier in Floors)
            Row(nodes, [.. data.Talents.Values.Where(t => t.Branch == Branch.Braids && t.Tier == tier).Select(t => t.Id)], 0, Braid, rowY[tier], tier, Branch.Braids, ChoiceGap);

        // The styles, right of the braid: the one looked at deployed, the others folded to one node a floor.
        float left = Braid + 36;
        float Wide(Branch b) => b == focus
            ? Math.Max(Large, Floors.Max(t => Count(data, b, t) is int n and > 0 ? (n * (t == Tier.Key ? Large : Small)) + ((n - 1) * Gap) : 0))
            : Large;
        float used = Styles.Sum(Wide);
        float space = Math.Max(16, (width - left - used) / (Styles.Count - 1));
        x = left;
        foreach (Branch style in Styles)
        {
            float w = Wide(style);
            Columns[style] = new Box(x, Top, w, rowY[Tier.First] + Small + Label - Top);
            foreach (Tier tier in Floors)
            {
                if (style == focus)
                    Row(nodes, [.. data.Talents.Values.Where(t => t.Branch == style && t.Tier == tier).Select(t => t.Id)], x, w, rowY[tier], tier, style, Gap);
                else if (Count(data, style, tier) > 0)
                {
                    float size = tier == Tier.Key ? Large : Small;
                    nodes.Add(new($"fold:{style}:{tier}", new Box(x + ((w - size) / 2), rowY[tier], size, size), tier == Tier.Key ? NodeShape.Key : NodeShape.Node, style, tier));
                }
            }
            x += w + space;
        }
        Nodes = nodes;
        _byId = nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
    }

    public Branch Focus { get; }

    public float Width { get; }

    public float Height { get; }

    /// <summary>The top of the fork's doors: where a style's first floor reaches down to when no door is taken.</summary>
    public float ForkTop { get; }

    /// <summary>The right edge of the fork's last Heart.</summary>
    public float ForkRight { get; }

    /// <summary>The top of the styles' names.</summary>
    public float Top { get; }

    /// <summary>Each style's column, its name above it.</summary>
    public Dictionary<Branch, Box> Columns { get; } = [];

    public IReadOnlyList<TreeNode> Nodes { get; }

    public TreeNode? this[string id] => _byId.GetValueOrDefault(id);

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
    /// The links of the tree, parent to child, between the nodes laid out. A style's first floor asks for any door: it is
    /// linked to the doors taken only (with none, the window draws a dotted stub down to the fork). A folded style links
    /// its floors one above the other.
    /// </summary>
    public IEnumerable<(string From, string To)> Links(IReadOnlyList<string> taken, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        string[] doors = [.. taken.Where(id => data.Talents[id].IsFork)];
        foreach (TreeNode n in Nodes)
        {
            if (n.IsFold)
            {
                if (n.Tier == Tier.First)
                {
                    foreach (string door in doors.Where(d => n.Branch != Branch.Alchemy || data.Talents[d].IsPair))
                        yield return (door, n.Id);
                }
                else if (_byId.ContainsKey($"fold:{n.Branch}:{(n.Tier == Tier.Key ? Tier.Second : Tier.First)}"))
                    yield return ($"fold:{n.Branch}:{(n.Tier == Tier.Key ? Tier.Second : Tier.First)}", n.Id);
                else if (n.Tier == Tier.Key && _byId.ContainsKey($"fold:{n.Branch}:{Tier.First}"))
                    yield return ($"fold:{n.Branch}:{Tier.First}", n.Id);
                continue;
            }
            if (!n.IsTalent)
                continue;
            TalentSpec t = data.Talents[n.Id];
            foreach (string parent in (t.Requires ?? []).Concat(t.RequiresAny ?? []).Distinct(StringComparer.Ordinal))
            {
                bool door = data.Talents[parent].IsFork && t.Branch != Branch.Braids;
                if (_byId.ContainsKey(parent) && (!door || doors.Contains(parent)))
                    yield return (parent, n.Id);
            }
        }
    }

    /// <summary>
    /// The style the window opens on (K4 a): the one with the most nodes taken, the first of <see cref="Styles"/> on a tie
    /// or with none.
    /// </summary>
    public static Branch FocusOf(IReadOnlyList<string> taken, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        int Taken(Branch b) => taken.Count(id => data.Talents.TryGetValue(id, out TalentSpec? t) && t.Branch == b);
        return Styles.OrderByDescending(Taken).First();
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

    /// <summary>A folded floor shows the best of its nodes: taken if one is, open if one can be taken, else locked (or out of level).</summary>
    public static NodeState FoldState(IReadOnlyList<string> taken, Branch style, Tier tier, int level, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        NodeState[] states = [.. data.Talents.Values.Where(t => t.Branch == style && t.Tier == tier).Select(t => State(taken, t.Id, level, data))];
        return states.Contains(NodeState.Taken) ? NodeState.Taken
            : states.Contains(NodeState.Open) ? NodeState.Open
            : states.All(s => s == NodeState.Level) ? NodeState.Level
            : states.All(s => s == NodeState.Excluded) ? NodeState.Excluded
            : NodeState.Locked;
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

    private static int Count(GameData data, Branch b, Tier t) => data.Talents.Values.Count(x => x.Branch == b && x.Tier == t);

    private static void Row(List<TreeNode> nodes, string[] ids, float left, float width, float y, Tier tier, Branch branch, float gap)
    {
        float size = tier == Tier.Key ? Large : Small;
        float row = (ids.Length * size) + (Math.Max(0, ids.Length - 1) * gap);
        float x = left + ((width - row) / 2);
        foreach (string id in ids.Order(StringComparer.Ordinal))
        {
            nodes.Add(new(id, new Box(x, y, size, size), tier == Tier.Key ? NodeShape.Key : NodeShape.Node, branch, tier));
            x += size + gap;
        }
    }
}
