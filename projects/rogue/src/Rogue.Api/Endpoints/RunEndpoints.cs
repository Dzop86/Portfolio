using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Rogue.Api.Data;
using Rogue.Core;

namespace Rogue.Api.Endpoints;

public static class RunEndpoints
{
    public static readonly TimeSpan TicketLifetime = TimeSpan.FromHours(24);

    public static void MapRunEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/runs").WithTags("Runs").RequireAuthorization();
        group.MapPost("/", StartRun)
            .WithSummary("Starts a ranked run: the server draws its seed.");
        group.MapPost("/{id:guid}/submission", Submit)
            .WithSummary("Submits a finished run: the server replays it and records the score it computes.");
        group.MapGet("/", MyRuns)
            .WithSummary("Lists my runs, newest first.");
    }

    internal static async Task<Created<RunTicket>> StartRun(ClaimsPrincipal user, ScoresDb db, TimeProvider clock, CancellationToken cancel)
    {
        DateTimeOffset now = clock.GetUtcNow();
        var run = new RankedRun
        {
            PlayerId = PlayerId(user),
            // Drawn by a cryptographic generator: a player cannot guess the next seed and prepare a run for it.
            Seed = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong))),
            IssuedAt = now,
            ExpiresAt = now + TicketLifetime,
            Status = RunStatus.Open,
        };
        db.Runs.Add(run);
        await db.SaveChangesAsync(cancel);
        return TypedResults.Created($"/api/runs/{run.Id}", new RunTicket(run.Id, SeedText(run.Seed), run.ExpiresAt));
    }

    /// <summary>
    /// Malformed bodies, another seed and unfinished runs are refused without closing the run (a
    /// client mistake); a run the rules refuse closes it, like a scored one: one verdict per seed.
    /// </summary>
    internal static async Task<Results<Ok<RunResult>, ProblemHttpResult>> Submit(
        Guid id, RunSubmission submission, ClaimsPrincipal user, ScoresDb db, TimeProvider clock, CancellationToken cancel)
    {
        Guid playerId = PlayerId(user);
        // Somebody else's run is reported as missing: its existence is none of the caller's business.
        RankedRun? run = await db.Runs.SingleOrDefaultAsync(r => r.Id == id && r.PlayerId == playerId, cancel);
        if (run is null)
            return Problem(StatusCodes.Status404NotFound, "No such run.");
        if (run.Status != RunStatus.Open)
            return Problem(StatusCodes.Status409Conflict, "This run has already been submitted.");
        DateTimeOffset now = clock.GetUtcNow();
        if (now >= run.ExpiresAt)
            return Problem(StatusCodes.Status410Gone, "This run has expired.");

        RunRecord record;
        try
        {
            record = RunRecord.FromParts(submission.Format, submission.Version, submission.Seed, submission.Actions);
        }
        catch (RunFormatException e)
        {
            return Problem(StatusCodes.Status400BadRequest, e.Message, e.Error);
        }
        if (record.Seed != run.Seed)
            return Problem(StatusCodes.Status422UnprocessableEntity, "This is not the seed of this run.", error: "SeedMismatch");

        ReplayResult result = Replay.Run(record);
        if (result.IsValid && result.State == GameState.Playing)
            return Problem(StatusCodes.Status422UnprocessableEntity, "The run is not over.", error: "Unfinished");

        run.SubmittedAt = now;
        run.Actions = record.Actions;
        run.Outcome = result.State;
        run.Depth = result.Depth;
        run.Turns = result.Turns;
        run.Kills = result.Kills;
        if (result.IsValid)
        {
            run.Status = RunStatus.Scored;
            run.Score = result.Score;
        }
        else
        {
            run.Status = RunStatus.Rejected;
            run.Rejection = result.ActionIndex is int i ? $"{result.Error} at action {i}" : result.Error.ToString();
        }
        try
        {
            await db.SaveChangesAsync(cancel);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Two submissions of the same run at the same time: the first one saved wins.
            return Problem(StatusCodes.Status409Conflict, "This run has already been submitted.");
        }
        if (!result.IsValid)
            return Problem(StatusCodes.Status422UnprocessableEntity, "The rules refuse this run.", result.Error, result.ActionIndex);
        return TypedResults.Ok(new RunResult(run.Id, result.State, result.Score, result.Depth, result.Turns, result.Kills));
    }

    internal static async Task<Ok<List<MyRun>>> MyRuns(ClaimsPrincipal user, ScoresDb db, CancellationToken cancel)
    {
        Guid playerId = PlayerId(user);
        List<RankedRun> runs = await db.Runs.AsNoTracking()
            .Where(r => r.PlayerId == playerId)
            .OrderByDescending(r => r.IssuedAt)
            .Take(50)
            .ToListAsync(cancel);
        return TypedResults.Ok(runs.Select(r =>
            new MyRun(r.Id, SeedText(r.Seed), r.Status, r.IssuedAt, r.ExpiresAt, r.Outcome, r.Score, r.Rejection)).ToList());
    }

    private static Guid PlayerId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? throw new InvalidOperationException("Token without subject."));

    private static string SeedText(ulong seed) => seed.ToString(CultureInfo.InvariantCulture);

    private static ProblemHttpResult Problem(int status, string title, object? error = null, int? actionIndex = null)
    {
        var extensions = new Dictionary<string, object?>();
        if (error is not null)
            extensions["error"] = error.ToString();
        if (actionIndex is not null)
            extensions["actionIndex"] = actionIndex;
        return TypedResults.Problem(statusCode: status, title: title, extensions: extensions);
    }
}
