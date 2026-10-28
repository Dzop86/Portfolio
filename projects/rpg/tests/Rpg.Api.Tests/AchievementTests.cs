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

    [Fact]
    public async Task AnAchievementsReward_IsClaimedOnce_EarnedOnly_AndEveryOneAtOnce()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("reclame");
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ClaimAchievement(c.Id, "woods-all", Cancel)));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ClaimAchievement(c.Id, null, Cancel)));
        // Clairval's first steps, done before the rewards existed: claimable all the same.
        string[] steps = [.. Achievements.All.Single(a => a.Id == "clairval-first-steps").Targets!];
        api.WithDb(db =>
        {
            Rpg.Api.Data.Character ch = db.Characters.Single();
            ch.Quests = string.Join(',', steps);
            ch.ContractsDone = 10;
            return db.SaveChanges();
        });
        long shards = api.WithDb(db => db.Characters.Single().Shards);
        AchievementClaim first = await server.ClaimAchievement(c.Id, "clairval-first-steps", Cancel);
        Assert.Equal((50, 25), (first.Xp, first.Shards));
        Assert.Equal(50, first.Character.Xp);
        Assert.Equal(["clairval-first-steps"], first.Achievements.Claimed!);
        Assert.Equal(HttpStatusCode.Conflict, await Refused(() => server.ClaimAchievement(c.Id, "clairval-first-steps", Cancel)));
        // Every other one earned at once: quests-10 is not (six quests), contracts-10 is.
        AchievementClaim all = await server.ClaimAchievement(c.Id, null, Cancel);
        AchievementReward contracts = Achievements.Reward(Achievements.All.Single(a => a.Id == "contracts-10"));
        Assert.Equal((contracts.Xp, contracts.Shards), ((int)all.Xp, (int)all.Shards));
        Assert.Equal(shards + 25 + contracts.Shards, api.WithDb(db => db.Characters.Single().Shards));
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.ClaimAchievement(c.Id, null, Cancel)));
        Assert.Equal(HttpStatusCode.NotFound, await Refused(() => server.ClaimAchievement(c.Id, "no-such", Cancel)));
    }

    [Fact]
    public async Task TwoClaimsAtOnce_PayOnce()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("deuxfois");
        CharacterSummary c = await server.CreateCharacter("Ronce", "female-c", "guard", cancel: Cancel);
        api.WithDb(db =>
        {
            db.Characters.Single().ContractsDone = 10;
            return db.SaveChanges();
        });
        Task<AchievementClaim>[] both = [server.ClaimAchievement(c.Id, "contracts-10", Cancel), server.ClaimAchievement(c.Id, "contracts-10", Cancel)];
        int paid = 0;
        foreach (Task<AchievementClaim> t in both)
        {
            try
            {
                await t;
                paid++;
            }
            catch (ServerException e)
            {
                Assert.Equal(HttpStatusCode.Conflict, e.Status);
            }
        }
        Assert.Equal(1, paid);
        Assert.Equal(Achievements.Reward(Achievements.All.Single(a => a.Id == "contracts-10")).Xp, api.WithDb(db => db.Characters.Single().Xp));
    }
}
