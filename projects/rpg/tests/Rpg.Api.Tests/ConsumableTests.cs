using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The potions and oils on the server (sprint 111): bought, carried into a fight, drunk, used up; only those owned.</summary>
public class ConsumableTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APotionBoughtFromRose_IsDrunkInAFight_TheServerReplaysIt_AndUsesItUp()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("pot");
        CharacterSummary c = await server.CreateCharacter("Fiole", "female-c", "guard", cancel: Cancel);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            Cell square = GameData.Embedded.Towns[Shop.Village].Spawn;
            (ch.Shards, ch.Town, ch.X, ch.Y) = (20, Shop.Village, square.X, square.Y);
            return db.SaveChanges();
        });
        // At Rose's, 10 Éclats and one a level.
        Assert.Equal(7L, (await server.Buy(c.Id, "healing-potion", 1, Cancel)).Shards);
        FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
        CharacterSummary now = (await server.Characters(Cancel)).Single();
        var fight = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, now.Hero, heroHp: ticket.HeroHp, pack: ["healing-potion"]);
        while (fight.Current != fight.HeroFighter)
            fight.Apply(Ai.Decide(fight));
        Assert.Equal(ActionError.None, fight.Apply(new UseItemAction("healing-potion")));
        Ai.PlayOut(fight);
        FightResult result = await server.ReportFight(c.Id, ticket.Id, FightRecord.Of(fight), Cancel);
        // Used up; the training's enemies may leave another one.
        int left = (await server.Characters(Cancel)).Single().Inventory!.Where(i => i.Item == "healing-potion").Sum(i => i.Count);
        Assert.Equal(result.Loot!.Where(i => i.Item == "healing-potion").Sum(i => i.Count), left);
        // A potion not owned (an elixir, which the training never leaves): the record is refused.
        ticket = await server.StartFight(c.Id, "training", Cancel);
        now = (await server.Characters(Cancel)).Single();
        var cheat = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, now.Hero, heroHp: ticket.HeroHp, pack: ["healing-elixir"]);
        Ai.PlayOut(cheat);
        var refused = await Assert.ThrowsAsync<ServerException>(() => server.ReportFight(c.Id, ticket.Id, FightRecord.Of(cheat), Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, refused.Status);
    }
}
