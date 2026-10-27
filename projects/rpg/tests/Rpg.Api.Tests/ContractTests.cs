using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The notice board on the server (sprint 153, D83): three contracts the first day, a hunt counted after the fights,
/// handed back only at a board and fulfilled, its reward; a gathering takes its items; a change free once, then paid.
/// </summary>
public class ContractTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task Give(ApiFactory api, string account, int level, params ItemCount[] items)
    {
        using IServiceScope scope = api.Services.CreateScope();
        await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, level, items), Cancel);
    }

    private static void Board(ApiFactory api, ContractBoard board, string? town = null, long shards = -1) => api.WithDb(db =>
    {
        Character c = db.Characters.Single();
        c.Contracts = Contracts.Write(board);
        if (town is not null)
            (c.Town, c.X, c.Y) = (town, 24, 16);
        if (shards >= 0)
            c.Shards = shards;
        return db.SaveChanges();
    });

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => (await Assert.ThrowsAsync<ServerException>(call)).Status;

    [Fact]
    public async Task AHunt_IsCountedByTheFights_AndHandedBackAtABoard_ForItsReward()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("avis");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        // A first board: three contracts.
        Assert.Equal(Contracts.PerDay, (await server.Contracts(c.Id, Cancel)).Offered.Count);
        await Give(api, account, 40);
        int today = Contracts.Day(DateTimeOffset.UtcNow);
        Board(api, new ContractBoard(today, 0, [new Contract(ContractKind.Hunt, "boar", 3, 40)]));
        MonsterGroup[] boars = [.. GameData.Embedded.Groups.Values.Where(g => g.Family == "boar" && g.Fight is null).OrderBy(g => g.Monsters.Count)];
        int down = 0;
        for (int i = 0; down < 3; i++)
        {
            FightTicket ticket = await server.StartFight(c.Id, boars[i].Scenario, Cancel);
            var fight = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, (await server.Characters(Cancel)).Single().Hero, heroHp: ticket.HeroHp);
            Ai.PlayOut(fight);
            await server.ReportFight(c.Id, ticket.Id, FightRecord.Of(fight), Cancel);
            if (fight.WinningTeam == 0)
                down += boars[i].Monsters.Count;
        }
        Assert.Equal(3, (await server.Contracts(c.Id, Cancel)).Offered[0].Done);
        // The bestiary too (sprint 154): the boars beaten counted, in the character's summary.
        IReadOnlyDictionary<string, int> bestiary = (await server.Characters(Cancel)).Single().Bestiary!;
        Assert.True(boars[0].Monsters.Distinct().All(bestiary.ContainsKey));
        Assert.True(bestiary.Values.Sum() >= 3);
        // Away from a board: refused; at Clairval's: handed back.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.HandInContract(c.Id, 0, Cancel)));
        Board(api, Contracts.Read(api.WithDb(db => db.Characters.Single().Contracts)), town: "clairval");
        long xp = (await server.Characters(Cancel)).Single().Xp;
        ContractReward reward = await server.HandInContract(c.Id, 0, Cancel);
        Assert.Equal((1000L, 200L), (reward.Xp, reward.Shards));
        Assert.Equal(xp + 1000, reward.Character.Xp);
        Assert.Empty(reward.Board.Offered);
    }

    [Fact]
    public async Task AGathering_TakesItsItems_AndAChange_IsFreeOnce_ThenPaid()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("avis");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        await Give(api, account, 20, new ItemCount("oak-log", 12));
        int today = Contracts.Day(DateTimeOffset.UtcNow);
        var gather = new Contract(ContractKind.Gather, "oak-log", 10, 20);
        Board(api, new ContractBoard(today, 0, [gather, new Contract(ContractKind.Hunt, "wolf", 6, 20)]), town: "four-ways", shards: 50);
        // Not fulfilled: refused.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.HandInContract(c.Id, 1, Cancel)));
        ContractReward reward = await server.HandInContract(c.Id, 0, Cancel);
        Assert.Contains(new ItemCount("oak-log", 2), reward.Character.Inventory!);
        Assert.Equal(50 + 100, reward.Character.Shards);
        // The first change free; the second costs 5 × level; the third, more than what is left, refused.
        ContractBoard changed = await server.ChangeContract(c.Id, 0, Cancel);
        Assert.Equal(1, changed.Changes);
        Assert.Equal(150, (await server.Characters(Cancel)).Single().Shards);
        await server.ChangeContract(c.Id, 0, Cancel);
        CharacterSummary after = (await server.Characters(Cancel)).Single();
        Assert.Equal(150 - (5 * after.Level), after.Shards);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChangeContract(c.Id, 0, Cancel)));
    }
}
