using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// Friends, ignored characters and whispers (sprint 126, D79), with two game clients on two maps: a friend listed with
/// its zone, a whisper heard wherever one is, a friend coming into play told; one ignored can neither whisper, nor
/// invite, nor trade, and is told nothing more than "not in play".
/// </summary>
public class ContactTests
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

    [Fact]
    public async Task AFriend_IsListedWithItsZone_HeardWhereverOneIs_AndToldComingIntoPlay()
    {
        await using var api = new ApiFactory();
        GameServer sa = await api.SignedIn("ct"), sb = await api.SignedIn("ct");
        CharacterSummary aube = await sa.CreateCharacter("Aube", "female-c", "guard", cancel: Cancel);
        CharacterSummary brume = await sb.CreateCharacter("Brume", "female-c", "mage", cancel: Cancel);
        ChatClient a = await Connect(api, sa, aube.Id), b = await Connect(api, sb, brume.Id);
        Town woods = GameData.Embedded.Towns.Values.First(t => t.Zone is not null && !Shop.InVillage(t.Id));
        await sb.SavePlace(brume.Id, new Place(woods.Id, woods.Spawn.X, woods.Spawn.Y), Cancel);
        // A friend: its zone, not its map; oneself and a name that does not exist refused.
        ContactList list = await sa.AddContact(aube.Id, "Brume", cancel: Cancel);
        Assert.Equal(("Brume", woods.Zone), (list.Friends.Single().Name, list.Friends.Single().Zone));
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => sa.AddContact(aube.Id, "Aube", cancel: Cancel))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => sa.AddContact(aube.Id, "Personne", cancel: Cancel))).Status);
        // A whisper from another map, seen by both; to someone not in play, refused.
        Assert.True(await a.Say("/w Brume on se retrouve ?", ChatChannels.Map, Cancel));
        ChatLine heard = (await Next(b, e => e.Line is not null))!.Line!;
        Assert.Equal((ChatChannels.Whisper, "Aube", "Brume", "on se retrouve ?"), (heard.Channel, heard.From, heard.To, heard.Text));
        Assert.Equal(ChatChannels.Whisper, (await Next(a, e => e.Line is not null))!.Line!.Channel);
        await a.Whisper("Personne", "allô", Cancel);
        Assert.Equal("Personne is not in play.", (await Next(a, e => e.Refusal is not null))!.Refusal);
        // Brume comes back into play: Aube, her friend, is told, with the zone.
        await b.DisposeAsync();
        b = await Connect(api, sb, brume.Id);
        ContactNews online = (await Next(a, e => e.Contact is not null))!.Contact!;
        Assert.Equal((ContactNewsKind.Online, "Brume", woods.Zone), (online.Contact, online.Name, online.Zone));
        // Taken out of the contacts.
        Assert.Empty((await sa.RemoveContact(aube.Id, brume.Id, Cancel)).Friends);
        await a.DisposeAsync();
        await b.DisposeAsync();
    }

    [Fact]
    public async Task OneIgnored_CanNeitherWhisperNorInviteNorTrade()
    {
        await using var api = new ApiFactory();
        GameServer sa = await api.SignedIn("ct"), sb = await api.SignedIn("ct");
        CharacterSummary aube = await sa.CreateCharacter("Aube", "female-c", "guard", cancel: Cancel);
        CharacterSummary brume = await sb.CreateCharacter("Brume", "female-c", "mage", cancel: Cancel);
        ChatClient a = await Connect(api, sa, aube.Id), b = await Connect(api, sb, brume.Id);
        ContactList list = await sb.AddContact(brume.Id, "Aube", ignore: true, cancel: Cancel);
        Assert.Equal(["Aube"], list.Ignored.Select(c => c.Name));
        Assert.Null(list.Ignored.Single().Zone);
        await a.Whisper("Brume", "coucou", Cancel);
        Assert.Equal("Brume is not in play.", (await Next(a, e => e.Refusal is not null))!.Refusal);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => sa.Invite(aube.Id, "Brume", Cancel))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => sa.AskTrade(aube.Id, brume.Id, Cancel))).Status);
        // Brume heard nothing of it; the other way round still works.
        Assert.Null(await Next(b, e => e.Line is not null || e.Party is not null || e.Trade is not null));
        await sb.Invite(brume.Id, "Aube", Cancel);
        Assert.NotNull(await Next(a, e => e.Party?.Party == PartyNewsKind.Invited));
        await a.DisposeAsync();
        await b.DisposeAsync();
    }
}
