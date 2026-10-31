namespace Rpg.Client;

/// <summary>
/// The talent tree's camera (sprint 176, D87, <c>docs/arbre-lisible.md</c> §7.4), without any engine: the tree's world drawn
/// at <see cref="Zoom"/>, its corner at <see cref="X"/>, <see cref="Y"/> of the view. It zooms by steps of
/// <see cref="Step"/> around a point that stays under the mouse, from the whole tree in the view up to <see cref="MaxZoom"/>;
/// it is dragged; it opens framed on the build; the tree never leaves more than half the view. Godot only draws where this
/// says.
/// </summary>
public sealed class TreeCamera
{
    public const float MaxZoom = 1.5f, Step = 1.2f;

    /// <summary>The least zoom a frame on the build opens at (nodes of 45 px and more).</summary>
    public const float FrameZoom = 0.8f;

    /// <summary>How far, in pixels, a press moves before it is a drag rather than a click.</summary>
    public const float DragThreshold = 6;

    private const float FrameMargin = 48;

    public TreeCamera(float worldWidth, float worldHeight, float viewWidth, float viewHeight)
    {
        (WorldWidth, WorldHeight, ViewWidth, ViewHeight) = (worldWidth, worldHeight, viewWidth, viewHeight);
        Fit();
    }

    public float WorldWidth { get; }
    public float WorldHeight { get; }
    public float ViewWidth { get; }
    public float ViewHeight { get; }

    public float Zoom { get; private set; } = 1;

    /// <summary>Where the world's corner stands in the view.</summary>
    public float X { get; private set; }
    public float Y { get; private set; }

    /// <summary>The overview: the whole tree in the view; never zoomed out further.</summary>
    public float MinZoom => Math.Min(MaxZoom, Math.Min(ViewWidth / WorldWidth, ViewHeight / WorldHeight));

    /// <summary>The whole tree, centred.</summary>
    public void Fit()
    {
        Zoom = MinZoom;
        Centre(WorldWidth / 2, WorldHeight / 2);
    }

    /// <summary>Zooms by <paramref name="steps"/> (negative: out) around a point of the view, which stays over the same point of the world.</summary>
    public void ZoomAt(float viewX, float viewY, float steps)
    {
        (float wx, float wy) = ToWorld(viewX, viewY);
        Zoom = Math.Clamp(Zoom * MathF.Pow(Step, steps), MinZoom, MaxZoom);
        (X, Y) = (viewX - (wx * Zoom), viewY - (wy * Zoom));
        Keep();
    }

    public void PanBy(float dx, float dy)
    {
        (X, Y) = (X + dx, Y + dy);
        Keep();
    }

    /// <summary>
    /// Framed on boxes of the world (the build): all of them in the view with a margin, at <see cref="FrameZoom"/> at least
    /// (centred on them when they do not fit), at <see cref="MaxZoom"/> at most; the overview without any.
    /// </summary>
    public void Frame(IReadOnlyCollection<Box> boxes)
    {
        ArgumentNullException.ThrowIfNull(boxes);
        if (boxes.Count == 0)
        {
            Fit();
            return;
        }
        float left = boxes.Min(b => b.X), top = boxes.Min(b => b.Y), right = boxes.Max(b => b.X + b.W), bottom = boxes.Max(b => b.Y + b.H + TreeLayout.Label);
        float fits = Math.Min(ViewWidth / (right - left + (2 * FrameMargin)), ViewHeight / (bottom - top + (2 * FrameMargin)));
        Zoom = Math.Clamp(Math.Max(FrameZoom, fits), MinZoom, MaxZoom);
        Centre((left + right) / 2, (top + bottom) / 2);
    }

    /// <summary>A point of the world in the middle of the view.</summary>
    public void Centre(float worldX, float worldY)
    {
        (X, Y) = ((ViewWidth / 2) - (worldX * Zoom), (ViewHeight / 2) - (worldY * Zoom));
        Keep();
    }

    public (float X, float Y) ToWorld(float viewX, float viewY) => ((viewX - X) / Zoom, (viewY - Y) / Zoom);

    public (float X, float Y) ToView(float worldX, float worldY) => (X + (worldX * Zoom), Y + (worldY * Zoom));

    /// <summary>Whether a press that moved this far is a drag (more than <see cref="DragThreshold"/>), not a click.</summary>
    public static bool IsDrag(float dx, float dy) => (dx * dx) + (dy * dy) > DragThreshold * DragThreshold;

    // The tree never leaves more than half the view: its far edge past the view's middle, either way.
    private void Keep()
    {
        X = Math.Clamp(X, (ViewWidth / 2) - (WorldWidth * Zoom), ViewWidth / 2);
        Y = Math.Clamp(Y, (ViewHeight / 2) - (WorldHeight * Zoom), ViewHeight / 2);
    }
}
