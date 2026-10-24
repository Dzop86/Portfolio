using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The talents by the API (sprint 137, D80, T103): saved with the points from level 30 within the tree's rules, kept when
/// a client sends none, played in a fight the server replays, given back when the level goes down.
/// </summary>
public class TalentTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => (await Assert.ThrowsAsync<ServerException>(call)).Status;

    private static FightRecord Play(FightTicket ticket, Hero hero)
    {
        var fight = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, hero, heroHp: ticket.HeroHp);
        Ai.PlayOut(fight);
        return FightRecord.Of(fight);
    }

    [Fact]
    public async Task TheTalents_AreSavedFromLevelThirty_KeptWithoutAWord_AndPlayedInAFightTheServerReplays()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("tal");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "sentinel", cancel: Cancel);
        Assert.Null(c.Talents);
        // Before 30: refused.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: ["bogging"]), Cancel)));
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 35, []), Cancel);
        CharacterSummary saved = await server.SavePoints(c.Id, new Points(null, null, Talents: ["up-close", "bogging"]), Cancel);
        Assert.Equal(["bogging", "up-close"], saved.Talents!);
        Assert.Equal(saved.Talents, (await server.Characters(Cancel)).Single().Talents);
        // A client from before the tree sends none: they stay; an unknown one, or twice the same, is refused.
        Assert.Equal(["bogging", "up-close"], (await server.SavePoints(c.Id, new Points(null, null), Cancel)).Talents!);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: ["flight"]), Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: ["bogging", "bogging"]), Cancel)));

        // A fight with them: the server replays it with the talents; a record without them is not this character's.
        Hero hero = (await server.Characters(Cancel)).Single().Hero;
        Assert.Equal(["bogging", "up-close"], hero.Talents!);
        FightTicket cheat = await server.StartFight(c.Id, "training", Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ReportFight(c.Id, cheat.Id, Play(cheat, hero with { Talents = null }), Cancel)));
        FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
        FightResult result = await server.ReportFight(c.Id, ticket.Id, Play(ticket, hero), Cancel);
        Assert.True(result.TotalXp > 0);

        // Sent empty, none; the level lowered, every point back.
        Assert.Null((await server.SavePoints(c.Id, new Points(null, null, Talents: []), Cancel)).Talents);
        await server.SavePoints(c.Id, new Points(null, null, Talents: ["bogging"]), Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 20, []), Cancel);
        Assert.Null((await server.Characters(Cancel)).Single().Talents);
    }

    [Fact]
    public async Task ASentinelConvertedToLightning_SavesItsDeck_AndTheServerReplaysItsFight()
    {
        // Sprint 138 (T104): the conversion's keys, a deck chosen before turned into Lightning, a fight replayed with it.
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("lgt");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "sentinel", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 45, []), Cancel);
        // A twin without its talent: refused.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, ["lightning-fire-arrow"]), Cancel)));
        string[] talents = ["open-lightning", "full-grimoire", "prism", "convert-lightning"];
        CharacterSummary saved = await server.SavePoints(c.Id, new Points(null, null, ["fire-arrow", "stone-arrow", "lightning-recoil-shot"], talents), Cancel);
        Hero hero = saved.Hero;
        Assert.Equal(["lightning-fire-arrow", "stone-arrow", "lightning-recoil-shot"], hero.DeckOf(GameData.Embedded));
        FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
        await server.ReportFight(c.Id, ticket.Id, Play(ticket, hero), Cancel);
        Assert.Equal(saved.Deck, (await server.Characters(Cancel)).Single().Deck);
    }

    [Fact]
    public async Task GivingTalentsBack_IsFreeUpToForty_ThenCostsTenTimesTheLevel_InAVillageOnly()
    {
        // Sprint 140 (T106): taking more is free anywhere; giving back at 45 costs 450 Éclats, in a village, out of a fight.
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("tpr");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "sentinel", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 45, []), Cancel);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            (ch.Town, ch.X, ch.Y, ch.Shards) = ("fields-0-0", 5, 5, 1000);
            return db.SaveChanges();
        });
        await server.SavePoints(c.Id, new Points(null, null, Talents: ["bogging"]), Cancel);
        Assert.Equal(["bogging", "up-close"], (await server.SavePoints(c.Id, new Points(null, null, Talents: ["bogging", "up-close"]), Cancel)).Talents!);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: ["up-close"]), Cancel)));
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            (ch.Town, ch.X, ch.Y) = ("clairval-1-1", 5, 5);
            return db.SaveChanges();
        });
        CharacterSummary after = await server.SavePoints(c.Id, new Points(null, null, Talents: ["up-close"]), Cancel);
        Assert.Equal(["up-close"], after.Talents!);
        Assert.Equal(550, after.Shards);
    }
}
