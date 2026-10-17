using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The budget of every piece of equipment, and the craftsmen's pieces (sprint 109, D76, docs/suite-apres-d69.md
/// §5 quater): about half of 8 + the level in characteristics, a point of initiative counting 0.6, a two-handed
/// weapon twice that; a piece a trade makes is the loot's budget in Vitality and one element, with no initiative.
/// </summary>
public class ItemBudgetTests
{
    /// <summary>The equipment trades, their main gatherer and their second one.</summary>
    public static readonly TheoryData<string, string, string> Trades = new()
    {
        { "smith", "woodcutter", "miner" },
        { "tailor", "farmer", "herbalist" },
        { "jeweller", "miner", "fisher" },
        { "cobbler", "fisher", "woodcutter" },
    };

    /// <summary>The slots each equipment trade makes.</summary>
    private static readonly Dictionary<string, ItemSlot[]> SlotsOf = new()
    {
        ["smith"] = [ItemSlot.OneHanded, ItemSlot.TwoHanded, ItemSlot.Shield],
        ["tailor"] = [ItemSlot.Chest, ItemSlot.Shoulders, ItemSlot.Hat, ItemSlot.Cape],
        ["jeweller"] = [ItemSlot.Ring, ItemSlot.Amulet],
        ["cobbler"] = [ItemSlot.Boots, ItemSlot.Belt],
    };

    /// <summary>The levels of the pieces made in each of the four zones, from the woods to the fields.</summary>
    private static readonly int[][] Zones = [[3, 6], [10, 14], [18, 22], [26, 30]];

    private static int Budget(Item i) => (int)Math.Ceiling((8 + i.Level) / 2.0) * (i.Slot == ItemSlot.TwoHanded ? 2 : 1);

    private static int Sum(Characteristics? c) => c is null ? 0 : c.Vitality + c.Earth + c.Fire + c.Water + c.Air;

    private static int Elements(Characteristics c) => new[] { c.Earth, c.Fire, c.Water, c.Air }.Count(v => v > 0);

    [Fact]
    public void EveryPieceOfEquipment_IsWithinItsBudget_ATwoHandedWeaponCountingTwice()
    {
        // The pet and the mount stay out of the rule.
        foreach (Item i in Real.Items.Values.Where(i => i.Kind == ItemKind.Equipment && i.Slot is not (ItemSlot.Pet or ItemSlot.Mount)))
            Assert.True(Sum(i.Stats) + 0.6 * i.Initiative / 2 <= Budget(i) + 1, $"{i.Id}: {Sum(i.Stats)} points and {i.Initiative} initiative at level {i.Level}");
    }

    [Fact]
    public void ACraftsmansPiece_IsTheLootsBudget_InVitalityAndOneElement_WithoutInitiative()
    {
        Item[] made = [.. Real.MadePieces];
        Assert.Equal(32, made.Length);
        foreach (Item i in made)
        {
            Assert.InRange(Sum(i.Stats), Budget(i) - 1, Budget(i) + 1);
            Assert.True(i.Stats!.Vitality > 0 && Elements(i.Stats) == 1, i.Id);
            Assert.Equal((0, 0, 0, null), (i.Initiative, i.Ap, i.Mp, i.Set));
        }
        // The loading refuses a made piece that would be quick.
        Item quick = Real.Items["rye-pelerine"] with { Initiative = 5 };
        Assert.Throws<InvalidDataException>(() => new GameData(Real.Spells.Values, Real.Maps.Values, Real.Scenarios.Values, Real.Classes,
            items: Real.Items.Values.Select(i => i.Id == quick.Id ? quick : i), sets: Real.Sets.Values, jobs: Real.Jobs.Values));
    }

    [Theory]
    [MemberData(nameof(Trades))]
    public void AnEquipmentTrade_HasARecipeEveryFiveLevels_FromItsGatherers_AndAMonstersResource(string trade, string main, string second)
    {
        Job job = Real.Jobs[trade];
        Recipe[] pieces = [.. job.Recipes.Where(r => Real.Items[r.Item].Kind == ItemKind.Equipment)];
        Assert.Equal([1, 5, 10, 15, 20, 25, 30, 35], pieces.Select(r => r.Level));
        Assert.NotNull(job.Workshop);
        foreach (Recipe r in pieces)
        {
            Item item = Real.Items[r.Item];
            Assert.Contains(item.Slot!.Value, SlotsOf[trade]);
            Assert.Equal(3, r.Ingredients.Count);
            Assert.Equal(r.Level, Real.Jobs[main].Harvests!.Single(h => h.Item == r.Ingredients[0].Item).Level);
            Assert.Equal(r.Level, Real.Jobs[second].Harvests!.Single(h => h.Item == r.Ingredients[1].Item).Level);
            int monster = Shop.Level(Real.Items[r.Ingredients[2].Item], Real);
            Assert.True(Real.Monsters.Values.Any(m => (m.Loot ?? []).Any(l => l.Item == r.Ingredients[2].Item)), r.Item);
            Assert.InRange(monster, item.Level - 10, item.Level);
            // The Éclats: the item's level, twice for two hands.
            Assert.Equal(item.Level * (item.Slot == ItemSlot.TwoHanded ? 2 : 1), r.Shards);
        }
        // Each slot of the trade is made.
        Assert.Equal(SlotsOf[trade].Order(), pieces.Select(r => Real.Items[r.Item].Slot!.Value).Distinct().Order());
    }

    [Fact]
    public void TheMadePieces_OfAZone_CoverTheFourElementsAlike_TwoOfATradeNeverTheSame()
    {
        foreach (int[] levels in Zones)
        {
            Item[] here = [.. Real.MadePieces.Where(i => levels.Contains(i.Level))];
            int[] counts = [here.Count(i => i.Stats!.Earth > 0), here.Count(i => i.Stats!.Fire > 0), here.Count(i => i.Stats!.Water > 0), here.Count(i => i.Stats!.Air > 0)];
            Assert.True(counts.All(c => c == counts[0] && c > 0), $"zone of levels {levels[0]} and {levels[1]}: {string.Join(", ", counts)}");
            foreach (Job job in Real.Jobs.Values.Where(j => j.Recipes.Any(r => here.Any(i => i.Id == r.Item))))
            {
                Item[] mine = [.. here.Where(i => job.RecipeFor(i.Id) is not null)];
                Assert.Equal(mine.Length, mine.Select(i => (i.Stats!.Earth > 0, i.Stats.Fire > 0, i.Stats.Water > 0, i.Stats.Air > 0)).Distinct().Count());
            }
        }
        // Since sprint 110: the rings, the amulets, the boots and the belts each cover the four elements over the 35 levels.
        foreach (ItemSlot slot in new[] { ItemSlot.Ring, ItemSlot.Amulet, ItemSlot.Boots, ItemSlot.Belt })
        {
            Characteristics[] pieces = [.. Real.MadePieces.Where(i => i.Slot == slot).Select(i => i.Stats!)];
            Assert.True(pieces.Any(c => c.Earth > 0) && pieces.Any(c => c.Fire > 0) && pieces.Any(c => c.Water > 0) && pieces.Any(c => c.Air > 0), slot.ToString());
        }
    }

    [Fact]
    public void MakingAPiece_TeachesMoreThanAKey_AndNothingTwentyLevelsUnderTheTrade()
    {
        Recipe billhook = Real.Jobs["smith"].RecipeFor("copper-billhook")!, falchion = Real.Jobs["smith"].RecipeFor("reaper-falchion")!;
        Recipe stake = Real.Jobs["smith"].RecipeFor("palisade-stake")!;
        Assert.Equal(165, Crafting.Xp(billhook, Real.Items[billhook.Item]));
        Assert.Equal(675, Crafting.Xp(falchion, Real.Items[falchion.Item], 35));
        Assert.Equal(25, Crafting.Xp(stake, Real.Items[stake.Item]));
        Assert.Equal(165, Crafting.Xp(billhook, Real.Items[billhook.Item], billhook.Level + Crafting.NoXpBelow - 1));
        Assert.Equal(0, Crafting.Xp(billhook, Real.Items[billhook.Item], billhook.Level + Crafting.NoXpBelow));
    }

    [Fact]
    public void MakingToSell_NeverPaysMoreThanSellingTheIngredients()
    {
        // Rose buys a piece at twice its level and resources at 1 + level / 10 (T65); the recipe's Éclats close the loop.
        foreach (Recipe r in Real.Jobs.Values.SelectMany(j => j.Recipes).Where(r => Real.Items[r.Item].Kind == ItemKind.Equipment))
        {
            long ingredients = r.Ingredients.Sum(i => Shop.BuyPrice(Real.Items[i.Item], Real)!.Value * i.Count);
            Assert.True(ingredients + r.Shards > Shop.BuyPrice(Real.Items[r.Item], Real), r.Item);
        }
    }

    [Fact]
    public void TheStartingPieces_AreBackWithinTheBudget_SoTheCraftsmensAreWorthIt()
    {
        // §5 quater.7: the orc's breastplate (20 points) beat every chest made up to level 30.
        Assert.Equal(7, Sum(Real.Items["orc-breastplate"].Stats));
        Assert.True(Sum(Real.Items["wooden-shield"].Stats) < Sum(Real.Items["yew-targe"].Stats));
        Assert.Equal(new Characteristics(Air: 5), Real.Sets["poacher"].Bonuses.Single(b => b.Pieces == 2).Stats);
    }
}
