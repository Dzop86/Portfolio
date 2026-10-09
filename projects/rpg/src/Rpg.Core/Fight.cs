namespace Rpg.Core;

/// <summary>A fighter during a fight. Its characteristics come from a <see cref="FighterSpec"/>.</summary>
public sealed class Fighter
{
    internal readonly Dictionary<string, int> CastsThisTurn = new(StringComparer.Ordinal);

    internal Fighter(int id, FighterSpec spec, Cell cell, IReadOnlyList<Spell> spells)
    {
        Id = id;
        Spec = spec;
        Hp = spec.Hp;
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

    public int CastsLeft(Spell spell) => spell.PerTurn - CastsThisTurn.GetValueOrDefault(spell.Id);
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

    private readonly List<Fighter> _order;
    private readonly List<FightAction> _history = [];
    private readonly List<FightEvent> _events = [];
    private readonly Rng _rng;
    private int _current;

    /// <param name="hero">The player's character, in place of the scenario's first fighter of team A; null keeps it.</param>
    public Fight(GameData data, string scenarioId, ulong seed, Hero? hero = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        Scenario = data.Scenarios.TryGetValue(scenarioId, out Scenario? s) ? s : throw new KeyNotFoundException($"Unknown scenario '{scenarioId}'.");
        if (hero?.Problem(data) is string problem)
            throw new ArgumentException(problem, nameof(hero));
        Hero = hero;
        Board = data.Board(Scenario.Map);
        Seed = seed;
        _rng = new Rng(seed);
        int heroIndex = Scenario.Fighters.ToList().FindIndex(f => f.Team == 0);
        Fighters = [.. Scenario.Fighters.Select((f, i) =>
        {
            FighterSpec spec = hero is not null && i == heroIndex ? AsHero(f, hero, data.Class(hero.Class)) : f;
            return new Fighter(i, spec, Board.Starts[f.Team][f.Start], [.. spec.Spells.Select(id => data.Spells[id])]);
        })];
        // Highest initiative first; equal initiatives keep the scenario's order.
        _order = [.. Fighters.OrderByDescending(f => f.Spec.Initiative).ThenBy(f => f.Id)];
        Round = 1;
        _events.Add(new TurnStarted(Current.Id, Round));
    }

    /// <summary>The scenario's fighter, played by the hero: its name and look, its class's characteristics and spells.</summary>
    private static FighterSpec AsHero(FighterSpec f, Hero hero, HeroClass? c)
    {
        FighterSpec spec = f with { Name = new LocalizedText(hero.Name, hero.Name), Look = hero.Look };
        return c is null ? spec : spec with { Hp = c.Hp, Ap = c.Ap, Mp = c.Mp, Initiative = c.Initiative, Spells = c.Spells };
    }

    public Scenario Scenario { get; }
    public Hero? Hero { get; }
    public Board Board { get; }
    public ulong Seed { get; }

    /// <summary>All fighters, by <see cref="Fighter.Id"/>.</summary>
    public IReadOnlyList<Fighter> Fighters { get; }

    /// <summary>The fighters in the order they play.</summary>
    public IReadOnlyList<Fighter> TurnOrder => _order;
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

    private void Cast(Fighter me, Spell spell, Cell target)
    {
        me.Ap -= spell.ApCost;
        me.CastsThisTurn[spell.Id] = me.CastsThisTurn.GetValueOrDefault(spell.Id) + 1;
        _events.Add(new SpellCast(me.Id, spell.Id, target));
        // The roll is drawn even on an empty cell: the sequence of draws depends only on the actions.
        int damage = _rng.Next(spell.DamageMin, spell.DamageMax);
        if (At(target) is not Fighter hit)
            return;
        hit.Hp = Math.Max(0, hit.Hp - damage);
        _events.Add(new Damaged(hit.Id, damage, hit.Hp));
        if (hit.IsAlive)
            return;
        _events.Add(new Died(hit.Id));
        for (int team = 0; team < 2; team++)
        {
            if (!Fighters.Any(f => f.Team == team && f.IsAlive))
            {
                End(1 - team);
                return;
            }
        }
        // A fighter who dies during its own turn hands over at once.
        if (!me.IsAlive)
            NextTurn();
    }

    private void NextTurn()
    {
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
        next.Ap = next.Spec.Ap;
        next.Mp = next.Spec.Mp;
        next.CastsThisTurn.Clear();
        _events.Add(new TurnStarted(next.Id, Round));
    }

    private void End(int? winningTeam)
    {
        IsOver = true;
        WinningTeam = winningTeam;
        _events.Add(new FightEnded(winningTeam));
    }
}
