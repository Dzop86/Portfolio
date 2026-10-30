using System.Globalization;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// A quest's kind as the journal draws it (sprint 165, D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §3.2): an icon in
/// relief of <c>tools/ui_art.py</c> (<c>assets/icons/relief/quest-&lt;kind&gt;.svg</c>, greyed <c>-locked</c> for a kind
/// filtered out) and its colour, that of the card's subtitle; the first steps leave the pistachio, kept for what is
/// chosen or done.
/// </summary>
public static class QuestKindStyle
{
    /// <summary>The icon's file name, without its folder and extension.</summary>
    public static string Icon(QuestKind kind, bool locked = false) =>
        "quest-" + kind.ToString().ToLowerInvariant() + (locked ? "-locked" : "");

    /// <summary>
    /// The kind's colour: the achievements' gold for the story, a sky blue for an inhabitant's request, the steel of the
    /// trades' menu, the dungeons' violet, the alchemy's turquoise for the first steps.
    /// </summary>
    public static string Colour(QuestKind kind) => kind switch
    {
        QuestKind.Main => "#ffd166",
        QuestKind.Trade => "#a9bccd",
        QuestKind.Dungeon => "#b48cff",
        QuestKind.Tutorial => "#4fd1c5",
        _ => "#6bb8f2",
    };

    /// <summary>The contrast of two colours "#rrggbb" (WCAG 2), 1 to 21.</summary>
    public static double Contrast(string a, string b)
    {
        double la = Luminance(a), lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(string hex)
    {
        double Channel(int at)
        {
            double c = int.Parse(hex.AsSpan(at, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(1) + 0.7152 * Channel(3) + 0.0722 * Channel(5);
    }
}
