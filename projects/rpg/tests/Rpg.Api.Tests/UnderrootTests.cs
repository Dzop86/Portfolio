using System.Net;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The Underroot Mines by the API (sprint 224): past the forest, their waystone discovered, a spot of level 50 gathered.</summary>
public class UnderrootTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APlaceInTheMines_NeedsFourTrails_ItsWaystoneIsDiscovered_AndItsSpotsGivePastLevelFifty()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("sousracine");
        CharacterSummary c = await server.CreateCharacter("Galène", "female-c", "warrior", cancel: Cancel);
        Town entry = GameData.Embedded.Towns["underroot-2-0"];
        var arrival = new Place(entry.Id, entry.Spawn.X, entry.Spawn.Y);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePlace(c.Id, arrival, Cancel))).Status);
        // A miner of level 49, past Clairval's east gate.
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            ch.Quests = "the-disjoint,four-trails";
            ch.Jobs = $"miner:{Crafting.XpToReach(49)}";
            return db.SaveChanges();
        });
        await server.SavePlace(c.Id, arrival, Cancel);
        Assert.Contains("waystone-underroot", (await server.Waystones(c.Id, Cancel)).Known);
        HarvestNode cobalt = GameData.Embedded.Nodes.Values.First(n => n.Item == "cobalt-ore");
        Cell next = cobalt.At.Neighbours().First(GameData.Embedded.Towns[cobalt.Map].CanStand);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            (ch.Town, ch.X, ch.Y) = (cobalt.Map, next.X, next.Y);
            return db.SaveChanges();
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Harvest(c.Id, cobalt.Id, next, Cancel))).Status);
        api.WithDb(db =>
        {
            db.Characters.Single().Jobs = $"miner:{Crafting.XpToReach(50)}";
            return db.SaveChanges();
        });
        Gathered got = await server.Harvest(c.Id, cobalt.Id, next, Cancel);
        Assert.Equal((new ItemCount("cobalt-ore", 1), 25L + 5 * 50), (got.Item, got.Xp));
    }
}
