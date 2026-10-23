using System.Net;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The Forest of the Ancestors by the API (sprint 131): past Quatre-Chemins, its waystone discovered, a spot of level 40 gathered.</summary>
public class ForestTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APlaceInTheForest_NeedsFourTrails_ItsWaystoneIsDiscovered_AndItsSpotsGivePastLevelForty()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("aieux");
        CharacterSummary c = await server.CreateCharacter("Mousse", "female-c", "guard", cancel: Cancel);
        Town entry = GameData.Embedded.Towns["ancestors-1-0"];
        var arrival = new Place(entry.Id, entry.Spawn.X, entry.Spawn.Y);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePlace(c.Id, arrival, Cancel))).Status);
        // A woodcutter of level 39, past Clairval's east gate.
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            ch.Quests = "the-disjoint,four-trails";
            ch.Jobs = $"woodcutter:{Crafting.XpToReach(39)}";
            return db.SaveChanges();
        });
        await server.SavePlace(c.Id, arrival, Cancel);
        Assert.Contains("waystone-ancestors", (await server.Waystones(c.Id, Cancel)).Known);
        HarvestNode hornbeam = GameData.Embedded.Nodes.Values.First(n => n.Item == "hornbeam-log");
        Cell next = hornbeam.At.Neighbours().First(GameData.Embedded.Towns[hornbeam.Map].CanStand);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            (ch.Town, ch.X, ch.Y) = (hornbeam.Map, next.X, next.Y);
            return db.SaveChanges();
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Harvest(c.Id, hornbeam.Id, next, Cancel))).Status);
        api.WithDb(db =>
        {
            db.Characters.Single().Jobs = $"woodcutter:{Crafting.XpToReach(40)}";
            return db.SaveChanges();
        });
        Gathered got = await server.Harvest(c.Id, hornbeam.Id, next, Cancel);
        Assert.Equal((new ItemCount("hornbeam-log", 1), 25L + 5 * 40), (got.Item, got.Xp));
    }
}
