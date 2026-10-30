using System.Security.Cryptography;
using System.Text.Json;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The world map's photographs (sprint 169, D88): each map of the world rendered once from the sky by the game itself
/// (<c>--render-maps</c>, its real models: trees, houses, water, rocks), kept as <c>assets/world/photos/&lt;id&gt;.webp</c>
/// with, in <c>photos.json</c>, the fingerprint of the map it shows, so that a map changed since asks its photograph again.
/// The photograph's sides without a neighbouring map fade out, under the atlas's waving coast.
/// </summary>
public static class MapPhotos
{
    /// <summary>A photograph's size in pixels: six a cell of a map of 48 × 32.</summary>
    public const int Width = 288, Height = 192;

    /// <summary>How far a side without a neighbour fades out, in pixels of the photograph.</summary>
    public const int Fade = 30;

    public const string Folder = "assets/world/photos", Index = "photos.json";

    /// <summary>What a map's photograph shows, in short: a hash of the map as the data describe it.</summary>
    public static string Fingerprint(Town map)
    {
        ArgumentNullException.ThrowIfNull(map);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(map);
        return Convert.ToHexStringLower(SHA256.HashData(json))[..16];
    }

    /// <summary>The world's maps photographed: every map but a dungeon's halls.</summary>
    public static IEnumerable<Town> World(GameData data) =>
        (data ?? throw new ArgumentNullException(nameof(data))).Towns.Values.Where(t => !t.IsHall).OrderBy(t => t.Id, StringComparer.Ordinal);

    /// <summary>Which sides of a map have no map of the world next to them: left, top, right, bottom.</summary>
    public static (bool Left, bool Top, bool Right, bool Bottom) Open(GameData data, Town map)
    {
        ArgumentNullException.ThrowIfNull(map);
        HashSet<Cell> taken = [.. World(data).Select(t => t.MapAt)];
        Cell at = map.MapAt;
        return (!taken.Contains(at + new Cell(-1, 0)), !taken.Contains(at + new Cell(0, -1)), !taken.Contains(at + new Cell(1, 0)), !taken.Contains(at + new Cell(0, 1)));
    }

    /// <summary>
    /// The opacity of a pixel of a photograph: whole inside, fading to nothing over <see cref="Fade"/> pixels towards a side
    /// with no neighbour (smoothly, so that no line shows).
    /// </summary>
    public static double Alpha((bool Left, bool Top, bool Right, bool Bottom) open, int x, int y)
    {
        double a = 1;
        double Ramp(int distance) => distance >= Fade ? 1 : Math.Pow(Math.Sin(Math.PI / 2 * distance / Fade), 2);
        if (open.Left)
            a *= Ramp(x);
        if (open.Right)
            a *= Ramp(Width - 1 - x);
        if (open.Top)
            a *= Ramp(y);
        if (open.Bottom)
            a *= Ramp(Height - 1 - y);
        return a;
    }

    /// <summary>The index of the photographs: each map's fingerprint, by its id.</summary>
    public static string WriteIndex(GameData data) => JsonSerializer.Serialize(World(data).ToDictionary(t => t.Id, Fingerprint), Indented) + "\n";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>The maps whose photograph is missing or shows the map as it was (none when all are up to date).</summary>
    public static IReadOnlyList<string> Stale(GameData data, string? index)
    {
        Dictionary<string, string> known = string.IsNullOrWhiteSpace(index) ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(index) ?? [];
        return [.. World(data).Where(t => known.GetValueOrDefault(t.Id) != Fingerprint(t)).Select(t => t.Id)];
    }

    public static string File(string id) => id + ".webp";
}
