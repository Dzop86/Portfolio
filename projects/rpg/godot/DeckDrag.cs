using Godot;

namespace Rpg.Desktop;

/// <summary>
/// Drag and drop for the deck (sprint 82, D66): what a control hands when dragged, what it accepts and
/// does when something is dropped on it, given by the screen that builds it.
/// </summary>
public sealed class DragRules
{
    public Func<Variant>? Hand { get; init; }

    public Func<Control>? Preview { get; init; }

    public Func<Variant, bool>? Accepts { get; init; }

    public Action<Variant>? Take { get; init; }
}

/// <summary>A spell of the book: dragged onto the deck; a spell of the deck dropped on it leaves the deck.</summary>
public partial class SpellTile : Button
{
    public DragRules Rules { get; set; } = new();

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Rules.Hand?.Invoke() is not { VariantType: not Variant.Type.Nil } data)
            return default;
        if (Rules.Preview?.Invoke() is Control preview)
            SetDragPreview(preview);
        return data;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) => Rules.Accepts?.Invoke(data) == true;

    public override void _DropData(Vector2 atPosition, Variant data) => Rules.Take?.Invoke(data);
}

/// <summary>A place of the deck: takes a spell dropped on it, hands its own to another place or off the bar.</summary>
public partial class DeckSlot : SpellIcon
{
    public DragRules Rules { get; set; } = new();

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Rules.Hand?.Invoke() is not { VariantType: not Variant.Type.Nil } data)
            return default;
        if (Rules.Preview?.Invoke() is Control preview)
            SetDragPreview(preview);
        return data;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) => Rules.Accepts?.Invoke(data) == true;

    public override void _DropData(Vector2 atPosition, Variant data) => Rules.Take?.Invoke(data);
}
