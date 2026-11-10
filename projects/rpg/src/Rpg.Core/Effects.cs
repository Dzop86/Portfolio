using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>
/// The element a spell strikes in. Each of the four has its own characteristic (sprint 55) and
/// fighters resist them separately; a neutral spell ignores resistances. Since sprint 138 (D80), the
/// compounds of the talent tree follow (<see cref="Elements"/>).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Element>))]
public enum Element
{
    Neutral,
    Earth,
    Fire,
    Water,
    Air,

    /// <summary>Fire and Air (sprint 138): its signature, the Arc.</summary>
    Lightning,

    /// <summary>Water and Air (sprint 184): its signature, Bite (−1 AP).</summary>
    Ice,

    /// <summary>Earth and Water (sprint 184): its signature, Mire (−1 MP).</summary>
    Mud,

    /// <summary>Earth and Fire (sprint 184): its signature, Melt (−8 % resistance).</summary>
    Magma,

    /// <summary>Fire and Water (sprint 184): its signature, Veil (−10 % damage dealt).</summary>
    Steam,

    /// <summary>Earth and Air (sprint 184): its signature, Grit (−1 range).</summary>
    Sand,

    /// <summary>Fire, Water and Air (sprint 185): its signature, Crossing (no line of sight needed).</summary>
    Ether,

    /// <summary>Earth, Fire and Air (sprint 185): its signature, Fallout (a glyph of ash).</summary>
    Ash,

    /// <summary>Earth, Fire and Water (sprint 185): its signature, Bud (the most wounded ally healed).</summary>
    Sap,

    /// <summary>Earth, Water and Air (sprint 185): its signature, Brine (half the healing received).</summary>
    Salt,
}

/// <summary>Who an effect touches among the fighters in the spell's area.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Affects>))]
public enum Affects
{
    Enemies,
    Allies,
    All,

    /// <summary>The caster alone, wherever the spell lands.</summary>
    Caster,

    /// <summary>A passive's only (<see cref="Trigger.Hit"/>): the caster of the spell that set it off.</summary>
    Attacker,
}

/// <summary>The shape of a spell's area, around the target cell.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AreaShape>))]
public enum AreaShape
{
    Point,

    /// <summary>The target and the cells in the four straight directions, up to the radius.</summary>
    Cross,

    /// <summary>Every cell within the radius in steps (a diamond on the grid).</summary>
    Circle,

    /// <summary>From the target onwards, away from the caster, radius cells more.</summary>
    Line,
}

/// <summary>A spell's area: the cells it touches around its target.</summary>
public sealed record Area(AreaShape Shape, int Radius = 0)
{
    public static readonly Area One = new(AreaShape.Point);

    /// <summary>The cells touched when <paramref name="caster"/> aims at <paramref name="target"/>, target first.</summary>
    public IEnumerable<Cell> Cells(Cell caster, Cell target)
    {
        yield return target;
        switch (Shape)
        {
            case AreaShape.Cross:
                foreach (Cell step in Cell.Steps)
                {
                    for (int i = 1; i <= Radius; i++)
                        yield return new Cell(target.X + step.X * i, target.Y + step.Y * i);
                }
                break;
            case AreaShape.Circle:
                for (int dy = -Radius; dy <= Radius; dy++)
                {
                    for (int dx = -Radius; dx <= Radius; dx++)
                    {
                        int d = Math.Abs(dx) + Math.Abs(dy);
                        if (d > 0 && d <= Radius)
                            yield return new Cell(target.X + dx, target.Y + dy);
                    }
                }
                break;
            case AreaShape.Line:
                Cell dir = Cell.Direction(caster, target);
                for (int i = 1; i <= Radius; i++)
                    yield return new Cell(target.X + dir.X * i, target.Y + dir.Y * i);
                break;
        }
    }
}

/// <summary>What lasts on a fighter for some turns.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Stat>))]
public enum Stat
{
    /// <summary>Damage at the start of each of the fighter's turns, in the status's element.</summary>
    Poison,

    /// <summary>Action points added (or removed) at the start of each turn, and at once on the fighter playing (sprint 90).</summary>
    Ap,

    /// <summary>Movement points added (or removed) at the start of each turn, and at once on the fighter playing (sprint 90).</summary>
    Mp,

    /// <summary>Damage the fighter deals, in percent.</summary>
    Damage,

    /// <summary>Resistance to the status's element (to every element when neutral), in percent.</summary>
    Resistance,

    /// <summary>Cells of range added (or removed) to the fighter's spells that reach farther than 1, never under 1 (sprint 140).</summary>
    Range,

    /// <summary>
    /// Electrified (sprint 140): this percent of every hit the fighter takes jumps to the nearest fighter of its side within
    /// <see cref="Elements.ArcCells"/> cells; one fighter electrified at a time by each caster.
    /// </summary>
    Electrified,

    /// <summary>Healing received, in percent (Salt's Brine, sprint 185).</summary>
    Healing,

    /// <summary>Neither healing nor shield received (the reaction Parched, sprint 185).</summary>
    Parched,

    /// <summary>The fighter's cell blocks neither way nor sight (the reaction Scattered, sprint 185).</summary>
    Incorporeal,

    /// <summary>
    /// A Feint (sprint 191, D95, brick I): the next fighter whose spell hurts it from next to it loses this many movement points,
    /// and the status is spent.
    /// </summary>
    Feint,

    /// <summary>Neither a push nor a pull moves the fighter (sprint 192, D95, brick K): Stand Fast.</summary>
    Anchored,

    /// <summary>Hit points given back at the start of each of the fighter's turns, before the poisons (the Priest's Prayer, sprint 201): a poison the other way.</summary>
    Regen,

    /// <summary>
    /// Stealth (the Rogue's, sprint 204): an enemy can aim a spell at the bearer's cell from <see cref="Fight.HiddenCells"/> cells or less only;
    /// it ends when the bearer hurts an enemy.
    /// </summary>
    Hidden,

    /// <summary>
    /// A form (the Berserker's Rage, sprint 206, docs/classes-berserker.md §2): this percent more damage, as long as the fight lasts; spells marked
    /// "form" ask for it. Nothing takes it away, not even a purge.
    /// </summary>
    Rage,

    /// <summary>
    /// The Summoner's order (sprint 211, docs/classes-invocateur.md §3): the creatures of the status's source strike the bearer for this percent more,
    /// and go for it first. One fighter ordered by each summoner at a time.
    /// </summary>
    Ordered,
}

/// <summary>
/// An effect of a spell besides its damage, described in <c>data/spells.json</c> with a "kind".
/// Effects with a range draw one roll each per cast, after the damage's, whoever they touch.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(HealEffect), "heal")]
[JsonDerivedType(typeof(ShieldEffect), "shield")]
[JsonDerivedType(typeof(PushEffect), "push")]
[JsonDerivedType(typeof(PullEffect), "pull")]
[JsonDerivedType(typeof(StatusEffect), "status")]
[JsonDerivedType(typeof(SummonEffect), "summon")]
[JsonDerivedType(typeof(LeapEffect), "leap")]
[JsonDerivedType(typeof(SwapEffect), "swap")]
[JsonDerivedType(typeof(GlyphEffect), "glyph")]
[JsonDerivedType(typeof(BorrowEffect), "borrow")]
[JsonDerivedType(typeof(DelayEffect), "delay")]
[JsonDerivedType(typeof(HasteEffect), "haste")]
[JsonDerivedType(typeof(PurgeEffect), "purge")]
[JsonDerivedType(typeof(ReviveEffect), "revive")]
[JsonDerivedType(typeof(DrainEffect), "drain")]
public abstract record SpellEffect(Affects Affects);

// The Chronomancer's time (sprint 199, docs/classes-mage.md §3).

/// <summary>The caster takes <see cref="Ap"/> action points from its next turn: it has them at once, and that many fewer when its next turn starts.</summary>
public sealed record BorrowEffect(Affects Affects, int Ap) : SpellEffect(Affects);

/// <summary>
/// Each fighter touched changes place in the order of play: <see cref="Places"/> living fighters later, or earlier when negative. It never passes the fighter
/// playing: one yet to play stays after it, one that has played stays before it (its new place counts from the next round); the fighter playing does not move.
/// </summary>
public sealed record DelayEffect(Affects Affects, int Places) : SpellEffect(Affects);

/// <summary>Each fighter touched has its cooldowns <see cref="Turns"/> shorter; one down to nothing is over.</summary>
public sealed record HasteEffect(Affects Affects, int Turns) : SpellEffect(Affects);

/// <summary>
/// Gives back hit points, never above the maximum; with <see cref="Overflow"/> (the Priest's, sprint 201), what would go past it becomes a shield
/// for <see cref="Fight.OverflowTurns"/> of the healed fighter's turns. With <see cref="Grows"/> (the Priest's too), the roll gains what a hit gains from the
/// caster's strongest element (<see cref="Fight.HealBonus"/>), so that a healer's heals keep up with the blows.
/// </summary>
public sealed record HealEffect(Affects Affects, int Min, int Max, bool Overflow = false, bool Grows = false) : SpellEffect(Affects);

/// <summary>
/// The purge (sprint 201): an ally loses its ailments (poisons, values below zero, Parched, Electrified); an enemy its boons (values above zero,
/// Incorporeal, Anchored, a Feint) and its shields.
/// </summary>
public sealed record PurgeEffect(Affects Affects) : SpellEffect(Affects);

/// <summary>
/// The resurrection (the Cleric's, sprint 202): a fallen ally of the caster, not a creature, rises on its cell if it is free, with <see cref="Percent"/> % of its
/// hit points, its statuses and shields gone; once a fight for each caster and spell.
/// </summary>
public sealed record ReviveEffect(Affects Affects, int Percent) : SpellEffect(Affects);

/// <summary>Life drawn (the Berserker's, sprint 206): the caster heals this percent of the damage the spell did to its enemies.</summary>
public sealed record DrainEffect(Affects Affects, int Percent) : SpellEffect(Affects);

/// <summary>Absorbs the next damage, up to <see cref="Amount"/>, for some turns of the fighter.</summary>
public sealed record ShieldEffect(Affects Affects, int Amount, int Turns) : SpellEffect(Affects);

/// <summary>
/// Pushes away from the caster, cell by cell; stopped by an obstacle, a hole's edge, a fighter or the
/// board's edge, the fighter takes <see cref="Fight.CollisionDamage"/> per cell it could not travel.
/// </summary>
public sealed record PushEffect(Affects Affects, int Cells) : SpellEffect(Affects);

/// <summary>Pulls towards the caster, cell by cell, stopping before anything in the way (no damage).</summary>
public sealed record PullEffect(Affects Affects, int Cells) : SpellEffect(Affects);

/// <summary>A status for some of the fighter's turns: poison, or more (or fewer) AP, MP, damage, resistance.</summary>
public sealed record StatusEffect(Affects Affects, Stat Stat, int Value, int Turns, Element Element = Element.Neutral) : SpellEffect(Affects);

/// <summary>
/// Summons a creature of <c>data/summons.json</c> on the target cell, which must be free: it joins the
/// caster's team, plays right after the caster, and dies with it; at most <see cref="Max"/> at once.
/// A passive (sprint 90) summons <see cref="Count"/> of them on the free cells next to its bearer.
/// </summary>
public sealed record SummonEffect(Affects Affects, string Summon, int Max = 1, int Count = 1) : SpellEffect(Affects);

/// <summary>The caster lands on the target cell, which must be a free floor cell (sprint 90).</summary>
public sealed record LeapEffect(Affects Affects) : SpellEffect(Affects);

/// <summary>The caster and the fighter on the target cell trade places (sprint 90).</summary>
public sealed record SwapEffect(Affects Affects) : SpellEffect(Affects);

/// <summary>
/// A glyph (sprint 130, T98): the floor cells of the spell's area are marked for <see cref="Turns"/> of the
/// caster's turns. It strikes a fighter it <see cref="SpellEffect.Affects"/> who walks onto one of its cells,
/// lands there (push, pull, leap, swap) or starts its turn there: damage in <see cref="Element"/>, a status,
/// or both; once per turn of play for each fighter. It vanishes with its caster, and a new glyph of the same
/// spell and caster replaces the old one.
/// </summary>
/// <param name="Status">The status it sets, if any, with <paramref name="Value"/> for <paramref name="StatusTurns"/> turns.</param>
/// <param name="Marks">A Seal's (sprint 142): it leaves its element's mark on whom it strikes, if its caster lays marks.</param>
/// <param name="Decays">A Remanence's (sprint 142): its damage falls away from its centre, 100, 70, 45 then 25 %.</param>
/// <param name="Max">How many of this spell's glyphs its caster keeps at once (sprint 191, brick G): a new one past that replaces the oldest.</param>
public sealed record GlyphEffect(Affects Affects, int Turns, int DamageMin = 0, int DamageMax = 0, Element Element = Element.Neutral,
    Stat? Status = null, int Value = 0, int StatusTurns = 0, bool Marks = false, bool Decays = false, int Max = 1) : SpellEffect(Affects)
{
    /// <summary>The status the glyph sets, as a spell's status effect, or null.</summary>
    public StatusEffect? AsStatus => Status is Stat stat ? new StatusEffect(Affects, stat, Value, StatusTurns, Element) : null;

    public double AverageDamage => (DamageMin + DamageMax) / 2.0;
}

/// <summary>A glyph on the board during a fight (sprint 130): its cells, its caster and spell, and its caster's turns left.</summary>
public sealed class Glyph
{
    internal Glyph(int id, int caster, string spell, IReadOnlyList<Cell> cells, GlyphEffect effect, Cell? centre = null)
    {
        Centre = centre ?? (cells.Count > 0 ? cells[0] : default);
        Id = id;
        Caster = caster;
        Spell = spell;
        Cells = cells;
        Effect = effect;
        TurnsLeft = effect.Turns;
    }

    /// <summary>The cell it was cast on (sprint 142): a decaying glyph strikes hardest there.</summary>
    public Cell Centre { get; }

    /// <summary>The share of its damage on a cell (sprint 142): all of it, or for a Remanence's, less away from the centre.</summary>
    public int PercentAt(Cell cell) => !Effect.Decays ? 100 : Centre.DistanceTo(cell) switch
    {
        0 => 100,
        1 => 70,
        2 => 45,
        _ => 25,
    };

    /// <summary>Its number in the fight, in the order glyphs were laid: the same in a replay.</summary>
    public int Id { get; }
    public int Caster { get; }
    public string Spell { get; }
    public IReadOnlyList<Cell> Cells { get; }
    public GlyphEffect Effect { get; }

    /// <summary>Laid by the reaction Buried (sprint 185), not by its spell's own effect.</summary>
    internal bool Buried { get; init; }

    /// <summary>Counted down at the start of each of its caster's turns; it vanishes at 0.</summary>
    public int TurnsLeft { get; internal set; }

    /// <summary>The fighters it struck during the turn being played: once each.</summary>
    internal readonly HashSet<int> Struck = [];

    /// <summary>
    /// The fighters standing on its cells when it was laid (sprint 152, D83, the limit of T100): spared at the start of their
    /// next turn, so they have the time to leave it; a fighter loses this the first time its turn starts.
    /// </summary>
    internal readonly HashSet<int> Spared = [];
}

/// <summary>When a passive fires (sprint 90, D69).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Trigger>))]
public enum Trigger
{
    /// <summary>At the start of the bearer's turn, after its poisons, if it is still standing.</summary>
    TurnStart,

    /// <summary>A spell damages the bearer (hit points or shield) and it survives; once per spell cast.</summary>
    Hit,

    /// <summary>Once per fight, the first time its hit points fall below a percentage of its maximum.</summary>
    LowHp,

    /// <summary>
    /// It fells an enemy that is not a summon (sprint 186, a path's): by its spell, Arc, glyph, passive, poison or push. Nothing
    /// fires once the fight is over.
    /// </summary>
    Kill,

    /// <summary>A fighter of its team and family (not a summon) dies, within some steps.</summary>
    AllyDies,

    /// <summary>At its death, unless that death ends the fight.</summary>
    Death,

    /// <summary>
    /// Another fighter of its team, not a summon, would fall within <see cref="PassiveSpec.Range"/> steps (sprint 192, D95, brick L): it stays at 1 hit point instead. The
    /// passive has no effect of its own.
    /// </summary>
    AllyFalls,
}

/// <summary>
/// A monster's passive (data/passives.json, sprint 90): when it fires, and the spell effects it applies,
/// its bearer in the caster's place. <see cref="Area"/> is centred on the bearer (null: its cell only);
/// damage, if any, is rolled once and strikes every fighter of the area, allies included.
/// </summary>
/// <param name="Melee">Hit only: the attacker stands next to the bearer.</param>
/// <param name="Percent">LowHp only: 1 to 99.</param>
/// <param name="Range">AllyDies and AllyFalls only: the ally fell within that many steps (0: anywhere).</param>
/// <param name="PerFight">Sprint 190 (D95, brick D): it fires at most that many times a fight (0: no limit).</param>
/// <param name="OncePerTurn">It fires at most once a turn of the bearer's.</param>
/// <param name="Still">TurnStart only: only if the bearer did not walk on its turn before.</param>
/// <param name="Clear">TurnStart only (sprint 186): it fires only with no living fighter of the other side within that many steps (0: always).</param>
public sealed record PassiveSpec(
    string Id,
    LocalizedText Name,
    LocalizedText Description,
    Trigger Trigger,
    IReadOnlyList<SpellEffect>? Effects = null,
    Area? Area = null,
    bool Melee = false,
    int Percent = 0,
    int Range = 0,
    int DamageMin = 0,
    int DamageMax = 0,
    Element Element = Element.Neutral,
    int Clear = 0,
    int PerFight = 0,
    bool OncePerTurn = false,
    bool Still = false)
{
    public IReadOnlyList<SpellEffect> AllEffects => Effects ?? [];
}

/// <summary>A status on a fighter during a fight; set by a reaction (sprint 137), it bears its id.</summary>
public sealed record Status(Stat Stat, int Value, int TurnsLeft, Element Element, int Source, string? Reaction = null);
