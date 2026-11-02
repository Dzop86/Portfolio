using Rpg.Core;

namespace Rpg.Client;

/// <summary>Where a fusion stands for this hero.</summary>
public enum FusionState
{
    /// <summary>The level has not opened it yet.</summary>
    Locked,

    /// <summary>It can be created.</summary>
    Open,

    /// <summary>A compound the draft has holds one of its parents (sprint 187, D94).</summary>
    Blocked,

    /// <summary>The draft has it: it can be dissolved.</summary>
    Created,
}

/// <summary>A pair or a trio of the wheel: its parents, the level that opens it, its reaction, its compound.</summary>
public sealed record Fusion(string Id, IReadOnlyList<Element> Parents, int Level, Element Compound, ReactionSpec? Reaction, FusionState State, IReadOnlyList<Element> BlockedBy);

/// <summary>A base element's bar: its points with the equipment's, the threshold of D85, the bonus they give.</summary>
public sealed record ElementBar(Element Element, int Total, int Threshold, int Bonus, double Fill, bool Over, Element? ConsumedBy = null);

/// <summary>A step of the frieze of tiers.</summary>
public sealed record ElementTier(int Level, bool Reached, bool Next = false);

/// <summary>
/// The Elements page (sprint 183, D93; sprint 189, D94, <c>docs/maquette-elements.md</c>), everything but the drawing: the base
/// elements and the threshold of D85, the 6 pairs and the 4 trios, the frieze of tiers. A fusion's id is its compound's name.
/// </summary>
public sealed class ElementsPage(PointsEditor editor)
{
    private static readonly (string Id, Element[] Parents)[] All =
    [
        ("lightning", [Element.Fire, Element.Air]),
        ("ice", [Element.Water, Element.Air]),
        ("mud", [Element.Earth, Element.Water]),
        ("magma", [Element.Earth, Element.Fire]),
        ("steam", [Element.Fire, Element.Water]),
        ("sand", [Element.Earth, Element.Air]),
        ("ether", [Element.Fire, Element.Water, Element.Air]),
        ("ash", [Element.Earth, Element.Fire, Element.Air]),
        ("sap", [Element.Earth, Element.Fire, Element.Water]),
        ("salt", [Element.Earth, Element.Water, Element.Air]),
    ];

    private static readonly (Element Element, Characteristic Stat)[] Base =
        [(Element.Earth, Characteristic.Earth), (Element.Fire, Characteristic.Fire), (Element.Water, Characteristic.Water), (Element.Air, Characteristic.Air)];

    private static readonly int[] TierLevels = [30, 50, 100];

    private readonly PointsEditor _editor = editor ?? throw new ArgumentNullException(nameof(editor));

    public IReadOnlyList<Fusion> Fusions => [.. All.Select(Of)];

    private Fusion Of((string Id, Element[] Parents) f)
    {
        int level = f.Parents.Length == 2 ? Elements.Pairs : Elements.Trios;
        Element compound = Enum.Parse<Element>(f.Id, ignoreCase: true);
        ReactionSpec? reaction = f.Parents.Length == 2 ? Talents.ReactionOf(f.Parents[0], f.Parents[1], _editor.Data) : Talents.TrioReactionOf(compound, _editor.Data);
        FusionState state = _editor.CompoundsCreated.Contains(compound) ? FusionState.Created
            : _editor.Hero.Level < level ? FusionState.Locked
            : Elements.BlockedBy(compound, _editor.CompoundsCreated).Count > 0 ? FusionState.Blocked
            : FusionState.Open;
        return new Fusion(f.Id, f.Parents, level, compound, reaction, state, Elements.BlockedBy(compound, _editor.CompoundsCreated));
    }

    /// <summary>Create or Dissolve pressed: true when the draft changed (never on a locked fusion, nor in a fight).</summary>
    public bool Press(Fusion fusion)
    {
        ArgumentNullException.ThrowIfNull(fusion);
        Element compound = fusion.Compound;
        if (_editor.CanDissolve(compound))
        {
            _editor.Dissolve(compound);
            return true;
        }
        if (_editor.CanCreate(compound))
        {
            _editor.Create(compound);
            return true;
        }
        return false;
    }

    /// <summary>The spells this fusion gives this hero at its level: the twins of the class's spells it has unlocked.</summary>
    public int SpellsOf(Fusion fusion)
    {
        ArgumentNullException.ThrowIfNull(fusion);
        Element compound = fusion.Compound;
        return Talents.GrimoireOf(_editor.Draft with { Compounds = [compound] }, _editor.Data).Spells.Count(id => _editor.Data.Spells[id].Element == compound);
    }

    /// <summary>The four base elements of the draft.</summary>
    public IReadOnlyList<ElementBar> Bars
    {
        get
        {
            var sheet = new CharacterSheet(_editor.Draft, _editor.Data);
            int threshold = Characteristics.Threshold(_editor.Hero.Level);
            return [.. Base.Select(x =>
            {
                SheetLine line = sheet[x.Stat];
                Element? by = _editor.CompoundsCreated.Where(c => Elements.Parents(c).Contains(x.Element)).Cast<Element?>().FirstOrDefault();
                return new ElementBar(x.Element, line.Total, threshold, line.Bonus, Math.Min(1, (double)line.Total / threshold), line.Total > threshold, by);
            })];
        }
    }

    public IReadOnlyList<ElementTier> Tiers
    {
        get
        {
            int next = TierLevels.Where(l => _editor.Hero.Level < l).DefaultIfEmpty(0).First();
            return [.. TierLevels.Select(l => new ElementTier(l, _editor.Hero.Level >= l, l == next))];
        }
    }

    /// <summary>The base elements no compound of the draft holds (D94): the page's one counter.</summary>
    public int FreeElements => ElementsAtelier.ElementOrder.Count - Elements.ConsumedBy(_editor.CompoundsCreated).Count;

    /// <summary>The pairs and the trios, in the workshop's columns.</summary>
    public IReadOnlyList<Fusion> Pairs => [.. ElementsAtelier.PairOrder.Select(id => Fusions.Single(f => f.Id == id))];

    public IReadOnlyList<Fusion> Trios => [.. ElementsAtelier.TrioOrder.Select(id => Fusions.Single(f => f.Id == id))];

    /// <summary>What blocks a fusion, as the panel says it: "Fire in Lightning, Earth in Mud".</summary>
    public static string BlockedLine(Fusion fusion, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(fusion);
        ArgumentNullException.ThrowIfNull(texts);
        return string.Join(", ", fusion.BlockedBy.Select(c => texts["elements.blocked.part",
            string.Join(" + ", fusion.Parents.Intersect(Elements.Parents(c)).Select(e => texts["element." + e])), texts[$"fusion.{c.ToString().ToLowerInvariant()}.the"]]));
    }

    /// <summary>The foot of the reading panel (docs/maquette-elements.md §5.7): two lines, the first in the colour of what blocks when <c>Blocked</c>.</summary>
    public (string First, string Second, bool Blocked) Foot(Fusion fusion, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(fusion);
        ArgumentNullException.ThrowIfNull(texts);
        string book = texts["elements.book", Talents.GrimoireOf(_editor.Draft, _editor.Data).Spells.Count];
        if (fusion.State == FusionState.Blocked)
            return (texts["elements.blocked.by", BlockedLine(fusion, texts)], texts["elements.blocked.how"], true);
        if (fusion.State == FusionState.Locked)
            return (texts["elements.spells.locked", fusion.Level], book, false);
        int spells = SpellsOf(fusion);
        string parents = string.Join(" + ", fusion.Parents.Select(e => texts["element." + e])), name = texts[$"fusion.{fusion.Id}.name"];
        string first = spells == 0 ? texts["elements.spells.none"]
            : fusion.State == FusionState.Created
                ? (spells == 1 ? texts["elements.spells.created.one", name, parents] : texts["elements.spells.created", spells, name, parents])
                : (spells == 1 ? texts["elements.spells.open.one", parents, name] : texts["elements.spells.open", spells, parents, name]);
        return (first, book, false);
    }
}

/// <summary>
/// The workshop's layout (sprint 189, D94, docs/maquette-elements.md §4), in pixels: from left to right the elements, their strands,
/// the pairs by couples that share no element, the braces, the trios, the reading panel. The three piles have the same height.
/// </summary>
public static class ElementsAtelier
{
    public const float Width = 1108, Height = 352, FriezeHeight = 44, ElementWidth = 188, ElementHeight = 68, ElementGap = 8, StrandsX = 188, StrandsWidth = 64,
        PairX = 252, PairWidth = 236, PairHeight = 52, PairGap = 4, CoupleGap = 14, BraceX = 488, TrioX = 504, TrioWidth = 236, TrioHeight = 82, TrioGap = 8,
        PanelX = 756, PanelWidth = 352, StatsButtonHeight = 44, StatsButtonGap = 12, ActionWidth = 76, ActionHeight = 44;

    /// <summary>The pairs from top to bottom: each two after the other share no element.</summary>
    public static readonly IReadOnlyList<string> PairOrder = ["magma", "ice", "mud", "lightning", "sand", "steam"];

    /// <summary>The trios from top to bottom, in the order of the element they lack.</summary>
    public static readonly IReadOnlyList<string> TrioOrder = ["ether", "salt", "ash", "sap"];

    /// <summary>The base elements from top to bottom.</summary>
    public static readonly IReadOnlyList<Element> ElementOrder = [Element.Earth, Element.Fire, Element.Water, Element.Air];

    public static float ElementY(int index) => index * (ElementHeight + ElementGap);

    public static float PairY(int index) => index / 2 * (2 * PairHeight + PairGap + CoupleGap) + index % 2 * (PairHeight + PairGap);

    public static float TrioY(int index) => index * (TrioHeight + TrioGap);

    /// <summary>Where a strand leaves an element's row, by the order of its pairs in the column: three strands, 12 apart.</summary>
    public static float StrandFrom(Element element, string pair)
    {
        string[] pairs = [.. PairOrder.Where(p => Elements.Parents(Enum.Parse<Element>(p, ignoreCase: true)).Contains(element))];
        return ElementY(ElementOrder.ToList().IndexOf(element)) + (ElementHeight / 2) + ((Array.IndexOf(pairs, pair) - 1) * 12);
    }

    /// <summary>Where a strand enters a pair's tile: the first parent in the order of the rows above, the second below.</summary>
    public static float StrandTo(string pair, Element element)
    {
        Element[] parents = [.. ElementOrder.Where(Elements.Parents(Enum.Parse<Element>(pair, ignoreCase: true)).Contains)];
        return PairY(PairOrder.ToList().IndexOf(pair)) + (PairHeight / 2) + (element == parents[0] ? -7 : 7);
    }
}
