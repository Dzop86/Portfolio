using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The life kept between fights on the server (sprint 101, D73): frozen by the ticket, kept or lost by the report, grown back, eaten.</summary>
public class LifeTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task<(FightTicket Ticket, Fight Fight)> Play(GameServer server, CharacterSummary c, Func<Fight, bool>? until = null)
    {
        FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
        var fight = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, (await server.Characters(Cancel)).Single().Hero, heroHp: ticket.HeroHp);
        Ai.PlayOut(fight);
        return (ticket, fight);
    }

    [Fact]
    public async Task AFight_StartsWithTheLifeLeft_EndsWithItsOwn_AndTheLifeGrowsBackOutOfAFight()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("lif");
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        // A first fight from full life: the ticket says so.
        (FightTicket first, Fight fight) = await Play(server, c);
        Assert.Null(first.HeroHp);
        Assert.True((await server.Characters(Cancel)).Single().InFight);
        await server.ReportFight(c.Id, first.Id, FightRecord.Of(fight), Cancel);
        CharacterSummary after = (await server.Characters(Cancel)).Single();
        int max = Life.MaxHp(after.Hero, GameData.Embedded);
        int left = fight.WinningTeam == 0 && after.Level == c.Level ? Math.Min(fight.HeroFighter!.Hp, max) : fight.WinningTeam == 0 ? max : 1;
        Assert.Equal(left, after.LifeAt(api.Clock.GetUtcNow(), GameData.Embedded));
        Assert.False(after.InFight);
        // The next fight starts with it; a record from full life is refused.
        FightTicket next = await server.StartFight(c.Id, "training", Cancel);
        Assert.Equal(left >= max ? null : left, next.HeroHp);
        if (next.HeroHp is not null)
        {
            var full = new Fight(GameData.Embedded, next.Scenario, next.Seed, after.Hero);
            Ai.PlayOut(full);
            Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.ReportFight(c.Id, next.Id, FightRecord.Of(full), Cancel))).Status);
        }
    }

    [Fact]
    public async Task AFightNeverReported_IsLost_ThenTheLifeGrowsBack_AndBreadHealsAtOnce()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("lif");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, null, [new ItemCount("bread", 2)]), Cancel);
        await server.StartFight(c.Id, "training", Cancel);
        // Another ticket: the first fight is lost, at 1 hit point.
        FightTicket second = await server.StartFight(c.Id, "training", Cancel);
        Assert.Equal(1, second.HeroHp);
        // The second abandoned too, half an hour: 1, then one a second (a hero under 100 hit points).
        api.Clock.Advance(Life.Abandoned + TimeSpan.FromSeconds(10));
        CharacterSummary now = (await server.Characters(Cancel)).Single();
        Assert.Equal(11, now.LifeAt(api.Clock.GetUtcNow(), GameData.Embedded));
        // Eating: not in a fight... the abandoned fight still counts as one until reported or replaced.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Eat(c.Id, "bread", Cancel))).Status);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            ch.SetLife(10, api.Clock.GetUtcNow());
            return db.SaveChanges();
        });
        CharacterSummary fed = await server.Eat(c.Id, "bread", Cancel);
        Assert.Equal(50, fed.LifeAt(api.Clock.GetUtcNow(), GameData.Embedded));
        Assert.Equal(1, fed.Inventory!.Single(i => i.Item == "bread").Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.Eat(c.Id, "copper-ring", Cancel))).Status);
    }
}
