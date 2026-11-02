using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The paths by the API, on PostgreSQL (sprint 186, D93): the quest at Four Ways opens the choice, the first path is free, the next
/// paid in a village and out of a fight; the server refuses a fight played on another path; the operator's <c>--spec</c>.
/// </summary>
public class SpecTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => (await Assert.ThrowsAsync<ServerException>(call)).Status;

    private static async Task Level(ApiFactory api, string account, int level)
    {
        using IServiceScope scope = api.Services.CreateScope();
        await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, level, []), Cancel);
    }

    private static void Change(ApiFactory api, Guid id, Action<Character> change) =>
        api.WithDb(db =>
        {
            change(db.Characters.Single(c => c.Id == id));
            return db.SaveChanges();
        });

    private static async Task<(ApiFactory Api, GameServer Server, CharacterSummary Hero, string Account)> Mage(string name, int level)
    {
        var api = new ApiFactory();
        GameServer server = await api.SignedIn(name);
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "mage", cancel: Cancel);
        await Level(api, account, level);
        return (api, server, c, account);
    }

    private static async Task DoTheQuest(ApiFactory api, GameServer server, Guid id)
    {
        Change(api, id, c => c.Quests = "where-roads-part");
        await server.TakeQuest(id, Specs.Quest, Cancel);
        await server.QuestTalk(id, Specs.Quest, "four-ways/josette", Cancel);
        await server.HandInQuest(id, Specs.Quest, Cancel);
    }

    [Fact]
    public async Task TheQuestOpensTheChoice_TheFirstPathIsFree_AtAnyLevel_AndShows()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Mage("sp1", 60);
        await using (api)
        {
            Assert.Null((await server.Characters(Cancel)).Single().Spec);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "archmage", Cancel)));
            await DoTheQuest(api, server, c.Id);
            long shards = (await server.Characters(Cancel)).Single().Shards;
            Change(api, c.Id, x => x.Town = "fields-0-0");
            CharacterSummary chosen = await server.ChooseSpec(c.Id, "archmage", Cancel);
            Assert.Equal("archmage", chosen.Spec);
            Assert.Equal(shards, chosen.Shards);
            Assert.Equal("archmage", chosen.Hero.Spec);
            Assert.Equal("archmage", (await server.Characters(Cancel)).Single().Spec);
            // The same again changes nothing, and costs nothing.
            Assert.Equal(shards, (await server.ChooseSpec(c.Id, "archmage", Cancel)).Shards);
        }
    }

    [Fact]
    public async Task APathOfAnotherClass_Unknown_OrBeforeThirty_IsRefused()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, string account) = await Mage("sp2", 29);
        await using (api)
        {
            Change(api, c.Id, x => x.Quests = Specs.Quest);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "archmage", Cancel)));
            await Level(api, account, 30);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "palisade", Cancel)));
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "nowhere", Cancel)));
            Assert.Equal("archmage", (await server.ChooseSpec(c.Id, "archmage", Cancel)).Spec);
        }
    }

    [Fact]
    public async Task ChangingPath_IsPaid_InAVillage_OutOfAFight()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Mage("sp3", 50);
        await using (api)
        {
            Change(api, c.Id, x => (x.Quests, x.Shards, x.Town) = (Specs.Quest, 2000, "clairval"));
            await server.ChooseSpec(c.Id, "archmage", Cancel);
            // Out of a village.
            Change(api, c.Id, x => x.Town = "fields-0-0");
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "sorcerer", Cancel)));
            Change(api, c.Id, x => x.Town = "clairval");
            // In a fight.
            Change(api, c.Id, x => x.InFight = true);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "sorcerer", Cancel)));
            Change(api, c.Id, x => x.InFight = false);
            // Too poor: 30 × 50 = 1500.
            Change(api, c.Id, x => x.Shards = 1499);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "sorcerer", Cancel)));
            Assert.Equal("archmage", (await server.Characters(Cancel)).Single().Spec);
            Change(api, c.Id, x => x.Shards = 2000);
            CharacterSummary changed = await server.ChooseSpec(c.Id, "sorcerer", Cancel);
            Assert.Equal(("sorcerer", 500L), (changed.Spec, changed.Shards));
        }
    }

    [Fact]
    public async Task AFightPlayedOnAnotherPath_IsRefused()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Mage("sp4", 30);
        await using (api)
        {
            Change(api, c.Id, x => x.Quests = Specs.Quest);
            CharacterSummary chosen = await server.ChooseSpec(c.Id, "archmage", Cancel);
            FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
            var other = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, chosen.Hero with { Spec = "sorcerer" }, heroHp: ticket.HeroHp);
            Ai.PlayOut(other);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ReportFight(c.Id, ticket.Id, FightRecord.Of(other), Cancel)));
            ticket = await server.StartFight(c.Id, "training", Cancel);
            var own = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, chosen.Hero, heroHp: ticket.HeroHp);
            Ai.PlayOut(own);
            await server.ReportFight(c.Id, ticket.Id, FightRecord.Of(own), Cancel);
        }
    }

    [Fact]
    public async Task TheOperators_Spec_GivesEachCharacterThePathOfItsClass_AndTakesItBack()
    {
        Assert.Null(Admin.Parse(["admin", "x", "--spec", "nowhere"]).Order);
        Assert.Null(Admin.Parse(["admin", "x", "--spec"]).Order);
        Assert.Equal(["archmage", "palisade"], Admin.Parse(["admin", "x", "--spec", "archmage,palisade"]).Order!.Specs!);
        Assert.Empty(Admin.Parse(["admin", "x", "--spec", "none"]).Order!.Specs!);

        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("sp5");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        await server.CreateCharacter("Plop", "female-a", "mage", cancel: Cancel);
        await server.CreateCharacter("Pluf", "female-a", "guard", cancel: Cancel);
        await server.CreateCharacter("Plif", "female-a", "sentinel", cancel: Cancel);
        await Level(api, account, 100);
        async Task Run(params string[] args)
        {
            using IServiceScope scope = api.Services.CreateScope();
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), Admin.Parse(["admin", account, .. args]).Order!, Cancel);
        }
        await Run("--spec", "archmage,palisade");
        Dictionary<string, string?> specs = (await server.Characters(Cancel)).ToDictionary(x => x.Name, x => x.Spec);
        Assert.Equal(("archmage", "palisade", (string?)null), (specs["Plop"], specs["Pluf"], specs["Plif"]));
        Assert.All(await server.Characters(Cancel), x => Assert.Null(x.Hero.Problem(GameData.Embedded)));
        await Run("--spec", "none");
        Assert.All(await server.Characters(Cancel), x => Assert.Null(x.Spec));
        // A level below the paths takes them back with the points.
        await Run("--spec", "archmage");
        await Level(api, account, 20);
        Assert.All(await server.Characters(Cancel), x => Assert.Null(x.Spec));
    }
}
