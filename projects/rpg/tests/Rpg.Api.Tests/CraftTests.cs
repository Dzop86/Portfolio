using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The locksmith on the server (sprint 81): ingredients used up, the key made, the trade's experience; since sprint 109
/// (D76) the smith, a piece gathered, made, worn and sold from start to end.
/// </summary>
public class CraftTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TheKey_IsMadeFromItsIngredients_AndTheTradeLearns_ThenItOpensTheDungeon()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("cra");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Serrure", "male-c", "guard", cancel: Cancel);
        // Nothing yet: refused, and nothing changes.
        var refused = await Assert.ThrowsAsync<ServerException>(() => server.Craft(c.Id, "locksmith", "boar-wallow-key", cancel: Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, refused.Status);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, null, [new("boar-tusk", 13), new("boar-bristle", 9), new("frog-leg", 6), new("slime", 4)]), Cancel);
        // Since sprint 97, a key also costs Éclats: 30 each for the wallow's.
        api.WithDb(db =>
        {
            db.Characters.Single().Shards = 60;
            return db.SaveChanges();
        });
        CharacterSummary made = await server.Craft(c.Id, "locksmith", "boar-wallow-key", 2, Cancel);
        Assert.Equal(0, made.Shards);
        Assert.Equal([new ItemCount("boar-bristle", 1), new ItemCount("boar-tusk", 1), new ItemCount("boar-wallow-key", 2), new ItemCount("frog-leg", 6), new ItemCount("slime", 4)], made.Inventory!);
        Assert.Equal(new Dictionary<string, long> { ["locksmith"] = 50 }, made.Jobs!);
        Assert.Equal(2, made.JobLevel("locksmith"));
        // A level-20 recipe, its ingredients owned: the level is missing.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Craft(c.Id, "locksmith", "frog-grotto-key", cancel: Cancel))).Status);
        // Unknown trade or item, too many at once.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Craft(c.Id, "baker", "bread", cancel: Cancel))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Craft(c.Id, "locksmith", "copper-ring", cancel: Cancel))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Craft(c.Id, "locksmith", "boar-wallow-key", Crafting.MostAtOnce + 1, Cancel))).Status);
        Assert.Equal(made, (await server.Characters(Cancel)).Single());
        // The key made opens its dungeon.
        Assert.Equal(new DungeonRun("boar-wallow", 1), (await server.EnterDungeon(c.Id, "boar-wallow", Cancel)).Run);
        // Someone else's character: unknown.
        GameServer other = await api.SignedIn("oth");
        Assert.Equal(HttpStatusCode.NotFound, (await Assert.ThrowsAsync<ServerException>(() => other.Craft(c.Id, "locksmith", "boar-wallow-key", cancel: Cancel))).Status);
    }

    [Fact]
    public async Task ThePiece_IsGatheredMadeAtTheForge_Worn_AndSoldToRose_ForLessThanItsIngredients()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("smi");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Forge", "female-c", "guard", cancel: Cancel);
        Recipe billhook = GameData.Embedded.Jobs["smith"].RecipeFor("copper-billhook")!;
        // The oaks and the copper gathered on Clairval's spots, the fangs from the wolves (given here).
        foreach (ItemCount wanted in billhook.Ingredients.Take(2))
        {
            HarvestNode spot = GameData.Embedded.Nodes.Values.First(n => n.Item == wanted.Item && Shop.InVillage(n.Map));
            Cell next = spot.At.Neighbours().First(GameData.Embedded.Towns[spot.Map].CanStand);
            api.WithDb(db =>
            {
                Character ch = db.Characters.Single();
                (ch.Town, ch.X, ch.Y) = (spot.Map, next.X, next.Y);
                return db.SaveChanges();
            });
            for (int n = 0; n < wanted.Count; n++)
            {
                await server.Harvest(c.Id, spot.Id, next, Cancel);
                api.Clock.Advance(Harvesting.Regrowth + TimeSpan.FromSeconds(4));
            }
        }
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, null, [new("wolf-fang", 2)]), Cancel);
        // Without the recipe's Éclats: refused.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Craft(c.Id, "smith", "copper-billhook", cancel: Cancel))).Status);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            Cell square = GameData.Embedded.Towns[Shop.Village].Spawn;
            (ch.Shards, ch.Town, ch.X, ch.Y, ch.Level) = (billhook.Shards, Shop.Village, square.X, square.Y, 3);
            return db.SaveChanges();
        });
        CharacterSummary made = await server.Craft(c.Id, "smith", "copper-billhook", cancel: Cancel);
        Assert.Equal(0, made.Shards);
        Assert.Equal([new ItemCount("copper-billhook", 1)], made.Inventory!);
        // A piece of equipment teaches the trade 150 + 15 a level: the smith reaches level 4.
        Assert.Equal(165L, made.Jobs!["smith"]);
        Assert.Equal(4, made.JobLevel("smith"));
        // Worn at level 3, it counts; taken off, Rose buys it at twice its level.
        Assert.Equal("copper-billhook", (await server.SaveEquipment(c.Id, new Dictionary<Slot, string> { [Slot.OneHanded] = "copper-billhook" }, Cancel)).Worn![Slot.OneHanded]);
        await server.SaveEquipment(c.Id, new Dictionary<Slot, string>(), Cancel);
        Assert.Equal(6L, (await server.Sell(c.Id, "copper-billhook", 1, Cancel)).Shards);
    }

    [Fact]
    public async Task TheJewellersBand_IsMade_Worn_ThenBroughtToEudes_WhoGivesItBackMarked_StillWorn()
    {
        // Sprint 110 (D76): the band made at the jeweller's bench, worn, then brought for "first-band": taken and given
        // back in the same answer, it stays on its finger.
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("jew");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Joncs", "male-c", "guard", cancel: Cancel);
        Recipe band = GameData.Embedded.Jobs["jeweller"].RecipeFor("copper-band")!;
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, null, [.. band.Ingredients]), Cancel);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            Cell square = GameData.Embedded.Towns[Shop.Village].Spawn;
            (ch.Shards, ch.Level, ch.Quests, ch.Town, ch.X, ch.Y) = (band.Shards, 3, "palisade-stakes", Shop.Village, square.X, square.Y);
            return db.SaveChanges();
        });
        CharacterSummary made = await server.Craft(c.Id, "jeweller", "copper-band", cancel: Cancel);
        Assert.Equal(165L, made.Jobs!["jeweller"]);
        await server.SaveEquipment(c.Id, new Dictionary<Slot, string> { [Slot.Ring1] = "copper-band" }, Cancel);
        await server.TakeQuest(c.Id, "first-band", Cancel);
        QuestReward reward = await server.HandInQuest(c.Id, "first-band", Cancel);
        CharacterSummary after = (await server.Characters(Cancel)).Single();
        Assert.Equal(30L, after.Shards);
        Assert.Contains("first-band", after.Quests!);
        Assert.Equal([new ItemCount("copper-band", 1)], after.Inventory!);
        Assert.Equal("copper-band", after.Worn![Slot.Ring1]);
        Assert.NotNull(reward);
    }
}
