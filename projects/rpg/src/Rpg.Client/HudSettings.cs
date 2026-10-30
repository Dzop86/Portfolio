using System.Globalization;

namespace Rpg.Client;

/// <summary>
/// The panels laid on the map that fold (sprint 165, D87, docs/graphisme/s165-retours-30-oct.md §1-2): the quest followed
/// and the menus, each folded to its header or not, kept on this computer between two games like the sound and the
/// mini-map; following another quest does not unfold the tracker.
/// </summary>
public sealed record HudSettings(bool TrackerFolded = false, bool MenusFolded = false)
{
    public const string FileName = "hud.txt";

    public string ToText() => $"tracker={(TrackerFolded ? 1 : 0)}\nmenus={(MenusFolded ? 1 : 0)}\n";

    /// <summary>The settings in <paramref name="text"/>; what is missing or unreadable stays unfolded.</summary>
    public static HudSettings Parse(string? text)
    {
        var s = new HudSettings();
        foreach (string line in (text ?? "").Split('\n'))
        {
            string[] kv = line.Trim().Split('=', 2);
            if (kv.Length != 2 || !int.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                continue;
            s = kv[0] switch
            {
                "tracker" => s with { TrackerFolded = n != 0 },
                "menus" => s with { MenusFolded = n != 0 },
                _ => s,
            };
        }
        return s;
    }
}

/// <summary>The sizes and the counts of the folding panels (sprint 165, s165-retours-30-oct.md §1.3 and §2.3).</summary>
public static class HudLayout
{
    /// <summary>The header both panels fold to: a button the height of a touch target.</summary>
    public const int Header = 44;

    /// <summary>The menus' tiles, the gap between them and the panel's margin under the last row.</summary>
    public const int Tile = 48, Gap = 4, Bottom = 8;

    /// <summary>The menus panel's height: its header alone folded; else the header, then the rows of tiles and the margin.</summary>
    public static int MenusHeight(int tiles, int columns, bool folded)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        if (folded || tiles <= 0)
            return Header;
        int rows = (tiles + columns - 1) / columns;
        return Header + Gap + rows * Tile + (rows - 1) * Gap + Bottom;
    }

    /// <summary>What the folded menus' header counts: the sum of the badges on its tiles (none at 0).</summary>
    public static int Badges(IEnumerable<int> counts) => counts.Where(c => c > 0).Sum();

    /// <summary>What the folded tracker still says of the quest: its objectives reached out of all, "1/2".</summary>
    public static string Reached(JournalQuest quest)
    {
        ArgumentNullException.ThrowIfNull(quest);
        return string.Create(CultureInfo.InvariantCulture, $"{quest.Objectives.Count(o => o.Reached)}/{quest.Objectives.Count}");
    }
}
