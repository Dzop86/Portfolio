namespace Rpg.Core;

/// <summary>A fighter during a fight. Its characteristics come from a <see cref="FighterSpec"/>.</summary>
public sealed class Fighter
{
    internal readonly Dictionary<string, int> CastsThisTurn = new(StringComparer.Ordinal);

    internal Fighter(int id, FighterSpec spec, Cell cell, IReadOnlyList<Spell> spells, int? summoner = null, string? summonKind = null)
    {
        Id = id;
        Spec = spec;
        Summoner = summoner;
        SummonKind = summonKind;
        Hp = spec.Hp + spec.Characteristics.Vitality;
        Ap = spec.Ap;
        Mp = spec.Mp;
        Cell = cell;
        Spells = spells;
    }

    /// <summary>The fighter's index in the scenario: stable, and the same in a replay.</summary>
    public int Id { get; }
    public FighterSpec Spec { get; }
    public LocalizedText Name => Spec.Name;
    public int Team => Spec.Team;
    public int Hp { get; internal set; }
    public int Ap { get; internal set; }
    public int Mp { get; internal set; }
    public Cell Cell { get; internal set; }
    public IReadOnlyList<Spell> Spells { get; }
    public bool IsAlive => Hp > 0;

    /// <summary>Hit points at most: the description's, plus Vitality, minus the erosion.</summary>
    public int MaxHp => Spec.Hp + Spec.Characteristics.Vitality - Eroded;

    /// <summary>Maximum hit points lost for the rest of the fight (<see cref="Fight.Erosion"/>).</summary>
    public int Eroded { get; internal set; }

    /// <summary>The fighter who summoned this one, if it is a summon.</summary>
    public int? Summoner { get; }

    public bool IsSummon => Summoner is not null;

    /// <summary>The summon's id in <c>data/summons.json</c>, if it is a summon.</summary>
    public string? SummonKind { get; }

    internal readonly List<Status> StatusList = [];
    internal readonly List<(int Amount, int TurnsLeft)> Shields = [];

    /// <summary>The statuses on the fighter, oldest first.</summary>
    public IReadOnlyList<Status> Statuses => StatusList;

    /// <summary>Damage the shields will absorb before the hit points.</summary>
    public int Shield => Shields.Sum(s => s.Amount);

    /// <summary>Resistance to an element now, in percent: the fighter's own and its statuses', at most 90.</summary>
    public int Resistance(Element element) => element == Element.Neutral ? 0
        : Math.Min(90, Spec.Resistance(element) + StatusList.Where(s => s.Stat == Stat.Resistance && (s.Element == element || s.Element == Element.Neutral)).Sum(s => s.Value));

    /// <summary>Extra damage the fighter deals now, in percent (statuses), at least -100.</summary>
    public int DamageBonus => Math.Max(-100, StatusList.Where(s => s.Stat == Stat.Damage).Sum(s => s.Value));

    public int CastsLeft(Spell spell) => spell.PerTurn - CastsThisTurn.GetValueOrDefault(spell.Id);

    internal readonly Dictionary<string, int> Cooldowns = new(StringComparer.Ordinal);

    /// <summary>
    /// The fighter's turn starts before it may cast the spell again (see <see cref="Spell.Cooldown"/>):
    /// 0 when it may cast it now.
    /// </summary>
    public int CooldownLeft(Spell spell) => Cooldowns.GetValueOrDefault(spell.Id);
}

/// <summary>
/// A turn-based fight between two teams, deterministic for a given scenario and seed: the only
/// randomness is the damage roll, drawn from <see cref="Rng"/>. Every action is checked first;
/// a refused one changes nothing and returns why.
/// </summary>
public sealed class Fight
{
    /// <summary>After this many rounds the fight is a draw, so that it always ends.</summary>
    public const int RoundLimit = 50;

    /// <summary>Hit points a hero gains with each level, unless its class says otherwise (<see cref="HeroClass.HpPerLevel"/>).</summary>
    public const int HpPerLevel = 3;

    /// <summary>Damage per cell a pushed fighter could not travel, against whatever stopped it.</summary>
    public const int CollisionDamage = 4;

    /// <summary>
    /// Percent of the damage to hit points that is also lost from the maximum, for the rest of the
    /// fight: heals cannot give it back, so that healing never outlasts damage forever.
    /// </summary>
    public const int Erosion = 10;

    private readonly List<Fighter> _order;
    private readonly List<FightAction> _history = [];
    private readonly List<FightEvent> _events = [];
    private readonly Rng _rng;
    private int _current;

    /// <param name="hero">The player's character, in place of the scenario's first fighter of team A; null keeps it.</param>
    /// <param name="rival">Another hero, in place of the scenario's first fighter of team B (class duels).</param>
    public Fight(GameData data, string scenarioId, ulong seed, Hero? hero = null, Hero? rival = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        Scenario = data.Scenarios.TryGetValue(scenarioId, out Scenario? s) ? s : throw new KeyNotFoundException($"Unknown scenario '{scenarioId}'.");
        if (hero?.Problem(data) is string problem)
            throw new ArgumentException(problem, nameof(hero));
        if (rival?.Problem(data) is string rivalProblem)
            throw new ArgumentException(rivalProblem, nameof(rival));
        Hero = hero;
        Rival = rival;
        Board = data.Board(Scenario.Map);
        Seed = seed;
        _rng = new Rng(seed);
        int heroIndex = Scenario.Fighters.ToList().FindIndex(f => f.Team == 0);
        int rivalIndex = Scenario.Fighters.ToList().FindIndex(f => f.Team == 1);
        _fighters = [.. Scenario.Fighters.Select((f, i) =>
        {
            FighterSpec spec = hero is not null && i == heroIndex ? AsHero(f, hero, data)
                : rival is not null && i == rivalIndex ? AsHero(f, rival, data)
                : f;
            return new Fighter(i, spec, Board.Starts[f.Team][f.Start], SpellsOf(spec, data));
        })];
        _data = data;
        // Highest initiative first; equal initiatives keep the scenario's order.
        _order = [.. _fighters.OrderByDescending(f => f.Spec.Initiative).ThenBy(f => f.Id)];
        Round = 1;
        _events.Add(new TurnStarted(Current.Id, Round));
    }

    /// <summary>
    /// The scenario's fighter, played by the hero: its name and look, its class's characteristics and
    /// the class's spells its level has unlocked.
    /// </summary>
    private static FighterSpec AsHero(FighterSpec f, Hero hero, GameData data)
    {
        FighterSpec spec = f with { Name = new LocalizedText(hero.Name, hero.Name), Look = hero.Look };
        if (data.Class(hero.Class) is not HeroClass c)
            return spec;
        // What the hero wears adds up with its own points; its action and movement points too.
        (Characteristics worn, int ap, int mp) = Equipment.Total(hero.Worn, data);
        return spec with
        {
            Hp = c.Hp + c.HpPerLevel * (hero.Level - 1),
            Ap = c.Ap + ap,
            Mp = c.Mp + mp,
            Initiative = c.Initiative,
            Spells = [.. c.Spells.Where(id => data.Spells[id].Level <= hero.Level)],
            Stats = Equipment.Add(hero.Stats ?? Characteristics.None, worn),
            SpellRanks = hero.Ranks,
            Level = hero.Level,
        };
    }

    /// <summary>A fighter's spells at their ranks (rank 1 unless the description says otherwise).</summary>
    private static List<Spell> SpellsOf(FighterSpec spec, GameData data) =>
        [.. spec.Spells.Select(id => data.Spells[id].AtRank(spec.SpellRanks?.GetValueOrDefault(id, 1) ?? 1))];

    public Scenario Scenario { get; }
    public Hero? Hero { get; }

    /// <summary>The hero playing team B's first fighter, if any.</summary>
    public Hero? Rival { get; }
    public Board Board { get; }
    public ulong Seed { get; }

    /// <summary>All fighters, by <see cref="Fighter.Id"/>; summons are added at the end as they appear.</summary>
    public IReadOnlyList<Fighter> Fighters => _fighters;

    private readonly List<Fighter> _fighters;
    private readonly GameData _data;

    /// <summary>The fighters in the order they play.</summary>
    public IReadOnlyList<Fighter> TurnOrder => _order;

    /// <summary>
    /// The next <paramref name="count"/> turns, the current one first: the living fighters in the
    /// order of play, round after round (the turn order's timeline).
    /// </summary>
    public IReadOnlyList<Fighter> NextTurns(int count)
    {
        var turns = new List<Fighter>();
        if (IsOver)
            return turns;
        for (int i = 0; turns.Count < count && i < count * _order.Count; i++)
        {
            Fighter f = _order[(_current + i) % _order.Count];
            if (f.IsAlive)
                turns.Add(f);
        }
        return turns;
    }

    /// <summary>
    /// What the current fighter casting <paramref name="spell"/> on <paramref name="target"/> would do
    /// to each fighter of the area, before shields: the damage of the lowest and highest rolls, and
    /// the healing, capped at the hit points missing. Nothing is drawn: the fight does not change.
    /// </summary>
    public IReadOnlyList<Forecast> Foresee(Spell spell, Cell target)
    {
        ArgumentNullException.ThrowIfNull(spell);
        Fighter me = Current;
        var forecasts = new List<Forecast>();
        IReadOnlyList<Fighter> area = InArea(spell, me.Cell, target);
        HealEffect[] heals = [.. spell.AllEffects.OfType<HealEffect>()];
        foreach (Fighter f in area.Concat(heals.Any(h => h.Affects == Affects.Caster) && !area.Contains(me) ? [me] : []))
        {
            bool hit = spell.DamageMax > 0 && area.Contains(f);
            int healMin = 0, healMax = 0;
            foreach (HealEffect h in heals.Where(h => h.Affects == Affects.Caster ? f == me : area.Contains(f) && Touches(h.Affects, me, f)))
            {
                healMin += h.Min * (100 + me.Spec.Characteristics.Intelligence) / 100;
                healMax += h.Max * (100 + me.Spec.Characteristics.Intelligence) / 100;
            }
            int missing = f.MaxHp - f.Hp;
            if (hit || healMax > 0)
            {
                forecasts.Add(new Forecast(f, hit ? Damage(spell.DamageMin, me, f, spell.Element) : 0, hit ? Damage(spell.DamageMax, me, f, spell.Element) : 0,
                    Math.Min(healMin, missing), Math.Min(healMax, missing), spell.Element));
            }
        }
        return forecasts;
    }
    public Fighter Current => _order[_current];
    public int Round { get; private set; }
    public bool IsOver { get; private set; }
    public int? WinningTeam { get; private set; }

    /// <summary>The accepted actions, in order: with the scenario and the seed, they are the whole fight.</summary>
    public IReadOnlyList<FightAction> History => _history;
    public IReadOnlyList<FightEvent> Events => _events;

    /// <summary>The living fighter on a cell, if any.</summary>
    public Fighter? At(Cell cell) => Fighters.FirstOrDefault(f => f.IsAlive && f.Cell == cell);

    public bool IsFree(Cell cell) => Board.IsFloor(cell) && At(cell) is null;

    /// <summary>Obstacles and living fighters block the view; holes do not.</summary>
    public bool BlocksSight(Cell cell) => Board[cell] == Terrain.Obstacle || At(cell) is not null;

    /// <summary>The cells the current fighter can walk to this turn, with their cost in movement points.</summary>
    public IReadOnlyDictionary<Cell, int> ReachableCells() => Pathfinding.Reachable(Board, Current.Cell, Current.Mp, IsFree);

    /// <summary>
    /// Whether <paramref name="caster"/> could cast <paramref name="spell"/> on <paramref name="target"/>
    /// if it stood on <paramref name="from"/> (its own cell then no longer blocks the view).
    /// </summary>
    public ActionError CheckCast(Fighter caster, Spell spell, Cell from, Cell target)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(spell);
        if (!caster.Spells.Contains(spell))
            return ActionError.UnknownSpell;
        if (caster.Ap < spell.ApCost)
            return ActionError.NotEnoughAp;
        if (caster.CastsLeft(spell) <= 0)
            return ActionError.CastLimit;
        if (caster.CooldownLeft(spell) > 0)
            return ActionError.Cooldown;
        if (!Board.Contains(target))
            return ActionError.OffBoard;
        if (!Board.IsFloor(target))
            return ActionError.NotFloor;
        int distance = from.DistanceTo(target);
        if (distance < spell.MinRange || distance > spell.MaxRange)
            return ActionError.OutOfRange;
        if (spell.InLine && !from.IsInLineWith(target))
            return ActionError.NotInLine;
        if (spell.LineOfSight && !LineOfSight.IsClear(from, target, c => c != caster.Cell && BlocksSight(c)))
            return ActionError.NoLineOfSight;
        foreach (SummonEffect summon in spell.AllEffects.OfType<SummonEffect>())
        {
            if (At(target) is not null && At(target) != caster)
                return ActionError.Occupied;
            if (Fighters.Count(f => f.IsAlive && f.Summoner == caster.Id && f.SummonKind == summon.Summon) >= summon.Max)
                return ActionError.TooManySummons;
        }
        return ActionError.None;
    }

    /// <summary>Why <paramref name="action"/> would be refused now, or <see cref="ActionError.None"/>.</summary>
    public ActionError Check(FightAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsOver)
            return ActionError.FightOver;
        Fighter me = Current;
        switch (action)
        {
            case MoveAction move:
                if (!Board.Contains(move.To))
                    return ActionError.OffBoard;
                if (!Board.IsFloor(move.To))
                    return ActionError.NotFloor;
                if (move.To == me.Cell || At(move.To) is not null)
                    return ActionError.Occupied;
                IReadOnlyList<Cell>? path = Pathfinding.FindPath(Board, me.Cell, move.To, IsFree);
                if (path is null)
                    return ActionError.Unreachable;
                return path.Count > me.Mp ? ActionError.NotEnoughMp : ActionError.None;
            case CastAction cast:
                Spell? spell = me.Spells.FirstOrDefault(s => s.Id == cast.Spell);
                return spell is null ? ActionError.UnknownSpell : CheckCast(me, spell, me.Cell, cast.Target);
            case EndTurnAction:
                return ActionError.None;
            default:
                throw new ArgumentException($"Unknown action {action.GetType().Name}.", nameof(action));
        }
    }

    /// <summary>Plays <paramref name="action"/> for the current fighter, or refuses it and changes nothing.</summary>
    public ActionError Apply(FightAction action)
    {
        ActionError error = Check(action);
        if (error != ActionError.None)
            return error;
        _history.Add(action);
        Fighter me = Current;
        switch (action)
        {
            case MoveAction move:
                IReadOnlyList<Cell> path = Pathfinding.FindPath(Board, me.Cell, move.To, IsFree)!;
                me.Mp -= path.Count;
                me.Cell = move.To;
                _events.Add(new Moved(me.Id, path));
                break;
            case CastAction cast:
                Cast(me, me.Spells.First(s => s.Id == cast.Spell), cast.Target);
                break;
            case EndTurnAction:
                NextTurn();
                break;
        }
        return ActionError.None;
    }

    /// <summary>
    /// The damage a hit of <paramref name="roll"/> does: more with the caster's damage bonus, less with
    /// the target's resistance to the element; a neutral hit without bonus is the roll itself.
    /// </summary>
    public static int Damage(int roll, Fighter caster, Fighter target, Element element)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(target);
        long scaled = (long)roll * (100 + caster.Spec.Characteristics.For(element) + caster.DamageBonus) * (100 - target.Resistance(element));
        return (int)Math.Max(0, scaled / 10_000);
    }

    /// <summary>The living fighters on the cells of a spell's area, in the area's order, each once.</summary>
    public IReadOnlyList<Fighter> InArea(Spell spell, Cell from, Cell target)
    {
        ArgumentNullException.ThrowIfNull(spell);
        var seen = new List<Fighter>();
        foreach (Cell c in spell.Zone.Cells(from, target))
        {
            if (Board.Contains(c) && At(c) is Fighter f && !seen.Contains(f))
                seen.Add(f);
        }
        return seen;
    }

    private static bool Touches(Affects affects, Fighter caster, Fighter other) => affects switch
    {
        Affects.Enemies => other.Team != caster.Team,
        Affects.Allies => other.Team == caster.Team,
        Affects.Caster => other == caster,
        _ => true,
    };

    private void Cast(Fighter me, Spell spell, Cell target)
    {
        me.Ap -= spell.ApCost;
        me.CastsThisTurn[spell.Id] = me.CastsThisTurn.GetValueOrDefault(spell.Id) + 1;
        // Counted down at the start of each of the caster's turns, so one more than the turns to skip.
        if (spell.Cooldown > 0)
            me.Cooldowns[spell.Id] = spell.Cooldown + 1;
        _events.Add(new SpellCast(me.Id, spell.Id, target));
        // The roll is drawn even on an empty cell: the sequence of draws depends only on the actions.
        int roll = _rng.Next(spell.DamageMin, spell.DamageMax);
        IReadOnlyList<Fighter> area = InArea(spell, me.Cell, target);
        if (spell.DamageMax > 0)
        {
            foreach (Fighter hit in area)
            {
                if (hit.IsAlive)
                    Hurt(hit, Damage(roll, me, hit, spell.Element), spell.Element);
            }
        }
        foreach (SpellEffect effect in spell.AllEffects)
        {
            // Healing grows with Intelligence, 1 % a point.
            int effectRoll = effect is HealEffect heal ? _rng.Next(heal.Min, heal.Max) * (100 + me.Spec.Characteristics.Intelligence) / 100 : 0;
            if (effect is SummonEffect summon)
            {
                Summon(me, _data.Summons[summon.Summon], target);
                continue;
            }
            IEnumerable<Fighter> touched = effect.Affects == Affects.Caster ? [me] : area.Where(f => Touches(effect.Affects, me, f));
            foreach (Fighter f in touched.ToList())
            {
                if (IsOver || !f.IsAlive)
                    continue;
                switch (effect)
                {
                    case HealEffect:
                        int healed = Math.Min(effectRoll, f.MaxHp - f.Hp);
                        f.Hp += healed;
                        _events.Add(new Healed(f.Id, healed, f.Hp));
                        break;
                    case ShieldEffect shield:
                        f.Shields.Add((shield.Amount, shield.Turns));
                        _events.Add(new Shielded(f.Id, shield.Amount, shield.Turns));
                        break;
                    case PushEffect push:
                        Shove(f, Cell.Direction(me.Cell, f.Cell), push.Cells, collide: true);
                        break;
                    case PullEffect pull:
                        Shove(f, Cell.Direction(f.Cell, me.Cell), Math.Min(pull.Cells, f.Cell.DistanceTo(me.Cell) - 1), collide: false);
                        break;
                    case StatusEffect status:
                        // The same status from the same caster is renewed, not stacked.
                        f.StatusList.RemoveAll(st => st.Stat == status.Stat && st.Element == status.Element && st.Source == me.Id);
                        f.StatusList.Add(new Status(status.Stat, status.Value, status.Turns, status.Element, me.Id));
                        _events.Add(new StatusAdded(f.Id, status.Stat, status.Value, status.Turns, status.Element));
                        break;
                }
            }
        }
        // A fighter who dies during its own turn hands over at once.
        if (!IsOver && !me.IsAlive)
            NextTurn();
    }

    /// <summary>A creature on a free cell, in the caster's team, playing right after it.</summary>
    private void Summon(Fighter me, SummonSpec m, Cell cell)
    {
        if (!IsFree(cell))
            return;
        var spec = new FighterSpec(m.Name, m.Look, me.Team, m.Hp, m.Ap, m.Mp, m.Initiative, 0, m.Spells);
        var summoned = new Fighter(_fighters.Count, spec, cell, SpellsOf(spec, _data), me.Id, m.Id);
        _fighters.Add(summoned);
        _order.Insert(_current + 1, summoned);
        _events.Add(new Summoned(summoned.Id, me.Id, cell));
    }

    /// <summary>Moves a fighter cell by cell; what stops a push hurts, per cell left.</summary>
    private void Shove(Fighter f, Cell direction, int cells, bool collide)
    {
        if (direction == new Cell(0, 0) || cells <= 0)
            return;
        var path = new List<Cell>();
        Cell at = f.Cell;
        for (int i = 0; i < cells; i++)
        {
            Cell next = at + direction;
            if (!IsFree(next))
                break;
            path.Add(next);
            at = next;
        }
        int blocked = collide ? cells - path.Count : 0;
        f.Cell = at;
        _events.Add(new Pushed(f.Id, path, blocked));
        if (blocked > 0)
            Hurt(f, blocked * CollisionDamage);
    }

    /// <summary>Damage through the shields first, then the hit points; a death may end the fight.</summary>
    private void Hurt(Fighter hit, int damage, Element element = Element.Neutral)
    {
        int absorbed = 0;
        for (int i = 0; i < hit.Shields.Count && damage > 0; i++)
        {
            (int amount, int turns) = hit.Shields[i];
            int take = Math.Min(amount, damage);
            hit.Shields[i] = (amount - take, turns);
            damage -= take;
            absorbed += take;
        }
        hit.Shields.RemoveAll(s => s.Amount == 0);
        if (absorbed > 0)
            _events.Add(new ShieldAbsorbed(hit.Id, absorbed, hit.Shield));
        if (damage == 0 && absorbed > 0)
            return;
        hit.Hp = Math.Max(0, hit.Hp - damage);
        hit.Eroded += damage * Erosion / 100;
        _events.Add(new Damaged(hit.Id, damage, hit.Hp, element));
        if (hit.IsAlive)
            return;
        _events.Add(new Died(hit.Id));
        hit.StatusList.Clear();
        hit.Shields.Clear();
        // Summons die with their summoner.
        foreach (Fighter summon in Fighters.Where(f => f.Summoner == hit.Id && f.IsAlive).ToList())
        {
            summon.Hp = 0;
            _events.Add(new Died(summon.Id));
        }
        // A team with nobody left has lost (summons never outlive their summoner).
        for (int team = 0; team < 2; team++)
        {
            if (!Fighters.Any(f => f.Team == team && f.IsAlive))
            {
                End(1 - team);
                return;
            }
        }
    }

    private void NextTurn()
    {
        // The statuses and shields of the fighter whose turn ends count one turn less.
        Fighter ending = Current;
        foreach (Status st in ending.StatusList.Where(st => st.TurnsLeft <= 1).ToList())
            _events.Add(new StatusEnded(ending.Id, st.Stat));
        for (int i = 0; i < ending.StatusList.Count; i++)
            ending.StatusList[i] = ending.StatusList[i] with { TurnsLeft = ending.StatusList[i].TurnsLeft - 1 };
        ending.StatusList.RemoveAll(st => st.TurnsLeft <= 0);
        for (int i = 0; i < ending.Shields.Count; i++)
            ending.Shields[i] = (ending.Shields[i].Amount, ending.Shields[i].TurnsLeft - 1);
        ending.Shields.RemoveAll(sh => sh.TurnsLeft <= 0);
        do
        {
            _current++;
            if (_current == _order.Count)
            {
                _current = 0;
                Round++;
            }
        }
        while (!Current.IsAlive);
        if (Round > RoundLimit)
        {
            Round = RoundLimit;
            End(null);
            return;
        }
        Fighter next = Current;
        next.Ap = Math.Max(0, next.Spec.Ap + next.StatusList.Where(st => st.Stat == Stat.Ap).Sum(st => st.Value));
        next.Mp = Math.Max(0, next.Spec.Mp + next.StatusList.Where(st => st.Stat == Stat.Mp).Sum(st => st.Value));
        next.CastsThisTurn.Clear();
        foreach (string id in next.Cooldowns.Keys.ToList())
        {
            if (--next.Cooldowns[id] == 0)
                next.Cooldowns.Remove(id);
        }
        _events.Add(new TurnStarted(next.Id, Round));
        // Poisons strike at the start of the turn, in their element, against the resistances.
        foreach (Status poison in next.StatusList.Where(st => st.Stat == Stat.Poison).ToList())
        {
            if (IsOver || !next.IsAlive)
                break;
            long damage = (long)poison.Value * (100 - next.Resistance(poison.Element)) / 100;
            Hurt(next, (int)Math.Max(0, damage), poison.Element);
        }
        if (!IsOver && !next.IsAlive)
            NextTurn();
    }

    private void End(int? winningTeam)
    {
        IsOver = true;
        WinningTeam = winningTeam;
        _events.Add(new FightEnded(winningTeam));
    }
}
