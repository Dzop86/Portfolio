using Godot;

namespace Rpg.Desktop;

/// <summary>
/// The game's windows (characteristics, spells, inventory, map, workshop) moved by their header (D67):
/// pressed in its top band and dragged, the window follows the mouse, its header kept on screen; each
/// window opens again where it was left.
/// </summary>
public static class WindowDrag
{
    /// <summary>The height of the band one grabs, from the top of the window.</summary>
    public const float Header = 48;

    private static readonly Dictionary<string, Vector2> Moved = [];

    public static void Make(Control window, string key)
    {
        // Where it was left, once laid out.
        if (Moved.TryGetValue(key, out Vector2 by))
            Callable.From(() => Shift(window, by)).CallDeferred();
        bool dragging = false;
        window.GuiInput += e =>
        {
            switch (e)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Left } b:
                    dragging = b.Pressed && b.Position.Y <= Header;
                    if (dragging)
                        window.AcceptEvent();
                    break;
                case InputEventMouseMotion m when dragging:
                    Vector2 before = window.Position;
                    Shift(window, m.Relative);
                    Moved[key] = Moved.GetValueOrDefault(key) + (window.Position - before);
                    window.AcceptEvent();
                    break;
            }
        };
    }

    /// <summary>Moves a window as a drag would (for the self-test too), its header kept inside the screen.</summary>
    public static void Shift(Control window, Vector2 by)
    {
        Vector2 screen = window.GetViewportRect().Size;
        Vector2 to = window.Position + by;
        to.X = Mathf.Clamp(to.X, -window.Size.X + 80, screen.X - 80);
        to.Y = Mathf.Clamp(to.Y, 0, screen.Y - Header);
        window.Position = to;
    }
}
