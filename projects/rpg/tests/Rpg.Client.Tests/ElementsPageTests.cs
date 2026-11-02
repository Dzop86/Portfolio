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
        Assert.All(page.Fusions.Where(f => f.Parents.Count == 3), f => Assert.Null(f.Reaction));
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
        // The ones whose spells are not in the game yet: sprints 184 and 185 turn them on.
        Assert.All(at50.Fusions.Where(f => f.Compound is null), f => Assert.Equal(FusionState.Coming, f.State));
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
        Assert.NotEmpty(editor.Previewed);
        Assert.Equal(FusionState.Created, page.Fusions.Single(f => f.Id == "lightning").State);
        Assert.True(page.Press(page.Fusions.Single(f => f.Id == "lightning")));
        Assert.Empty(editor.CompoundsCreated);
        Assert.False(editor.Changed);
    }

    [Theory]
    [InlineData("mage")]
    [InlineData("guard")]
    [InlineData("sentinel")]
    public void ACompound_GivesTheTwinsOfWhatTheLevelUnlocked_ShownAsAPreviewUntilSaved(string id)
    {
        var at30 = new PointsEditor(new Hero("Ondine", "female-a", id, Level: 30), Data);
        var page = new ElementsPage(at30);
        Fusion lightning = page.Fusions.Single(f => f.Id == "lightning");
        int said = page.SpellsOf(lightning);
        Assert.InRange(said, 1, 8);
        page.Press(lightning);
        Assert.Equal(said, at30.Previewed.Count);
        // The number stays what the fusion gives once created, and the book has them once saved.
        Assert.Equal(said, page.SpellsOf(page.Fusions.Single(f => f.Id == "lightning")));
        Assert.Equal(said, new PointsEditor(at30.Hero with { Compounds = [Element.Lightning] }, Data).Spells.Count - at30.Spells.Count);
        // At 100, all eight (docs/elements-specialisations.md §v3.1).
        var top = new ElementsPage(new PointsEditor(new Hero("Ondine", "female-a", id, Level: 100), Data));
        Assert.Equal(8, top.SpellsOf(top.Fusions.Single(f => f.Id == "lightning")));
        Assert.Equal(0, top.SpellsOf(top.Fusions.Single(f => f.Id == "ice")));
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
        Assert.False(page.Press(page.Fusions.Single(f => f.Id == "ice")));
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
    public void TheWheel_FitsItsArea_NoNodeOverlaps_AndEachCanBeTouched()
    {
        (string Id, float X, float Y)[] nodes = [.. ElementsWheel.Nodes];
        Assert.Equal(14, nodes.Length);
        Assert.True(ElementsWheel.NodeHeight >= 44);
        foreach ((string id, float x, float y) in nodes)
        {
            Assert.InRange(x - ElementsWheel.NodeWidth / 2, 0, ElementsWheel.Width);
            Assert.InRange(x + ElementsWheel.NodeWidth / 2, 0, ElementsWheel.Width);
            Assert.InRange(y - ElementsWheel.NodeHeight / 2, 0, ElementsWheel.Height);
            Assert.InRange(y + ElementsWheel.NodeHeight / 2, 0, ElementsWheel.Height);
        }
        for (int i = 0; i < nodes.Length; i++)
            for (int j = i + 1; j < nodes.Length; j++)
                Assert.False(Math.Abs(nodes[i].X - nodes[j].X) < ElementsWheel.NodeWidth && Math.Abs(nodes[i].Y - nodes[j].Y) < ElementsWheel.NodeHeight, $"{nodes[i].Id} on {nodes[j].Id}");
    }

    [Fact]
    public void AWheelNode_SitsBetweenItsParents_AndEveryFusionHasOne()
    {
        (string Id, float X, float Y)[] nodes = [.. ElementsWheel.Nodes];
        (float X, float Y) At(string id) { var n = nodes.Single(x => x.Id == id); return (n.X, n.Y); }
        foreach (Fusion f in new ElementsPage(Editor(50)).Fusions.Where(x => x.Parents.Count == 2))
        {
            (float ax, float ay) = At(f.Parents[0].ToString());
            (float bx, float by) = At(f.Parents[1].ToString());
            (float x, float y) = At(f.Id);
            // On the line between its two parents.
            float cross = (bx - ax) * (y - ay) - (by - ay) * (x - ax);
            Assert.True(Math.Abs(cross) < 1, $"{f.Id} off the line of its parents: {cross}");
            Assert.InRange(x, Math.Min(ax, bx), Math.Max(ax, bx));
            Assert.InRange(y, Math.Min(ay, by), Math.Max(ay, by));
        }
        Assert.All(new ElementsPage(Editor(50)).Fusions, f => At(f.Id));
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
            if (f.Parents.Count == 3)
                Assert.False(string.IsNullOrWhiteSpace(texts[$"fusion.{f.Id}.reaction"]));
        }
        foreach (ElementTier tier in new ElementsPage(Editor(50)).Tiers)
            Assert.False(string.IsNullOrWhiteSpace(texts[$"elements.tier.{tier.Level}"]));
    }
}
