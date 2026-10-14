using Godot;

namespace Rpg.Desktop;

/// <summary>
/// The arrow under the mouse near the edge of a map (sprint 83, D66), as in the games of the genre: a
/// chevron in the accent colour pointing to the next map, the zone's name below it when the way leaves
/// the zone. It turns to the side's direction on screen.
/// </summary>
public partial class EdgeArrow : Control
{
    private readonly Label _name = new() { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };

    public EdgeArrow()
    {
        Size = new Vector2(36, 36);
        PivotOffset = Size / 2;
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
    }

    public override void _Ready()
    {
        _name.AddThemeFontSizeOverride("font_size", 14);
        _name.AddThemeConstantOverride("outline_size", 6);
        _name.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.8f));
        GetParent().CallDeferred(Node.MethodName.AddChild, _name);
    }

    /// <summary>The arrow at <paramref name="at"/> on screen, pointing along <paramref name="direction"/> (screen space).</summary>
    public void Point(Vector2 at, Vector2 direction, string? zone)
    {
        Visible = true;
        Position = at - Size / 2 + direction.Normalized() * 18;
        Rotation = Mathf.Atan2(direction.X, -direction.Y);
        _name.Text = zone ?? "";
        _name.Visible = zone is not null;
        _name.Position = at + new Vector2(-_name.Size.X / 2, 22);
    }

    public new void Hide()
    {
        Visible = false;
        _name.Visible = false;
    }

    /// <summary>For the self-test: the zone's name shown, if any.</summary>
    public string ZoneText => _name.Visible ? _name.Text : "";

    public override void _Draw()
    {
        // A chevron pointing up, drawn twice: a dark outline, then the accent colour.
        Vector2 c = Size / 2;
        Vector2[] chevron = [c + new Vector2(-12, 6), c + new Vector2(0, -8), c + new Vector2(12, 6)];
        DrawPolyline(chevron, new Color(0, 0, 0, 0.75f), 9, true);
        DrawPolyline(chevron, new Color("bef374"), 5, true);
    }
}
