using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api.Endpoints;

/// <summary>
/// The fights with the party (sprint 120, D74): opened on a group of monsters, joined, started, played action by
/// action; the server leads them (<see cref="Battles"/>) and tells each step on the map's socket (<see cref="SharedFightNews"/>).
/// </summary>
public static class BattleEndpoints
{
    public static void MapBattleEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/characters").WithTags("Fights with the party").RequireAuthorization();
        group.MapPost("/{id:guid}/battles", Open).WithSummary("Opens a fight with the party on a group of monsters of the character's map; its members on the map may join for 15 seconds.");
        group.MapPost("/{id:guid}/battles/{fight:guid}/join", Join).WithSummary("Joins a fight a member of the party opened, before it starts.");
        group.MapPost("/{id:guid}/battles/{fight:guid}/go", Go).WithSummary("Starts the fight one opened without waiting the 15 seconds.");
        group.MapPost("/{id:guid}/battles/{fight:guid}/act", Act).WithSummary("Plays an action of the character's hero, in its turn (30 seconds at most).");
        group.MapGet("/{id:guid}/battles/{fight:guid}", Now).WithSummary("The fight's start and every action so far, for a player coming back.");
    }

    private static ValidationProblem Refused(string why) => TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["fight"] = [why] });

    private static Task<Character?> Mine(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = CharacterEndpoints.AccountId(user);
        return db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
    }

    internal static async Task<Results<Ok<SharedFightNews>, NotFound, ValidationProblem>> Open(
        Guid id, NewSharedFight request, ClaimsPrincipal user, GameDb db, Battles battles, Parties parties, Hunts hunts, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (request?.Scenario is not string scenario || Hunts.GroupOf(scenario) is not MonsterGroup group)
            return Refused("A group of monsters of the world.");
        if (parties.Of(id) is not (_, IReadOnlyList<Guid> party))
            return Refused("Not in a party: a fight alone is started as before.");
        if (hub.MapOf(id) != group.Map)
            return Refused("Not on the group's map.");
        if (battles.Of(id) is not null)
            return Refused("Already in a fight with the party.");
        if (!hunts.Start(group.Id, id))
            return Refused("This group is fighting someone else, or beaten and not back yet.");
        await hub.Tell(group.Map, new MapNews(MapNewsKind.Fighting, Group: group.Id), cancel: cancel);
        Guid fight = await battles.Open(id, me.Name, scenario, group.Map, group, party, cancel);
        return TypedResults.Ok(new SharedFightNews(SharedFightKind.Opened, fight, me.Name, scenario));
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Join(
        Guid id, Guid fight, ClaimsPrincipal user, GameDb db, Battles battles, Parties parties, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        return await battles.Join(fight, id, parties.Of(id)?.Members, cancel) is string why ? Refused(why) : TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Go(Guid id, Guid fight, ClaimsPrincipal user, GameDb db, Battles battles, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        return await battles.Go(fight, id, cancel) is string why ? Refused(why) : TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Act(
        Guid id, Guid fight, FightAction action, ClaimsPrincipal user, GameDb db, Battles battles, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        if (action is null)
            return Refused("An action.");
        return await battles.Act(fight, id, action, cancel) is string why ? Refused(why) : TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<SharedFightNews>, NotFound>> Now(Guid id, Guid fight, ClaimsPrincipal user, GameDb db, Battles battles, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        return battles.Now(fight, id) is SharedFightNews now ? TypedResults.Ok(now) : TypedResults.NotFound();
    }
}
