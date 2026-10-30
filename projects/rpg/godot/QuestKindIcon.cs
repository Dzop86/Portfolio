using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A quest's kind in the journal (sprint 116, D77). Since sprint 165 (D87, <c>docs/graphisme/s165-retours-30-oct.md</c>
/// §3.2) its icon in relief of <c>tools/ui_art.py</c> and nothing else (<see cref="QuestKindStyle"/>): a laurel crown for
/// the story, a speech bubble for a side quest, an anvil for a trade, a gate for a dungeon, an open book for the first
/// steps; greyed for a kind filtered out.
/// </summary>
public partial class QuestKindIcon : Control
{
    private QuestKind _kind;
    private bool _locked;

    public QuestKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            QueueRedraw();
        }
    }

    /// <summary>Greyed (<c>-locked</c>): a kind the journal leaves out.</summary>
    public bool Locked
    {
        get => _locked;
        set
        {
            _locked = value;
            QueueRedraw();
        }
    }

    /// <summary>The kind's colour, that of the card's subtitle.</summary>
    public static Color Colour(QuestKind kind) => new(QuestKindStyle.Colour(kind));

    public override void _Draw() =>
        DrawTextureRect(GD.Load<Texture2D>($"res://assets/icons/relief/{QuestKindStyle.Icon(_kind, _locked)}.svg"), new Rect2(Vector2.Zero, Size), false);
}
