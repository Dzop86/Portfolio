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
    /// The best spell and target cell from <paramref name="from"/>, aiming at a fighter's cell: the
    /// highest <see cref="Value"/>, then the weakest target, then the lowest fighter id, then the
    /// spell listed first. Null when nothing is worth casting.
    /// </summary>
    internal static (Spell Spell, Cell Target, double Value)? BestCast(Fight fight, Fighter me, Cell from)
    {
        (Spell, Cell, double)? best = null;
        (double Value, int Hp, int Id) bestKey = (0, int.MaxValue, int.MaxValue);
        foreach (Fighter aim in fight.Fighters)
        {
            if (!aim.IsAlive)
                continue;
            foreach (Spell spell in me.Spells)
            {
                if (fight.CheckCast(me, spell, from, aim.Cell) != ActionError.None)
                    continue;
                double value = Value(fight, me, spell, from, aim.Cell);
                bool better = value > bestKey.Value
                    || (value == bestKey.Value && (aim.Hp < bestKey.Hp || (aim.Hp == bestKey.Hp && aim.Id < bestKey.Id)));
                if (better)
                {
                    best = (spell, aim.Cell, value);
                    bestKey = (value, aim.Hp, aim.Id);
                }
            }
        }
        // Summons need a free cell: the free cells next to the caster, in a fixed order.
        foreach (Spell spell in me.Spells.Where(sp => sp.AllEffects.Any(e => e is SummonEffect)))
        {
            foreach (Cell cell in from.Neighbours())
            {
                if (!fight.IsFree(cell) || fight.CheckCast(me, spell, from, cell) != ActionError.None)
                    continue;
                double value = Value(fight, me, spell, from, cell);
                if (value > bestKey.Value)
                {
                    best = (spell, cell, value);
                    bestKey = (value, int.MaxValue, int.MaxValue);
                }
            }
        }
        return best;
    }

    /// <summary>
    /// What a cast is worth: average damage to enemies, capped at their hit points (a sure kill beats
    /// overkill), minus one and a half times the damage to allies; healing that fills missing hit
    /// points; a little for shields, statuses and pushes. A single-target damage spell is worth its
    /// average damage capped at the target's hit points, as before effects existed.
    /// </summary>
    internal static double Value(Fight fight, Fighter me, Spell spell, Cell from, Cell target)
    {
        IReadOnlyList<Fighter> area = fight.InArea(spell, from, target);
        double value = 0;
        if (spell.DamageMax > 0)
        {
            foreach (Fighter f in area)
            {
                double hit = Math.Min(spell.AverageDamage * (100 + me.DamageBonus) * (100 - f.Resistance(spell.Element)) / 10_000, f.Hp + f.Shield);
                value += f.Team == me.Team ? -1.5 * hit : hit;
            }
        }
        foreach (SpellEffect effect in spell.AllEffects)
        {
            // A creature on our side is worth a few hits, once, whoever stands nearby.
            if (effect is SummonEffect)
            {
                value += 8;
                continue;
            }
            IEnumerable<Fighter> touched = effect.Affects switch
            {
                Affects.Caster => [me],
                Affects.Enemies => area.Where(f => f.Team != me.Team),
                Affects.Allies => area.Where(f => f.Team == me.Team),
                _ => area,
            };
            foreach (Fighter f in touched)
            {
                double sign = f.Team == me.Team ? 1 : -1;
                value += effect switch
                {
                    HealEffect h => sign * Math.Min((h.Min + h.Max) / 2.0, f.MaxHp - f.Hp),
                    // Shields and statuses pay off later: worth a part of what they hold.
                    ShieldEffect sh => sign * 0.3 * sh.Amount,
                    StatusEffect st => st.Stat switch
                    {
                        Stat.Poison => -sign * 0.6 * Math.Min(st.Value * st.Turns, f.Hp),
                        Stat.Ap or Stat.Mp => sign * 2.0 * st.Value * st.Turns,
                        _ => sign * 0.05 * st.Value * st.Turns,
                    },
                    PushEffect or PullEffect => -sign * 1.0,
                    _ => 0,
                };
            }
        }
        return value;
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
