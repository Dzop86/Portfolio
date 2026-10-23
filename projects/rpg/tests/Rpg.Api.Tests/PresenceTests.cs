using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// Seeing each other on the map (sprint 118, D74), with two game clients on the server's sockets: who arrives, walks
/// and leaves; a group of monsters fought by one, refused to the other, away for both once beaten; a spot gathered by
/// one, growing back for the other.
/// </summary>
public class PresenceTests
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
        GameServer server = await api.SignedIn("pr");
        CharacterSummary c = await server.CreateCharacter(name, "female-c", "mage", cancel: Cancel);
        ChatClient chat = await server.OpenChat(c.Id, Through(api), Cancel);
        var hub = (ChatHub)api.Services.GetService(typeof(ChatHub))!;
        for (int i = 0; i < 100 && !hub.On("clairval").Contains(c.Id); i++)
            await Task.Delay(20, Cancel);
        return (server, c, chat);
    }

    // The next news of that kind, the other events passed over; null if none came in time.
    private static async Task<MapNews?> Next(ChatClient chat, MapNewsKind kind)
    {
        while (await chat.Next(Wait, Cancel) is ChatEvent e)
        {
            if (e.News?.Kind == kind)
                return e.News;
        }
        return null;
    }

    private static async Task<bool> Quiet(ChatClient chat)
    {
        while (await chat.Next(Silence, Cancel) is ChatEvent e)
        {
            if (e.News is not null)
                return false;
        }
        return true;
    }

    [Fact]
    public async Task TwoPlayers_SeeEachOther_Arrive_Walk_ChangeMaps_AndLeave()
    {
        await using var api = new ApiFactory();
        (GameServer sa, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (GameServer sb, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        Town village = GameData.Embedded.Towns["clairval"];
        // Each learns of the other: their name, their look, where they stand.
        MapNews? brumeHere = await Next(a, MapNewsKind.Here);
        Assert.Equal((brume.Id, "Brume", brume.Hero, (Cell?)village.Spawn), (brumeHere?.Character, brumeHere?.Name, brumeHere?.Look, brumeHere?.At));
        Assert.Equal(aube.Id, (await Next(b, MapNewsKind.Here))?.Character);
        // Aube walks: Brume sees the cell she goes to; a wall is not a cell to walk to.
        Cell to = village.Spawn.Neighbours().First(village.CanStand);
        await a.WalkTo(to, Cancel);
        MapNews? walk = await Next(b, MapNewsKind.Walk);
        Assert.Equal((aube.Id, (Cell?)to), (walk?.Character, walk?.At));
        Cell wall = Enumerable.Range(0, village.Width).SelectMany(x => Enumerable.Range(0, village.Rows.Count).Select(y => new Cell(x, y))).First(c => !village.CanStand(c));
        await a.WalkTo(wall, Cancel);
        Assert.True(await Quiet(b));
        // Aube takes the West Gate's way: gone for Brume; Brume follows: both see each other there.
        Town gate = GameData.Embedded.Towns["clairval-0-1"];
        await sa.SavePlace(aube.Id, new Place(gate.Id, gate.Spawn.X, gate.Spawn.Y), Cancel);
        Assert.Equal(aube.Id, (await Next(b, MapNewsKind.Gone))?.Character);
        await sb.SavePlace(brume.Id, new Place(gate.Id, gate.Spawn.X, gate.Spawn.Y), Cancel);
        Assert.Equal(brume.Id, (await Next(a, MapNewsKind.Here))?.Character);
        Assert.Equal(aube.Id, (await Next(b, MapNewsKind.Here))?.Character);
        // The game closed: gone.
        await a.DisposeAsync();
        Assert.Equal(aube.Id, (await Next(b, MapNewsKind.Gone))?.Character);
        await b.DisposeAsync();
    }

    [Fact]
    public async Task AGroupFought_IsAwayForEveryone_UntilItComesBack_AndASpotGathered_GrowsBackForEveryone()
    {
        await using var api = new ApiFactory();
        (GameServer sa, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (GameServer sb, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        MonsterGroup group = GameData.Embedded.Groups.Values.First(g => GameData.Embedded.Towns[g.Map].Dungeon is null && GameData.Embedded.QuestToReach(g.Map) is null);
        Town map = GameData.Embedded.Towns[group.Map];
        await sb.SavePlace(brume.Id, new Place(map.Id, map.Spawn.X, map.Spawn.Y), Cancel);
        // Aube attacks it: Brume, on its map, sees it fought, and cannot attack it too.
        await sa.StartFight(aube.Id, group.Scenario, Cancel);
        Assert.Equal(group.Id, (await Next(b, MapNewsKind.Fighting))?.Group);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => sb.StartFight(brume.Id, group.Scenario, Cancel))).Status);
        // Beaten (as a report won would end it): away two minutes, then Brume may attack it.
        var hunts = (Hunts)api.Services.GetService(typeof(Hunts))!;
        Assert.Null(hunts.End(group.Id, brume.Id, won: true, out bool notHers));
        Assert.False(notHers);
        Assert.Equal(api.Clock.GetUtcNow() + Roam.Respawn, hunts.End(group.Id, aube.Id, won: true, out bool hers));
        Assert.True(hers);
        Assert.Equal([new MapNews(MapNewsKind.Beaten, Group: group.Id, Until: api.Clock.GetUtcNow() + Roam.Respawn)], hunts.On(map.Id));
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => sb.StartFight(brume.Id, group.Scenario, Cancel))).Status);
        api.Clock.Advance(Roam.Respawn);
        Assert.Empty(hunts.On(map.Id));
        await sb.StartFight(brume.Id, group.Scenario, Cancel);
        // A fight lost frees the group at once.
        Assert.Null(hunts.End(group.Id, brume.Id, won: false, out _));
        Assert.Empty(hunts.On(map.Id));
        // Aube gathers an oak where Brume stands: Brume sees it growing back.
        HarvestNode oak = GameData.Embedded.Nodes.Values.First(n => n.Level == 1 && n.Kind == NodeKind.Tree);
        Town wood = GameData.Embedded.Towns[oak.Map];
        Cell next = oak.At.Neighbours().First(wood.CanStand);
        await sa.SavePlace(aube.Id, new Place(wood.Id, next.X, next.Y), Cancel);
        await sb.SavePlace(brume.Id, new Place(wood.Id, wood.Spawn.X, wood.Spawn.Y), Cancel);
        Gathered got = await sa.Harvest(aube.Id, oak.Id, next, Cancel);
        MapNews? taken = await Next(b, MapNewsKind.Taken);
        Assert.Equal((oak.Id, (DateTimeOffset?)got.GrowsBack), (taken?.Spot, taken?.Until));
        await a.DisposeAsync();
        await b.DisposeAsync();
    }
}
