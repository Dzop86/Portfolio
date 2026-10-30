using System.Text.Json;
using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The quests' kinds in relief (sprint 165, D87, s165-retours-30-oct.md §3.2): their icons, greyed, and their colours.</summary>
public class QuestKindStyleTests
{
    private static string Rpg([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    [Fact]
    public void EachKind_HasItsIconInReliefAndInGrey_FromItsCreditedSilhouette()
    {
        string relief = Path.Combine(Rpg(), "godot", "assets", "icons", "relief");
        using JsonDocument palette = JsonDocument.Parse(File.ReadAllText(Path.Combine(relief, "palette.json")));
        using JsonDocument credits = JsonDocument.Parse(File.ReadAllText(Path.Combine(relief, "credits.json")));
        foreach (QuestKind kind in Enum.GetValues<QuestKind>())
        {
            string name = QuestKindStyle.Icon(kind);
            Assert.Equal("quest-" + kind.ToString().ToLowerInvariant(), name);
            JsonElement tone = palette.RootElement.GetProperty(name);
            Assert.Equal(tone.GetProperty("shape").GetString(), credits.RootElement.GetProperty(name).GetString());
            // The colour of its subtitle is the icon's.
            Assert.Equal(QuestKindStyle.Colour(kind), tone.GetProperty("main").GetString());
            foreach (string svg in new[] { name, QuestKindStyle.Icon(kind, locked: true) })
            {
                string body = File.ReadAllText(Path.Combine(relief, svg + ".svg"));
                Assert.DoesNotContain("<filter", body, StringComparison.Ordinal);
                Assert.Contains("viewBox=\"0 0 512 512\"", body, StringComparison.Ordinal);
                Assert.Contains("svg/scale=0.140625", File.ReadAllText(Path.Combine(relief, svg + ".svg.import")), StringComparison.Ordinal);
            }
            Assert.Contains("#8a8a8a", File.ReadAllText(Path.Combine(relief, QuestKindStyle.Icon(kind, true) + ".svg")), StringComparison.Ordinal);
        }
        // The tick of an objective reached, credited too.
        Assert.Equal("delapouite/check-mark", credits.RootElement.GetProperty("check").GetString());
    }

    [Fact]
    public void EachKind_HasItsOwnColour_ReadableOnTheWindow_AndNoneIsThePistachio()
    {
        string[] colours = [.. Enum.GetValues<QuestKind>().Select(QuestKindStyle.Colour)];
        Assert.Equal(colours.Length, colours.Distinct().Count());
        Assert.DoesNotContain(UiStyle.Pistachio, colours);
        Assert.All(colours, c => Assert.True(QuestKindStyle.Contrast(c, UiStyle.Window) >= 4.5, c));
    }

    [Fact]
    public void TheContrast_IsWcags()
    {
        Assert.Equal(21, QuestKindStyle.Contrast("#000000", "#ffffff"), 3);
        Assert.Equal(1, QuestKindStyle.Contrast("#1f1f1f", "#1f1f1f"), 3);
        Assert.Equal(QuestKindStyle.Contrast("#6bb8f2", "#1f1f1f"), QuestKindStyle.Contrast("#1f1f1f", "#6bb8f2"), 9);
    }
}
