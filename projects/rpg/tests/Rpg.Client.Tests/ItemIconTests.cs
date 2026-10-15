using System.Text.Json;
using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The items' icons (sprint 98, D72): a game-icons.net drawing each, credited, framed by where the item comes from.</summary>
public class ItemIconTests
{
    private static GameData Data => GameData.Embedded;

    /// <summary>The game's drawings, found from the test's folder up to the project.</summary>
    private static string Drawings()
    {
        for (DirectoryInfo? d = new(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            string path = Path.Combine(d.FullName, "godot", "assets", "icons", "items");
            if (Directory.Exists(path))
                return path;
        }
        throw new DirectoryNotFoundException("godot/assets/icons/items");
    }

    [Fact]
    public void EveryItem_HasADrawing_AndEveryDrawingItsAuthor()
    {
        string dir = Drawings();
        var credits = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(dir, "credits.json")))!;
        foreach (Item item in Data.Items.Values)
        {
            Assert.False(string.IsNullOrEmpty(item.Icon), item.Id);
            string name = item.Icon!.Split('/')[1];
            Assert.True(File.Exists(Path.Combine(dir, name + ".svg")), item.Icon);
            Assert.Equal(item.Icon, credits[name]);
        }
        // Every drawing is used and white, for the game to tint it.
        foreach (string file in Directory.GetFiles(dir, "*.svg"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            Assert.Contains(Data.Items.Values, i => i.Icon!.EndsWith("/" + name, StringComparison.Ordinal));
            Assert.DoesNotContain("M0 0h512v512H0z", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("king-tusk", Provenance.Chief)]
    [InlineData("leader-fang", Provenance.Chief)]
    [InlineData("wolf-boots", Provenance.Set)]
    [InlineData("boar-wallow-key", Provenance.Craft)]
    [InlineData("copper-ring", Provenance.Common)]
    [InlineData("boar-bristle", Provenance.Common)]
    public void TheFrame_SaysWhereTheItemComesFrom(string id, Provenance provenance)
    {
        Assert.Equal(provenance, ItemStyle.ProvenanceOf(Data.Items[id], Data));
        Assert.Equal(4, Enum.GetValues<Provenance>().Select(ItemStyle.Frame).Distinct().Count());
    }
}
