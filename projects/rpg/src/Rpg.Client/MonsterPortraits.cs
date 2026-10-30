using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The monsters' portraits (sprint 172, D88): each monster of the bestiary rendered once by the game itself
/// (<c>--render-portraits</c>, its model as in a fight, three quarters, on nothing), kept as
/// <c>assets/bestiary/portraits/&lt;id&gt;.webp</c>, with its silhouette for as long as it is unknown, <c>&lt;id&gt;-unknown.webp</c>:
/// the same shape in matt black, edged with a thin light so that it stands out of the hollow it lies in
/// (<c>docs/graphisme/s165-retours-30-oct.md</c> §6.4). <c>portraits.json</c> keeps the look each one shows, so that a monster
/// whose look changed asks its portrait again. The look's model is drawn by code (<c>godot/Creatures.cs</c>): a change in that
/// code is not seen, the portraits are then rendered again by hand.
/// </summary>
public static class MonsterPortraits
{
    /// <summary>A portrait's side in pixels: the card shows the silhouette at 140, the rows at 52.</summary>
    public const int Size = 160;

    /// <summary>The silhouette's black and its edge of light, and how wide the edge is in pixels.</summary>
    public const string Black = "#0d0d0d", Edge = "#3a3a3d";

    public const int EdgeWidth = 2;

    public const string Folder = "assets/bestiary/portraits", Index = "portraits.json";

    /// <summary>Every monster of the bestiary, by id.</summary>
    public static IEnumerable<MonsterSpec> All(GameData data) =>
        (data ?? throw new ArgumentNullException(nameof(data))).Monsters.Values.OrderBy(m => m.Id, StringComparer.Ordinal);

    /// <summary>What a portrait shows, in short: a hash of the monster's look.</summary>
    public static string Fingerprint(MonsterSpec monster)
    {
        ArgumentNullException.ThrowIfNull(monster);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(monster.Look)))[..16];
    }

    public static string File(string id) => id + ".webp";

    public static string Shadow(string id) => id + "-unknown.webp";

    /// <summary>The picture of a monster as the bestiary knows it: its portrait once met, its silhouette before.</summary>
    public static string Picture(string id, Knowledge known) => Folder + "/" + (known == Knowledge.Unknown ? Shadow(id) : File(id));

    /// <summary>The index of the portraits: each monster's fingerprint, by its id.</summary>
    public static string WriteIndex(GameData data) => JsonSerializer.Serialize(All(data).ToDictionary(m => m.Id, Fingerprint), Indented) + "\n";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>The monsters whose portrait is missing or shows another look (none when all are up to date).</summary>
    public static IReadOnlyList<string> Stale(GameData data, string? index)
    {
        Dictionary<string, string> known = string.IsNullOrWhiteSpace(index) ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(index) ?? [];
        return [.. All(data).Where(m => known.GetValueOrDefault(m.Id) != Fingerprint(m)).Select(m => m.Id)];
    }

    /// <summary>
    /// The silhouette of a portrait (RGBA, 8 bits a channel, row after row): each pixel of the model in <see cref="Black"/>
    /// at its own opacity, and around the shape, on <see cref="EdgeWidth"/> pixels of what was empty, <see cref="Edge"/> at
    /// the opacity of the nearest pixel of the model. Nothing of the model's colours is kept.
    /// </summary>
    public static byte[] Silhouette(ReadOnlySpan<byte> rgba, int width, int height)
    {
        if (width <= 0 || height <= 0 || rgba.Length != width * height * 4)
            throw new ArgumentException("The picture is not width × height RGBA pixels.", nameof(rgba));
        (byte r, byte g, byte b) black = Rgb(Black), edge = Rgb(Edge);
        var shadow = new byte[rgba.Length];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                byte alpha = rgba[i + 3];
                (byte r, byte g, byte b) colour = black;
                if (alpha < 128)
                {
                    // Empty (or nearly): the edge, if the model is close.
                    byte near = 0;
                    for (int dy = -EdgeWidth; dy <= EdgeWidth; dy++)
                        for (int dx = -EdgeWidth; dx <= EdgeWidth; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && ny >= 0 && nx < width && ny < height && dx * dx + dy * dy <= EdgeWidth * EdgeWidth)
                                near = Math.Max(near, rgba[(ny * width + nx) * 4 + 3]);
                        }
                    if (near >= 128)
                        (colour, alpha) = (edge, near);
                }
                (shadow[i], shadow[i + 1], shadow[i + 2], shadow[i + 3]) = (colour.r, colour.g, colour.b, alpha);
            }
        return shadow;
    }

    /// <summary>
    /// How far from its upright axis a model reaches, whichever way it turns (the bestiary's turning portrait, framed once on
    /// it): the farthest of its box's corners seen from above. A wolf, long and narrow, needs as much room facing the viewer
    /// as seen from the side.
    /// </summary>
    public static double TurningRadius(double minX, double maxX, double minZ, double maxZ) =>
        Math.Sqrt(Math.Max(minX * minX, maxX * maxX) + Math.Max(minZ * minZ, maxZ * maxZ));

    private static (byte, byte, byte) Rgb(string hex) =>
        (Convert.ToByte(hex[1..3], 16), Convert.ToByte(hex[3..5], 16), Convert.ToByte(hex[5..7], 16));
}
