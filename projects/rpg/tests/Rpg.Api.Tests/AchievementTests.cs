using System.Net;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The achievements on the server (sprint 155, D83): read from what is kept, so retroactive; a title worn only once earned;
/// a contract handed back and a trade counted.
/// </summary>
public class AchievementTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => (await Assert.ThrowsAsync<ServerException>(call)).Status;

    [Fact]
    public async Task WhatIsKept_IsCounted_AndATitle_IsWornOnlyOnceEarned()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("succes");
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        AchievementView fresh = await server.Achievements(c.Id, Cancel);
        Assert.Equal(Achievements.All.Count, fresh.Progress.Count);
        Assert.DoesNotContain(fresh.Progress, p => p.Earned);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.WearTitle(c.Id, "chapter-1", Cancel)));
        // Chapter 1 done before the achievements existed: earned all the same.
        api.WithDb(db =>
        {
            Rpg.Api.Data.Character ch = db.Characters.Single();
            ch.Quests = "the-registry,the-disjoint";
            ch.ContractsDone = 10;
            ch.Feat(Achievements.Traded);
            return db.SaveChanges();
        });
        AchievementView after = await server.Achievements(c.Id, Cancel);
        string[] earned = [.. after.Progress.Where(p => p.Earned).Select(p => p.Id)];
        Assert.Contains("chapter-1", earned);
        Assert.Contains("contracts-10", earned);
        Assert.Contains("trader", earned);
        Assert.Equal(Achievements.Points(earned), after.Points);
        AchievementView worn = await server.WearTitle(c.Id, "chapter-1", Cancel);
        Assert.Equal("chapter-1", worn.Title);
        // A plain achievement gives no title; none at all is always allowed.
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.WearTitle(c.Id, "trader", Cancel)));
        Assert.Null((await server.WearTitle(c.Id, null, Cancel)).Title);
    }
}
