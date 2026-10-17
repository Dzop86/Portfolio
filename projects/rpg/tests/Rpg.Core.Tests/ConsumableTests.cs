using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The alchemist's potions and the baker's breads (sprint 111, D71; out of a fight only since sprint 115, D77): they
/// heal out of a fight; a fight has no action to drink or eat, and a record that tries is refused.
/// </summary>
public class ConsumableTests
{
    private static readonly Hero Guard = new("Essai", "female-d", "guard", Level: 10);

    [Fact]
    public void TheAlchemist_MakesFourHealingPotions_DrunkOutOfAFight()
    {
        Job alchemist = Real.Jobs["alchemist"];
        Item[] potions = [.. alchemist.Recipes.Select(r => Real.Items[r.Item])];
        Assert.Equal([3, 10, 18, 26], potions.Select(p => p.Level));
        Assert.All(potions, p => Assert.Equal((ItemKind.Consumable, 50 + 20 * p.Level), (p.Kind, p.Heal)));
        // The herbalist's plant of the recipe's level in each.
        Assert.All(alchemist.Recipes, r => Assert.Contains(Real.Jobs["herbalist"].Harvests!, h => h.Item == r.Ingredients[0].Item && h.Level == r.Level));
        // The oils of sprint 111 are gone.
        Assert.DoesNotContain(Real.Items.Keys, id => id.EndsWith("-oil", StringComparison.Ordinal) || id.EndsWith("essence", StringComparison.Ordinal));
    }

    [Fact]
    public void APotion_HealsMore_ThanTheBreadOfItsLevel()
    {
        foreach (Recipe potion in Real.Jobs["alchemist"].Recipes)
        {
            Recipe bread = Real.Jobs["baker"].Recipes.Last(r => r.Level <= potion.Level);
            Assert.True(Real.Items[potion.Item].Heal > Real.Items[bread.Item].Heal, potion.Item);
        }
    }

    [Fact]
    public void TheBaker_BakesEightBreads_TheFirstTheVillagesLoaf_EatenOutOfAFight()
    {
        Job baker = Real.Jobs["baker"];
        Assert.Equal([1, 5, 10, 15, 20, 25, 30, 35], baker.Recipes.Select(r => r.Level));
        Assert.Equal(Shop.Loaf, baker.Recipes[0].Item);
        Assert.All(baker.Recipes, r => Assert.Equal(30 + 10 * r.Level, Real.Items[r.Item].Heal));
        // Three of the farmer's grain of its level.
        Assert.All(baker.Recipes, r => Assert.Contains(Real.Jobs["farmer"].Harvests!, h => h.Item == r.Ingredients[0].Item && h.Level == r.Level && r.Ingredients[0].Count == 3));
        // The still and the oven by Sidonie's and Basile's doors.
        Town row = Real.Towns["clairval-3-1"];
        foreach ((string trade, string who) in new[] { ("alchemist", "sidonie"), ("baker", "basile") })
            Assert.True(row.Npcs.Single(n => n.Id == who).At.DistanceTo(row.Workshops!.Single(w => w.Job == trade).At) <= 3, trade);
    }

    [Fact]
    public void AFight_HasNoActionToDrink_AndARecordThatDrinks_IsRefused()
    {
        var fight = new Fight(Real, "training", 3, Guard);
        Ai.PlayOut(fight);
        Assert.DoesNotContain(fight.History, a => a is not (MoveAction or CastAction or EndTurnAction));
        // A record of sprint 111 with a potion drunk ("use") no longer reads.
        var record = (System.Text.Json.Nodes.JsonObject)System.Text.Json.Nodes.JsonNode.Parse(FightRecord.Of(fight).ToJson())!;
        string actions = record.Select(p => p.Key).Single(k => k.Equals("actions", StringComparison.OrdinalIgnoreCase));
        record[actions]!.AsArray().Insert(0, new System.Text.Json.Nodes.JsonObject { ["type"] = "use", ["item"] = "healing-potion" });
        Assert.Throws<InvalidFightRecordException>(() => FightRecord.FromJson(record.ToJsonString()));
    }
}
