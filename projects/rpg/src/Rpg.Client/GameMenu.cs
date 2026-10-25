using System.Globalization;

namespace Rpg.Client;

/// <summary>
/// The sound settings of the Escape menu (sprint 69): the general, music and effects volumes from 0 to
/// 100, and the sound cut. Kept as a few "key=value" lines in the player's settings file.
/// </summary>
public sealed record AudioSettings(int Master = 80, int Music = 60, int Effects = 80, bool Muted = false)
{
    public const string FileName = "settings.txt";

    /// <summary>The same settings, each volume kept between 0 and 100.</summary>
    public AudioSettings Clamped() => this with { Master = Math.Clamp(Master, 0, 100), Music = Math.Clamp(Music, 0, 100), Effects = Math.Clamp(Effects, 0, 100) };

    /// <summary>
    /// A volume in decibels for the engine: 100 is 0 dB, 50 about -6 dB, 0 silent (-80 dB, the
    /// engine's floor). Linear in loudness, as a slider is expected to be.
    /// </summary>
    public static double Decibels(int volume) => volume <= 0 ? -80 : Math.Max(-80, 20 * Math.Log10(Math.Min(volume, 100) / 100.0));

    /// <summary>The general bus: silent when the sound is cut.</summary>
    public double MasterDb => Muted ? -80 : Decibels(Master);
    public double MusicDb => Decibels(Music);
    public double EffectsDb => Decibels(Effects);

    public string ToText() => string.Create(CultureInfo.InvariantCulture, $"master={Master}\nmusic={Music}\neffects={Effects}\nmuted={(Muted ? 1 : 0)}\n");

    /// <summary>The settings in <paramref name="text"/>; what is missing or unreadable keeps its default.</summary>
    public static AudioSettings Parse(string? text)
    {
        var s = new AudioSettings();
        foreach (string line in (text ?? "").Split('\n'))
        {
            string[] kv = line.Trim().Split('=', 2);
            if (kv.Length != 2 || !int.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                continue;
            s = kv[0] switch
            {
                "master" => s with { Master = n },
                "music" => s with { Music = n },
                "effects" => s with { Effects = n },
                "muted" => s with { Muted = n != 0 },
                _ => s,
            };
        }
        return s.Clamped();
    }
}

/// <summary>The windows over the town, each opened by its icon on the side bar or by its key.</summary>
public enum GameWindow
{
    Characteristics,
    Spells,
    Inventory,
    Map,
    Journal,
    Jobs,
    Contacts,
    Talents,
}

/// <summary>What the Escape menu offers.</summary>
public enum MenuChoice
{
    Resume,
    Servers,
    Characters,
    Quit,
}

/// <summary>
/// Which window and menu are open (sprint 69), without any engine: one window at a time over the
/// town, opened and closed by its icon or its key (C, S, I, M), the Escape menu over everything.
/// Escape closes the menu if it is open, else the window, else opens the menu.
/// </summary>
public sealed class WindowState
{
    /// <summary>The side bar, top to bottom: each window and its key.</summary>
    public static readonly IReadOnlyList<(GameWindow Window, char Key)> Bar =
    [
        (GameWindow.Characteristics, 'C'),
        (GameWindow.Spells, 'S'),
        (GameWindow.Inventory, 'I'),
        (GameWindow.Map, 'M'),
        (GameWindow.Journal, 'Q'),
        (GameWindow.Jobs, 'J'),
        (GameWindow.Contacts, 'F'),
        // The talent tree in its own window (sprint 148, K2 b).
        (GameWindow.Talents, 'T'),
    ];

    public GameWindow? Open { get; private set; }
    public bool MenuOpen { get; private set; }

    /// <summary>The window a key opens, if any (either case).</summary>
    public static GameWindow? ForKey(char key)
    {
        char k = char.ToUpperInvariant(key);
        foreach ((GameWindow w, char c) in Bar)
        {
            if (c == k)
                return w;
        }
        return null;
    }

    /// <summary>
    /// The windows a fight opens too (sprint 93, Charles): the characteristics and the inventory, to look at only; since
    /// sprint 150 (D82, O4 a), the tree, read only.
    /// </summary>
    public static bool OpensInFight(GameWindow window) => window is GameWindow.Characteristics or GameWindow.Inventory or GameWindow.Talents;

    /// <summary>Opens the window, or closes it when it is the open one; nothing while the menu is open.</summary>
    public void Toggle(GameWindow window)
    {
        if (MenuOpen)
            return;
        Open = Open == window ? null : window;
    }

    public void Close() => Open = null;

    public void CloseMenu() => MenuOpen = false;

    /// <summary>The menu's icon: opens the menu over the window, or closes it.</summary>
    public void ToggleMenu() => MenuOpen = !MenuOpen;

    public void Escape()
    {
        if (MenuOpen)
            MenuOpen = false;
        else if (Open is not null)
            Open = null;
        else
            MenuOpen = true;
    }
}
