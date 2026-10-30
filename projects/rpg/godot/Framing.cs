using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// Framing a model in a picture of its own (sprint 172, D88): the monsters' portraits (<see cref="MonsterPhotographer"/>) and
/// the bestiary's turning portrait (<see cref="Portrait.Fit"/>), whatever the model's size, a piglet as large as a king.
/// </summary>
public static class Framing
{
    /// <summary>The camera's angle of view, and how much room is left around the model (1 = touching the edges).</summary>
    public const float Fov = 30, Room = 1.1f;

    /// <summary>The box around every visible mesh under a node, in the node's own space.</summary>
    public static Aabb Bounds(Node3D root)
    {
        Aabb? box = null;
        void Walk(Node node, Transform3D transform)
        {
            foreach (Node child in node.GetChildren())
            {
                if (child is not Node3D spatial)
                    continue;
                Transform3D t = transform * spatial.Transform;
                if (spatial is GeometryInstance3D { Visible: true } visual)
                {
                    Aabb own = t * visual.GetAabb();
                    box = box is Aabb b ? b.Merge(own) : own;
                }
                Walk(spatial, t);
            }
        }
        Walk(root, Transform3D.Identity);
        return box ?? new Aabb(new Vector3(-0.5f, 0, -0.5f), Vector3.One);
    }

    /// <summary>The box a model fills whichever way it is turned about its upright axis: for a portrait that turns.</summary>
    public static Aabb Turning(Aabb box)
    {
        float radius = (float)MonsterPortraits.TurningRadius(box.Position.X, box.End.X, box.Position.Z, box.End.Z);
        return new Aabb(new Vector3(-radius, box.Position.Y, -radius), new Vector3(2 * radius, box.Size.Y, 2 * radius));
    }

    /// <summary>
    /// Frames a box from the front and a little above, as the player looks at a fight: its picture's middle in the middle of
    /// the camera's viewport, its larger side filling it but for <see cref="Room"/>. The camera must be in the scene tree.
    /// </summary>
    public static void Frame(Camera3D camera, Aabb box)
    {
        Vector2 size = camera.GetViewport().GetVisibleRect().Size;
        camera.Fov = Fov;
        Vector3 centre = box.GetCenter(), towards = new Vector3(0, 0.35f, 1).Normalized();
        float distance = box.Size.Length();
        camera.LookAtFromPosition(centre + towards * distance, centre, Vector3.Up);
        // Three times: measure where the corners fall, move the camera sideways to centre them, then nearer or farther to fit them.
        for (int pass = 0; pass < 3; pass++)
        {
            Vector2 low = new(float.MaxValue, float.MaxValue), high = new(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector2 at = camera.UnprojectPosition(box.GetEndpoint(i));
                (low, high) = (low.Min(at), high.Max(at));
            }
            // The camera's angle of view is vertical: a pixel's size in the world at that distance.
            float pixel = 2 * distance * Mathf.Tan(Mathf.DegToRad(Fov / 2)) / size.Y;
            Vector2 off = ((low + high) / 2 - size / 2) * pixel;
            Basis b = camera.GlobalTransform.Basis;
            centre += b.X * off.X - b.Y * off.Y;
            distance *= Mathf.Max((high.X - low.X) / size.X, (high.Y - low.Y) / size.Y) * Room;
            camera.LookAtFromPosition(centre + towards * distance, centre, Vector3.Up);
            camera.Far = distance * 4;
        }
    }
}
