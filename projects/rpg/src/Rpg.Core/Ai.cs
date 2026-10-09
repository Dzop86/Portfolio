namespace Rpg.Core;

/// <summary>
/// A simple, deterministic opponent. On its turn it plans its casts: the set of spells its action
/// points can pay for that is worth the most (<see cref="BestTurn"/>), and casts the strongest of
/// them, unless a cell within its walk offers a turn worth much more (<see cref="StepAwayGain"/>):
/// it walks there first (a ranged fighter steps back from melee); a sure kill comes before anything.
/// With nothing to cast it walks to the cell offering the best turn, otherwise towards the nearest
/// enemy, but never with its action points spent; then it ends its turn. It only ever proposes
/// actions the rules accept.
/// </summary>
public static class Ai
{
    /// <summary>How much more a turn from another cell must be worth to walk there before casting.</summary>
    internal const double StepAwayGain = 1.5;

    /// <summary>What finishing off an enemy is worth, on top of the damage.</summary>
    internal const double KillBonus = 10;

    /// <summary>The next action of the current fighter.</summary>
    public static FightAction Decide(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        Fighter me = fight.Current;
        (Spell Spell, Cell Target, double Value, double Turn)? here = BestTurn(fight, me, me.Cell);
        // A sure kill is never worth walking away from: the value of a cast stops at the target's hit points.
        bool kills = here is var (_, aimed, worth, _) && Kills(fight, me, aimed, worth);
        if (here is var (spell, target, _, _) && (me.Mp == 0 || kills))
            return new CastAction(spell.Id, target);
        if (me.Mp == 0)
            return new EndTurnAction();
        IReadOnlyDictionary<Cell, int> reachable = fight.ReachableCells();

        // Walk where the turn is worth the most, then the shortest walk; with a cast possible here,
        // only for a turn worth much more.
        Cell? best = null;
        double bestValue = here is var (_, _, _, t) ? t * StepAwayGain : 0;
        foreach ((Cell cell, _) in Ordered(reachable))
        {
            if (BestTurn(fight, me, cell) is (_, _, _, double value) && value > bestValue)
            {
                (best, bestValue) = (cell, value);
            }
        }
        if (best is Cell attackFrom)
            return new MoveAction(attackFrom);
        if (here is var (castSpell, castTarget, _, _))
            return new CastAction(castSpell.Id, castTarget);

        // Nothing left to cast this turn: stay, so that a ranged fighter keeps its distance.
        if (!me.Spells.Any(sp => sp.ApCost <= me.Ap && me.CastsLeft(sp) > 0 && me.CooldownLeft(sp) == 0))
            return new EndTurnAction();

        // Nothing in reach: get closer, counting real walks around obstacles.
        IReadOnlyDictionary<Cell, int> toEnemy = WalkingDistanceToEnemies(fight, me);
        int distance = toEnemy.GetValueOrDefault(me.Cell, int.MaxValue);
        Cell? closer = null;
        int closest = distance;
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
    /// The turn's plan from <paramref name="from"/>: each spell's best cast (<see cref="BestCast"/>),
    /// as many times as allowed, chosen to be worth the most within the action points (a small
    /// knapsack: two Spears beat one Arrow and two wasted points). Returns the strongest cast of the
    /// plan (listed first on a tie), its value and the plan's; a cast that kills is a plan of its
    /// own. Null when nothing is worth casting.
    /// </summary>
    internal static (Spell Spell, Cell Target, double Value, double Turn)? BestTurn(Fight fight, Fighter me, Cell from)
    {
        if (BestCast(fight, me, from) is not var (topSpell, topTarget, topValue))
            return null;
        if (Kills(fight, me, topTarget, topValue))
            return (topSpell, topTarget, topValue, topValue);
        // One item per possible cast: a spell with a cooldown is cast at most once.
        List<(Spell Spell, Cell Target, double Value)> casts = [];
        foreach (Spell spell in me.Spells)
        {
            if (BestCast(fight, me, from, spell) is var (s, t, v))
            {
                int times = spell.Cooldown > 0 ? 1 : Math.Min(me.CastsLeft(spell), me.Ap / spell.ApCost);
                casts.AddRange(Enumerable.Repeat((s, t, v), times));
            }
        }
        // worth[i, a]: the most the first i casts are worth with a action points; strict gains only,
        // so that on a tie the casts listed first are kept.
        double[,] worth = new double[casts.Count + 1, me.Ap + 1];
        for (int i = 1; i <= casts.Count; i++)
        {
            for (int a = 0; a <= me.Ap; a++)
            {
                worth[i, a] = worth[i - 1, a];
                int cost = casts[i - 1].Spell.ApCost;
                if (cost <= a && worth[i - 1, a - cost] + casts[i - 1].Value > worth[i, a])
                    worth[i, a] = worth[i - 1, a - cost] + casts[i - 1].Value;
            }
        }
        (Spell Spell, Cell Target, double Value)? first = null;
        for (int i = casts.Count, a = me.Ap; i > 0; i--)
        {
            if (worth[i, a] == worth[i - 1, a])
                continue;
            if (first is null || casts[i - 1].Value >= first.Value.Value)
                first = casts[i - 1];
            a -= casts[i - 1].Spell.ApCost;
        }
        return first is var (firstSpell, firstTarget, firstValue) ? (firstSpell, firstTarget, firstValue, worth[casts.Count, me.Ap]) : (topSpell, topTarget, topValue, topValue);
    }

    /// <summary>Whether a cast worth <paramref name="value"/> on <paramref name="target"/> surely kills an enemy there.</summary>
    private static bool Kills(Fight fight, Fighter me, Cell target, double value) =>
        fight.Fighters.Any(f => f.IsAlive && f.Cell == target && f.Team != me.Team && value >= f.Hp + f.Shield);

    /// <summary>
    /// The best spell and target cell from <paramref name="from"/>, aiming at a fighter's cell: the
    /// highest <see cref="Value"/>, then the weakest target, then the lowest fighter id, then the
    /// spell listed first. Null when nothing is worth casting.
    /// </summary>
    internal static (Spell Spell, Cell Target, double Value)? BestCast(Fight fight, Fighter me, Cell from, Spell? only = null)
    {
        (Spell, Cell, double)? best = null;
        (double Value, int Hp, int Id) bestKey = (0, int.MaxValue, int.MaxValue);
        foreach (Fighter aim in fight.Fighters)
        {
            if (!aim.IsAlive)
                continue;
            foreach (Spell spell in me.Spells)
            {
                if ((only is not null && spell != only) || fight.CheckCast(me, spell, from, aim.Cell) != ActionError.None)
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
        foreach (Spell spell in me.Spells.Where(sp => (only is null || sp == only) && sp.AllEffects.Any(e => e is SummonEffect)))
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
                double hit = Math.Min(spell.AverageDamage * (100 + me.Spec.Characteristics.For(spell.Element) + me.DamageBonus) * (100 - f.Resistance(spell.Element)) / 10_000, f.Hp + f.Shield);
                // A summon dies with its summoner: hitting it is worth half. An enemy finished off
                // hits back no more: a sure kill is worth a bonus.
                value += f.Team == me.Team ? -1.5 * hit : f.IsSummon ? 0.5 * hit : hit;
                if (f.Team != me.Team && !f.IsSummon && hit >= f.Hp + f.Shield)
                    value += KillBonus;
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
                    StatusEffect st => Renewed(me, f, st) * st.Stat switch
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

    /// <summary>
    /// The part of a status that is new: the same status from the same caster is renewed, not
    /// stacked (see <see cref="Fight"/>), so only the turns it adds count.
    /// </summary>
    private static double Renewed(Fighter me, Fighter f, StatusEffect st)
    {
        int left = f.Statuses.Where(s => s.Stat == st.Stat && s.Element == st.Element && s.Source == me.Id).Select(s => s.TurnsLeft).DefaultIfEmpty(0).Max();
        return Math.Max(0, st.Turns - left) / (double)st.Turns;
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
