using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>"Keep or dig" by the API (sprint 232, D103): the side taken is kept by the server; the rival quest is refused, then and after.</summary>
public class KeepOrDigTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => (await Assert.ThrowsAsync<ServerException>(call)).Status;

    [Fact]
    public async Task TakingBerthesSide_ShutsBeroldsWall_WhileUnderWay_AndOnceHandedBack()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("kod");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "warrior", cancel: Cancel);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 46, []), Cancel);
        api.WithDb(db =>
        {
            db.Characters.Single().Quests = "roots-of-the-mother-tree";
            return db.SaveChanges();
        });
        await server.TakeQuest(c.Id, "widen-the-passage", Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.TakeQuest(c.Id, "wall-the-passage", Cancel)));
        api.WithDb(db =>
        {
            Character ch = db.Characters.Single();
            ch.Quests += ",widen-the-passage";
            ch.QuestLog = "";
            return db.SaveChanges();
        });
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.TakeQuest(c.Id, "wall-the-passage", Cancel)));
        // Berthe's side goes on, from level 48.
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 48, []), Cancel);
        await server.TakeQuest(c.Id, "follow-the-worms", Cancel);
    }
}
