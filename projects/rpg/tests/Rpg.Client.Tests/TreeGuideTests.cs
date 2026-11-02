using System.Text.Json;
using System.Text.RegularExpressions;
using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The tree explained (sprint 149, D82): every node with its name, effect, counterpart, line of lore and icon in both
/// languages, the icons credited; the layers and their banners; the build's title for the reference builds, its summary
/// and the ceiling's warning; Helping Hand's elements; the glossary and the tutorial; the tree's colours only in
/// <c>tokens.css</c> and <c>ElementStyle</c>. Since sprint 182 (D93), no pair, no Heart, no advice: the Elements page's.
/// </summary>
public partial class TreeGuideTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static string Repo([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", ".."));

    private static string Rpg => Path.Combine(Repo(), "projects", "rpg");

    [Fact]
    public void EachNode_HasItsTexts_InBothLanguages_AndItsIcon_Credited()
    {
        string icons = Path.Combine(Rpg, "godot", "assets", "icons", "talents");
        var credits = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(icons, "credits.json")))!;
        foreach (TalentSpec t in Data.Talents.Values)
        {
            foreach (LocalizedText? text in new[] { t.Name, t.Description, t.Counterpart, t.Lore })
            {
                Assert.False(string.IsNullOrWhiteSpace(text?.Fr), t.Id);
                Assert.False(string.IsNullOrWhiteSpace(text?.En), t.Id);
            }
            Assert.True(File.Exists(Path.Combine(icons, t.Id + ".svg")), t.Id);
            Assert.Equal(t.Icon, credits[t.Id]);
        }
        // The trunk's three nodes too; every drawing credited, none left without a node.
        foreach (int level in TreeLayout.TrunkLevels)
            Assert.True(File.Exists(Path.Combine(icons, $"trunk-{level}.svg")) && credits.ContainsKey($"trunk-{level}"));
        Assert.Equal(credits.Keys.Order(StringComparer.Ordinal), Directory.GetFiles(icons, "*.svg").Select(f => Path.GetFileNameWithoutExtension(f)).Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("sentinel-matter", "Tempo")]
    [InlineData("sentinel-manner", "Mêlée")]
    [InlineData("sentinel-pure", "Distance")]
    [InlineData("guard-matter", "Tempo")]
    [InlineData("guard-manner", "Alchimie")]
    [InlineData("guard-pure", "Terrain")]
    [InlineData("mage-matter", "Tempo")]
    [InlineData("mage-manner", "Tempo")]
    [InlineData("mage-pure", "Terrain")]
    public void TheReferenceBuilds_HaveTheirTitle(string build, string title)
    {
        Hero hero = Progression.Talented(new Hero("Ondine", "female-a", build.Split('-')[0], Level: 45), build, Data);
        Assert.Equal(title, new Texts("fr").TreeTitle(hero.Talents!, Data));
        Assert.Equal("No talent", new Texts("en").TreeTitle([], Data));
    }

    [Fact]
    public void TheSummary_SaysEveryCounterpart_AndWarnsPastTheCeiling()
    {
        var fr = new Texts("fr");
        Assert.Equal(["Aucune contrepartie.", "Au mieux, +0 % sur un coup."], fr.TreeBuildSummary([], Data));
        IReadOnlyList<string> two = fr.TreeBuildSummary(["up-close", "firm-footing"], Data);
        Assert.Equal("Contreparties : Au plus près (−2 PO à tous tes sorts) ; Pied ferme (−5 % de dégâts dès que tu as bougé ce tour)", two[0]);
        Assert.Equal("Au mieux, +25 % sur un coup.", two[1]);
        // Up close, firm footing, heavy push and close quarters: 10 + 15 + 25 = 50, past the 35 of the ceiling.
        string[] melee = ["up-close", "firm-footing", "heavy-push", "close-quarters"];
        Assert.Equal(50, TreeGuide.Best(melee, Data));
        Assert.Equal("At best, +50% on a hit: anything above +35% is lost.", new Texts("en").TreeBuildSummary(melee, Data)[1]);
    }

    [Fact]
    public void TheCard_SaysTheLayer_TheBanner_TheCounterpart_AndTheLore()
    {
        var layout = new TreeLayout(Data);
        var editor = new PointsEditor(new Hero("Ondine", "female-a", "mage", Level: 40), Data);
        var fr = new Texts("fr");
        TreeCard hand = fr.TreeCard(layout["helping-hand"]!, editor);
        Assert.Equal("Styles · Soutien", hand.Layer);
        Assert.StartsWith("Pour les autres", hand.Banner, StringComparison.Ordinal);
        Assert.Equal("En échange : Tes coups font 5 % de dégâts en moins ; un soutien par tour.", hand.Counterpart);
        Assert.False(string.IsNullOrWhiteSpace(hand.Lore));
        Assert.Equal(Data.Talents["helping-hand"].Name.Fr, fr.TreeName(layout["helping-hand"]!, Data));
        // Helping Hand said with the four elements every hero has (D9 a, sprint 182).
        Assert.StartsWith("Avec tes éléments : Terre, un bouclier de 60 % des dégâts ; Feu,", hand.Extra!.Split('\n')[0], StringComparison.Ordinal);
        Assert.Equal([Element.Earth, Element.Fire, Element.Water, Element.Air], TreeGuide.Helps);
        TreeCard key = fr.TreeCard(layout["devotion"]!, editor);
        Assert.Contains("une clé par style", key.Banner, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Fondations", fr.TreeCard(layout["trunk:30"]!, editor).Layer);
        Assert.Equal([TreeLayer.Foundations, TreeLayer.Styles], Enum.GetValues<TreeLayer>());
    }

    [Fact]
    public void TheGlossary_AndTheTutorial_AreSaid_InBothLanguages()
    {
        foreach (string lang in new[] { "fr", "en" })
        {
            var t = new Texts(lang);
            Assert.Equal(3, t.TreeGlossary.Count);
            Assert.All(t.TreeGlossary, g => Assert.False(string.IsNullOrWhiteSpace(g.Term) || string.IsNullOrWhiteSpace(g.Meaning)));
            Assert.Equal(3, t.TreeTutorial.Count);
            Assert.All(Enum.GetValues<TreeLayer>(), l => Assert.False(string.IsNullOrWhiteSpace(t["tree.layer." + l] + t["tree.banner." + l])));
        }
        Assert.Equal("tree2", Seen.TreeTutorial);
    }

    [GeneratedRegex("#[0-9a-fA-F]{6}")]
    private static partial Regex Hex();

    [Fact]
    public void TheTreesColours_LiveInTheTokensAndElementStyle_Only()
    {
        // Sprint 149 (L4 a): no colour written in the tree's window, each of ElementStyle's in tokens.css.
        string window = File.ReadAllText(Path.Combine(Rpg, "godot", "TreePanel.cs"));
        Assert.Empty(Hex().Matches(window));
        Assert.DoesNotContain("new Color(\"", window, StringComparison.Ordinal);
        string tokens = File.ReadAllText(Path.Combine(Repo(), "src", "assets", "tokens.css")).ToLowerInvariant();
        string style = File.ReadAllText(Path.Combine(Rpg, "src", "Rpg.Client", "ElementStyle.cs"));
        Assert.All(Hex().Matches(style).Select(m => m.Value.ToLowerInvariant()).Distinct(), c => Assert.Contains(c, tokens, StringComparison.Ordinal));
    }
}
