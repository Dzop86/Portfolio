namespace Rpg.Client;

/// <summary>
/// The camera of the zones (sprint 60), without any engine: it follows the player smoothly, zooms
/// between <see cref="MinZoom"/> and <see cref="MaxZoom"/> (the height of the view, in cells), and
/// turns a quarter at a time around the player, so that the view stays isometric. Godot only places
/// its camera where this says.
/// </summary>
public sealed class ExploreCamera(double x, double z, double zoom = ExploreCamera.DefaultZoom)
{
    public const double MinZoom = 7;
    public const double MaxZoom = 22;
    public const double DefaultZoom = 13.5;

    /// <summary>How much of the way to the player the camera covers in a second.</summary>
    public const double FollowRate = 6;

    /// <summary>The point looked at, on the ground.</summary>
    public double X { get; private set; } = x;
    public double Z { get; private set; } = z;

    public double Zoom { get; private set; } = Math.Clamp(zoom, MinZoom, MaxZoom);

    /// <summary>Quarter turns from the first view, 0 to 3.</summary>
    public int Quarter { get; private set; }

    /// <summary>The camera's turn around the vertical, in degrees: 45 for the first view.</summary>
    public double Yaw => 45 + 90 * Quarter;

    /// <summary>Moves towards the player's point, the more the longer the frame; never past it.</summary>
    public void Follow(double targetX, double targetZ, double seconds)
    {
        double t = Math.Clamp(seconds * FollowRate, 0, 1);
        X += (targetX - X) * t;
        Z += (targetZ - Z) * t;
    }

    /// <summary>Puts the camera on the player at once (arriving in a zone).</summary>
    public void Jump(double targetX, double targetZ) => (X, Z) = (targetX, targetZ);

    /// <summary>Zooms in (steps above 0) or out, 15 % a step, within the bounds.</summary>
    public void ZoomBy(int steps) => Zoom = Math.Clamp(Zoom * Math.Pow(0.85, steps), MinZoom, MaxZoom);

    /// <summary>A quarter turn, to the right (1) or to the left (-1).</summary>
    public void Turn(int quarters) => Quarter = ((Quarter + quarters) % 4 + 4) % 4;
}
