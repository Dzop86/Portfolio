using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// A fight with the party that survives the server (sprint 125, D79): written as it goes, rebuilt by replaying it when a
/// server starts again on the same database, its hero given its whole turn again; one written by another build of the
/// rules is stopped, its heroes out of it with their life, nobody rewarded.
/// </summary>
public class BattleRestartTests
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

    [Fact]
    public void AFightRebuiltFromItsStartAndActions_IsTheOneInMemory_AndAWrongActionIsRefused()
    {
        Hero aube = Progression.Built(new Hero("Aube", "female-c", "guard", Level: 20), GameData.Embedded);
        Hero brume = Progression.Built(new Hero("Brume", "female-c", "mage", Level: 20), GameData.Embedded);
        var start = new SharedFightStart(Guid.NewGuid(), Group.Scenario, 42, aube, null, [brume], [null], [Guid.NewGuid(), Guid.NewGuid()]);
        Fight played = start.Build(GameData.Embedded);
        for (int i = 0; i < 25 && !played.IsOver; i++)
            Assert.Equal(ActionError.None, played.Apply(Ai.Decide(played)));
        Fight again = Battles.Rebuild(start, played.History, GameData.Embedded)!;
        Assert.Equal(Fingerprint(played), Fingerprint(again));
        Assert.Null(Battles.Rebuild(start, [new CastAction("no-such-spell", new Cell(0, 0))], GameData.Embedded));
    }

    private static string Fingerprint(Fight f) =>
        string.Join('|', f.Events.Select(e => e is Moved m ? $"Moved {m.Fighter} {string.Join(' ', m.Path)}" : e.ToString())) + string.Join(';', f.Fighters.Select(x => $"{x.Id}:{x.Hp}:{x.Cell}"));

    // Two players at level 100 in a party, their fight on the group started, played until three actions or more and a hero's turn.
    private static async Task<(Guid Fight, IReadOnlyList<string> Names, IReadOnlyList<Guid> Ids, int Played, int Fighter, ulong Seed)> Started(ApiFactory api)
    {
        var games = new List<(GameServer Server, CharacterSummary Character, ChatClient Chat)>();
        foreach (string name in new[] { "Aube", "Brume" })
        {
            GameServer server = await api.SignedIn("br");
            CharacterSummary c = await server.CreateCharacter(name, "female-c", name == "Aube" ? "guard" : "mage", cancel: Cancel);
            ChatClient chat = await server.OpenChat(c.Id, Through(api), Cancel);
            var hub = api.Services.GetRequiredService<ChatHub>();
            for (int i = 0; i < 100 && hub.MapOf(c.Id) is null; i++)
                await Task.Delay(20, Cancel);
            Town map = GameData.Embedded.Towns[Group.Map];
            await server.SavePlace(c.Id, new Place(map.Id, map.Spawn.X, map.Spawn.Y), Cancel);
            games.Add((server, c, chat));
        }
        api.WithDb(db =>
        {
            foreach (Character c in db.Characters)
                (c.Xp, c.Level) = (Progression.XpToReach(100), 100);
            return db.SaveChanges();
        });
        (GameServer sa, CharacterSummary aube, ChatClient a) = games[0];
        (GameServer sb, CharacterSummary brume, _) = games[1];
        await sa.Invite(aube.Id, "Brume", Cancel);
        await sb.JoinParty(brume.Id, aube.Id, Cancel);
        SharedFightNews opened = await sa.OpenBattle(aube.Id, Group.Scenario, Cancel);
        await sb.JoinBattle(brume.Id, opened.Fight, Cancel);
        await sa.GoBattle(aube.Id, opened.Fight, Cancel);
        SharedFightNews started = (await Next(a, SharedFightKind.Started))!;
        Fight fight = started.Start!.Build(GameData.Embedded);
        for (int step = 0; step < 200 && (fight.History.Count < 3 || !fight.HeroFighters.Contains(fight.Current)); step++)
        {
            if (fight.HeroFighters.Contains(fight.Current))
            {
                int k = fight.HeroFighters.ToList().IndexOf(fight.Current);
                await games[k].Server.Act(games[k].Character.Id, opened.Fight, new EndTurnAction(), Cancel);
            }
            if (await Next(a, SharedFightKind.Acted) is { Action: FightAction done })
                Assert.Equal(ActionError.None, fight.Apply(done));
        }
        // What the server told since is all applied: the history as it stands.
        while (a.TryRead(out ChatEvent? e))
        {
            if (e!.Shared is { Shared: SharedFightKind.Acted, Action: FightAction more })
                fight.Apply(more);
        }
        foreach ((_, _, ChatClient chat) in games)
            await chat.DisposeAsync();
        return (opened.Fight, [.. api.Accounts], [aube.Id, brume.Id], fight.History.Count, fight.Current.Id, started.Start.Seed);
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
    public async Task AFight_GoesOnAfterTheServerStartsAgain_WhereItWas()
    {
        string database = "rpg_test_" + Guid.NewGuid().ToString("N");
        (Guid fight, IReadOnlyList<string> names, IReadOnlyList<Guid> ids, int played, int fighter, ulong seed) started;
        await using (var api = new ApiFactory { Database = database, KeepDatabase = true })
            started = await Started(api);
        Assert.True(started.played >= 3);
        // A new server on the same database: the fight is back, as it was, and its hero plays on.
        await using var again = new ApiFactory { Database = database };
        GameServer sa = await again.SignedInAs(started.names[0]);
        SharedFightNews? now = null;
        for (int i = 0; i < 100 && now is null; i++)
        {
            try
            {
                now = await sa.Battle(started.ids[0], started.fight, Cancel);
            }
            catch (ServerException e) when (e.Status == HttpStatusCode.NotFound)
            {
                await Task.Delay(50, Cancel);
            }
        }
        Assert.NotNull(now);
        Assert.Equal(started.seed, now!.Start!.Seed);
        Assert.Equal(started.played, now.Actions!.Count);
        Assert.Equal(started.fighter, now.Fighter);
        Fight fight = Battles.Rebuild(now.Start, now.Actions, GameData.Embedded)!;
        int k = fight.HeroFighters.ToList().IndexOf(fight.Current);
        GameServer player = k == 0 ? sa : await again.SignedInAs(started.names[1]);
        await player.Act(started.ids[k], started.fight, new EndTurnAction(), Cancel);
        Assert.True((await sa.Battle(started.ids[0], started.fight, Cancel)).Actions!.Count > started.played);
    }

    [Fact]
    public async Task AFightOfAnotherBuildOfTheRules_IsStopped_TheHeroesOutWithTheirLife()
    {
        string database = "rpg_test_" + Guid.NewGuid().ToString("N");
        (Guid fight, IReadOnlyList<string> names, IReadOnlyList<Guid> ids, int played, int fighter, ulong seed) started;
        var life = new Dictionary<Guid, int?>();
        await using (var api = new ApiFactory { Database = database, KeepDatabase = true })
        {
            started = await Started(api);
            api.WithDb(db =>
            {
                Assert.Equal(started.played, db.BattleActions.Count());
                foreach (Character c in db.Characters)
                    life[c.Id] = c.Hp;
                return db.BattleRecords.ExecuteUpdate(s => s.SetProperty(r => r.Engine, "another-build"));
            });
        }
        await using var again = new ApiFactory { Database = database };
        GameServer sa = await again.SignedInAs(started.names[0]);
        for (int i = 0; i < 100 && again.WithDb(db => db.BattleRecords.Count()) > 0; i++)
            await Task.Delay(50, Cancel);
        Assert.Equal(HttpStatusCode.NotFound, (await Assert.ThrowsAsync<ServerException>(() => sa.Battle(started.ids[0], started.fight, Cancel))).Status);
        again.WithDb(db =>
        {
            Assert.Empty(db.BattleActions);
            Assert.All(db.Characters, c => Assert.False(c.InFight));
            Assert.All(db.Characters, c => Assert.Equal(life[c.Id], c.Hp));
            return 0;
        });
    }
}
