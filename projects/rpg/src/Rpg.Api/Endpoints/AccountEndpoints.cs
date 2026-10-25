using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api.Endpoints;

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
        RouteGroupBuilder me = app.MapGroup("/api/account").WithTags("Accounts").RequireAuthorization();
        me.MapGet("/seen", SeenOf).WithSummary("What the signed-in player has seen once (the tree's tutorial), not to show it again.");
        me.MapPut("/seen/{flag}", MarkSeen).WithSummary("Marks something seen once on the account (one of the known flags).");
    }

    /// <summary>What an account may mark as seen (sprint 149, M6 a).</summary>
    public static readonly IReadOnlyList<string> Flags = [Seen.TreeTutorial];

    internal static async Task<Ok<string[]>> SeenOf(ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid id = CharacterEndpoints.AccountId(user);
        string seen = await db.Accounts.Where(a => a.Id == id).Select(a => a.Seen).SingleAsync(cancel);
        return TypedResults.Ok(seen.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }

    internal static async Task<Results<NoContent, ProblemHttpResult>> MarkSeen(string flag, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        if (!Flags.Contains(flag))
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Unknown flag '{flag}'.");
        Guid id = CharacterEndpoints.AccountId(user);
        Account account = await db.Accounts.SingleAsync(a => a.Id == id, cancel);
        string[] seen = account.Seen.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (!seen.Contains(flag))
        {
            account.Seen = string.Join(',', [.. seen, flag]);
            await db.SaveChangesAsync(cancel);
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Created<AccountCreated>, ValidationProblem, ProblemHttpResult>> CreateAccount(
        Credentials credentials, GameDb db, IPasswordHasher<Account> hasher, TimeProvider clock, CancellationToken cancel)
    {
        var errors = new Dictionary<string, string[]>();
        if (credentials.Name is null || !NamePattern().IsMatch(credentials.Name))
            errors["name"] = ["3 to 20 characters: letters, digits, '-' or '_'."];
        if (credentials.Password is not { Length: >= 10 and <= 128 })
            errors["password"] = ["10 to 128 characters."];
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        string normalized = credentials.Name!.ToUpperInvariant();
        if (await db.Accounts.AnyAsync(a => a.NormalizedName == normalized, cancel))
            return NameTaken();
        var account = new Account { Name = credentials.Name, NormalizedName = normalized, CreatedAt = clock.GetUtcNow() };
        account.PasswordHash = hasher.HashPassword(account, credentials.Password!);
        db.Accounts.Add(account);
        try
        {
            await db.SaveChangesAsync(cancel);
        }
        catch (DbUpdateException)
        {
            // Two sign-ups with the same name at the same time: the unique index decides.
            return NameTaken();
        }
        return TypedResults.Created($"/api/accounts/{account.Name}", new AccountCreated(account.Name));
    }

    private static ProblemHttpResult NameTaken() =>
        TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "This name is already taken.");

    internal static async Task<Results<Ok<AccessToken>, ProblemHttpResult>> CreateToken(
        Credentials credentials, GameDb db, IPasswordHasher<Account> hasher, Tokens tokens, CancellationToken cancel)
    {
        string normalized = (credentials.Name ?? "").ToUpperInvariant();
        Account? account = await db.Accounts.SingleOrDefaultAsync(a => a.NormalizedName == normalized, cancel);
        // An unknown name costs as much as a wrong password: the timing does not tell which names exist.
        PasswordVerificationResult result = hasher.VerifyHashedPassword(account ?? Decoy, account?.PasswordHash ?? DecoyHash, credentials.Password ?? "");
        if (account is null || result == PasswordVerificationResult.Failed)
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unknown name or wrong password.");
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = hasher.HashPassword(account, credentials.Password!);
            await db.SaveChangesAsync(cancel);
        }
        (string token, DateTimeOffset expires) = tokens.Issue(account);
        return TypedResults.Ok(new AccessToken(token, expires));
    }

    private static readonly Account Decoy = new() { Name = "-", NormalizedName = "-" };
    private static readonly string DecoyHash = new PasswordHasher<Account>().HashPassword(Decoy, Guid.NewGuid().ToString());
}
