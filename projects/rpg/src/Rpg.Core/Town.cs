namespace Rpg.Core;

/// <summary>Someone to talk to in a town: a name, a model (look and outfit colour), a cell, a dialogue.</summary>
public sealed record Npc(string Id, LocalizedText Name, string Look, int Colour, Cell At, string Dialogue);

/// <summary>A cell of a town that leads to a fight (the town gate, the training ground).</summary>
public sealed record TownExit(Cell At, string Scenario, LocalizedText Name);

/// <summary>
/// A cell of a map that leads to another map (<see cref="To"/>), where the player arrives on
/// <see cref="Arrival"/>; <see cref="Name"/>, the zone it leads to when it leaves this one, none inside a zone (D64);
/// since sprint 129, a way that stays shut until the character has done <see cref="Quest"/> (Clairval's east gate, after
/// "Four Trails").
/// </summary>
public sealed record ZoneLink(Cell At, string To, Cell Arrival, LocalizedText? Name = null, string? Quest = null)
{
    /// <summary>Whether the way is open to a character who has done these quests.</summary>
    public bool OpenFor(IReadOnlyCollection<string> done) => Quest is null || (done ?? throw new ArgumentNullException(nameof(done))).Contains(Quest);
}

/// <summary>A trade's workshop on a map (sprint 81): walking onto it opens the trade's recipes.</summary>
public sealed record Workshop(Cell At, string Job);

/// <summary>
/// A beast of the village (sprint 104, D75: hens, horses, sheep), there to be looked at: it wanders round
/// where it stands, nobody fights it or talks to it, and it never stops anyone walking.
/// </summary>
public sealed record Animal(string Kind, Cell At)
{
    /// <summary>The kinds the game can draw.</summary>
    public static readonly IReadOnlyList<string> Kinds = ["hen", "horse", "sheep"];
}

/// <summary>
/// Something to examine on a map (sprint 105, D75): a clue a quest asks to find ("Interact"), seen as
/// <see cref="Look"/> ("stone", "claws", "mycelium"...), named, with what one reads in it when clicked
/// from beside it. Nobody stands on it. Its <see cref="Id"/> is the world's only one. Since sprint 107, it may start
/// a <see cref="Fight"/> (the loft's hatch), for a quest under way that asks to win it.
/// </summary>
public sealed record Prop(string Id, Cell At, string Look, LocalizedText Name, LocalizedText Text, string? Fight = null);

/// <summary>The door of a dungeon on a map of the world (sprint 80): walking onto it leads in.</summary>
public sealed record DungeonDoor(Cell At, string Dungeon);

/// <summary>
/// A map of the world, as described in <c>data/towns/*.json</c> (a town is one): one walks there and
/// talks; fights start from its exits. Its rows say what stands on each cell (see <see cref="Decor"/>);
/// the player arrives on <see cref="Spawn"/>. Its <see cref="Links"/> lead to the maps around it, each
/// on the edge that faces its neighbour; its <see cref="Level"/> says what level it is meant for, and
/// <see cref="MapAt"/> is its place in the world, one step per map, y growing southwards (sprint 60).
/// Since sprint 70 a zone is a grid of maps, all with the zone's name: <see cref="Zone"/> says which;
/// a map without one (the village) is a zone of its own.
/// </summary>
public sealed record Town(string Id, LocalizedText Name, IReadOnlyList<string> Rows, Cell Spawn, IReadOnlyList<Npc> Npcs, IReadOnlyList<TownExit> Exits,
    IReadOnlyList<ZoneLink>? Links = null, int Level = 1, Cell MapAt = default, string? Zone = null, IReadOnlyList<Workshop>? Workshops = null,
    string? Dungeon = null, IReadOnlyList<Animal>? Animals = null, IReadOnlyList<Prop>? Props = null)
{
    /// <summary>A room of a dungeon (sprint 86): a map apart from the world, its ways leading to the next room or out.</summary>
    public bool IsHall => Dungeon is not null;

    public Workshop? WorkshopAt(Cell c) => (Workshops ?? []).FirstOrDefault(w => w.At == c);

    public ZoneLink? LinkAt(Cell c) => (Links ?? []).FirstOrDefault(l => l.At == c);

    /// <summary>The doors of dungeons on this map, from <c>data/dungeons.json</c> (not the map's file).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<DungeonDoor> Doors { get; init; } = [];

    public DungeonDoor? DoorAt(Cell c) => Doors.FirstOrDefault(d => d.At == c);

    /// <summary>The zone this map belongs to: <see cref="Zone"/>, or the map itself.</summary>
    public string ZoneId => Zone ?? Id;

    public int Width => Rows.Count == 0 ? 0 : Rows[0].Length;

    /// <summary>
    /// What a character of the rows stands for. Walkable: '.' grass, '=' road, ',' flowers. Not walkable:
    /// 'H' a house (top-left of its 2 × 2 cells, the other three written 'h'), 'F' a fountain (2 × 2,
    /// 'f'), 'K' a dungeon's keep (3 × 3, 'k', sprint 86), 'Q' a hen house and 'E' a stable (2 × 2, 'q' and 'e', sprint 104), 'T' a tree, 'R' a rock, 'S' a market stall, 'L' a lantern, 'C' a cart, '~' water, 'M' a
    /// giant mushroom, 'B' a bush or a hedge (sprint 74), 'P' the village's palisade (sprint 102), 'X' a fence (sprint 104).
    /// </summary>
    public static readonly IReadOnlyDictionary<char, string> Decor = new Dictionary<char, string>
    {
        ['.'] = "grass",
        ['='] = "road",
        ['H'] = "house",
        ['h'] = "house",
        ['F'] = "fountain",
        ['f'] = "fountain",
        ['T'] = "tree",
        ['R'] = "rock",
        ['S'] = "stall",
        ['L'] = "lantern",
        ['C'] = "cart",
        ['~'] = "water",
        [','] = "flowers",
        ['M'] = "mushroom",
        ['B'] = "bush",
        ['K'] = "keep",
        ['k'] = "keep",
        ['P'] = "palisade",
        ['Q'] = "hen house",
        ['q'] = "hen house",
        ['E'] = "stable",
        ['e'] = "stable",
        ['X'] = "fence",
    };

    public char this[Cell c] => c.Y >= 0 && c.Y < Rows.Count && c.X >= 0 && c.X < Rows[c.Y].Length ? Rows[c.Y][c.X] : '~';

    /// <summary>The town as a board: grass, road and flowers are floor, water a hole, the rest obstacles.</summary>
    public Board Board() =>
        Core.Board.Parse([.. Rows.Select(r => string.Concat(r.Select(c => c switch { '.' or '=' or ',' => '.', '~' => '~', _ => '#' })))]);

    public Npc? NpcAt(Cell c) => Npcs.FirstOrDefault(n => n.At == c);

    /// <summary>The thing to examine on this cell (sprint 105), if any.</summary>
    public Prop? PropAt(Cell c) => (Props ?? []).FirstOrDefault(p => p.At == c);

    public TownExit? ExitAt(Cell c) => Exits.FirstOrDefault(e => e.At == c);

    /// <summary>Where one can stand: floor without anybody, nor anything to examine, on it.</summary>
    public bool IsFree(Board board, Cell c) => (board ?? throw new ArgumentNullException(nameof(board))).IsFloor(c) && NpcAt(c) is null && PropAt(c) is null;

    /// <summary>A cell the player can be on: free, and reachable on foot from the arrival.</summary>
    public bool CanStand(Cell c)
    {
        Board board = Board();
        return IsFree(board, c) && Pathfinding.FindPath(board, Spawn, c, x => IsFree(board, x)) is not null;
    }
}

/// <summary>One answer of a dialogue: it leads to another line (<see cref="Next"/>), to a fight, opens the stall (sprint 97), or ends the talk.</summary>
public sealed record DialogueAnswer(LocalizedText Text, string? Next = null, string? Fight = null, string? Take = null, string? HandIn = null, bool Shop = false);

/// <summary>A line an NPC says, and the answers the player may give (1 to 4).</summary>
public sealed record DialogueLine(LocalizedText Text, IReadOnlyList<DialogueAnswer> Answers);

/// <summary>A dialogue, as described in <c>data/dialogues/*.json</c>: lines by id, starting with <see cref="Start"/>.</summary>
public sealed record Dialogue(string Id, string Start, IReadOnlyDictionary<string, DialogueLine> Lines);
