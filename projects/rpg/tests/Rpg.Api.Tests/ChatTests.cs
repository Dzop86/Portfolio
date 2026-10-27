using System.Net.Http.Headers;
using System.Net.WebSockets;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The proximity chat on the server (sprint 84): heard on the same map only, cleaned, paced.</summary>
public class ChatTests
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
        GameServer server = await api.SignedIn("ch");
        CharacterSummary c = await server.CreateCharacter(name, "female-c", "mage", cancel: Cancel);
        return (server, c, await server.OpenChat(c.Id, Through(api), Cancel));
    }

    private static async Task WaitUntilOn(ApiFactory api, string map, Guid character)
    {
        var hub = (ChatHub)api.Services.GetService(typeof(ChatHub))!;
        for (int i = 0; i < 250 && !hub.On(map).Contains(character); i++)
            await Task.Delay(20, Cancel);
        // Said, not passed over: a test that goes on without its player on the map fails further, for another reason.
        Assert.Contains(character, hub.On(map));
    }

    // The next line or refusal, the map's news passed over (sprint 118: the same socket tells who is here).
    private static async Task<ChatEvent?> Heard(ChatClient chat, TimeSpan within, CancellationToken cancel)
    {
        while (await chat.Next(within, cancel) is ChatEvent e)
        {
            if (e.News is null)
                return e;
        }
        return null;
    }

    [Fact]
    public async Task ALine_IsHeardOnItsMap_SenderIncluded_AndNowhereElse()
    {
        await using var api = new ApiFactory();
        (_, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (_, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        (GameServer cendreServer, CharacterSummary cendre, ChatClient c) = await Player(api, "Cendre");
        // In play on the place first: a place saved before its socket is served would be read again from the base too late.
        await WaitUntilOn(api, "clairval", cendre.Id);
        await cendreServer.SavePlace(cendre.Id, new Place("woods-4-2", 46, 5), Cancel);
        foreach ((string map, Guid who) in new[] { ("clairval", aube.Id), ("clairval", brume.Id), ("woods-4-2", cendre.Id) })
            await WaitUntilOn(api, map, who);

        Assert.True(await a.Say("  Bonjour   à tous ", Cancel));
        foreach (ChatClient heard in new[] { a, b })
        {
            ChatLine line = (await Heard(heard, Wait, Cancel))!.Line!;
            Assert.Equal((ChatChannels.Map, "Aube", "Bonjour à tous"), (line.Channel, line.From, line.Text));
        }
        Assert.Null(await Heard(c, Silence, Cancel));
        // Cendre walks back to the village: heard there now.
        await cendreServer.SavePlace(cendre.Id, new Place("clairval", 7, 11), Cancel);
        await b.Say("Te revoilà", Cancel);
        Assert.Equal("Te revoilà", (await Heard(c, Wait, Cancel))!.Line!.Text);
        await a.DisposeAsync();
        await b.DisposeAsync();
        await c.DisposeAsync();
    }

    [Fact]
    public async Task NothingToSay_AnotherChannel_OrTooManyLines_AreRefused_ToTheSenderAlone()
    {
        await using var api = new ApiFactory();
        (_, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (_, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        // Both on the place before Aube speaks: since sprint 145 a socket opening also writes its last connection.
        foreach (Guid who in new[] { aube.Id, brume.Id })
            await WaitUntilOn(api, "clairval", who);
        // Sent raw: the game itself would not send these.
        Assert.False(await a.Say("   ", Cancel));
        for (int i = 0; i < ChatRules.Burst; i++)
            await a.Say($"ligne {i}", Cancel);
        for (int i = 0; i < ChatRules.Burst; i++)
            Assert.Equal($"ligne {i}", (await Heard(a, Wait, Cancel))!.Line!.Text);
        await a.Say("une de trop", Cancel);
        Assert.NotNull((await Heard(a, Wait, Cancel))!.Refusal);
        // Ten seconds later (the server's clock), heard again.
        api.Clock.Advance(ChatRules.Window);
        await a.Say(new string('x', 300), Cancel);
        Assert.Equal(ChatRules.MaxLength, (await Heard(a, Wait, Cancel))!.Line!.Text.Length);
        // Brume heard the five lines and the long one, not the refusal.
        int lines = 0;
        while (await Heard(b, Silence, Cancel) is ChatEvent e)
        {
            Assert.NotNull(e.Line);
            lines++;
        }
        Assert.Equal(ChatRules.Burst + 1, lines);
        await a.DisposeAsync();
        await b.DisposeAsync();
    }

    [Fact]
    public async Task AnItemShared_IsHeardWithItsLink_AnUnknownItemOrTooMany_AreRefused()
    {
        // Sprint 117 (D77): "[item:copper-ring]" in a line, clickable in the game.
        await using var api = new ApiFactory();
        (_, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (_, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        // Both on the place before Aube speaks: since sprint 145 a socket opening also writes its last connection.
        foreach (Guid who in new[] { aube.Id, brume.Id })
            await WaitUntilOn(api, "clairval", who);
        string shared = $"Regardez {ChatLinks.Of("copper-ring")} !";
        await a.Say(shared, Cancel);
        Assert.Equal(shared, (await Heard(a, Wait, Cancel))!.Line!.Text);
        Assert.Equal(shared, (await Heard(b, Wait, Cancel))!.Line!.Text);
        await a.Say($"{ChatLinks.Of("no-such-thing")}", Cancel);
        Assert.Contains("no-such-thing", (await Heard(a, Wait, Cancel))!.Refusal, StringComparison.Ordinal);
        await a.Say(string.Concat(Enumerable.Repeat(ChatLinks.Of("bread"), ChatLinks.Most + 1)), Cancel);
        Assert.NotNull((await Heard(a, Wait, Cancel))!.Refusal);
        // Brume heard the shared ring only.
        Assert.Null(await Heard(b, Silence, Cancel));
        await a.DisposeAsync();
        await b.DisposeAsync();
    }

    [Fact]
    public async Task SomeoneElsesCharacter_HasNoChat()
    {
        await using var api = new ApiFactory();
        (_, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        GameServer other = await api.SignedIn("ot");
        await Assert.ThrowsAnyAsync<Exception>(() => other.OpenChat(aube.Id, Through(api), Cancel));
        await a.DisposeAsync();
    }
}
