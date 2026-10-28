using Rpg.Core;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Rpg.Client.Tests;

/// <summary>
/// The interface's one theme and the menu's icons in relief (sprint 157, D84, <c>docs/graphisme/s157-maquettes.md</c>):
/// every colour of the theme a token of <c>tokens.css</c>, every icon drawn by <c>tools/ui_art.py</c> from its credited
/// silhouette without any SVG filter, every frame the theme stretches there.
/// </summary>
public partial class InterfaceThemeTests
{
    private static string Repo([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", ".."));

    private static string Rpg => Path.Combine(Repo(), "projects", "rpg");

    private static string Bar => Path.Combine(Rpg, "godot", "assets", "icons", "bar");

    private static string Tokens => File.ReadAllText(Path.Combine(Repo(), "src", "assets", "tokens.css")).ToLowerInvariant();

    [GeneratedRegex("#[0-9a-fA-F]{6}(?:[0-9a-fA-F]{2})?\\b")]
    private static partial Regex Hex();

    [Fact]
    public void EachColourOfTheInterface_IsATokenOfTheCharter()
    {
        string tokens = Tokens;
        Assert.All(UiStyle.All, c => Assert.Contains(c.ToLowerInvariant() + ";", tokens, StringComparison.Ordinal));
        // The theme writes no other colour, but for black and white made see-through (shadows, lines, hollows).
        string theme = File.ReadAllText(Path.Combine(Rpg, "godot", "OsmoseTheme.cs"));
        Assert.All(Hex().Matches(theme).Select(m => m.Value.ToLowerInvariant()).Distinct(), c =>
            Assert.True(tokens.Contains(c + ";", StringComparison.Ordinal) || c.StartsWith("#000000", StringComparison.Ordinal) || c.StartsWith("#ffffff", StringComparison.Ordinal), c));
        // The Menus panel and the Escape menu take theirs from the theme alone.
        foreach (string window in new[] { "SideBar.cs", "MenuPanel.cs", "AchievementsPanel.cs" })
            Assert.Empty(Hex().Matches(File.ReadAllText(Path.Combine(Rpg, "godot", window))));
    }

    [Fact]
    public void EachMenuIcon_IsInRelief_FromItsCreditedSilhouette_WithoutAFilter()
    {
        using JsonDocument palette = JsonDocument.Parse(File.ReadAllText(Path.Combine(Bar, "palette.json")));
        using JsonDocument credits = JsonDocument.Parse(File.ReadAllText(Path.Combine(Bar, "credits.json")));
        string[] names = [.. palette.RootElement.EnumerateObject().Select(p => p.Name)];
        Assert.Equal(13, names.Length);
        foreach (string name in names)
        {
            JsonElement tone = palette.RootElement.GetProperty(name);
            string svg = File.ReadAllText(Path.Combine(Bar, name + ".svg"));
            Assert.Contains("viewBox=\"0 0 512 512\"", svg, StringComparison.Ordinal);
            Assert.DoesNotContain("<filter", svg, StringComparison.Ordinal);
            AssertLayers(svg, tone, "main");
            Assert.Equal(tone.GetProperty("shape").GetString(), credits.RootElement.GetProperty(name).GetString());
            if (tone.TryGetProperty("accent", out JsonElement accent))
            {
                AssertLayers(svg, accent, "acc");
                Assert.Equal(accent.GetProperty("shape").GetString(), credits.RootElement.GetProperty("accents").GetProperty(name).GetString());
            }
        }
        // Two neighbours of the grid never share a colour.
        Assert.Equal(names.Length, names.Select(n => palette.RootElement.GetProperty(n).GetProperty("main").GetString()).Distinct().Count());
    }

    private static void AssertLayers(string svg, JsonElement tone, string key)
    {
        string path = File.ReadAllText(Path.Combine(Bar, "shapes", tone.GetProperty("shape").GetString()!.Replace('/', '_') + ".path")).Trim();
        // The shadow, the outline, the gradient's fill and the highlight's clip: the silhouette four times.
        Assert.Equal(4, Regex.Count(svg, Regex.Escape($"d=\"{path}\"")));
        foreach (string part in new[] { "light", "main", "dark", "outline" })
            Assert.Contains(tone.GetProperty(part).GetString()!, svg, StringComparison.Ordinal);
        Assert.Contains($"fill=\"url(#g-{key})\"", svg, StringComparison.Ordinal);
        Assert.Contains($"clip-path=\"url(#c-{key})\"", svg, StringComparison.Ordinal);
        Assert.Contains("opacity=\"0.45\"", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void EachFrameOfTheTheme_IsDrawn_AndEveryVariationIsNamedOnce()
    {
        string theme = File.ReadAllText(Path.Combine(Rpg, "godot", "OsmoseTheme.cs"));
        string[] frames = [.. Regex.Matches(theme, "Raised\\(\"([a-z-]+)\"").Select(m => m.Groups[1].Value).Concat(
            Regex.Matches(theme, "\"(?:secondary|primary|tile)[a-z-]*\"").Select(m => m.Value.Trim('"'))).Distinct()];
        Assert.NotEmpty(frames);
        Assert.All(frames, f =>
        {
            string svg = File.ReadAllText(Path.Combine(Rpg, "godot", "assets", "ui", "frames", f + ".svg"));
            Assert.DoesNotContain("<filter", svg, StringComparison.Ordinal);
        });
        Assert.True(File.Exists(Path.Combine(Rpg, "godot", "assets", "ui", "grid.svg")));
        // The project's theme is the one the game fills at start.
        Assert.Contains("theme/custom=\"res://assets/ui/osmose.theme.tres\"", File.ReadAllText(Path.Combine(Rpg, "godot", "project.godot")), StringComparison.Ordinal);
    }

    [Fact]
    public void EachCategoryOfAchievements_HasItsIconInColourAndInGrey_AndItsBanner()
    {
        string relief = Path.Combine(Rpg, "godot", "assets", "icons", "relief");
        using JsonDocument credits = JsonDocument.Parse(File.ReadAllText(Path.Combine(relief, "credits.json")));
        Assert.Equal("delapouite/dungeon-gate", credits.RootElement.GetProperty("dungeons").GetString());
        foreach (string category in Achievements.All.Select(a => a.Category).Distinct())
        {
            string icon = AchievementsPage.Icon(category);
            Assert.True(File.Exists(Path.Combine(icon == "dungeons" ? relief : Bar, icon + ".svg")), icon);
            string grey = File.ReadAllText(Path.Combine(relief, icon + "-locked.svg"));
            Assert.Contains("#8a8a8a", grey, StringComparison.Ordinal);
            string banner = File.ReadAllText(Path.Combine(Rpg, "godot", "assets", "ui", "achievements", $"banner-{category}.svg"));
            Assert.Contains("width=\"704\" height=\"192\"", banner, StringComparison.Ordinal);
            Assert.DoesNotContain("<filter", banner, StringComparison.Ordinal);
        }
        Assert.True(File.Exists(Path.Combine(relief, "achievements-locked.svg")));
    }
}
