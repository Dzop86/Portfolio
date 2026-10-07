using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Rogue.Api.Data;

namespace Rogue.Api.Endpoints;

public static class ScoreEndpoints
{
    public static void MapScoreEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/scores", Leaderboard)
            .WithTags("Scores")
            .WithSummary("The leaderboard: each player's best scored run, best first.");
    }

    /// <summary>Ties go to the shorter run, then to the earlier one.</summary>
    internal static async Task<Results<Ok<List<ScoreEntry>>, ValidationProblem>> Leaderboard(ScoresDb db, CancellationToken cancel, int limit = 10)
    {
        if (limit is < 1 or > 100)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["limit"] = ["Between 1 and 100."] });
        // DISTINCT ON keeps the first row of each player in the given order: their best run.
        var best = await db.Runs.FromSql($"""
                SELECT DISTINCT ON ("PlayerId") * FROM runs
                WHERE "Status" = 'Scored'
                ORDER BY "PlayerId", "Score" DESC, "Turns", "SubmittedAt"
                """)
            .AsNoTracking()
            .OrderByDescending(r => r.Score).ThenBy(r => r.Turns).ThenBy(r => r.SubmittedAt)
            .Take(limit)
            .Select(r => new { r.Player.Name, r.Score, r.Outcome, r.Depth, r.Turns, r.SubmittedAt })
            .ToListAsync(cancel);
        return TypedResults.Ok(best.Select((r, i) =>
            new ScoreEntry(i + 1, r.Name, r.Score!.Value, r.Outcome!.Value, r.Depth!.Value, r.Turns!.Value, r.SubmittedAt!.Value)).ToList());
    }
}
