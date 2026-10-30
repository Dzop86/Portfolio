using System.Text.Json;
using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The items' icons (sprint 98, D72): a game-icons.net drawing each, credited, framed by where the item comes from; since
/// sprint 166 (D87, s165-retours-30-oct.md §4) a tile in relief each, its drawing in the colour of its matter.
/// </summary>
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
        // Every drawing is used and white, for tools/item_art.py to colour it.
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

    private static string Tiles => Path.Combine(Drawings(), "tiles");

    private static Dictionary<string, string> Palette() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Drawings(), "palette.json")))!;

    [Fact]
    public void EveryItem_HasItsTileInReliefAndFlat_RimmedByWhereItComesFrom_DottedByItsElement()
    {
        Assert.Equal(Data.Items.Count, Palette().Count);
        foreach (Item item in Data.Items.Values)
        {
            string tile = File.ReadAllText(Path.Combine(Tiles, ItemStyle.Tile(item) + ".svg"));
            string flat = File.ReadAllText(Path.Combine(Tiles, ItemStyle.Tile(item, locked: true) + ".svg"));
            foreach (string svg in new[] { tile, flat })
            {
                Assert.Contains("viewBox=\"0 0 512 512\"", svg, StringComparison.Ordinal);
                Assert.DoesNotContain("<filter", svg, StringComparison.Ordinal);
                // The dot of its element on equipment that gives something, and only there.
                Assert.Equal(ItemStyle.Element(item) is not null, svg.Contains("cx=\"428\" cy=\"84\"", StringComparison.Ordinal));
            }
            Provenance from = ItemStyle.ProvenanceOf(item, Data);
            Assert.Contains(ItemStyle.Frame(from), tile, StringComparison.Ordinal);
            if (from == Provenance.Chief)
                Assert.Contains(UiStyle.GoldBottom, tile, StringComparison.Ordinal);
            // Its matter's tones in relief; flat, the grey of what is locked.
            Assert.Contains(Palette()[item.Id], tile, StringComparison.Ordinal);
            Assert.Contains("#7a7a7a", flat, StringComparison.Ordinal);
            Assert.DoesNotContain("url(#g-m)", flat, StringComparison.Ordinal);
        }
        foreach (ItemSlot slot in Enum.GetValues<ItemSlot>())
            Assert.True(File.Exists(Path.Combine(Tiles, ItemStyle.Tile(slot) + ".svg")), slot.ToString());
        // Imported at 128 pixels with mipmaps, as the spells': sharp from 40 to 64.
        Assert.All(Directory.GetFiles(Tiles, "*.svg"), f => Assert.Contains("svg/scale=0.25", File.ReadAllText(f + ".import"), StringComparison.Ordinal));
        Assert.All(Directory.GetFiles(Tiles, "*.svg"), f => Assert.Contains("mipmaps/generate=true", File.ReadAllText(f + ".import"), StringComparison.Ordinal));
    }

    [Fact]
    public void NoMatter_IsThePistachioOrAnElementsOwnColour()
    {
        string[] forbidden = [UiStyle.Pistachio, .. new[] { Element.Earth, Element.Fire, Element.Water, Element.Air, Element.Neutral }.Select(ElementStyle.Colour)];
        Assert.All(Palette(), p => Assert.DoesNotContain(p.Value, forbidden));
    }

    [Fact]
    public void NoTwoRecipesOfATrade_NorTwoItemsOfASlotAndLevel_LookTheSame()
    {
        Dictionary<string, string> palette = Palette();
        (string, string) Look(string id) => (Data.Items[id].Icon!, palette[id]);
        foreach (Job job in Data.Jobs.Values)
        {
            string[] made = [.. job.Recipes.Select(r => r.Item)];
            Assert.True(made.Select(Look).Distinct().Count() == made.Length, $"{job.Id}: {string.Join(", ", made.GroupBy(Look).Where(g => g.Count() > 1).Select(g => string.Join("/", g)))}");
        }
        foreach (IGrouping<(ItemSlot?, int), Item> same in Data.Items.Values.Where(i => i.Slot is not null).GroupBy(i => (i.Slot, i.Level)))
            Assert.True(same.Select(i => Look(i.Id)).Distinct().Count() == same.Count(), string.Join(", ", same.Select(i => i.Id)));
    }
}
