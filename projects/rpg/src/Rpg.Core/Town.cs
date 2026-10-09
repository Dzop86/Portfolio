namespace Rpg.Core;

/// <summary>Someone to talk to in a town: a name, a model (look and outfit colour), a cell, a dialogue.</summary>
public sealed record Npc(string Id, LocalizedText Name, string Look, int Colour, Cell At, string Dialogue);

/// <summary>A cell of a town that leads to a fight (the town gate, the training ground).</summary>
public sealed record TownExit(Cell At, string Scenario, LocalizedText Name);

/// <summary>
/// A town, as described in <c>data/towns/*.json</c>: no monsters, one walks there and talks. Its rows
/// say what stands on each cell (see <see cref="Decor"/>); the player arrives on <see cref="Spawn"/>.
/// </summary>
public sealed record Town(string Id, LocalizedText Name, IReadOnlyList<string> Rows, Cell Spawn, IReadOnlyList<Npc> Npcs, IReadOnlyList<TownExit> Exits)
{
    /// <summary>
    /// What a character of the rows stands for. Walkable: '.' grass, '=' road. Not walkable: 'H' a house
    /// (top-left of its 2 × 2 cells, the other three written 'h'), 'F' a fountain (2 × 2, 'f'), 'T' a
    /// tree, 'R' a rock, 'S' a market stall, 'L' a lantern, 'C' a cart, '~' water.
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
    };

    public char this[Cell c] => c.Y >= 0 && c.Y < Rows.Count && c.X >= 0 && c.X < Rows[c.Y].Length ? Rows[c.Y][c.X] : '~';

    /// <summary>The town as a board: grass and road are floor, water a hole, the rest obstacles.</summary>
    public Board Board() =>
        Core.Board.Parse([.. Rows.Select(r => string.Concat(r.Select(c => c switch { '.' or '=' => '.', '~' => '~', _ => '#' })))]);

    public Npc? NpcAt(Cell c) => Npcs.FirstOrDefault(n => n.At == c);

    public TownExit? ExitAt(Cell c) => Exits.FirstOrDefault(e => e.At == c);

    /// <summary>Where one can stand: floor without anybody on it.</summary>
    public bool IsFree(Board board, Cell c) => (board ?? throw new ArgumentNullException(nameof(board))).IsFloor(c) && NpcAt(c) is null;

    /// <summary>A cell the player can be on: free, and reachable on foot from the arrival.</summary>
    public bool CanStand(Cell c)
    {
        Board board = Board();
        return IsFree(board, c) && Pathfinding.FindPath(board, Spawn, c, x => IsFree(board, x)) is not null;
    }
}

/// <summary>One answer of a dialogue: it leads to another line (<see cref="Next"/>), to a fight, or ends the talk.</summary>
public sealed record DialogueAnswer(LocalizedText Text, string? Next = null, string? Fight = null);

/// <summary>A line an NPC says, and the answers the player may give (1 to 4).</summary>
public sealed record DialogueLine(LocalizedText Text, IReadOnlyList<DialogueAnswer> Answers);

/// <summary>A dialogue, as described in <c>data/dialogues/*.json</c>: lines by id, starting with <see cref="Start"/>.</summary>
public sealed record Dialogue(string Id, string Start, IReadOnlyDictionary<string, DialogueLine> Lines);
