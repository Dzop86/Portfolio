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
        var editor = new PointsEditor(Sentinel(33), Data);
        TalentSpec close = Data.Talents["up-close"], footing = Data.Talents["firm-footing"];
        Assert.Equal(2, editor.TalentPointsLeft);
        editor.Take(close);
        editor.Take(footing);
        Assert.Equal(0, editor.TalentPointsLeft);
        Assert.Equal(["up-close", "firm-footing"], editor.TalentsTaken);
        Assert.True(editor.Changed);
        Assert.Equal(["up-close", "firm-footing"], editor.ToPoints().Talents!);
        Assert.Null(editor.Draft.Problem(Data));
        editor.Drop(close);
        editor.Drop(footing);
        Assert.Empty(editor.ToPoints().Talents!);
        // Before 30, nothing to take; in a fight, nothing changes; nothing taken, an empty list goes to the server.
        Assert.False(new PointsEditor(Sentinel(29), Data).CanTake(close));
        var fight = new PointsEditor(Sentinel(35, "up-close"), Data, readOnly: true);
        Assert.False(fight.CanDrop(close) || fight.CanTake(footing));
        Assert.Empty(new PointsEditor(Sentinel(40), Data).ToPoints().Talents!);
        Assert.False(new PointsEditor(Sentinel(40, "up-close"), Data).Changed);
    }

    [Fact]
    public void ATile_TellsTheTalentsName_ItsBranch_ItsFloor_ItsCost_AndWhatItDoes()
    {
        Assert.StartsWith($"{Data.Talents["side-step"].Name.Fr} · Terrain, étage 1, 1 point(s)\n", new Texts("fr").Talent(Data.Talents["side-step"]), StringComparison.Ordinal);
        Assert.StartsWith("Up close · Melee, first floor, 1 point(s)\n+10% damage", new Texts("en").Talent(Data.Talents["up-close"]), StringComparison.Ordinal);
    }

    [Fact]
    public void TheForecast_AndTheLog_TellTheMarkAndTheReaction()
    {
        // The Sentinel of 35, a wolf 3 cells away: an Earth arrow, then the forecast of a Water one (sprint 182: no talent asked).
        var text = new LocalizedText("loup", "wolf");
        FighterSpec me = new(text, "female-a", 0, 50, 6, 3, 200, 0, ["strike"]), wolf = new(text, "orc", 1, 900, 6, 3, 0, 0, ["frost-arrow"]);
        var data = new GameData(Data.Spells.Values, [new MapSpec("m", text, ["A..B"])], [new Scenario("s", text, "m", [me, wolf])], Data.Classes,
            summons: Data.Summons.Values, passives: Data.Passives.Values, talents: Data.Talents.Values, reactions: Data.Reactions.Values);
        var fight = new Fight(data, "s", 1, Progression.Built(Sentinel(35), data));
        Fighter hero = fight.HeroFighter!, target = fight.Fighters[1];
        Spell stone = hero.Spells.First(s => s.Id == "stone-arrow"), frost = hero.Spells.First(s => s.Id == "frost-arrow");
        Assert.Equal(ActionError.None, fight.Apply(new CastAction(stone.Id, target.Cell)));
        int marked = fight.Events.ToList().FindLastIndex(e => e is Marked);
        Assert.Equal($"{target.Name.Fr} porte l'empreinte Terre.", new Texts("fr").Describe(fight.Events[marked], fight));
        Assert.Equal($"{target.Name.En} bears the Earth mark.", new Texts("en").Describe(fight.Events[marked], fight));
        Assert.Equal(ActionError.None, fight.CheckCast(hero, frost, hero.Cell, target.Cell));
        // The damage line ends with the reaction; the tree's part comes under it (sprint 150).
        Assert.EndsWith(" — Enlisé !", new Texts("fr").Forecast(fight.Foresee(frost, target.Cell)).Split('\n')[0], StringComparison.Ordinal);
        fight.Apply(new CastAction(frost.Id, target.Cell));
        FightEvent reaction = fight.Events.Last(e => e is ReactionTriggered);
        Assert.Equal($"{target.Name.En}: Bogged!", new Texts("en").Describe(reaction, fight));
        FightEvent status = fight.Events.Last(e => e is StatusAdded { Reaction: "bogged" });
        Assert.Equal($"{target.Name.Fr} : Enlisé, -2 PM pendant 1 tour(s).", new Texts("fr").Describe(status, fight));
    }

    [Fact]
    public void Lightning_HasItsCard_ItsColour_AndItsSpellsInTheBook_OnceCreatedAndSaved()
    {
        // Sprint 138 (T104): the Arc on the card, the yellow of Lightning; since sprint 182 (D93), its spells only once it
        // is created and saved, a preview before, out of the filter and of the deck.
        Spell storm = Data.Spells["lightning-fire-arrow"];
        Assert.Contains("Arc : 35 % des dégâts sautent sur l'ennemi le plus proche à 2 cases", new Texts("fr").SpellCard(storm), StringComparison.Ordinal);
        Assert.Contains("Lightning", new Texts("en").SpellCard(storm), StringComparison.Ordinal);
        Assert.Equal("#f2d03b", ElementStyle.Colour(Element.Lightning));
        var editor = new PointsEditor(Sentinel(35), Data);
        Assert.DoesNotContain(new SpellBook(editor).Spells, b => b.Element == Element.Lightning);
        Assert.DoesNotContain(Element.Lightning, new SpellBook(editor).Elements);
        // Created in the draft.
        editor.Create(Element.Lightning);
        var book = new SpellBook(editor);
        // Sprint 187 (D94): at once, in the filter, and they take their origins' place in the deck.
        BookSpell[] twins = [.. book.Spells.Where(b => b.Element == Element.Lightning)];
        Assert.NotEmpty(twins);
        Assert.All(twins, b => Assert.False(b.Preview));
        Assert.Contains(Element.Lightning, book.Elements);
        Assert.NotEmpty(book.Of(Element.Lightning));
        Assert.DoesNotContain(storm, editor.Previewed);
        // Saved: its own spells, in the filter and the deck.
        var saved = new PointsEditor(editor.Draft, Data);
        Assert.Empty(saved.Previewed);
        Assert.Contains(Element.Lightning, new SpellBook(saved).Elements);
        Assert.All(new SpellBook(saved).Spells.Where(b => b.Element == Element.Lightning), b => Assert.False(b.Preview));
        Assert.True(saved.CanAddToDeck(storm) || saved.Deck.Count == saved.DeckLimit);
        saved.RemoveFromDeck(Data.Spells["stone-arrow"]);
        saved.AddToDeck(storm);
        Assert.Contains("lightning-fire-arrow", saved.Deck);
        // Its rank is Burning Arrow's; undone, Lightning takes its spell out of the deck.
        Assert.Equal(saved.Rank(Data.Spells["fire-arrow"]), saved.Rank(storm));
        saved.Dissolve(Element.Lightning);
        Assert.DoesNotContain("lightning-fire-arrow", saved.Deck);
        Assert.Null(saved.Draft.Problem(Data));
        // Undone for free, whatever the level (Q10 a).
        Assert.Equal(0, saved.ResetCost);
    }

    [Fact]
    public void TheSheet_ShowsTheTrunk_FromThirty()
    {
        // Sprint 146 (D82, O7 a): +3 % damage at 30, +3 % resistances at 35, +5 % hit points at 40.
        CharacterSheet young = new(Sentinel(29), Data), grown = new(Sentinel(40), Data);
        Assert.Equal((0, 0, 0), (young.TreeDamage, young.TreeResistance, young.TreeHp));
        Assert.Equal((3, 3, 5), (grown.TreeDamage, grown.TreeResistance, grown.TreeHp));
        Assert.All(grown.Lines.Where(l => l.Element is not null), l => Assert.Equal(3, l.Resistance));
        int bare = Fight.HeroTotals(Sentinel(40), Data)!.Hp;
        Assert.Equal(bare * 105 / 100, grown.MaxHp);
        Assert.Equal("Tronc, gratuit : +3 % de dégâts (niv. 30) · +3 % de résistances (niv. 35), à venir · +5 % de PV (niv. 40), à venir", new Texts("fr").Trunk(30));
        Assert.Equal("Trunk, free: +3% damage (lv 30) · +3% resistances (lv 35) · +5% HP (lv 40)", new Texts("en").Trunk(40));
    }

    [Fact]
    public void GivingATalentBack_AfterForty_SaysItsPrice_TakingOneDoesNot()
    {
        // Sprint 140 (T106): 10 × the level in Éclats, before saving.
        var editor = new PointsEditor(Sentinel(45, "side-step"), Data);
        editor.Take(Data.Talents["backed"]);
        Assert.Equal(0, editor.ResetCost);
        editor.Drop(Data.Talents["side-step"]);
        Assert.Equal(450, editor.ResetCost);
        var at40 = new PointsEditor(Sentinel(40, "side-step"), Data);
        at40.Drop(Data.Talents["side-step"]);
        Assert.Equal((0, 0L), (at40.TalentsTaken.Count, at40.ResetCost));
    }

    [Fact]
    public void TheTreesSpells_HaveTheirCards_AndARemanenceItsName()
    {
        // Sprint 142 (T108): Relay, the Seals and the Decoy in both languages; a Remanence named on hovering, not by a spell id.
        foreach (string id in new[] { "relay", "seal-earth", "seal-fire", "seal-water", "seal-air", "decoy" })
        {
            Assert.DoesNotContain("{", new Texts("fr").SpellCard(Data.Spells[id]), StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(Data.Spells[id].Name.En));
        }
        Assert.Equal("Sceau d'Air", Data.Spells["seal-air"].Name.Fr);
        Assert.Equal(("Rémanence", "Remanence"), (new Texts("fr")["glyph.remanence"], new Texts("en")["glyph.remanence"]));
    }
}
