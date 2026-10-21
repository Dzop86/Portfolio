using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api;

/// <summary>
/// The fights with the party (sprint 120, D74), led by the server: one is opened on a group of monsters, the party's
/// members on the map join within fifteen seconds, then the server holds the <see cref="Fight"/>, takes each hero's
/// actions from its player (in its turn, by the same rules), plays the monsters with <see cref="Ai"/>, passes a hero's
/// turn after thirty seconds, and tells every player each action accepted, numbered. Once over, each hero is rewarded
/// on its own: experience, Éclats, loot drawn for it, quests, the life it ends with. Kept in memory, in one server
/// process; a player who comes back gets the start and every action so far.
/// </summary>
public sealed class Battles(IServiceScopeFactory scopes, ChatHub hub, Hunts hunts, TimeProvider clock)
{
    private sealed class Battle(Guid id, string scenario, ulong seed, Guid starter, string map, MonsterGroup? group, DateTimeOffset joinUntil)
    {
        public Guid Id { get; } = id;
        public string Scenario { get; } = scenario;
        public ulong Seed { get; } = seed;
        public Guid Starter { get; } = starter;
        public string Map { get; } = map;
        public MonsterGroup? Group { get; } = group;
        public DateTimeOffset JoinUntil { get; } = joinUntil;
        public List<Guid> Players { get; } = [starter];
        public SharedFightStart? Start { get; set; }
        public Fight? Fight { get; set; }
        public DateTimeOffset TurnEnds { get; set; }
        public bool Done { get; set; }
        public SemaphoreSlim Lock { get; } = new(1, 1);
    }

    private readonly ConcurrentDictionary<Guid, Battle> _battles = new();

    /// <summary>The scenario of a fight not over, if any.</summary>
    public string? ScenarioOf(Guid fight) => _battles.TryGetValue(fight, out Battle? b) ? b.Scenario : null;

    /// <summary>The fight a character opened or joined and that is not over, if any.</summary>
    public Guid? Of(Guid character) => _battles.Values.FirstOrDefault(b => b.Players.Contains(character))?.Id;

    /// <summary>Opens a fight on a group of the world for a character in a party: its id, the party told.</summary>
    public async Task<Guid> Open(Guid starter, string starterName, string scenario, string map, MonsterGroup? group, IReadOnlyList<Guid> party, CancellationToken cancel)
    {
        var battle = new Battle(Guid.NewGuid(), scenario, BitConverter.ToUInt64(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)), starter, map, group,
            clock.GetUtcNow() + SharedRules.JoinFor);
        _battles[battle.Id] = battle;
        foreach (Guid member in party.Where(m => m != starter && hub.MapOf(m) == map))
            await hub.TellCharacter(member, new SharedFightNews(SharedFightKind.Opened, battle.Id, starterName, scenario), cancel);
        return battle.Id;
    }

    /// <summary>A party member joins before the fight starts: why not (no such fight, started, full, another map), or null.</summary>
    public async Task<string?> Join(Guid fight, Guid character, IReadOnlyList<Guid>? party, CancellationToken cancel)
    {
        if (!_battles.TryGetValue(fight, out Battle? b))
            return "No such fight.";
        await b.Lock.WaitAsync(cancel);
        try
        {
            string? why = b.Fight is not null || b.Done || clock.GetUtcNow() > b.JoinUntil ? "The fight has started."
                : party?.Contains(b.Starter) != true ? "Not in the party of who opened it."
                : hub.MapOf(character) != b.Map ? "Not on its map."
                : b.Players.Contains(character) ? "Already in it."
                : Of(character) is not null ? "Already in another fight."
                : b.Players.Count >= PartyRules.Most ? $"At most {PartyRules.Most} heroes."
                : null;
            if (why is null)
                b.Players.Add(character);
            return why;
        }
        finally
        {
            b.Lock.Release();
        }
    }

    /// <summary>The one who opened it starts it now, without waiting the fifteen seconds.</summary>
    public async Task<string?> Go(Guid fight, Guid character, CancellationToken cancel)
    {
        if (!_battles.TryGetValue(fight, out Battle? b) || b.Starter != character)
            return "Not a fight this character opened.";
        await b.Lock.WaitAsync(cancel);
        try
        {
            if (b.Fight is null && !b.Done)
                await Begin(b, cancel);
            return null;
        }
        finally
        {
            b.Lock.Release();
        }
    }

    /// <summary>An action of a hero's player, in its hero's turn: why not, or null once applied and told.</summary>
    public async Task<string?> Act(Guid fight, Guid character, FightAction action, CancellationToken cancel)
    {
        if (!_battles.TryGetValue(fight, out Battle? b) || !b.Players.Contains(character))
            return "Not in this fight.";
        await b.Lock.WaitAsync(cancel);
        try
        {
            if (b.Fight is not Fight f || f.IsOver)
                return "The fight is not under way.";
            if (f.HeroFighters[b.Players.IndexOf(character)] != f.Current)
                return "Not this hero's turn.";
            int round = f.Round, current = f.Current.Id;
            ActionError error = f.Apply(action);
            if (error != ActionError.None)
                return error.ToString();
            await Played(b, action, cancel);
            // The turn passed (or the fight ended): the monsters play up to the next hero.
            if (f.IsOver || f.Current.Id != current || f.Round != round)
                await Monsters(b, cancel);
            return null;
        }
        finally
        {
            b.Lock.Release();
        }
    }

    /// <summary>The fight as a player coming back sees it: its start and every action so far; null when it is not under way.</summary>
    public SharedFightNews? Now(Guid fight, Guid character) =>
        _battles.TryGetValue(fight, out Battle? b) && b.Players.Contains(character) && b.Fight is Fight f && b.Start is not null
            ? new SharedFightNews(SharedFightKind.Started, b.Id, Start: b.Start, Actions: [.. f.History], TurnEnds: b.TurnEnds)
            : null;

    /// <summary>
    /// The clock (each quarter of a second, <see cref="BattleClock"/>): the fights whose fifteen seconds have passed
    /// start; a hero whose thirty seconds have passed ends its turn.
    /// </summary>
    public async Task Tick(CancellationToken cancel)
    {
        DateTimeOffset now = clock.GetUtcNow();
        foreach (Battle b in _battles.Values)
        {
            if (!await b.Lock.WaitAsync(0, cancel))
                continue;
            try
            {
                if (b.Done)
                    continue;
                if (b.Fight is null && now >= b.JoinUntil)
                    await Begin(b, cancel);
                else if (b.Fight is Fight f && !f.IsOver && now >= b.TurnEnds && f.HeroFighters.Contains(f.Current))
                {
                    f.Apply(new EndTurnAction());
                    await Played(b, new EndTurnAction(), cancel);
                    await Monsters(b, cancel);
                }
            }
            finally
            {
                b.Lock.Release();
            }
        }
    }

    // The heroes as the server keeps them, their life frozen for the fight as for a fight alone; the fight built and told.
    private async Task Begin(Battle b, CancellationToken cancel)
    {
        using IServiceScope scope = scopes.CreateScope();
        GameDb db = scope.ServiceProvider.GetRequiredService<GameDb>();
        DateTimeOffset now = clock.GetUtcNow();
        List<Character> heroes = await db.Characters.Where(c => b.Players.Contains(c.Id)).ToListAsync(cancel);
        heroes = [.. b.Players.Select(p => heroes.First(h => h.Id == p))];
        foreach (Character c in heroes)
        {
            if (c.InFight)
            {
                c.SetLife(1, now);
                await db.PendingFights.Where(f => f.CharacterId == c.Id).ExecuteDeleteAsync(cancel);
            }
            c.KeepLife(now);
            c.InFight = true;
        }
        await db.SaveChangesAsync(cancel);
        b.Start = new SharedFightStart(b.Id, b.Scenario, b.Seed, heroes[0].Hero, heroes[0].Hp,
            [.. heroes.Skip(1).Select(h => h.Hero)], [.. heroes.Skip(1).Select(h => (int?)h.Hp)], [.. b.Players]);
        b.Fight = b.Start.Build(GameData.Embedded);
        b.TurnEnds = now + SharedRules.Turn;
        await Tell(b, new SharedFightNews(SharedFightKind.Started, b.Id, Start: b.Start, Actions: [], TurnEnds: b.TurnEnds), cancel);
        await Monsters(b, cancel);
    }

    // The monsters play until a hero's turn, or the end; a hero's turn starts its thirty seconds.
    private async Task Monsters(Battle b, CancellationToken cancel)
    {
        Fight f = b.Fight!;
        while (!f.IsOver && !f.HeroFighters.Contains(f.Current))
        {
            FightAction action = Ai.Decide(f);
            if (f.Apply(action) != ActionError.None)
            {
                action = new EndTurnAction();
                f.Apply(action);
            }
            await Played(b, action, cancel);
        }
        if (f.IsOver)
            await Finish(b, cancel);
        else
            b.TurnEnds = clock.GetUtcNow() + SharedRules.Turn;
    }

    private Task Played(Battle b, FightAction action, CancellationToken cancel) =>
        Tell(b, new SharedFightNews(SharedFightKind.Acted, b.Id, Number: b.Fight!.History.Count - 1, Action: action, TurnEnds: b.TurnEnds), cancel);

    private async Task Tell(Battle b, SharedFightNews news, CancellationToken cancel)
    {
        foreach (Guid p in b.Players)
            await hub.TellCharacter(p, news, cancel);
    }

    // Each hero rewarded on its own (as a fight alone is, sprint 101 for the life), the group beaten or back, everyone told.
    private async Task Finish(Battle b, CancellationToken cancel)
    {
        b.Done = true;
        _battles.TryRemove(b.Id, out _);
        Fight f = b.Fight!;
        using IServiceScope scope = scopes.CreateScope();
        GameDb db = scope.ServiceProvider.GetRequiredService<GameDb>();
        List<Character> heroes = await db.Characters.Where(c => b.Players.Contains(c.Id)).ToListAsync(cancel);
        DateTimeOffset now = clock.GetUtcNow();
        bool won = f.WinningTeam == 0;
        var results = new List<SharedFightResult>();
        IReadOnlyDictionary<string, int> defeated = Rpg.Core.Quests.Defeated(GameData.Embedded, f);
        for (int k = 0; k < b.Players.Count; k++)
        {
            Character c = heroes.First(h => h.Id == b.Players[k]);
            Hero hero = k == 0 ? b.Start!.Hero : b.Start!.Allies[k - 1];
            long xp = Progression.FightXp(f, hero);
            long shards = Progression.FightShards(f);
            List<ItemCount> items = [.. Equipment.Loot(f, Rpg.Core.Quests.LootWanted(GameData.Embedded, c.ActiveQuests, c.InventoryList), k)];
            Quest? quest = won ? GameData.Embedded.Quests.Values.FirstOrDefault(q => q.Scenario == b.Scenario && !c.QuestList.Contains(q.Id)) : null;
            if (quest is not null)
            {
                xp += quest.Xp;
                c.Quests = string.Join(',', c.QuestList.Append(quest.Id));
                items.AddRange(quest.Items ?? []);
            }
            string? scenarioWon = won ? b.Scenario : null;
            // A dungeon's room (sprint 121), as for a fight alone: won, the next room, or out with the last one's experience twice; lost, out.
            (string, int)? roomWon = null;
            if (Dungeons.Parse(b.Scenario) is (string dungeon, int room) && c.Run?.IsAt(dungeon, room) == true)
            {
                bool done = won && room == Dungeons.RoomCount;
                if (done)
                    xp += Progression.FightXp(f, hero);
                roomWon = won ? (dungeon, room) : null;
                (c.Dungeon, c.DungeonRoom) = won && !done ? (dungeon, room + 1) : ((string?)null, 0);
            }
            if (defeated.Count > 0 || roomWon is not null || scenarioWon is not null)
                c.QuestLog = Rpg.Core.Quests.Write(c.ActiveQuests.Select(p => Rpg.Core.Quests.AfterFight(GameData.Embedded.Quests[p.Quest], p, defeated, roomWon, scenarioWon)));
            c.Receive(items);
            c.Xp += xp;
            c.Shards += shards;
            int before = c.Level;
            c.Level = Progression.LevelFor(c.Xp);
            c.SetLife(c.Level > before ? int.MaxValue : won ? Math.Max(1, f.HeroFighters[k].Hp) : 1, now);
            c.InFight = false;
            results.Add(new SharedFightResult(c.Id, c.Name, xp, shards, items, c.Level, quest?.Id));
        }
        await db.SaveChangesAsync(cancel);
        if (b.Group is MonsterGroup group)
        {
            DateTimeOffset? until = hunts.End(group.Id, b.Starter, won, out bool itsFight);
            if (itsFight)
                await hub.Tell(group.Map, until is null ? new MapNews(MapNewsKind.Back, Group: group.Id) : new MapNews(MapNewsKind.Beaten, Group: group.Id, Until: until), cancel: cancel);
        }
        await Tell(b, new SharedFightNews(SharedFightKind.Over, b.Id, Results: results, Winner: f.WinningTeam), cancel);
    }
}

/// <summary>Ticks the fights with the party every quarter of a second (sprint 120): their fifteen seconds to join, their thirty-second turns.</summary>
public sealed partial class BattleClock(Battles battles, ILogger<BattleClock> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await battles.Tick(stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Failed(log, e);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "A fight with the party could not go on.")]
    private static partial void Failed(ILogger log, Exception e);
}
