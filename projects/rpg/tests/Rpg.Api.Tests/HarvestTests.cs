using System.Net;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>Gathering on the server (sprint 99, D71): the trade's level, the place, the spot ready, the resource and the experience.</summary>
public class HarvestTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => (await Assert.ThrowsAsync<ServerException>(call)).Status;

    [Fact]
    public async Task ASpot_IsGatheredNextToIt_AtTheTradesLevel_ThenGrowsBack_ForEveryone()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("har");
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        HarvestNode oak = GameData.Embedded.Nodes.Values.First(n => n.Level == 1 && n.Kind == NodeKind.Tree);
        HarvestNode? hazel = GameData.Embedded.Nodes.Values.FirstOrDefault(n => n.Level == 5 && n.Map == oak.Map);
        Cell next = oak.At.Neighbours().First(GameData.Embedded.Towns[oak.Map].CanStand);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            (ch.Town, ch.X, ch.Y) = (oak.Map, next.X, next.Y);
            return db.SaveChanges();
        });
        // Too far, or a level-5 resource at level 1: refused.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.Harvest(c.Id, oak.Id, GameData.Embedded.Towns[oak.Map].Spawn, Cancel)));
        Gathered got = await server.Harvest(c.Id, oak.Id, next, Cancel);
        Assert.Equal((new ItemCount(oak.Item, 1), 30L), (got.Item, got.Xp));
        Assert.Equal(30L, got.Character.Jobs!["woodcutter"]);
        Assert.Contains(new ItemCount(oak.Item, 1), got.Character.Inventory!);
        Assert.Contains(oak.Id, (await server.Growing(c.Id, Cancel)).Spots.Keys);
        // Growing back: refused, even after the gathering's three seconds.
        api.Clock.Advance(TimeSpan.FromSeconds(4));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.Harvest(c.Id, oak.Id, next, Cancel)));
        api.Clock.Advance(Harvesting.Regrowth(oak.Level));
        Assert.Equal(60L, (await server.Harvest(c.Id, oak.Id, next, Cancel)).Character.Jobs!["woodcutter"]);
        if (hazel is not null)
        {
            Cell nearHazel = hazel.At.Neighbours().First(GameData.Embedded.Towns[hazel.Map].CanStand);
            api.Clock.Advance(TimeSpan.FromSeconds(4));
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.Harvest(c.Id, hazel.Id, nearHazel, Cancel)));
        }
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.Harvest(c.Id, "no-such-spot", next, Cancel)));
    }
}
