using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// A fight with the party led by the server (sprint 120, D74), with two game clients: opened on a group, joined, started;
/// each player plays its hero in its turn, the server the monsters; both games keep the same fight; a turn left alone
/// passes after thirty seconds; once over, each hero is rewarded on its own and the group is away.
/// </summary>
public class BattleTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

    private static Func<Uri, AuthenticationHeaderValue?, CancellationToken, Task<WebSocket>> Through(ApiFactory api) => (uri, auth, cancel) =>
    {
        var client = api.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization = auth?.ToString();
        return client.ConnectAsync(uri, cancel);
    };

    private static readonly MonsterGroup Group = GameData.Embedded.Groups.Values.First(g => GameData.Embedded.Towns[g.Map].Dungeon is null && g.Monsters.Count == 1);

    private static async Task<(GameServer Server, CharacterSummary Character, ChatClient Chat)> Player(ApiFactory api, string name)
    {
        GameServer server = await api.SignedIn("ba");
        CharacterSummary c = await server.CreateCharacter(name, "female-c", name == "Aube" ? "guard" : "mage", cancel: Cancel);
        ChatClient chat = await server.OpenChat(c.Id, Through(api), Cancel);
        var hub = (ChatHub)api.Services.GetService(typeof(ChatHub))!;
        for (int i = 0; i < 100 && hub.MapOf(c.Id) is null; i++)
            await Task.Delay(20, Cancel);
        Town map = GameData.Embedded.Towns[Group.Map];
        await server.SavePlace(c.Id, new Place(map.Id, map.Spawn.X, map.Spawn.Y), Cancel);
        return (server, c, chat);
    }

    private static async Task<SharedFightNews?> Next(ChatClient chat, SharedFightKind kind)
    {
        while (await chat.Next(Wait, Cancel) is ChatEvent e)
        {
            if (e.Shared?.Shared == kind)
                return e.Shared;
        }
        return null;
    }

    [Fact]
    public async Task TwoPlayers_FightAGroupTogether_EachInTheirTurn_AndAreEachRewarded()
    {
        await using var api = new ApiFactory();
        (GameServer sa, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (GameServer sb, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        // Strong enough to win: level 30 both.
        api.WithDb(db =>
        {
            foreach (Character c in db.Characters)
                (c.Xp, c.Level) = (Progression.XpToReach(30), 30);
            return db.SaveChanges();
        });
        // Not in a party: refused; in one, Aube opens the fight and Brume, on the map, is told.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => sa.OpenBattle(aube.Id, Group.Scenario, Cancel))).Status);
        await sa.Invite(aube.Id, "Brume", Cancel);
        await sb.JoinParty(brume.Id, aube.Id, Cancel);
        SharedFightNews opened = await sa.OpenBattle(aube.Id, Group.Scenario, Cancel);
        Assert.Equal((opened.Fight, "Aube"), (await Next(b, SharedFightKind.Opened)) is SharedFightNews told ? (told.Fight, told.Name) : default);
        await sb.JoinBattle(brume.Id, opened.Fight, Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => sb.GoBattle(brume.Id, opened.Fight, Cancel))).Status);
        await sa.GoBattle(aube.Id, opened.Fight, Cancel);
        // Both games build the same fight from the start, then play the actions told.
        var games = new List<(GameServer Server, Guid Me, ChatClient Chat, Fight Fight)>();
        foreach ((GameServer s, Guid me, ChatClient chat) in new[] { (sa, aube.Id, a), (sb, brume.Id, b) })
        {
            SharedFightNews started = (await Next(chat, SharedFightKind.Started))!;
            Assert.Equal([aube.Id, brume.Id], started.Start!.Players);
            games.Add((s, me, chat, started.Start.Build(GameData.Embedded)));
        }
        Assert.Equal(2, games[0].Fight.HeroFighters.Count);
        SharedFightNews? over = null;
        for (int step = 0; step < 3000 && over is null; step++)
        {
            // Each game reads what came; the player whose hero's turn it is plays as the AI would.
            foreach ((GameServer s, Guid me, ChatClient chat, Fight fight) in games)
            {
                while (chat.TryRead(out ChatEvent? e))
                {
                    if (e!.Shared is { Shared: SharedFightKind.Acted, Action: FightAction done })
                        Assert.Equal(ActionError.None, fight.Apply(done));
                    else if (e.Shared is { Shared: SharedFightKind.Over } end && me == aube.Id)
                        over = end;
                }
            }
            (GameServer server, Guid id, _, Fight mine) = games.FirstOrDefault(g => !g.Fight.IsOver && g.Fight.HeroFighters[g.Me == aube.Id ? 0 : 1] == g.Fight.Current);
            if (mine is not null)
            {
                FightAction action = Ai.Decide(mine);
                if (await TryAct(server, id, opened.Fight, action) is false)
                    await server.Act(id, opened.Fight, new EndTurnAction(), Cancel);
            }
            await Task.Delay(5, Cancel);
        }
        Assert.NotNull(over);
        Assert.Equal(games[0].Fight.History, games[1].Fight.History);
        Assert.Equal(games[0].Fight.WinningTeam, over!.Winner);
        Assert.Equal(["Aube", "Brume"], over.Results!.Select(r => r.Name));
        // Each one rewarded, out of the fight; the group away for everyone if beaten.
        api.WithDb(db =>
        {
            Assert.All(db.Characters, c => Assert.False(c.InFight));
            return 0;
        });
        var hunts = (Hunts)api.Services.GetService(typeof(Hunts))!;
        if (over.Winner == 0)
            Assert.Contains(hunts.On(Group.Map), n => n.Group == Group.Id && n.Kind == MapNewsKind.Beaten);
        await a.DisposeAsync();
        await b.DisposeAsync();
    }

    private static async Task<bool> TryAct(GameServer server, Guid id, Guid fight, FightAction action)
    {
        try
        {
            await server.Act(id, fight, action, Cancel);
            return true;
        }
        catch (ServerException)
        {
            return false;
        }
    }

    [Fact]
    public async Task AHeroLeftAlone_PassesItsTurnAfterThirtySeconds_AndOnlyItsPlayerPlaysIt()
    {
        await using var api = new ApiFactory();
        (GameServer sa, CharacterSummary aube, ChatClient a) = await Player(api, "Aube");
        (GameServer sb, CharacterSummary brume, ChatClient b) = await Player(api, "Brume");
        await sa.Invite(aube.Id, "Brume", Cancel);
        await sb.JoinParty(brume.Id, aube.Id, Cancel);
        SharedFightNews opened = await sa.OpenBattle(aube.Id, Group.Scenario, Cancel);
        await sb.JoinBattle(brume.Id, opened.Fight, Cancel);
        // Nobody says go: it starts by itself after fifteen seconds.
        api.Clock.Advance(SharedRules.JoinFor);
        SharedFightNews started = (await Next(a, SharedFightKind.Started))!;
        Fight fight = started.Start!.Build(GameData.Embedded);
        foreach (FightAction done in started.Actions ?? [])
            fight.Apply(done);
        // The monsters play first if quicker: until a hero's turn.
        for (int i = 0; i < 200 && !fight.HeroFighters.Contains(fight.Current); i++)
        {
            if (await a.Next(Wait, Cancel) is { Shared: { Shared: SharedFightKind.Acted, Action: FightAction done } })
                fight.Apply(done);
        }
        // A hero's turn now: the other player cannot play it; thirty seconds later, it passes.
        int hero = fight.HeroFighters.ToList().IndexOf(fight.Current);
        Assert.InRange(hero, 0, 1);
        GameServer other = hero == 0 ? sb : sa;
        Guid otherId = hero == 0 ? brume.Id : aube.Id;
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => other.Act(otherId, opened.Fight, new EndTurnAction(), Cancel))).Status);
        api.Clock.Advance(SharedRules.Turn);
        SharedFightNews passed = (await Next(a, SharedFightKind.Acted))!;
        Assert.IsType<EndTurnAction>(passed.Action);
        // Coming back: the start and every action so far.
        SharedFightNews now = await sb.Battle(brume.Id, opened.Fight, Cancel);
        Assert.Equal(started.Start.Seed, now.Start!.Seed);
        Assert.True(now.Actions!.Count >= 1);
        await a.DisposeAsync();
        await b.DisposeAsync();
    }
}
