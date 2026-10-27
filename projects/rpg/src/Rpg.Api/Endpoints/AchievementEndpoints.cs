using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
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
    }

    /// <summary>What the achievements read of a character.</summary>
    internal static async Task<AchievementState> StateOf(Character c, GameDb db, CancellationToken cancel) =>
        new(c.WaystoneList, c.BestiaryList, c.JobList.ToDictionary(j => j.Key, j => Crafting.LevelFor(j.Value), StringComparer.Ordinal), c.QuestList,
            c.ContractsDone, c.TalentList, await db.GuildMembers.AnyAsync(m => m.CharacterId == c.Id, cancel), c.SharedWins, c.FeatList);

    private static async Task<AchievementView> ViewOf(Character c, GameDb db, CancellationToken cancel)
    {
        IReadOnlyList<AchievementProgress> progress = Achievements.Progress(await StateOf(c, db, cancel), GameData.Embedded);
        return new AchievementView(progress, Achievements.Points(progress.Where(p => p.Earned).Select(p => p.Id)), c.Title);
    }

    private static Task<Character?> Mine(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = CharacterEndpoints.AccountId(user);
        return db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
    }

    internal static async Task<Results<Ok<AchievementView>, NotFound>> Of(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel) =>
        await Mine(id, user, db, cancel) is Character c ? TypedResults.Ok(await ViewOf(c, db, cancel)) : TypedResults.NotFound();

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
}
