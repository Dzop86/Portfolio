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
        // Two copper rings owned, but the same ring never on both hands (sprint 110, D76).
        Assert.Equal("already worn", editor.WhyNot(ring));
        Assert.False(editor.Put(ring));
        Assert.Equal(["copper-ring"], editor.Worn.Values);
        Assert.Equal(1, editor.Page(InventoryPage.Equipment)[0].Free);
        editor.Put(Data.Items["dagger"]);
        Assert.Equal("none left", editor.WhyNot(Data.Items["dagger"]));
        editor.Put(Data.Items["wooden-shield"]);
        // The club takes both hands; the dagger then takes the club off.
        editor.Put(Data.Items["orc-club"]);
        Assert.Equal([Slot.Ring1, Slot.TwoHanded], editor.Worn.Keys.Order());
        editor.Put(Data.Items["dagger"]);
        Assert.Equal([Slot.Ring1, Slot.OneHanded], editor.Worn.Keys.Order());
        Assert.Equal("level 20", editor.WhyNot(Data.Items["donkey"]));
        Assert.Equal("not equipment", editor.WhyNot(Data.Items["bread"]));
        Assert.False(editor.Put(Data.Items["bread"]));
        Assert.Null(editor.Draft.Problem(Data));
        Assert.True(editor.Changed);
        editor.TakeOff(Slot.Ring1);
        Assert.Equal(2, editor.Page(InventoryPage.Equipment)[0].Free);
    }

    [Fact]
    public void AnItemCard_SaysWhereItGoes_WhatItGives_AndItsSet()
    {
        var fr = new Texts("fr");
        Assert.Equal("Cape du braconnier · Cape · niveau 1\n+5 Air\nPanoplie du braconnier : 2 pièces +5 Air ; 3 pièces +20 Air, +1 PM", fr.ItemCard(Data.Items["poacher-cape"]));
        Assert.Equal("Donkey · Mount · level 20\n+1 MP", new Texts("en").ItemCard(Data.Items["donkey"]));
        Assert.Equal("Pain · consommable\nRend 40 PV, hors combat.", fr.ItemCard(Data.Items["bread"]));
        // Since sprint 111, a potion and an oil say what they do in a fight.
        Assert.Equal("Potion de soin · consommable\nEn combat : rend 26 PV (2 PA, une potion ou une huile par combat).", fr.ItemCard(Data.Items["healing-potion"]));
        Assert.Equal("Earth Oil · consumable\nIn a fight: +10 % Earth damage for 3 turns (2 AP, one potion or oil a fight).", new Texts("en").ItemCard(Data.Items["earth-oil"]));
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

    [Fact]
    public void InAFight_TheEquipmentIsShown_NothingIsPutOnOrTakenOff()
    {
        var hero = new Hero("Élise", "female-c", "guard", Level: 10, Worn: new Dictionary<Slot, string> { [Slot.Ring1] = "copper-ring" });
        var editor = new InventoryEditor(hero, Bag, Data, readOnly: true);
        Assert.Equal("in a fight", editor.WhyNot(Data.Items["dagger"]));
        Assert.False(editor.Put(Data.Items["dagger"]));
        editor.TakeOff(Slot.Ring1);
        Assert.Equal("copper-ring", editor.Worn[Slot.Ring1]);
        Assert.Equal(8, editor.Page(InventoryPage.All).Count);
        Assert.False(editor.Changed);
    }
}
