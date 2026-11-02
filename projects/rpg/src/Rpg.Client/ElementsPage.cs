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
public sealed record ElementBar(Element Element, int Total, int Threshold, int Bonus, double Fill, bool Over);

/// <summary>A step of the frieze of tiers.</summary>
public sealed record ElementTier(int Level, bool Reached);

/// <summary>
/// The Elements page (sprint 183, D93, <c>docs/elements-specialisations.md</c> §v3.3), everything but the drawing: the base
/// elements and the threshold of D85, the wheel of the 6 pairs and the 4 trios, the frieze of tiers. A fusion's id
/// is its compound's name.
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
                return new ElementBar(x.Element, line.Total, threshold, line.Bonus, Math.Min(1, (double)line.Total / threshold), line.Total > threshold);
            })];
        }
    }

    public IReadOnlyList<ElementTier> Tiers => [.. TierLevels.Select(l => new ElementTier(l, _editor.Hero.Level >= l))];
}

/// <summary>
/// The wheel's layout (sprint 183): the four base elements at the compass points, each pair on the line between its parents,
/// each trio in the corner of two of its parents. In pixels, tall enough for a finger; the window's 1280 × 720 hold it.
/// </summary>
public static class ElementsWheel
{
    public const float Width = 520, Height = 320, NodeWidth = 104, NodeHeight = 44;

    public static IEnumerable<(string Id, float X, float Y)> Nodes =>
    [
        (nameof(Element.Earth), 260, 22), (nameof(Element.Fire), 468, 160), (nameof(Element.Water), 260, 298), (nameof(Element.Air), 52, 160),
        ("sand", 156, 91), ("magma", 364, 91), ("steam", 364, 229), ("ice", 156, 229), ("mud", 260, 229), ("lightning", 166, 160),
        ("salt", 52, 22), ("ash", 468, 22), ("sap", 468, 298), ("ether", 52, 298),
    ];
}
