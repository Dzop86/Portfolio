using System.Globalization;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>Where a tile of the Specialisation page stands for this hero (docs/maquette-specialisation.md §6).</summary>
public enum SpecState
{
    /// <summary>The level has not opened it, and no road of the hero's leads there.</summary>
    Locked,

    /// <summary>It can be taken now.</summary>
    Open,

    /// <summary>On the hero's road, reached.</summary>
    Chosen,

    /// <summary>The hero's road goes elsewhere.</summary>
    Crossed,

    /// <summary>On the hero's road, its level still to come.</summary>
    Ahead,

    /// <summary>Open to the level, but something comes first: the quest, or its branch.</summary>
    Waiting,
}

/// <summary>
/// A tile of the tree: a branch at 30, a lineage's evolution at 50 or 100, or a final class at 100 (sprint 196, maquette §14). <see cref="Key"/> is the branch's id, the
/// lineage's, the lineage's followed by "@100", or the lineage's and the final class's, "lineage:final".
/// </summary>
public sealed record SpecNode(string Key, string Spec, string? Fork, SpecTier Tier, LocalizedText? Lore, string? Icon, SpecState State, bool Head, string? Final = null,
    FinalTemplate? Template = null)
{
    public int Level => Tier.Level;
}

/// <summary>What a tile's button would do: its words, whether it can, why not; the road it writes, its price.</summary>
public sealed record SpecAction(string Text, bool Enabled, string Tip, string Spec, string? Fork, long Cost, bool First, string? Final = null);

/// <summary>The foot of the panel: two lines, and a button or none (§7.3).</summary>
public sealed record SpecFoot(string First, string Second, SpecAction? Action);

/// <summary>A line of the confirmation: an evolution received.</summary>
public sealed record SpecGift(string Title, string Line, string? Icon);

/// <summary>The confirmation of a choice or of a crossing-out (§5.6).</summary>
public sealed record SpecConfirm(string Title, string? Leaves, string Section, IReadOnlyList<SpecGift> Gifts, IReadOnlyList<string> Ranks, string Price, string? Later, string? Deck, string Yes, string No, bool First);

/// <summary>What a link of the tree looks like (§5.3).</summary>
public enum SpecWood
{
    /// <summary>Both ends chosen: living wood and sap.</summary>
    Alive,

    /// <summary>It leads to a tile that can be taken.</summary>
    Open,

    /// <summary>On the hero's road, not reached yet.</summary>
    Bare,

    /// <summary>All the rest.</summary>
    Out,
}

/// <summary>What the page reads of the character beyond its points; the server checks it all again.</summary>
public sealed record SpecContext(bool QuestDone, long Shards, bool InVillage)
{
    /// <summary>A character as the server sent it.</summary>
    public static SpecContext Of(CharacterSummary character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return new SpecContext((character.Quests ?? []).Contains(Specs.Quest), character.Shards, Shop.InVillage(character.Place?.Town));
    }
}

/// <summary>
/// The Specialisation page (sprint 191, D95, docs/maquette-specialisation.md), everything but the drawing: the fifteen tiles and their states, the foot of the panel, the
/// confirmation, the links. <paramref name="preview"/> is a branch looked at, from level 50, before naming its lineage: it is never sent nor kept. <paramref name="open"/>
/// is the branch whose column 100 shows (sprint 196, maquette §14.1): the one last clicked; by default the one looked at, the hero's, or the first.
/// </summary>
public sealed class SpecPage(PointsEditor editor, SpecContext context, string? preview = null, string? open = null)
{
    private readonly PointsEditor _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    private readonly SpecContext _context = context ?? throw new ArgumentNullException(nameof(context));

    private Hero Hero => _editor.Hero;

    private GameData Data => _editor.Data;

    /// <summary>The class's branches, in the order of the data, which is fixed.</summary>
    public IReadOnlyList<Specialisation> Branches => [.. Data.Specs.Values.Where(s => s.Class == Hero.Class)];

    /// <summary>A branch whose content exists: before S192 and S193, the Guard's and the Mage's are read but not taken.</summary>
    public static bool Playable(Specialisation branch) => (branch ?? throw new ArgumentNullException(nameof(branch))).Base.Spells is { Count: > 0 };

    /// <summary>
    /// The branch whose column 100 shows (§14.1): the one clicked, the one looked at, the hero's, or the first; "" for a class whose branches are not yet
    /// written (the Priest of sprint 201).
    /// </summary>
    public string OpenBranch => new[] { open, preview, Hero.Spec }.FirstOrDefault(id => id is not null && Branches.Any(b => b.Id == id)) ?? (Branches.Count > 0 ? Branches[0].Id : "");

    /// <summary>The tiles, branch by branch: its own, then each lineage's at 50; then the column 100 of the open branch (§14.1).</summary>
    public IReadOnlyList<SpecNode> Nodes =>
        [.. Branches.SelectMany(s => new[] { Node(s, null, s.Base) }.Concat(s.Forks.SelectMany(f => f.Tiers.Where(t => t.Level == Specs.Forks).Select(t => Node(s, f, t))))), .. Last];

    /// <summary>The column 100 (§14.2): for each lineage of the open branch, its two final classes, or its one evolution.</summary>
    public IReadOnlyList<SpecNode> Last
    {
        get
        {
            Specialisation branch = Data.Specs[OpenBranch];
            return [.. branch.Forks.SelectMany(f => f.Finals is { Count: > 0 } finals
                ? finals.Select(x => FinalNode(branch, f, x))
                : f.Tiers.Where(t => t.Level == Specs.Finals).Select(t => Node(branch, f, t)))];
        }
    }

    public SpecNode this[string key] => Nodes.Single(n => n.Key == key);

    /// <summary>The tile of this key, if the page shows it: a column 100 follows its branch.</summary>
    public SpecNode? Find(string? key) => Nodes.FirstOrDefault(n => n.Key == key);

    private SpecNode FinalNode(Specialisation branch, SpecFork fork, SpecFinal final)
    {
        SpecState state = FinalState(branch, fork, final);
        // The sister of the final class taken: a fourth pencil stroke at most (§14.5).
        bool head = state == SpecState.Crossed && branch.Id == Hero.Spec && fork.Id == Hero.Fork;
        return new SpecNode($"{fork.Id}:{final.Id}", branch.Id, fork.Id, final.Tier, final.Lore ?? fork.Lore ?? branch.Lore, final.Icon ?? fork.Icon ?? branch.Icon, state, head,
            final.Id, final.Template);
    }

    // In the order of §14.5.
    private SpecState FinalState(Specialisation branch, SpecFork fork, SpecFinal final)
    {
        bool mine = branch.Id == Hero.Spec, lineage = mine && fork.Id == Hero.Fork;
        if (lineage && Hero.Final == final.Id && Hero.Level >= Specs.Finals)
            return SpecState.Chosen;
        if (lineage && Hero.Level < Specs.Finals)
            return SpecState.Ahead;
        bool previewed = branch.Id == preview && Hero.Level >= Specs.Finals;
        if (!previewed && ((lineage && Hero.Final is not null) || (Hero.Spec is not null && !mine) || (mine && Hero.Fork is not null && !lineage)))
            return SpecState.Crossed;
        if (Hero.Level < Specs.Finals)
            return SpecState.Locked;
        return _context.QuestDone && (lineage || (mine && Hero.Fork is null) || previewed) ? SpecState.Open : SpecState.Waiting;
    }

    /// <summary>The hero's lineage ends in final classes, its level opens them, and it has none (§14.9).</summary>
    public bool FinalDue => Hero.Level >= Specs.Finals && Hero.Final is null && Specs.ForkOf(Hero, Data)?.Finals is { Count: > 0 };

    // A final class can be had on the hero's way (§14.9): its lineage, else its branch, else its class, has some.
    private bool FinalPossible
    {
        get
        {
            IEnumerable<SpecFork> forks = Data.Specs.GetValueOrDefault(Hero.Spec ?? "") is Specialisation mine
                ? (mine.ForkOf(Hero.Fork) is SpecFork fork ? [fork] : mine.Forks)
                : Branches.SelectMany(b => b.Forks);
            return forks.Any(f => f.Finals is { Count: > 0 });
        }
    }

    private SpecNode Node(Specialisation branch, SpecFork? fork, SpecTier tier)
    {
        string key = fork is null ? branch.Id : tier.Level == Specs.Forks ? fork.Id : $"{fork.Id}@{tier.Level.ToString(CultureInfo.InvariantCulture)}";
        SpecState state = StateOf(branch, fork, tier.Level);
        // The head of a road left: the two other branches, the sister lineage; three pencil strokes at most (§5.4).
        bool head = state == SpecState.Crossed && (fork is null || (branch.Id == Hero.Spec && tier.Level == Specs.Forks));
        return new SpecNode(key, branch.Id, fork?.Id, tier, fork?.Lore ?? branch.Lore, fork?.Icon ?? branch.Icon, state, head);
    }

    // In the order of §6.
    private SpecState StateOf(Specialisation branch, SpecFork? fork, int level)
    {
        bool mine = branch.Id == Hero.Spec, onRoad = mine && (fork is null || fork.Id == Hero.Fork);
        if (onRoad)
            return Hero.Level >= level ? SpecState.Chosen : SpecState.Ahead;
        bool previewed = fork is not null && branch.Id == preview && Hero.Level >= level;
        if (Hero.Spec is not null && (!mine || Hero.Fork is not null) && !previewed)
            return SpecState.Crossed;
        if (Hero.Level < level)
            return SpecState.Locked;
        if (_context.QuestDone)
        {
            if (fork is null)
            {
                if (preview is null || preview == branch.Id)
                    return SpecState.Open;
            }
            else if (mine || previewed)
            {
                return SpecState.Open;
            }
        }
        return SpecState.Waiting;
    }

    /// <summary>Whether a choice waits: a road possible and not taken, or a lineage due: the point on the tab (§5.7).</summary>
    public bool Pending => Branches.Any(Playable) && !_editor.ReadOnly
        && ((Hero.Spec is null && Hero.Level >= Specs.Opens && _context.QuestDone) || (Hero.Spec is not null && Hero.Fork is null && Hero.Level >= Specs.Forks) || FinalDue);

    /// <summary>The choices made and possible at this level (§5.1, §14.9): (0, 0) before 30; a third at 100 when a final class can be had.</summary>
    public (int Done, int Total) Choices
    {
        get
        {
            int total = Hero.Level < Specs.Opens ? 0 : Hero.Level < Specs.Forks ? 1 : Hero.Level < Specs.Finals || !FinalPossible ? 2 : 3;
            return ((Hero.Spec is null ? 0 : 1) + (Hero.Fork is null ? 0 : 1) + (Hero.Final is null || total < 3 ? 0 : 1), total);
        }
    }

    /// <summary>Whether the open branch ends in final classes: the frieze's cell 100 says so (§14.9).</summary>
    public bool OpenHasFinals => Data.Specs.TryGetValue(OpenBranch, out Specialisation? open) && open.Forks.Any(f => f.Finals is { Count: > 0 });

    /// <summary>The tiers, for the frieze: reached, or the next to come.</summary>
    public IReadOnlyList<ElementTier> Tiers
    {
        get
        {
            int? next = Specs.TierLevels.Where(l => l > Hero.Level).Cast<int?>().FirstOrDefault();
            return [.. Specs.TierLevels.Select(l => new ElementTier(l, Hero.Level >= l, l == next))];
        }
    }

    /// <summary>The tile the panel opens on (§5.5): the first lineage due, or the last evolution reached, or the first branch.</summary>
    public string Opening
    {
        get
        {
            if (Data.Specs.TryGetValue(Hero.Spec ?? "", out Specialisation? mine))
            {
                if (Hero.Fork is null)
                    return Hero.Level >= Specs.Forks ? mine.Forks[0].Id : mine.Id;
                if (Hero.Level < Specs.Finals)
                    return Hero.Fork;
                // At 100 (§14.6): the final class taken, or the first to take.
                return mine.ForkOf(Hero.Fork) is { Finals: { Count: > 0 } finals } fork ? $"{fork.Id}:{(fork.FinalOf(Hero.Final) ?? finals[0]).Id}" : $"{Hero.Fork}@100";
            }
            return Branches.Count > 0 ? Branches[0].Id : "";
        }
    }

    /// <summary>Thousands the way the game's prices write them: a non-breaking space in French, a comma in English.</summary>
    public static string Price(long amount, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        return amount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", texts.Lang == "fr" ? " " : ",", StringComparison.Ordinal);
    }

    private long BranchPrice => 30L * Hero.Level;

    private long ForkPrice => 10L * Hero.Level;

    private long FinalPrice => 5L * Hero.Level;

    /// <summary>The foot of the panel for a tile (§7.3).</summary>
    public SpecFoot Foot(SpecNode node, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(texts);
        string level = node.Level.ToString(CultureInfo.InvariantCulture);
        string opens = texts["spec.opens", level];
        if (!_context.QuestDone && Hero.Spec is null)
            return new SpecFoot(Hero.Level < node.Level ? opens : texts["spec.quest"], "", null);
        string changeFork = texts["spec.price.fork", Price(ForkPrice, texts)], changeBranch = texts["spec.price.branch", Price(BranchPrice, texts)];
        if (node.Final is not null)
            return FinalFoot(node, texts, opens, changeFork, changeBranch);
        switch (node.State)
        {
            case SpecState.Locked:
                return new SpecFoot(opens, "", null);
            case SpecState.Waiting:
                return new SpecFoot(texts[_context.QuestDone ? "spec.branch-first" : "spec.quest"], "", null);
            case SpecState.Ahead:
                int left = node.Level - Hero.Level;
                return new SpecFoot(left == 1 ? texts["spec.ahead.one"] : texts["spec.ahead", left], texts["spec.ahead.alone"], null);
            case SpecState.Chosen when node.Fork is not null && FinalDue:
                return new SpecFoot(texts["spec.final.due"], "", null);
            case SpecState.Chosen:
                return Hero.Fork is null ? new SpecFoot(Hero.Level >= Specs.Forks ? texts["spec.splits"] : changeBranch, "", null) : new SpecFoot(changeFork, changeBranch, null);
            case SpecState.Crossed when Hero.Level < node.Level:
                return new SpecFoot(opens, node.Level == 100 ? texts["spec.pick-at-50"] : "", null);
            case SpecState.Crossed when node.Fork is null && Hero.Level >= Specs.Forks:
                return new SpecFoot(texts["spec.needs-fork.paid"], changeBranch, null);
            case SpecState.Open when node.Fork is null && Hero.Level >= Specs.Forks:
                return new SpecFoot(texts["spec.needs-fork"], texts["spec.free"], null);
        }
        // What is left has a button: a tile open, or crossed with a road to it.
        long cost = Specs.ChangeCost(Hero, node.Spec, node.Fork);
        SpecAction action = Action(node, cost, texts);
        if (cost == 0)
        {
            return node.Fork is null
                ? new SpecFoot(texts["spec.free"], texts["spec.then-cost", Price(BranchPrice, texts)], action)
                : new SpecFoot(texts["spec.free"], texts["spec.at-once"], action);
        }
        return node.Spec == Hero.Spec ? new SpecFoot(changeFork, texts["spec.rank.one"], action) : new SpecFoot(changeBranch, texts["spec.rank.many"], action);
    }

    // A final class's foot (§14.7): its price is 5 × the level; another lineage's or branch's brings it along at their price.
    private SpecFoot FinalFoot(SpecNode node, Texts texts, string opens, string changeFork, string changeBranch)
    {
        string changeFinal = texts["spec.price.final", Price(FinalPrice, texts)];
        switch (node.State)
        {
            case SpecState.Locked:
            case SpecState.Crossed when Hero.Level < node.Level:
                return new SpecFoot(opens, texts["spec.final.pick"], null);
            case SpecState.Waiting:
                return new SpecFoot(texts[_context.QuestDone ? "spec.branch-first" : "spec.quest"], "", null);
            case SpecState.Ahead:
                int left = node.Level - Hero.Level;
                return new SpecFoot(left == 1 ? texts["spec.ahead.one"] : texts["spec.ahead", left], texts["spec.final.ahead"], null);
            case SpecState.Chosen:
                return new SpecFoot(changeFinal, changeFork, null);
        }
        long cost = Specs.ChangeCost(Hero, node.Spec, node.Fork, node.Final);
        SpecAction action = Action(node, cost, texts);
        if (cost == 0)
            return new SpecFoot(texts["spec.free"], node.Spec == Hero.Spec && node.Fork == Hero.Fork ? texts["spec.final.then-cost", Price(FinalPrice, texts)] : texts["spec.at-once"], action);
        return node.Spec != Hero.Spec ? new SpecFoot(changeBranch, texts["spec.final.with-branch"], action)
            : node.Fork != Hero.Fork ? new SpecFoot(changeFork, texts["spec.final.with-fork"], action)
            : new SpecFoot(changeFinal, texts["spec.final.same"], action);
    }

    // The button keeps its place and size when it cannot act, and says why, by priority (§7.4).
    private SpecAction Action(SpecNode node, long cost, Texts texts)
    {
        bool first = cost == 0;
        string words = first ? texts[node.Final is not null ? "spec.take.final" : node.Fork is null ? "spec.take" : "spec.take.fork"] : texts["spec.cross", Price(cost, texts)];
        SpecAction Refused(string text, string tip) => new(text, false, tip, node.Spec, node.Fork, cost, first, node.Final);
        if (!Playable(Data.Specs[node.Spec]))
            return Refused(texts["spec.soon"], "");
        if (_editor.Changed)
            return Refused(texts["spec.need.save"], texts["spec.need.save.tip"]);
        if (!first && !_context.InVillage)
            return Refused(texts["spec.need.village"], texts["spec.need.village.tip"]);
        if (cost > _context.Shards)
            return Refused(texts["spec.need.shards", Price(cost - _context.Shards, texts)], texts["spec.need.shards.tip", Price(cost, texts)]);
        return new SpecAction(words, true, "", node.Spec, node.Fork, cost, first, node.Final);
    }

    /// <summary>The road from the root to a tile, as keys of tiles.</summary>
    public static IReadOnlyList<string> RoadTo(SpecNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Fork is null)
            return [node.Key];
        return node.Level == Specs.Forks ? [node.Spec, node.Fork] : [node.Spec, node.Fork, node.Key];
    }

    /// <summary>A link's look, by the tile it leads to: §5.3.</summary>
    public SpecWood WoodTo(SpecNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.State switch
        {
            SpecState.Chosen => SpecWood.Alive,
            SpecState.Open => SpecWood.Open,
            SpecState.Ahead => SpecWood.Bare,
            // A lineage of its own branch, under 50: on its road, not reached.
            SpecState.Locked when node.Spec == Hero.Spec => SpecWood.Bare,
            _ => SpecWood.Out,
        };
    }

    /// <summary>The confirmation of a road (§5.6, §7.2 and §7.4): what comes at once, what changes hands, the price.</summary>
    public SpecConfirm Confirm(string spec, string? fork, Texts texts) => Confirm(spec, fork, null, texts);

    /// <summary>The same, naming a final class (§14.8): alone, it moves no spell; with a lineage or a branch, it comes at their price.</summary>
    public SpecConfirm Confirm(string spec, string? fork, string? final, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        long cost = Specs.ChangeCost(Hero, spec, fork, final);
        bool first = cost == 0, alone = final is not null && spec == Hero.Spec && fork == Hero.Fork;
        Hero moved = Specs.Transferred(Hero, spec, fork, Data, final);
        SpecTier[] had = [.. Specs.TiersOf(Hero, Data)];
        SpecFinal? coming = Specs.FinalOf(moved, Data);
        SpecGift[] gifts = [.. Specs.TiersOf(moved, Data).Where(t => !had.Contains(t)).Select(t => coming is not null && ReferenceEquals(t, coming.Tier) ? FinalGift(spec, fork, coming, texts) : Gift(spec, fork, t, texts))];
        string lang = texts.Lang;
        string? leaves = null;
        string title = texts[alone && first ? "spec.confirm.final" : "spec.confirm.first"];
        List<string> ranks = [];
        SpecFinal? going = Specs.FinalOf(Hero, Data);
        if (!first && alone)
        {
            title = texts["spec.confirm.cross", going?.Tier.Title.In(lang) ?? ""];
            ranks.Add(texts["spec.confirm.final.same"]);
        }
        else if (!first)
        {
            bool branch = spec != Hero.Spec;
            Specialisation from = Data.Specs[Hero.Spec!];
            string? oldFork = from.ForkOf(Hero.Fork)?.Tiers[0].Title.In(lang);
            title = texts["spec.confirm.cross", branch || oldFork is null ? from.Name.In(lang) : oldFork];
            string[] left = branch ? [from.Name.In(lang), .. oldFork is null ? [] : new[] { oldFork }] : going is not null ? [oldFork!] : [];
            if (left.Length > 0)
                leaves = texts["spec.confirm.leave", string.Join(", ", going is null ? left : [.. left, going.Tier.Title.In(lang)])];
            ranks.AddRange(Handed(from, spec, fork, texts));
            // Another lineage of the same branch: the final class falls, the next is free (§14.8).
            if (!branch && going is not null && coming is null)
                ranks.Add(texts["spec.confirm.final.after"]);
        }
        string price = first ? texts["spec.confirm.free"] : texts["spec.confirm.cost", Price(cost, texts), Price(_context.Shards - cost, texts)];
        string? later = !first ? null
            : alone ? texts["spec.confirm.later.final", Price(FinalPrice, texts)]
            : final is not null ? texts["spec.confirm.later.all", Price(FinalPrice, texts), Price(ForkPrice, texts), Price(BranchPrice, texts)]
            : moved.Level >= Specs.Forks ? texts["spec.confirm.later", Price(ForkPrice, texts), Price(BranchPrice, texts)]
            : texts["spec.confirm.later.branch", Price(BranchPrice, texts)];
        int comingIn = Specs.SpellsOf(moved, Data).Count(id => Hero.Deck is not null && !(moved.Deck ?? []).Contains(id));
        string? deck = comingIn == 0 ? null : texts[comingIn == 1 ? "spec.deck" : "spec.deck.many"];
        string yes = first ? texts[alone ? "spec.confirm.final.yes" : "spec.confirm.yes"] : texts["spec.cross", Price(cost, texts)];
        string no = texts[first ? "spec.confirm.no" : alone ? "spec.confirm.final.keep" : "spec.confirm.keep"];
        return new SpecConfirm(title, leaves, texts[first ? "spec.confirm.get" : "spec.confirm.take"], gifts, ranks, price, later, deck, yes, no, first);
    }

    /// <summary>A final class's name in words (§14.10).</summary>
    public static string TemplateName(FinalTemplate template, Texts texts) => (texts ?? throw new ArgumentNullException(nameof(texts)))[$"spec.template.{template}"];

    // A final class's line: no spell changes, its signature says what it does (§14.8).
    private SpecGift FinalGift(string spec, string? fork, SpecFinal final, Texts texts) =>
        new(final.Tier.Title.In(texts.Lang), $"{texts["spec.level", final.Tier.Level]} · {texts["spec.confirm.signature", TemplateName(final.Template, texts)]}",
            final.Icon ?? Data.Specs[spec].ForkOf(fork)?.Icon);

    private SpecGift Gift(string spec, string? fork, SpecTier tier, Texts texts)
    {
        Specialisation branch = Data.Specs[spec];
        string[] spells = [.. (tier.Spells ?? []).Select(id => Data.Spells[id].Name.In(texts.Lang))];
        string gives = spells.Length switch
        {
            0 => texts["spec.confirm.no-spell"],
            1 => texts["spec.confirm.spell", spells[0]],
            _ => texts["spec.confirm.spells", string.Join(", ", spells)],
        };
        return new SpecGift(tier.Title.In(texts.Lang), $"{texts["spec.level", tier.Level]} · {gives}", tier.Level == Specs.Opens ? branch.Icon : branch.ForkOf(fork)?.Icon);
    }

    // Case by case, as Specs.Transferred gives them: a spell of rank 2 or more to its heir.
    private IEnumerable<string> Handed(Specialisation from, string spec, string? fork, Texts texts)
    {
        string[] old = [.. from.SpellsWith(Hero.Fork)], heirs = [.. Data.Specs[spec].SpellsWith(fork)];
        for (int i = 0; i < old.Length && i < heirs.Length; i++)
        {
            int rank = Hero.Ranks?.GetValueOrDefault(old[i], 1) ?? 1;
            if (rank >= 2 && old[i] != heirs[i] && Data.Spells[heirs[i]].Level <= Hero.Level)
                yield return texts["spec.confirm.rank", Data.Spells[old[i]].Name.In(texts.Lang), rank, Data.Spells[heirs[i]].Name.In(texts.Lang)];
        }
    }
}

/// <summary>
/// The page's layout (sprint 191, D95, docs/maquette-specialisation.md §4), in pixels: the frieze, the tree of four columns and the panel, which keeps the place of the
/// Elements page's. The six lineages take the pile of the pairs.
/// </summary>
public static class SpecTree
{
    public const float FriezeWidth = 756, TreeY = 52, Width = 740, Height = ElementsAtelier.Height, RootWidth = 92, RootHeight = 96, RootY = 128, ButtonY = 300, ButtonHeight = 52,
        BranchX = 128, ForkX = 344, TileWidth = 188, TileHeight = ElementsAtelier.PairHeight, TrunkX = 110, ElbowX = 330, RootCentre = 176;

    /// <summary>
    /// The column 100 (sprint 196, maquette §14.2): narrower tiles, a gutter of 40 for two elbows; its frieze's cell keeps x 552 (Q15). The elbows nest as stairs (§14.3).
    /// </summary>
    public const float TierX = 552, FinalX = 572, FinalWidth = 168, FinalTitleWidth = 108, ElbowIn = 544, ElbowMid = 552, ElbowOut = 560;

    /// <summary>The frieze's cells: where each starts, the root's first; then its end.</summary>
    public static readonly IReadOnlyList<float> FriezeCells = [0, BranchX, ForkX, TierX, FriezeWidth];

    /// <summary>The top of a final class in the column 100, by lineage (couple) and sister, whatever the branch open.</summary>
    public static float FinalY(int couple, int sister) => (couple == 0 ? 28 : 216) + (56 * sister);

    /// <summary>The top of a lineage's one evolution at 100: the outer place of its couple.</summary>
    public static float SoleY(int couple) => couple == 0 ? 28 : 272;

    /// <summary>The top of a tile of the column 100 of this branch.</summary>
    public static float LastY(Specialisation branch, SpecNode node)
    {
        ArgumentNullException.ThrowIfNull(branch);
        ArgumentNullException.ThrowIfNull(node);
        int couple = branch.Forks.ToList().FindIndex(f => f.Id == node.Fork);
        if (node.Final is null)
            return SoleY(couple);
        return FinalY(couple, branch.Forks[couple].Finals!.ToList().FindIndex(f => f.Id == node.Final));
    }

    /// <summary>A lineage's elbow towards the column 100: the one with the shorter way takes the outer one; the middle branch's are aligned.</summary>
    public static float Elbow(int branch, int fork) => branch == 1 ? ElbowMid : (branch == 0) == (fork == 0) ? ElbowOut : ElbowIn;

    /// <summary>The top of a branch, centred on its two lineages.</summary>
    public static float BranchY(int branch) => ForkY((branch * 2) + 1) - 28;

    /// <summary>The top of a lineage's tile, of six: the pile of the pairs.</summary>
    public static float ForkY(int index) => ElementsAtelier.PairY(index);

    private static float Centre(float top) => top + (TileHeight / 2);

    /// <summary>The line from the root to a branch.</summary>
    public static (float X, float Y)[] ToBranch(int branch) =>
        [(RootWidth, RootCentre), (TrunkX, RootCentre), (TrunkX, Centre(BranchY(branch))), (BranchX, Centre(BranchY(branch)))];

    /// <summary>The line from a branch to one of its two lineages.</summary>
    public static (float X, float Y)[] ToFork(int branch, int fork) =>
        [(BranchX + TileWidth, Centre(BranchY(branch))), (ElbowX, Centre(BranchY(branch))), (ElbowX, Centre(ForkY((branch * 2) + fork))), (ForkX, Centre(ForkY((branch * 2) + fork)))];

    /// <summary>The line from a lineage to a tile of the column 100 whose top is <paramref name="top"/> (§14.3): out, the elbow, in.</summary>
    public static (float X, float Y)[] ToFinal(int branch, int fork, float top)
    {
        float from = Centre(ForkY((branch * 2) + fork)), to = Centre(top), elbow = Elbow(branch, fork);
        return [(ForkX + TileWidth, from), (elbow, from), (elbow, to), (FinalX, to)];
    }
}
