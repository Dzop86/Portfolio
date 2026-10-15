using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>Rose's stall as the game shows it (sprint 97), and the price of taking points back on the sheet.</summary>
public class StallTests
{
    private static CharacterSummary Someone(long shards, ItemCount[] inventory, Dictionary<Slot, string>? worn = null, int level = 20) =>
        new(Guid.Empty, "Élise", "female-c", "guard", 0, DateTimeOffset.UnixEpoch, Level: level, Inventory: inventory, Worn: worn, Shards: shards);

    [Fact]
    public void TheStall_SaysTheÉclats_WhatTheyBuy_AndWhatRoseWouldTake()
    {
        var fr = new Texts("fr");
        var stall = new Stall(Someone(12, [new("boar-bristle", 3), new("acorn-ring", 1), new("garance-badge", 1)], new() { [Slot.Ring1] = "acorn-ring" }), fr);
        Assert.Equal("L'étal de Rose · 12 Éclats", stall.Title);
        StallRow bristle = stall.ToBuy.Single(r => r.Item.Id == "boar-bristle");
        Assert.Equal((5L, 2), (bristle.Price, bristle.Most));
        Assert.Equal("Soies de sanglier · 5 Éclats", bristle.Title);
        // The badge is not bought; the ring is worn.
        Assert.Equal(["boar-bristle", "acorn-ring"], stall.ToSell.Select(r => r.Item.Id));
        Assert.Equal((0, "Porté."), (stall.ToSell[1].Most, stall.ToSell[1].Refusal));
        Assert.Equal(3, stall.ToSell[0].Most);
        Assert.All(new Stall(null, fr).ToBuy, r => Assert.Equal(0, r.Most));
    }

    [Fact]
    public void TheSheet_SaysWhatTakingPointsBackWillCost()
    {
        var hero = new Hero("Élise", "female-c", "guard", Level: 20, Stats: new Characteristics(Vitality: 20));
        var editor = new PointsEditor(hero, GameData.Embedded);
        editor.Add(Characteristic.Earth, 5);
        Assert.Equal(0, editor.ResetCost);
        editor.Remove(Characteristic.Vitality, 1);
        Assert.Equal(200, editor.ResetCost);
        Assert.Equal("Reprendre des points coûtera 200 Éclats.", new Texts("fr")["points.reset-cost", editor.ResetCost]);
    }
}
