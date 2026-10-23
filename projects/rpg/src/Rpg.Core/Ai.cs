namespace Rpg.Core;

/// <summary>
/// A simple, deterministic opponent. On its turn it plans its casts: the set of spells its action
/// points can pay for that is worth the most (<see cref="BestTurn"/>), and casts the strongest of
/// them, unless a cell within its walk offers a turn worth much more (<see cref="StepAwayGain"/>):
/// it walks there first (a ranged fighter steps back from melee); a sure kill comes before anything.
/// With nothing to cast it walks to the cell offering the best turn, otherwise towards the nearest
/// enemy, but never with its action points spent; then it ends its turn. It only ever proposes
/// actions the rules accept. Since sprint 122, a hero fighting beside other heroes plays as a team
/// (<see cref="Focus"/>); a hero alone plays as before.
/// </summary>
public static class Ai
{
    /// <summary>How much more a turn from another cell must be worth to walk there before casting.</summary>
    internal const double StepAwayGain = 1.5;

    /// <summary>What finishing off an enemy is worth, on top of the damage.</summary>
    internal const double KillBonus = 10;

    /// <summary>Team play (sprint 122): damage to a monster that can heal is worth this much more.</summary>
    internal const double HealerFocus = 1.5;

    /// <summary>Team play (sprint 122): damage to a monster another hero hit this round is worth this much more.</summary>
    internal const double SharedTargetFocus = 1.25;

    /// <summary>Team play (sprint 122): the part of the damage lost for each monster a hit would alert (a passive like the bees' Alarm).</summary>
    internal const double AlertPenalty = 0.1;

    /// <summary>The part of a glyph's worth the AI counts when laying it (sprint 130): the fighter on it may walk away.</summary>
    internal const double GlyphOdds = 0.6;

    /// <summary>What one step closer to an enemy is worth against a glyph's damage, when the AI only walks (sprint 130).</summary>
    internal const double StepWorth = 4;

    // The monsters each fight's other heroes hit this round, with the number of events read (sprint 122).
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Fight, Tuple<int, int, HashSet<int>>> HitThisRound = [];

    /// <summary>The next action of the current fighter.</summary>
    public static FightAction Decide(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        Fighter me = fight.Current;
        (Spell Spell, Cell Target, double Value, double Turn)? here = BestTurn(fight, me, me.Cell);
        // A sure kill is never worth walking away from: the value of a cast stops at the target's hit points.
        bool kills = here is var (aimSpell, aimed, _, _) && Kills(fight, me, aimSpell, me.Cell, aimed);
        if (here is var (spell, target, _, _) && (me.Mp == 0 || kills))
            return new CastAction(spell.Id, target);
        if (me.Mp == 0)
            return BestLeap(fight, me, 0) is var (jump, onto) ? new CastAction(jump.Id, onto) : new EndTurnAction();
        IReadOnlyDictionary<Cell, int> reachable = fight.ReachableCells();

        // Walk where the turn is worth the most, then the shortest walk; with a cast possible here,
        // only for a turn worth much more. The glyphs that would strike on the way or there count against
        // a cell, those under the fighter against staying (sprint 130).
        Cell? best = null;
        double stay = GlyphRisk(fight, me, me.Cell);
        double bestValue = (here is var (_, _, _, t) ? t * StepAwayGain : 0) - stay;
        foreach ((Cell cell, _) in Ordered(reachable))
        {
            if (BestTurn(fight, me, cell) is (_, _, _, double value) && value - GlyphRisk(fight, me, cell) > bestValue)
            {
                (best, bestValue) = (cell, value - GlyphRisk(fight, me, cell));
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

        // Nothing left to cast this turn: stay, so that a ranged fighter keeps its distance; off a glyph
        // that would strike it at its next turn, to the nearest cell that costs less (sprint 130).
        if (!me.Spells.Any(sp => sp.ApCost <= me.Ap && me.CastsLeft(sp) > 0 && me.CooldownLeft(sp) == 0))
        {
            Cell? safer = null;
            double safest = stay;
            foreach ((Cell cell, _) in Ordered(reachable))
            {
                if (GlyphRisk(fight, me, cell) < safest)
                    (safer, safest) = (cell, GlyphRisk(fight, me, cell));
            }
            return safer is Cell off ? new MoveAction(off) : new EndTurnAction();
        }

        // Nothing in reach: get closer, counting real walks around obstacles, and the glyphs on the way (sprint 130).
        IReadOnlyDictionary<Cell, int> toEnemy = WalkingDistanceToEnemies(fight, me);
        int distance = toEnemy.GetValueOrDefault(me.Cell, int.MaxValue);
        Cell? closer = null;
        double closest = distance == int.MaxValue ? double.MaxValue : distance * StepWorth + stay;
        foreach ((Cell cell, _) in Ordered(reachable))
        {
            int d = toEnemy.GetValueOrDefault(cell, int.MaxValue);
            if (d == int.MaxValue)
                continue;
            double score = d * StepWorth + GlyphRisk(fight, me, cell);
            if (score < closest)
            {
                (closer, closest) = (cell, score);
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
        if (Kills(fight, me, topSpell, from, topTarget))
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
                if (BestTurn(fight, me, landing, me.Ap - leap.ApCost) is (_, _, _, double value) && value - GlyphRisk(fight, me, landing, [landing]) > toBeat)
                {
                    best = (leap, landing);
                    toBeat = value - GlyphRisk(fight, me, landing, [landing]);
                }
            }
        }
        return best;
    }

    /// <summary>
    /// What the glyphs would cost <paramref name="me"/> going to <paramref name="to"/> (sprint 130): each glyph that
    /// would strike it on the cells it would walk (the board's path, or <paramref name="way"/>) and has not struck it
    /// this turn, once; then each one on <paramref name="to"/> again, at the start of its next turn. 0 without glyphs.
    /// </summary>
    internal static double GlyphRisk(Fight fight, Fighter me, Cell to, IReadOnlyList<Cell>? way = null)
    {
        Glyph[] hostile = [.. fight.Glyphs.Where(g => fight.Strikes(g, me))];
        if (hostile.Length == 0)
            return 0;
        IReadOnlyList<Cell> path = way ?? (to == me.Cell ? [] : fight.PathTo(to) ?? []);
        double risk = hostile.Where(g => !g.Struck.Contains(me.Id) && path.Any(g.Cells.Contains)).Sum(g => GlyphWorth(fight, g, me));
        return risk + hostile.Where(g => g.Cells.Contains(to)).Sum(g => GlyphWorth(fight, g, me));
    }

    /// <summary>What a glyph striking <paramref name="f"/> once is worth: its average damage, at most the hit points, and its status.</summary>
    private static double GlyphWorth(Fight fight, Glyph g, Fighter f)
    {
        Fighter caster = fight.Fighters[g.Caster];
        GlyphEffect e = g.Effect;
        double hit = Math.Min(e.AverageDamage * (100 + caster.Spec.Characteristics.For(e.Element) + caster.DamageBonusFor(e.Element)) * (100 - f.Resistance(e.Element)) / 10_000, f.Hp + f.Shield);
        return hit + (e.AsStatus is StatusEffect st ? StatusWorth(st, f) : 0);
    }

    /// <summary>A status's worth on a fighter, whichever side it is on: what <see cref="Value"/> counts for it.</summary>
    private static double StatusWorth(StatusEffect st, Fighter f) => st.Stat switch
    {
        Stat.Poison => 0.6 * Math.Min(st.Value * st.Turns, f.Hp),
        Stat.Ap or Stat.Mp => -2.0 * st.Value * st.Turns,
        _ => -0.05 * st.Value * st.Turns,
    };

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

    /// <summary>
    /// Whether a cast on <paramref name="target"/> surely kills an enemy there: its worth, without team play
    /// (<see cref="Focus"/> only chooses the target), reaches the enemy's hit points and shields.
    /// </summary>
    private static bool Kills(Fight fight, Fighter me, Spell spell, Cell from, Cell target)
    {
        double value = Value(fight, me, spell, from, target, focus: false);
        return fight.Fighters.Any(f => f.IsAlive && f.Cell == target && f.Team != me.Team && value >= f.Hp + f.Shield);
    }

    /// <summary>
    /// Team play (sprint 122, T90): how much damage to <paramref name="enemy"/> is worth when <paramref name="me"/> is a
    /// hero fighting beside at least one other hero, as players would play: the monsters that heal first
    /// (<see cref="HealerFocus"/>), all on the same monster (<see cref="SharedTargetFocus"/>, one another hero hit this
    /// round), and a monster that would alert others when hit, and survive, last (<see cref="AlertPenalty"/> for each). 1 otherwise,
    /// so that a hero alone, a monster and every fight recorded before plays exactly as before.
    /// </summary>
    internal static double Focus(Fight fight, Fighter me, Fighter enemy, bool kills = false)
    {
        IReadOnlyList<Fighter> heroes = fight.HeroFighters;
        if (heroes.Count < 2 || !heroes.Contains(me) || enemy.Team == me.Team)
            return 1;
        double focus = 1;
        if (enemy.Spells.Any(sp => sp.AllEffects.Any(e => e is HealEffect && e.Affects != Affects.Enemies)))
            focus *= HealerFocus;
        if (HitByOtherHeroes(fight, me).Contains(enemy.Id))
            focus *= SharedTargetFocus;
        int alerted = 0;
        foreach (PassiveSpec passive in enemy.Passives.Where(p => !kills && p.Trigger == Trigger.Hit && p.Area is not null
            && p.AllEffects.Any(e => e.Affects is Affects.Allies or Affects.All)))
        {
            alerted += passive.Area!.Cells(enemy.Cell, enemy.Cell).Skip(1).Select(fight.At)
                .Count(f => f is not null && f.Team == enemy.Team);
        }
        return focus * Math.Max(0, 1 - AlertPenalty * alerted);
    }

    /// <summary>
    /// The enemies the heroes other than <paramref name="me"/> damaged since the round began, read from the events
    /// back to the round's first turn; kept per fight while no event is added.
    /// </summary>
    private static HashSet<int> HitByOtherHeroes(Fight fight, Fighter me)
    {
        IReadOnlyList<FightEvent> events = fight.Events;
        if (HitThisRound.TryGetValue(fight, out Tuple<int, int, HashSet<int>>? known) && known.Item1 == events.Count && known.Item2 == me.Id)
            return known.Item3;
        var hit = new HashSet<int>();
        var damaged = new List<int>();
        for (int i = events.Count - 1; i >= 0; i--)
        {
            if (events[i] is Damaged d)
            {
                damaged.Add(d.Fighter);
            }
            else if (events[i] is TurnStarted turn)
            {
                if (turn.Round != fight.Round)
                    break;
                if (turn.Fighter != me.Id && fight.HeroFighters.Any(h => h.Id == turn.Fighter))
                    hit.UnionWith(damaged.Where(id => fight.Fighters[id].Team != me.Team));
                damaged.Clear();
            }
        }
        HitThisRound.AddOrUpdate(fight, Tuple.Create(events.Count, me.Id, hit));
        return hit;
    }

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
    /// average damage capped at the target's hit points, as before effects existed. With
    /// <paramref name="focus"/>, the damage to each enemy is weighed by team play (<see cref="Focus"/>).
    /// </summary>
    internal static double Value(Fight fight, Fighter me, Spell spell, Cell from, Cell target, bool focus = true)
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
                value += f.Team == me.Team ? -1.5 * hit : (f.IsSummon ? 0.5 * hit : hit) * (focus ? Focus(fight, me, f, hit >= f.Hp + f.Shield) : 1);
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
                    // A glyph under a fighter strikes at its next turn, unless it walks away (sprint 130).
                    GlyphEffect g => -sign * GlyphOdds * (Math.Min(g.AverageDamage * (100 + me.Spec.Characteristics.For(g.Element) + me.DamageBonusFor(g.Element)) * (100 - f.Resistance(g.Element)) / 10_000, f.Hp)
                        + (g.AsStatus is StatusEffect gs ? StatusWorth(gs, f) : 0)),
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
