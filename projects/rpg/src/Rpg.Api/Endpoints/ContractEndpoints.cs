using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api.Endpoints;

/// <summary>
/// The notice board (sprint 153, D83, the game designer's §12.4): three contracts a day and a character, kept up to nine,
/// renewed at dawn on the server's clock; handed back at a board for experience and Éclats, a change free once a day.
/// </summary>
public static class ContractEndpoints
{
    /// <summary>The look of a notice board on a map: Clairval's square and Quatre-Chemins's.</summary>
    public const string BoardLook = "notice-board";

    public static void MapContractEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/characters").WithTags("Contracts").RequireAuthorization();
        group.MapGet("/{id:guid}/contracts", Board).WithSummary("The character's notice board: three new contracts a day, nine at most.");
        group.MapPost("/{id:guid}/contracts/hand-in", HandIn).WithSummary("Hands a contract back at a notice board, for a quarter of a level's experience and 5 × level Éclats (twice for a group's).");
        group.MapPost("/{id:guid}/contracts/change", Change).WithSummary("Changes a contract for another: the first of the day free, a group's always, then 5 × level Éclats.");
    }

    private static ValidationProblem Refused(string why) => TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["index"] = [why] });

    /// <summary>What the character can be sent to: the zones it reaches, its trades' levels.</summary>
    internal static Contracts.Reach ReachOf(Character c) =>
        new(Contracts.Zones(GameData.Embedded, c.QuestList), c.JobList.ToDictionary(j => j.Key, j => Crafting.LevelFor(j.Value), StringComparer.Ordinal));

    /// <summary>The character's board as of today, renewed if a dawn has passed (kept, not saved: the caller saves).</summary>
    internal static ContractBoard Today(Character c, TimeProvider clock)
    {
        ContractBoard board = Contracts.Renew(Contracts.Read(c.Contracts), Contracts.Day(clock.GetUtcNow()), c.Id, c.Level, ReachOf(c), GameData.Embedded);
        c.Contracts = Contracts.Write(board);
        return board;
    }

    /// <summary>The contracts after a won fight (a hunt, a horde, a room), as the fight's results are kept.</summary>
    internal static void AfterFight(Character c, Fight fight, (string Dungeon, int Room)? roomWon)
    {
        ContractBoard board = Contracts.Read(c.Contracts);
        if (board.Offered.Count > 0 && fight.WinningTeam == 0)
            c.Contracts = Contracts.Write(board with { Offered = [.. board.Offered.Select(x => Contracts.AfterFight(x, GameData.Embedded, fight, roomWon))] });
    }

    private static Task<Character?> Mine(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = CharacterEndpoints.AccountId(user);
        return db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
    }

    internal static async Task<Results<Ok<ContractBoard>, NotFound>> Board(Guid id, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character c)
            return TypedResults.NotFound();
        ContractBoard board = Today(c, clock);
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(board);
    }

    internal static async Task<Results<Ok<ContractReward>, NotFound, ValidationProblem>> HandIn(
        Guid id, ContractOrder request, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character c)
            return TypedResults.NotFound();
        ContractBoard board = Today(c, clock);
        int index = request?.Index ?? -1;
        if (index < 0 || index >= board.Offered.Count)
            return Refused("No such contract on the board.");
        // At a board: on a map that has one.
        if (c.Town is not string map || !GameData.Embedded.Towns.TryGetValue(map, out Town? town) || !(town.Props ?? []).Any(p => p.Look == BoardLook))
            return Refused("Contracts are handed back at a notice board.");
        Contract contract = board.Offered[index];
        if (!Contracts.IsComplete(contract, c.InventoryList))
            return Refused("This contract is not fulfilled yet.");
        foreach (ItemCount taken in contract.Taken)
            c.Spend(taken);
        c.ContractsDone++;
        c.Xp += contract.Xp;
        c.Shards += contract.Shards;
        int before = c.Level;
        c.Level = Progression.LevelFor(c.Xp);
        if (c.Level > before)
            c.SetLife(int.MaxValue, clock.GetUtcNow());
        var left = board.Offered.ToList();
        left.RemoveAt(index);
        board = board with { Offered = left };
        c.Contracts = Contracts.Write(board);
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(new ContractReward(contract.Xp, contract.Shards, CharacterEndpoints.Summary(c), board));
    }

    internal static async Task<Results<Ok<ContractBoard>, NotFound, ValidationProblem>> Change(
        Guid id, ContractOrder request, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character c)
            return TypedResults.NotFound();
        ContractBoard board = Today(c, clock);
        int index = request?.Index ?? -1;
        if (index < 0 || index >= board.Offered.Count)
            return Refused("No such contract on the board.");
        long price = Contracts.ChangePrice(board, board.Offered[index], c.Level);
        if (c.Shards < price)
            return Refused($"Changing this contract costs {price} Éclats.");
        c.Shards -= price;
        board = Contracts.Change(board, index, c.Id, c.Level, ReachOf(c), GameData.Embedded);
        c.Contracts = Contracts.Write(board);
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(board);
    }
}
