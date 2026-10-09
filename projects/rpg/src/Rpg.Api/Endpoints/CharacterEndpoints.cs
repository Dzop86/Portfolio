using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api.Endpoints;

public static class CharacterEndpoints
{
    public static void MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/characters").WithTags("Characters").RequireAuthorization();
        group.MapGet("", List).WithSummary("The signed-in player's characters, oldest first.");
        group.MapPost("", Create).WithSummary($"Creates a character (at most {Accounts.MaxCharacters} per account, a name unique on the server).");
        group.MapDelete("/{id:guid}", Delete).WithSummary("Deletes one of the signed-in player's characters.");
        group.MapPut("/{id:guid}/place", SavePlace).WithSummary("Saves where one of the player's characters stands in a town (a cell they can walk to).");
    }

    /// <summary>The account in the token; tokens of a deleted account no longer match any.</summary>
    private static Guid AccountId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid id) ? id : Guid.Empty;

    internal static async Task<Ok<CharacterSummary[]>> List(ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        var list = await db.Characters.Where(c => c.AccountId == account)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Look, c.Class, c.Colour, c.CreatedAt, c.Town, c.X, c.Y, c.Server, c.Level })
            .ToArrayAsync(cancel);
        return TypedResults.Ok(list.Select(c => new CharacterSummary(c.Id, c.Name, c.Look, c.Class, c.Colour, c.CreatedAt,
            c.Town is string town && c.X is int x && c.Y is int y ? new Place(town, x, y) : null, c.Server, c.Level)).ToArray());
    }

    internal static async Task<Results<Created<CharacterSummary>, ValidationProblem, ProblemHttpResult>> Create(
        NewCharacter request, ClaimsPrincipal user, GameDb db, GameServers servers, TimeProvider clock, CancellationToken cancel)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Hero.IsValidName(request.Name))
            errors["name"] = ["3 to 20 letters, with single hyphens or apostrophes inside."];
        if (request.Look is null || !Hero.Looks.Contains(request.Look))
            errors["look"] = [$"One of: {string.Join(", ", Hero.Looks)}."];
        // Every new character has a class; the classes come from the rules' own data.
        if (GameData.Embedded.Class(request.Class) is null)
            errors["class"] = [$"One of: {string.Join(", ", GameData.Embedded.Classes.Select(c => c.Id))}."];
        if (request.Colour is < 0 or >= Hero.Colours)
            errors["colour"] = [$"0 to {Hero.Colours - 1}."];
        string server = request.Server ?? servers.All[0].Id;
        if (!servers.Exists(server))
            errors["server"] = [$"One of: {string.Join(", ", servers.All.Select(s => s.Id))}."];
        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        Guid account = AccountId(user);
        if (!await db.Accounts.AnyAsync(a => a.Id == account, cancel))
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "This account no longer exists.");
        if (await db.Characters.CountAsync(c => c.AccountId == account, cancel) >= Accounts.MaxCharacters)
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: $"At most {Accounts.MaxCharacters} characters per account.");
        string normalized = request.Name!.ToUpperInvariant();
        if (await db.Characters.AnyAsync(c => c.NormalizedName == normalized, cancel))
            return NameTaken();
        var character = new Character { AccountId = account, Name = request.Name, NormalizedName = normalized, Look = request.Look!, Class = request.Class!, Colour = request.Colour, Server = server, CreatedAt = clock.GetUtcNow() };
        db.Characters.Add(character);
        try
        {
            await db.SaveChangesAsync(cancel);
        }
        catch (DbUpdateException)
        {
            // The same name taken at the same moment by someone else: the unique index decides.
            return NameTaken();
        }
        return TypedResults.Created($"/api/characters/{character.Id}", new CharacterSummary(character.Id, character.Name, character.Look, character.Class, character.Colour, character.CreatedAt, null, character.Server, character.Level));
    }

    internal static async Task<Results<NoContent, NotFound>> Delete(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        // Someone else's character answers like a missing one: ids of other players stay unknown.
        int deleted = await db.Characters.Where(c => c.Id == id && c.AccountId == account).ExecuteDeleteAsync(cancel);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> SavePlace(
        Guid id, Place place, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        // The rules' own towns decide: a known town, a cell the player could walk to from the arrival.
        if (place?.Town is null || !GameData.Embedded.Towns.TryGetValue(place.Town, out Town? town) || !town.CanStand(place.Cell))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["place"] = ["A known town and a cell one can walk to there."] });
        Guid account = AccountId(user);
        int saved = await db.Characters.Where(c => c.Id == id && c.AccountId == account)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.Town, place.Town).SetProperty(c => c.X, place.X).SetProperty(c => c.Y, place.Y), cancel);
        return saved == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    private static ProblemHttpResult NameTaken() =>
        TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "This character name is already taken.");
}
