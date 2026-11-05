using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The Elements page (sprint 183, D93, docs/elements-specialisations.md §v3.3): the base elements' bars, the wheel of the fusions, the tiers.</summary>
public class ElementsPageTests
{
    private static GameData Data => GameData.Embedded;

    private static PointsEditor Editor(int level, Characteristics? stats = null, bool readOnly = false, params Element[] compounds) =>
        new(new Hero("Ondine", "female-a", "mage", Level: level, Stats: stats, Compounds: compounds.Length == 0 ? null : compounds), Data, readOnly);

    [Fact]
    public void TheWheel_HasTheSixPairs_AndTheFourTrios_EachOfDistinctParents()
    {
        var page = new ElementsPage(Editor(50));
        Assert.Equal(6, page.Fusions.Count(f => f.Parents.Count == 2));
        Assert.Equal(4, page.Fusions.Count(f => f.Parents.Count == 3));
        Assert.Equal(10, page.Fusions.Select(f => string.Join('+', f.Parents.Order())).Distinct().Count());
        Assert.All(page.Fusions, f => Assert.Equal(f.Parents.Count == 2 ? Elements.Pairs : Elements.Trios, f.Level));
        Assert.Equal([Element.Fire, Element.Air], page.Fusions.Single(f => f.Id == "lightning").Parents);
    }

    [Fact]
    public void EachPair_NamesItsReaction_FromTheGamesData()
    {
        var page = new ElementsPage(Editor(30));
        Assert.All(page.Fusions.Where(f => f.Parents.Count == 2), f =>
        {
            Assert.NotNull(f.Reaction);
            Assert.True(f.Reaction!.Joins(f.Parents[0], f.Parents[1]));
        });
        // A trio's reaction is its own, not a pair's (sprint 185).
        Assert.All(page.Fusions.Where(f => f.Parents.Count == 3), f => Assert.Equal(f.Compound, f.Reaction!.Trio));
    }

    [Fact]
    public void TheStates_FollowTheLevel_WhatTheGameHas_AndWhatIsCreated()
    {
        FusionState State(ElementsPage p, string id) => p.Fusions.Single(f => f.Id == id).State;
        var at29 = new ElementsPage(Editor(29));
        Assert.Equal(FusionState.Locked, State(at29, "lightning"));
        Assert.Equal(FusionState.Locked, State(at29, "ether"));
        var at30 = new ElementsPage(Editor(30));
        Assert.Equal(FusionState.Open, State(at30, "lightning"));
        Assert.Equal(FusionState.Locked, State(at30, "ether"));
        var at50 = new ElementsPage(Editor(50, compounds: Element.Lightning));
        Assert.Equal(FusionState.Created, State(at50, "lightning"));
        Assert.Equal(FusionState.Open, State(at30, "ice"));
        // Sprint 187 (D94): Lightning holds Fire and Air.
        Assert.Equal(FusionState.Blocked, State(at50, "ether"));
        Assert.Equal(FusionState.Blocked, State(at50, "ice"));
        Assert.Equal(FusionState.Open, State(at50, "mud"));
        Assert.Equal(FusionState.Open, State(new ElementsPage(Editor(50)), "ether"));
    }

    [Fact]
    public void CreateAndDissolve_ChangeTheDraft_ForFree_AndTheTwinsFollow()
    {
        PointsEditor editor = Editor(30);
        var page = new ElementsPage(editor);
        Fusion lightning = page.Fusions.Single(f => f.Id == "lightning");
        Assert.True(page.Press(lightning));
        Assert.Equal([Element.Lightning], editor.CompoundsCreated);
        Assert.True(editor.Changed);
        Assert.Equal(0, editor.ResetCost);
        // Sprint 187 (D94): the twins come at once, no preview.
        Assert.Empty(editor.Previewed);
        Assert.Contains(editor.Spells, s => s.Element == Element.Lightning);
        Assert.Equal(FusionState.Created, page.Fusions.Single(f => f.Id == "lightning").State);
        Assert.True(page.Press(page.Fusions.Single(f => f.Id == "lightning")));
        Assert.Empty(editor.CompoundsCreated);
        Assert.False(editor.Changed);
    }

    [Theory]
    [InlineData("mage")]
    [InlineData("warrior")]
    [InlineData("archer")]
    public void ACompound_GivesTheTwinsOfWhatTheLevelUnlocked_AtOnce(string id)
    {
        var at30 = new PointsEditor(new Hero("Ondine", "female-a", id, Level: 30), Data);
        var page = new ElementsPage(at30);
        Fusion lightning = page.Fusions.Single(f => f.Id == "lightning");
        int said = page.SpellsOf(lightning);
        Assert.InRange(said, 1, 8);
        page.Press(lightning);
        Assert.Empty(at30.Previewed);
        Assert.Equal(said, at30.Spells.Count(s => s.Element == Element.Lightning));
        // The number stays what the fusion gives once created, and the book has them once saved.
        Assert.Equal(said, page.SpellsOf(page.Fusions.Single(f => f.Id == "lightning")));
        Assert.Equal(said, new PointsEditor(at30.Hero with { Compounds = [Element.Lightning] }, Data).Spells.Count(s => s.Element == Element.Lightning));
        // At 100, all eight (docs/elements-specialisations.md §v3.1).
        var top = new ElementsPage(new PointsEditor(new Hero("Ondine", "female-a", id, Level: 100), Data));
        Assert.Equal(8, top.SpellsOf(top.Fusions.Single(f => f.Id == "lightning")));
        Assert.Equal(8, top.SpellsOf(top.Fusions.Single(f => f.Id == "ice")));
        Assert.Equal(12, top.SpellsOf(top.Fusions.Single(f => f.Id == "ether")));
    }

    [Theory]
    [InlineData(29, false)]
    [InlineData(30, true)]
    public void NothingHappens_OnALockedNode_NorInAFight(int level, bool pressed)
    {
        PointsEditor editor = Editor(level);
        var page = new ElementsPage(editor);
        Assert.Equal(pressed, page.Press(page.Fusions.Single(f => f.Id == "lightning")));
        Assert.Equal(pressed ? 1 : 0, editor.CompoundsCreated.Count);
        PointsEditor fight = Editor(30, readOnly: true);
        var shown = new ElementsPage(fight);
        Assert.False(shown.Press(shown.Fusions.Single(f => f.Id == "lightning")));
        Assert.Empty(fight.CompoundsCreated);
        // A trio at 30.
        Assert.False(new ElementsPage(Editor(30)).Press(new ElementsPage(Editor(30)).Fusions.Single(f => f.Id == "ether")));
    }

    [Fact]
    public void TheBars_ShowTheDraftsPoints_AgainstTheThresholdOfD85()
    {
        // 3 × 30 + 5 = 95 where a quarter starts to count.
        var page = new ElementsPage(Editor(30, new Characteristics(Earth: 40, Fire: 120)));
        Assert.Equal([Element.Earth, Element.Fire, Element.Water, Element.Air], page.Bars.Select(b => b.Element));
        ElementBar earth = page.Bars[0], fire = page.Bars[1];
        Assert.Equal((40, 95, Characteristics.Bonus(40), false), (earth.Total, earth.Threshold, earth.Bonus, earth.Over));
        Assert.Equal((120, 95, true), (fire.Total, fire.Threshold, fire.Over));
        Assert.Equal(Characteristics.Bonus(Characteristics.Effective(120, 95)), fire.Bonus);
        Assert.InRange(earth.Fill, 0.42, 0.43);
        Assert.Equal(1, fire.Fill);
    }

    [Fact]
    public void TheTiers_Are30_50_100_ReachedByTheLevel()
    {
        Assert.Equal([30, 50, 100], new ElementsPage(Editor(1)).Tiers.Select(t => t.Level));
        Assert.Equal([true, false, false], new ElementsPage(Editor(30)).Tiers.Select(t => t.Reached));
        Assert.Equal([true, true, false], new ElementsPage(Editor(99)).Tiers.Select(t => t.Reached));
        Assert.Equal([true, true, true], new ElementsPage(Editor(100)).Tiers.Select(t => t.Reached));
    }

    [Fact]
    public void TheColumns_RangeThePairsByDisjointCouples_AndAllFusions()
    {
        Assert.Equal(new ElementsPage(Editor(50)).Fusions.Select(f => f.Id).Order(), ElementsAtelier.PairOrder.Concat(ElementsAtelier.TrioOrder).Order());
        for (int i = 0; i < 6; i += 2)
        {
            Element a = Enum.Parse<Element>(ElementsAtelier.PairOrder[i], ignoreCase: true), b = Enum.Parse<Element>(ElementsAtelier.PairOrder[i + 1], ignoreCase: true);
            Assert.Empty(Elements.BlockedBy(a, [b]));
        }
    }

    [Fact]
    public void ThePiles_FallTheSameHeight_AndEachTargetCanBeTouched()
    {
        Assert.Equal(ElementsAtelier.Height, ElementsAtelier.ElementY(3) + ElementsAtelier.ElementHeight + ElementsAtelier.StatsButtonGap + ElementsAtelier.StatsButtonHeight);
        Assert.Equal(ElementsAtelier.Height, ElementsAtelier.PairY(5) + ElementsAtelier.PairHeight);
        Assert.Equal(ElementsAtelier.Height, ElementsAtelier.TrioY(3) + ElementsAtelier.TrioHeight);
        Assert.Equal(ElementsAtelier.Width, ElementsAtelier.PanelX + ElementsAtelier.PanelWidth);
        Assert.True(ElementsAtelier.ActionHeight >= 44 && ElementsAtelier.PairHeight >= 44 && ElementsAtelier.StatsButtonHeight >= 44);
        Assert.True(ElementsAtelier.ActionHeight <= ElementsAtelier.PairHeight);
        for (int i = 0; i < 5; i++)
            Assert.True(ElementsAtelier.PairY(i) + ElementsAtelier.PairHeight <= ElementsAtelier.PairY(i + 1));
    }

    [Fact]
    public void TheStrands_LeaveEachRowApart_AndEnterEachPairSideBySide()
    {
        foreach (string pair in ElementsAtelier.PairOrder)
        {
            IReadOnlyList<Element> parents = Elements.Parents(Enum.Parse<Element>(pair, ignoreCase: true));
            Assert.Equal(14, Math.Abs(ElementsAtelier.StrandTo(pair, parents[0]) - ElementsAtelier.StrandTo(pair, parents[1])));
            Assert.All(parents, e => Assert.InRange(ElementsAtelier.StrandFrom(e, pair), 0, ElementsAtelier.Height));
        }
        foreach (Element e in ElementsAtelier.ElementOrder)
        {
            float[] starts = [.. ElementsAtelier.PairOrder.Where(p => Elements.Parents(Enum.Parse<Element>(p, ignoreCase: true)).Contains(e)).Select(p => ElementsAtelier.StrandFrom(e, p))];
            Assert.Equal(3, starts.Distinct().Count());
        }
    }

    [Fact]
    public void WithLightning_OnlyMudRemains_AndWithMudNothing_TwoThenNoElementFree()
    {
        PointsEditor editor = Editor(60, compounds: Element.Lightning);
        var page = new ElementsPage(editor);
        Assert.Equal(2, page.FreeElements);
        Assert.Equal(["mud"], page.Fusions.Where(f => f.State == FusionState.Open).Select(f => f.Id));
        Assert.Equal(8, page.Fusions.Count(f => f.State == FusionState.Blocked));
        Assert.Equal(Element.Lightning, page.Bars.Single(b => b.Element == Element.Air).ConsumedBy);
        Assert.Null(page.Bars.Single(b => b.Element == Element.Earth).ConsumedBy);
        Assert.True(page.Press(page.Fusions.Single(f => f.Id == "mud")));
        Assert.Equal(0, page.FreeElements);
        Assert.DoesNotContain(page.Fusions, f => f.State == FusionState.Open);
    }

    [Fact]
    public void ATrio_LeavesOneElementFree_AndBlocksEverythingElse()
    {
        var page = new ElementsPage(Editor(60, compounds: Element.Ether));
        Assert.Equal(1, page.FreeElements);
        Assert.Equal(9, page.Fusions.Count(f => f.State == FusionState.Blocked));
        Assert.Null(page.Bars.Single(b => b.Element == Element.Earth).ConsumedBy);
    }

    [Fact]
    public void TheFoot_SaysWhatIsExchanged_WhatBlocks_AndAtWhichLevel()
    {
        var fr = new Texts("fr");
        var page = new ElementsPage(Editor(100, compounds: Element.Lightning));
        Fusion F(string id) => page.Fusions.Single(f => f.Id == id);
        Assert.Equal("Feu dans la Foudre", ElementsPage.BlockedLine(F("magma"), fr));
        (string first, string second, bool blocked) = page.Foot(F("magma"), fr);
        Assert.Equal(("Bloqué : Feu dans la Foudre.", "Dissoudre rend les éléments.", true), (first, second, blocked));
        Assert.Equal("Feu + Air dans la Foudre", ElementsPage.BlockedLine(F("ether"), fr));
        Assert.Equal("Change 8 sorts de Terre + Eau en Boue.", page.Foot(F("mud"), fr).First);
        Assert.Equal("8 sorts de Foudre à la place de Feu + Air.", page.Foot(F("lightning"), fr).First);
        Assert.Equal("Livre : 24 sorts, quoi que tu crées.", page.Foot(F("mud"), fr).Second);
        var young = new ElementsPage(Editor(29));
        Assert.Equal("Ouvert au niveau 30.", young.Foot(young.Fusions.Single(f => f.Id == "mud"), fr).First);
        var two = new ElementsPage(Editor(100, compounds: [Element.Lightning, Element.Mud]));
        Assert.Equal("Feu dans la Foudre, Terre dans la Boue", ElementsPage.BlockedLine(two.Fusions.Single(f => f.Id == "magma"), fr));
    }

    [Fact]
    public void TheNextTier_IsMarked_OnlyOne()
    {
        Assert.Equal([30], new ElementsPage(Editor(29)).Tiers.Where(t => t.Next).Select(t => t.Level));
        Assert.Equal([50], new ElementsPage(Editor(30)).Tiers.Where(t => t.Next).Select(t => t.Level));
        Assert.DoesNotContain(new ElementsPage(Editor(100)).Tiers, t => t.Next);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void EveryFusion_IsNamedAndExplained_InBothLanguages(string lang)
    {
        var texts = new Texts(lang);
        foreach (Fusion f in new ElementsPage(Editor(50)).Fusions)
        {
            Assert.False(string.IsNullOrWhiteSpace(texts[$"fusion.{f.Id}.name"]));
            Assert.False(string.IsNullOrWhiteSpace(texts[$"fusion.{f.Id}.signature"]));
            Assert.False(string.IsNullOrWhiteSpace(texts[$"element.{f.Compound}"]));
        }
        foreach (ElementTier tier in new ElementsPage(Editor(50)).Tiers)
            Assert.False(string.IsNullOrWhiteSpace(texts[$"elements.tier.{tier.Level}"]));
    }

    // Sprint 187, D94: a compound consumes its parents.
    private static PointsEditor Mage100(params Element[] compounds) => new(new Hero("Ronce", "female-c", "mage", Level: 100, Compounds: compounds.Length == 0 ? null : compounds), GameData.Embedded);

    [Fact]
    public void ACreatedPair_BlocksTheFusionsOfItsParents_AndLeavesTheOtherOne()
    {
        var page = new ElementsPage(Mage100(Element.Lightning));
        Assert.Equal(["lightning"], page.Fusions.Where(f => f.State == FusionState.Created).Select(f => f.Id));
        Assert.Equal(["mud"], page.Fusions.Where(f => f.State == FusionState.Open).Select(f => f.Id));
        Fusion magma = page.Fusions.Single(f => f.Id == "magma");
        Assert.Equal((FusionState.Blocked, Element.Lightning), (magma.State, magma.BlockedBy.Single()));
        Assert.False(page.Press(magma));
    }

    [Fact]
    public void DissolvingOpensThemAgain_AndACompound_CountsItsSpells()
    {
        PointsEditor editor = Mage100(Element.Lightning);
        var page = new ElementsPage(editor);
        Assert.True(page.Press(page.Fusions.Single(f => f.Id == "lightning")));
        Assert.Equal(FusionState.Open, page.Fusions.Single(f => f.Id == "magma").State);
        Assert.Equal(8, page.SpellsOf(page.Fusions.Single(f => f.Id == "magma")));
        Assert.Equal(8, new ElementsPage(Mage100(Element.Lightning)).SpellsOf(new ElementsPage(Mage100(Element.Lightning)).Fusions.Single(f => f.Id == "mud")));
    }

    [Fact]
    public void TheBook_HidesTheParents_AndTheTwinsComeAtOnce_InPlaceInTheDeck()
    {
        PointsEditor editor = Mage100();
        Spell fire = editor.Spells.First(s => s.Element == Element.Fire);
        editor.ClearDeck();
        editor.AddToDeck(fire);
        var page = new ElementsPage(editor);
        Assert.True(page.Press(page.Fusions.Single(f => f.Id == "lightning")));
        var book = new SpellBook(editor);
        Assert.DoesNotContain(book.Spells, s => s.Element is Element.Fire or Element.Air);
        Assert.Equal(8, book.Spells.Count(s => s.Element == Element.Lightning && !s.Preview));
        Assert.Contains(Element.Lightning, book.Elements);
        Spell twin = GameData.Embedded.Spells.Values.Single(s => s.Twin == fire.Id && s.Element == Element.Lightning);
        Assert.Equal([twin.Id], editor.Deck);
        Assert.Empty(editor.DeckChanges);
        Assert.Null(editor.Draft.Problem(GameData.Embedded));
        Assert.True(page.Press(page.Fusions.Single(f => f.Id == "lightning")));
        Assert.Equal([fire.Id], editor.Deck);
    }
}
