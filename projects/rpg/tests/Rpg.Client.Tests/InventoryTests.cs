using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The inventory and equipment screen, the item cards, the loot on the end screen (sprint 59).</summary>
public class InventoryTests
{
    private static GameData Data => GameData.Embedded;

    private static readonly ItemCount[] Bag =
        [new("copper-ring", 2), new("orc-club", 1), new("dagger", 1), new("wooden-shield", 1), new("bread", 3), new("orc-fang", 2), new("garance-badge", 1), new("donkey", 1)];

    [Fact]
    public void ThePages_SortTheItemsByKind()
    {
        var editor = new InventoryEditor(new Hero("Élise", "female-c", "guard", Level: 10), Bag, Data);
        Assert.Equal(8, editor.Page(InventoryPage.All).Count);
        Assert.Equal(["copper-ring", "dagger", "wooden-shield", "orc-club", "donkey"], editor.Page(InventoryPage.Equipment).Select(p => p.Item.Id));
        Assert.Equal([("bread", 3)], editor.Page(InventoryPage.Consumable).Select(p => (p.Item.Id, p.Count)));
        Assert.Equal(["orc-fang"], editor.Page(InventoryPage.Resource).Select(p => p.Item.Id));
        Assert.Equal(["garance-badge"], editor.Page(InventoryPage.Quest).Select(p => p.Item.Id));
    }

    [Fact]
    public void PuttingOn_FindsTheSlot_TakesOffWhatCannotStay_AndNeverWearsMoreThanOwned()
    {
        var editor = new InventoryEditor(new Hero("Élise", "female-c", "guard", Level: 10), Bag, Data);
        Item ring = Data.Items["copper-ring"];
        Assert.True(editor.Put(ring));
        Assert.True(editor.Put(ring));
        Assert.Equal(("copper-ring", "copper-ring"), (editor.Worn[Slot.Ring1], editor.Worn[Slot.Ring2]));
        Assert.Equal("none left", editor.WhyNot(ring));
        Assert.Equal(0, editor.Page(InventoryPage.Equipment)[0].Free);
        editor.Put(Data.Items["dagger"]);
        editor.Put(Data.Items["wooden-shield"]);
        // The club takes both hands; the dagger then takes the club off.
        editor.Put(Data.Items["orc-club"]);
        Assert.Equal([Slot.Ring1, Slot.Ring2, Slot.TwoHanded], editor.Worn.Keys.Order());
        editor.Put(Data.Items["dagger"]);
        Assert.Equal([Slot.Ring1, Slot.Ring2, Slot.OneHanded], editor.Worn.Keys.Order());
        Assert.Equal("level 20", editor.WhyNot(Data.Items["donkey"]));
        Assert.Equal("not equipment", editor.WhyNot(Data.Items["bread"]));
        Assert.False(editor.Put(Data.Items["bread"]));
        Assert.Null(editor.Draft.Problem(Data));
        Assert.True(editor.Changed);
        editor.TakeOff(Slot.Ring1);
        Assert.Equal(1, editor.Page(InventoryPage.Equipment)[0].Free);
    }

    [Fact]
    public void AnItemCard_SaysWhereItGoes_WhatItGives_AndItsSet()
    {
        var fr = new Texts("fr");
        Assert.Equal("Cape du braconnier · Cape · niveau 1\n+6 Agilité\nPanoplie du braconnier : 2 pièces +10 Agilité ; 3 pièces +20 Agilité, +1 PM", fr.ItemCard(Data.Items["poacher-cape"]));
        Assert.Equal("Donkey · Mount · level 20\n+1 MP", new Texts("en").ItemCard(Data.Items["donkey"]));
        Assert.Equal("Pain · consommable", fr.ItemCard(Data.Items["bread"]));
        foreach (Item i in Data.Items.Values)
        {
            Assert.DoesNotContain("{", fr.ItemCard(i), StringComparison.Ordinal);
            Assert.DoesNotContain("{", new Texts("en").ItemCard(i), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheEndScreen_ListsTheLoot()
    {
        Assert.Equal("+60 XP\nButin : Croc d'orque ×2, Pain", new Texts("fr").Result(new FightResult(60, 60, 1, null, [new("orc-fang", 2), new("bread", 1)]), 1));
    }
}
