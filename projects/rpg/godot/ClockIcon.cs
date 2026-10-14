using Godot;

namespace Rpg.Desktop;

/// <summary>A small clock drawn by the game, next to the round's number on the timeline (sprint 79).</summary>
public partial class ClockIcon : Control
{
    public override void _Draw()
    {
        Vector2 centre = Size / 2;
        float r = Mathf.Min(Size.X, Size.Y) / 2 - 1;
        var light = new Color(0.92f, 0.92f, 0.92f);
        DrawCircle(centre, r, new Color(0.12f, 0.12f, 0.12f, 0.85f));
        DrawArc(centre, r, 0, Mathf.Tau, 24, light, 1.5f, true);
        DrawLine(centre, centre + new Vector2(0, -r * 0.65f), light, 1.5f, true);
        DrawLine(centre, centre + new Vector2(r * 0.5f, 0), light, 1.5f, true);
    }
}
