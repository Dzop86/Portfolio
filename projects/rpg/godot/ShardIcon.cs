using Godot;

namespace Rpg.Desktop;

/// <summary>An Éclat, the money (sprint 87), drawn by the game: a crystal of four points, one per element, in their colours.</summary>
public partial class ShardIcon : Control
{
    private static readonly Color[] Points = [new("8a5a34"), new("4aa3df"), new("e05a47"), new("6cc06a")];

    public override void _Draw()
    {
        Vector2 c = Size / 2;
        float r = Mathf.Min(Size.X, Size.Y) / 2 - 1, w = r * 0.32f;
        // Earth up, Water right, Fire down, Air left: each point a thin diamond from the centre.
        for (int i = 0; i < 4; i++)
        {
            Vector2 dir = Vector2.Up.Rotated(i * Mathf.Pi / 2), side = dir.Orthogonal() * w;
            DrawColoredPolygon([c + side, c + dir * r, c - side, c - dir * w * 0.4f], Points[i]);
        }
        DrawCircle(c, w * 0.55f, new Color(0.95f, 0.95f, 0.95f));
    }
}
