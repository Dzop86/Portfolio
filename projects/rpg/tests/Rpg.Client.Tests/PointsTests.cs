using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The characteristics and spells screens, and what a fight earned (sprint 58).</summary>
public class PointsTests
{
    private static GameData Data => GameData.Embedded;

    [Fact]
    public void TheButtons_SpendThePointsTheLevelGives_NeverMore()
    {
        var editor = new PointsEditor(new Hero("Élise", "female-c", "guard", Level: 3), Data);
        Assert.Equal((20, 2), (editor.CharacteristicPointsLeft, editor.SpellPointsLeft));
        editor.Add(Characteristic.Vitality, 15);
        editor.Add(Characteristic.Earth, 10);
        Assert.Equal((15, 5, 0), (editor[Characteristic.Vitality], editor[Characteristic.Earth], editor.CharacteristicPointsLeft));
        Assert.False(editor.CanAdd(Characteristic.Water));
        editor.Remove(Characteristic.Vitality, 20);
        Assert.Equal((0, 15), (editor[Characteristic.Vitality], editor.CharacteristicPointsLeft));
        Assert.False(editor.CanRemove(Characteristic.Air));
        Assert.Null(editor.Draft.Problem(Data));
    }

    [Fact]
    public void ARank_CostsMoreTheHigherItGoes_OnlyForUnlockedSpells()
    {
        var editor = new PointsEditor(new Hero("Élise", "female-c", "guard", Level: 4), Data);
        Spell bash = Data.Spells["shield-bash"];
        Assert.Equal(["shield-bash", "burning-blade", "ice-blade", "slash", "taunt"], editor.Spells.Select(s => s.Id));
        Assert.Equal((1, 1), (editor.Rank(bash), editor.NextRankCost(bash)));
        editor.Raise(bash);
        Assert.Equal((2, 2, 2), (editor.Rank(bash), editor.NextRankCost(bash), editor.SpellPointsLeft));
        editor.Raise(bash);
        Assert.Equal((3, 0), (editor.Rank(bash), editor.SpellPointsLeft));
        Assert.False(editor.CanRaise(Data.Spells["slash"]));
        Assert.False(editor.CanRaise(Data.Spells["bulwark"]));
        editor.Lower(bash);
        editor.Lower(bash);
        editor.Lower(bash);
        Assert.Equal((1, 3), (editor.Rank(bash), editor.SpellPointsLeft));
        Assert.False(editor.Changed);
    }

    [Fact]
    public void WhatIsSaved_StartsTheEditor_AndTheDraftGoesToTheServer()
    {
        var saved = new Hero("Élise", "female-c", "mage", Level: 10, Stats: new Characteristics(Vitality: 30, Fire: 20), Ranks: new Dictionary<string, int> { ["ice-shard"] = 3 });
        var editor = new PointsEditor(saved, Data);
        Assert.Equal((30, 20, 40, 3, 6), (editor[Characteristic.Vitality], editor[Characteristic.Fire], editor.CharacteristicPointsLeft, editor.Rank(Data.Spells["ice-shard"]), editor.SpellPointsLeft));
        Assert.False(editor.Changed);
        editor.Add(Characteristic.Water, 5);
        Assert.True(editor.Changed);
        Points p = editor.ToPoints();
        Assert.Equal(new Characteristics(Vitality: 30, Fire: 20, Water: 5), p.Stats);
        Assert.Equal(3, p.Ranks!["ice-shard"]);
    }

    [Fact]
    public void TheEndScreen_SaysWhatTheFightEarned()
    {
        var fr = new Texts("fr");
        Assert.Equal("+210 XP · Quête terminée : Première leçon · Niveau 2 !", fr.Result(new FightResult(210, 210, 2, "first-lesson"), 1));
        Assert.Equal("+60 XP", new Texts("en").Result(new FightResult(60, 270, 2), 2));
        Assert.Equal("Niveau 2 · 270 / 300 XP", fr.XpLine(2, 270));
        Assert.Equal("Level 100 · 600000 XP", new Texts("en").XpLine(100, 600_000));
        foreach (Characteristic c in Enum.GetValues<Characteristic>())
            Assert.NotEqual(fr["char." + c], new Texts("en")["char." + c + ".help"]);
    }

    [Fact]
    public void ThePoints_CanBeTakenBack_AndSpentElsewhere_ByValueMinOrMax()
    {
        // A hero who spent everything in Fire spends it in Water instead (D62).
        var editor = new PointsEditor(new Hero("Élise", "female-c", "mage", Level: 11, Stats: new Characteristics(Fire: 100)), Data);
        Assert.Equal((0, 100), (editor.CharacteristicPointsLeft, editor.Max(Characteristic.Fire)));
        editor.ResetCharacteristics();
        Assert.Equal((100, 0), (editor.CharacteristicPointsLeft, editor[Characteristic.Fire]));
        editor.Set(Characteristic.Water, editor.Max(Characteristic.Water));
        Assert.Equal((100, 0), (editor[Characteristic.Water], editor.CharacteristicPointsLeft));
        // A typed value stays within bounds.
        editor.Set(Characteristic.Water, 250);
        editor.Set(Characteristic.Air, -5);
        Assert.Equal((100, 0), (editor[Characteristic.Water], editor[Characteristic.Air]));
        editor.Set(Characteristic.Water, 30);
        Assert.Equal((30, 70, 70), (editor[Characteristic.Water], editor.CharacteristicPointsLeft, editor.Max(Characteristic.Earth)));
        Assert.Null(editor.Draft.Problem(Data));
        Assert.Equal(new Characteristics(Water: 30), editor.ToPoints().Stats);
    }

    [Fact]
    public void TheDeck_StartsWithTheFirstSpells_TakesOnlyUnlockedOnes_UpToTwelve()
    {
        var editor = new PointsEditor(new Hero("Élise", "female-c", "mage", Level: 1), Data);
        // At level 1, the five spells unlocked; left as it is, the deck is no change.
        Assert.Equal(Data.Class("mage")!.Spells.Take(5), editor.Deck);
        Assert.False(editor.Changed);
        Spell mend = Data.Spells["mend"], ward = Data.Spells["frost-ward"];
        editor.RemoveFromDeck(mend);
        Assert.Equal(4, editor.Deck.Count);
        Assert.True(editor.Changed);
        Assert.False(editor.CanAddToDeck(ward));
        editor.AddToDeck(mend);
        Assert.Equal("mend", editor.Deck[^1]);
        Assert.Equal(editor.Deck, editor.ToPoints().Deck);
        // A hero of level 95 has every spell, but only twelve go in the deck.
        var high = new PointsEditor(new Hero("Élise", "female-c", "mage", Level: 95), Data);
        Assert.Equal(12, high.Deck.Count);
        Spell last = Data.Spells[Data.Class("mage")!.Spells[^1]];
        Assert.False(high.CanAddToDeck(last));
        high.RemoveFromDeck(Data.Spells[high.Deck[0]]);
        high.AddToDeck(last);
        Assert.Equal(last.Id, high.Deck[^1]);
        Assert.True(new SpellBook(high).Spells.Single(s => s.Spell.Id == last.Id).InDeck);
        Assert.Null(high.Draft.Problem(Data));
    }
}
