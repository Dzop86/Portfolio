using Godot;

namespace Rpg.Desktop;

/// <summary>A small triangle before a zone's name in the journal (sprint 116): pointing down when the zone is open, right when folded.</summary>
public partial class FoldArrow : Control
{
    private bool _open = true;

    public bool Open
    {
        get => _open;
        set
        {
            _open = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        Vector2 c = Size / 2;
        float r = Mathf.Min(Size.X, Size.Y) / 2 - 2;
        Vector2[] down = [c + new Vector2(-r, -r * 0.5f), c + new Vector2(r, -r * 0.5f), c + new Vector2(0, r * 0.6f)];
        Vector2[] right = [c + new Vector2(-r * 0.5f, -r), c + new Vector2(-r * 0.5f, r), c + new Vector2(r * 0.6f, 0)];
        DrawColoredPolygon(_open ? down : right, FighterView.PlayerColour);
    }
}
