using System.Net;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>Quatre-Chemins by the API (sprint 129, D79): past Clairval's east gate only once "Four Trails" is done.</summary>
public class FourWaysTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APlaceInQuatreChemins_IsRefusedBeforeFourTrails_AndKeptAfter_ItsWaystoneDiscovered()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("qc");
        CharacterSummary c = await server.CreateCharacter("Aube", "female-c", "guard", cancel: Cancel);
        Town square = GameData.Embedded.Towns[Shop.Hamlet];
        var place = new Place(square.Id, square.Spawn.X, square.Spawn.Y);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePlace(c.Id, place, Cancel))).Status);
        // The way to it, in Clairval, is anybody's.
        await server.SavePlace(c.Id, new Place("clairval-3-1", 40, 16), Cancel);
        api.WithDb(db =>
        {
            db.Characters.Single().Quests = "the-disjoint,four-trails";
            return db.SaveChanges();
        });
        await server.SavePlace(c.Id, place, Cancel);
        CharacterSummary there = (await server.Characters(Cancel)).Single();
        Assert.Equal(Shop.Hamlet, there.Place!.Town);
        Assert.Contains("waystone-four-ways", (await server.Waystones(c.Id, Cancel)).Known);
    }
}
