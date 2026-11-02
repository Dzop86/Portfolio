using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The talents by the API (sprint 137, D80, T103): saved with the points from level 30 within the tree's rules, kept when
/// a client sends none, played in a fight the server replays, given back when the level goes down; since sprint 182 (D93),
/// the compounds the same.
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
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: ["up-close"]), Cancel)));
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 35, []), Cancel);
        CharacterSummary saved = await server.SavePoints(c.Id, new Points(null, null, Talents: ["up-close", "firm-footing"]), Cancel);
        Assert.Equal(["firm-footing", "up-close"], saved.Talents!);
        Assert.Equal(saved.Talents, (await server.Characters(Cancel)).Single().Talents);
        // A client from before the tree sends none: they stay; an unknown one, or twice the same, is refused.
        Assert.Equal(["firm-footing", "up-close"], (await server.SavePoints(c.Id, new Points(null, null), Cancel)).Talents!);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: ["flight"]), Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: ["up-close", "up-close"]), Cancel)));
        // A talent of the tree of version 3, gone with the elements (sprint 182): unknown.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: ["bogging"]), Cancel)));

        // A fight with them: the server replays it with the talents; a record without them is not this character's.
        Hero hero = (await server.Characters(Cancel)).Single().Hero;
        Assert.Equal(["firm-footing", "up-close"], hero.Talents!);
        FightTicket cheat = await server.StartFight(c.Id, "training", Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ReportFight(c.Id, cheat.Id, Play(cheat, hero with { Talents = null }), Cancel)));
        FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
        FightResult result = await server.ReportFight(c.Id, ticket.Id, Play(ticket, hero), Cancel);
        Assert.True(result.TotalXp > 0);

        // Sent empty, none; the level lowered, every point back.
        Assert.Null((await server.SavePoints(c.Id, new Points(null, null, Talents: []), Cancel)).Talents);
        await server.SavePoints(c.Id, new Points(null, null, Talents: ["up-close"], Compounds: [Element.Lightning]), Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 20, []), Cancel);
        Assert.Null((await server.Characters(Cancel)).Single().Talents);
        Assert.Null((await server.Characters(Cancel)).Single().Compounds);
    }

    [Fact]
    public async Task ADraftUndoneInTheTreesWindow_IsNotSent_TheSavedTalentsStay()
    {
        // Sprint 148 (I4 a): the tree's window takes a talent and gives a first floor back, then undoes it all; what it saves is
        // the talents the server holds, and the server keeps them.
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("und");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "sentinel", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 40, []), Cancel);
        Hero hero = (await server.SavePoints(c.Id, new Points(null, null, Talents: ["up-close", "side-step"]), Cancel)).Hero;
        var editor = new PointsEditor(hero, GameData.Embedded);
        editor.Take(GameData.Embedded.Talents["seal"]);
        editor.Drop(GameData.Embedded.Talents["side-step"]);
        editor.Drop(GameData.Embedded.Talents["up-close"]);
        Assert.Empty(editor.TalentsTaken);
        editor.UndoTalents();
        CharacterSummary saved = await server.SavePoints(c.Id, editor.ToPoints(), Cancel);
        Assert.Equal(["side-step", "up-close"], saved.Talents!);
        Assert.Equal(["side-step", "up-close"], (await server.Characters(Cancel)).Single().Talents!);
    }

    [Fact]
    public async Task APlanOfLevelSixty_IsRefused_AtFortyFive()
    {
        // Sprint 151 (N2 a): the window never saves a plan; were it sent, its ghosts are more points than the level gives.
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("pln");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "mage", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 45, []), Cancel);
        string[] plan = ["up-close", "side-step", "seal", "nomad", "heavy-blows", "short-cooldown", "thrift", "dancing-step"];
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: plan), Cancel)));
        Assert.Null((await server.Characters(Cancel)).Single().Talents);
    }

    [Fact]
    public async Task ASentinelOfLightning_SavesItsDeck_AndTheServerReplaysItsFight_ARecordWithoutLightningRefused()
    {
        // Sprint 138 (T104), sprint 182 (D93): Lightning created with the points, a deck of twins (their origins refused since sprint 187, D94), a
        // fight replayed with it; the same fight sent without Lightning is not this character's.
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("lgt");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "sentinel", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 45, []), Cancel);
        // A twin without Lightning: refused.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, ["lightning-fire-arrow"]), Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, ["fire-arrow", "lightning-fire-arrow"], Compounds: [Element.Lightning]), Cancel)));
        CharacterSummary saved = await server.SavePoints(c.Id, new Points(null, null, ["lightning-fire-arrow", "stone-arrow", "lightning-recoil-shot"],
            Compounds: [Element.Lightning]), Cancel);
        Hero hero = saved.Hero;
        Assert.Equal([Element.Lightning], hero.Compounds);
        Assert.Equal(["lightning-fire-arrow", "stone-arrow", "lightning-recoil-shot"], hero.DeckOf(GameData.Embedded));
        FightTicket cheat = await server.StartFight(c.Id, "training", Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ReportFight(c.Id, cheat.Id, Play(cheat, hero with { Compounds = null, Deck = ["stone-arrow"] }), Cancel)));
        FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
        await server.ReportFight(c.Id, ticket.Id, Play(ticket, hero), Cancel);
        Assert.Equal(saved.Deck, (await server.Characters(Cancel)).Single().Deck);
        // Undone, for free: the twins leave the deck the client sends; the server refuses a deck that keeps one.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, saved.Deck, Compounds: []), Cancel)));
        CharacterSummary undone = await server.SavePoints(c.Id, new Points(null, null, ["fire-arrow", "stone-arrow"], Compounds: []), Cancel);
        Assert.Null(undone.Compounds);
        Assert.Equal(saved.Shards, undone.Shards);
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
        await server.SavePoints(c.Id, new Points(null, null, Talents: ["side-step"]), Cancel);
        Assert.Equal(["side-step", "up-close"], (await server.SavePoints(c.Id, new Points(null, null, Talents: ["side-step", "up-close"]), Cancel)).Talents!);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: ["side-step"]), Cancel)));
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            (ch.Town, ch.X, ch.Y) = ("clairval-1-1", 5, 5);
            return db.SaveChanges();
        });
        CharacterSummary after = await server.SavePoints(c.Id, new Points(null, null, Talents: ["side-step"]), Cancel);
        Assert.Equal(["side-step"], after.Talents!);
        Assert.Equal(550, after.Shards);
    }

    [Fact]
    public async Task ASpellOfTheTree_IsSavedInTheDeck_WithItsTalentOnly()
    {
        // Sprint 142 (T108): a Seal in the deck with the talent Seal, refused without; the deck read back keeps it.
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("trs");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "guard", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 40, []), Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, ["seal-water"]), Cancel)));
        CharacterSummary saved = await server.SavePoints(c.Id, new Points(null, null, ["seal-water", "shield-bash"], ["side-step", "backed", "seal"]), Cancel);
        Assert.Equal(["seal-water", "shield-bash"], (await server.Characters(Cancel)).Single().Deck!);
        FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
        await server.ReportFight(c.Id, ticket.Id, Play(ticket, saved.Hero), Cancel);
    }
}
