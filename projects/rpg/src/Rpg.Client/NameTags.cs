namespace Rpg.Client;

/// <summary>A line of the words over a player: its guild's name, its own name, the title it wears.</summary>
public enum NameTagLine
{
    Guild,
    Name,
    Title,
}

/// <summary>
/// The words over a player in the world (sprint 173, D89, Charles: « le nom de la guilde, ensuite juste en dessous le nom du
/// perso, et seulement au survol ») : from the top, its guild's name, its name, then the title it wears; shown only while the
/// mouse is over that player, the player's own character included. The inhabitants keep their names shown near the player.
/// </summary>
public static class NameTags
{
    /// <summary>The height of the lowest line over a player's feet, and between two lines, in the person's units.</summary>
    public const double Lowest = 0.98, Spacing = 0.13;

    /// <summary>How tall a player stands on screen, in the person's units, and how wide it is for the mouse, in its heights.</summary>
    public const double Height = 0.95, Width = 0.6;

    /// <summary>The lines over a player, from the top, each at its height: those it has.</summary>
    public static IReadOnlyList<(NameTagLine Line, double At)> Layout(bool guild, bool name, bool title)
    {
        List<NameTagLine> lines = [];
        if (guild)
            lines.Add(NameTagLine.Guild);
        if (name)
            lines.Add(NameTagLine.Name);
        if (title)
            lines.Add(NameTagLine.Title);
        return [.. lines.Select((l, i) => (l, Lowest + (Spacing * (lines.Count - 1 - i))))];
    }

    /// <summary>
    /// Whether the mouse is over a player standing on screen with its feet at (<paramref name="feetX"/>, <paramref name="feetY"/>)
    /// and its head at <paramref name="headY"/> (screen pixels, y downwards): a box as tall as it and <see cref="Width"/> of that wide.
    /// </summary>
    public static bool Over(double feetX, double feetY, double headY, double mouseX, double mouseY)
    {
        double tall = feetY - headY;
        return tall > 0 && mouseY >= headY && mouseY <= feetY && Math.Abs(mouseX - feetX) <= tall * Width / 2;
    }
}
