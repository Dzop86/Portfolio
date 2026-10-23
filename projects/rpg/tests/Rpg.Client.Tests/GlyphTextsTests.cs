using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The glyphs on the client's side (sprint 130): the spell's card, the hover panel, the log and the path shown.</summary>
public class GlyphTextsTests
{
    private static readonly LocalizedText Text = new("x", "x");
    private static readonly Spell Mark = new("mark", new LocalizedText("Sceau de pierre", "Stone Seal"), 2, 0, 6, false, false, 0, 0, 1, Element.Earth, null,
        [new GlyphEffect(Affects.Enemies, 2, 8, 12, Element.Earth, Stat.Mp, -1, 1)]);

    private static FightController Make()
    {
        FighterSpec me = new(new LocalizedText("Élise", "Élise"), "female-c", 0, 50, 6, 3, 200, 0, ["mark"]);
        FighterSpec orc = new(new LocalizedText("Orque", "Orc"), "orc", 1, 40, 6, 3, 100, 0, ["mark"]);
        var data = new GameData([Mark], [new MapSpec("m", Text, ["A...B", "....."])], [new Scenario("s", Text, "m", [me, orc])]);
        return new FightController(new Fight(data, "s", 1));
    }

    [Fact]
    public void TheSpellsCard_TellsTheGlyph()
    {
        Assert.EndsWith("\nglyphe de 2 tour(s) : 8 à 12 dégâts, Terre, -1 PM (1 tour(s))", new Texts("fr").SpellCard(Mark), StringComparison.Ordinal);
        Assert.EndsWith("\nglyph for 2 turn(s): 8 to 12 damage, Earth, -1 MP (1 turn(s))", new Texts("en").SpellCard(Mark), StringComparison.Ordinal);
    }

    [Fact]
    public void HoveringAGlyphsCell_TellsItsCasterWhatItDoesAndItsTurnsLeft()
    {
        FightController c = Make();
        c.SelectSpell(0);
        Assert.NotNull(c.Click(new Cell(2, 0)));
        Assert.Equal("Glyphe de Élise (Sceau de pierre) : frappe ses ennemis, 8 à 12 dégâts, Terre, -1 PM (1 tour(s)) ; encore 2 tour(s)", c.Info(new Cell(2, 0), new Texts("fr")));
        Assert.Equal("Élise's glyph (Stone Seal): strikes its enemies, 8 to 12 damage, Earth, -1 MP (1 turn(s)); 2 turn(s) left", c.Info(new Cell(2, 0), new Texts("en")));
        Assert.Null(c.Info(new Cell(1, 0), new Texts("fr")));
    }

    [Fact]
    public void AGlyphsStrike_IsInTheLog_AndThePathShownIsThePathWalked()
    {
        // The hero's glyph on (2, 0); the orc, played here by the test, walks onto it.
        FightController c = Make();
        c.SelectSpell(0);
        c.Click(new Cell(2, 0));
        c.EndTurn();
        Fight fight = c.Fight;
        Assert.Equal(ActionError.None, fight.Apply(new MoveAction(new Cell(2, 0))));
        string[] fr = [.. fight.Events.Select(e => new Texts("fr").Describe(e, fight)).OfType<string>()];
        Assert.Contains("Orque : un glyphe frappe !", fr);
        Assert.Contains("Orc: a glyph strikes!", fight.Events.Select(e => new Texts("en").Describe(e, fight)));
        // The orc's glyph now on (1, 0): the hero's preview goes round it, as the move will.
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("mark", new Cell(1, 0))));
        fight.Apply(new EndTurnAction());
        Assert.True(c.IsPlayerTurn);
        IReadOnlyList<Cell> path = c.Hover(new Cell(2, 1)).Path;
        Assert.DoesNotContain(new Cell(1, 0), path);
        Assert.Equal(fight.PathTo(new Cell(2, 1)), path);
    }
}
