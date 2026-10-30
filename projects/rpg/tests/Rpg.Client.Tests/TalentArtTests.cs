using Rpg.Core;
using System.Text.Json;

namespace Rpg.Client.Tests;

/// <summary>
/// The talents' icons in relief (sprint 164, <c>docs/graphisme/s163-icones-arbre.md</c> §3.5, <c>s164-arbre-maquette.md</c>
/// §3.4), drawn by <c>tools/talent_art.py</c>: three files a node of the tree (in its family's colour, open in light greys,
/// locked flat grey), from its credited silhouette, without any filter, never in pistachio.
/// </summary>
public class TalentArtTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static string Rpg([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static string Relief => Path.Combine(Rpg(), "godot", "assets", "icons", "talents", "relief");

    private static string Svg(string name) => File.ReadAllText(Path.Combine(Relief, name + ".svg"));

    private static string ShapeOf(string name) =>
        File.ReadAllText(Path.Combine(Rpg(), "godot", "assets", "icons", "bar", "shapes", name.Replace('/', '_') + ".path")).Trim();

    [Fact]
    public void EachNodeOfTheTree_IsDrawnThreeTimes_FromItsCreditedSilhouette_WithoutAFilter()
    {
        var credits = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Relief, "..", "credits.json")))!;
        string[] nodes = [.. Data.Talents.Keys, .. TreeLayout.TrunkLevels.Select(l => $"trunk-{l}")];
        Assert.Equal(59, nodes.Length);
        foreach (string id in nodes)
        {
            string path = ShapeOf(credits[id]);
            foreach (string suffix in new[] { "", "-open", "-locked" })
            {
                string svg = Svg(id + suffix);
                Assert.Contains("viewBox=\"0 0 512 512\"", svg, StringComparison.Ordinal);
                Assert.DoesNotContain("<filter", svg, StringComparison.Ordinal);
                Assert.Contains($"d=\"{path}\"", svg, StringComparison.Ordinal);
                // Pistachio says "taken" (the node's ring): never an icon's colour.
                Assert.DoesNotContain("#bef374", svg, StringComparison.OrdinalIgnoreCase);
            }
            Assert.Contains("#cfcfcf", Svg(id + "-open"), StringComparison.Ordinal);
            Assert.Contains("fill=\"#7a7a7a\"", Svg(id + "-locked"), StringComparison.Ordinal);
        }
        Assert.Equal(nodes.Length * 3, Directory.GetFiles(Relief, "*.svg").Length);
        // The Terrain's relay no longer a blade (§1.9): the back and forth of the spell it gives.
        Assert.Equal("lorc/back-forth", Data.Talents["relay-talent"].Icon);
    }

    [Theory]
    [InlineData("heart-fire", "#ef4b4b")]
    [InlineData("electrifying", "#f2d03b")]
    [InlineData("bogging", "#8c7a3a")]
    [InlineData("convert-lightning", "#f2d03b")]
    [InlineData("artillery", "#6bb8f2")]
    [InlineData("devotion", "#ff7eb6")]
    [InlineData("seal", "#c9a27f")]
    [InlineData("reserve", "#b48cff")]
    [InlineData("catalysis", "#4fd1c5")]
    [InlineData("up-close", "#e8a33d")]
    [InlineData("trunk-30", "#ffd166")]
    public void ANode_IsInItsFamilysColour(string id, string main) =>
        Assert.Contains($"stop-color=\"{main}\"", Svg(id), StringComparison.Ordinal);

    [Fact]
    public void AStyle_IsInItsBranchsColour_AsTheTreeWritesIt()
    {
        foreach (TalentSpec t in Data.Talents.Values.Where(t => t.Branch is Branch.Melee or Branch.Distance or Branch.Support or Branch.Terrain or Branch.Tempo or Branch.Alchemy))
            Assert.Contains($"stop-color=\"{ElementStyle.StyleColour(t.Branch)}\"", Svg(t.Id), StringComparison.Ordinal);
    }
}
