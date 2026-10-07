namespace Rogue.Core;

public enum GameState
{
    Playing,
    Dead,
    Escaped,
}

/// <summary>
/// A whole run. Everything follows from the seed and the actions played: the same seed and the
/// same actions always give the same dungeon, fights and score, which lets a server replay a run.
/// </summary>
public sealed class Game
{
    public const int MaxDepth = 5;
    public const int PotionHeal = 12;
    public const int RegenerationPeriod = 8;
    public const int HitChance = 85;
    public const int PointsPerFloor = 100;
    public const int EscapeBonus = 500;
    public const int LevelHp = 5;

    private readonly Rng _rng;
    private readonly List<GameAction> _history = [];
    private List<Monster> _monsters;
    private Dictionary<Point, Item> _items;
    private bool[] _visible;
    private int _killPoints;

    public Game(ulong seed)
        : this(seed, new Rng(seed), depth: 1, level: null)
    {
    }

    /// <summary>Starts on a given floor (tests build small floors by hand).</summary>
    internal Game(ulong seed, Rng rng, int depth, Level? level)
    {
        Seed = seed;
        _rng = rng;
        Depth = depth;
        level ??= DungeonGenerator.Generate(_rng, depth);
        Map = level.Map;
        Stairs = level.Stairs;
        _monsters = level.Monsters;
        _items = level.Items;
        Player.Position = level.Start;
        _visible = UpdateView();
    }

    public ulong Seed { get; }

    public Map Map { get; private set; }

    public Player Player { get; } = new();

    public Point Stairs { get; private set; }

    public IReadOnlyList<Monster> Monsters => _monsters;

    public IReadOnlyDictionary<Point, Item> Items => _items;

    public int Depth { get; private set; }

    /// <summary>Turns played (every legal action takes one).</summary>
    public int Turn { get; private set; }

    public int Kills { get; private set; }

    public int Gold { get; private set; }

    public GameState State { get; private set; }

    public IReadOnlyList<GameAction> History => _history;

    /// <summary>Gold, plus the value of the monsters killed, plus each floor reached below the first, plus the escape bonus.</summary>
    public int Score =>
        Gold + _killPoints + PointsPerFloor * (Depth - 1) + (State == GameState.Escaped ? EscapeBonus : 0);

    public bool IsVisible(Point p) => Map.InBounds(p) && _visible[Map.Index(p)];

    public Monster? MonsterAt(Point p) => _monsters.Find(m => m.Position == p);

    public bool CanApply(GameAction action)
    {
        if (State != GameState.Playing)
            return false;
        return action switch
        {
            GameAction.Wait => true,
            GameAction.Descend => Player.Position == Stairs,
            GameAction.Drink => Player.Potions > 0,
            _ => Map.IsWalkable(Player.Position.Step(ActionCodec.DirectionOf(action)!.Value)),
        };
    }

    /// <summary>Plays one turn: the player's action, then the monsters'.</summary>
    /// <exception cref="InvalidOperationException">The action is not possible now (see <see cref="CanApply"/>).</exception>
    public IReadOnlyList<GameEvent> Apply(GameAction action)
    {
        if (!CanApply(action))
            throw new InvalidOperationException($"Action {action} is not possible now.");
        _history.Add(action);
        Turn++;
        var events = new List<GameEvent>();
        switch (action)
        {
            case GameAction.Wait:
                break;
            case GameAction.Drink:
                Player.Potions--;
                int healed = Math.Min(PotionHeal, Player.MaxHp - Player.Hp);
                Player.Hp += healed;
                events.Add(new PotionDrunk(healed));
                break;
            case GameAction.Descend:
                Descend(events);
                // Arriving on a floor (or escaping) ends the turn: the new monsters have not seen the player yet.
                _visible = UpdateView();
                return events;
            default:
                MoveOrAttack(ActionCodec.DirectionOf(action)!.Value, events);
                break;
        }
        MonstersAct(events);
        if (State == GameState.Playing && Turn % RegenerationPeriod == 0 && Player.Hp < Player.MaxHp)
            Player.Hp++;
        _visible = UpdateView();
        return events;
    }

    private void MoveOrAttack(Direction direction, List<GameEvent> events)
    {
        Point target = Player.Position.Step(direction);
        Monster? monster = MonsterAt(target);
        if (monster is not null)
        {
            int damage = Strike(Player.Attack, monster.Defense);
            if (damage == 0)
            {
                events.Add(new PlayerMissed(monster.Kind));
                return;
            }
            monster.Hp -= damage;
            events.Add(new PlayerAttacked(monster.Kind, damage));
            if (monster.Hp <= 0)
                Kill(monster, events);
            return;
        }
        Player.Position = target;
        if (_items.Remove(target, out Item item))
        {
            if (item.Kind == ItemKind.Gold)
            {
                Gold += item.Amount;
                events.Add(new GoldPicked(item.Amount));
            }
            else
            {
                Player.Potions++;
                events.Add(new PotionPicked());
            }
        }
    }

    private void Kill(Monster monster, List<GameEvent> events)
    {
        _monsters.Remove(monster);
        Kills++;
        _killPoints += monster.Value;
        events.Add(new MonsterKilled(monster.Kind, monster.Value));
        Player.Xp += monster.Value;
        while (Player.Xp >= Player.XpToNextLevel)
        {
            Player.Xp -= Player.XpToNextLevel;
            Player.Level++;
            Player.MaxHp += LevelHp;
            Player.Hp += LevelHp;
            Player.Attack++;
            if (Player.Level % 3 == 0)
                Player.Defense++;
            events.Add(new LevelGained(Player.Level));
        }
    }

    private void Descend(List<GameEvent> events)
    {
        if (Depth == MaxDepth)
        {
            State = GameState.Escaped;
            events.Add(new DungeonEscaped());
            return;
        }
        Depth++;
        Level level = DungeonGenerator.Generate(_rng, Depth);
        Map = level.Map;
        Stairs = level.Stairs;
        _monsters = level.Monsters;
        _items = level.Items;
        Player.Position = level.Start;
        events.Add(new FloorReached(Depth));
    }

    /// <summary>
    /// Each awake monster, in order, hits the player if it stands next to them, otherwise takes one
    /// step along a shortest path towards them (ties broken north, south, east, west).
    /// </summary>
    private void MonstersAct(List<GameEvent> events)
    {
        int[]? distances = null;
        foreach (Monster monster in _monsters)
        {
            if (!monster.Awake)
                continue;
            if (monster.Position.IsAdjacentTo(Player.Position))
            {
                int damage = Strike(monster.Attack, Player.Defense);
                if (damage == 0)
                {
                    events.Add(new MonsterMissed(monster.Kind));
                    continue;
                }
                Player.Hp -= damage;
                events.Add(new MonsterAttacked(monster.Kind, damage));
                if (Player.Hp <= 0)
                {
                    Player.Hp = 0;
                    State = GameState.Dead;
                    events.Add(new PlayerDied(monster.Kind));
                    return;
                }
                continue;
            }
            distances ??= Map.Distances(Player.Position);
            int here = distances[Map.Index(monster.Position)];
            foreach (Direction d in Directions.All)
            {
                Point next = monster.Position.Step(d);
                if (!Map.InBounds(next))
                    continue;
                int there = distances[Map.Index(next)];
                if (there >= 0 && there < here && MonsterAt(next) is null)
                {
                    monster.Position = next;
                    break;
                }
            }
        }
    }

    /// <summary>Damage of one blow: 0 for a miss, otherwise attack minus defence, give or take one, and at least 1.</summary>
    private int Strike(int attack, int defense)
    {
        if (!_rng.Chance(HitChance))
            return 0;
        return Math.Max(1, attack - defense + _rng.Next(-1, 1));
    }

    private bool[] UpdateView()
    {
        bool[] visible = FieldOfView.Compute(Map, Player.Position);
        foreach (Point p in Map.Points())
            if (visible[Map.Index(p)])
                Map.MarkExplored(p);
        foreach (Monster monster in _monsters)
            if (visible[Map.Index(monster.Position)])
                monster.Awake = true;
        return visible;
    }
}
