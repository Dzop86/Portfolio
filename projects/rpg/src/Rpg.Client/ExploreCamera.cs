namespace Rpg.Client;

/// <summary>
/// The camera of the zones (sprint 60), without any engine: it follows the player smoothly, zooms
/// between <see cref="MinZoom"/> and <see cref="MaxZoom"/> (the height of the view, in cells), always
/// from the same side (D66: no more turning, sprint 83). Godot only places its camera where this says.
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

    /// <summary>The camera's turn around the vertical, in degrees: the isometric view.</summary>
    public const double Yaw = 45;

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

}
