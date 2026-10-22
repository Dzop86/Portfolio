using Godot;

namespace Rpg.Desktop;

/// <summary>
/// Over a card of the timeline, in a fight with the party (sprint 124, D79): a ring around the portrait of the one
/// playing that empties with its turn, the seconds left above it, in the alert colour for the last ten; "away" under
/// the portrait of an absent hero. Draws nothing otherwise.
/// </summary>
public partial class TurnRing : Control
{
    private static readonly Color Alert = new("ffb4a2");

    /// <summary>Seconds left, or null when this card's fighter is not playing.</summary>
    public int? Seconds { get; private set; }

    public int Total { get; private set; } = 30;
    public bool InAlert { get; private set; }

    /// <summary>The word under an absent hero's portrait, or null.</summary>
    public string? AbsentText { get; private set; }

    public void Set(int? seconds, int total, bool alert, string? absent)
    {
        if ((Seconds, Total, InAlert, AbsentText) == (seconds, total, alert, absent))
            return;
        (Seconds, Total, InAlert, AbsentText) = (seconds, Math.Max(1, total), alert, absent);
        QueueRedraw();
    }

    public override void _Draw()
    {
        Font font = ThemeDB.FallbackFont;
        var centre = new Vector2(Size.X / 2, 26);
        if (Seconds is int s)
        {
            Color colour = InAlert ? Alert : FighterView.PlayerColour;
            DrawArc(centre, 27, 0, Mathf.Tau, 48, new Color(colour, 0.25f), 3, true);
            float part = Mathf.Clamp(s / (float)Total, 0, 1);
            if (part > 0)
                DrawArc(centre, 27, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * part, 48, colour, 3, true);
            string text = s.ToString(System.Globalization.CultureInfo.InvariantCulture);
            DrawStringOutline(font, new Vector2(Size.X - 20, 14), text, HorizontalAlignment.Left, -1, 15, 5, Colors.Black);
            DrawString(font, new Vector2(Size.X - 20, 14), text, HorizontalAlignment.Left, -1, 15, colour);
        }
        if (AbsentText is string away)
        {
            DrawStringOutline(font, new Vector2(0, 48), away, HorizontalAlignment.Center, Size.X, 12, 4, Colors.Black);
            DrawString(font, new Vector2(0, 48), away, HorizontalAlignment.Center, Size.X, 12, new Color(1, 1, 1, 0.75f));
        }
    }
}
