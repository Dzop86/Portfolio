using System.Text.Json;
using System.Text.RegularExpressions;
using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The tree explained (sprint 149, D82): every node with its name, effect, counterpart, line of lore and icon in both
/// languages, the icons credited; the layers and their banners; the build's title for the reference builds, its summary
/// and the ceiling's warning; the pair the characteristics point to; Helping Hand's elements; the glossary and the
/// tutorial; the tree's colours only in <c>tokens.css</c> and <c>ElementStyle</c>.
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
    [InlineData("sentinel-matter", "Foudre convertie · Boue")]
    [InlineData("sentinel-manner", "Boue · Mêlée")]
    [InlineData("sentinel-pure", "Cœur d'Air · Distance")]
    [InlineData("guard-matter", "Foudre convertie · Boue")]
    [InlineData("guard-manner", "Glace et Magma · Alchimie")]
    [InlineData("guard-pure", "Cœur de Terre · Terrain")]
    [InlineData("mage-matter", "Foudre convertie · Tempo")]
    [InlineData("mage-manner", "Vapeur · Tempo")]
    [InlineData("mage-pure", "Cœur de Feu · Terrain")]
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
        Assert.Equal(["Aucune contrepartie.", "Au mieux, +0 % sur un coup."], fr.TreeBuildSummary(["bogging"], Data));
        IReadOnlyList<string> two = fr.TreeBuildSummary(["bogging", "up-close", "firm-footing"], Data);
        Assert.Equal("Contreparties : Au plus près (−2 PO à tous tes sorts) ; Pied ferme (−5 % de dégâts dès que tu as bougé ce tour)", two[0]);
        Assert.Equal("Au mieux, +25 % sur un coup.", two[1]);
        // Up close, firm footing, heavy push and close quarters: 10 + 15 + 25 = 50, past the 35 of the ceiling.
        string[] melee = ["bogging", "up-close", "firm-footing", "heavy-push", "close-quarters"];
        Assert.Equal(50, TreeGuide.Best(melee, Data));
        Assert.Equal("At best, +50% on a hit: beyond +35%, the rest is lost.", new Texts("en").TreeBuildSummary(melee, Data)[1]);
    }

    [Fact]
    public void TheAdvice_PointsToThePairOfTheTwoHighest_UntilAPairIsTaken()
    {
        var hero = new Hero("Ondine", "female-a", "guard", Level: 30, Stats: new Characteristics(Vitality: 10, Earth: 40, Fire: 5, Water: 30, Air: 0));
        Assert.Equal("bogging", TreeGuide.Advice(hero, Data));
        Assert.Equal("Conseil : Terre et Eau, tes caractéristiques les plus hautes, vont à Boue.", new Texts("fr").TreeAdvice(new PointsEditor(hero, Data)));
        Assert.Null(new Texts("fr").TreeAdvice(new PointsEditor(hero with { Talents = ["chilling"] }, Data)));
        Assert.Null(TreeGuide.Advice(hero with { Stats = new Characteristics(Earth: 10) }, Data));
    }

    [Fact]
    public void TheCard_SaysTheLayer_TheBanner_ThePairsEquation_TheCounterpart_AndTheLore()
    {
        var layout = new TreeLayout(Data, Branch.Support, 934, 642);
        var editor = new PointsEditor(new Hero("Ondine", "female-a", "mage", Level: 40, Talents: ["electrifying"]), Data);
        var fr = new Texts("fr");
        TreeCard pair = fr.TreeCard(layout["electrifying"]!, editor);
        Assert.Equal("Affinités", pair.Layer);
        Assert.Equal("Foudre = Feu + Air", pair.Equation);
        Assert.Equal("Foudre", fr.TreeName(layout["electrifying"]!, Data));
        Assert.Equal("Le feu qui court dans l'air ne choisit pas où il tombe.", pair.Lore);
        TreeCard hand = fr.TreeCard(layout["helping-hand"]!, editor);
        Assert.Equal("Styles · Soutien", hand.Layer);
        Assert.StartsWith("Pour les autres", hand.Banner, StringComparison.Ordinal);
        Assert.Equal("En échange : Tes coups font 5 % de moins ; un soutien par tour.", hand.Counterpart);
        // Helping Hand said with the hero's elements (D9 a): Electrifying's Fire and Air.
        Assert.Equal("Avec tes éléments : Feu, un soin de 40 % des dégâts (15 % de ses PV au plus) ; Air, 1 PM tous les 2 PA (2 au plus) et 1 PO", hand.Extra);
        Assert.Equal([Element.Earth, Element.Fire, Element.Water, Element.Air], TreeGuide.Helps([], Data));
        TreeCard key = fr.TreeCard(layout["devotion"]!, editor);
        Assert.Contains("une clé par style", key.Banner, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Fondations", fr.TreeCard(layout["trunk:30"]!, editor).Layer);
        Assert.Equal("Heart: Fire", new Texts("en").TreeCard(layout["heart-fire"]!, editor).Name.Split(" · ")[0]);
        Assert.Equal("Fire", new Texts("en").TreeName(layout["heart-fire"]!, Data));
    }

    [Fact]
    public void TheGlossary_AndTheTutorial_AreSaid_InBothLanguages()
    {
        foreach (string lang in new[] { "fr", "en" })
        {
            var t = new Texts(lang);
            Assert.Equal(6, t.TreeGlossary.Count);
            Assert.All(t.TreeGlossary, g => Assert.False(string.IsNullOrWhiteSpace(g.Term) || string.IsNullOrWhiteSpace(g.Meaning)));
            Assert.Equal(3, t.TreeTutorial.Count);
            Assert.All(Enum.GetValues<TreeLayer>(), l => Assert.False(string.IsNullOrWhiteSpace(t["tree.layer." + l] + t["tree.banner." + l])));
        }
        Assert.Equal("tree-tutorial", Seen.TreeTutorial);
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
