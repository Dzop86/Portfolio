namespace Rpg.Client;

/// <summary>
/// The atlas's colours (sprint 167, D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §5.4), tokens of <c>tokens.css</c>
/// (<c>--osmose-map-*</c>): each zone its biome's, lighter and less saturated than its sky for a map to read; the sea from
/// the open to the coast, the coast's line, the beach, the zones' edges, the trees' stamps and the roads' dots.
/// </summary>
public static class AtlasStyle
{
    public const string Sea = "#3f86c4", SeaDeep = "#2a5f94", Shore = "#e8d9a8", Coast = "#1d4a75", Tree = "#2e6b3a", Road = "#e6e1d2";

    /// <summary>The zones' names (sprint 168): parchment, edged in the ground's black (--osmose-ground).</summary>
    public const string Parchment = "#f0e6cc", Ink = "#1b1b1b";

    /// <summary>A zone's biome: the woods', the mouldy woods', the lake's, the fields', the ancestors' forest, or a town's paving.</summary>
    public static string Biome(string zone) => zone switch
    {
        "mouldy" => "#8f9862",
        "lake" => "#72bd7c",
        "fields" => "#c4c76a",
        "ancestors" => "#4f7f48",
        "clairval" or "four-ways" => "#d9c39a",
        _ => "#6fae5a",
    };

    /// <summary>Every colour of the atlas, for the test that finds each one in <c>tokens.css</c>.</summary>
    public static IReadOnlyList<string> All { get; } =
        [Sea, SeaDeep, Shore, Coast, Tree, Road, Parchment, Ink, .. new[] { "woods", "mouldy", "lake", "fields", "ancestors", "clairval" }.Select(Biome)];
}
