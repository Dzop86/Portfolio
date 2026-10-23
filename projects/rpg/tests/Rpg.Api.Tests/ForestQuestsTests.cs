using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The Forest of the Ancestors' main thread by the API (sprint 136, D79): after "Where the Roads Part", Anatole sends the
/// hero to the miners' camp, Berthe has the test pits examined, then the keepers' quest is offered; the Roots' step gives
/// its key once, and is handed back at Quatre-Chemins.
/// </summary>
public class ForestQuestsTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => (await Assert.ThrowsAsync<ServerException>(call)).Status;

    [Fact]
    public async Task TheForestsMainThread_FollowsWhereTheRoadsPart_ThroughTheCampAndThePits_ToTheKeepers()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("fqt");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 33, []), Cancel);
        // Not before Anatole's first step.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.TakeQuest(c.Id, "the-tree-town", Cancel)));
        api.WithDb(db =>
        {
            db.Characters.Single().Quests = "four-trails,where-roads-part";
            return db.SaveChanges();
        });
        await server.TakeQuest(c.Id, "the-tree-town", Cancel);
        await server.QuestTalk(c.Id, "the-tree-town", "ancestors/berold", Cancel);
        Assert.Equal([1, 1], (await server.QuestTalk(c.Id, "the-tree-town", "ancestors/berthe", Cancel)).QuestLog!.Single().Counts);
        QuestReward first = await server.HandInQuest(c.Id, "the-tree-town", Cancel);
        Assert.Equal((1860, 310), (first.Xp, first.Shards));

        // The three test pits, then the judge.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.TakeQuest(c.Id, "mossy-keepers", Cancel)));
        await server.TakeQuest(c.Id, "the-unmapped-passage", Cancel);
        foreach (string pit in new[] { "trial-pit-1", "trial-pit-2", "trial-pit-3" })
            await server.QuestInteract(c.Id, "the-unmapped-passage", pit, Cancel);
        Assert.Equal([1, 1, 1, 1], (await server.QuestTalk(c.Id, "the-unmapped-passage", "ancestors/berold", Cancel)).QuestLog!.Single().Counts);
        await server.HandInQuest(c.Id, "the-unmapped-passage", Cancel);
        Assert.Contains("the-unmapped-passage", (await server.Characters(Cancel)).Single().Quests!);

        // The keepers' quest is offered at its level, and the Roots' step, given at 41 once the keepers are done, hands its key over.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.TakeQuest(c.Id, "mossy-keepers", Cancel)));
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 41, []), Cancel);
        await server.TakeQuest(c.Id, "mossy-keepers", Cancel);
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            ch.Quests += ",mossy-keepers";
            ch.QuestLog = "";
            return db.SaveChanges();
        });
        CharacterSummary roots = await server.TakeQuest(c.Id, "roots-of-the-mother-tree", Cancel);
        Assert.Equal(1, roots.Inventory!.Where(i => i.Item == "mother-tree-roots-key").Sum(i => i.Count));
    }
}
