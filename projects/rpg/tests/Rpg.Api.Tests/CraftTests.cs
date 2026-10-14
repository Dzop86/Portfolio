using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The locksmith on the server (sprint 81): ingredients used up, the key made, the trade's experience.</summary>
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
        CharacterSummary made = await server.Craft(c.Id, "locksmith", "boar-wallow-key", 2, Cancel);
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
}
