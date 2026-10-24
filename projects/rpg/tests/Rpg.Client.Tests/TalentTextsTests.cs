using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The talents on the client's side (sprint 137, D80, T103): the list on the character screen, the points it spends and
/// gives back, the tiles' texts, the forecast that tells the reaction and the log that tells the mark and the reaction.
/// </summary>
public class TalentTextsTests
{
    private static GameData Data => GameData.Embedded;

    private static Hero Sentinel(int level, params string[] talents) =>
        new("Ondine", "female-a", "sentinel", Level: level, Talents: talents.Length == 0 ? null : talents);

    [Fact]
    public void TheList_TakesAndGivesBackTalents_WithinThePointsOfTheLevel()
    {
        var editor = new PointsEditor(Sentinel(30), Data);
        TalentSpec bogging = Data.Talents["bogging"], close = Data.Talents["up-close"];
        Assert.Equal(2, editor.TalentPointsLeft);
        editor.Take(bogging);
        editor.Take(close);
        Assert.Equal(0, editor.TalentPointsLeft);
        Assert.Equal(["bogging", "up-close"], editor.TalentsTaken);
        Assert.True(editor.Changed);
        Assert.Equal(["bogging", "up-close"], editor.ToPoints().Talents!);
        Assert.Null(editor.Draft.Problem(Data));
        editor.Drop(bogging);
        Assert.Equal(["up-close"], editor.ToPoints().Talents!);
        // Before 30, nothing to take; in a fight, nothing changes; nothing taken, an empty list goes to the server.
        Assert.False(new PointsEditor(Sentinel(29), Data).CanTake(bogging));
        var fight = new PointsEditor(Sentinel(35, "bogging"), Data, readOnly: true);
        Assert.False(fight.CanDrop(bogging) || fight.CanTake(close));
        Assert.Empty(new PointsEditor(Sentinel(40), Data).ToPoints().Talents!);
        Assert.False(new PointsEditor(Sentinel(40, "bogging"), Data).Changed);
    }

    [Fact]
    public void ATile_TellsTheTalentsName_ItsBranch_ItsFloor_ItsCost_AndWhatItDoes()
    {
        Assert.StartsWith("Enlisement · Réactions, étage 1, 1 point(s)\nFrapper en Terre", new Texts("fr").Talent(Data.Talents["bogging"]), StringComparison.Ordinal);
        Assert.StartsWith("Up close · Distance, first floor, 1 point(s)\n+10% damage", new Texts("en").Talent(Data.Talents["up-close"]), StringComparison.Ordinal);
    }

    [Fact]
    public void TheForecast_AndTheLog_TellTheMarkAndTheReaction()
    {
        // The Sentinel of 35 with Bogging, a wolf 3 cells away: an Earth arrow, then the forecast of a Water one.
        var text = new LocalizedText("loup", "wolf");
        FighterSpec me = new(text, "female-a", 0, 50, 6, 3, 200, 0, ["strike"]), wolf = new(text, "orc", 1, 900, 6, 3, 0, 0, ["frost-arrow"]);
        var data = new GameData(Data.Spells.Values, [new MapSpec("m", text, ["A..B"])], [new Scenario("s", text, "m", [me, wolf])], Data.Classes,
            summons: Data.Summons.Values, passives: Data.Passives.Values, talents: Data.Talents.Values, reactions: Data.Reactions.Values);
        var fight = new Fight(data, "s", 1, Progression.Built(Sentinel(35, "bogging"), data));
        Fighter hero = fight.HeroFighter!, target = fight.Fighters[1];
        Spell stone = hero.Spells.First(s => s.Id == "stone-arrow"), frost = hero.Spells.First(s => s.Id == "frost-arrow");
        Assert.Equal(ActionError.None, fight.Apply(new CastAction(stone.Id, target.Cell)));
        int marked = fight.Events.ToList().FindLastIndex(e => e is Marked);
        Assert.Equal($"{target.Name.Fr} porte l'empreinte Terre.", new Texts("fr").Describe(fight.Events[marked], fight));
        Assert.Equal($"{target.Name.En} bears the Earth mark.", new Texts("en").Describe(fight.Events[marked], fight));
        Assert.Equal(ActionError.None, fight.CheckCast(hero, frost, hero.Cell, target.Cell));
        Assert.EndsWith(" — Enlisé !", new Texts("fr").Forecast(fight.Foresee(frost, target.Cell)), StringComparison.Ordinal);
        fight.Apply(new CastAction(frost.Id, target.Cell));
        FightEvent reaction = fight.Events.Last(e => e is ReactionTriggered);
        Assert.Equal($"{target.Name.En}: Bogged!", new Texts("en").Describe(reaction, fight));
        FightEvent status = fight.Events.Last(e => e is StatusAdded { Reaction: "bogged" });
        Assert.Equal($"{target.Name.Fr} : Enlisé, -2 PM pendant 1 tour(s).", new Texts("fr").Describe(status, fight));
    }

    [Fact]
    public void Lightning_HasItsCard_ItsColour_AndItsSpellsInTheBook_WithTheTalents()
    {
        // Sprint 138 (T104): the Arc on the card, the yellow of Lightning, the spells of the opening in the book and the deck.
        Spell storm = Data.Spells["lightning-fire-arrow"];
        Assert.Contains("Arc : 35 % des dégâts sautent sur l'ennemi le plus proche à 2 cases", new Texts("fr").SpellCard(storm), StringComparison.Ordinal);
        Assert.Contains("Lightning", new Texts("en").SpellCard(storm), StringComparison.Ordinal);
        Assert.Equal("#f2d03b", ElementStyle.Colour(Element.Lightning));
        var editor = new PointsEditor(Sentinel(35), Data);
        Assert.DoesNotContain(new SpellBook(editor).Spells, b => b.Element == Element.Lightning);
        editor.Take(Data.Talents["open-lightning"]);
        Assert.Equal(4, new SpellBook(editor).Spells.Count(b => b.Element == Element.Lightning));
        Assert.True(editor.CanAddToDeck(storm) || editor.Deck.Count == Hero.DeckSize);
        editor.RemoveFromDeck(Data.Spells["stone-arrow"]);
        editor.AddToDeck(storm);
        Assert.Contains("lightning-fire-arrow", editor.Deck);
        // Its rank is Burning Arrow's; given back, the opening takes its spell out of the deck.
        Assert.Equal(editor.Rank(Data.Spells["fire-arrow"]), editor.Rank(storm));
        editor.Drop(Data.Talents["open-lightning"]);
        Assert.DoesNotContain("lightning-fire-arrow", editor.Deck);
        Assert.Null(editor.Draft.Problem(Data));
    }
}
