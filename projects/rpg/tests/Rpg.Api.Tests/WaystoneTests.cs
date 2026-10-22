using System.Net;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The recall scroll and the waystones by the API (sprint 127, D79).</summary>
public class WaystoneTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AWaystonePassedBy_IsKnown_TravellingSpendsTheÉclats_AndTheScrollTakesOneHome()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("ws");
        CharacterSummary c = await server.CreateCharacter("Aube", "female-c", "guard", cancel: Cancel);
        api.WithDb(db =>
        {
            Character me = db.Characters.First();
            me.Shards = 100;
            me.Receive([new ItemCount(Waystones.Recall, 1)]);
            return db.SaveChanges();
        });
        // Walking by the lake's stone and the village's: both known.
        foreach (string id in new[] { "waystone-lake", "waystone-clairval" })
        {
            Place by = Waystones.Arrival(id, GameData.Embedded);
            await server.SavePlace(c.Id, by, Cancel);
        }
        WaystoneView known = await server.Waystones(c.Id, Cancel);
        Assert.Equal(["waystone-clairval", "waystone-lake"], known.Known);
        // To the fields: unknown, refused; to the lake: there, the Éclats spent.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.TravelTo(c.Id, "waystone-fields", Cancel))).Status);
        CharacterSummary there = await server.TravelTo(c.Id, "waystone-lake", Cancel);
        Assert.Equal(("lake-3-4", 100 - Waystones.Cost("waystone-lake", GameData.Embedded)), (there.Place!.Town, there.Shards));
        // The lake is home now; at the village, the scroll takes it back there, and is used up.
        await server.TravelTo(c.Id, Waystones.Village, Cancel);
        Assert.Equal("waystone-lake", (await server.MakeHome(c.Id, "waystone-lake", Cancel)).Home);
        CharacterSummary home = await server.Eat(c.Id, Waystones.Recall, Cancel);
        Assert.Equal("lake-3-4", home.Place!.Town);
        Assert.DoesNotContain(home.Inventory ?? [], i => i.Item == Waystones.Recall);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Eat(c.Id, Waystones.Recall, Cancel))).Status);
        // Rose sells the scroll.
        await server.SavePlace(c.Id, Waystones.Arrival(Waystones.Village, GameData.Embedded), Cancel);
        await server.Buy(c.Id, Waystones.Recall, 1, Cancel);
    }
}
