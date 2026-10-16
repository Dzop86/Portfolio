using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// A talk with an NPC: the line said, the answers; an answer leads on, ends the talk, or starts a fight.
/// Since sprint 88, the first line also offers the quests the NPC gives (<paramref name="offers"/>: its
/// words, "I accept" or "Later") and takes back those done (<paramref name="handIns"/>: its thanks).
/// </summary>
public sealed class Conversation(Npc npc, Dialogue dialogue, IReadOnlyList<Quest>? offers = null, IReadOnlyList<Quest>? handIns = null)
{
    private const string OfferLine = "quest:", HandInLine = "hand-in:";

    public Npc Npc { get; } = npc ?? throw new ArgumentNullException(nameof(npc));
    public Dialogue Dialogue { get; } = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
    public string LineId { get; private set; } = dialogue.Start;
    public bool IsOver { get; private set; }

    private readonly IReadOnlyList<Quest> _offers = offers ?? [], _handIns = handIns ?? [];

    public DialogueLine Line
    {
        get
        {
            if (LineId.StartsWith(OfferLine, StringComparison.Ordinal) && _offers.FirstOrDefault(q => q.Id == LineId[OfferLine.Length..]) is Quest offer)
                return new DialogueLine(offer.Text!, [new DialogueAnswer(Texts.Both("quest.accept"), Take: offer.Id), new DialogueAnswer(Texts.Both("quest.later"))]);
            if (LineId.StartsWith(HandInLine, StringComparison.Ordinal) && _handIns.FirstOrDefault(q => q.Id == LineId[HandInLine.Length..]) is Quest done)
                return new DialogueLine(done.Thanks!, [new DialogueAnswer(Texts.Both("quest.give"), HandIn: done.Id)]);
            DialogueLine line = Dialogue.Lines[LineId];
            if (LineId != Dialogue.Start)
                return line;
            // The quests to hand back first, then those to take.
            return line with
            {
                Answers = [.. _handIns.Select(q => new DialogueAnswer(Texts.Both("quest.handin", q.Name), HandInLine + q.Id)),
                    .. _offers.Select(q => new DialogueAnswer(Texts.Both("quest.offer", q.Name), OfferLine + q.Id)),
                    .. line.Answers],
            };
        }
    }

    /// <summary>The scenario the last answer started, if it started a fight.</summary>
    public string? Fight { get; private set; }

    /// <summary>The quest the last answer took, if any (sprint 88).</summary>
    public string? Taken { get; private set; }

    /// <summary>The quest the last answer handed back, if any (sprint 88).</summary>
    public string? HandedIn { get; private set; }

    /// <summary>Whether the last answer opened the stall (sprint 97).</summary>
    public bool Shop { get; private set; }

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
        (Fight, Taken, HandedIn, Shop) = (a.Fight, a.Take, a.HandIn, a.Shop);
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
/// a new click during a walk stops it where the player is (<see cref="Interrupt"/>). In a dungeon's hall
/// (sprint 86), the far door stays shut (<see cref="Shut"/>) while the room's group stands, and a click on
/// the fountain walks up to it to heal (<see cref="Spring"/>). The Godot client draws it;
/// the tests and <see cref="TownTour"/> play it.
/// </summary>
public sealed class TownController
{
    private readonly GameData _data;
    private readonly Dictionary<string, Cell> _groups = new(StringComparer.Ordinal);

    /// <param name="at">Where the player was (saved by the server); a cell they cannot stand on gives the arrival.</param>
    /// <param name="absent">Groups of monsters not on the map (beaten, not back yet: <see cref="MonsterPresence"/>).</param>
    /// <param name="run">Where the player is in a dungeon (sprint 86): in its halls, the rooms behind have their group beaten and their far door open.</param>
    public TownController(GameData data, string townId, Cell? at = null, IEnumerable<string>? absent = null, DungeonRun? run = null)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        Town = data.Towns.TryGetValue(townId, out Town? t) ? t : throw new KeyNotFoundException($"Unknown town '{townId}'.");
        Board = Town.Board();
        HashSet<string> away = [.. absent ?? []];
        Run = run;
        if (Town.Dungeon is string dungeon)
        {
            int room = data.Dungeons[dungeon].RoomOf(Town.Id);
            if (run?.Cleared(dungeon, room) == true)
                away.Add(data.Dungeons[dungeon].GroupOf(room));
            else
                _shut = [.. (Town.Links ?? []).Where(l => data.Towns[l.To].Dungeon == dungeon)];
        }
        foreach (MonsterGroup g in data.GroupsOn(Town.Id).Where(g => !away.Contains(g.Id)))
            _groups[g.Id] = g.Home;
        Position = at is Cell c && CanStand(c) && GroupAt(c) is null ? c : Town.Spawn;
    }

    private readonly HashSet<ZoneLink> _shut = [];

    /// <summary>The character's quests (sprint 88): the inhabitants offer theirs in their talks. None for a hero without a character.</summary>
    public QuestBook? Book { get; set; }

    /// <summary>The quests under way that asked to talk to the inhabitant the talk is with (the game tells the server).</summary>
    public IReadOnlyList<Quest> QuestTalks { get; private set; } = [];

    /// <summary>The quest the talk just ended took, or handed back (sprint 88): the game asks the server.</summary>
    public string? QuestTaken { get; private set; }

    /// <summary>Whether the last talk ended on Rose's stall (sprint 97); the game opens it and says it is seen to.</summary>
    public bool ShopOpened { get; private set; }

    public void ShopSeenTo() => ShopOpened = false;

    public string? QuestHandedIn { get; private set; }

    /// <summary>Where the player is in a dungeon, as the controller was given it.</summary>
    public DungeonRun? Run { get; }

    /// <summary>The ways that stay shut: a hall's far door while its group stands (sprint 86).</summary>
    public IReadOnlyCollection<ZoneLink> ShutWays => _shut;

    /// <summary>The shut way the player walked onto, if any: the game says why it does not open.</summary>
    public ZoneLink? Shut { get; private set; }

    /// <summary>The player walked up to a hall's fountain (sprint 86): the game heals them.</summary>
    public bool Spring { get; private set; }

    /// <summary>A hall's fountain (its four cells, 'F' and 'f'); none outside the dungeons.</summary>
    public bool IsSpring(Cell c) => Town.IsHall && Town[c] is 'F' or 'f' && Board.Contains(c);

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

    /// <summary>The gathering spots of this map (sprint 99).</summary>
    public IReadOnlyList<HarvestNode> Nodes => [.. _data.Nodes.Values.Where(n => n.Map == Town.Id)];

    public HarvestNode? NodeAt(Cell c) => _data.Nodes.Values.FirstOrDefault(n => n.Map == Town.Id && n.At == c);

    /// <summary>The spot the player walked up to, to gather it (sprint 99); the game gathers it, then says it is seen to.</summary>
    public HarvestNode? Gathering { get; private set; }

    public void GatheringSeenTo() => Gathering = null;

    /// <summary>Not going in after all, or done at the bench: the player stays there, free to walk on.</summary>
    public void CloseDoor() => (Door, Bench, Shut, Spring) = (null, null, null, false);

    /// <summary>The map the way leads to, the player on its arrival cell, without the <paramref name="absent"/> groups; in the same dungeon run.</summary>
    public TownController Enter(ZoneLink link, IEnumerable<string>? absent = null) =>
        new(_data, (link ?? throw new ArgumentNullException(nameof(link))).To, link.Arrival, absent, Run);

    /// <summary>A free cell the player can walk to from the arrival (exits included).</summary>
    public bool CanStand(Cell c) => Town.CanStand(c);

    /// <summary>The cells a click on <paramref name="target"/> would walk, for the hover preview; null if it does nothing.</summary>
    public IReadOnlyList<Cell>? PathTo(Cell target)
    {
        if (Talk is not null || Fight is not null || Travel is not null || Door is not null || Bench is not null || Shut is not null || Spring)
            return null;
        // A gathering spot (sprint 99): on it if one can stand there (a plot, herbs), else next to it.
        if (NodeAt(target) is not null && GroupAt(target) is null)
        {
            return new[] { target }.Concat(Wander.Steps.Select(s => target + s)).Where(c => Board.Contains(c) && IsFree(c))
                .Select(n => n == Position ? [] : Wander.FindPath(Board, Position, n, IsFree))
                .OfType<IReadOnlyList<Cell>>()
                .OrderBy(p => p.Count)
                .FirstOrDefault();
        }
        // Someone to talk to, monsters to fight or a fountain: up to the nearest cell next to them.
        if (Town.NpcAt(target) is not null || GroupAt(target) is not null || IsSpring(target))
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

    /// <summary>How near an edge a cell leads to the next map (sprints 83 and 85): on it, or up to two cells in.</summary>
    public const int EdgeReach = 2;

    /// <summary>
    /// As in the games of the genre (sprints 83 and 85, D66, D67): a cell near the edge of the map, decor
    /// included, or beyond it (the dark around the map), leads to the way out on that side the player can
    /// reach by the shortest walk; with the side's direction (one step out), for the arrow under the mouse.
    /// Beyond a corner, the side it is farthest beyond first, then the other. Null away from the edges, on
    /// sides without a way, on someone or on monsters.
    /// </summary>
    public (ZoneLink Way, Cell Direction)? EdgeWay(Cell target)
    {
        if (Town.NpcAt(target) is not null || GroupAt(target) is not null || Talk is not null || Fight is not null || Travel is not null || Door is not null || Bench is not null || Shut is not null || Spring)
            return null;
        (int distance, Cell direction, Func<Cell, bool> onSide)[] sides =
        [
            (target.X, new Cell(-1, 0), c => c.X == 0),
            (Board.Width - 1 - target.X, new Cell(1, 0), c => c.X == Board.Width - 1),
            (target.Y, new Cell(0, -1), c => c.Y == 0),
            (Board.Height - 1 - target.Y, new Cell(0, 1), c => c.Y == Board.Height - 1),
        ];
        // Beyond the map: the sides it is beyond, the farthest first; inside: those near enough, the nearest first.
        bool outside = !Board.Contains(target);
        foreach ((_, Cell toward, Func<Cell, bool> side) in sides.Where(s => outside ? s.distance < 0 : s.distance <= EdgeReach).OrderBy(s => s.distance))
        {
            ZoneLink? way = (Town.Links ?? []).Where(l => side(l.At))
                .Select(l => (Link: l, Path: l.At == Position ? [] : Wander.FindPath(Board, Position, l.At, IsFree)))
                .Where(p => p.Path is not null)
                .OrderBy(p => p.Path!.Count).ThenBy(p => p.Link.At.DistanceTo(target))
                .Select(p => p.Link)
                .FirstOrDefault();
            if (way is not null)
                return (way, toward);
        }
        return null;
    }

    /// <summary>
    /// A left click: walks there, or up to someone (and talks) or to a group of monsters (without
    /// fighting it: that is <see cref="Attack"/>, sprint 78); near an edge, to its way out (<see cref="EdgeWay"/>).
    /// </summary>
    public TownStep? Click(Cell target) =>
        Town.LinkAt(target) is null && Town.ExitAt(target) is null && Town.DoorAt(target) is null && Town.WorkshopAt(target) is null && EdgeWay(target) is ({ } way, _)
            ? Walk(way.At, attack: false)
            : Walk(target, attack: false);

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
        bool spring = npc is null && group is null && IsSpring(target);
        bool there = npc is null && group is null && !spring && Position == target;
        if (npc is not null)
        {
            // The character's quests (sprint 88): this inhabitant's to take and to hand back, and those asking to see them.
            string who = _data.RefOf(Town, npc);
            Talk = new Conversation(npc, _data.Dialogues[npc.Dialogue], Book?.Offers(who), Book?.HandIns(who));
            QuestTalks = Book?.Talks(who) ?? [];
        }
        else if (group is not null)
        {
            if (attack)
                (Fight, FightGroup) = (group.Scenario, group);
        }
        else if (exit is not null && there)
            Fight = exit.Scenario;
        else if (spring)
            Spring = true;
        else if (link is not null && there && _shut.Contains(link))
            Shut = link;
        else if (link is not null && there)
            Travel = link;
        else if (door is not null && there)
            Door = door;
        else if (bench is not null && there)
            Bench = bench;
        else if (NodeAt(target) is HarvestNode node && npc is null && group is null)
            Gathering = node;
        return new TownStep(path, npc, there ? exit : null, there && Shut is null ? link : null, attack ? group : null, there ? door : null)
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
        Shut = null;
        Spring = false;
        Gathering = null;
    }

    /// <summary>The quest taken or handed back, seen to by the game.</summary>
    public void QuestSeenTo() => (QuestTaken, QuestHandedIn) = (null, null);

    /// <summary>An answer in the talk going on; an answer that starts a fight sets <see cref="Fight"/>.</summary>
    public void Answer(int index)
    {
        Conversation talk = Talk ?? throw new InvalidOperationException("Nobody is talking.");
        talk.Choose(index);
        if (talk.IsOver)
        {
            Talk = null;
            Fight = talk.Fight;
            (QuestTaken, QuestHandedIn) = (talk.Taken, talk.HandedIn);
            ShopOpened = talk.Shop;
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
        // No gate to the training fight any more (sprint 102): the tour ends by the last one talked to.
        return lines;
    }
}
