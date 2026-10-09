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
        group.MapPost("/{id:guid}/fights", StartFight).WithSummary("Draws the seed of a fight one of the player's characters is about to play.");
        group.MapPost("/{id:guid}/fights/{fight:guid}", ReportFight)
            .Accepts<FightRecord>("application/json")
            .WithSummary("Replays the record of a fight the server started, once, and gives the experience it earned.");
        group.MapPut("/{id:guid}/equipment", SaveEquipment).WithSummary("Puts on a character the items it owns, each in a slot it fits, within its level.");
        group.MapPut("/{id:guid}/points", SavePoints).WithSummary("Spends a character's characteristic and spell points, within what its level gives.");
    }

    /// <summary>The account in the token; tokens of a deleted account no longer match any.</summary>
    private static Guid AccountId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid id) ? id : Guid.Empty;

    internal static async Task<Ok<CharacterSummary[]>> List(ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        var list = await db.Characters.Where(c => c.AccountId == account)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Name)
            .ToArrayAsync(cancel);
        return TypedResults.Ok(list.Select(c => Summary(c, c.Town is string town && c.X is int x && c.Y is int y ? new Place(town, x, y) : null)).ToArray());
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
        if (request.Hair is < 0 or >= Hero.HairColours)
            errors["hair"] = [$"0 to {Hero.HairColours - 1}."];
        if (request.Skin is < 0 or >= Hero.SkinTones)
            errors["skin"] = [$"0 to {Hero.SkinTones - 1}."];
        if (Math.Abs(request.Height) > Hero.Shape)
            errors["height"] = [$"-{Hero.Shape} to {Hero.Shape}."];
        if (Math.Abs(request.Build) > Hero.Shape)
            errors["build"] = [$"-{Hero.Shape} to {Hero.Shape}."];
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
        var character = new Character { AccountId = account, Name = request.Name, NormalizedName = normalized, Look = request.Look!, Class = request.Class!, Colour = request.Colour, Server = server, Hair = request.Hair, Skin = request.Skin, Height = request.Height, Build = request.Build, CreatedAt = clock.GetUtcNow() };
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
        return TypedResults.Created($"/api/characters/{character.Id}", Summary(character, null));
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

    internal static async Task<Results<Ok<FightTicket>, NotFound, ValidationProblem>> StartFight(
        Guid id, NewFight request, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel)
    {
        if (request?.Scenario is not string scenario || !GameData.Embedded.Scenarios.ContainsKey(scenario))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["scenario"] = [$"One of: {string.Join(", ", GameData.Embedded.Scenarios.Keys)}."] });
        Guid account = AccountId(user);
        if (!await db.Characters.AnyAsync(c => c.Id == id && c.AccountId == account, cancel))
            return TypedResults.NotFound();
        // The server draws the seed: the player cannot pick the rolls.
        var pending = new PendingFight { CharacterId = id, Scenario = scenario, Seed = BitConverter.ToInt64(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)), CreatedAt = clock.GetUtcNow() };
        db.PendingFights.Add(pending);
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(new FightTicket(pending.Id, scenario, unchecked((ulong)pending.Seed)));
    }

    internal static async Task<Results<Ok<FightResult>, NotFound, ValidationProblem>> ReportFight(
        Guid id, Guid fight, HttpRequest request, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        PendingFight? pending = character is null ? null : await db.PendingFights.FirstOrDefaultAsync(f => f.Id == fight && f.CharacterId == id, cancel);
        if (character is null || pending is null)
            return TypedResults.NotFound();
        // A ticket serves once, whatever the record says.
        db.PendingFights.Remove(pending);
        await db.SaveChangesAsync(cancel);
        using var reader = new StreamReader(request.Body);
        string? problem;
        Fight? played = null;
        try
        {
            FightRecord record = FightRecord.FromJson(await reader.ReadToEndAsync(cancel));
            problem = record.Scenario != pending.Scenario || record.Seed != unchecked((ulong)pending.Seed) ? "Not the fight the server started."
                : record.Hero != character.Hero || record.Rival is not null ? "Not this character's fight."
                : null;
            if (problem is null)
            {
                played = record.Replay(GameData.Embedded);
                problem = played.IsOver ? null : "The fight is not over.";
            }
        }
        catch (InvalidFightRecordException e)
        {
            problem = e.Message;
        }
        if (problem is not null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["record"] = [problem] });
        long xp = Progression.FightXp(played!);
        Quest? quest = played!.WinningTeam == 0
            ? GameData.Embedded.Quests.Values.FirstOrDefault(q => q.Scenario == pending.Scenario && !character.QuestList.Contains(q.Id))
            : null;
        // The loot follows the fight's seed, which the server drew: the game cannot pick it either.
        List<ItemCount> items = [.. Equipment.Loot(played)];
        if (quest is not null)
        {
            xp += quest.Xp;
            character.Quests = string.Join(',', character.QuestList.Append(quest.Id));
            items.AddRange(quest.Items ?? []);
        }
        character.Receive(items);
        character.Xp += xp;
        character.Level = Progression.LevelFor(character.Xp);
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(new FightResult(xp, character.Xp, character.Level, quest?.Id, items));
    }

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> SaveEquipment(
        Guid id, Wear request, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        var worn = new Dictionary<Slot, string>(request?.Worn ?? new Dictionary<Slot, string>());
        // Only what it owns: two rings alike need two in the inventory.
        Dictionary<string, int> owned = character.InventoryList.ToDictionary(i => i.Item, i => i.Count, StringComparer.Ordinal);
        string? problem = worn.Values.GroupBy(i => i).FirstOrDefault(g => g.Count() > owned.GetValueOrDefault(g.Key)) is IGrouping<string, string> missing
            ? $"'{missing.Key}': not owned, or not that many."
            : (character.Hero with { Worn = worn }).Problem(GameData.Embedded);
        if (problem is not null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["equipment"] = [problem] });
        character.Worn = string.Join(',', worn.OrderBy(w => w.Key).Select(w => $"{w.Key}:{w.Value}"));
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> SavePoints(
        Guid id, Points points, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        Characteristics stats = points?.Stats ?? Characteristics.None;
        var ranks = new Dictionary<string, int>(points?.Ranks ?? new Dictionary<string, int>(), StringComparer.Ordinal);
        // The rules decide: within the level's points, ranks of the class's unlocked spells.
        if ((character.Hero with { Stats = stats, Ranks = ranks }).Problem(GameData.Embedded) is string problem)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["points"] = [problem] });
        (character.Vitality, character.Strength, character.Intelligence, character.Chance, character.Agility) = (stats.Vitality, stats.Strength, stats.Intelligence, stats.Chance, stats.Agility);
        character.Ranks = string.Join(',', ranks.Where(r => r.Value > 1).OrderBy(r => r.Key, StringComparer.Ordinal).Select(r => $"{r.Key}:{r.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    private static CharacterSummary Summary(Character c, Place? place) =>
        new(c.Id, c.Name, c.Look, c.Class, c.Colour, c.CreatedAt, place, c.Server, c.Level, c.Hair, c.Skin, c.Height, c.Build, c.Xp, c.Stats, c.RankList, c.QuestList, c.InventoryList, c.WornList);

    private static ProblemHttpResult NameTaken() =>
        TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "This character name is already taken.");
}
