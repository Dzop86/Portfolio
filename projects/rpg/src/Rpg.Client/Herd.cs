using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The beasts of a map wandering (sprint 104, D75: hens round their hen house, horses in their paddock,
/// sheep in their meadow): each keeps to its pasture, the grass and flowers reached from where it stands
/// without crossing a road, a way or a workshop, no more than <see cref="Reach"/> steps away; a fence
/// keeps it in. It never stops the player, who walks through. Kept by the client only, for show.
/// </summary>
public sealed class Herd
{
    /// <summary>How far from its place a beast wanders, in steps.</summary>
    public const int Reach = 5;

    /// <summary>How far one walk of a beast goes, at most, in steps.</summary>
    public const int Stroll = 3;

    private readonly Cell[] _at;
    private readonly HashSet<Cell>[] _pastures;
    private readonly Board _board;

    public Herd(Town town)
    {
        ArgumentNullException.ThrowIfNull(town);
        Animals = town.Animals ?? [];
        _board = town.Board();
        _at = [.. Animals.Select(a => a.At)];
        _pastures = [.. Animals.Select(a => PastureOf(town, _board, a))];
    }

    public IReadOnlyList<Animal> Animals { get; }

    /// <summary>Where the beast <paramref name="i"/> stands now.</summary>
    public Cell At(int i) => _at[i];

    /// <summary>The cells the beast <paramref name="i"/> may wander on.</summary>
    public IReadOnlySet<Cell> Pasture(int i) => _pastures[i];

    /// <summary>
    /// The beast <paramref name="i"/> walks to a cell of its pasture no more than <see cref="Stroll"/> steps
    /// away, chosen by <paramref name="rng"/>: the cells walked (without the start), or null when it stays.
    /// </summary>
    public IReadOnlyList<Cell>? Roam(int i, Rng rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        Cell from = _at[i];
        HashSet<Cell> pasture = _pastures[i];
        Cell[] places = [.. pasture.Where(c => c != from && c.DistanceTo(from) <= Stroll).OrderBy(c => c.Y).ThenBy(c => c.X)];
        if (places.Length == 0)
            return null;
        Cell to = places[rng.Next(0, places.Length - 1)];
        IReadOnlyList<Cell>? path = Wander.FindPath(_board, from, to, pasture.Contains);
        if (path is { Count: > 0 })
            _at[i] = path[^1];
        return path;
    }

    private static HashSet<Cell> PastureOf(Town town, Board board, Animal a)
    {
        bool Grass(Cell c) => board.Contains(c) && board.IsFloor(c) && town[c] != '=' && town.LinkAt(c) is null
            && town.ExitAt(c) is null && town.WorkshopAt(c) is null && town.NpcAt(c) is null;
        var seen = new HashSet<Cell> { a.At };
        var todo = new Queue<Cell>([a.At]);
        while (todo.Count > 0)
        {
            Cell c = todo.Dequeue();
            foreach (Cell n in c.Neighbours())
            {
                if (n.DistanceTo(a.At) <= Reach && Grass(n) && seen.Add(n))
                    todo.Enqueue(n);
            }
        }
        return seen;
    }
}
