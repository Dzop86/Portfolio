using System.Text.Json;

namespace Rpg.Core;

/// <summary>
/// Where a guild stands (sprint 156, D83): its level and the points toward the next, the points of this week against the
/// leader's goal, the leader's message and the crest. Nothing of it fights (question 79 a).
/// </summary>
public sealed record GuildStanding(int Level, long Points, long Into, long ToNext, int Week, int Goal, string? Message, GuildCrest Crest);

/// <summary>
/// A guild's crest (sprint 156, question 82 a): one shape of <see cref="GuildCrests.Shapes"/>, one pictogram of
/// <see cref="GuildCrests.Icons"/> and two colours of the charter (<see cref="GuildCrests.Colours"/>), the field's and the
/// charge's. Ids only: the art direction may redraw them without touching the code.
/// </summary>
public sealed record GuildCrest(string Shape, string Icon, string Field, string Charge);

/// <summary>One tab of a guild's chest: the stacks it holds (<see cref="GuildChests.TabSize"/> at most), and whether its members may take from it.</summary>
public sealed record GuildTab(bool Open, IReadOnlyList<ItemCount> Items);

/// <summary>One movement of the chest: who, when, which item, how many (taken out below zero), in which tab.</summary>
public sealed record GuildMove(DateTimeOffset At, string Who, string Item, int Count, int Tab);

/// <summary>A guild's chest at Edmée's (sprint 156): its tabs and the last movements, the latest first.</summary>
public sealed record GuildChest(IReadOnlyList<GuildTab> Tabs, IReadOnlyList<GuildMove> Journal);

/// <summary>What a member asks of the chest: an item and how many, in a tab (from 0).</summary>
public sealed record ChestOrder(string? Item, int Count, int Tab);

/// <summary>A tab opened to the members, or closed again, by the leader.</summary>
public sealed record ChestTabOrder(int Tab, bool Open);

/// <summary>The leader's message and the week's goal.</summary>
public sealed record GuildNotice(string? Message, int Goal);

/// <summary>The crests' parts: six shapes, thirty pictograms of game-icons.net, eight colours of the charter.</summary>
public static class GuildCrests
{
    public static readonly IReadOnlyList<string> Shapes = ["heater", "round", "square", "banner", "kite", "lozenge"];

    // The pictograms' ids, the drawings in godot/assets/icons/crest (CC BY 3.0, authors in its credits.json).
    public static readonly IReadOnlyList<string> Icons =
    [
        "oak-leaf", "pine-tree", "acorn", "wheat", "rose", "mushroom", "wolf-head", "boar", "stag-head", "bear-head",
        "owl", "eagle-head", "fox-head", "bee", "fish", "snake", "dragon-head", "crown", "castle", "key",
        "swords", "axe", "bow", "shield", "hammer", "anvil", "book", "lantern", "sun", "moon",
    ];

    /// <summary>The colours, by their token's name in <c>src/assets/tokens.css</c>.</summary>
    public static readonly IReadOnlyDictionary<string, string> Colours = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["pistache"] = "#bef374",
        ["pistache-pale"] = "#e6f8c9",
        ["bambou"] = "#4d7a1c",
        ["chocolat"] = "#5a3a22",
        ["chocolat-clair"] = "#c9a27f",
        ["gris-fond"] = "#1f1f1f",
        ["gris-texte"] = "#cccccc",
        ["osmose-cost"] = "#f2d36b",
    };

    /// <summary>A new guild's crest: a pistachio oak leaf on chocolate.</summary>
    public static readonly GuildCrest Default = new("heater", "oak-leaf", "chocolat", "pistache");

    /// <summary>Why a crest cannot be worn, or null: unknown parts, or the two colours alike.</summary>
    public static string? WhyNot(GuildCrest? crest)
    {
        return crest is null ? "No crest."
            : !Shapes.Contains(crest.Shape) ? "Unknown shape."
            : !Icons.Contains(crest.Icon) ? "Unknown pictogram."
            : !Colours.ContainsKey(crest.Field) || !Colours.ContainsKey(crest.Charge) ? "Unknown colour."
            : crest.Field == crest.Charge ? "The field and the pictogram need two colours."
            : null;
    }

    /// <summary>The crest as the server keeps it: its four ids parted by slashes.</summary>
    public static string Write(GuildCrest crest)
    {
        ArgumentNullException.ThrowIfNull(crest);
        return $"{crest.Shape}/{crest.Icon}/{crest.Field}/{crest.Charge}";
    }

    /// <summary>A kept crest; the default when empty or no longer valid (a part the art direction withdrew).</summary>
    public static GuildCrest Read(string? kept)
    {
        string[] parts = (kept ?? "").Split('/');
        GuildCrest? crest = parts.Length == 4 ? new GuildCrest(parts[0], parts[1], parts[2], parts[3]) : null;
        return WhyNot(crest) is null ? crest! : Default;
    }
}

/// <summary>
/// A guild's level and week (sprint 156, D83): points from its members' contracts (10, a group's 20), achievements (5) and
/// dungeons finished by two members or more (30); from level n to n + 1, 150 × n points, up to 10 (6 750 in all).
/// </summary>
public static partial class GuildRules
{
    public const int TopLevel = 10;
    public const int AchievementPoints = 5;
    public const int DungeonPoints = 30;
    public const int DefaultGoal = 120;
    public const int MostGoal = 5000;
    public const int MessageLength = 200;

    /// <summary>The points a contract handed back brings its member's guild.</summary>
    public static int ContractPoints(bool group) => group ? 20 : 10;

    /// <summary>The points from level <paramref name="level"/> to the next.</summary>
    public static long Cost(int level) => 150L * level;

    /// <summary>The level of a guild of so many points, from 1 to <see cref="TopLevel"/>.</summary>
    public static int LevelFor(long points) => Progress(points).Level;

    /// <summary>The level, the points into it and those it costs (0 at the top).</summary>
    public static (int Level, long Into, long ToNext) Progress(long points)
    {
        int level = 1;
        while (level < TopLevel && points >= Cost(level))
            points -= Cost(level++);
        return level == TopLevel ? (level, 0, 0) : (level, points, Cost(level));
    }

    /// <summary>The guild's week at a moment: from Monday's dawn (that of the contracts) to the next.</summary>
    public static int Week(DateTimeOffset now) => (int)Math.Floor((Contracts.Day(now) + 3) / 7.0);

    /// <summary>Why one of that rank cannot leave that message and goal, or null: the leader only, at most 200 characters, a goal of 1 to 5 000.</summary>
    public static string? WhyNotNotice(GuildRank by, string? message, int goal)
    {
        return by != GuildRank.Leader ? "Only the leader writes the guild's message and goal."
            : (message?.Length ?? 0) > MessageLength ? $"A message of {MessageLength} characters at most."
            : goal is < 1 or > MostGoal ? $"A goal of 1 to {MostGoal} points."
            : null;
    }

    /// <summary>Why one of that rank cannot change the crest, or null: the leader only.</summary>
    public static string? WhyNotCrest(GuildRank by, GuildCrest? crest) =>
        by != GuildRank.Leader ? "Only the leader changes the crest." : GuildCrests.WhyNot(crest);

    /// <summary>The standing told to the members.</summary>
    public static GuildStanding Standing(long points, int weekPoints, int week, int now, int goal, string? message, GuildCrest crest)
    {
        (int level, long into, long toNext) = Progress(points);
        return new GuildStanding(level, points, into, toNext, week == now ? weekPoints : 0, goal, message, crest);
    }
}

/// <summary>
/// The guild's chest at Edmée's (sprint 156, question 81 a): tabs of 40 stacks, one at level 1, two at 3, three at 6, four
/// at 9; everyone deposits, the leader and the officers take out, the members also from a tab the leader opened to them; the
/// 100 last movements kept.
/// </summary>
public static class GuildChests
{
    public const int TabSize = 40;
    public const int JournalSize = 100;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The tabs a guild of that level has.</summary>
    public static int TabsAt(int level) => level >= 9 ? 4 : level >= 6 ? 3 : level >= 3 ? 2 : 1;

    /// <summary>The chest with as many tabs as the level gives (a new one closed and empty).</summary>
    public static GuildChest Sized(GuildChest chest, int level)
    {
        ArgumentNullException.ThrowIfNull(chest);
        int tabs = Math.Max(chest.Tabs.Count, TabsAt(level));
        return chest with { Tabs = [.. chest.Tabs, .. Enumerable.Range(0, tabs - chest.Tabs.Count).Select(_ => new GuildTab(false, []))] };
    }

    public static string Write(GuildChest chest) => JsonSerializer.Serialize(chest, Json);

    /// <summary>A kept chest, sized to the level; an empty one when nothing is kept.</summary>
    public static GuildChest Read(string? kept, int level) =>
        Sized(string.IsNullOrEmpty(kept) ? new GuildChest([], []) : JsonSerializer.Deserialize<GuildChest>(kept, Json)!, level);

    /// <summary>Why one may not put so many of that item in that tab, owning <paramref name="owned"/>, or null.</summary>
    public static string? WhyNotDeposit(GuildChest chest, ChestOrder? order, int owned, GameData data)
    {
        ArgumentNullException.ThrowIfNull(chest);
        ArgumentNullException.ThrowIfNull(data);
        return order is null || order.Tab < 0 || order.Tab >= chest.Tabs.Count ? "No such tab."
            : order.Item is null || !data.Items.ContainsKey(order.Item) ? "No such item."
            : order.Count < 1 ? "At least one."
            : owned < order.Count ? "Not that many in the bag."
            : chest.Tabs[order.Tab].Items.All(i => i.Item != order.Item) && chest.Tabs[order.Tab].Items.Count >= TabSize ? $"The tab is full ({TabSize})."
            : null;
    }

    /// <summary>Whether one of that rank may take out of that tab at all: the leader and the officers, a member from a tab opened to it.</summary>
    public static bool MayTake(GuildChest chest, int tab, GuildRank by)
    {
        ArgumentNullException.ThrowIfNull(chest);
        return tab >= 0 && tab < chest.Tabs.Count && (by != GuildRank.Member || chest.Tabs[tab].Open);
    }

    /// <summary>Why one of that rank may not take so many of that item out of that tab, or null.</summary>
    public static string? WhyNotWithdraw(GuildChest chest, ChestOrder? order, GuildRank by)
    {
        ArgumentNullException.ThrowIfNull(chest);
        return order is null || order.Tab < 0 || order.Tab >= chest.Tabs.Count ? "No such tab."
            : !MayTake(chest, order.Tab, by) ? "Only the leader and the officers take out of this tab."
            : order.Count < 1 ? "At least one."
            : chest.Tabs[order.Tab].Items.FirstOrDefault(i => i.Item == order.Item)?.Count is not int held || held < order.Count ? "Not that many in the tab."
            : null;
    }

    /// <summary>Why one of that rank may not open or close a tab to the members, or null.</summary>
    public static string? WhyNotOpen(GuildChest chest, ChestTabOrder? order, GuildRank by)
    {
        ArgumentNullException.ThrowIfNull(chest);
        return by != GuildRank.Leader ? "Only the leader opens a tab to the members."
            : order is null || order.Tab < 0 || order.Tab >= chest.Tabs.Count ? "No such tab."
            : null;
    }

    /// <summary>
    /// The chest after a movement (<paramref name="count"/> below zero: taken out), checked by the caller: the stack grown,
    /// shrunk or gone, the movement first in the journal, which keeps <see cref="JournalSize"/>.
    /// </summary>
    public static GuildChest Move(GuildChest chest, int tab, string item, int count, string who, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(chest);
        GuildTab t = chest.Tabs[tab];
        int held = t.Items.FirstOrDefault(i => i.Item == item)?.Count ?? 0;
        List<ItemCount> items = held == 0 ? [.. t.Items, new ItemCount(item, count)]
            : [.. t.Items.Select(i => i.Item == item ? i with { Count = held + count } : i).Where(i => i.Count > 0)];
        var tabs = chest.Tabs.ToList();
        tabs[tab] = t with { Items = items };
        return new GuildChest(tabs, [new GuildMove(at, who, item, count, tab), .. chest.Journal.Take(JournalSize - 1)]);
    }

    /// <summary>The chest with a tab opened to the members, or closed.</summary>
    public static GuildChest Open(GuildChest chest, int tab, bool open)
    {
        ArgumentNullException.ThrowIfNull(chest);
        var tabs = chest.Tabs.ToList();
        tabs[tab] = tabs[tab] with { Open = open };
        return chest with { Tabs = tabs };
    }
}

/// <summary>The chest as one member sees it (sprint 156): its tabs and journal, the member's rank (what it may take), and its bag after a movement.</summary>
public sealed record ChestView(GuildChest Chest, GuildRank Rank, CharacterSummary? Character = null);
