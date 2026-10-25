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
        // Since sprint 147, Mud is Up close's door: given back, it takes Up close with it.
        editor.Drop(bogging);
        Assert.Empty(editor.ToPoints().Talents!);
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
        Assert.StartsWith("Enlisement · Paires, étage 1, 1 point(s)\nFrapper en Terre", new Texts("fr").Talent(Data.Talents["bogging"]), StringComparison.Ordinal);
        Assert.StartsWith("Up close · Melee, first floor, 1 point(s)\n+10% damage", new Texts("en").Talent(Data.Talents["up-close"]), StringComparison.Ordinal);
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
    public void Lightning_HasItsCard_ItsColour_AndItsSpellsInTheBook_OnceATransmutationIsSaved()
    {
        // Sprint 138 (T104): the Arc on the card, the yellow of Lightning; since sprint 146 (D82, R1 and R2), its spells only
        // once a Transmutation or the Conversion is saved, a preview before, out of the filter and of the deck.
        Spell storm = Data.Spells["lightning-fire-arrow"];
        Assert.Contains("Arc : 35 % des dégâts sautent sur l'ennemi le plus proche à 2 cases", new Texts("fr").SpellCard(storm), StringComparison.Ordinal);
        Assert.Contains("Lightning", new Texts("en").SpellCard(storm), StringComparison.Ordinal);
        Assert.Equal("#f2d03b", ElementStyle.Colour(Element.Lightning));
        var editor = new PointsEditor(Sentinel(35), Data);
        Assert.DoesNotContain(new SpellBook(editor).Spells, b => b.Element == Element.Lightning);
        Assert.DoesNotContain(Element.Lightning, new SpellBook(editor).Elements);
        editor.Take(Data.Talents["electrifying"]);
        Assert.DoesNotContain(new SpellBook(editor).Spells, b => b.Element == Element.Lightning);
        // Taken in the draft: a preview, dotted, neither to filter by nor to put in the deck.
        editor.Take(Data.Talents["transmute-fire-lightning"]);
        var book = new SpellBook(editor);
        BookSpell[] previews = [.. book.Spells.Where(b => b.Element == Element.Lightning)];
        Assert.NotEmpty(previews);
        Assert.All(previews, b => Assert.True(b.Preview));
        Assert.DoesNotContain(Element.Lightning, book.Elements);
        Assert.Empty(book.Of(Element.Lightning));
        Assert.Contains(storm, editor.Previewed);
        Assert.False(editor.CanAddToDeck(storm));
        Assert.DoesNotContain(editor.Deck, id => Data.Spells[id].Element == Element.Lightning);
        // Saved: its own spells, in the filter and the deck.
        var saved = new PointsEditor(editor.Draft, Data);
        Assert.Empty(saved.Previewed);
        Assert.Contains(Element.Lightning, new SpellBook(saved).Elements);
        Assert.All(new SpellBook(saved).Spells.Where(b => b.Element == Element.Lightning), b => Assert.False(b.Preview));
        Assert.True(saved.CanAddToDeck(storm) || saved.Deck.Count == saved.DeckLimit);
        saved.RemoveFromDeck(Data.Spells["stone-arrow"]);
        saved.AddToDeck(storm);
        Assert.Contains("lightning-fire-arrow", saved.Deck);
        // Its rank is Burning Arrow's; given back, the Transmutation takes its spell out of the deck.
        Assert.Equal(saved.Rank(Data.Spells["fire-arrow"]), saved.Rank(storm));
        saved.Drop(Data.Talents["transmute-fire-lightning"]);
        Assert.DoesNotContain("lightning-fire-arrow", saved.Deck);
        Assert.Null(saved.Draft.Problem(Data));
    }

    [Fact]
    public void TheConversion_ReplacesTheTransmutation_AndTheWindowSaysSo()
    {
        // Sprint 146 (D82, F3 a): the point of the Transmutation comes back, and nothing is paid for it after 40.
        var editor = new PointsEditor(Sentinel(45, "electrifying", "transmute-fire-lightning"), Data);
        Assert.True(editor.CanTake(Data.Talents["convert-lightning"]));
        editor.Take(Data.Talents["convert-lightning"]);
        Assert.Equal(["electrifying", "convert-lightning"], editor.TalentsTaken);
        Assert.Equal("transmute-fire-lightning", editor.Replaced);
        Assert.Equal(0, editor.ResetCost);
        Assert.Equal(7 - 3, editor.TalentPointsLeft);
        Assert.Equal("La Conversion remplace Transmutation : Feu → Foudre : son point revient.", new Texts("fr")["talents.replaced", Data.Talents["transmute-fire-lightning"].Name.Fr]);
    }

    [Fact]
    public void APair_ThatSharesAnElement_SaysWhichAndWithWhom()
    {
        // Sprint 146 (D82, C2): Mud and Ice share Water; Lightning and Mud make a couple.
        var editor = new PointsEditor(Sentinel(35, "bogging"), Data);
        Assert.False(editor.CanTake(Data.Talents["chilling"]));
        Assert.True(editor.CanTake(Data.Talents["electrifying"]));
        Assert.Equal("l'Eau est déjà prise par Boue", new Texts("fr").PairTaken(editor.TalentsTaken, Data.Talents["chilling"], Data));
        Assert.Equal("Water is already taken by Mud", new Texts("en").PairTaken(editor.TalentsTaken, Data.Talents["chilling"], Data));
        Assert.Null(new Texts("fr").PairTaken(editor.TalentsTaken, Data.Talents["electrifying"], Data));
        Assert.Null(new Texts("fr").PairTaken(editor.TalentsTaken, Data.Talents["up-close"], Data));
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
        Assert.Equal("Tronc, sans point : +3 % de dégâts (niv. 30) · +3 % de résistances (niv. 35), à venir · +5 % de PV (niv. 40), à venir", new Texts("fr").Trunk(30));
        Assert.Equal("Trunk, no point: +3% damage (lv 30) · +3% resistances (lv 35) · +5% HP (lv 40)", new Texts("en").Trunk(40));
    }

    [Fact]
    public void GivingATalentBack_AfterForty_SaysItsPrice_TakingOneDoesNot()
    {
        // Sprint 140 (T106): 10 × the level in Éclats, before saving.
        var editor = new PointsEditor(Sentinel(45, "bogging"), Data);
        editor.Take(Data.Talents["chilling"]);
        Assert.Equal(0, editor.ResetCost);
        editor.Drop(Data.Talents["bogging"]);
        Assert.Equal(450, editor.ResetCost);
        var at40 = new PointsEditor(Sentinel(40, "bogging"), Data);
        at40.Drop(Data.Talents["bogging"]);
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
