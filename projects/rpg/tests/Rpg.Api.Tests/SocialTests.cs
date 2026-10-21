using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// Playing together (sprint 119, D74), with game clients on the server's sockets: a party formed by invitation, its
/// channel heard by its members only, left; a trade between two players of a map, offered, accepted, made all at once.
/// </summary>
public class SocialTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5), Silence = TimeSpan.FromMilliseconds(400);

    private static Func<Uri, AuthenticationHeaderValue?, CancellationToken, Task<WebSocket>> Through(ApiFactory api) => (uri, auth, cancel) =>
    {
        var client = api.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization = auth?.ToString();
        return client.ConnectAsync(uri, cancel);
    };

    private static async Task<(GameServer Server, CharacterSummary Character, ChatClient Chat)> Player(ApiFactory api, string name)
    {
        GameServer server = await api.SignedIn("so");
        CharacterSummary c = await server.CreateCharacter(name, "female-c", "mage", cancel: Cancel);
        ChatClient chat = await server.OpenChat(c.Id, Through(api), Cancel);
        var hub = (ChatHub)api.Services.GetService(typeof(ChatHub))!;
        for (int i = 0; i < 100 && hub.MapOf(c.Id) is null; i++)
            await Task.Delay(20, Cancel);
        return (server, c, chat);
    }

    private static async Task<T?> Next<T>(ChatClient chat, Func<ChatEvent, T?> pick) where T : class
    {
        while (await chat.Next(Wait, Cancel) is ChatEvent e)
        {
            if (pick(e) is T found)
                return found;
        }
        return null;
    }

    private static async Task<bool> NoLine(ChatClient chat)
    {
        while (await chat.Next(Silence, Cancel) is ChatEvent e)
        {
            if (e.Line is not null)
                return false;
        }
        return true;
    }

    private static Task<HttpStatusCode> Refused(Func<Task> call) => Assert.ThrowsAsync<ServerException>(call).ContinueWith(t => t.Result.Status, TaskScheduler.Default);

    [Fact]
    public async Task AParty_IsFormedByInvitation_ItsChannelHeardByItsMembersOnly_AndLeft()
    {
        await using var api = new ApiFactory();
        (GameServer sa, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (GameServer sb, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        (GameServer sc, CharacterSummary cendre, ChatClient c) = await Player(api, "Cendre");
        // Aube invites Brume by her name; Brume is told and joins: both see the party, Aube leading.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sa.Invite(aube.Id, "Aube", Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sa.Invite(aube.Id, "Personne", Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sb.JoinParty(brume.Id, aube.Id, Cancel)));
        await sa.Invite(aube.Id, "Brume", Cancel);
        PartyNews? invited = await Next(b, e => e.Party);
        Assert.Equal((PartyNewsKind.Invited, (Guid?)aube.Id, "Aube"), (invited?.Party, invited?.From, invited?.Name));
        await sb.JoinParty(brume.Id, aube.Id, Cancel);
        foreach (ChatClient member in new[] { a, b })
        {
            PartyNews? party = await Next(member, e => e.Party?.Party == PartyNewsKind.Changed ? e.Party : null);
            Assert.Equal(aube.Id, party?.Leader);
            Assert.Equal(["Aube", "Brume"], party?.Members?.Select(m => m.Name));
            Assert.Equal("clairval", party?.Members?[1].Map);
        }
        Assert.Equal(PartyNewsKind.Left, (await sc.Party(cendre.Id, Cancel)).Party);
        Assert.Equal(["Aube", "Brume"], (await sb.Party(brume.Id, Cancel)).Members?.Select(m => m.Name));
        // Only the leader invites.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sb.Invite(brume.Id, "Cendre", Cancel)));
        // The party's channel: Brume hears it, Cendre (on the same map) does not; Cendre has no party to talk to.
        await a.Say("On y va ?", ChatChannels.Party, Cancel);
        ChatLine? heard = await Next(b, e => e.Line);
        Assert.Equal((ChatChannels.Party, "Aube", "On y va ?"), (heard?.Channel, heard?.From, heard?.Text));
        Assert.True(await NoLine(c));
        await c.Say("Et moi ?", ChatChannels.Party, Cancel);
        Assert.NotNull(await Next(c, e => e.Refusal));
        // Aube leaves: alone, Brume has no party any more.
        await sa.LeaveParty(aube.Id, Cancel);
        Assert.Equal(PartyNewsKind.Left, (await Next(b, e => e.Party))?.Party);
        Assert.Equal(PartyNewsKind.Left, (await sb.Party(brume.Id, Cancel)).Party);
        foreach (ChatClient chat in new[] { a, b, c })
            await chat.DisposeAsync();
    }

    [Fact]
    public async Task ATrade_IsOffered_Accepted_AndMadeAllAtOnce_OrCalledOff()
    {
        await using var api = new ApiFactory();
        (GameServer sa, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (GameServer sb, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        api.WithDb(db =>
        {
            Character ca = db.Characters.Single(x => x.Id == aube.Id), cb = db.Characters.Single(x => x.Id == brume.Id);
            ca.Receive([new ItemCount("bread", 3)]);
            cb.Shards = 50;
            return db.SaveChanges();
        });
        // Brume is asked, and opens the trade: both see it.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sb.OpenTrade(brume.Id, aube.Id, Cancel)));
        await sa.AskTrade(aube.Id, brume.Id, Cancel);
        Assert.Equal((TradeNewsKind.Asked, (Guid?)aube.Id), (await Next(b, e => e.Trade)) is TradeNews asked ? (asked.Trade, asked.From) : default);
        await sb.OpenTrade(brume.Id, aube.Id, Cancel);
        Assert.Equal("Brume", (await Next(a, e => e.Trade))?.View?.WithName);
        Assert.Equal("Aube", (await Next(b, e => e.Trade))?.View?.WithName);
        // More bread than she has: refused; two breads, and thirty Éclats from Brume.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sa.Offer(aube.Id, new TradeOffer([new("bread", 4)]), Cancel)));
        await sa.Offer(aube.Id, new TradeOffer([new("bread", 2)]), Cancel);
        await sb.Offer(brume.Id, new TradeOffer([], 30), Cancel);
        await sa.ReadyTrade(aube.Id, Cancel);
        // Brume changes her offer: Aube has to accept again.
        await sb.Offer(brume.Id, new TradeOffer([], 40), Cancel);
        await sb.ReadyTrade(brume.Id, Cancel);
        TradeView? seen = null;
        while (await Next(a, e => e.Trade) is TradeNews t && t.Trade == TradeNewsKind.Changed)
        {
            seen = t.View;
            if (seen is { TheirsReady: true })
                break;
        }
        Assert.Equal((40L, false, true), (seen!.Theirs.Shards, seen.MineReady, seen.TheirsReady));
        CharacterSummary after = await sa.ReadyTrade(aube.Id, Cancel);
        Assert.Equal(TradeNewsKind.Done, (await Next(b, e => e.Trade?.Trade == TradeNewsKind.Done ? e.Trade : null))?.Trade);
        Assert.Contains(new ItemCount("bread", 1), after.Inventory!);
        Assert.Equal(aube.Shards + 40, after.Shards);
        api.WithDb(db =>
        {
            Character cb = db.Characters.Single(x => x.Id == brume.Id);
            Assert.Contains(new ItemCount("bread", 2), cb.InventoryList);
            Assert.Equal(10L, cb.Shards);
            return 0;
        });
        // Another one, called off: Brume is told.
        await sa.AskTrade(aube.Id, brume.Id, Cancel);
        await sb.OpenTrade(brume.Id, aube.Id, Cancel);
        await sa.CancelTrade(aube.Id, Cancel);
        Assert.NotNull(await Next(b, e => e.Trade?.Trade == TradeNewsKind.Cancelled ? e.Trade : null));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => sb.ReadyTrade(brume.Id, Cancel)));
        await a.DisposeAsync();
        await b.DisposeAsync();
    }
}
