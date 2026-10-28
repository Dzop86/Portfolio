using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The guild that serves (sprint 156, D83), with three game clients: the leader's message, goal and crest (seen on the
/// map), the chest at Edmée's (a deposit, a member refused a withdrawal, an officer's, a tab opened to the members, the
/// journal), the level gained from the contracts and achievements, and the playtest's counters.
/// </summary>
public class GuildHallTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

    private static Func<Uri, AuthenticationHeaderValue?, CancellationToken, Task<WebSocket>> Through(ApiFactory api) => (uri, auth, cancel) =>
    {
        var client = api.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization = auth?.ToString();
        return client.ConnectAsync(uri, cancel);
    };

    private static async Task<ChatClient> Connect(ApiFactory api, GameServer server, Guid id)
    {
        ChatClient chat = await server.OpenChat(id, Through(api), Cancel);
        var hub = api.Services.GetRequiredService<ChatHub>();
        for (int i = 0; i < 100 && hub.MapOf(id) is null; i++)
            await Task.Delay(20, Cancel);
        return chat;
    }

    private static async Task<ChatEvent?> Next(ChatClient chat, Func<ChatEvent, bool> wanted)
    {
        while (await chat.Next(Wait, Cancel) is ChatEvent e)
        {
            if (wanted(e))
                return e;
        }
        return null;
    }

    private static HttpStatusCode Status(Exception e) => ((ServerException)e).Status;

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => Status(await Assert.ThrowsAsync<ServerException>(call));

    // Aube leads, Brume is a member, Cendre an officer; all three on Clairval's square, where Edmée stands.
    private static async Task<(GameServer Sa, GameServer Sb, GameServer Sc, CharacterSummary Aube, CharacterSummary Brume, CharacterSummary Cendre, ChatClient A, ChatClient B, ChatClient C)> Guild(ApiFactory api)
    {
        GameServer sa = await api.SignedIn("gh"), sb = await api.SignedIn("gh"), sc = await api.SignedIn("gh");
        CharacterSummary aube = await sa.CreateCharacter("Aube", "female-c", "guard", cancel: Cancel);
        CharacterSummary brume = await sb.CreateCharacter("Brume", "female-c", "mage", cancel: Cancel);
        CharacterSummary cendre = await sc.CreateCharacter("Cendre", "male-c", "mage", cancel: Cancel);
        ChatClient a = await Connect(api, sa, aube.Id), b = await Connect(api, sb, brume.Id), c = await Connect(api, sc, cendre.Id);
        api.WithDb(db =>
        {
            db.Characters.Single(x => x.Name == "Aube").Shards = GuildRules.Price;
            return db.SaveChanges();
        });
        await sa.FoundGuild(aube.Id, "Les Veilleurs", Cancel);
        foreach ((GameServer s, CharacterSummary who, ChatClient chat) in new[] { (sb, brume, b), (sc, cendre, c) })
        {
            await sa.InviteToGuild(aube.Id, who.Name, Cancel);
            Assert.NotNull(await Next(chat, e => e.Guild?.Guild == GuildNewsKind.Invited));
            await s.JoinGuild(who.Id, aube.Id, Cancel);
        }
        await sa.SetGuildRank(aube.Id, cendre.Id, GuildRank.Officer, Cancel);
        return (sa, sb, sc, aube, brume, cendre, a, b, c);
    }

    [Fact]
    public async Task TheLeader_WritesTheMessage_TheGoal_AndTheCrest_SeenByTheMembers_AndOnTheMap()
    {
        await using var api = new ApiFactory();
        var (sa, sb, sc, aube, brume, cendre, a, b, c) = await Guild(api);
        GuildStanding fresh = (await sb.Guild(brume.Id, Cancel)).View!.Standing!;
        Assert.Equal((1, 0L, GuildRules.DefaultGoal, GuildCrests.Default), (fresh.Level, fresh.Points, fresh.Goal, fresh.Crest));
        // An officer may not; the leader may.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sc.SetGuildNotice(cendre.Id, "Ce soir, la Combe !", 200, Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sa.SetGuildNotice(aube.Id, "Ce soir, la Combe !", 0, Cancel)));
        await sa.SetGuildNotice(aube.Id, "Ce soir, la Combe !", 200, Cancel);
        GuildStanding told = (await Next(b, e => e.Guild?.View?.Standing?.Message is not null))!.Guild!.View!.Standing!;
        Assert.Equal(("Ce soir, la Combe !", 200), (told.Message, told.Goal));
        // The crest: two colours alike refused; a wolf on grey, told to the members and seen beside Aube's guild on the map.
        var wolf = new GuildCrest("banner", "wolf-head", "gris-fond", "osmose-cost");
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sa.SetGuildCrest(aube.Id, wolf with { Charge = "gris-fond" }, Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sb.SetGuildCrest(brume.Id, wolf, Cancel)));
        await sa.SetGuildCrest(aube.Id, wolf, Cancel);
        Assert.NotNull(await Next(c, e => e.Guild?.View?.Standing?.Crest == wolf));
        Assert.Equal(GuildCrests.Write(wolf), (await Next(b, e => e.News is { Kind: MapNewsKind.Here, Name: "Aube", Crest: not null }))!.News!.Crest);
        await a.DisposeAsync();
        await b.DisposeAsync();
        await c.DisposeAsync();
    }

    [Fact]
    public async Task TheChest_AtEdmees_EveryoneDeposits_AMemberIsRefused_UntilTheLeaderOpensTheTab_AndTheJournalSaysWho()
    {
        await using var api = new ApiFactory();
        var (sa, sb, sc, aube, brume, cendre, a, b, c) = await Guild(api);
        api.WithDb(db =>
        {
            db.Characters.Single(x => x.Name == "Brume").Receive([new ItemCount("oak-log", 12)]);
            return db.SaveChanges();
        });
        ChestView empty = await sb.GuildChest(brume.Id, Cancel);
        Assert.Equal((1, GuildRank.Member), (empty.Chest.Tabs.Count, empty.Rank));
        // Brume deposits five logs: the bag and the chest say so.
        ChestView put = await sb.Deposit(brume.Id, "oak-log", 5, 0, Cancel);
        Assert.Contains(new ItemCount("oak-log", 7), put.Character!.Inventory!);
        Assert.Equal([new ItemCount("oak-log", 5)], put.Chest.Tabs[0].Items);
        // More than the bag holds, a tab the guild has not: refused.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sb.Deposit(brume.Id, "oak-log", 8, 0, Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sb.Deposit(brume.Id, "oak-log", 1, 1, Cancel)));
        // A member takes nothing out of a closed tab; the officer does.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sb.Withdraw(brume.Id, "oak-log", 1, 0, Cancel)));
        ChestView taken = await sc.Withdraw(cendre.Id, "oak-log", 2, 0, Cancel);
        Assert.Contains(new ItemCount("oak-log", 2), taken.Character!.Inventory!);
        // Only the leader opens the tab; then the member takes out of it.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sc.OpenChestTab(cendre.Id, 0, true, Cancel)));
        await sa.OpenChestTab(aube.Id, 0, true, Cancel);
        ChestView mine = await sb.Withdraw(brume.Id, "oak-log", 3, 0, Cancel);
        Assert.Empty(mine.Chest.Tabs[0].Items);
        Assert.Equal([("Brume", -3), ("Cendre", -2), ("Brume", 5)], mine.Chest.Journal.Select(m => (m.Who, m.Count)));
        // Away from Edmée's: refused.
        Town woods = GameData.Embedded.Towns.Values.First(t => t.Zone is not null && !Shop.InVillage(t.Id) && GameData.Embedded.QuestToReach(t.Id) is null);
        await sb.SavePlace(brume.Id, new Place(woods.Id, woods.Spawn.X, woods.Spawn.Y), Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sb.Deposit(brume.Id, "oak-log", 1, 0, Cancel)));
        await a.DisposeAsync();
        await b.DisposeAsync();
        await c.DisposeAsync();
    }

    [Fact]
    public async Task AContract_AndAnAchievement_BringTheGuildItsPoints_AndItsLevel_ItsSecondTabAtThree()
    {
        await using var api = new ApiFactory();
        var (sa, sb, sc, aube, brume, cendre, a, b, c) = await Guild(api);
        // 440 points so far: a contract handed back by Brume (10), then an achievement of Cendre's (5) at the next look.
        Guid guild = api.WithDb(db => db.Guilds.Single().Id);
        api.WithDb(db =>
        {
            db.Guilds.Single().Points = 440;
            Character brumeRow = db.Characters.Single(x => x.Name == "Brume");
            brumeRow.Receive([new ItemCount("oak-log", 10)]);
            // At Clairval's notice board.
            (brumeRow.Town, brumeRow.X, brumeRow.Y) = ("clairval", 24, 16);
            brumeRow.Contracts = Contracts.Write(new ContractBoard(Contracts.Day(api.Clock.GetUtcNow()), 0, [new Contract(ContractKind.Gather, "oak-log", 10, 1)]));
            return db.SaveChanges();
        });
        await sb.HandInContract(brume.Id, 0, Cancel);
        GuildStanding after = (await Next(a, e => e.Guild?.View?.Standing?.Points == 450))!.Guild!.View!.Standing!;
        Assert.Equal((3, 0L, 450L, 10), (after.Level, after.Into, after.ToNext, after.Week));
        Assert.Equal(2, (await sa.GuildChest(aube.Id, Cancel)).Chest.Tabs.Count);
        // Cendre trades once: an achievement, counted for the guild when its window looks, and only once.
        api.WithDb(db =>
        {
            db.Characters.Single(x => x.Name == "Cendre").Feat(Achievements.Traded);
            return db.SaveChanges();
        });
        await sc.Achievements(cendre.Id, Cancel);
        await sc.Achievements(cendre.Id, Cancel);
        Assert.Equal(455, api.WithDb(db => db.Guilds.Single(g => g.Id == guild).Points));
        // A new week: the week's points start again, the level stays.
        api.Clock.Advance(TimeSpan.FromDays(7));
        GuildStanding next = (await sb.Guild(brume.Id, Cancel)).View!.Standing!;
        Assert.Equal((3, 0), (next.Level, next.Week));
        await a.DisposeAsync();
        await b.DisposeAsync();
        await c.DisposeAsync();
    }

    [Fact]
    public async Task ThePlaytestsCounters_SayTheDaysPlayed_TheTime_AndTheContracts_WithoutAName()
    {
        await using var api = new ApiFactory();
        // An hour before dawn: two evenings of ninety minutes, one each side of it, the token still good.
        DateTimeOffset now = api.Clock.GetUtcNow();
        DateTimeOffset dawn = DateTimeOffset.FromUnixTimeSeconds((Contracts.Day(now) + 1) * 86_400L) + Contracts.Dawn;
        api.Clock.Advance(dawn - now - TimeSpan.FromHours(1));
        GameServer server = await api.SignedIn("compteurs");
        CharacterSummary ronce = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        await server.Contracts(ronce.Id, Cancel);
        var hub = api.Services.GetRequiredService<ChatHub>();
        for (int evening = 0; evening < 2; evening++)
        {
            ChatClient chat = await Connect(api, server, ronce.Id);
            api.Clock.Advance(TimeSpan.FromMinutes(90));
            await chat.DisposeAsync();
            for (int i = 0; i < 100 && hub.MapOf(ronce.Id) is not null; i++)
                await Task.Delay(20, Cancel);
        }
        for (int i = 0; i < 100 && api.WithDb(db => db.Characters.Single().SecondsPlayed) < 2 * 5400; i++)
            await Task.Delay(20, Cancel);
        using IServiceScope scope = api.Services.CreateScope();
        IReadOnlyList<string> lines = await Admin.Counters(scope.ServiceProvider.GetRequiredService<GameDb>(), Cancel);
        Assert.Equal(["#1 level 1 guard: 2 day(s), 3 h 00 min, contracts 0 of 3 (0 %)"], lines);
        Assert.DoesNotContain(lines, l => l.Contains("Ronce", StringComparison.Ordinal) || l.Contains("compteurs", StringComparison.Ordinal));
    }
}
