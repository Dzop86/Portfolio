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
        group.MapPost("/{id:guid}/dungeon", Enter).WithSummary("Takes one of the player's characters into a dungeon, with its key (used up) or the operator's keyring; back into the room it was at if it is already there.");
        group.MapPost("/{id:guid}/craft", Make).WithSummary("Makes items of a trade's recipe for one of the player's characters: the trade's level, the ingredients used up, the trade's experience.");
        group.MapPost("/{id:guid}/dungeon/rest", Rest).WithSummary("Heals one of the player's characters in its dungeon, at the fountain of a hall: all its hit points for the next room.");
        group.MapPost("/{id:guid}/quests", Take).WithSummary("Takes a quest an inhabitant gives, from its level and after the quests it follows.");
        group.MapPost("/{id:guid}/quests/{quest}/talk", Talk).WithSummary("Counts a talk to someone a quest under way asks to see.");
        group.MapPost("/{id:guid}/quests/{quest}/done", HandIn).WithSummary("Hands a quest back to its giver, its objectives reached: the items it asks are taken, its rewards given.");
        group.MapDelete("/{id:guid}/dungeon", Leave).WithSummary("Takes one of the player's characters out of its dungeon; the key is not given back.");
        group.Map("/{id:guid}/chat", Chat).WithSummary("The proximity chat of one of the player's characters, as a WebSocket: lines in, the lines said on its map out.");
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
            errors["name"] = ["3 to 20 letters and nothing else, the first a capital."];
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

    /// <summary>The chat's socket (sprint 84): for a character of the account, on the map the server knows it on.</summary>
    internal static async Task<IResult> Chat(Guid id, HttpContext context, ClaimsPrincipal user, GameDb db, ChatHub hub, CancellationToken cancel)
    {
        if (!context.WebSockets.IsWebSocketRequest)
            return TypedResults.BadRequest();
        Guid account = AccountId(user);
        Character? character = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        using System.Net.WebSockets.WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
        await hub.Serve(character.Id, character.Name, character.Town ?? "clairval", socket, context.RequestAborted);
        return TypedResults.Empty;
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> SavePlace(
        Guid id, Place place, ClaimsPrincipal user, GameDb db, ChatHub hub, CancellationToken cancel)
    {
        // The rules' own towns decide: a known town, a cell the player could walk to from the arrival.
        if (place?.Town is null || !GameData.Embedded.Towns.TryGetValue(place.Town, out Town? town) || !town.CanStand(place.Cell))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["place"] = ["A known town and a cell one can walk to there."] });
        Guid account = AccountId(user);
        // A dungeon's hall (sprint 86): only for who is in that dungeon, up to the room it is at.
        if (town.Dungeon is string hallOf)
        {
            var run = await db.Characters.Where(c => c.Id == id && c.AccountId == account).Select(c => new { c.Dungeon, c.DungeonRoom }).FirstOrDefaultAsync(cancel);
            if (run is null)
                return TypedResults.NotFound();
            if (run.Dungeon != hallOf || GameData.Embedded.Dungeons[hallOf].RoomOf(town.Id) > run.DungeonRoom)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["place"] = ["A hall of the dungeon this character is in, up to the room it is at."] });
        }
        int saved = await db.Characters.Where(c => c.Id == id && c.AccountId == account)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.Town, place.Town).SetProperty(c => c.X, place.X).SetProperty(c => c.Y, place.Y), cancel);
        if (saved > 0)
            hub.Move(id, place.Town);
        return saved == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<FightTicket>, NotFound, ValidationProblem>> StartFight(
        Guid id, NewFight request, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel)
    {
        // A scenario of the data files, or a group of monsters of the world (sprint 72).
        if (request?.Scenario is not string scenario || GameData.Embedded.FindScenario(scenario) is null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["scenario"] = [$"One of: {string.Join(", ", GameData.Embedded.Scenarios.Keys)}, or \"{MonsterGroup.Prefix}\" and a group of monsters."] });
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        // A dungeon's room only for who went in, and only the room it is at (sprint 80).
        if (Dungeons.Parse(scenario) is (string dungeon, int room) && character.Run?.IsAt(dungeon, room) != true)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["scenario"] = ["Not the room of the dungeon this character is at."] });
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
        // In a dungeon's room, the hero starts with the hit points kept from the room before (sprint 86).
        DungeonRun? inRoom = Dungeons.Parse(pending.Scenario) is (string runOf, int roomOf) && character.Run?.IsAt(runOf, roomOf) == true ? character.Run : null;
        try
        {
            FightRecord record = FightRecord.FromJson(await reader.ReadToEndAsync(cancel));
            problem = record.Scenario != pending.Scenario || record.Seed != unchecked((ulong)pending.Seed) ? "Not the fight the server started."
                : record.Hero != character.Hero || record.Rival is not null ? "Not this character's fight."
                : record.HeroHp != inRoom?.Hp ? "Not the hit points this character has."
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
        // A dungeon's room: won, the next room with the hit points left, or out with the last one's experience twice; lost, out.
        bool done = false;
        if (inRoom is DungeonRun run)
        {
            bool won = played.WinningTeam == 0;
            done = won && run.Room == Dungeons.RoomCount;
            if (done)
                xp += Progression.FightXp(played);
            Fighter hero = played.HeroFighter!;
            (character.Dungeon, character.DungeonRoom, character.DungeonHp) = won && !done
                ? (run.Dungeon, run.Room + 1, hero.Hp < hero.MaxHp ? hero.Hp : (int?)null)
                : ((string?)null, 0, (int?)null);
        }
        long shards = Progression.FightShards(played);
        // The quests under way (sprint 88): the monsters defeated, the dungeon finished.
        IReadOnlyDictionary<string, int> defeated = Rpg.Core.Quests.Defeated(GameData.Embedded, played);
        string? finished = done ? inRoom!.Dungeon : null;
        if (defeated.Count > 0 || finished is not null)
            character.QuestLog = Rpg.Core.Quests.Write(character.ActiveQuests.Select(p => Rpg.Core.Quests.AfterFight(GameData.Embedded.Quests[p.Quest], p, defeated, finished)));
        character.Receive(items);
        character.Xp += xp;
        character.Shards += shards;
        character.Level = Progression.LevelFor(character.Xp);
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(new FightResult(xp, character.Xp, character.Level, quest?.Id, items, character.Run, done, shards, character.Shards));
    }

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Enter(
        Guid id, EnterDungeon request, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        if (request?.Dungeon is not string dungeonId || !GameData.Embedded.Dungeons.TryGetValue(dungeonId, out Dungeon? dungeon))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["dungeon"] = [$"One of: {string.Join(", ", GameData.Embedded.Dungeons.Keys)}."] });
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        // Already in it: back to its room, without a key.
        if (character.Dungeon != dungeon.Id)
        {
            List<ItemCount> owned = character.InventoryList;
            if (!Dungeons.CanEnter(dungeon, owned))
                return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["key"] = [$"The key of this dungeon ('{dungeon.Key}') is needed."] });
            if (Dungeons.UsedOnEntry(dungeon, owned) is ItemCount used)
                character.Spend(used);
            (character.Dungeon, character.DungeonRoom, character.DungeonHp) = (dungeon.Id, 1, null);
            await db.SaveChangesAsync(cancel);
        }
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Make(
        Guid id, Craft request, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        if (request?.Job is not string jobId || !GameData.Embedded.Jobs.TryGetValue(jobId, out Job? job)
            || request.Item is not string item || job.RecipeFor(item) is not Recipe recipe)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["recipe"] = ["A trade, and an item it makes."] });
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        Dictionary<string, long> jobs = character.JobList;
        // The rules decide: the trade's level, the ingredients owned (worn ones count as owned, but are kept on).
        if (Crafting.Problem(recipe, Crafting.LevelFor(jobs.GetValueOrDefault(job.Id)), character.InventoryList, request.Count) is string problem)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["recipe"] = [problem] });
        foreach (ItemCount i in recipe.Ingredients)
            character.Spend(i with { Count = i.Count * request.Count });
        character.Receive([new ItemCount(recipe.Item, request.Count)]);
        jobs[job.Id] = jobs.GetValueOrDefault(job.Id) + Crafting.Xp(recipe) * request.Count;
        character.Jobs = string.Join(',', jobs.OrderBy(j => j.Key, StringComparer.Ordinal).Select(j => $"{j.Key}:{j.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Take(
        Guid id, TakeQuest request, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        if (request?.Quest is not string questId || !GameData.Embedded.Quests.TryGetValue(questId, out Quest? quest))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["quest"] = ["A known quest."] });
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        List<QuestProgress> active = character.ActiveQuests;
        // The rules decide (like the ways, T31, the server does not check that the giver is near).
        if (Rpg.Core.Quests.WhyNot(quest, character.Level, character.QuestList, active) is string why)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["quest"] = [why] });
        character.QuestLog = Rpg.Core.Quests.Write(active.Append(Rpg.Core.Quests.Start(quest)));
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Talk(
        Guid id, string quest, QuestTalk request, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        List<QuestProgress> active = character.ActiveQuests;
        if (active.FirstOrDefault(p => p.Quest == quest) is not QuestProgress progress
            || request?.Npc is not string npc || !GameData.Embedded.Quests[quest].Objectives!.Any(o => o.Goal == QuestGoal.Talk && o.Target == npc))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["quest"] = ["A quest under way that asks to talk to this inhabitant."] });
        character.QuestLog = Rpg.Core.Quests.Write(active.Select(p => p == progress ? Rpg.Core.Quests.AfterTalk(GameData.Embedded.Quests[quest], p, npc) : p));
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    internal static async Task<Results<Ok<QuestReward>, NotFound, ValidationProblem>> HandIn(
        Guid id, string quest, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        List<QuestProgress> active = character.ActiveQuests;
        if (active.FirstOrDefault(p => p.Quest == quest) is not QuestProgress progress)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["quest"] = ["Not a quest under way."] });
        Quest q = GameData.Embedded.Quests[quest];
        if (!Rpg.Core.Quests.IsComplete(q, progress, character.InventoryList))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["quest"] = ["Its objectives are not all reached."] });
        foreach (ItemCount taken in Rpg.Core.Quests.Taken(q))
            character.Spend(taken);
        character.Receive(q.Items ?? []);
        character.Xp += q.Xp;
        character.Shards += q.Shards;
        character.Level = Progression.LevelFor(character.Xp);
        character.Quests = string.Join(',', character.QuestList.Append(q.Id));
        character.QuestLog = Rpg.Core.Quests.Write(active.Where(p => p != progress));
        await db.SaveChangesAsync(cancel);
        CharacterSummary summary = Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null);
        return TypedResults.Ok(new QuestReward(q.Id, q.Xp, q.Shards, q.Items ?? [], character.Level, summary));
    }

    internal static async Task<Results<NoContent, NotFound>> Leave(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        int left = await db.Characters.Where(c => c.Id == id && c.AccountId == account)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.Dungeon, (string?)null).SetProperty(c => c.DungeonRoom, 0).SetProperty(c => c.DungeonHp, (int?)null), cancel);
        return left == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Rest(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        // The fountains are in the halls: in a dungeon only (like the ways, the server does not check the cell).
        if (character.Dungeon is null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["dungeon"] = ["Not in a dungeon."] });
        character.DungeonHp = null;
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
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
        // No deck sent: the class's first spells; a deck sent empty is kept empty (D66).
        string[]? deck = points?.Deck is null ? null : [.. points.Deck];
        // The rules decide: within the level's points, ranks of the class's unlocked spells, a deck of them.
        if ((character.Hero with { Stats = stats, Ranks = ranks, Deck = deck }).Problem(GameData.Embedded) is string problem)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["points"] = [problem] });
        (character.Vitality, character.Earth, character.Fire, character.Water, character.Air) = (stats.Vitality, stats.Earth, stats.Fire, stats.Water, stats.Air);
        character.Ranks = string.Join(',', ranks.Where(r => r.Value > 1).OrderBy(r => r.Key, StringComparer.Ordinal).Select(r => $"{r.Key}:{r.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        character.Deck = deck is null ? "" : deck.Length == 0 ? Character.EmptyDeck : string.Join(',', deck);
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    private static CharacterSummary Summary(Character c, Place? place) =>
        new(c.Id, c.Name, c.Look, c.Class, c.Colour, c.CreatedAt, place, c.Server, c.Level, c.Hair, c.Skin, c.Height, c.Build, c.Xp, c.Stats, c.RankList, c.QuestList, c.InventoryList, c.WornList, c.DeckList, c.Run, c.JobList, c.Shards, c.ActiveQuests);

    private static ProblemHttpResult NameTaken() =>
        TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "This character name is already taken.");
}
