using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The dungeons on the server (sprint 80): the key used up, the rooms in turn, the way out.</summary>
public class DungeonTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task Give(ApiFactory api, string account, int level, params ItemCount[] items)
    {
        using IServiceScope scope = api.Services.CreateScope();
        await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, level, items), Cancel);
    }

    /// <summary>Plays the room the server allows, the AI choosing for the hero, and reports it.</summary>
    private static async Task<(FightResult Result, Fight Played)> Fight(GameServer server, CharacterSummary c, string scenario)
    {
        FightTicket ticket = await server.StartFight(c.Id, scenario, Cancel);
        var fight = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, (await server.Characters(Cancel)).Single(x => x.Id == c.Id).Hero);
        Ai.PlayOut(fight);
        return (await server.ReportFight(c.Id, ticket.Id, FightRecord.Of(fight), Cancel), fight);
    }

    [Fact]
    public async Task TheKey_OpensTheDungeon_IsUsedUp_AndTheRoomsComeInTurn_ToTheWayOut()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("dun");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        Dungeon wallow = GameData.Embedded.Dungeons["boar-wallow"];
        // Without its key, the door stays shut.
        var shut = await Assert.ThrowsAsync<ServerException>(() => server.EnterDungeon(c.Id, wallow.Id, Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, shut.Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.EnterDungeon(c.Id, "nowhere", Cancel))).Status);
        // Nor can a room be fought from outside.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.StartFight(c.Id, wallow.Scenario(1), Cancel))).Status);

        // Strong enough to win every room, with two keys.
        await Give(api, account, 60, new ItemCount(wallow.Key, 2));
        CharacterSummary inside = await server.EnterDungeon(c.Id, wallow.Id, Cancel);
        Assert.Equal(new DungeonRun(wallow.Id, 1), inside.Run);
        Assert.Equal([new ItemCount(wallow.Key, 1)], inside.Inventory!);
        // Going in again while there: the same room, no key.
        Assert.Equal(inside, await server.EnterDungeon(c.Id, wallow.Id, Cancel));
        // The rooms in turn: the second cannot be fought before the first.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.StartFight(c.Id, wallow.Scenario(2), Cancel))).Status);
        for (int room = 1; room <= Dungeons.RoomCount; room++)
        {
            (FightResult result, Fight played) = await Fight(server, c, wallow.Scenario(room));
            Assert.Equal(0, played.WinningTeam);
            Assert.Equal(room == 3, result.DungeonDone);
            Assert.Equal(room == 3 ? null : new DungeonRun(wallow.Id, room + 1), result.Run);
            // The last room's experience comes twice.
            Assert.Equal(Progression.FightXp(played) * (room == 3 ? 2 : 1), result.Xp);
        }
        Assert.Null((await server.Characters(Cancel)).Single().Run);
        // Out of it, its rooms are shut again.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.StartFight(c.Id, wallow.Scenario(3), Cancel))).Status);
    }

    [Fact]
    public async Task ALostRoom_OrLeaving_TakesOut_AndTheKeyIsGone()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("dun");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "mage", cancel: Cancel);
        Dungeon reef = GameData.Embedded.Dungeons["crab-reef"];
        // A level 1 hero against crabs of level 17: lost.
        await Give(api, account, 1, new ItemCount(reef.Key, 2));
        await server.EnterDungeon(c.Id, reef.Id, Cancel);
        (FightResult lost, Fight played) = await Fight(server, c, reef.Scenario(1));
        Assert.Equal(1, played.WinningTeam);
        Assert.Null(lost.Run);
        Assert.False(lost.DungeonDone);
        CharacterSummary after = (await server.Characters(Cancel)).Single();
        Assert.Null(after.Run);
        Assert.Equal([new ItemCount(reef.Key, 1)], after.Inventory!);
        // The second key, then leaving: out, the key not given back.
        Assert.Equal(new DungeonRun(reef.Id, 1), (await server.EnterDungeon(c.Id, reef.Id, Cancel)).Run);
        await server.LeaveDungeon(c.Id, Cancel);
        after = (await server.Characters(Cancel)).Single();
        Assert.Null(after.Run);
        Assert.Empty(after.Inventory!);
    }

    [Fact]
    public async Task TheKeyring_OpensEveryDungeon_AndStays()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("dun");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "sentinel", cancel: Cancel);
        await Give(api, account, 100, new ItemCount(Dungeons.Keyring, 1));
        foreach (Dungeon d in GameData.Embedded.Dungeons.Values)
        {
            CharacterSummary inside = await server.EnterDungeon(c.Id, d.Id, Cancel);
            Assert.Equal(new DungeonRun(d.Id, 1), inside.Run);
            Assert.Equal([new ItemCount(Dungeons.Keyring, 1)], inside.Inventory!);
        }
        // Someone else's character: unknown.
        GameServer other = await api.SignedIn("oth");
        Assert.Equal(HttpStatusCode.NotFound, (await Assert.ThrowsAsync<ServerException>(() => other.EnterDungeon(c.Id, "boar-wallow", Cancel))).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Assert.ThrowsAsync<ServerException>(() => other.LeaveDungeon(c.Id, Cancel))).Status);
    }
}
