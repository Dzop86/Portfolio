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

    private static Task<(ApiFactory Api, GameServer Server, CharacterSummary Hero, string Account)> Mage(string name, int level) => Hero(name, level, "mage");

    private static async Task<(ApiFactory Api, GameServer Server, CharacterSummary Hero, string Account)> Hero(string name, int level, string cls)
    {
        var api = new ApiFactory();
        GameServer server = await api.SignedIn(name);
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", cls, cancel: Cancel);
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
    public void TheOperators_Spec_RefusesAnUnknownFinalClass() =>
        // Sprint 195 (D96): the form branch:lineage:final is parsed; none exists yet to give (S197).
        Assert.Contains("branch[:lineage[:final]]", Admin.Parse(["admin", "x", "--spec", "archmage:patient:nowhere"]).Error, StringComparison.Ordinal);

    [Fact]
    public async Task AFinalClass_IsCheckedByTheRules_StoredByTheColumn_AndLostWithItsLineage()
    {
        // Sprint 194 (D96): no lineage has final classes before the Warrior (S197), so the refusals and the column, not a choice that succeeds: FinalsTests has that one, on its own data.
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Mage("Finale", 100);
        await using (api)
        {
            await DoTheQuest(api, server, c.Id);
            await server.ChooseSpec(c.Id, "arcanist", "scholar", Cancel);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "arcanist", "scholar", "nowhere", Cancel)));
            Assert.Null((await server.Characters(Cancel)).Single().Final);
            // Stored, and handed back; naming the same lineage keeps it.
            Change(api, c.Id, x => x.Final = "stored");
            Assert.Equal("stored", (await server.Characters(Cancel)).Single().Final);
            Assert.Equal("stored", (await server.ChooseSpec(c.Id, "arcanist", cancel: Cancel)).Final);
            // Another lineage does not carry it over.
            Change(api, c.Id, x => (x.Town, x.Shards) = ("clairval", 100_000));
            Assert.Null((await server.ChooseSpec(c.Id, "arcanist", "rune-weaver", Cancel)).Final);
        }
    }

    // Sprint 197: the Warrior's lineages end in final classes, the first choice of one succeeds (T155 put it off to here).
    [Fact]
    public async Task AWarriorsFinalClass_IsFreeTheFirstTime_ThenFiveTimesTheLevel_InAVillage()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Hero("Finales", 100, "warrior");
        await using (api)
        {
            await DoTheQuest(api, server, c.Id);
            Change(api, c.Id, x => (x.Town, x.Shards) = ("clairval", 1_000));
            await server.ChooseSpec(c.Id, "palisade", "rampart", Cancel);
            CharacterSummary taken = await server.ChooseSpec(c.Id, "palisade", "rampart", "crusader", Cancel);
            Assert.Equal(("crusader", 1_000L), (taken.Final, taken.Shards));
            Assert.Equal("Croisé", Specs.TitleOf(taken.Hero, GameData.Embedded)!.Fr);
            // Its sister: 5 × 100, in a village.
            CharacterSummary sister = await server.ChooseSpec(c.Id, "palisade", "rampart", "sentry", Cancel);
            Assert.Equal(("sentry", 500L), (sister.Final, sister.Shards));
            Change(api, c.Id, x => x.Town = null);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "palisade", "rampart", "crusader", Cancel)));
            // A final class of the sister lineage, taken with it at its price, from a village.
            Change(api, c.Id, x => (x.Town, x.Shards) = ("clairval", 1_000));
            CharacterSummary both = await server.ChooseSpec(c.Id, "palisade", "bastion", "warlord", Cancel);
            Assert.Equal(("bastion", "warlord", 0L), (both.Fork, both.Final, both.Shards));
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "palisade", "bastion", "crusader", Cancel)));
        }
    }

    [Fact]
    public async Task TheOperators_Spec_GivesAWarriorItsFinalClass()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("sp6");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        await server.CreateCharacter("Pluf", "female-a", "warrior", cancel: Cancel);
        await Level(api, account, 100);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), Admin.Parse(["admin", account, "--spec", "breach:harpooner:pugilist"]).Order!, Cancel);
        CharacterSummary pluf = (await server.Characters(Cancel)).Single();
        Assert.Equal(("breach", "harpooner", "pugilist"), (pluf.Spec, pluf.Fork, pluf.Final));
        Assert.Null(pluf.Hero.Problem(GameData.Embedded));
        Assert.Null(Admin.Parse(["admin", account, "--spec", "breach:harpooner:crusader"]).Order);
    }

    [Fact]
    public async Task TheQuestOpensTheChoice_TheFirstPathIsFree_AtAnyLevel_AndShows()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Mage("sp1", 60);
        await using (api)
        {
            Assert.Null((await server.Characters(Cancel)).Single().Spec);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "arcanist", cancel: Cancel)));
            await DoTheQuest(api, server, c.Id);
            long shards = (await server.Characters(Cancel)).Single().Shards;
            Change(api, c.Id, x => x.Town = "fields-0-0");
            CharacterSummary chosen = await server.ChooseSpec(c.Id, "arcanist", cancel: Cancel);
            Assert.Equal("arcanist", chosen.Spec);
            Assert.Equal(shards, chosen.Shards);
            Assert.Equal("arcanist", chosen.Hero.Spec);
            Assert.Equal("arcanist", (await server.Characters(Cancel)).Single().Spec);
            // The same again changes nothing, and costs nothing.
            Assert.Equal(shards, (await server.ChooseSpec(c.Id, "arcanist", cancel: Cancel)).Shards);
        }
    }

    [Fact]
    public async Task APathOfAnotherClass_Unknown_OrBeforeThirty_IsRefused()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, string account) = await Mage("sp2", 29);
        await using (api)
        {
            Change(api, c.Id, x => x.Quests = Specs.Quest);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "arcanist", cancel: Cancel)));
            await Level(api, account, 30);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "palisade", cancel: Cancel)));
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "nowhere", cancel: Cancel)));
            Assert.Equal("arcanist", (await server.ChooseSpec(c.Id, "arcanist", cancel: Cancel)).Spec);
        }
    }

    [Fact]
    public async Task ChangingPath_IsPaid_InAVillage_OutOfAFight()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Mage("sp3", 50);
        await using (api)
        {
            Change(api, c.Id, x => (x.Quests, x.Shards, x.Town) = (Specs.Quest, 2000, "clairval"));
            await server.ChooseSpec(c.Id, "arcanist", cancel: Cancel);
            // Out of a village.
            Change(api, c.Id, x => x.Town = "fields-0-0");
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "chronomancer", "time-weaver", Cancel)));
            Change(api, c.Id, x => x.Town = "clairval");
            // In a fight.
            Change(api, c.Id, x => x.InFight = true);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "chronomancer", "time-weaver", Cancel)));
            Change(api, c.Id, x => x.InFight = false);
            // Too poor: 30 × 50 = 1500.
            Change(api, c.Id, x => x.Shards = 1499);
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "chronomancer", "time-weaver", Cancel)));
            Assert.Equal("arcanist", (await server.Characters(Cancel)).Single().Spec);
            Change(api, c.Id, x => x.Shards = 2000);
            CharacterSummary changed = await server.ChooseSpec(c.Id, "chronomancer", "time-weaver", Cancel);
            Assert.Equal(("chronomancer", "time-weaver", 500L), (changed.Spec, changed.Fork, changed.Shards));
        }
    }

    [Fact]
    public async Task ALineage_IsFreeTheFirstTime_ThenPaid_AndABranchChange_NamesOne()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Mage("sp6", 60);
        await using (api)
        {
            Change(api, c.Id, x => (x.Quests, x.Shards, x.Town) = (Specs.Quest, 3000, "fields-0-0"));
            // A hero of 60 takes both in one gesture, out of a village, for free.
            CharacterSummary chosen = await server.ChooseSpec(c.Id, "arcanist", "scholar", Cancel);
            Assert.Equal(("arcanist", "scholar", 3000L), (chosen.Spec, chosen.Fork, chosen.Shards));
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "arcanist", "time-weaver", Cancel)));
            // The other lineage: in a village, 10 × the level.
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "arcanist", "rune-weaver", Cancel)));
            Change(api, c.Id, x => x.Town = "clairval");
            CharacterSummary other = await server.ChooseSpec(c.Id, "arcanist", "rune-weaver", Cancel);
            Assert.Equal(("rune-weaver", 2400L), (other.Fork, other.Shards));
            // Naming none keeps it, and costs nothing.
            Assert.Equal(("rune-weaver", 2400L), ((await server.ChooseSpec(c.Id, "arcanist", cancel: Cancel)).Fork, (await server.Characters(Cancel)).Single().Shards));
            // Another branch at 50 or more names its lineage, at 30 × the level.
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "chronomancer", cancel: Cancel)));
            CharacterSummary branch = await server.ChooseSpec(c.Id, "chronomancer", "distorter", Cancel);
            Assert.Equal(("chronomancer", "distorter", 600L), (branch.Spec, branch.Fork, branch.Shards));
        }
    }

    [Fact]
    public async Task ALineage_WaitsForFifty_AndAHeroWithABranch_ChoosesItLater()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, string account) = await Mage("sp7", 49);
        await using (api)
        {
            Change(api, c.Id, x => (x.Quests, x.Town) = (Specs.Quest, "fields-0-0"));
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ChooseSpec(c.Id, "arcanist", "scholar", Cancel)));
            Assert.Null((await server.ChooseSpec(c.Id, "arcanist", cancel: Cancel)).Fork);
            await Level(api, account, 50);
            Assert.Equal("scholar", (await server.ChooseSpec(c.Id, "arcanist", "scholar", Cancel)).Fork);
        }
    }

    [Fact]
    public async Task AFightPlayedOnAnotherPath_IsRefused()
    {
        (ApiFactory api, GameServer server, CharacterSummary c, _) = await Mage("sp4", 30);
        await using (api)
        {
            Change(api, c.Id, x => x.Quests = Specs.Quest);
            CharacterSummary chosen = await server.ChooseSpec(c.Id, "arcanist", cancel: Cancel);
            FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
            var other = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, chosen.Hero with { Spec = "chronomancer" }, heroHp: ticket.HeroHp);
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
        Assert.Equal(["arcanist:scholar", "palisade"], Admin.Parse(["admin", "x", "--spec", "arcanist:scholar,palisade"]).Order!.Specs!);
        Assert.Null(Admin.Parse(["admin", "x", "--spec", "arcanist:time-weaver"]).Order);
        Assert.Empty(Admin.Parse(["admin", "x", "--spec", "none"]).Order!.Specs!);

        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("sp5");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        await server.CreateCharacter("Plop", "female-a", "mage", cancel: Cancel);
        await server.CreateCharacter("Pluf", "female-a", "warrior", cancel: Cancel);
        await server.CreateCharacter("Plif", "female-a", "archer", cancel: Cancel);
        await Level(api, account, 100);
        async Task Run(params string[] args)
        {
            using IServiceScope scope = api.Services.CreateScope();
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), Admin.Parse(["admin", account, .. args]).Order!, Cancel);
        }
        await Run("--spec", "arcanist:scholar,palisade");
        Assert.Equal("scholar", (await server.Characters(Cancel)).Single(x => x.Name == "Plop").Fork);
        Dictionary<string, string?> specs = (await server.Characters(Cancel)).ToDictionary(x => x.Name, x => x.Spec);
        Assert.Equal(("arcanist", "palisade", (string?)null), (specs["Plop"], specs["Pluf"], specs["Plif"]));
        Assert.All(await server.Characters(Cancel), x => Assert.Null(x.Hero.Problem(GameData.Embedded)));
        await Run("--spec", "none");
        Assert.All(await server.Characters(Cancel), x => Assert.Null(x.Spec));
        // A level below the paths takes them back with the points.
        await Run("--spec", "arcanist");
        await Level(api, account, 20);
        Assert.All(await server.Characters(Cancel), x => Assert.Null(x.Spec));
    }
}
