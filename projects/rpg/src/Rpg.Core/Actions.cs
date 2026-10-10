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
/// <param name="CritMin">The damage of a critical hit's lowest roll; with <paramref name="CritMax"/>, 0 when the spell never lands one.</param>
public sealed record Forecast(Fighter Fighter, int DamageMin, int DamageMax, int HealMin, int HealMax, Element Element, int CritMin = 0, int CritMax = 0)
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

public sealed record StatusAdded(int Fighter, Stat Stat, int Value, int Turns, Element Element) : FightEvent;

public sealed record StatusEnded(int Fighter, Stat Stat) : FightEvent;

/// <summary>A creature summoned by <see cref="Summoner"/> appears; it is <c>Fighters[Fighter]</c>.</summary>
public sealed record Summoned(int Fighter, int Summoner, Cell Cell) : FightEvent;

/// <summary>The end of the fight; <see cref="WinningTeam"/> is null for a draw at the round limit.</summary>
public sealed record FightEnded(int? WinningTeam) : FightEvent;
