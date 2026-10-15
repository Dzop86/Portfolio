using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The quests on the server (sprint 88): taken, moved on by fights and talks, handed back with their rewards.</summary>
public class QuestTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;
    private static readonly Quest Edge = GameData.Embedded.Quests["edge-of-the-woods"];

    private static async Task Give(ApiFactory api, string account, int level, params ItemCount[] items)
    {
        using IServiceScope scope = api.Services.CreateScope();
        await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, level, items), Cancel);
    }

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => (await Assert.ThrowsAsync<ServerException>(call)).Status;

    [Fact]
    public async Task AQuest_IsTaken_MovesOnWithFightsAndTalks_AndIsHandedBack_ForItsRewards()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("que");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.TakeQuest(c.Id, "no-such-quest", Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.TakeQuest(c.Id, "first-lesson", Cancel)));
        CharacterSummary taken = await server.TakeQuest(c.Id, Edge.Id, Cancel);
        Assert.Equal([new QuestProgress(Edge.Id, [0, 0, 0])], taken.QuestLog!);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.TakeQuest(c.Id, Edge.Id, Cancel)));
        // Not done yet: not handed back.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.HandInQuest(c.Id, Edge.Id, Cancel)));
        // Rose, but not Garance, counts.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.QuestTalk(c.Id, Edge.Id, "clairval/garance", Cancel)));
        Assert.Equal([0, 0, 1], (await server.QuestTalk(c.Id, Edge.Id, "clairval/rose", Cancel)).QuestLog![0].Counts);

        // Strong enough: boars fought until three are down.
        await Give(api, account, 40);
        MonsterGroup[] boars = [.. GameData.Embedded.Groups.Values.Where(g => g.Family == "boar" && g.Fight is null).OrderBy(g => g.Monsters.Count)];
        int down = 0;
        for (int i = 0; down < 3; i++)
        {
            FightTicket ticket = await server.StartFight(c.Id, boars[i].Scenario, Cancel);
            var fight = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, (await server.Characters(Cancel)).Single().Hero);
            Ai.PlayOut(fight);
            await server.ReportFight(c.Id, ticket.Id, FightRecord.Of(fight), Cancel);
            if (fight.WinningTeam == 0)
                down += boars[i].Monsters.Count;
            Assert.Equal(Math.Min(3, down), (await server.Characters(Cancel)).Single().QuestLog![0].Counts[0]);
        }
        // The bristles owned, then handed back: taken, the rewards given, the quest done.
        await Give(api, account, 40, new ItemCount("boar-bristle", 3));
        CharacterSummary before = (await server.Characters(Cancel)).Single();
        QuestReward reward = await server.HandInQuest(c.Id, Edge.Id, Cancel);
        Assert.Equal((Edge.Xp, Edge.Shards), (reward.Xp, reward.Shards));
        CharacterSummary after = reward.Character;
        Assert.Equal((before.Xp + Edge.Xp, before.Shards + Edge.Shards), (after.Xp, after.Shards));
        Assert.Contains(Edge.Id, after.Quests!);
        Assert.Empty(after.QuestLog!);
        // The boars' own loot may have given bristles too: two fewer than before.
        Assert.Equal(before.Inventory!.Single(i => i.Item == "boar-bristle").Count - 2, after.Inventory!.Single(i => i.Item == "boar-bristle").Count);
        Assert.Equal(before.Inventory!.Where(i => i.Item == "bread").Sum(i => i.Count) + 2, after.Inventory!.Where(i => i.Item == "bread").Sum(i => i.Count));
        Assert.Equal(after, (await server.Characters(Cancel)).Single());
        // Done: neither taken again nor handed back twice.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.TakeQuest(c.Id, Edge.Id, Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.HandInQuest(c.Id, Edge.Id, Cancel)));
        // Someone else's character: unknown.
        GameServer other = await api.SignedIn("oth");
        Assert.Equal(HttpStatusCode.NotFound, await Refused(() => other.TakeQuest(c.Id, Edge.Id, Cancel)));
    }
}
