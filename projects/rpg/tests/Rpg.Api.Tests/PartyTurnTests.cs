using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The fight with the party that one reads, the party that holds (sprint 124, D79), with two game clients on a fake
/// clock: each hero's turn told with its end by the server's clock; a silent player's turns pass in 30, 30, then 10
/// seconds, and 30 again once it acts; the leader sends a member out; a closed game calls its trade off and leaves its
/// party after ten minutes; what one wears is seen by the map.
/// </summary>
public class PartyTurnTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

    private static readonly MonsterGroup Group = GameData.Embedded.Groups.Values.First(g => GameData.Embedded.Towns[g.Map].Dungeon is null && g.Monsters.Count == 1);

    private static Func<Uri, AuthenticationHeaderValue?, CancellationToken, Task<WebSocket>> Through(ApiFactory api) => (uri, auth, cancel) =>
    {
        var client = api.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization = auth?.ToString();
        return client.ConnectAsync(uri, cancel);
    };

    private static async Task<(GameServer Server, CharacterSummary Character, ChatClient Chat)> Player(ApiFactory api, string name)
    {
        GameServer server = await api.SignedIn("pt");
        CharacterSummary c = await server.CreateCharacter(name, "female-c", name == "Aube" ? "guard" : "mage", cancel: Cancel);
        ChatClient chat = await server.OpenChat(c.Id, Through(api), Cancel);
        var hub = api.Services.GetRequiredService<ChatHub>();
        for (int i = 0; i < 100 && hub.MapOf(c.Id) is null; i++)
            await Task.Delay(20, Cancel);
        Town map = GameData.Embedded.Towns[Group.Map];
        await server.SavePlace(c.Id, new Place(map.Id, map.Spawn.X, map.Spawn.Y), Cancel);
        return (server, c, chat);
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

    private static async Task Party(GameServer sa, CharacterSummary aube, GameServer sb, CharacterSummary brume)
    {
        await sa.Invite(aube.Id, "Brume", Cancel);
        await sb.JoinParty(brume.Id, aube.Id, Cancel);
    }

    [Fact]
    public async Task ASilentPlayersTurns_PassIn30_30_Then10Seconds_And30AgainOnceItActs()
    {
        await using var api = new ApiFactory();
        (GameServer sa, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (GameServer sb, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        // Strong enough to stand many rounds without doing anything.
        api.WithDb(db =>
        {
            foreach (Character c in db.Characters)
                (c.Xp, c.Level) = (Progression.XpToReach(100), 100);
            return db.SaveChanges();
        });
        await Party(sa, aube, sb, brume);
        SharedFightNews opened = await sa.OpenBattle(aube.Id, Group.Scenario, Cancel);
        await sb.JoinBattle(brume.Id, opened.Fight, Cancel);
        await sa.GoBattle(aube.Id, opened.Fight, Cancel);
        SharedFightNews started = (await Next(a, e => e.Shared?.Shared == SharedFightKind.Started))!.Shared!;
        Assert.NotNull(started.ServerNow);
        Fight fight = started.Start!.Build(GameData.Embedded);
        int aubeHero = fight.HeroFighters[0].Id;
        // Each hero's turn told with its length; nobody plays: Aube's turns last 30, 30, then 10 seconds, absent.
        var lengths = new List<int>();
        SharedFightNews? turn = null;
        for (int i = 0; i < 12 && lengths.Count < 3; i++)
        {
            turn = (await Next(a, e => e.Shared?.Shared == SharedFightKind.Turn))!.Shared!;
            int seconds = (int)Math.Round((turn.TurnEnds!.Value - turn.ServerNow!.Value).TotalSeconds);
            if (turn.Fighter == aubeHero)
                lengths.Add(seconds);
            api.Clock.Advance(TimeSpan.FromSeconds(seconds));
        }
        Assert.Equal([30, 30, 10], lengths);
        Assert.Contains(aubeHero, turn!.Absent!);
        // Aube plays: no longer absent, 30 seconds at its next turn.
        await sa.Act(aube.Id, opened.Fight, new EndTurnAction(), Cancel);
        for (int i = 0; i < 12; i++)
        {
            turn = (await Next(a, e => e.Shared?.Shared == SharedFightKind.Turn))!.Shared!;
            int seconds = (int)Math.Round((turn.TurnEnds!.Value - turn.ServerNow!.Value).TotalSeconds);
            if (turn.Fighter == aubeHero)
            {
                Assert.Equal(30, seconds);
                Assert.DoesNotContain(aubeHero, turn.Absent!);
                break;
            }
            api.Clock.Advance(TimeSpan.FromSeconds(seconds));
        }
        Assert.Equal(aubeHero, turn.Fighter);
        // Coming back: whose turn it is, and who is absent.
        SharedFightNews now = await sb.Battle(brume.Id, opened.Fight, Cancel);
        Assert.Equal(aubeHero, now.Fighter);
        Assert.NotNull(now.ServerNow);
        await a.DisposeAsync();
        await b.DisposeAsync();
    }

    [Fact]
    public async Task TheLeader_SendsAMemberOut_AndNobodyElseCan()
    {
        await using var api = new ApiFactory();
        (GameServer sa, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (GameServer sb, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        await Party(sa, aube, sb, brume);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => sb.KickFromParty(brume.Id, aube.Id, Cancel))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => sa.KickFromParty(aube.Id, aube.Id, Cancel))).Status);
        await sa.KickFromParty(aube.Id, brume.Id, Cancel);
        Assert.NotNull(await Next(b, e => e.Party?.Party == PartyNewsKind.Left));
        Assert.Equal(PartyNewsKind.Left, (await sb.Party(brume.Id, Cancel)).Party);
        Assert.Equal(PartyNewsKind.Left, (await sa.Party(aube.Id, Cancel)).Party);
        await a.DisposeAsync();
        await b.DisposeAsync();
    }

    [Fact]
    public async Task AGameClosed_CallsItsTradeOff_AndLeavesItsPartyAfterTenMinutes()
    {
        await using var api = new ApiFactory();
        (GameServer sa, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (GameServer sb, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        await Party(sa, aube, sb, brume);
        await sa.AskTrade(aube.Id, brume.Id, Cancel);
        await sb.OpenTrade(brume.Id, aube.Id, Cancel);
        Assert.NotNull(await Next(a, e => e.Trade?.Trade == TradeNewsKind.Opened));
        // Brume closes the game: the trade is off at once, the party waits.
        await b.DisposeAsync();
        Assert.NotNull(await Next(a, e => e.Trade?.Trade == TradeNewsKind.Cancelled));
        var parties = api.Services.GetRequiredService<Parties>();
        var hub = api.Services.GetRequiredService<ChatHub>();
        var scopes = api.Services.GetRequiredService<IServiceScopeFactory>();
        api.Clock.Advance(PartyRules.OfflineFor - TimeSpan.FromSeconds(1));
        await BattleClock.SweepParties(parties, hub, scopes, Cancel);
        Assert.NotNull(parties.Of(aube.Id));
        api.Clock.Advance(TimeSpan.FromSeconds(1));
        await BattleClock.SweepParties(parties, hub, scopes, Cancel);
        Assert.Null(parties.Of(aube.Id));
        Assert.NotNull(await Next(a, e => e.Party?.Party == PartyNewsKind.Left));
        await a.DisposeAsync();
    }

    [Fact]
    public async Task WhatOneWears_IsSeenByTheMap()
    {
        await using var api = new ApiFactory();
        (GameServer sa, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (_, _, ChatClient b) = await Player(api, "Brume");
        api.WithDb(db =>
        {
            db.Characters.First(c => c.Id == aube.Id).Receive([new ItemCount("copper-ring", 1)]);
            return db.SaveChanges();
        });
        await sa.SaveEquipment(aube.Id, new Dictionary<Slot, string> { [Slot.Ring1] = "copper-ring" }, Cancel);
        ChatEvent? seen = await Next(b, e => e.News is { Kind: MapNewsKind.Here } n && n.Character == aube.Id && n.Look?.Worn?.ContainsKey(Slot.Ring1) == true);
        Assert.NotNull(seen);
        await a.DisposeAsync();
        await b.DisposeAsync();
    }
}
