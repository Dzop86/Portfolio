using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The spells window and the fight after the tree (sprint 150, D82): a spell replaced by a saved Transmutation gone from the
/// book (Charles, after 1.0.134), its twin saying where it comes from and what it replaced, the talents that change a spell,
/// "new" until looked at; the twin in the deck, said; the forecast's part of the tree, its sum the hit's bonus; a node's
/// line before and after; the talents of the turn lit, then off.
/// </summary>
public class SpellOriginTests
{
    private static GameData Data => GameData.Embedded;

    private static Hero Sentinel(int level, params string[] talents) =>
        new("Ondine", "female-a", "sentinel", Level: level, Talents: talents.Length == 0 ? null : talents);

    // A wolf 3 cells away on an open row, as in the talents' texts.
    private static GameData Arena()
    {
        var text = new LocalizedText("loup", "wolf");
        FighterSpec me = new(text, "female-a", 0, 50, 6, 3, 200, 0, ["strike"]), wolf = new(text, "orc", 1, 900, 6, 3, 0, 0, ["frost-arrow"]);
        return new GameData(Data.Spells.Values, [new MapSpec("m", text, ["A..B", "...."])], [new Scenario("s", text, "m", [me, wolf])], Data.Classes,
            summons: Data.Summons.Values, passives: Data.Passives.Values, talents: Data.Talents.Values, reactions: Data.Reactions.Values);
    }

    [Fact]
    public void ASavedTransmutation_TakesTheParentOut_AndItsTwinSaysWhereItComesFrom()
    {
        var editor = new PointsEditor(Sentinel(35, "electrifying", "transmute-fire-lightning"), Data);
        var book = new SpellBook(editor);
        Assert.DoesNotContain(book.Spells, b => b.Spell.Id == "fire-arrow");
        BookSpell twin = book.Spells.Single(b => b.Spell.Id == "lightning-fire-arrow");
        Assert.Equal("transmute-fire-lightning", twin.Origin);
        Assert.Equal("fire-arrow", twin.Replaces);
        Assert.Equal(["Donné par Transmutation : Feu → Foudre", "Prend la place de Flèche ardente"], new Texts("fr").SpellOrigin(twin, Data));
        Assert.Equal(["Given by Transmutation: Fire → Lightning", "Takes the place of Burning Arrow"], new Texts("en").SpellOrigin(twin, Data));
        // New until looked at (O2 a); a class's spell is never new.
        Assert.True(twin.New);
        SpellBook.Look(twin.Spell.Id);
        Assert.False(new SpellBook(editor).Spells.Single(b => b.Spell.Id == "lightning-fire-arrow").New);
        Assert.All(book.Spells.Where(b => b.Spell.Twin is null), b => Assert.False(b.New));
    }

    [Fact]
    public void ADraftsTransmutation_IsAPreview_TheParentStaysUntilTheSave()
    {
        var editor = new PointsEditor(Sentinel(35, "electrifying"), Data);
        editor.Take(Data.Talents["transmute-fire-lightning"]);
        var book = new SpellBook(editor);
        Assert.Contains(book.Spells, b => b.Spell.Id == "fire-arrow");
        BookSpell twin = book.Spells.Single(b => b.Spell.Id == "lightning-fire-arrow");
        Assert.True(twin.Preview);
        Assert.False(twin.New);
    }

    [Fact]
    public void TheTreesChanges_AreSaid_OnTheSpellsTheyTouch()
    {
        var editor = new PointsEditor(Sentinel(35, "bogging", "up-close"), Data);
        BookSpell arrow = new SpellBook(editor).Spells.First(b => b.Spell.Id == "stone-arrow");
        Assert.Equal(["up-close"], arrow.ModifiedBy);
        Assert.Equal(["Modifié par l'arbre : Au plus près"], new Texts("fr").SpellOrigin(arrow, Data));
        Assert.Empty(new Texts("fr").SpellOrigin(new SpellBook(new PointsEditor(Sentinel(35, "bogging"), Data)).Spells.First(b => b.Spell.Id == "stone-arrow"), Data));
    }

    [Fact]
    public void TheDeck_SaysWhatTheTalentsDidToIt()
    {
        Hero hero = Sentinel(45, "electrifying", "transmute-fire-lightning") with { Deck = ["lightning-fire-arrow", "stone-arrow", "frost-arrow"] };
        var editor = new PointsEditor(hero, Data);
        // The Conversion takes the Transmutation's place: Lightning's twins stay, the Air spells' twins are previews until the save.
        editor.Take(Data.Talents["convert-lightning"]);
        Assert.Empty(editor.DeckChanges.Swapped);
        // Giving the pair back: the twin goes, its parent comes back in the deck's place.
        var back = new PointsEditor(hero, Data);
        back.Drop(Data.Talents["electrifying"]);
        Assert.Equal(["lightning-fire-arrow"], back.DeckChanges.Left);
        Assert.Equal(["Flèche d'orage quitte ton deck jusqu'à l'enregistrement."], new Texts("fr").DeckChanges(back));
        // A saved deck of class spells, a Transmutation saved: the twin in the parent's place.
        var saved = new PointsEditor(Sentinel(35, "electrifying") with { Deck = ["fire-arrow", "stone-arrow"] }, Data);
        saved.Take(Data.Talents["transmute-fire-lightning"]);
        Assert.Equal(["stone-arrow"], saved.Deck);
        Assert.Equal(["fire-arrow"], saved.DeckChanges.Left);
    }

    [Fact]
    public void TheForecast_DetailsTheTree_ItsSumTheHitsBonus()
    {
        GameData data = Arena();
        var fight = new Fight(data, "s", 1, Progression.Built(Sentinel(35, "bogging", "up-close", "firm-footing"), data));
        Fighter hero = fight.HeroFighter!, target = fight.Fighters[1];
        Spell stone = hero.Spells.First(s => s.Id == "stone-arrow");
        Forecast fc = fight.Foresee(stone, target.Cell).Single(f => f.Fighter == target);
        HitContext context = fight.ContextOf(hero, stone, hero.Cell, target);
        Assert.Equal(Talents.Percent(hero.Talents, hero.Cell, target.Cell, stone.Element, context), Math.Min(Talents.Ceiling, fc.Tree!.Sum(s => s.Percent)));
        Assert.Equal(3, fc.Trunk);
        string said = new Texts("fr").TreeShares(fc)!;
        Assert.StartsWith("   arbre : tronc +3 %", said, StringComparison.Ordinal);
        Assert.Contains("Pied ferme +15 %", said, StringComparison.Ordinal);
        Assert.Equal(said, new Texts("fr").Forecast([fc]).Split('\n')[1]);
    }

    [Fact]
    public void ANodesLine_BeforeAndAfter_IsThePercentsDifference()
    {
        Hero hero = Sentinel(40, "bogging") with { Stats = new Characteristics(Earth: 50) };
        var editor = new PointsEditor(hero, Data);
        (int before, int after) = TreeGuide.BeforeAfter(editor.TalentsTaken, "up-close", hero, Data);
        Assert.Equal((0, 10), (before, after));
        Assert.Equal("Sur un coup type (à 2 cases, sans bouger, premier sort de 3 PA) : 0 % → +10 %", new Texts("fr").BeforeAfter("up-close", editor));
        // A taken node: the bonus without it, then with it; a node that changes nothing at that hit says nothing.
        var taken = new PointsEditor(hero with { Talents = ["bogging", "up-close", "firm-footing"] }, Data);
        Assert.Equal((10, 25), TreeGuide.BeforeAfter(taken.TalentsTaken, "firm-footing", taken.Draft, Data));
        Assert.Null(new Texts("fr").BeforeAfter("side-step", editor));
        Assert.Contains("0 % → +10 %", new Texts("fr").TreeCard(new TreeLayout(Data)["up-close"]!, editor).Extra, StringComparison.Ordinal);
    }

    [Fact]
    public void TheTalentsOfTheTurn_AreLit_ThenOff_AsTheHeroMoves()
    {
        GameData data = Arena();
        var fight = new Fight(data, "s", 1, Progression.Built(Sentinel(40, "bogging", "firm-footing", "side-step", "dancing-step"), data));
        Fighter hero = fight.HeroFighter!;
        IReadOnlyList<TalentLight> still = Talents.Lights(hero.Talents, fight.ContextOf(hero, null, hero.Cell, hero));
        Assert.Equal([new TalentLight("firm-footing", true, 15), new TalentLight("dancing-step", false, 0)], still);
        Assert.Equal("Talents du tour : Pied ferme allumé (+15 %), Pas dansant éteint (0 %)", new Texts("fr").Lights(still));
        // One step: Firm Footing off, Dancing Step lit.
        Cell next = new[] { new Cell(1, 0), new Cell(-1, 0), new Cell(0, 1), new Cell(0, -1) }.Select(d => hero.Cell + d).First(fight.IsFree);
        Assert.Equal(ActionError.None, fight.Apply(new MoveAction(next)));
        IReadOnlyList<TalentLight> moved = Talents.Lights(hero.Talents, fight.ContextOf(hero, null, hero.Cell, hero));
        Assert.Equal([new TalentLight("firm-footing", false, -5), new TalentLight("dancing-step", true, 4)], moved);
        Assert.Null(new Texts("en").Lights([]));
    }
}
