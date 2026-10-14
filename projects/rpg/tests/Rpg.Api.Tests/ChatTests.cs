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
        for (int i = 0; i < 100 && !hub.On(map).Contains(character); i++)
            await Task.Delay(20, Cancel);
    }

    [Fact]
    public async Task ALine_IsHeardOnItsMap_SenderIncluded_AndNowhereElse()
    {
        await using var api = new ApiFactory();
        (_, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (_, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        (GameServer cendreServer, CharacterSummary cendre, ChatClient c) = await Player(api, "Cendre");
        await cendreServer.SavePlace(cendre.Id, new Place("woods-4-2", 46, 5), Cancel);
        foreach ((string map, Guid who) in new[] { ("clairval", aube.Id), ("clairval", brume.Id), ("woods-4-2", cendre.Id) })
            await WaitUntilOn(api, map, who);

        Assert.True(await a.Say("  Bonjour   à tous ", Cancel));
        foreach (ChatClient heard in new[] { a, b })
        {
            ChatLine line = (await heard.Next(Wait, Cancel))!.Line!;
            Assert.Equal((ChatChannels.Map, "Aube", "Bonjour à tous"), (line.Channel, line.From, line.Text));
        }
        Assert.Null(await c.Next(Silence, Cancel));
        // Cendre walks back to the village: heard there now.
        await cendreServer.SavePlace(cendre.Id, new Place("clairval", 7, 11), Cancel);
        await b.Say("Te revoilà", Cancel);
        Assert.Equal("Te revoilà", (await c.Next(Wait, Cancel))!.Line!.Text);
        await a.DisposeAsync();
        await b.DisposeAsync();
        await c.DisposeAsync();
    }

    [Fact]
    public async Task NothingToSay_AnotherChannel_OrTooManyLines_AreRefused_ToTheSenderAlone()
    {
        await using var api = new ApiFactory();
        (_, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (_, _, ChatClient b) = await Player(api, "Brume");
        await WaitUntilOn(api, "clairval", aube.Id);
        // Sent raw: the game itself would not send these.
        Assert.False(await a.Say("   ", Cancel));
        for (int i = 0; i < ChatRules.Burst; i++)
            await a.Say($"ligne {i}", Cancel);
        for (int i = 0; i < ChatRules.Burst; i++)
            Assert.Equal($"ligne {i}", (await a.Next(Wait, Cancel))!.Line!.Text);
        await a.Say("une de trop", Cancel);
        Assert.NotNull((await a.Next(Wait, Cancel))!.Refusal);
        // Ten seconds later (the server's clock), heard again.
        api.Clock.Advance(ChatRules.Window);
        await a.Say(new string('x', 300), Cancel);
        Assert.Equal(ChatRules.MaxLength, (await a.Next(Wait, Cancel))!.Line!.Text.Length);
        // Brume heard the five lines and the long one, not the refusal.
        int lines = 0;
        while (await b.Next(Silence, Cancel) is ChatEvent e)
        {
            Assert.NotNull(e.Line);
            lines++;
        }
        Assert.Equal(ChatRules.Burst + 1, lines);
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
