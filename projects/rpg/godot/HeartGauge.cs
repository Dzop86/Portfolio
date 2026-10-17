using Godot;

namespace Rpg.Desktop;

/// <summary>
/// The character's life in a heart (sprint 115, instead of the experience line): filled from the
/// bottom as high as the share of life left, with the life over the most and the share written inside.
/// </summary>
public partial class HeartGauge : Control
{
    private readonly Label _points = Line(17), _share = Line(14);
    private float _fill;
    private bool _low;

    public HeartGauge()
    {
        CustomMinimumSize = new Vector2(96, 88);
        MouseFilter = MouseFilterEnum.Stop;
        var lines = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        lines.SetAnchorsPreset(LayoutPreset.FullRect);
        lines.OffsetTop = 4;
        lines.AddThemeConstantOverride("separation", -4);
        lines.AddChild(_points);
        lines.AddChild(_share);
        AddChild(lines);
    }

    /// <summary>The two lines inside, as shown (for the self-test).</summary>
    public string Text => _points.Text.Length == 0 ? "" : $"{_points.Text} {_share.Text}";

    /// <summary>The life now and the most; nothing drawn while <paramref name="max"/> is 0.</summary>
    public void Show(int now, int max, bool low)
    {
        Visible = max > 0;
        if (max <= 0)
            return;
        float fill = Math.Clamp((float)now / max, 0, 1);
        _points.Text = $"{now}/{max}";
        _share.Text = $"{(int)Math.Floor(fill * 100)}%";
        if (fill != _fill || low != _low)
        {
            _fill = fill;
            _low = low;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        Vector2[] heart = Outline(Size);
        DrawColoredPolygon(heart, new Color(0.1f, 0.1f, 0.1f, 0.85f));
        if (_fill > 0)
        {
            float top = Size.Y * (1 - _fill);
            Vector2[] below = [new(-1, top), new(Size.X + 1, top), new(Size.X + 1, Size.Y + 1), new(-1, Size.Y + 1)];
            var red = _low ? new Color("ff5a5a") : new Color("d93a4a");
            foreach (Vector2[] part in Geometry2D.IntersectPolygons(heart, below))
                DrawColoredPolygon(part, red);
        }
        DrawPolyline([.. heart, heart[0]], new Color(0.95f, 0.95f, 0.95f, 0.9f), 2, true);
    }

    /// <summary>The heart's outline in a box of <paramref name="size"/>: the classic curve, scaled to fill it.</summary>
    public static Vector2[] Outline(Vector2 size)
    {
        const int Steps = 64;
        var raw = new Vector2[Steps];
        for (int i = 0; i < Steps; i++)
        {
            double t = Math.Tau * i / Steps;
            double x = 16 * Math.Pow(Math.Sin(t), 3);
            double y = 13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t);
            raw[i] = new Vector2((float)x, (float)-y);
        }
        // The curve spans x in [-16, 16] and y in [-12, 17] (y down once turned).
        const float Width = 32, Height = 29.2f;
        for (int i = 0; i < Steps; i++)
            raw[i] = new Vector2((raw[i].X + 16) / Width * (size.X - 4) + 2, (raw[i].Y + 12) / Height * (size.Y - 4) + 2);
        return raw;
    }

    private static Label Line(int size)
    {
        var label = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 5);
        return label;
    }
}
