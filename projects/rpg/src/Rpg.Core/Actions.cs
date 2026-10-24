using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>What the fighter whose turn it is wants to do; recorded as is, so that a fight can be replayed.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MoveAction), "move")]
[JsonDerivedType(typeof(CastAction), "cast")]
[JsonDerivedType(typeof(EndTurnAction), "end")]
public abstract record FightAction;

/// <summary>Walk to a cell by a shortest path, one movement point per step.</summary>
public sealed record MoveAction(Cell To) : FightAction;

/// <summary>Cast a spell on a cell, whoever stands on it.</summary>
public sealed record CastAction(string Spell, Cell Target) : FightAction;

/// <summary>End the turn: the next fighter plays.</summary>
public sealed record EndTurnAction : FightAction;

/// <summary>Why an action was refused; the fight is then left unchanged.</summary>
public enum ActionError
{
    None,
    FightOver,
    OffBoard,
    NotFloor,
    Occupied,
    Unreachable,
    NotEnoughMp,
    UnknownSpell,
    NotEnoughAp,
    CastLimit,
    OutOfRange,
    NotInLine,
    NoLineOfSight,

    /// <summary>A summon needs a free cell, and the caster has as many of these creatures as allowed.</summary>
    TooManySummons,

    /// <summary>The spell was cast too recently: its cooldown is not over.</summary>
    Cooldown,

    /// <summary>A leap needs a free floor cell to land on (sprint 90).</summary>
    TargetNotFree,

    /// <summary>A swap needs a fighter on the target cell, of the side it affects, other than the caster (sprint 90).</summary>
    NoValidTarget,
}

/// <summary>What happened, in order: the client animates these, the tests read them.</summary>
public abstract record FightEvent;

public sealed record TurnStarted(int Fighter, int Round) : FightEvent;

public sealed record Moved(int Fighter, IReadOnlyList<Cell> Path) : FightEvent;

public sealed record SpellCast(int Fighter, string Spell, Cell Target, bool Critical = false) : FightEvent;

/// <summary>
/// What a cast would do to one fighter (<see cref="Fight.Foresee"/>): damage before shields, from
/// the lowest to the highest roll, and healing.
/// </summary>
/// <summary>
/// A hero's numbers in a fight (<see cref="Fight.HeroTotals"/>): hit points before Vitality, action and
/// movement points, initiative, its characteristics with what it wears, what it wears alone, its spells.
/// </summary>
public sealed record HeroNumbers(int Hp, int Ap, int Mp, int Initiative, Characteristics Stats, Characteristics Worn, IReadOnlyList<string> Spells);

/// <param name="CritMin">The damage of a critical hit's lowest roll; with <paramref name="CritMax"/>, 0 when the spell never lands one.</param>
/// <param name="Reaction">The reaction the hit would set off on this fighter (sprint 137), if any.</param>
public sealed record Forecast(Fighter Fighter, int DamageMin, int DamageMax, int HealMin, int HealMax, Element Element, int CritMin = 0, int CritMax = 0, string? Reaction = null)
{
    /// <summary>Even the lowest roll kills, through the shields.</summary>
    public bool SureKill => DamageMax > 0 && DamageMin >= Fighter.Hp + Fighter.Shield;
}

/// <summary>Hit points lost, in the element of what hit (neutral for a collision).</summary>
public sealed record Damaged(int Fighter, int Amount, int HpLeft, Element Element = Element.Neutral) : FightEvent;

public sealed record Died(int Fighter) : FightEvent;

public sealed record Healed(int Fighter, int Amount, int HpLeft) : FightEvent;

public sealed record Shielded(int Fighter, int Amount, int Turns) : FightEvent;

/// <summary>Damage a shield took instead of the fighter's hit points.</summary>
public sealed record ShieldAbsorbed(int Fighter, int Amount, int ShieldLeft) : FightEvent;

/// <summary>Pushed or pulled along <see cref="Path"/>; <see cref="Blocked"/> cells it could not travel (collision).</summary>
public sealed record Pushed(int Fighter, IReadOnlyList<Cell> Path, int Blocked) : FightEvent;

/// <summary>A status set on a fighter; by a reaction (sprint 137), with its id.</summary>
public sealed record StatusAdded(int Fighter, Stat Stat, int Value, int Turns, Element Element, string? Reaction = null) : FightEvent;

public sealed record StatusEnded(int Fighter, Stat Stat) : FightEvent;

/// <summary>A creature summoned by <see cref="Summoner"/> appears; it is <c>Fighters[Fighter]</c>.</summary>
public sealed record Summoned(int Fighter, int Summoner, Cell Cell) : FightEvent;

/// <summary>A passive of <see cref="Fighter"/> fires (sprint 90): its effects follow.</summary>
public sealed record PassiveTriggered(int Fighter, string Passive) : FightEvent;

/// <summary>The fighter leaps from a cell to another (sprint 90).</summary>
public sealed record Leaped(int Fighter, Cell From, Cell To) : FightEvent;

/// <summary>Two fighters trade places (sprint 90): <see cref="A"/> cast the spell.</summary>
public sealed record Swapped(int A, int B) : FightEvent;

/// <summary>A glyph is laid on <see cref="Cells"/> (sprint 130); it is <c>Glyphs</c>' glyph of that id.</summary>
public sealed record GlyphPlaced(int Glyph, int Caster, string Spell, IReadOnlyList<Cell> Cells, int Turns, Element Element = Element.Neutral) : FightEvent;

/// <summary>A glyph strikes a fighter on one of its cells: its damage and status follow.</summary>
public sealed record GlyphTriggered(int Glyph, int Fighter) : FightEvent;

/// <summary>A glyph vanishes: its turns are over, its caster died, or it was laid again.</summary>
public sealed record GlyphEnded(int Glyph) : FightEvent;

/// <summary>Lightning's Arc (sprint 138): part of a hit on <see cref="From"/> jumps to <see cref="To"/>; its damage follows.</summary>
public sealed record Arced(int From, int To) : FightEvent;

/// <summary>An Echo strikes (sprint 142): <see cref="Fighter"/>'s last spell again from <see cref="From"/>, on <see cref="Target"/>; its damage follows.</summary>
public sealed record Echoed(int Fighter, int Target, Cell From) : FightEvent;

/// <summary>A hero's hit leaves the mark of its element on an enemy (sprint 137), in place of any before.</summary>
public sealed record Marked(int Fighter, Element Element, int Source) : FightEvent;

/// <summary>A fighter's mark goes: used up by a reaction, its time over, its source or bearer dead.</summary>
public sealed record MarkEnded(int Fighter) : FightEvent;

/// <summary>A hero's hit sets off a reaction on a marked enemy (sprint 137): its statuses follow.</summary>
public sealed record ReactionTriggered(int Fighter, string Reaction, int Caster) : FightEvent;

/// <summary>The end of the fight; <see cref="WinningTeam"/> is null for a draw at the round limit.</summary>
public sealed record FightEnded(int? WinningTeam) : FightEvent;
