namespace Rpg.Core;

/// <summary>
/// A simple, deterministic opponent. On its turn it hits the hardest it can from where it stands;
/// otherwise it walks to the cell from which it can hit the hardest; otherwise it walks towards the
/// nearest enemy; then it ends its turn. It only ever proposes actions the rules accept.
/// </summary>
public static class Ai
{
    /// <summary>The next action of the current fighter.</summary>
    public static FightAction Decide(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        Fighter me = fight.Current;
        if (BestCast(fight, me, me.Cell) is (Spell spell, Cell target, _))
            return new CastAction(spell.Id, target);
        if (me.Mp == 0)
            return new EndTurnAction();
        IReadOnlyDictionary<Cell, int> reachable = fight.ReachableCells();

        // Walk where a spell reaches an enemy, the strongest hit first, then the shortest walk.
        Cell? best = null;
        double bestValue = 0;
        foreach ((Cell cell, _) in Ordered(reachable))
        {
            if (BestCast(fight, me, cell) is (_, _, double value) && value > bestValue)
            {
                (best, bestValue) = (cell, value);
            }
        }
        if (best is Cell attackFrom)
            return new MoveAction(attackFrom);

        // Nothing in reach: get closer, counting real walks around obstacles.
        IReadOnlyDictionary<Cell, int> toEnemy = WalkingDistanceToEnemies(fight, me);
        int here = toEnemy.GetValueOrDefault(me.Cell, int.MaxValue);
        Cell? closer = null;
        int closest = here;
        foreach ((Cell cell, _) in Ordered(reachable))
        {
            int d = toEnemy.GetValueOrDefault(cell, int.MaxValue);
            if (d < closest)
            {
                (closer, closest) = (cell, d);
            }
        }
        return closer is Cell step ? new MoveAction(step) : new EndTurnAction();
    }

    /// <summary>Lets the AI play every fighter until the fight ends; returns the number of actions.</summary>
    public static int PlayOut(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        int actions = 0;
        while (!fight.IsOver)
        {
            FightAction action = Decide(fight);
            ActionError error = fight.Apply(action);
            if (error != ActionError.None)
                throw new InvalidOperationException($"The AI proposed {action}, refused: {error}.");
            actions++;
        }
        return actions;
    }

    /// <summary>
    /// The best spell and enemy target from <paramref name="from"/>: the most damage on average,
    /// capped at the target's hit points (a sure kill beats overkill), then the weakest target,
    /// then the lowest fighter id, then the spell listed first.
    /// </summary>
    internal static (Spell Spell, Cell Target, double Value)? BestCast(Fight fight, Fighter me, Cell from)
    {
        (Spell, Cell, double)? best = null;
        (double Value, int Hp, int Id) bestKey = (0, int.MaxValue, int.MaxValue);
        foreach (Fighter enemy in fight.Fighters)
        {
            if (!enemy.IsAlive || enemy.Team == me.Team)
                continue;
            foreach (Spell spell in me.Spells)
            {
                if (spell.DamageMax == 0 || fight.CheckCast(me, spell, from, enemy.Cell) != ActionError.None)
                    continue;
                double value = Math.Min(spell.AverageDamage, enemy.Hp);
                bool better = value > bestKey.Value
                    || (value == bestKey.Value && (enemy.Hp < bestKey.Hp || (enemy.Hp == bestKey.Hp && enemy.Id < bestKey.Id)));
                if (better)
                {
                    best = (spell, enemy.Cell, value);
                    bestKey = (value, enemy.Hp, enemy.Id);
                }
            }
        }
        return best;
    }

    /// <summary>Reachable cells, nearest first, then in reading order: the AI's choices never depend on hashing.</summary>
    private static IEnumerable<(Cell Cell, int Steps)> Ordered(IReadOnlyDictionary<Cell, int> reachable) =>
        reachable.Select(kv => (kv.Key, kv.Value)).OrderBy(c => c.Value).ThenBy(c => c.Key.Y).ThenBy(c => c.Key.X);

    /// <summary>Steps from each free cell to the nearest enemy (a breadth-first search from all enemies).</summary>
    private static Dictionary<Cell, int> WalkingDistanceToEnemies(Fight fight, Fighter me)
    {
        var distance = new Dictionary<Cell, int>();
        var queue = new Queue<Cell>();
        foreach (Fighter enemy in fight.Fighters.Where(f => f.IsAlive && f.Team != me.Team))
        {
            distance[enemy.Cell] = 0;
            queue.Enqueue(enemy.Cell);
        }
        while (queue.TryDequeue(out Cell current))
        {
            foreach (Cell next in current.Neighbours())
            {
                bool walkable = next == me.Cell || fight.IsFree(next);
                if (walkable && distance.TryAdd(next, distance[current] + 1))
                    queue.Enqueue(next);
            }
        }
        return distance;
    }
}
