using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rogue.Api.Data;

namespace Rogue.Api.Endpoints;

public static partial class AccountEndpoints
{
    public const string RateLimitPolicy = "accounts";

    [GeneratedRegex("^[A-Za-z0-9_-]{3,20}$")]
    private static partial Regex NamePattern();

    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api").WithTags("Accounts").RequireRateLimiting(RateLimitPolicy);
        group.MapPost("/accounts", CreateAccount)
            .WithSummary("Creates an account.");
        group.MapPost("/tokens", CreateToken)
            .WithSummary("Signs in: exchanges a name and password for an access token.");
    }

    internal static async Task<Results<Created<AccountCreated>, ValidationProblem, ProblemHttpResult>> CreateAccount(
        Credentials credentials, ScoresDb db, IPasswordHasher<Player> hasher, TimeProvider clock, CancellationToken cancel)
    {
        var errors = new Dictionary<string, string[]>();
        if (credentials.Name is null || !NamePattern().IsMatch(credentials.Name))
            errors["name"] = ["3 to 20 characters: letters, digits, '-' or '_'."];
        if (credentials.Password is not { Length: >= 10 and <= 128 })
            errors["password"] = ["10 to 128 characters."];
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        string normalized = credentials.Name!.ToUpperInvariant();
        if (await db.Players.AnyAsync(p => p.NormalizedName == normalized, cancel))
            return NameTaken();
        var player = new Player { Name = credentials.Name, NormalizedName = normalized, CreatedAt = clock.GetUtcNow() };
        player.PasswordHash = hasher.HashPassword(player, credentials.Password!);
        db.Players.Add(player);
        try
        {
            await db.SaveChangesAsync(cancel);
        }
        catch (DbUpdateException)
        {
            // Two sign-ups with the same name at the same time: the unique index decides.
            return NameTaken();
        }
        return TypedResults.Created($"/api/players/{player.Name}", new AccountCreated(player.Name));
    }

    private static ProblemHttpResult NameTaken() =>
        TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "This name is already taken.");

    internal static async Task<Results<Ok<AccessToken>, ProblemHttpResult>> CreateToken(
        Credentials credentials, ScoresDb db, IPasswordHasher<Player> hasher, Tokens tokens, CancellationToken cancel)
    {
        string normalized = (credentials.Name ?? "").ToUpperInvariant();
        Player? player = await db.Players.SingleOrDefaultAsync(p => p.NormalizedName == normalized, cancel);
        // An unknown name costs as much as a wrong password: the timing does not tell which names exist.
        PasswordVerificationResult result = hasher.VerifyHashedPassword(player ?? Decoy, player?.PasswordHash ?? DecoyHash, credentials.Password ?? "");
        if (player is null || result == PasswordVerificationResult.Failed)
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unknown name or wrong password.");
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            player.PasswordHash = hasher.HashPassword(player, credentials.Password!);
            await db.SaveChangesAsync(cancel);
        }
        (string token, DateTimeOffset expires) = tokens.Issue(player);
        return TypedResults.Ok(new AccessToken(token, expires));
    }

    private static readonly Player Decoy = new() { Name = "-", NormalizedName = "-" };
    private static readonly string DecoyHash = new PasswordHasher<Player>().HashPassword(Decoy, Guid.NewGuid().ToString());
}
