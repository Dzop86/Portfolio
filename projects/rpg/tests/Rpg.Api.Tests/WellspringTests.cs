using System.Net;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>Wellspring Vale by the API (sprint 233): past the mines, its waystone discovered, a spot of level 65 gathered.</summary>
public class WellspringTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APlaceInTheVale_NeedsFourTrails_ItsWaystoneIsDiscovered_AndItsSpotsGivePastLevelSixtyFive()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("val");
        CharacterSummary c = await server.CreateCharacter("Galène", "female-c", "warrior", cancel: Cancel);
        Town entry = GameData.Embedded.Towns["wellspring-2-0"];
        var arrival = new Place(entry.Id, entry.Spawn.X, entry.Spawn.Y);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePlace(c.Id, arrival, Cancel))).Status);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            ch.Quests = "the-disjoint,four-trails";
            ch.Jobs = $"farmer:{Crafting.XpToReach(64)}";
            return db.SaveChanges();
        });
        await server.SavePlace(c.Id, arrival, Cancel);
        Assert.Contains("waystone-wellspring", (await server.Waystones(c.Id, Cancel)).Known);
        HarvestNode maize = GameData.Embedded.Nodes.Values.First(n => n.Item == "maize");
        Cell next = maize.At.Neighbours().First(GameData.Embedded.Towns[maize.Map].CanStand);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            (ch.Town, ch.X, ch.Y) = (maize.Map, next.X, next.Y);
            return db.SaveChanges();
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Harvest(c.Id, maize.Id, next, Cancel))).Status);
        api.WithDb(db =>
        {
            db.Characters.Single().Jobs = $"farmer:{Crafting.XpToReach(65)}";
            return db.SaveChanges();
        });
        Gathered got = await server.Harvest(c.Id, maize.Id, next, Cancel);
        Assert.Equal((new ItemCount("maize", 1), 25L + 5 * 65), (got.Item, got.Xp));
    }
}
