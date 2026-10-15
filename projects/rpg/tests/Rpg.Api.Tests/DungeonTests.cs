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

    /// <summary>Plays the room the server allows, the AI choosing for the hero, and reports it; the hit points kept from the room before unless given.</summary>
    private static async Task<(FightResult Result, Fight Played)> Fight(GameServer server, CharacterSummary c, string scenario, int? hp = -1)
    {
        FightTicket ticket = await server.StartFight(c.Id, scenario, Cancel);
        CharacterSummary now = (await server.Characters(Cancel)).Single(x => x.Id == c.Id);
        var fight = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, now.Hero, heroHp: hp == -1 ? ticket.HeroHp : hp);
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

        // Strong enough to win every room, with two keys: at level 80, without points, 1,000 wins out of 1,000
        // against the boar king since he lights his shard (sprint 91); 81 % at level 60.
        await Give(api, account, 80, new ItemCount(wallow.Key, 2));
        CharacterSummary inside = await server.EnterDungeon(c.Id, wallow.Id, Cancel);
        Assert.Equal(new DungeonRun(wallow.Id, 1), inside.Run);
        Assert.Equal([new ItemCount(wallow.Key, 1)], inside.Inventory!);
        // Going in again while there: the same room, no key.
        Assert.Equal(inside, await server.EnterDungeon(c.Id, wallow.Id, Cancel));
        // The rooms in turn: the second cannot be fought before the first.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.StartFight(c.Id, wallow.Scenario(2), Cancel))).Status);
        for (int room = 1; room <= Dungeons.RoomCount; room++)
        {
            // At the fountain of the hall first (sprint 86): the hit points of the room before are kept otherwise.
            if (room > 1)
                await server.Rest(c.Id, Cancel);
            (FightResult result, Fight played) = await Fight(server, c, wallow.Scenario(room));
            Assert.Equal(0, played.WinningTeam);
            Assert.Equal(room == 3, result.DungeonDone);
            Assert.Equal(room == 3 ? null : $"{wallow.Id} {room + 1}", result.Run is { } run ? $"{run.Dungeon} {run.Room}" : null);
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

    [Fact]
    public async Task AWonRoom_KeepsTheHitPointsLeft_TheFountainHealsThem_AndTheHallsAreForWhoIsThere()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("dun");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        Dungeon wallow = GameData.Embedded.Dungeons["boar-wallow"];
        await Give(api, account, 20, new ItemCount(wallow.Key, 1));
        // Outside, neither a hall nor the fountain.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePlace(c.Id, new Place(wallow.Halls![0].Map, 10, 11), Cancel))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Rest(c.Id, Cancel))).Status);
        await server.EnterDungeon(c.Id, wallow.Id, Cancel);
        // In, the first hall; not the second before its room.
        await server.SavePlace(c.Id, new Place(wallow.Halls![0].Map, 10, 11), Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePlace(c.Id, new Place(wallow.Halls[1].Map, 10, 11), Cancel))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePlace(c.Id, new Place(GameData.Embedded.Dungeons["wolf-den"].Halls![0].Map, 10, 11), Cancel))).Status);

        (FightResult first, Fight played) = await Fight(server, c, wallow.Scenario(1));
        Assert.Equal(0, played.WinningTeam);
        Fighter hero = played.HeroFighter!;
        Assert.True(hero.Hp < hero.MaxHp);
        Assert.Equal(new DungeonRun(wallow.Id, 2), first.Run);
        // The hit points left are the character's life now (sprint 101).
        Assert.Equal(hero.Hp, (await server.Characters(Cancel)).Single().LifeAt(api.Clock.GetUtcNow(), GameData.Embedded));
        await server.SavePlace(c.Id, new Place(wallow.Halls[1].Map, 10, 11), Cancel);
        // The next room with all the hit points: refused, the ticket used up.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => Fight(server, c, wallow.Scenario(2), hp: null))).Status);
        // The fountain: all of them, the same room.
        CharacterSummary rested = await server.Rest(c.Id, Cancel);
        Assert.Equal(new DungeonRun(wallow.Id, 2), rested.Run);
        // Then with what it has (all): played, and counted. The server draws the seed: a guard of level 20
        // without points wins this room about 99 times in 100, not always.
        (FightResult second, Fight secondPlayed) = await Fight(server, c, wallow.Scenario(2));
        Assert.Equal(secondPlayed.WinningTeam == 0, second.Xp > 0);
        // A lost or a finished run forgets them.
        await server.LeaveDungeon(c.Id, Cancel);
        Assert.Null((await server.Characters(Cancel)).Single().Run);
    }
}
