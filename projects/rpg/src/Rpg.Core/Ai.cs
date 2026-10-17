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

    /// <summary>
    /// What the hero drinks, if it carried something in (sprint 111, how the balance measures it): a healing potion
    /// below two fifths of its hit points, else, on its first turn, the oil of the element it strikes most in.
    /// The monsters carry nothing.
    /// </summary>
    private static string? Drink(Fight fight, Fighter me)
    {
        if (fight.Pack.Count == 0 || fight.UsedItem is not null)
            return null;
        Item[] usable = [.. fight.Pack.Where(i => fight.CheckUse(i) == ActionError.None).Select(i => fight.Data.Items[i])];
        if (me.Hp * 5 < me.MaxHp * 2 && usable.Where(i => i.Effects!.Any(e => e is HealEffect)).MaxBy(i => i.Level) is Item potion)
            return potion.Id;
        Element strongest = me.Spells.Where(s => s.DamageMax > 0).GroupBy(s => s.Element).OrderByDescending(g => me.Spec.Characteristics.For(g.Key)).ThenBy(g => g.Key).Select(g => g.Key).FirstOrDefault();
        return fight.Round == 1 && usable.Where(i => i.Effects!.Any(e => e is StatusEffect st && st.Element == strongest)).MaxBy(i => i.Level) is Item oil ? oil.Id : null;
    }

    /// <summary>The next action of the current fighter.</summary>
    public static FightAction Decide(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        Fighter me = fight.Current;
        if (Drink(fight, me) is string item)
            return new UseItemAction(item);
        (Spell Spell, Cell Target, double Value, double Turn)? here = BestTurn(fight, me, me.Cell);
        // A sure kill is never worth walking away from: the value of a cast stops at the target's hit points.
        bool kills = here is var (_, aimed, worth, _) && Kills(fight, me, aimed, worth);
        if (here is var (spell, target, _, _) && (me.Mp == 0 || kills))
            return new CastAction(spell.Id, target);
        if (me.Mp == 0)
            return BestLeap(fight, me, 0) is var (jump, onto) ? new CastAction(jump.Id, onto) : new EndTurnAction();
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
        // A leap is a walk (sprint 90): taken when the turn from where it lands, with the action points
        // left, beats the best walk, and the turn from here by as much.
        if (BestLeap(fight, me, bestValue) is var (leap, landing))
            return new CastAction(leap.Id, landing);
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
    internal static (Spell Spell, Cell Target, double Value, double Turn)? BestTurn(Fight fight, Fighter me, Cell from, int? apLeft = null)
    {
        int ap = apLeft ?? me.Ap;
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
                int times = spell.Cooldown > 0 ? 1 : Math.Min(me.CastsLeft(spell), ap / spell.ApCost);
                casts.AddRange(Enumerable.Repeat((s, t, v), times));
            }
        }
        // worth[i, a]: the most the first i casts are worth with a action points; strict gains only,
        // so that on a tie the casts listed first are kept.
        double[,] worth = new double[casts.Count + 1, ap + 1];
        for (int i = 1; i <= casts.Count; i++)
        {
            for (int a = 0; a <= ap; a++)
            {
                worth[i, a] = worth[i - 1, a];
                int cost = casts[i - 1].Spell.ApCost;
                if (cost <= a && worth[i - 1, a - cost] + casts[i - 1].Value > worth[i, a])
                    worth[i, a] = worth[i - 1, a - cost] + casts[i - 1].Value;
            }
        }
        (Spell Spell, Cell Target, double Value)? first = null;
        for (int i = casts.Count, a = ap; i > 0; i--)
        {
            if (worth[i, a] == worth[i - 1, a])
                continue;
            if (first is null || casts[i - 1].Value >= first.Value.Value)
                first = casts[i - 1];
            a -= casts[i - 1].Spell.ApCost;
        }
        return first is var (firstSpell, firstTarget, firstValue) ? (firstSpell, firstTarget, firstValue, worth[casts.Count, ap]) : (topSpell, topTarget, topValue, topValue);
    }

    /// <summary>
    /// The leap worth taking (sprint 90): among the free cells a leaping spell reaches, the one whose turn,
    /// with the action points left after the leap, is worth more than <paramref name="toBeat"/>; the
    /// nearest, then in reading order, on a tie. Null when no leap beats it.
    /// </summary>
    private static (Spell Spell, Cell Landing)? BestLeap(Fight fight, Fighter me, double toBeat)
    {
        (Spell, Cell)? best = null;
        foreach (Spell leap in me.Spells.Where(sp => sp.AllEffects.Any(e => e is LeapEffect)))
        {
            Cell[] cells = [.. Enumerable.Range(-leap.MaxRange, 2 * leap.MaxRange + 1)
                .SelectMany(dy => Enumerable.Range(-leap.MaxRange, 2 * leap.MaxRange + 1).Select(dx => me.Cell + new Cell(dx, dy)))
                .Where(c => fight.CheckCast(me, leap, me.Cell, c) == ActionError.None)
                .OrderBy(c => c.DistanceTo(me.Cell)).ThenBy(c => c.Y).ThenBy(c => c.X)];
            foreach (Cell landing in cells)
            {
                if (BestTurn(fight, me, landing, me.Ap - leap.ApCost) is (_, _, _, double value) && value > toBeat)
                {
                    best = (leap, landing);
                    toBeat = value;
                }
            }
        }
        return best;
    }

    /// <summary>A spell that trades places with an ally or pulls one (sprint 90).</summary>
    private static bool MovesAnAlly(Spell spell) =>
        spell.AllEffects.Any(e => e is SwapEffect || (e is PullEffect && e.Affects is Affects.Allies or Affects.All));

    /// <summary>
    /// Whether an ally should be swapped with or pulled away (sprint 90): below half its hit points with
    /// an enemy next to it; to take its place, the caster must have more hit points than it.
    /// </summary>
    private static bool NeedsRescue(Fight fight, Fighter me, Spell spell, Fighter ally) =>
        ally.Team == me.Team && ally != me && ally.Hp * 2 < ally.MaxHp
        && fight.Fighters.Any(f => f.IsAlive && f.Team != me.Team && f.Cell.DistanceTo(ally.Cell) == 1)
        && (!spell.AllEffects.Any(e => e is SwapEffect) || me.Hp > ally.Hp);

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
                if ((only is not null && spell != only) || fight.CheckCast(me, spell, from, aim.Cell) != ActionError.None
                    || (MovesAnAlly(spell) && !NeedsRescue(fight, me, spell, aim)))
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
                double hit = Math.Min(spell.AverageDamage * (100 + me.Spec.Characteristics.For(spell.Element) + me.DamageBonusFor(spell.Element)) * (100 - f.Resistance(spell.Element)) / 10_000, f.Hp + f.Shield);
                // A summon dies with its summoner: hitting it is worth half. An enemy finished off
                // hits back no more: a sure kill is worth a bonus.
                value += f.Team == me.Team ? -1.5 * hit : f.IsSummon ? 0.5 * hit : hit;
                if (f.Team != me.Team && !f.IsSummon && hit >= f.Hp + f.Shield)
                    value += KillBonus;
            }
        }
        foreach (SpellEffect effect in spell.AllEffects)
        {
            // A creature on our side is worth a few hits, once, whoever stands nearby; so is getting a hurt
            // ally out of reach (only cast then, see NeedsRescue).
            if (effect is SummonEffect || effect is SwapEffect || (effect is PullEffect && effect.Affects != Affects.Enemies && area.Any(f => f.Team == me.Team)))
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
