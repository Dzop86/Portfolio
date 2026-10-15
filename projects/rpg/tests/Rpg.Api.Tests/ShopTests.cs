using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>Rose's stall and the Éclats spent (sprint 97, D71): the server decides every deal.</summary>
public class ShopTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => (await Assert.ThrowsAsync<ServerException>(call)).Status;

    private static async Task<(ApiFactory Api, GameServer Server, CharacterSummary Character, string Account)> Shopper(int level, long shards, string? town, params ItemCount[] items)
    {
        var api = new ApiFactory();
        GameServer server = await api.SignedIn("sho");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, level, items), Cancel);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            (ch.Shards, ch.Town, ch.X, ch.Y) = (shards, town, 5, 5);
            return db.SaveChanges();
        });
        return (api, server, (await server.Characters(Cancel)).Single(), account);
    }

    [Fact]
    public async Task Rose_SellsAndBuys_InTheVillage_ForÉclats_NeverWhatIsWornOrTooDear()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Shopper(3, 12, Shop.Village, new ItemCount("boar-bristle", 2), new ItemCount("acorn-ring", 1));
        await using (api)
        {
            CharacterSummary after = await server.Buy(c.Id, "boar-bristle", 2, Cancel);
            Assert.Equal((2L, 4), (after.Shards, after.Inventory!.Single(i => i.Item == "boar-bristle").Count));
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.Buy(c.Id, "boar-bristle", 1, Cancel)));
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.Buy(c.Id, "boar-wallow-key", 1, Cancel)));
            after = await server.Sell(c.Id, "boar-bristle", 4, Cancel);
            Assert.Equal(6L, after.Shards);
            Assert.DoesNotContain(after.Inventory!, i => i.Item == "boar-bristle");
            await server.SaveEquipment(c.Id, new Dictionary<Slot, string> { [Slot.Ring1] = "acorn-ring" }, Cancel);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.Sell(c.Id, "acorn-ring", 1, Cancel)));
            await server.SaveEquipment(c.Id, new Dictionary<Slot, string>(), Cancel);
            Assert.Equal(6L + 2, (await server.Sell(c.Id, "acorn-ring", 1, Cancel)).Shards);
        }
    }

    [Fact]
    public async Task OutOfTheVillage_ThereIsNoStall()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Shopper(3, 100, "woods-0-0", new ItemCount("boar-bristle", 2));
        await using (api)
        {
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.Buy(c.Id, "boar-bristle", 1, Cancel)));
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.Sell(c.Id, "boar-bristle", 1, Cancel)));
        }
    }

    [Fact]
    public async Task TakingPointsBack_CostsÉclatsFromLevelEleven_AndAKey_ItsPrice()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Shopper(20, 250, Shop.Village, new ItemCount("boar-tusk", 6), new ItemCount("boar-bristle", 4));
        await using (api)
        {
            var some = new Points(new Characteristics(Vitality: 20), new Dictionary<string, int>(), null);
            CharacterSummary spent = await server.SavePoints(c.Id, some, Cancel);
            Assert.Equal(250L, spent.Shards);
            CharacterSummary back = await server.SavePoints(c.Id, some with { Stats = new Characteristics(Vitality: 10) }, Cancel);
            Assert.Equal(250L - Shop.ResetCost(20), back.Shards);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, some with { Stats = Characteristics.None }, Cancel)));
            // The wallow's key: its ingredients and 30 of the 50 Éclats left.
            CharacterSummary key = await server.Craft(c.Id, "locksmith", "boar-wallow-key", cancel: Cancel);
            Assert.Equal(50L - 30, key.Shards);
        }
    }
}
