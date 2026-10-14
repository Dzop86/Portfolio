using Rpg.Core;

namespace Rpg.Client;

/// <summary>A talk with an NPC: the line said, the answers; an answer leads on, ends the talk, or starts a fight.</summary>
public sealed class Conversation(Npc npc, Dialogue dialogue)
{
    public Npc Npc { get; } = npc ?? throw new ArgumentNullException(nameof(npc));
    public Dialogue Dialogue { get; } = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
    public string LineId { get; private set; } = dialogue.Start;
    public DialogueLine Line => Dialogue.Lines[LineId];
    public bool IsOver { get; private set; }

    /// <summary>The scenario the last answer started, if it started a fight.</summary>
    public string? Fight { get; private set; }

    public void Choose(int answer)
    {
        if (IsOver)
            throw new InvalidOperationException("The talk is over.");
        DialogueAnswer a = Line.Answers[answer];
        if (a.Next is string next)
        {
            LineId = next;
            return;
        }
        IsOver = true;
        Fight = a.Fight;
    }
}

/// <summary>
/// What a click did in town: the cells to walk, then a talk, a way to a fight or to another zone, or
/// neither. <see cref="Waypoints"/> is the same walk in straight lines (sprint 69), what the client draws.
/// </summary>
public sealed record TownStep(IReadOnlyList<Cell> Path, Npc? TalkTo, TownExit? Exit, ZoneLink? Link = null, MonsterGroup? Group = null, DungeonDoor? Door = null)
{
    public IReadOnlyList<Cell> Waypoints { get; init; } = Path;
}

/// <summary>
/// The player in a town, without an engine: a click walks the shortest way, in eight directions
/// (<see cref="Wander"/>), a click on someone walks up to them and opens the talk, a click on an exit
/// leads to its fight; "Attack" on a group of monsters (a right click, sprint 78) walks up to it and
/// starts its fight, a left click only walks up to it; walking onto the door of a dungeon (sprint 80)
/// asks to go in (<see cref="Door"/>); onto a workshop (sprint 81), opens its trade (<see cref="Bench"/>);
/// a new click during a walk stops it where the player is (<see cref="Interrupt"/>). The Godot client draws it;
/// the tests and <see cref="TownTour"/> play it.
/// </summary>
public sealed class TownController
{
    private readonly GameData _data;
    private readonly Dictionary<string, Cell> _groups = new(StringComparer.Ordinal);

    /// <param name="at">Where the player was (saved by the server); a cell they cannot stand on gives the arrival.</param>
    /// <param name="absent">Groups of monsters not on the map (beaten, not back yet: <see cref="MonsterPresence"/>).</param>
    public TownController(GameData data, string townId, Cell? at = null, IEnumerable<string>? absent = null)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        Town = data.Towns.TryGetValue(townId, out Town? t) ? t : throw new KeyNotFoundException($"Unknown town '{townId}'.");
        Board = Town.Board();
        HashSet<string> away = [.. absent ?? []];
        foreach (MonsterGroup g in data.GroupsOn(Town.Id).Where(g => !away.Contains(g.Id)))
            _groups[g.Id] = g.Home;
        Position = at is Cell c && CanStand(c) && GroupAt(c) is null ? c : Town.Spawn;
    }

    /// <summary>The groups of monsters on the map (sprint 72) and the cell each stands on.</summary>
    public IReadOnlyDictionary<string, Cell> Groups => _groups;

    /// <summary>The group of monsters standing on a cell, if any.</summary>
    public MonsterGroup? GroupAt(Cell c) => _groups.FirstOrDefault(g => g.Value == c).Key is string id ? _data.Groups[id] : null;

    /// <summary>The group the player walked up to, whose fight <see cref="Fight"/> is.</summary>
    public MonsterGroup? FightGroup { get; private set; }

    /// <summary>Where one may walk: free of decor, people and monsters.</summary>
    public bool IsFree(Cell c) => Town.IsFree(Board, c) && GroupAt(c) is null;

    /// <summary>A group wanders onto <paramref name="to"/>: a free cell where the player is not.</summary>
    public void MoveGroup(string id, Cell to)
    {
        if (!_groups.TryGetValue(id, out Cell at))
            throw new KeyNotFoundException($"No group '{id}' on {Town.Id}.");
        if (at != to && (!Board.Contains(to) || !IsFree(to) || to == Position))
            throw new ArgumentException($"The group cannot stand on {to}.", nameof(to));
        _groups[id] = to;
    }

    public Town Town { get; }
    public Board Board { get; }
    public GameData Data => _data;
    public Cell Position { get; private set; }

    /// <summary>The talk going on, if any: clicks on the town wait until it is over.</summary>
    public Conversation? Talk { get; private set; }

    /// <summary>The scenario to fight, once an exit or an answer chose one.</summary>
    public string? Fight { get; private set; }

    /// <summary>The way to another zone the player walked onto, if any (sprint 60).</summary>
    public ZoneLink? Travel { get; private set; }

    /// <summary>The door of a dungeon the player walked onto, if any (sprint 80): the game asks before going in.</summary>
    public DungeonDoor? Door { get; private set; }

    /// <summary>The workshop the player walked onto, if any (sprint 81): the game opens its trade's recipes.</summary>
    public Workshop? Bench { get; private set; }

    /// <summary>Not going in after all, or done at the bench: the player stays there, free to walk on.</summary>
    public void CloseDoor() => (Door, Bench) = (null, null);

    /// <summary>The map the way leads to, the player on its arrival cell, without the <paramref name="absent"/> groups.</summary>
    public TownController Enter(ZoneLink link, IEnumerable<string>? absent = null) =>
        new(_data, (link ?? throw new ArgumentNullException(nameof(link))).To, link.Arrival, absent);

    /// <summary>A free cell the player can walk to from the arrival (exits included).</summary>
    public bool CanStand(Cell c) => Town.CanStand(c);

    /// <summary>The cells a click on <paramref name="target"/> would walk, for the hover preview; null if it does nothing.</summary>
    public IReadOnlyList<Cell>? PathTo(Cell target)
    {
        if (Talk is not null || Fight is not null || Travel is not null || Door is not null || Bench is not null)
            return null;
        // Someone to talk to, or monsters to fight: up to the nearest cell next to them.
        if (Town.NpcAt(target) is not null || GroupAt(target) is not null)
        {
            return Wander.Steps.Select(s => target + s).Where(IsFree)
                .Select(n => n == Position ? [] : Wander.FindPath(Board, Position, n, IsFree))
                .OfType<IReadOnlyList<Cell>>()
                .OrderBy(p => p.Count)
                .FirstOrDefault();
        }
        if (target == Position)
            return [];
        return IsFree(target) ? Wander.FindPath(Board, Position, target, IsFree) : null;
    }

    /// <summary>
    /// A left click: walks there, or up to someone (and talks) or to a group of monsters (without
    /// fighting it: that is <see cref="Attack"/>, sprint 78).
    /// </summary>
    public TownStep? Click(Cell target) => Walk(target, attack: false);

    /// <summary>"Attack" in the menu of a group of monsters (a right click on it, sprint 78): walks up to it and starts its fight.</summary>
    public TownStep? Attack(Cell target) => GroupAt(target) is null ? null : Walk(target, attack: true);

    private TownStep? Walk(Cell target, bool attack)
    {
        if (PathTo(target) is not IReadOnlyList<Cell> path)
            return null;
        Cell start = Position;
        if (path.Count > 0)
            Position = path[^1];
        Npc? npc = Town.NpcAt(target);
        MonsterGroup? group = GroupAt(target);
        TownExit? exit = Town.ExitAt(Position);
        ZoneLink? link = Town.LinkAt(Position);
        DungeonDoor? door = Town.DoorAt(Position);
        Workshop? bench = Town.WorkshopAt(Position);
        bool there = npc is null && group is null && Position == target;
        if (npc is not null)
            Talk = new Conversation(npc, _data.Dialogues[npc.Dialogue]);
        else if (group is not null)
        {
            if (attack)
                (Fight, FightGroup) = (group.Scenario, group);
        }
        else if (exit is not null && there)
            Fight = exit.Scenario;
        else if (link is not null && there)
            Travel = link;
        else if (door is not null && there)
            Door = door;
        else if (bench is not null && there)
            Bench = bench;
        return new TownStep(path, npc, there ? exit : null, there ? link : null, attack ? group : null, there ? door : null)
        {
            Waypoints = Wander.Smooth(start, path, c => Board.Contains(c) && IsFree(c)),
        };
    }

    /// <summary>
    /// A walk cut short at <paramref name="reached"/>, a free cell the player has got to: they stand
    /// there, and what the walk was going to (a talk, a fight, another zone) does not happen.
    /// </summary>
    public void Interrupt(Cell reached)
    {
        if (!Board.Contains(reached) || !IsFree(reached))
            throw new ArgumentException($"The player cannot stand on {reached}.", nameof(reached));
        Position = reached;
        Talk = null;
        Fight = null;
        FightGroup = null;
        Travel = null;
        Door = null;
        Bench = null;
    }

    /// <summary>An answer in the talk going on; an answer that starts a fight sets <see cref="Fight"/>.</summary>
    public void Answer(int index)
    {
        Conversation talk = Talk ?? throw new InvalidOperationException("Nobody is talking.");
        talk.Choose(index);
        if (talk.IsOver)
        {
            Talk = null;
            Fight = talk.Fight;
        }
    }
}

/// <summary>
/// Walks the whole town through the player's clicks: up to every NPC, every line of their talk read
/// (an answer to a line not read yet first, then a goodbye), then through the first exit. The Godot
/// client runs it with its views (option --town-selftest), the tests without.
/// </summary>
public static class TownTour
{
    /// <summary>
    /// Goes through the whole world by its ways, from <paramref name="start"/>: each map entered once,
    /// by the first way found to it, depth first. Returns the maps visited.
    /// </summary>
    public static IReadOnlyList<string> World(TownController start, Action<TownController, TownStep>? afterClick = null)
    {
        ArgumentNullException.ThrowIfNull(start);
        var visited = new List<string> { start.Town.Id };
        void Visit(TownController here)
        {
            foreach (ZoneLink link in here.Town.Links ?? [])
            {
                if (visited.Contains(link.To))
                    continue;
                TownStep step = here.Click(link.At) ?? throw new InvalidOperationException($"Cannot walk to the way to {link.To}.");
                afterClick?.Invoke(here, step);
                TownController there = here.Enter(here.Travel!);
                // Back from there, the player stands on the way, free to walk to the next one.
                here.Interrupt(here.Position);
                visited.Add(there.Town.Id);
                Visit(there);
            }
        }
        Visit(start);
        return visited;
    }

    /// <returns>The number of lines read.</returns>
    public static int Run(TownController town, Action<TownStep>? afterClick = null, Action<Conversation>? afterLine = null)
    {
        ArgumentNullException.ThrowIfNull(town);
        int lines = 0;
        foreach (Npc npc in town.Town.Npcs)
        {
            TownStep step = town.Click(npc.At) ?? throw new InvalidOperationException($"Cannot walk up to {npc.Id}.");
            afterClick?.Invoke(step);
            var read = new HashSet<string>(StringComparer.Ordinal);
            while (town.Talk is Conversation talk)
            {
                read.Add(talk.LineId);
                lines++;
                afterLine?.Invoke(talk);
                IReadOnlyList<DialogueAnswer> answers = talk.Line.Answers;
                int next = Enumerable.Range(0, answers.Count).FirstOrDefault(i => answers[i].Next is string n && !read.Contains(n), -1);
                if (next < 0)
                    next = Enumerable.Range(0, answers.Count).FirstOrDefault(i => answers[i].Next is null && answers[i].Fight is null, 0);
                town.Answer(next);
            }
            if (town.Fight is not null)
                return lines;
        }
        TownExit exit = town.Town.Exits[0];
        afterClick?.Invoke(town.Click(exit.At) ?? throw new InvalidOperationException($"Cannot walk to the exit at {exit.At}."));
        return lines;
    }
}
