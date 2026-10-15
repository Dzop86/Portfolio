using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The initiative found in loot and the families' sets (sprint 95, D71, docs/suite-apres-d69.md §3): boots,
/// capes and rings carry it, the sets add to it, the turn order follows it with the heroes first on a tie;
/// each family of the Woods and the Mouldy Woods has five pieces, one left by each of its monsters.
/// </summary>
public class InitiativeTests
{
    private static Dictionary<Slot, string> Worn(params (Slot, string)[] items) => items.ToDictionary(i => i.Item1, i => i.Item2);

    [Fact]
    public void TheInitiativeWorn_AndTheSetsBonus_AddToTheClasss()
    {
        var hero = new Hero("Élise", "female-c", "sentinel", Level: 4);
        int bare = Rpg.Core.Fight.HeroTotals(hero, Real)!.Initiative;
        Assert.Equal(10, bare);
        // Two pieces of the pack: theirs, and the set's +10 from two.
        HeroNumbers two = Rpg.Core.Fight.HeroTotals(hero with { Worn = Worn((Slot.Boots, "wolf-boots"), (Slot.Ring1, "howl-ring")) }, Real)!;
        Assert.Equal(bare + 10 + 10 + 10, two.Initiative);
        // The characteristics never give any.
        Assert.Equal(bare, Rpg.Core.Fight.HeroTotals(hero with { Stats = new Characteristics(Air: 40) }, Real)!.Initiative);
    }

    [Fact]
    public void OnATie_TheHeroesPlayFirst()
    {
        GameData data = Data(["B..A"], Spec(1, initiative: 50), Spec(0, initiative: 50));
        var fight = new Fight(data, "s", 1);
        Assert.Equal(0, fight.TurnOrder[0].Team);
    }

    [Fact]
    public void OnlyBootsCapesAndRings_CarryInitiative()
    {
        Item hat = Real.Items["cap-hat"] with { Initiative = 10 };
        Assert.Throws<InvalidDataException>(() => new GameData(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes,
            items: Real.Items.Values.Select(i => i.Id == hat.Id ? hat : i), sets: Real.Sets.Values));
        Assert.All(Real.Items.Values.Where(i => i.Initiative > 0), i => Assert.Contains(i.Slot!.Value, Item.Quick));
    }

    [Theory]
    [InlineData("boar")]
    [InlineData("wolf")]
    [InlineData("mushroom")]
    [InlineData("zombie")]
    public void AFamily_HasFivePieces_OneLeftByEachMonster_TheWeaponByItsChief(string id)
    {
        MonsterFamily family = Real.Families[id];
        Item[] pieces = [.. Real.Items.Values.Where(i => i.Set == id)];
        Assert.Equal(5, pieces.Length);
        Assert.Equal(3, Real.Sets[id].Bonuses.Count);
        foreach (MonsterSpec m in family.Monsters)
        {
            LootDrop[] drops = [.. m.Loot!.Where(l => pieces.Any(p => p.Id == l.Item))];
            if (m.Role == MonsterRole.Boss)
            {
                LootDrop weapon = Assert.Single(drops, d => d.Percent == 30);
                Assert.Contains(Real.Items[weapon.Item].Slot, new ItemSlot?[] { ItemSlot.OneHanded, ItemSlot.TwoHanded });
                Assert.Equal(4, drops.Count(d => d.Percent == 8));
            }
            else
                Assert.Equal(8, Assert.Single(drops).Percent);
        }
        // The breastplate and the pauldrons are left to the craftsmen (§5.6).
        Assert.DoesNotContain(pieces, p => p.Slot is ItemSlot.Chest or ItemSlot.Shoulders);
        // Within the budget: about half of 8 + the level in characteristics, a point of initiative counting 0.6.
        Assert.All(pieces, p => Assert.InRange(Sum(p.Stats) + 0.6 * p.Initiative / 2, 1, (8 + p.Level) / 2.0 + 1));
    }

    [Fact]
    public void ThePack_IsTheWoodsQuickSet_AndTheMushroomsHaveNone()
    {
        Assert.Equal(30, Real.Items.Values.Where(i => i.Set == "wolf").Sum(i => i.Initiative));
        Assert.Equal(0, Real.Items.Values.Where(i => i.Set == "mushroom").Sum(i => i.Initiative));
    }

    [Fact]
    public void TheKits_NoneZoneAndFast_AreWearable_AndFastIsTheQuickest()
    {
        Assert.Empty(Progression.Kit("none", "woods", 6, Real));
        IReadOnlyDictionary<Slot, string> zone = Progression.Kit("zone", "woods", 6, Real);
        IReadOnlyDictionary<Slot, string> fast = Progression.Kit("fast", "woods", 6, Real);
        Assert.Equal(2, zone.Count);
        Assert.Null(Equipment.Problem(fast, 6, Real));
        var hero = new Hero("Élise", "female-c", "guard", Level: 6);
        int Initiative(IReadOnlyDictionary<Slot, string> worn) => Rpg.Core.Fight.HeroTotals(hero with { Worn = worn }, Real)!.Initiative;
        Assert.True(Initiative(fast) > Initiative(zone) && Initiative(zone) > 10);
        // The Woods' "fast" kit: the pack's boots, ring and cape and their bonus, as the design says (50).
        Assert.Equal(50, Initiative(fast));
        Assert.Throws<ArgumentException>(() => Progression.Kit("heavy", "woods", 6, Real));
    }

    private static int Sum(Characteristics? c) => c is null ? 0 : c.Vitality + c.Earth + c.Fire + c.Water + c.Air;
}
