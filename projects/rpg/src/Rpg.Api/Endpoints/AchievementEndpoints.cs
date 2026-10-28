using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api.Endpoints;

/// <summary>
/// The achievements and titles (sprint 155, D83): read from what the server keeps of a character, so retroactive; a title
/// chosen among those of the achievements earned, told to the players of its map.
/// </summary>
public static class AchievementEndpoints
{
    public static void MapAchievementEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/characters").WithTags("Achievements").RequireAuthorization();
        group.MapGet("/{id:guid}/achievements", Of).WithSummary("The character's achievements: how far each one has gone, the points, the title worn.");
        group.MapPost("/{id:guid}/title", Wear).WithSummary("Wears the title of an achievement earned, or none.");
        group.MapPost("/{id:guid}/achievements/{achievement}/claim", Claim).WithSummary("Claims an achievement's reward (experience and Éclats at its reference level), once.");
        group.MapPost("/{id:guid}/achievements/claim", ClaimAll).WithSummary("Claims the reward of every achievement earned and not yet claimed, at once.");
    }

    /// <summary>What the achievements read of a character.</summary>
    internal static async Task<AchievementState> StateOf(Character c, GameDb db, CancellationToken cancel) =>
        StateOf(c, await db.GuildMembers.AnyAsync(m => m.CharacterId == c.Id, cancel));

    /// <summary>The same, its guild known (sprint 160: a guild's members, their achievement points).</summary>
    internal static AchievementState StateOf(Character c, bool inGuild) =>
        new(c.WaystoneList, c.BestiaryList, c.JobList.ToDictionary(j => j.Key, j => Crafting.LevelFor(j.Value), StringComparer.Ordinal), c.QuestList,
            c.ContractsDone, c.TalentList, inGuild, c.SharedWins, c.FeatList);

    private static async Task<AchievementView> ViewOf(Character c, GameDb db, CancellationToken cancel)
    {
        IReadOnlyList<AchievementProgress> progress = Achievements.Progress(await StateOf(c, db, cancel), GameData.Embedded);
        // The quests done that a line of quests asks (sprint 159), for the window's ticks.
        var asked = Achievements.All.Where(a => a.Goal == AchievementGoal.Quests).SelectMany(a => a.Targets ?? []).ToHashSet(StringComparer.Ordinal);
        return new AchievementView(progress, Achievements.Points(progress.Where(p => p.Earned).Select(p => p.Id)), c.Title, c.ClaimedList,
            [.. c.QuestList.Where(asked.Contains)]);
    }

    private static Task<Character?> Mine(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = CharacterEndpoints.AccountId(user);
        return db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
    }

    internal static async Task<Results<Ok<AchievementView>, NotFound>> Of(Guid id, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character c)
            return TypedResults.NotFound();
        // Those not yet counted for the guild count now (sprint 156).
        await GuildEndpoints.CreditAchievements(c, db, hub, clock, cancel);
        return TypedResults.Ok(await ViewOf(c, db, cancel));
    }

    internal static async Task<Results<Ok<AchievementView>, NotFound, ValidationProblem>> Wear(
        Guid id, TitleOrder request, ClaimsPrincipal user, GameDb db, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character c)
            return TypedResults.NotFound();
        IReadOnlyList<string> earned = Achievements.Earned(await StateOf(c, db, cancel), GameData.Embedded);
        if (Achievements.WhyNotTitle(request?.Achievement, [.. earned]) is string why)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["achievement"] = [why] });
        c.Title = request?.Achievement;
        await db.SaveChangesAsync(cancel);
        await hub.SetTitle(id, c.Title, cancel);
        return TypedResults.Ok(await ViewOf(c, db, cancel));
    }

    internal static Task<Results<Ok<AchievementClaim>, NotFound, ValidationProblem, Conflict<ProblemDetails>>> Claim(
        Guid id, string achievement, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel) =>
        Claiming(id, user, db, clock, achievement, cancel);

    internal static Task<Results<Ok<AchievementClaim>, NotFound, ValidationProblem, Conflict<ProblemDetails>>> ClaimAll(
        Guid id, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel) =>
        Claiming(id, user, db, clock, null, cancel);

    /// <summary>
    /// Claims one achievement, or every one claimable (sprint 159, D84, the game designer's §4): earned (counted again here) and
    /// not claimed; its experience and Éclats at its reference level, the levels gained as after a fight, its id kept; out of
    /// fights; the character's row locked, so that two claims at once pay once.
    /// </summary>
    private static async Task<Results<Ok<AchievementClaim>, NotFound, ValidationProblem, Conflict<ProblemDetails>>> Claiming(
        Guid id, ClaimsPrincipal user, GameDb db, TimeProvider clock, string? achievement, CancellationToken cancel)
    {
        Guid account = CharacterEndpoints.AccountId(user);
        await using var transaction = await db.Database.BeginTransactionAsync(cancel);
        // Read under the lock first, so that the context does not hold an older copy.
        Character? c = (await db.Characters.FromSql($"SELECT * FROM characters WHERE \"Id\" = {id} FOR UPDATE").ToListAsync(cancel)).SingleOrDefault();
        if (c is null || c.AccountId != account)
            return TypedResults.NotFound();
        if (c.InFight)
            return Refused("Not during a fight.");
        IReadOnlyList<string> earned = Achievements.Earned(await StateOf(c, db, cancel), GameData.Embedded);
        string[] claimed = c.ClaimedList;
        string[] taken;
        if (achievement is not null)
        {
            if (Achievements.All.All(a => a.Id != achievement))
                return TypedResults.NotFound();
            if (claimed.Contains(achievement))
                return TypedResults.Conflict(new ProblemDetails { Title = "Already claimed." });
            if (!earned.Contains(achievement))
                return Refused("Not earned yet.");
            taken = [achievement];
        }
        else
            taken = [.. Achievements.Claimable(earned, claimed)];
        if (taken.Length == 0)
            return Refused("Nothing to claim.");
        AchievementReward[] rewards = [.. taken.Select(t => Achievements.Reward(Achievements.All.Single(a => a.Id == t)))];
        long xp = rewards.Sum(r => (long)r.Xp), shards = rewards.Sum(r => (long)r.Shards);
        c.Xp += xp;
        c.Shards += shards;
        int before = c.Level;
        c.Level = Progression.LevelFor(c.Xp);
        if (c.Level > before)
            c.SetLife(int.MaxValue, clock.GetUtcNow());
        c.ClaimedAchievements = string.Join(',', claimed.Concat(taken));
        await db.SaveChangesAsync(cancel);
        await transaction.CommitAsync(cancel);
        return TypedResults.Ok(new AchievementClaim(xp, shards, CharacterEndpoints.Summary(c), await ViewOf(c, db, cancel)));
    }

    private static ValidationProblem Refused(string why) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["achievement"] = [why] });
}
