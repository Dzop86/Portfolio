using System.Text.Json;
using System.Text.RegularExpressions;

namespace Rpg.Client.Tests;

/// <summary>
/// The spells' icons in relief (sprint 163, <c>docs/graphisme/s163-icones-arbre.md</c> §1), drawn by <c>tools/spell_art.py</c>:
/// every spell with a silhouette drawn three times (whole, flat when locked, without its marks when small) from its credited
/// silhouette, without any SVG filter, imported at a quarter with mipmaps; a twin marked with its origin, a Seal with its
/// element's accent, a compound's rim a gradient of its parents, a light element's silhouette engraved.
/// </summary>
public class SpellArtTests
{
    private static string Rpg([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static string Spells => Path.Combine(Rpg(), "godot", "assets", "icons", "spells");

    private static string Svg(string id, string suffix = "") => File.ReadAllText(Path.Combine(Spells, id + suffix + ".svg"));

    private static Dictionary<string, JsonElement> Catalog()
    {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Rpg(), "data", "spells.json")));
        return doc.RootElement.EnumerateArray().ToDictionary(s => s.GetProperty("id").GetString()!, s => s.Clone());
    }

    private static string? Text(JsonElement spell, string name) => spell.TryGetProperty(name, out JsonElement v) ? v.GetString() : null;

    private static string ShapeOf(string name) =>
        File.ReadAllText(Path.Combine(Rpg(), "godot", "assets", "icons", "bar", "shapes", name.Replace('/', '_') + ".path")).Trim();

    [Fact]
    public void EachSpellWithASilhouette_IsDrawnThreeTimes_FromItsCreditedSilhouette_WithoutAFilter()
    {
        Dictionary<string, JsonElement> spells = Catalog();
        using JsonDocument credits = JsonDocument.Parse(File.ReadAllText(Path.Combine(Spells, "credits.json")));
        string[] drawn = [.. spells.Where(s => Text(s.Value, "icon") is not null).Select(s => s.Key)];
        Assert.Equal(102, drawn.Length);
        Assert.Equal(drawn.Order(), credits.RootElement.EnumerateObject().Select(p => p.Name).Order());
        // The classes' spells and the tree's are all drawn (the monsters' show a letter).
        using JsonDocument classes = JsonDocument.Parse(File.ReadAllText(Path.Combine(Rpg(), "data", "classes.json")));
        string[] needed = [.. classes.RootElement.EnumerateArray().SelectMany(c => c.GetProperty("spells").EnumerateArray().Select(s => s.GetString()!)),
            "relay", "seal-earth", "seal-fire", "seal-water", "seal-air", "decoy"];
        Assert.All(needed, id => Assert.Contains(id, drawn));
        foreach (string id in drawn)
        {
            string icon = Text(spells[id], "icon")!;
            Assert.Equal(icon, credits.RootElement.GetProperty(id).GetString());
            string path = ShapeOf(icon);
            foreach (string suffix in new[] { "", "-locked", "-small" })
            {
                string svg = Svg(id, suffix);
                Assert.Contains("viewBox=\"0 0 512 512\"", svg, StringComparison.Ordinal);
                Assert.DoesNotContain("<filter", svg, StringComparison.Ordinal);
                Assert.Contains($"d=\"{path}\"", svg, StringComparison.Ordinal);
                // Imported at 128 pixels with mipmaps: sharp in the deck's 32-pixel places and the book's 48.
                string import = File.ReadAllText(Path.Combine(Spells, id + suffix + ".svg.import"));
                Assert.Contains("svg/scale=0.25", import, StringComparison.Ordinal);
                Assert.Contains("mipmaps/generate=true", import, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void ALockedSpell_IsFlat_AndASmallOne_HasNoMarks()
    {
        foreach (string id in Catalog().Where(s => Text(s.Value, "icon") is not null).Select(s => s.Key))
        {
            string whole = Svg(id), locked = Svg(id, "-locked"), small = Svg(id, "-small");
            // In relief: the drop shadow and the bevel; flat: neither, a grey silhouette.
            Assert.Contains("url(#bevel-s)", whole, StringComparison.Ordinal);
            Assert.DoesNotContain("url(#bevel-s)", locked, StringComparison.Ordinal);
            Assert.DoesNotContain("opacity=\"0.45\"/><rect", locked, StringComparison.Ordinal);
            Assert.Contains("fill=\"#7a7a7a\"", locked, StringComparison.Ordinal);
            // Small: no origin dot, no Seal's accent.
            Assert.DoesNotContain("<circle", small, StringComparison.Ordinal);
            Assert.DoesNotContain("translate(300 36)", small, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ATwin_CarriesItsOriginsDot_ASeal_ItsAccent_ACompound_ItsParentsRim()
    {
        Dictionary<string, JsonElement> spells = Catalog();
        int twins = 0;
        foreach ((string id, JsonElement spell) in spells.Where(s => Text(s.Value, "icon") is not null))
        {
            string whole = Svg(id);
            bool twin = Text(spell, "twin") is not null;
            Assert.Equal(twin ? 1 : 0, Regex.Count(whole, "<circle cx=\"424\" cy=\"424\""));
            Assert.Equal(twin ? 1 : 0, Regex.Count(Svg(id, "-locked"), "<circle cx=\"424\" cy=\"424\""));
            twins += twin ? 1 : 0;
            // Lightning, the one light compound drawn: its rim from Fire to Air, its silhouette engraved.
            bool lightning = Text(spell, "element") == "Lightning";
            Assert.Equal(lightning, whole.Contains("url(#rim-s)", StringComparison.Ordinal));
            Assert.Equal(lightning, whole.Contains("url(#gf-s)", StringComparison.Ordinal));
            if (lightning)
            {
                Assert.Contains("stop-color=\"#ef4b4b\"", whole, StringComparison.Ordinal);
                Assert.Contains("stop-color=\"#52c45a\"", whole, StringComparison.Ordinal);
            }
        }
        Assert.True(twins > 0);
        foreach ((string seal, string heart) in new[] { ("seal-earth", "lorc/stone-sphere"), ("seal-fire", "lorc/fire-ring"), ("seal-water", "lorc/drop"), ("seal-air", "lorc/whirlwind") })
        {
            Assert.Contains("translate(300 36)", Svg(seal), StringComparison.Ordinal);
            Assert.Contains($"d=\"{ShapeOf(heart)}\"", Svg(seal), StringComparison.Ordinal);
        }
    }
}
