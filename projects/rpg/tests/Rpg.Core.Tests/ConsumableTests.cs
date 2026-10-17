using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The alchemist's potions and oils and the baker's breads (sprint 111, D71, docs/suite-apres-d69.md §5.5 and
/// §5 bis.5): one potion or oil a fight, for the hero, from what it carried in, replayed as it was; breads eaten out
/// of a fight.
/// </summary>
public class ConsumableTests
{
    private static readonly Hero Guard = new("Essai", "female-d", "guard", Level: 10);

    /// <summary>A training fight, the others played by the AI until the hero's turn.</summary>
    private static Fight AtTheHerosTurn(params string[] pack)
    {
        var fight = new Fight(Real, "training", 3, Guard, pack: pack);
        while (fight.Current != fight.HeroFighter)
            fight.Apply(Ai.Decide(fight));
        return fight;
    }

    [Fact]
    public void APotion_HealsTheHero_ForTwoActionPoints_OncePerFight_IfCarried()
    {
        Fight fight = AtTheHerosTurn("healing-potion", "earth-oil");
        Fighter hero = fight.HeroFighter!;
        hero.Hp = 10;
        int ap = hero.Ap;
        Assert.Equal(ActionError.ItemUnavailable, fight.Apply(new UseItemAction("strong-healing-potion")));
        Assert.Equal(ActionError.ItemUnavailable, fight.Apply(new UseItemAction("bread")));
        Assert.Equal(ActionError.None, fight.Apply(new UseItemAction("healing-potion")));
        Assert.Equal((10 + 26, ap - Fight.ItemAp, "healing-potion"), (hero.Hp, hero.Ap, fight.UsedItem));
        Assert.Contains(fight.Events, e => e is ItemUsed { Item: "healing-potion" });
        // One a fight, whichever it is.
        Assert.Equal(ActionError.ItemUnavailable, fight.Apply(new UseItemAction("earth-oil")));
    }

    [Fact]
    public void AnOil_RaisesItsElementsDamage_ForThreeTurns_NotTheOthers()
    {
        Fight fight = AtTheHerosTurn("concentrated-fire-oil");
        Fighter hero = fight.HeroFighter!, foe = fight.Fighters.First(f => f.Team != hero.Team);
        int fire = Fight.Damage(100, hero, foe, Element.Fire), water = Fight.Damage(100, hero, foe, Element.Water);
        Assert.Equal(ActionError.None, fight.Apply(new UseItemAction("concentrated-fire-oil")));
        Assert.Equal(15, hero.DamageBonusFor(Element.Fire));
        Assert.Equal(0, hero.DamageBonusFor(Element.Water));
        Assert.True(Fight.Damage(100, hero, foe, Element.Fire) > fire);
        Assert.Equal(water, Fight.Damage(100, hero, foe, Element.Water));
        Assert.Equal(3, hero.StatusList.Single(s => s.Stat == Stat.Damage).TurnsLeft);
    }

    [Fact]
    public void OnlyTheHero_Drinks_AndTheRecordReplaysItsPack()
    {
        var fight = new Fight(Real, "training", 3, Guard, pack: ["healing-potion"]);
        if (fight.Current != fight.HeroFighter)
            Assert.Equal(ActionError.ItemUnavailable, fight.Apply(new UseItemAction("healing-potion")));
        fight = AtTheHerosTurn("healing-potion");
        Assert.Equal(ActionError.None, fight.Apply(new UseItemAction("healing-potion")));
        Ai.PlayOut(fight);
        FightRecord record = FightRecord.FromJson(FightRecord.Of(fight).ToJson());
        Assert.Equal(["healing-potion"], record.Pack);
        Fight again = record.Replay(Real);
        Assert.Equal((fight.WinningTeam, "healing-potion"), (again.WinningTeam, again.UsedItem));
        // Without the potion carried, the same actions are refused.
        Assert.Throws<InvalidFightRecordException>(() => (record with { Pack = null }).Replay(Real));
    }

    [Fact]
    public void TheAi_DrinksForTheHero_InSimulations_AndTheMonstersNever()
    {
        // Below two fifths of its hit points, the hero played by the AI drinks its potion; no pack, nothing.
        Fight fight = AtTheHerosTurn("healing-potion");
        fight.HeroFighter!.Hp = 1;
        Assert.Equal(new UseItemAction("healing-potion"), Ai.Decide(fight));
        Fight bare = AtTheHerosTurn();
        bare.HeroFighter!.Hp = 1;
        Assert.IsNotType<UseItemAction>(Ai.Decide(bare));
    }

    [Fact]
    public void TheAlchemist_MakesFourPotions_AndAnOilOfEachElement_AtFourSizes()
    {
        Job alchemist = Real.Jobs["alchemist"];
        Item[] made = [.. alchemist.Recipes.Select(r => Real.Items[r.Item])];
        Assert.All(made, i => Assert.True(i.InFight, i.Id));
        Item[] potions = [.. made.Where(i => i.Effects!.Single() is HealEffect)];
        Assert.Equal([3, 10, 18, 26], potions.Select(p => p.Level));
        Assert.All(potions, p => Assert.Equal(20 + 2 * p.Level, ((HealEffect)p.Effects!.Single()).Max));
        StatusEffect[] oils = [.. made.Select(i => i.Effects!.Single()).OfType<StatusEffect>()];
        Assert.Equal(16, oils.Length);
        Assert.All(oils, o => Assert.Equal((Stat.Damage, 3, Affects.Caster), (o.Stat, o.Turns, o.Affects)));
        foreach (IGrouping<int, StatusEffect> size in oils.GroupBy(o => o.Value))
            Assert.Equal([Element.Earth, Element.Fire, Element.Water, Element.Air], size.Select(o => o.Element).Order());
        Assert.Equal([10, 15, 20, 25], oils.Select(o => o.Value).Distinct().Order());
        // The herbalist's plant of the recipe's level in each.
        Assert.All(alchemist.Recipes, r => Assert.Contains(Real.Jobs["herbalist"].Harvests!, h => h.Item == r.Ingredients[0].Item && h.Level == r.Level));
    }

    [Fact]
    public void TheBaker_BakesEightBreads_TheFirstTheVillagesLoaf_EatenOutOfAFight()
    {
        Job baker = Real.Jobs["baker"];
        Assert.Equal([1, 5, 10, 15, 20, 25, 30, 35], baker.Recipes.Select(r => r.Level));
        Assert.Equal(Shop.Loaf, baker.Recipes[0].Item);
        Assert.All(baker.Recipes, r => Assert.Equal(30 + 10 * r.Level, Real.Items[r.Item].Heal));
        Assert.All(baker.Recipes, r => Assert.False(Real.Items[r.Item].InFight));
        // Three of the farmer's grain of its level.
        Assert.All(baker.Recipes, r => Assert.Contains(Real.Jobs["farmer"].Harvests!, h => h.Item == r.Ingredients[0].Item && h.Level == r.Level && r.Ingredients[0].Count == 3));
        // The still and the oven by Sidonie's and Basile's doors.
        Town row = Real.Towns["clairval-3-1"];
        foreach ((string trade, string who) in new[] { ("alchemist", "sidonie"), ("baker", "basile") })
            Assert.True(row.Npcs.Single(n => n.Id == who).At.DistanceTo(row.Workshops!.Single(w => w.Job == trade).At) <= 3, trade);
    }

    [Fact]
    public void APotion_IsAConsumable_OnTheOneWhoDrinksIt()
    {
        Item bad = Real.Items["healing-potion"] with { Effects = [new HealEffect(Affects.Allies, 10, 10)] };
        Assert.Throws<InvalidDataException>(() => new GameData(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes,
            items: Real.Items.Values.Select(i => i.Id == bad.Id ? bad : i), sets: Real.Sets.Values));
    }
}
