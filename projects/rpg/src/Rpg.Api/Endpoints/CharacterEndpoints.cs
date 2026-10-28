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
        group.MapPost("/{id:guid}/use", Eat).WithSummary("Eats food the character owns, out of a fight: its hit points back at once; or reads a recall scroll, out of a fight and a dungeon: home at once.");
        group.MapGet("/{id:guid}/waystones", WaystonesOf).WithSummary("The waystones the character discovered, and its home.");
        group.MapPost("/{id:guid}/travel", TravelTo).WithSummary("Travels to a waystone discovered, for 10 Éclats and two a level of its map, out of a fight and a dungeon.");
        group.MapPost("/{id:guid}/home", MakeHome).WithSummary("Makes a waystone discovered the character's home, where the recall scroll takes it.");
        group.MapPost("/{id:guid}/harvest", Harvest).WithSummary("Gathers a spot of the character's map with a gathering trade: the trade's level, next to the spot, the spot ready; the resource and the trade's experience.");
        group.MapGet("/{id:guid}/harvest", Spots).WithSummary("The gathering spots of the character's map still growing back, and when.");
        group.MapPost("/{id:guid}/shop/buy", Buy).WithSummary("Buys from Rose's stall, in the village, what she sells, for Éclats.");
        group.MapPost("/{id:guid}/shop/sell", Sell).WithSummary("Sells Rose, in the village, resources and equipment owned and not worn, for Éclats.");
        group.MapPost("/{id:guid}/craft", Make).WithSummary("Makes items of a trade's recipe for one of the player's characters: the trade's level, the ingredients used up, the trade's experience.");
        group.MapPost("/{id:guid}/dungeon/rest", Rest).WithSummary("Heals one of the player's characters in its dungeon, at the fountain of a hall: all its hit points for the next room.");
        group.MapPost("/{id:guid}/quests", Take).WithSummary("Takes a quest an inhabitant gives, from its level and after the quests it follows.");
        group.MapPost("/{id:guid}/quests/{quest}/talk", Talk).WithSummary("Counts a talk to someone a quest under way asks to see.");
        group.MapPost("/{id:guid}/quests/{quest}/interact", Interact).WithSummary("Counts a thing on a map examined, which a quest under way asks to find.");
        group.MapPost("/{id:guid}/quests/{quest}/done", HandIn).WithSummary("Hands a quest back to its giver, its objectives reached: the items it asks are taken, its rewards given.");
        group.MapDelete("/{id:guid}/dungeon", Leave).WithSummary("Takes one of the player's characters out of its dungeon; the key is not given back.");
        group.Map("/{id:guid}/chat", Chat).WithSummary("The proximity chat of one of the player's characters, as a WebSocket: lines in, the lines said on its map out.");
        group.MapPut("/{id:guid}/points", SavePoints).WithSummary("Spends a character's characteristic and spell points, within what its level gives.");
    }

    /// <summary>The account in the token; tokens of a deleted account no longer match any.</summary>
    internal static Guid AccountId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid id) ? id : Guid.Empty;

    internal static async Task<Ok<CharacterSummary[]>> List(ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        var list = await db.Characters.Where(c => c.AccountId == account)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Name)
            .ToArrayAsync(cancel);
        bool[] reset = [.. list.Select(Retree)];
        if (reset.Any(r => r))
            await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(list.Select((c, i) => Summary(c, c.Town is string town && c.X is int x && c.Y is int y ? new Place(town, x, y) : null) with { TalentsReset = reset[i] }).ToArray());
    }

    /// <summary>
    /// Talents taken in an older tree (sprint 146, D82, I5 a), as the game signs in: given back once, for free, the deck
    /// keeping the spells the grimoire still has; true when there were talents to give back, for the game to say so.
    /// </summary>
    internal static bool Retree(Character c)
    {
        if (c.TalentsVersion >= Talents.Version)
            return false;
        c.TalentsVersion = Talents.Version;
        if (c.Talents.Length == 0)
            return false;
        c.Talents = "";
        if (c.Deck is not ("" or Character.EmptyDeck))
        {
            Grimoire grimoire = Talents.GrimoireOf(c.Hero, GameData.Embedded);
            string[] kept = [.. (c.DeckList ?? []).Where(grimoire.Spells.Contains)];
            c.Deck = kept.Length == 0 ? Character.EmptyDeck : string.Join(',', kept);
        }
        return true;
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

    internal static async Task<Results<NoContent, NotFound>> Delete(Guid id, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        // Its guild first (sprint 145): a leader deleted hands its title over as one who leaves.
        if (await db.Characters.AnyAsync(c => c.Id == id && c.AccountId == account, cancel))
            await GuildEndpoints.Depart(id, db, hub, clock, cancel);
        // Someone else's character answers like a missing one: ids of other players stay unknown.
        int deleted = await db.Characters.Where(c => c.Id == id && c.AccountId == account).ExecuteDeleteAsync(cancel);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    /// <summary>The chat's socket (sprint 84): for a character of the account, on the map the server knows it on.</summary>
    internal static async Task<IResult> Chat(Guid id, HttpContext context, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (!context.WebSockets.IsWebSocketRequest)
            return TypedResults.BadRequest();
        Guid account = AccountId(user);
        Character? character = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        using System.Net.WebSockets.WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
        string map = character.Town ?? "clairval";
        Cell at = character.X is int x && character.Y is int y ? new Cell(x, y) : GameData.Embedded.Towns[map].Spawn;
        // Its guild goes with it (sprint 145), and the guild's tab tells when it last came into play.
        var guild = await (from m in db.GuildMembers.AsNoTracking()
                           join g in db.Guilds.AsNoTracking() on m.GuildId equals g.Id
                           where m.CharacterId == id
                           select new { g.Id, g.Name, g.Crest }).FirstOrDefaultAsync(cancel);
        // The playtest's counters (sprint 156): a day more when the first of the day, the time in play once it ends.
        DateTimeOffset came = clock.GetUtcNow();
        int day = Contracts.Day(came);
        await db.Characters.Where(c => c.Id == id).ExecuteUpdateAsync(c => c.SetProperty(p => p.LastSeenAt, came)
            .SetProperty(p => p.DaysPlayed, p => p.LastDayPlayed == day ? p.DaysPlayed : p.DaysPlayed + 1)
            .SetProperty(p => p.LastDayPlayed, day), cancel);
        await hub.Serve(character.Id, character.Name, character.Hero, map, at, socket, context.RequestAborted, guild?.Id, guild?.Name, character.Title,
            guild is null ? null : GuildCrests.Write(GuildCrests.Read(guild.Crest)));
        long seconds = (long)(clock.GetUtcNow() - came).TotalSeconds;
        await db.Characters.Where(c => c.Id == id).ExecuteUpdateAsync(c => c.SetProperty(p => p.SecondsPlayed, p => p.SecondsPlayed + seconds), CancellationToken.None);
        return TypedResults.Empty;
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> SavePlace(
        Guid id, Place place, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
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
        Character? moving = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        // Beyond a way that waits for a quest (sprint 129: Quatre-Chemins after "Four Trails"): only once it is done.
        if (moving is not null && GameData.Embedded.QuestToReach(place.Town) is string gate && !moving.QuestList.Contains(gate))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["place"] = [$"A map past a way that opens after the quest '{gate}'."] });
        int saved = 0;
        if (moving is not null)
        {
            // The life grown so far at the map's rate (the village heals twice as fast, sprint 101), then the new map.
            if (moving.Town != place.Town && !moving.InFight)
                moving.KeepLife(clock.GetUtcNow());
            (moving.Town, moving.X, moving.Y) = (place.Town, place.X, place.Y);
            // A waystone passed by is discovered (sprint 127).
            moving.Discover();
            saved = await db.SaveChangesAsync(cancel);
        }
        if (saved > 0)
            await hub.Move(id, place.Town, place.Cell, cancel);
        return saved == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<FightTicket>, NotFound, ValidationProblem>> StartFight(
        Guid id, NewFight request, ClaimsPrincipal user, GameDb db, TimeProvider clock, Hunts hunts, ChatHub hub, CancellationToken cancel)
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
        // A group of the world (sprint 118, D74): one fight at a time, none while it is away; its map's players see it fought.
        if (Hunts.GroupOf(scenario) is MonsterGroup group)
        {
            if (!hunts.Start(group.Id, id))
                return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["scenario"] = ["This group is fighting someone else, or beaten and not back yet."] });
            await hub.Tell(group.Map, new MapNews(MapNewsKind.Fighting, Group: group.Id), cancel: cancel);
        }
        // The life (sprint 101, D73): a fight taken and never reported is lost (1 hit point); the life now is
        // the hero's to start with, frozen until the report.
        DateTimeOffset now = clock.GetUtcNow();
        if (character.InFight)
        {
            character.SetLife(1, now);
            await db.PendingFights.Where(f => f.CharacterId == id).ExecuteDeleteAsync(cancel);
        }
        character.KeepLife(now);
        character.InFight = true;
        character.HpAt ??= new DateTimeOffset(now.Ticks - now.Ticks % TimeSpan.TicksPerMillisecond, now.Offset);
        // The server draws the seed: the player cannot pick the rolls.
        var pending = new PendingFight { CharacterId = id, Scenario = scenario, Seed = BitConverter.ToInt64(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)), CreatedAt = now };
        db.PendingFights.Add(pending);
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(new FightTicket(pending.Id, scenario, unchecked((ulong)pending.Seed), character.Hp));
    }

    internal static async Task<Results<Ok<FightResult>, NotFound, ValidationProblem>> ReportFight(
        Guid id, Guid fight, HttpRequest request, ClaimsPrincipal user, GameDb db, TimeProvider clock, Hunts hunts, ChatHub hub, CancellationToken cancel)
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
        // The hero starts with the life frozen by the ticket (sprint 101; a dungeon's rooms since sprint 86).
        int? startHp = character.InFight ? character.Hp : null;
        DungeonRun? inRoom = Dungeons.Parse(pending.Scenario) is (string runOf, int roomOf) && character.Run?.IsAt(runOf, roomOf) == true ? character.Run : null;
        try
        {
            FightRecord record = FightRecord.FromJson(await reader.ReadToEndAsync(cancel));
            problem = record.Scenario != pending.Scenario || record.Seed != unchecked((ulong)pending.Seed) ? "Not the fight the server started."
                : record.Hero != character.Hero || record.Rival is not null ? "Not this character's fight."
                : record.HeroHp != startHp ? "Not the hit points this character has."
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
        // The group fought (sprint 118): beaten and away for a while, or back at once; told to its map either way.
        if (Hunts.GroupOf(pending.Scenario) is MonsterGroup group)
        {
            DateTimeOffset? until = hunts.End(group.Id, id, problem is null && played!.WinningTeam == 0, out bool itsFight);
            if (itsFight)
                await hub.Tell(group.Map, until is null ? new MapNews(MapNewsKind.Back, Group: group.Id) : new MapNews(MapNewsKind.Beaten, Group: group.Id, Until: until), cancel: cancel);
        }
        if (problem is not null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["record"] = [problem] });
        long xp = Progression.FightXp(played!);
        Quest? quest = played!.WinningTeam == 0
            ? GameData.Embedded.Quests.Values.FirstOrDefault(q => q.Scenario == pending.Scenario && !character.QuestList.Contains(q.Id))
            : null;
        // The loot follows the fight's seed, which the server drew: the game cannot pick it either. The quests'
        // loot falls only while its quest wants it (sprint 105).
        List<ItemCount> items = [.. Equipment.Loot(played, Rpg.Core.Quests.LootWanted(GameData.Embedded, character.ActiveQuests, character.InventoryList))];
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
            (character.Dungeon, character.DungeonRoom) = won && !done ? (run.Dungeon, run.Room + 1) : ((string?)null, 0);
        }
        long shards = Progression.FightShards(played);
        // The quests under way (sprint 88): the monsters defeated, the dungeon finished.
        IReadOnlyDictionary<string, int> defeated = Rpg.Core.Quests.Defeated(GameData.Embedded, played);
        // The room won (sprint 95): the last finishes the dungeon, an earlier one is enough for some quests.
        (string, int)? roomWon = inRoom is DungeonRun fought && played.WinningTeam == 0 ? (fought.Dungeon, fought.Room) : null;
        // The scenario won, for the quests that ask to win it (sprint 105).
        string? scenarioWon = played.WinningTeam == 0 ? pending.Scenario : null;
        if (defeated.Count > 0 || roomWon is not null || scenarioWon is not null)
            character.QuestLog = Rpg.Core.Quests.Write(character.ActiveQuests.Select(p => Rpg.Core.Quests.AfterFight(GameData.Embedded.Quests[p.Quest], p, defeated, roomWon, scenarioWon)));
        // The notice board's contracts (sprint 153): a hunt, a horde, a dungeon's room.
        ContractEndpoints.AfterFight(character, played, roomWon);
        // The bestiary (sprint 154): each monster faced met, a victory more for each one beaten.
        character.Bestiary = Rpg.Core.Bestiary.Write(Rpg.Core.Bestiary.AfterFight(character.BestiaryList, GameData.Embedded, played));
        character.Receive(items);
        character.Xp += xp;
        character.Shards += shards;
        int before = character.Level;
        character.Level = Progression.LevelFor(character.Xp);
        // The life the fight ends with (sprint 101, D73): what the hero has left, 1 if lost; a level gained gives it all back.
        DateTimeOffset ended = clock.GetUtcNow();
        character.SetLife(character.Level > before ? int.MaxValue : played.WinningTeam == 0 ? played.HeroFighter!.Hp : 1, ended);
        await db.SaveChangesAsync(cancel);
        // An achievement the fight brought counts for the guild (sprint 156).
        await GuildEndpoints.CreditAchievements(character, db, hub, clock, cancel);
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
            (character.Dungeon, character.DungeonRoom) = (dungeon.Id, 1);
            await db.SaveChangesAsync(cancel);
        }
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    /// <summary>Food eaten out of a fight (sprint 101, D73): owned, not in a fight; its hit points at once, the item used up.</summary>
    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Eat(
        Guid id, Use request, ClaimsPrincipal user, GameDb db, TimeProvider clock, ChatHub hub, CancellationToken cancel)
    {
        if (request?.Item is not string itemId || !GameData.Embedded.Items.TryGetValue(itemId, out Item? item) || (item.Heal <= 0 && itemId != Waystones.Recall))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["item"] = ["Food, which heals, or the recall scroll."] });
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        // The recall scroll (sprint 127): home at once, the life kept as it was, the map told.
        if (itemId == Waystones.Recall)
        {
            int owned = character.InventoryList.Where(i => i.Item == itemId).Sum(i => i.Count);
            if (Waystones.WhyNotRecall(owned, character.InFight, character.Dungeon is not null) is string no)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["item"] = [no] });
            character.Spend(new ItemCount(itemId, 1));
            return await MoveTo(character, Waystones.Home(character.Home, GameData.Embedded), db, hub, clock, cancel);
        }
        string? why = character.InFight ? "Not in a fight."
            : character.InventoryList.Where(i => i.Item == itemId).Sum(i => i.Count) < 1 ? "None owned." : null;
        if (why is not null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["item"] = [why] });
        DateTimeOffset now = clock.GetUtcNow();
        character.SetLife(character.LifeNow(now) + item.Heal, now);
        character.Spend(new ItemCount(itemId, 1));
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    // A character taken to a place (the recall scroll, a waystone): its life grown on the old map kept, saved, the maps told.
    private static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> MoveTo(Character c, Place to, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (!c.InFight)
            c.KeepLife(clock.GetUtcNow());
        (c.Town, c.X, c.Y) = (to.Town, to.X, to.Y);
        c.Discover();
        await db.SaveChangesAsync(cancel);
        await hub.Move(c.Id, to.Town, to.Cell, cancel);
        return TypedResults.Ok(Summary(c));
    }

    internal static async Task<Results<Ok<WaystoneView>, NotFound>> WaystonesOf(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? c = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        return c is null ? TypedResults.NotFound() : TypedResults.Ok(new WaystoneView(c.WaystoneList, c.Home));
    }

    /// <summary>A waystone discovered, out of a fight and a dungeon, for its price in Éclats (sprint 127).</summary>
    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> TravelTo(
        Guid id, Travel request, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? c = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (c is null)
            return TypedResults.NotFound();
        string waystone = request?.Waystone ?? "";
        if (Waystones.WhyNotTravel(waystone, c.WaystoneList.ToList(), c.Town, c.InFight, c.Dungeon is not null, c.Shards, GameData.Embedded) is string why)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["waystone"] = [why] });
        c.Shards -= Waystones.Cost(waystone, GameData.Embedded);
        return await MoveTo(c, Waystones.Arrival(waystone, GameData.Embedded), db, hub, clock, cancel);
    }

    /// <summary>A waystone discovered becomes the character's home (sprint 127).</summary>
    internal static async Task<Results<Ok<WaystoneView>, NotFound, ValidationProblem>> MakeHome(Guid id, Travel request, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? c = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (c is null)
            return TypedResults.NotFound();
        if (request?.Waystone is not string home || !c.WaystoneList.Contains(home))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["waystone"] = ["A waystone discovered."] });
        c.Home = home;
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(new WaystoneView(c.WaystoneList, c.Home));
    }

    /// <summary>A gathering (sprint 99, D71): the rules decide, the server keeps the spots and the trade's experience.</summary>
    internal static async Task<Results<Ok<Gathered>, NotFound, ValidationProblem>> Harvest(
        Guid id, Gather request, ClaimsPrincipal user, GameDb db, Fields fields, ChatHub hub, CancellationToken cancel)
    {
        if (request?.Node is not string nodeId || !GameData.Embedded.Nodes.TryGetValue(nodeId, out HarvestNode? node))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["node"] = ["A known gathering spot."] });
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        Dictionary<string, long> jobs = character.JobList;
        int level = Crafting.LevelFor(jobs.GetValueOrDefault(node.Job));
        // Where the character says it stands: a free cell of its map (like the ways, T31, the walk itself is not replayed).
        Town map = GameData.Embedded.Towns[node.Map];
        string? why = Harvesting.WhyNot(node, level, character.Town, request.From)
            ?? (map.CanStand(request.From) ? null : "Not a cell to stand on.");
        if (why is null && !fields.Take(node.Id, character.Id, Harvesting.Regrowth, Harvesting.Duration))
            why = "The spot is growing back, or a gathering is under way.";
        if (why is not null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["node"] = [why] });
        var item = new ItemCount(node.Item, Harvesting.Quantity(level, node.Level));
        long xp = Harvesting.Xp(level, node.Level);
        character.Receive([item]);
        jobs[node.Job] = jobs.GetValueOrDefault(node.Job) + xp;
        character.Jobs = string.Join(',', jobs.OrderBy(j => j.Key, StringComparer.Ordinal).Select(j => $"{j.Key}:{j.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        (character.X, character.Y) = (request.From.X, request.From.Y);
        await db.SaveChangesAsync(cancel);
        DateTimeOffset grows = fields.GrowsBack(node.Id)!.Value;
        // The map's other players see it gone (sprint 118, D74).
        await hub.Tell(map.Id, new MapNews(MapNewsKind.Taken, character.Id, Spot: node.Id, Until: grows), cancel: cancel, but: character.Id);
        return TypedResults.Ok(new Gathered(Summary(character, new Place(map.Id, request.From.X, request.From.Y)), item, xp, grows));
    }

    internal static async Task<Results<Ok<Growing>, NotFound>> Spots(Guid id, ClaimsPrincipal user, GameDb db, Fields fields, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        return TypedResults.Ok(new Growing(fields.Growing(GameData.Embedded.Nodes.Values.Where(n => n.Map == character.Town).Select(n => n.Id))));
    }

    /// <summary>Rose's stall (sprint 97, D71): the rules decide the price, the place, the Éclats and what is owned.</summary>
    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Buy(
        Guid id, Trade request, ClaimsPrincipal user, GameDb db, CancellationToken cancel) => await Deal(id, request, user, db, buying: true, cancel);

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Sell(
        Guid id, Trade request, ClaimsPrincipal user, GameDb db, CancellationToken cancel) => await Deal(id, request, user, db, buying: false, cancel);

    private static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Deal(
        Guid id, Trade request, ClaimsPrincipal user, GameDb db, bool buying, CancellationToken cancel)
    {
        if (request?.Item is not string itemId || !GameData.Embedded.Items.TryGetValue(itemId, out Item? item))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["item"] = ["A known item."] });
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        string? why = buying
            ? Shop.WhyNotBuy(item, request.Count, character.Shards, character.Town, GameData.Embedded)
            : Shop.WhyNotSell(item, request.Count, character.InventoryList, character.WornList, character.Town, GameData.Embedded);
        if (why is not null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["item"] = [why] });
        var deal = new ItemCount(item.Id, request.Count);
        if (buying)
        {
            character.Shards -= Shop.SellPrice(item, GameData.Embedded)!.Value * request.Count;
            character.Receive([deal]);
        }
        else
        {
            character.Shards += Shop.BuyPrice(item, GameData.Embedded)!.Value * request.Count;
            character.Spend(deal);
        }
        await db.SaveChangesAsync(cancel);
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
        int level = Crafting.LevelFor(jobs.GetValueOrDefault(job.Id));
        // The rules decide: the trade's level, the ingredients owned (worn ones count as owned, but are kept on).
        if (Crafting.Problem(recipe, level, character.InventoryList, request.Count, character.Shards) is string problem)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["recipe"] = [problem] });
        foreach (ItemCount i in recipe.Ingredients)
            character.Spend(i with { Count = i.Count * request.Count });
        // The key's price in Éclats (sprint 97).
        character.Shards -= recipe.Shards * request.Count;
        character.Receive([new ItemCount(recipe.Item, request.Count)]);
        // A piece of equipment teaches more than a key, and nothing 20 levels under the trade (sprint 109, D76).
        jobs[job.Id] = jobs.GetValueOrDefault(job.Id) + Crafting.Xp(recipe, GameData.Embedded.Items[recipe.Item], level) * request.Count;
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
        // What the quest gives to set out (sprint 95): a quest is taken once, so it is given once.
        character.Receive(quest.Grants ?? []);
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

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Interact(
        Guid id, string quest, QuestInteract request, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        List<QuestProgress> active = character.ActiveQuests;
        // Like a talk (T31), the server does not check that the character stands by it.
        if (active.FirstOrDefault(p => p.Quest == quest) is not QuestProgress progress
            || request?.Prop is not string prop || !GameData.Embedded.Quests[quest].Objectives!.Any(o => o.Goal == QuestGoal.Interact && o.Target == prop))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["quest"] = ["A quest under way that asks to examine this."] });
        character.QuestLog = Rpg.Core.Quests.Write(active.Select(p => p == progress ? Rpg.Core.Quests.AfterInteract(GameData.Embedded.Quests[quest], p, prop) : p));
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
        if (!Rpg.Core.Quests.IsComplete(q, progress, character.InventoryList, character.WornList.Values))
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
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.Dungeon, (string?)null).SetProperty(c => c.DungeonRoom, 0), cancel);
        return left == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Rest(Guid id, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        // The fountains are in the halls: in a dungeon only (like the ways, the server does not check the cell).
        if (character.Dungeon is null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["dungeon"] = ["Not in a dungeon."] });
        // The fountain gives the whole life back at once (sprint 101).
        character.SetLife(int.MaxValue, clock.GetUtcNow());
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> SaveEquipment(
        Guid id, Wear request, ClaimsPrincipal user, GameDb db, TimeProvider clock, ChatHub hub, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        var worn = new Dictionary<Slot, string>(request?.Worn ?? new Dictionary<Slot, string>());
        // The rules first (since sprint 110, the same ring is never worn twice), then only what it owns.
        Dictionary<string, int> owned = character.InventoryList.ToDictionary(i => i.Item, i => i.Count, StringComparer.Ordinal);
        string? problem = (character.Hero with { Worn = worn }).Problem(GameData.Embedded)
            ?? (worn.Values.GroupBy(i => i).FirstOrDefault(g => g.Count() > owned.GetValueOrDefault(g.Key)) is IGrouping<string, string> missing
                ? $"'{missing.Key}': not owned, or not that many." : null);
        if (problem is not null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["equipment"] = [problem] });
        // The life grown so far, at most the new maximum (changing what is worn does not heal, sprint 101).
        int life = character.InFight ? 0 : character.LifeNow(clock.GetUtcNow());
        character.Worn = string.Join(',', worn.OrderBy(w => w.Key).Select(w => $"{w.Key}:{w.Value}"));
        if (!character.InFight)
            character.SetLife(life, clock.GetUtcNow());
        await db.SaveChangesAsync(cancel);
        // Its map's players see what it now wears (sprint 124).
        await hub.Restyle(character.Id, character.Hero, cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> SavePoints(
        Guid id, Points points, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel)
    {
        Guid account = AccountId(user);
        Character? character = await db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
        if (character is null)
            return TypedResults.NotFound();
        Characteristics stats = points?.Stats ?? Characteristics.None;
        var ranks = new Dictionary<string, int>(points?.Ranks ?? new Dictionary<string, int>(), StringComparer.Ordinal);
        // No deck sent: the class's first spells; a deck sent empty is kept empty (D66).
        string[]? deck = points?.Deck is null ? null : [.. points.Deck];
        // The talents (sprint 137): none sent, those saved.
        string[] talents = points?.Talents is null ? character.TalentList : [.. points.Talents.Order(StringComparer.Ordinal)];
        // The rules decide: within the level's points, ranks of the class's unlocked spells, a deck of them, the tree's rules.
        if ((character.Hero with { Stats = stats, Ranks = ranks, Deck = deck, Talents = talents.Length == 0 ? null : talents }).Problem(GameData.Embedded) is string problem)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["points"] = [problem] });
        // Taking characteristic points back costs Éclats from level 11 (sprint 97, D71); talents from level 41, in a village, out of a fight (sprint 140).
        long talentCost = Talents.GivesBack(character.TalentList, talents, GameData.Embedded) ? Talents.ChangeCost(character.Level) : 0;
        if (talentCost > 0 && (character.InFight || !Shop.InVillage(character.Town)))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["points"] = ["Talents are given back in a village, out of a fight."] });
        long cost = (Shop.TakesBack(character.Stats, stats) ? Shop.ResetCost(character.Level) : 0) + talentCost;
        if (cost > character.Shards)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["points"] = [$"Taking points back costs {cost} Éclats."] });
        character.Shards -= cost;
        int lifeBefore = character.InFight ? 0 : character.LifeNow(clock.GetUtcNow());
        (character.Vitality, character.Earth, character.Fire, character.Water, character.Air) = (stats.Vitality, stats.Earth, stats.Fire, stats.Water, stats.Air);
        character.Ranks = string.Join(',', ranks.Where(r => r.Value > 1).OrderBy(r => r.Key, StringComparer.Ordinal).Select(r => $"{r.Key}:{r.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        character.Deck = deck is null ? "" : deck.Length == 0 ? Character.EmptyDeck : string.Join(',', deck);
        character.Talents = string.Join(',', talents);
        character.TalentsVersion = Talents.Version;
        if (!character.InFight)
            character.SetLife(lifeBefore, clock.GetUtcNow());
        await db.SaveChangesAsync(cancel);
        return TypedResults.Ok(Summary(character, character.Town is string town && character.X is int x && character.Y is int y ? new Place(town, x, y) : null));
    }

    /// <summary>The character as the game reads it, with the place it stands on.</summary>
    internal static CharacterSummary Summary(Character c) =>
        Summary(c, c.Town is string town && c.X is int x && c.Y is int y ? new Place(town, x, y) : null);

    private static CharacterSummary Summary(Character c, Place? place) =>
        new(c.Id, c.Name, c.Look, c.Class, c.Colour, c.CreatedAt, place, c.Server, c.Level, c.Hair, c.Skin, c.Height, c.Build, c.Xp, c.Stats, c.RankList, c.QuestList, c.InventoryList, c.WornList, c.DeckList, c.Run, c.JobList, c.Shards, c.ActiveQuests,
            c.Hp, c.HpAt, c.InFight, c.TalentList.Length == 0 ? null : c.TalentList, Bestiary: c.Bestiary.Length == 0 ? null : c.BestiaryList);

    private static ProblemHttpResult NameTaken() =>
        TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "This character name is already taken.");
}
