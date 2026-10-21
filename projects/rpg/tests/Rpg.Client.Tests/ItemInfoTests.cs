using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// An item's card and its right click (sprint 117, D77): the recipes that make it or need it, the monsters that leave
/// it, its set, what the menu offers; and an item shared in the chat, typed by its name, sent as a link, shown coloured.
/// </summary>
public class ItemInfoTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly ItemInfo Info = new(Data);

    [Fact]
    public void TheRecipes_ThatMakeTheItem_ComeBefore_ThoseThatNeedIt()
    {
        // The boar's bristle is made by no one, and needed by the locksmith (level 1), the cobbler and the tailor (5).
        IReadOnlyList<ItemRecipe> bristle = Info.Recipes("boar-bristle");
        Assert.All(bristle, r => Assert.False(r.Makes));
        Assert.Equal(["locksmith", "cobbler", "tailor"], bristle.Select(r => r.Job.Id).Take(3));
        Assert.Equal([1, 5, 5], bristle.Select(r => r.Recipe.Level).Take(3));
        // A made piece: its own recipe first.
        Item piece = Data.MadePieces.First(i => Info.Recipes(i.Id).Count > 0);
        ItemRecipe first = Info.Recipes(piece.Id)[0];
        Assert.True(first.Makes);
        Assert.Equal(piece.Id, first.Recipe.Item);
        Assert.Empty(Info.Recipes("no-such-thing"));
    }

    [Fact]
    public void TheBestiary_ListsTheMonstersThatLeaveTheItem_TheLikeliestFirst()
    {
        IReadOnlyList<ItemDrop> drops = Info.Drops("boar-bristle");
        Assert.NotEmpty(drops);
        Assert.All(drops, d => Assert.Contains(d.Monster.Loot!, l => l.Item == "boar-bristle" && l.Percent == d.Percent));
        Assert.Equal(drops.Select(d => d.Percent).OrderDescending(), drops.Select(d => d.Percent));
        Assert.Equal("boar", drops[0].Family?.Id);
        // A quest's loot says so (the wolves' marked fang, for the restless beasts).
        Assert.All(Info.Drops("marked-fang"), d => Assert.True(d.ForQuest));
        Assert.Equal("wolf", Info.Drops("marked-fang")[0].Family?.Id);
        Assert.Empty(Info.Drops("copper-ring"));
    }

    [Fact]
    public void TheSet_HasItsPieces_AndAnItemOfNoneHasNoSet()
    {
        Item cape = Data.Items["poacher-cape"];
        (ItemSet set, IReadOnlyList<Item> pieces) = Info.Set(cape)!.Value;
        Assert.Equal(cape.Set, set.Id);
        Assert.Contains(cape, pieces);
        Assert.All(pieces, p => Assert.Equal(set.Id, p.Set));
        Assert.True(pieces.Count >= 2);
        Assert.Null(Info.Set(Data.Items["copper-ring"]));
    }

    [Fact]
    public void TheRightClick_Offers_WhatTheItemAndThePlaceAllow()
    {
        Item cape = Data.Items["poacher-cape"], bread = Data.Items["bread"], bristle = Data.Items["boar-bristle"];
        // Owned in the inventory: put on; worn: taken off; in a fight, neither, nor eaten.
        Assert.Equal(ItemAction.Equip, Info.Actions(cape, new ItemPlace(Owned: 1, InInventory: true))[0]);
        Assert.Equal(ItemAction.TakeOff, Info.Actions(cape, new ItemPlace(Owned: 1, Worn: true, InInventory: true))[0]);
        Assert.DoesNotContain(ItemAction.Equip, Info.Actions(cape, new ItemPlace(Owned: 1, InInventory: true, InFight: true)));
        Assert.DoesNotContain(ItemAction.Equip, Info.Actions(cape, new ItemPlace(Owned: 1)));
        Assert.DoesNotContain(ItemAction.Equip, Info.Actions(cape, new ItemPlace(InInventory: true)));
        Assert.Contains(ItemAction.Set, Info.Actions(cape, new ItemPlace()));
        // Bread: eaten when owned, out of a fight.
        Assert.Equal(ItemAction.Use, Info.Actions(bread, new ItemPlace(Owned: 2))[0]);
        Assert.DoesNotContain(ItemAction.Use, Info.Actions(bread, new ItemPlace(Owned: 2, InFight: true)));
        Assert.DoesNotContain(ItemAction.Use, Info.Actions(bread, new ItemPlace()));
        // A resource: its recipes and its monsters; shared only with the chat at hand, always last.
        Assert.Equal([ItemAction.Recipes, ItemAction.Bestiary], Info.Actions(bristle, new ItemPlace(Owned: 3, InInventory: true)));
        Assert.Equal([ItemAction.Recipes, ItemAction.Bestiary, ItemAction.Share], Info.Actions(bristle, new ItemPlace(CanShare: true)));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void AnItemShared_IsTypedByItsName_SentAsItsLink_AndShownColoured(string lang)
    {
        Item ring = Data.Items["copper-ring"];
        string name = ChatMarkup.Shown(ring, lang);
        Assert.Equal($"[{ring.Name.In(lang)}]", name);
        // Typed: the name in brackets; sent: the link, the rest of the line as typed.
        Assert.Equal($"look {ChatLinks.Of("copper-ring")} here", ChatMarkup.Encode($"look {name} here", ["copper-ring"], Data, lang));
        // The name erased from the field: nothing to turn; an unknown item left alone.
        Assert.Equal("look here", ChatMarkup.Encode("look here", ["copper-ring"], Data, lang));
        Assert.Equal("[x]", ChatMarkup.Encode("[x]", ["no-such-thing"], Data, lang));
        // Twice the same item, twice its link.
        Assert.Equal(ChatLinks.Of("copper-ring") + ChatLinks.Of("copper-ring"), ChatMarkup.Encode(name + name, ["copper-ring", "copper-ring"], Data, lang));
        // Shown: the link in the item's colour, the player's own brackets escaped.
        string code = ChatMarkup.BbCode($"Aube : [b]vends[/b] {ChatLinks.Of("copper-ring")} {ChatLinks.Of("no-such-thing")}", Data, lang);
        Assert.Equal($"Aube : [lb]b]vends[lb]/b] [url=copper-ring][color={ItemStyle.Colour(ring)}][lb]{ring.Name.In(lang)}][/color][/url] [lb]item:no-such-thing]", code);
    }
}
