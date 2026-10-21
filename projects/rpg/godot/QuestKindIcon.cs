using Godot;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A quest's kind in the journal (sprint 116, D77), drawn by the game: a round badge in the kind's colour with its
/// sign, a star for the main quest, a "!" for a side quest, a hammer for a trade, a door for a dungeon, an open book
/// for the first steps.
/// </summary>
public partial class QuestKindIcon : Control
{
    private QuestKind _kind;

    public QuestKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            QueueRedraw();
        }
    }

    /// <summary>The kind's colour, the badge's and its button's.</summary>
    public static Color Colour(QuestKind kind) => kind switch
    {
        QuestKind.Main => new Color("ffd85a"),
        QuestKind.Trade => new Color("c08a5a"),
        QuestKind.Dungeon => new Color("a77de0"),
        QuestKind.Tutorial => FighterView.PlayerColour,
        _ => new Color("6cb6e8"),
    };

    public override void _Draw()
    {
        Vector2 c = Size / 2;
        float r = Mathf.Min(Size.X, Size.Y) / 2 - 1;
        DrawCircle(c, r, Colour(_kind));
        var ink = new Color(0.12f, 0.12f, 0.12f);
        float s = r * 0.62f;
        switch (_kind)
        {
            case QuestKind.Main:
                var star = new Vector2[10];
                for (int i = 0; i < 10; i++)
                    star[i] = c + Vector2.Up.Rotated(i * Mathf.Pi / 5) * (i % 2 == 0 ? s : s * 0.45f);
                DrawColoredPolygon(star, ink);
                break;
            case QuestKind.Side:
                DrawRect(new Rect2(c.X - s * 0.16f, c.Y - s, s * 0.32f, s * 1.25f), ink);
                DrawCircle(c + new Vector2(0, s * 0.72f), s * 0.2f, ink);
                break;
            case QuestKind.Trade:
                // A hammer: its handle leaning, its head across the top.
                DrawLine(c + new Vector2(-s * 0.6f, s * 0.8f), c + new Vector2(s * 0.2f, -s * 0.2f), ink, Mathf.Max(1.5f, s * 0.28f), true);
                DrawColoredPolygon([c + new Vector2(-s * 0.15f, -s * 0.95f), c + new Vector2(s * 0.95f, s * 0.15f), c + new Vector2(s * 0.6f, s * 0.5f), c + new Vector2(-s * 0.5f, -s * 0.6f)], ink);
                break;
            case QuestKind.Dungeon:
                // A door under an arch.
                DrawRect(new Rect2(c.X - s * 0.6f, c.Y - s * 0.2f, s * 1.2f, s), ink);
                DrawCircle(c + new Vector2(0, -s * 0.2f), s * 0.6f, ink);
                break;
            default:
                // An open book: two pages.
                DrawColoredPolygon([c + new Vector2(-s, -s * 0.6f), c + new Vector2(-s * 0.08f, -s * 0.35f), c + new Vector2(-s * 0.08f, s * 0.75f), c + new Vector2(-s, s * 0.5f)], ink);
                DrawColoredPolygon([c + new Vector2(s, -s * 0.6f), c + new Vector2(s * 0.08f, -s * 0.35f), c + new Vector2(s * 0.08f, s * 0.75f), c + new Vector2(s, s * 0.5f)], ink);
                break;
        }
    }
}
