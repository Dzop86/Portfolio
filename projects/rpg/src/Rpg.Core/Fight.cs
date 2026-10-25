namespace Rpg.Core;

/// <summary>A fighter during a fight. Its characteristics come from a <see cref="FighterSpec"/>.</summary>
public sealed class Fighter
{
    internal readonly Dictionary<string, int> CastsThisTurn = new(StringComparer.Ordinal);

    internal Fighter(int id, FighterSpec spec, Cell cell, IReadOnlyList<Spell> spells, IReadOnlyList<PassiveSpec> passives, int? summoner = null, string? summonKind = null,
        IReadOnlyList<TalentSpec>? talents = null)
    {
        Passives = passives;
        Talents = talents ?? [];
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

    /// <summary>The fighter's passives (sprint 90), in the order of its description.</summary>
    public IReadOnlyList<PassiveSpec> Passives { get; }

    /// <summary>A hero's talents (sprint 137, D80), in the order of its choice; none for a monster.</summary>
    public IReadOnlyList<TalentSpec> Talents { get; }

    /// <summary>The mark of a hero's last hit of a base element (sprint 137), if any.</summary>
    public Mark? Mark { get; internal set; }

    /// <summary>A hero, or a hero's summon: its direct hits of a base element mark the enemies (monsters never do, §6.3).</summary>
    public bool LaysMarks { get; internal set; }

    /// <summary>The turns it started (sprint 141): 1 during its first.</summary>
    public int Turn { get; internal set; }

    /// <summary>Cells walked this turn, cells walked since its last spell, damage spells cast this turn (sprint 141, the living talents).</summary>
    internal int StepsThisTurn, CellsSinceSpell, DamageCastsThisTurn;

    /// <summary>Action points kept from the turn before (Reserve, sprint 141).</summary>
    internal int CarriedAp;

    /// <summary>
    /// Terrain (sprint 142): the free step of Side Step taken this turn; where the turn started and the last damage spell
    /// cast with its target; the echo it left, to strike at the start of its next turn.
    /// </summary>
    internal bool SideStepped;

    /// <summary>Support (sprint 143): the supports given this turn, Relief used in this fight, action points the next turn loses.</summary>
    internal int SupportsThisTurn, NextTurnPenalty, ReactionsThisTurn;
    internal bool Relieved;
    internal Cell TurnStart;
    internal (string Spell, int Target)? LastHit;
    internal (Cell From, string Spell, int Target)? Echo;

    /// <summary>What its living talents would read of a hit with <paramref name="spell"/> now (sprint 141).</summary>
    public HitContext Context(Spell? spell, bool walking = false) =>
        new(walking || StepsThisTurn > 0, CellsSinceSpell, DamageCastsThisTurn, spell?.ApCost ?? 0);

    /// <summary>The low-health passives that have fired: once per fight each.</summary>
    internal readonly HashSet<string> LowHpFired = new(StringComparer.Ordinal);

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
    /// <summary>The damage bonus in percent of the statuses on the fighter: a neutral one for every element, an element's (an oil, sprint 111) for its own.</summary>
    public int DamageBonusFor(Element element) =>
        Math.Max(-100, StatusList.Where(s => s.Stat == Stat.Damage && (s.Element == Element.Neutral || s.Element == element)).Sum(s => s.Value));

    public int CastsLeft(Spell spell) => spell.PerTurn - CastsThisTurn.GetValueOrDefault(spell.Id);

    /// <summary>
    /// How far a spell reaches now (sprint 140): its range changed by the fighter's range statuses (Blinded) for a spell
    /// reaching farther than 1, never under 1 nor under its minimum.
    /// </summary>
    public int MaxRangeOf(Spell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        int change = StatusList.Where(s => s.Stat == Stat.Range).Sum(s => s.Value);
        return change == 0 || spell.MaxRange <= 1 ? spell.MaxRange : Math.Max(Math.Max(1, spell.MinRange), spell.MaxRange + change);
    }

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

    private readonly List<Glyph> _glyphs = [];
    private int _glyphsLaid;

    /// <summary>The passives set off during the action being played, with their bearer and, for a hit, the attacker (sprint 90).</summary>
    private readonly Queue<(Fighter Bearer, PassiveSpec Passive, Fighter? Attacker)> _pending = new();

    /// <param name="hero">The player's character, in place of the scenario's first fighter of team A; null keeps it.</param>
    /// <param name="rival">Another hero, in place of the scenario's first fighter of team B (class duels).</param>
    /// <param name="heroHp">The hero's hit points to start with, at most its maximum (a dungeon's rooms, sprint 86); null: all of them.</param>
    /// <param name="allies">
    /// Since sprint 120 (D74), the hero's party: up to three more heroes in team A, on the board's starts of team A the
    /// scenario leaves free, after the scenario's fighters (whose ids do not change).
    /// </param>
    /// <param name="alliesHp">Each ally's hit points to start with, in the same order; null for all of them.</param>
    public Fight(GameData data, string scenarioId, ulong seed, Hero? hero = null, Hero? rival = null, int? heroHp = null,
        IReadOnlyList<Hero>? allies = null, IReadOnlyList<int?>? alliesHp = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        Scenario = data.FindScenario(scenarioId) is Scenario s ? s : throw new KeyNotFoundException($"Unknown scenario '{scenarioId}'.");
        if (hero?.Problem(data) is string problem)
            throw new ArgumentException(problem, nameof(hero));
        if (rival?.Problem(data) is string rivalProblem)
            throw new ArgumentException(rivalProblem, nameof(rival));
        Allies = [.. allies ?? []];
        if (Allies.Count > 0 && hero is null)
            throw new ArgumentException("Allies fight with a hero.", nameof(allies));
        if (Allies.FirstOrDefault(a => a.Problem(data) is not null) is Hero wrong)
            throw new ArgumentException(wrong.Problem(data), nameof(allies));
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
            return new Fighter(i, spec, Board.Starts[f.Team][f.Start], SpellsOf(spec, data), PassivesOf(spec.Passives, data), talents: TalentsOf(spec, data))
            {
                LaysMarks = (hero is not null && i == heroIndex) || (rival is not null && i == rivalIndex),
            };
        })];
        _heroIndex = heroIndex;
        // The allies (sprint 120): the starts of team A no fighter of the scenario stands on, in order.
        int[] freeStarts = [.. Enumerable.Range(0, Board.Starts[0].Count).Except(Scenario.Fighters.Where(f => f.Team == 0).Select(f => f.Start))];
        if (Allies.Count > freeStarts.Length)
            throw new ArgumentException($"The board has {freeStarts.Length} free starts for allies.", nameof(allies));
        for (int k = 0; k < Allies.Count; k++)
        {
            FighterSpec spec = AsHero(Scenario.Fighters[heroIndex] with { Start = freeStarts[k], Loot = null }, Allies[k], data);
            var ally = new Fighter(_fighters.Count, spec, Board.Starts[0][freeStarts[k]], SpellsOf(spec, data), PassivesOf(spec.Passives, data), talents: TalentsOf(spec, data)) { LaysMarks = true };
            if (alliesHp?.ElementAtOrDefault(k) is int hp)
                ally.Hp = Math.Clamp(hp, 1, ally.MaxHp);
            _fighters.Add(ally);
            _allyIds.Add(ally.Id);
        }
        AlliesHp = [.. Enumerable.Range(0, Allies.Count).Select(k => alliesHp?.ElementAtOrDefault(k))];
        // As asked, for the record to replay it; clamped on the board (equipment changed since may have lowered the maximum).
        if (heroHp is int start && heroIndex >= 0)
        {
            _fighters[heroIndex].Hp = Math.Clamp(start, 1, _fighters[heroIndex].MaxHp);
            StartHeroHp = start;
        }
        _data = data;
        // Highest initiative first; equal initiatives keep the scenario's order.
        // By initiative; on a tie, the heroes' team first (sprint 95, D71), then the scenario's order.
        _order = [.. _fighters.OrderByDescending(f => f.Spec.Initiative).ThenBy(f => f.Team).ThenBy(f => f.Id)];
        Round = 1;
        // The first turn's points from the talents (sprint 141), as at the start of every turn.
        Current.Turn = 1;
        Current.TurnStart = Current.Cell;
        (int firstAp, int firstMp) = Talents.TurnPoints(Current.Talents, 1);
        (Current.Ap, Current.Mp) = (Math.Max(0, Current.Ap + firstAp), Math.Max(0, Current.Mp + firstMp));
        _events.Add(new TurnStarted(Current.Id, Round));
        Rally(Current);
        foreach (PassiveSpec p in Current.Passives.Where(p => p.Trigger == Trigger.TurnStart))
            _pending.Enqueue((Current, p, null));
        Resolve();
    }

    /// <summary>
    /// The scenario's fighter, played by the hero: its name and look, its class's characteristics and
    /// the class's spells its level has unlocked.
    /// </summary>
    private static FighterSpec AsHero(FighterSpec f, Hero hero, GameData data)
    {
        FighterSpec spec = f with { Name = new LocalizedText(hero.Name, hero.Name), Look = hero.Look };
        if (HeroTotals(hero, data) is not HeroNumbers n)
            return spec;
        // Nomad (sprint 142): the maximum, Vitality included, that many percent more or fewer; and the trunk's (sprint 146).
        int hpPercent = (hero.Talents ?? []).Where(data.Talents.ContainsKey).SelectMany(id => data.Talents[id].Rules).OfType<HpRule>().Sum(r => r.Percent)
            + Talents.TrunkHp(hero.Level);
        int hp = hpPercent == 0 ? n.Hp : (n.Hp + n.Stats.Vitality) * (100 + hpPercent) / 100 - n.Stats.Vitality;
        // The trunk's resistance (sprint 146, D82): that many points more to each base element, a compound its parents' average.
        int resist = Talents.TrunkResistance(hero.Level);
        IReadOnlyDictionary<Element, int>? resistances = resist == 0 ? f.Resistances
            : new[] { Element.Earth, Element.Fire, Element.Water, Element.Air }.ToDictionary(e => e, e => f.Resistance(e) + resist);
        return spec with
        {
            Hp = hp,
            Resistances = resistances,
            TreeDamage = Talents.TrunkDamage(hero.Level),
            Ap = n.Ap,
            Mp = n.Mp,
            Initiative = n.Initiative,
            Spells = n.Spells,
            Stats = n.Stats,
            SpellRanks = hero.Ranks,
            Level = hero.Level,
            Talents = hero.Talents,
        };
    }

    /// <summary>
    /// What a hero of a class fights with: its class's numbers at its level, what it wears added to its
    /// own points and to its action and movement points, the spells of its deck (Hero.DeckOf). Null without
    /// a class. The characteristics sheet shows the same numbers.
    /// </summary>
    public static HeroNumbers? HeroTotals(Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        if (data.Class(hero.Class) is not HeroClass c)
            return null;
        // The initiative comes from what is worn alone (sprint 95, D71), never from the characteristics.
        (Characteristics worn, int ap, int mp, int initiative) = Equipment.Total(hero.Worn, data);
        return new HeroNumbers(c.Hp + c.HpPerLevel * (hero.Level - 1), c.Ap + ap, c.Mp + mp, c.Initiative + initiative,
            Equipment.Add(hero.Stats ?? Characteristics.None, worn), worn, hero.DeckOf(data));
    }

    /// <summary>A fighter's spells at their ranks (rank 1 unless the description says otherwise), as its talents compile them (sprint 137).</summary>
    private static List<Spell> SpellsOf(FighterSpec spec, GameData data)
    {
        List<TalentSpec> talents = TalentsOf(spec, data);
        return [.. spec.Spells.Select(id => Talents.Compile(data.Spells[id].AtRank(spec.SpellRanks?.GetValueOrDefault(data.Spells[id].RankKey, 1) ?? 1), talents))];
    }

    private static List<TalentSpec> TalentsOf(FighterSpec spec, GameData data) => [.. (spec.Talents ?? []).Select(id => data.Talents[id])];

    private static List<PassiveSpec> PassivesOf(IReadOnlyList<string>? ids, GameData data) => [.. (ids ?? []).Select(id => data.Passives[id])];

    public Scenario Scenario { get; }

    private readonly int _heroIndex;

    /// <summary>The hero's hit points asked for at the start (sprint 86); null: all of them.</summary>
    public int? StartHeroHp { get; }

    /// <summary>The fighter the hero plays (the scenario's first of team A).</summary>
    public Fighter? HeroFighter => _heroIndex >= 0 ? _fighters[_heroIndex] : null;
    public Hero? Hero { get; }

    /// <summary>The hero playing team B's first fighter, if any.</summary>
    public Hero? Rival { get; }

    /// <summary>The hero's party (sprint 120): the other heroes of team A, in order.</summary>
    public IReadOnlyList<Hero> Allies { get; }

    /// <summary>Each ally's hit points asked for at the start, null for all of them.</summary>
    public IReadOnlyList<int?> AlliesHp { get; }

    /// <summary>The fighters the heroes play: the hero first, then each ally (sprint 120).</summary>
    public IReadOnlyList<Fighter> HeroFighters => HeroFighter is Fighter hero ? [hero, .. _allyIds.Select(i => _fighters[i])] : [];

    // The allies' ids, kept at the start: summons come after them in the list.
    private readonly List<int> _allyIds = [];
    public Board Board { get; }
    public ulong Seed { get; }

    /// <summary>All fighters, by <see cref="Fighter.Id"/>; summons are added at the end as they appear.</summary>
    public IReadOnlyList<Fighter> Fighters => _fighters;

    private readonly List<Fighter> _fighters;
    private readonly GameData _data;

    /// <summary>The data the fight was set up from.</summary>
    public GameData Data => _data;

    /// <summary>The fighters in the order they play.</summary>
    public IReadOnlyList<Fighter> TurnOrder => _order;

    /// <summary>
    /// The order of play of this round (sprint 78, D64): every fighter still standing, the one playing
    /// among them; the same list comes back each round, without those who fell.
    /// </summary>
    public IReadOnlyList<Fighter> RoundOrder => IsOver ? [] : [.. _order.Where(f => f.IsAlive)];

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
        // A support (sprint 143): its heal, the rest is told by the spell's card.
        if (SupportTarget(me, spell, target) is Fighter ally)
        {
            int heal = Math.Min(SupportFrom(me, spell, ally).Heal, ally.MaxHp - ally.Hp);
            return [new Forecast(ally, 0, 0, heal, heal, spell.Element)];
        }
        var forecasts = new List<Forecast>();
        IReadOnlyList<Fighter> area = InArea(spell, me.Cell, target);
        HealEffect[] heals = [.. spell.AllEffects.OfType<HealEffect>()];
        foreach (Fighter f in area.Concat(heals.Any(h => h.Affects == Affects.Caster) && !area.Contains(me) ? [me] : []))
        {
            bool hit = spell.DamageMax > 0 && area.Contains(f);
            int healMin = 0, healMax = 0;
            foreach (HealEffect h in heals.Where(h => h.Affects == Affects.Caster ? f == me : area.Contains(f) && Touches(h.Affects, me, f)))
            {
                healMin += h.Min;
                healMax += h.Max;
            }
            int missing = f.MaxHp - f.Hp;
            if (hit || healMax > 0)
            {
                int crit = Spell.CritBonus(spell.DamageMax);
                bool canCrit = hit && spell.Crit > 0;
                HitContext context = ContextOf(me, spell, me.Cell, f);
                forecasts.Add(new Forecast(f, hit ? Damage(spell.DamageMin, me, f, spell.Element, true, context) : 0, hit ? Damage(spell.DamageMax, me, f, spell.Element, true, context) : 0,
                    Math.Min(healMin, missing), Math.Min(healMax, missing), spell.Element,
                    canCrit ? Damage(spell.DamageMin + crit, me, f, spell.Element, true, context) : 0, canCrit ? Damage(spell.DamageMax + crit, me, f, spell.Element, true, context) : 0,
                    hit && f.Cell == target ? ReactionOn(me, f, spell.Element)?.Id : null));
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

    /// <summary>The glyphs on the board (sprint 130), oldest first.</summary>
    public IReadOnlyList<Glyph> Glyphs => _glyphs;

    /// <summary>The glyphs on a cell, oldest first.</summary>
    public IReadOnlyList<Glyph> GlyphsAt(Cell cell) => [.. _glyphs.Where(g => g.Cells.Contains(cell))];

    /// <summary>Whether a glyph would strike <paramref name="f"/>: it touches the fighter's side, as seen from its caster.</summary>
    public bool Strikes(Glyph glyph, Fighter f)
    {
        ArgumentNullException.ThrowIfNull(glyph);
        ArgumentNullException.ThrowIfNull(f);
        return Touches(glyph.Effect.Affects, _fighters[glyph.Caster], f);
    }

    /// <summary>
    /// The cells the current fighter walks to reach <paramref name="to"/>, without the start, or null when it
    /// cannot. A shortest path; with glyphs that would strike it on the board (sprint 130), the path within its
    /// movement points through the fewest of their cells, then the shortest: the board shows it, the move takes it.
    /// </summary>
    public IReadOnlyList<Cell>? PathTo(Cell to)
    {
        Fighter me = Current;
        IReadOnlyList<Cell>? shortest = Pathfinding.FindPath(Board, me.Cell, to, IsFree);
        if (shortest is null || !_glyphs.Any(g => Strikes(g, me)))
            return shortest;
        return Pathfinding.FindPath(Board, me.Cell, to, IsFree, me.Mp, c => _glyphs.Count(g => g.Cells.Contains(c) && Strikes(g, me))) ?? shortest;
    }

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
        Fighter? ally = SupportTarget(caster, spell, target);
        if (caster.Ap < ApCostOn(caster, spell, ally))
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
        if (distance < spell.MinRange || distance > caster.MaxRangeOf(spell) + ReachOn(caster, spell, target))
            return ActionError.OutOfRange;
        if (ally is not null && caster.SupportsThisTurn >= SupportOf(caster)!.PerTurn)
            return ActionError.SupportSpent;
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
        // A leap lands on a free cell; a swap trades places with a fighter of the side it affects (sprint 90).
        if (spell.AllEffects.Any(e => e is LeapEffect) && !IsFree(target))
            return ActionError.TargetNotFree;
        foreach (SwapEffect swap in spell.AllEffects.OfType<SwapEffect>())
        {
            if (At(target) is not Fighter other || other == caster || !Touches(swap.Affects, caster, other))
                return ActionError.NoValidTarget;
        }
        return ActionError.None;
    }

    /// <summary>The support rules a fighter's talents add up to (sprint 143), or null without Helping Hand.</summary>
    public static SupportRule? SupportOf(Fighter f)
    {
        ArgumentNullException.ThrowIfNull(f);
        SupportRule[] rules = [.. f.Talents.SelectMany(t => t.Rules).OfType<SupportRule>()];
        return rules.Length == 0 ? null : new SupportRule(rules.Max(r => r.PerTurn), (int)Math.Round(rules.Aggregate(100.0, (p, r) => p * r.Percent / 100)));
    }

    /// <summary>
    /// The ally a cast would help rather than hurt (sprint 143): an element spell that hurts, cast by a fighter with Helping
    /// Hand on the cell of a living fighter of its side who is not itself; null otherwise.
    /// </summary>
    public Fighter? SupportTarget(Fighter caster, Spell spell, Cell target)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(spell);
        if (spell.DamageMax <= 0 || spell.Element == Element.Neutral || caster.Talents.Count == 0 || SupportOf(caster) is null)
            return null;
        return At(target) is Fighter f && f.Team == caster.Team && f != caster ? f : null;
    }

    /// <summary>A cast's cost on its target (sprint 143): Harmony makes a support dearer, Relief free once.</summary>
    public static int ApCostOn(Fighter caster, Spell spell, Fighter? ally)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(spell);
        if (ally is null)
            return spell.ApCost;
        if (Relief(caster, ally) is not null)
            return 0;
        return spell.ApCost + caster.Talents.SelectMany(t => t.Rules).OfType<HarmonyRule>().Sum(r => r.ApCost);
    }

    /// <summary>Relief (sprint 143) for a support on <paramref name="ally"/>, if it applies now.</summary>
    private static RelieveRule? Relief(Fighter caster, Fighter ally) =>
        caster.Relieved ? null : caster.Talents.SelectMany(t => t.Rules).OfType<RelieveRule>().FirstOrDefault(r => ally.Hp * 100 < r.Below * ally.MaxHp);

    /// <summary>Cells more (or fewer) a spell reaches on what stands on <paramref name="target"/> (Long Hand, sprint 143).</summary>
    private int ReachOn(Fighter caster, Spell spell, Cell target)
    {
        ReachRule[] reach = [.. caster.Talents.SelectMany(t => t.Rules).OfType<ReachRule>()];
        if (reach.Length == 0 || At(target) is not Fighter f || f == caster)
            return 0;
        return f.Team == caster.Team ? reach.Sum(r => r.Ally) : (spell.MaxRange > 1 ? reach.Sum(r => r.Enemy) : 0);
    }

    /// <summary>What a support from <paramref name="caster"/> with <paramref name="spell"/> would give <paramref name="ally"/> (sprint 143).</summary>
    public static SupportEffect SupportFrom(Fighter caster, Spell spell, Fighter ally)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(spell);
        ArgumentNullException.ThrowIfNull(ally);
        int percent = SupportOf(caster)?.Percent ?? 100;
        if (Relief(caster, ally) is not null)
            percent *= 2;
        double d = spell.AverageDamage + Characteristics.Bonus(Strength(caster, spell.Element));
        return SupportEffect.Of(spell.Element, d, spell.ApCost, ally.MaxHp, percent);
    }

    /// <summary>
    /// A support (sprint 143): the heal, the shield, the statuses of a turn (resistance to every element, movement points,
    /// range), Harmony's resistance to the spell's element, Shared Momentum; no mark, no reaction, no damage.
    /// </summary>
    private void Support(Fighter me, Spell spell, Fighter ally)
    {
        SupportEffect s = SupportFrom(me, spell, ally);
        if (Relief(me, ally) is RelieveRule relief)
        {
            me.Relieved = true;
            me.NextTurnPenalty += relief.Penalty;
        }
        me.SupportsThisTurn++;
        _events.Add(new Supported(me.Id, ally.Id));
        if (s.Heal > 0)
        {
            int healed = Math.Min(s.Heal, ally.MaxHp - ally.Hp);
            ally.Hp += healed;
            _events.Add(new Healed(ally.Id, healed, ally.Hp));
        }
        if (s.Shield > 0)
        {
            ally.Shields.Add((s.Shield, 1));
            _events.Add(new Shielded(ally.Id, s.Shield, 1));
        }
        if (s.Resistance > 0)
            SetStatus(me, ally, new StatusEffect(Affects.Allies, Stat.Resistance, s.Resistance, 1));
        if (s.Mp > 0)
            SetStatus(me, ally, new StatusEffect(Affects.Allies, Stat.Mp, s.Mp, 1));
        if (s.Range > 0)
            SetStatus(me, ally, new StatusEffect(Affects.Allies, Stat.Range, s.Range, 1));
        foreach (HarmonyRule h in me.Talents.SelectMany(t => t.Rules).OfType<HarmonyRule>())
            SetStatus(me, ally, new StatusEffect(Affects.Allies, Stat.Resistance, h.Percent, 1, spell.Element));
        foreach (ShareRule share in me.Talents.SelectMany(t => t.Rules).OfType<ShareRule>())
        {
            SetStatus(me, ally, new StatusEffect(Affects.Allies, Stat.Damage, share.Ally, 1));
            SetStatus(me, me, new StatusEffect(Affects.Caster, Stat.Damage, share.Self, 1));
        }
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
                Walk(me, PathTo(move.To)!);
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
    /// Walks a path cell by cell, a movement point each. A glyph on a cell entered strikes (sprint 130): the
    /// walk so far is told, then the glyph, then the passives it set off; the walk goes on unless the walker
    /// fell, the fight ended, a fighter now stands in the way (the points left are kept) or a glyph took its
    /// last movement points.
    /// </summary>
    private void Walk(Fighter me, IReadOnlyList<Cell> path)
    {
        var walked = new List<Cell>();
        foreach (Cell next in path)
        {
            if (!IsFree(next) || me.Mp <= 0)
                break;
            me.Mp--;
            me.StepsThisTurn++;
            me.CellsSinceSpell++;
            me.Cell = next;
            walked.Add(next);
            if (!GlyphsAt(next).Any(g => Strikes(g, me) && !g.Struck.Contains(me.Id)))
                continue;
            _events.Add(new Moved(me.Id, walked));
            walked = [];
            StrikeAt(me);
            Resolve();
            if (IsOver || !me.IsAlive)
                break;
        }
        if (walked.Count > 0)
            _events.Add(new Moved(me.Id, walked));
        if (!IsOver && !me.IsAlive)
            NextTurn();
    }

    /// <summary>
    /// The glyphs on the fighter's cell that would strike it strike it, oldest first, once each per turn of
    /// play (sprint 130): damage from their caster, in their element, through the shields; then their status.
    /// A glyph's damage is not a spell's: it sets off no passive of a hit, as a poison.
    /// </summary>
    private void StrikeAt(Fighter f)
    {
        foreach (Glyph g in GlyphsAt(f.Cell))
        {
            if (IsOver || !f.IsAlive || !Strikes(g, f) || !g.Struck.Add(f.Id))
                continue;
            _events.Add(new GlyphTriggered(g.Id, f.Id));
            Fighter caster = _fighters[g.Caster];
            if (g.Effect.DamageMax > 0)
                Hurt(f, Damage(_rng.Next(g.Effect.DamageMin, g.Effect.DamageMax), caster, f, g.Effect.Element) * g.PercentAt(f.Cell) / 100, g.Effect.Element);
            if (!IsOver && f.IsAlive && g.Effect.AsStatus is StatusEffect status)
                SetStatus(caster, f, status);
            // A Seal's mark (sprint 142): it never sets a reaction off.
            if (!IsOver && f.IsAlive && g.Effect.Marks)
                Imprint(caster, f, g.Effect.Element, aimed: false);
        }
    }

    /// <summary>Lays a glyph on the floor cells of a spell's area (sprint 130), in place of the caster's last one of that spell.</summary>
    private void Lay(Fighter me, GlyphEffect effect, IReadOnlyList<Cell> zone, string spell)
    {
        foreach (Glyph old in _glyphs.Where(g => g.Caster == me.Id && g.Spell == spell).ToList())
            EndGlyph(old);
        Cell[] cells = [.. zone.Where(Board.IsFloor).Distinct()];
        if (cells.Length == 0)
            return;
        var glyph = new Glyph(_glyphsLaid++, me.Id, spell, cells, effect, zone[0]);
        _glyphs.Add(glyph);
        _events.Add(new GlyphPlaced(glyph.Id, me.Id, spell, cells, effect.Turns, effect.Element));
    }

    private void EndGlyph(Glyph glyph)
    {
        _glyphs.Remove(glyph);
        _events.Add(new GlyphEnded(glyph.Id));
    }

    /// <summary>
    /// The damage a hit of <paramref name="roll"/> does: 2 more for every 10 points the caster has in the
    /// element's characteristic (D61), then more with the caster's damage bonus in percent, less with the
    /// target's resistance to the element; a hit without points nor bonus is the roll itself. A fighter
    /// weakened (<see cref="FighterSpec.Power"/> below 100, a dungeon's first room) hits that much less.
    /// A spell's <paramref name="direct"/> hit (sprint 137) is also changed by the caster's living talents
    /// (<see cref="Talents.Percent"/>), apart from its statuses. A hero's trunk (sprint 146, D82) adds its own percent to every
    /// hit, neutral ones included, outside the talents' ceiling.
    /// </summary>
    /// <param name="guard">The target's resistance from its talents (Backed, sprint 142), within the ceiling of 90.</param>
    public static int Damage(int roll, Fighter caster, Fighter target, Element element, bool direct = false, HitContext? context = null, int guard = 0)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(target);
        long raw = roll + Characteristics.Bonus(Strength(caster, element));
        long talents = 100 + (direct ? Talents.Percent(caster.Talents, caster.Cell, target.Cell, element, context) : 0);
        int resistance = element == Element.Neutral ? 0 : Math.Min(90, target.Resistance(element) + guard);
        long scaled = raw * caster.Spec.Power * (100 + caster.DamageBonusFor(element)) * (100 - resistance) * talents * (100 + caster.Spec.TreeDamage);
        return (int)Math.Max(0, scaled / 10_000_000_000);
    }

    /// <summary>Next to an obstacle (sprint 142): one of the four cells around is one.</summary>
    public bool IsBacked(Cell cell) => cell.Neighbours().Any(n => Board.Contains(n) && Board[n] == Terrain.Obstacle);

    /// <summary>
    /// What <paramref name="caster"/>'s living talents read of a hit with <paramref name="spell"/> from <paramref name="from"/>
    /// on <paramref name="target"/> (sprints 141 and 142): its turn so far, a walk to get there, an obstacle beside it, the
    /// target in one of its glyphs.
    /// </summary>
    public HitContext ContextOf(Fighter caster, Spell? spell, Cell from, Fighter target)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(target);
        return caster.Context(spell, walking: from != caster.Cell) with
        {
            Backed = caster.Talents.Count > 0 && IsBacked(from),
            InMyGlyph = caster.Talents.Count > 0 && _glyphs.Any(g => g.Caster == caster.Id && g.Cells.Contains(target.Cell)),
        };
    }

    /// <summary>The resistance a fighter's talents give it where it stands (Backed, sprint 142).</summary>
    public int GuardOf(Fighter f) => f.Talents.Count == 0 ? 0 : Talents.Guard((f ?? throw new ArgumentNullException(nameof(f))).Talents, IsBacked(f.Cell));

    /// <summary>The characteristic that strengthens <paramref name="caster"/>'s hits of this element, its Conversion given (sprint 144).</summary>
    private static int Strength(Fighter caster, Element element) =>
        caster.Spec.Characteristics.For(element, Talents.Converts(caster.Talents, element));

    /// <summary>
    /// The damage the AI expects a hit of <paramref name="average"/> to do (sprint 141): the one estimate it uses for spells
    /// and glyphs alike, the talents' <paramref name="talents"/> percent given. It keeps the AI's scale of before D61 (a
    /// characteristic point as one percent), on which every balance measure was made; <see cref="Damage"/> is the rule.
    /// </summary>
    public static double Expected(double average, Fighter caster, Fighter target, Element element, int talents = 0)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(target);
        return average * (100 + Strength(caster, element) + caster.DamageBonusFor(element)) * (100 - target.Resistance(element)) / 10_000 * (100 + talents) / 100
            * (100 + caster.Spec.TreeDamage) / 100;
    }

    /// <summary>
    /// The reaction <paramref name="caster"/>'s direct hit in <paramref name="element"/> would set off on <paramref name="f"/>
    /// (sprint 137): an enemy marked by another base element, a caster who lays marks and has the reaction's talent.
    /// </summary>
    public ReactionSpec? ReactionOn(Fighter caster, Fighter f, Element element)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(f);
        if (!caster.LaysMarks || f.Team == caster.Team || f.Mark is not Mark m)
            return null;
        TalentRule[] rules = [.. caster.Talents.SelectMany(t => t.Rules)];
        // Virtuoso (sprint 144): its own marks only; Catalysis: one reaction a turn.
        if (rules.OfType<VirtuosoRule>().Any() && m.Source != caster.Id || rules.OfType<CatalysisRule>().Any() && caster.ReactionsThisTurn > 0)
            return null;
        Fighter source = _fighters[m.Source];
        return Talents.ReactionOf(caster.Talents, m.Element, element, _data)
            ?? (source != caster && source.Talents.SelectMany(t => t.Rules).OfType<SharedMarkRule>().Any() ? Talents.ReactionOf(source.Talents, m.Element, element, _data) : null);
    }

    /// <summary>
    /// A Remanence (sprint 142, the idea of Charles, docs/talents-arbre.md §4.6): a diamond around the cell aimed at, of the
    /// spell's cost in radius up to the rule's, each action point beyond 10 % more; a glyph of the spell's element for the
    /// rule's turns, of its share of the spell's damage, falling away from the centre; it replaces the caster's last one.
    /// </summary>
    private void Remain(Fighter me, Spell spell, Cell target, RemanenceRule rule)
    {
        int radius = Math.Min(spell.ApCost, rule.MaxRadius);
        double share = rule.Percent / 100.0 * (1 + 0.1 * Math.Max(0, spell.ApCost - rule.MaxRadius));
        var effect = new GlyphEffect(Affects.Enemies, rule.Turns, Math.Max(1, (int)Math.Round(spell.DamageMin * share)), Math.Max(1, (int)Math.Round(spell.DamageMax * share)),
            spell.Element, Decays: true);
        Lay(me, effect, [.. new Area(AreaShape.Circle, radius).Cells(me.Cell, target)], RemanenceKey);
    }

    /// <summary>The spell id a Remanence is laid under: one per caster.</summary>
    public const string RemanenceKey = "remanence";

    /// <summary>
    /// A jump (Lightning's Arc, sprint 138, <c>docs/elements.md</c> §2; Electrified, sprint 140): <paramref name="percent"/>
    /// of the hit jumps once to the nearest fighter of <paramref name="from"/>'s side within <see cref="Elements.ArcCells"/>
    /// cells of it, outside the spell's area (the lowest id on a tie). It is no spell's hit: no passive answers it, no mark
    /// is left.
    /// </summary>
    private void Arc(Fighter me, Fighter from, int roll, IReadOnlyList<Fighter> area, int percent, Element element, HitContext context)
    {
        Fighter? to = Fighters.Where(f => f.IsAlive && f != from && f.Team == from.Team && !area.Contains(f) && f.Cell.DistanceTo(from.Cell) <= Elements.ArcCells)
            .OrderBy(f => f.Cell.DistanceTo(from.Cell)).ThenBy(f => f.Id).FirstOrDefault();
        if (to is null)
            return;
        _events.Add(new Arced(from.Id, to.Id));
        Hurt(to, Damage(roll, me, to, element, true, context) * percent / 100, element);
    }

    /// <summary>
    /// What a hero's direct hit of a base element leaves on an enemy (sprint 137, D80 §6.1): on the fighter aimed at, if
    /// its mark is of another element and the caster has the reaction, the reaction, which uses the mark up; otherwise
    /// the hit's own mark in place of the one before, until the end of the caster's next turn.
    /// </summary>
    /// <param name="aimed">Whether this hit may set a reaction off: the fighter aimed at, or with Wave one of the first marked in the area.</param>
    /// <param name="dealt">The hit's damage, for Catalysis (sprint 144).</param>
    private void Imprint(Fighter me, Fighter f, Element element, bool aimed, int dealt = 0)
    {
        // A pair's hit leaves its first parent's mark with Prism (sprint 138), and sets nothing off.
        if (Elements.Parents(element) is [Element first, _] && Talents.HasPrism(me.Talents))
        {
            element = first;
            aimed = false;
        }
        if (!me.LaysMarks || !Talents.IsBase(element) || f.Team == me.Team || IsOver || !f.IsAlive)
            return;
        if (aimed && ReactionOn(me, f, element) is ReactionSpec reaction)
        {
            f.Mark = null;
            _events.Add(new MarkEnded(f.Id));
            _events.Add(new ReactionTriggered(f.Id, reaction.Id, me.Id));
            me.ReactionsThisTurn++;
            TalentRule[] rules = [.. me.Talents.SelectMany(t => t.Rules)];
            int longer = rules.OfType<LingerRule>().Sum(r => r.ExtraTurns);
            foreach (StatusEffect s in reaction.Statuses)
            {
                StatusEffect status = s with { Turns = s.Turns + longer };
                // A chief loses at most 2 action and 2 movement points to the reactions (§6.1).
                int value = status.Value;
                if (f.Spec.Chief && status.Stat is Stat.Ap or Stat.Mp && value < 0)
                    value = Math.Max(value, -2 - f.StatusList.Where(st => st.Stat == status.Stat && st.Reaction is not null && st.Value < 0 && st.Source != me.Id).Sum(st => st.Value));
                if (value != 0)
                    SetStatus(me, f, status with { Value = value }, reaction.Id);
            }
            int catalysis = rules.OfType<CatalysisRule>().Sum(r => r.Percent);
            if (catalysis > 0 && dealt > 0 && f.IsAlive && !IsOver)
                Hurt(f, dealt * catalysis / 100);
            return;
        }
        f.Mark = new Mark(element, me.Id, 2 + me.Talents.SelectMany(t => t.Rules).OfType<MarkRule>().Sum(r => r.ExtraTurns));
        _events.Add(new Marked(f.Id, element, me.Id));
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
        // A spell on an ally with Helping Hand (sprint 143): it helps, and nothing else; the dice are drawn all the same.
        if (SupportTarget(me, spell, target) is Fighter ally)
        {
            me.Ap -= ApCostOn(me, spell, ally);
            me.CastsThisTurn[spell.Id] = me.CastsThisTurn.GetValueOrDefault(spell.Id) + 1;
            if (spell.Cooldown > 0)
                me.Cooldowns[spell.Id] = spell.Cooldown + 1;
            bool crit = _rng.Next(1, 100) <= spell.Crit;
            _events.Add(new SpellCast(me.Id, spell.Id, target, crit));
            _rng.Next(spell.DamageMin, spell.DamageMax);
            Support(me, spell, ally);
            Resolve();
            return;
        }
        me.Ap -= spell.ApCost;
        me.CastsThisTurn[spell.Id] = me.CastsThisTurn.GetValueOrDefault(spell.Id) + 1;
        // Counted down at the start of each of the caster's turns, so one more than the turns to skip.
        if (spell.Cooldown > 0)
            me.Cooldowns[spell.Id] = spell.Cooldown + 1;
        // The critical hit, then the roll, are drawn even on an empty cell: the sequence of draws depends
        // only on the actions.
        bool critical = _rng.Next(1, 100) <= spell.Crit;
        _events.Add(new SpellCast(me.Id, spell.Id, target, critical));
        int roll = _rng.Next(spell.DamageMin, spell.DamageMax) + (critical ? Spell.CritBonus(spell.DamageMax) : 0);
        IReadOnlyList<Fighter> area = InArea(spell, me.Cell, target);
        // What the living talents read (sprint 141), as the turn stood before this spell; then counted.
        HitContext context = me.Context(spell);
        me.CellsSinceSpell = 0;
        if (spell.DamageMax > 0)
            me.DamageCastsThisTurn++;
        if (spell.DamageMax > 0)
        {
            // Who may react (sprint 137): the fighter aimed at; with Wave (sprint 144), the first marked ones of the area too.
            int wave = me.Talents.SelectMany(t => t.Rules).OfType<WaveRule>().Sum(r => r.Extra);
            HashSet<Fighter> reacting = [.. area.Where(f => f.Cell == target || wave > 0).Where(f => ReactionOn(me, f, spell.Element) is not null)
                .OrderBy(f => f.Cell == target ? 0 : 1).Take(1 + wave)];
            foreach (Fighter hit in area)
            {
                if (!hit.IsAlive)
                    continue;
                // Next to the caster at the moment of the hit, for a passive that answers blows (sprint 90).
                bool melee = hit.Cell.DistanceTo(me.Cell) == 1;
                bool reacts = reacting.Contains(hit) && ReactionOn(me, hit, spell.Element) is not null;
                HitContext here = ContextOf(me, spell, me.Cell, hit) with
                {
                    Walked = context.Walked,
                    CellsSinceSpell = context.CellsSinceSpell,
                    DamageCasts = context.DamageCasts,
                    Triggers = reacts,
                    OwnMark = hit.Mark?.Source == me.Id,
                    Zone = spell.Zone.Radius > 0,
                };
                int dealt = Damage(roll, me, hit, spell.Element, true, here, GuardOf(hit));
                if (Hurt(hit, dealt, spell.Element) > 0 && hit.IsAlive)
                {
                    foreach (PassiveSpec p in hit.Passives.Where(p => p.Trigger == Trigger.Hit && (!p.Melee || melee)))
                        _pending.Enqueue((hit, p, me));
                }
                Imprint(me, hit, spell.Element, reacts, dealt);
                // Lightning's Arc (sprint 138), an Electrified fighter (sprint 140): part of the hit jumps on, once.
                int jump = Math.Max(spell.Element == Element.Lightning ? Elements.Arc * (100 + Talents.SignatureBonus(me.Talents)) / 100 : 0,
                    hit.StatusList.Where(s => s.Stat == Stat.Electrified).Select(s => s.Value).DefaultIfEmpty(0).Max());
                if (jump > 0 && !IsOver)
                    Arc(me, hit, roll, area, jump, spell.Element, context);
            }
        }
        Apply(me, spell.AllEffects, area, target, critical, attacker: null, zone: [.. spell.Zone.Cells(me.Cell, target)], spell: spell.Id);
        // Terrain (sprint 142): the last damage spell and its target, for an Echo; a spell that strikes nobody leaves its
        // Remanence; Side Step's free cell, once a turn.
        if (spell.DamageMax > 0 && area.FirstOrDefault(f => f.Team != me.Team) is Fighter aimed)
            me.LastHit = (spell.Id, aimed.Id);
        if (spell.DamageMax > 0 && area.Count == 0 && !IsOver && me.Talents.SelectMany(t => t.Rules).OfType<RemanenceRule>().FirstOrDefault() is RemanenceRule rem)
            Remain(me, spell, target, rem);
        if (!me.SideStepped && me.IsAlive && me.Talents.SelectMany(t => t.Rules).OfType<SideStepRule>().Any())
        {
            me.SideStepped = true;
            me.Mp++;
        }
        Resolve();
        // A fighter who dies during its own turn hands over at once.
        if (!IsOver && !me.IsAlive)
            NextTurn();
    }

    /// <summary>
    /// The effects of a spell, or of a passive (sprint 90) with its bearer as <paramref name="me"/>, on the
    /// fighters of <paramref name="area"/>; <paramref name="attacker"/>, for a passive set off by a hit.
    /// </summary>
    /// <param name="zone">A spell's area, every cell of it, for a glyph (sprint 130); <paramref name="spell"/>, its id.</param>
    private void Apply(Fighter me, IReadOnlyList<SpellEffect> effects, IReadOnlyList<Fighter> area, Cell target, bool critical, Fighter? attacker,
        bool passive = false, IReadOnlyList<Cell>? zone = null, string? spell = null)
    {
        foreach (SpellEffect effect in effects)
        {
            // Healing grows with a critical hit, as damage does, and with no characteristic (D62).
            int effectRoll = effect is HealEffect heal ? _rng.Next(heal.Min, heal.Max) + (critical ? Spell.CritBonus(heal.Max) : 0) : 0;
            switch (effect)
            {
                case SummonEffect summon when passive:
                    SummonAround(me, _data.Summons[summon.Summon], summon.Count);
                    continue;
                case SummonEffect summon:
                    Summon(me, _data.Summons[summon.Summon], target);
                    continue;
                case LeapEffect:
                    if (IsFree(target))
                    {
                        Cell from = me.Cell;
                        me.Cell = target;
                        _events.Add(new Leaped(me.Id, from, target));
                        StrikeAt(me);
                    }
                    continue;
                case SwapEffect:
                    if (At(target) is Fighter other && other != me)
                    {
                        (me.Cell, other.Cell) = (other.Cell, me.Cell);
                        _events.Add(new Swapped(me.Id, other.Id));
                        StrikeAt(me);
                        StrikeAt(other);
                    }
                    continue;
                case GlyphEffect glyph:
                    if (zone is not null && spell is not null && !IsOver)
                        Lay(me, glyph, zone, spell);
                    continue;
            }
            IEnumerable<Fighter> touched = effect.Affects switch
            {
                Affects.Caster => [me],
                Affects.Attacker => attacker is null ? [] : [attacker],
                _ => area.Where(f => Touches(effect.Affects, me, f)),
            };
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
                        Shove(f, Cell.Direction(me.Cell, f.Cell), push.Cells, collide: true, Talents.Collision(me.Talents));
                        break;
                    case PullEffect pull:
                        Shove(f, Cell.Direction(f.Cell, me.Cell), Math.Min(pull.Cells, f.Cell.DistanceTo(me.Cell) - 1), collide: false);
                        break;
                    case StatusEffect status:
                        SetStatus(me, f, status);
                        break;
                }
            }
        }
    }

    /// <param name="reaction">The reaction setting it (sprint 137): the status bears its name, and renews only its own.</param>
    private void SetStatus(Fighter me, Fighter f, StatusEffect status, string? reaction = null)
    {
        // The same status from the same caster is renewed, not stacked; one fighter electrified by each caster (sprint 140).
        f.StatusList.RemoveAll(st => st.Stat == status.Stat && st.Element == status.Element && st.Source == me.Id && st.Reaction == reaction);
        if (status.Stat == Stat.Electrified)
        {
            foreach (Fighter other in Fighters.Where(o => o != f))
                other.StatusList.RemoveAll(st => st.Stat == Stat.Electrified && st.Source == me.Id);
        }
        f.StatusList.Add(new Status(status.Stat, status.Value, status.Turns, status.Element, me.Id, reaction));
        _events.Add(new StatusAdded(f.Id, status.Stat, status.Value, status.Turns, status.Element, reaction));
        // On the fighter playing, action and movement points change at once (sprint 90):
        // a status of one turn set on oneself counts this very turn.
        if (f == Current && status.Stat == Stat.Ap)
            f.Ap = Math.Max(0, f.Ap + status.Value);
        if (f == Current && status.Stat == Stat.Mp)
            f.Mp = Math.Max(0, f.Mp + status.Value);
    }

    /// <summary>A creature on a free cell, in the caster's team, playing right after it (after <paramref name="after"/>, if given).</summary>
    private Fighter? Summon(Fighter me, SummonSpec m, Cell cell, Fighter? after = null)
    {
        if (!IsFree(cell))
            return null;
        int hp = m.HpOfSummoner > 0 ? Math.Max(1, me.MaxHp * m.HpOfSummoner / 100) : m.Hp;
        var spec = new FighterSpec(m.Name, m.Look, me.Team, hp, m.Ap, m.Mp, m.Initiative, 0, m.Spells, Passives: m.Passives);
        var summoned = new Fighter(_fighters.Count, spec, cell, SpellsOf(spec, _data), PassivesOf(m.Passives, _data), me.Id, m.Id) { LaysMarks = me.LaysMarks };
        _fighters.Add(summoned);
        // Right after its summoner in the order of play, or after the one summoned just before.
        int at = _order.IndexOf(after ?? me) + 1;
        _order.Insert(at, summoned);
        if (at <= _current)
            _current++;
        _events.Add(new Summoned(summoned.Id, me.Id, cell));
        return summoned;
    }

    /// <summary>
    /// A passive's creatures (sprint 90): <paramref name="count"/> of them on the free cells next to the
    /// bearer, in the order of <see cref="Cell.Steps"/>, then two steps away; fewer if there is no room.
    /// </summary>
    private void SummonAround(Fighter me, SummonSpec m, int count)
    {
        Cell[] near = [.. Cell.Steps.Select(d => me.Cell + d)];
        Cell[] farther = [.. Cell.Steps.Select(d => me.Cell + d + d), .. Cell.Steps.SelectMany(d => Cell.Steps.Where(e => e != d && e != new Cell(-d.X, -d.Y)).Select(e => me.Cell + d + e)).Distinct()];
        Fighter? last = null;
        foreach (Cell cell in near.Concat(farther.Where(c => !near.Contains(c))))
        {
            if (count == 0)
                break;
            if (IsFree(cell) && Summon(me, m, cell, last) is Fighter added)
            {
                last = added;
                count--;
            }
        }
    }

    /// <summary>Moves a fighter cell by cell; what stops a push hurts, per cell left.</summary>
    /// <param name="collision">The damage a cell not travelled does (Heavy Push, sprint 141).</param>
    private void Shove(Fighter f, Cell direction, int cells, bool collide, int collision = CollisionDamage)
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
            Hurt(f, blocked * collision);
        // Where it lands, a glyph strikes (sprint 130), after the collision.
        if (path.Count > 0)
            StrikeAt(f);
    }

    /// <summary>
    /// Damage through the shields first, then the hit points; a death may end the fight. Returns what was
    /// taken, shield and hit points. Queues the passives a loss of hit points or a death sets off (sprint 90).
    /// </summary>
    private int Hurt(Fighter hit, int damage, Element element = Element.Neutral)
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
            return absorbed;
        hit.Hp = Math.Max(0, hit.Hp - damage);
        hit.Eroded += damage * Erosion / 100;
        _events.Add(new Damaged(hit.Id, damage, hit.Hp, element));
        if (hit.IsAlive)
        {
            // Below a share of its hit points, once per fight.
            foreach (PassiveSpec p in hit.Passives)
            {
                if (p.Trigger == Trigger.LowHp && damage > 0 && hit.Hp * 100 < p.Percent * hit.Spec.Hp && hit.LowHpFired.Add(p.Id))
                    _pending.Enqueue((hit, p, null));
            }
            return absorbed + damage;
        }
        _events.Add(new Died(hit.Id));
        hit.StatusList.Clear();
        hit.Shields.Clear();
        if (hit.Mark is not null)
        {
            hit.Mark = null;
            _events.Add(new MarkEnded(hit.Id));
        }
        List<Fighter> dead = [hit];
        // Summons die with their summoner.
        foreach (Fighter summon in Fighters.Where(f => f.Summoner == hit.Id && f.IsAlive).ToList())
        {
            summon.Hp = 0;
            _events.Add(new Died(summon.Id));
            dead.Add(summon);
        }
        foreach (Fighter d in dead)
        {
            foreach (PassiveSpec p in d.Passives.Where(p => p.Trigger == Trigger.Death))
                _pending.Enqueue((d, p, null));
            // Its glyphs vanish with it (sprint 130), and its marks (sprint 137).
            foreach (Glyph g in _glyphs.Where(g => g.Caster == d.Id).ToList())
                EndGlyph(g);
            foreach (Fighter marked in Fighters.Where(f => f.Mark?.Source == d.Id && f.IsAlive))
            {
                marked.Mark = null;
                _events.Add(new MarkEnded(marked.Id));
            }
        }
        // An ally of the same family falls (not a summon), within the passive's range.
        if (!hit.IsSummon && hit.Spec.Family is string family)
        {
            foreach (Fighter f in Fighters.Where(f => f.IsAlive && f.Team == hit.Team && f.Spec.Family == family))
            {
                foreach (PassiveSpec p in f.Passives.Where(p => p.Trigger == Trigger.AllyDies && (p.Range == 0 || f.Cell.DistanceTo(hit.Cell) <= p.Range)))
                    _pending.Enqueue((f, p, null));
            }
        }
        // A team with nobody left has lost (summons never outlive their summoner).
        for (int team = 0; team < 2; team++)
        {
            if (!Fighters.Any(f => f.Team == team && f.IsAlive))
            {
                End(1 - team);
                break;
            }
        }
        return absorbed + damage;
    }

    /// <summary>
    /// Plays the passives set off during an action, in the order they were set off; those their effects
    /// set off follow at the end (an explosion that kills sets off the next). Every fighter dies once
    /// only, so it ends. Nothing more fires once the fight is over.
    /// </summary>
    private void Resolve()
    {
        while (_pending.TryDequeue(out (Fighter Bearer, PassiveSpec Passive, Fighter? Attacker) next))
        {
            (Fighter bearer, PassiveSpec p, Fighter? attacker) = next;
            if (IsOver || (p.Trigger != Trigger.Death && !bearer.IsAlive))
                continue;
            _events.Add(new PassiveTriggered(bearer.Id, p.Id));
            // Around the bearer; without an area, its own cell.
            Cell[] cells = p.Area is Area a ? [.. a.Cells(bearer.Cell, bearer.Cell)] : [bearer.Cell];
            if (p.DamageMax > 0)
            {
                int roll = _rng.Next(p.DamageMin, p.DamageMax);
                foreach (Fighter f in cells.Skip(p.Area is null ? 0 : 1).Select(At).OfType<Fighter>().ToList())
                {
                    if (!IsOver && f.IsAlive)
                        Hurt(f, Damage(roll, bearer, f, p.Element), p.Element);
                }
            }
            if (IsOver)
                continue;
            IReadOnlyList<Fighter> area = [.. cells.Where(Board.Contains).Select(At).OfType<Fighter>().Distinct()];
            Apply(bearer, p.AllEffects, area, bearer.Cell, critical: false, attacker is { IsAlive: true } ? attacker : null, passive: true);
        }
    }

    private void NextTurn()
    {
        // The statuses and shields of the fighter whose turn ends count one turn less; the points it keeps (Reserve, sprint 141).
        Fighter ending = Current;
        ending.CarriedAp = ending.IsAlive ? Talents.Reserve(ending.Talents, ending.Ap) : 0;
        foreach (Status st in ending.StatusList.Where(st => st.TurnsLeft <= 1).ToList())
            _events.Add(new StatusEnded(ending.Id, st.Stat));
        for (int i = 0; i < ending.StatusList.Count; i++)
            ending.StatusList[i] = ending.StatusList[i] with { TurnsLeft = ending.StatusList[i].TurnsLeft - 1 };
        ending.StatusList.RemoveAll(st => st.TurnsLeft <= 0);
        for (int i = 0; i < ending.Shields.Count; i++)
            ending.Shields[i] = (ending.Shields[i].Amount, ending.Shields[i].TurnsLeft - 1);
        ending.Shields.RemoveAll(sh => sh.TurnsLeft <= 0);
        // An Echo (sprint 142): having walked far enough, the last damage spell is left on the cell the turn started from.
        ending.Echo = null;
        if (ending.IsAlive && ending.LastHit is (string echoed, int echoTarget)
            && ending.Talents.SelectMany(t => t.Rules).OfType<EchoRule>().FirstOrDefault() is EchoRule echo && ending.StepsThisTurn >= echo.Cells)
            ending.Echo = (ending.TurnStart, echoed, echoTarget);
        // The marks it laid count one of its turns less (sprint 137): they last until the end of its next turn.
        foreach (Fighter marked in Fighters.Where(f => f.Mark?.Source == ending.Id))
        {
            marked.Mark = marked.Mark! with { TurnsLeft = marked.Mark.TurnsLeft - 1 };
            if (marked.Mark.TurnsLeft > 0)
                continue;
            marked.Mark = null;
            _events.Add(new MarkEnded(marked.Id));
        }
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
        // With the talents' points of this turn and the points kept (sprint 141).
        next.Turn++;
        (int talentAp, int talentMp) = Talents.TurnPoints(next.Talents, next.Turn);
        next.Ap = Math.Max(0, next.Spec.Ap + next.StatusList.Where(st => st.Stat == Stat.Ap).Sum(st => st.Value) + talentAp + next.CarriedAp);
        next.Mp = Math.Max(0, next.Spec.Mp + next.StatusList.Where(st => st.Stat == Stat.Mp).Sum(st => st.Value) + talentMp);
        next.CarriedAp = 0;
        next.StepsThisTurn = next.DamageCastsThisTurn = 0;
        next.SideStepped = false;
        next.SupportsThisTurn = next.ReactionsThisTurn = 0;
        next.Ap = Math.Max(0, next.Ap - next.NextTurnPenalty);
        next.NextTurnPenalty = 0;
        next.TurnStart = next.Cell;
        next.LastHit = null;
        next.CastsThisTurn.Clear();
        foreach (string id in next.Cooldowns.Keys.ToList())
        {
            if (--next.Cooldowns[id] == 0)
                next.Cooldowns.Remove(id);
        }
        _events.Add(new TurnStarted(next.Id, Round));
        // A new turn of play: the fighter's glyphs count one turn less, and every glyph may strike again (sprint 130).
        foreach (Glyph g in _glyphs.Where(g => g.Caster == next.Id).ToList())
        {
            if (--g.TurnsLeft <= 0)
                EndGlyph(g);
        }
        foreach (Glyph g in _glyphs)
            g.Struck.Clear();
        // Poisons strike at the start of the turn, in their element, against the resistances.
        foreach (Status poison in next.StatusList.Where(st => st.Stat == Stat.Poison).ToList())
        {
            if (IsOver || !next.IsAlive)
                break;
            long damage = (long)poison.Value * (100 - next.Resistance(poison.Element)) / 100;
            Hurt(next, (int)Math.Max(0, damage), poison.Element);
        }
        // Then the glyphs under it, then its passives of the start of a turn (the order of T98), if it still stands (sprint 90).
        if (!IsOver && next.IsAlive)
            StrikeAt(next);
        Rally(next);
        // Then its echo (sprint 142), if its target stands within the spell's range of it.
        if (!IsOver && next.IsAlive && next.Echo is (Cell echoFrom, string echoSpell, int echoAt) && next.Spells.FirstOrDefault(s => s.Id == echoSpell) is Spell echoed2
            && _fighters[echoAt] is { IsAlive: true } echoTarget2 && echoFrom.DistanceTo(echoTarget2.Cell) is int d && d >= echoed2.MinRange && d <= next.MaxRangeOf(echoed2))
        {
            int percent = next.Talents.SelectMany(t => t.Rules).OfType<EchoRule>().Select(r => r.Percent).DefaultIfEmpty(0).Max();
            _events.Add(new Echoed(next.Id, echoTarget2.Id, echoFrom));
            Hurt(echoTarget2, Damage(_rng.Next(echoed2.DamageMin, echoed2.DamageMax), next, echoTarget2, echoed2.Element) * percent / 100, echoed2.Element);
        }
        next.Echo = null;
        if (!IsOver && next.IsAlive)
        {
            foreach (PassiveSpec p in next.Passives.Where(p => p.Trigger == Trigger.TurnStart))
                _pending.Enqueue((next, p, null));
        }
        Resolve();
        if (!IsOver && !next.IsAlive)
            NextTurn();
    }

    /// <summary>A Banner at the start of its bearer's turn (sprint 143): the allies near it deal more for a turn.</summary>
    private void Rally(Fighter bearer)
    {
        foreach (BannerRule banner in bearer.Talents.SelectMany(t => t.Rules).OfType<BannerRule>())
        {
            foreach (Fighter friend in Fighters.Where(f => f.IsAlive && f != bearer && f.Team == bearer.Team && f.Cell.DistanceTo(bearer.Cell) <= banner.Cells).ToList())
                SetStatus(bearer, friend, new StatusEffect(Affects.Allies, Stat.Damage, banner.Percent, 1));
        }
    }

    private void End(int? winningTeam)
    {
        IsOver = true;
        WinningTeam = winningTeam;
        _events.Add(new FightEnded(winningTeam));
    }
}
