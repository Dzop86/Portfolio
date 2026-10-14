using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The deck as Charles wants it (sprint 82, D66): 0 to 12 spells, placed at will, emptied, shared by a code.</summary>
public class DeckTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly HeroClass Mage = Data.Class("mage")!;

    private static PointsEditor Editor(int level = 100, IReadOnlyList<string>? deck = null) =>
        new(new Hero("Plume", "female-c", "mage", Level: level, Deck: deck), Data);

    [Fact]
    public void ADeck_OfOneSpell_OrNone_IsSaved_AsChosen()
    {
        PointsEditor editor = Editor();
        Assert.Equal(Hero.DeckSize, editor.Deck.Count);
        editor.ClearDeck();
        Assert.Empty(editor.Deck);
        Assert.True(editor.Changed);
        Assert.Equal([], editor.ToPoints().Deck!);
        Assert.Null(editor.Draft.Problem(Data));
        Assert.True(editor.PlaceInDeck(Mage.Spells[5], 0));
        Assert.Equal([Mage.Spells[5]], editor.ToPoints().Deck!);
        Assert.Null(editor.Draft.Problem(Data));
        // Chosen empty, it stays empty: not the class's first spells.
        Assert.Empty(new Hero("Plume", "female-c", "mage", Level: 100, Deck: []).DeckOf(Data));
        Assert.Equal(Hero.DeckSize, new Hero("Plume", "female-c", "mage", Level: 100).DeckOf(Data).Count);
        Assert.NotEqual(new Hero("Plume", "female-c", "mage", Deck: []), new Hero("Plume", "female-c", "mage"));
    }

    [Fact]
    public void ASpellDropped_GoesWhereItIsDropped_MovesIfThere_AndNotPastTwelve()
    {
        PointsEditor editor = Editor(deck: []);
        string[] fire = [.. Mage.Spells.Where(id => Data.Spells[id].Element == Element.Fire)];
        Assert.True(editor.PlaceInDeck(fire[0], 0));
        Assert.True(editor.PlaceInDeck(fire[1], 0));
        Assert.True(editor.PlaceInDeck(fire[2], 7));
        Assert.Equal([fire[1], fire[0], fire[2]], editor.Deck);
        // Moved, not doubled.
        Assert.True(editor.PlaceInDeck(fire[2], 0));
        Assert.Equal([fire[2], fire[1], fire[0]], editor.Deck);
        editor.RemoveFromDeckAt(1);
        Assert.Equal([fire[2], fire[0]], editor.Deck);
        editor.RemoveFromDeckAt(9);
        Assert.Equal(2, editor.Deck.Count);
        // Four fire spells, nothing else: a deck like any other.
        Assert.True(editor.PlaceInDeck(fire[1], 5) && editor.PlaceInDeck(fire[3], 5));
        Assert.Null(editor.Draft.Problem(Data));
        // Not another class's, not locked, not a thirteenth.
        Assert.False(editor.PlaceInDeck(Data.Class("guard")!.Spells[0], 0));
        Assert.False(Editor(level: 1, deck: []).PlaceInDeck(Mage.Spells[^1], 0));
        PointsEditor full = Editor();
        string outside = Mage.Spells.First(id => !full.Deck.Contains(id));
        Assert.False(full.PlaceInDeck(outside, 0));
        Assert.True(full.PlaceInDeck(full.Deck[11], 0));
    }

    [Fact]
    public void TheCode_GivesTheSameDeck_ToAnotherHeroOfTheClass()
    {
        PointsEditor mine = Editor(deck: [Mage.Spells[3], Mage.Spells[0], Mage.Spells[17]]);
        Assert.Equal("MAGE-DAR", mine.Code);
        PointsEditor friend = Editor();
        Assert.Equal(0, friend.UseCode(" mage-dar "));
        Assert.Equal(mine.Deck, friend.Deck);
        Assert.Equal("MAGE-", Editor(deck: []).Code);
        Assert.Equal(0, friend.UseCode("MAGE-"));
        Assert.Empty(friend.Deck);
        // Spells the friend has not unlocked yet are left out, and counted.
        PointsEditor young = Editor(level: 1);
        Assert.Equal(1, young.UseCode("MAGE-AR"));
        Assert.Equal([Mage.Spells[0]], young.Deck);
        // Not a code, or another class's: nothing changes.
        Assert.Null(friend.UseCode("GUARD-AB"));
        Assert.Null(friend.UseCode("hello"));
        Assert.Null(friend.UseCode("MAGE-AA"));
        Assert.Null(friend.UseCode("MAGE-Z"));
        Assert.Null(friend.UseCode("MAGE-ABCDEFGHIJKLM"));
        Assert.Null(DeckCode.Decode(null, Data));
        (string cls, IReadOnlyList<string> spells) = DeckCode.Decode("Guard-b", Data)!.Value;
        Assert.Equal("guard", cls);
        Assert.Equal([Data.Class("guard")!.Spells[1]], spells);
    }
}
