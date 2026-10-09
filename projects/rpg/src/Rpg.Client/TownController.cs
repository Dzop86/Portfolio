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

/// <summary>What a click did in town: the cells to walk, then a talk or a way out, or neither.</summary>
public sealed record TownStep(IReadOnlyList<Cell> Path, Npc? TalkTo, TownExit? Exit);

/// <summary>
/// The player in a town, without an engine: a click walks the shortest way, a click on someone walks
/// up to them and opens the talk, a click on an exit leads to its fight. The Godot client draws it;
/// the tests and <see cref="TownTour"/> play it.
/// </summary>
public sealed class TownController
{
    private readonly GameData _data;

    /// <param name="at">Where the player was (saved by the server); a cell they cannot stand on gives the arrival.</param>
    public TownController(GameData data, string townId, Cell? at = null)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        Town = data.Towns.TryGetValue(townId, out Town? t) ? t : throw new KeyNotFoundException($"Unknown town '{townId}'.");
        Board = Town.Board();
        Position = at is Cell c && CanStand(c) ? c : Town.Spawn;
    }

    public Town Town { get; }
    public Board Board { get; }
    public Cell Position { get; private set; }

    /// <summary>The talk going on, if any: clicks on the town wait until it is over.</summary>
    public Conversation? Talk { get; private set; }

    /// <summary>The scenario to fight, once an exit or an answer chose one.</summary>
    public string? Fight { get; private set; }

    /// <summary>A free cell the player can walk to from the arrival (exits included).</summary>
    public bool CanStand(Cell c) => Town.CanStand(c);

    /// <summary>The cells a click on <paramref name="target"/> would walk, for the hover preview; null if it does nothing.</summary>
    public IReadOnlyList<Cell>? PathTo(Cell target)
    {
        if (Talk is not null || Fight is not null)
            return null;
        if (Town.NpcAt(target) is not null)
        {
            return target.Neighbours().Where(n => Town.IsFree(Board, n))
                .Select(n => n == Position ? [] : Pathfinding.FindPath(Board, Position, n, c => Town.IsFree(Board, c)))
                .OfType<IReadOnlyList<Cell>>()
                .OrderBy(p => p.Count)
                .FirstOrDefault();
        }
        if (target == Position)
            return [];
        return Town.IsFree(Board, target) ? Pathfinding.FindPath(Board, Position, target, c => Town.IsFree(Board, c)) : null;
    }

    public TownStep? Click(Cell target)
    {
        if (PathTo(target) is not IReadOnlyList<Cell> path)
            return null;
        if (path.Count > 0)
            Position = path[^1];
        Npc? npc = Town.NpcAt(target);
        TownExit? exit = Town.ExitAt(Position);
        if (npc is not null)
            Talk = new Conversation(npc, _data.Dialogues[npc.Dialogue]);
        else if (exit is not null && Position == target)
            Fight = exit.Scenario;
        return new TownStep(path, npc, npc is null && Position == target ? exit : null);
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
