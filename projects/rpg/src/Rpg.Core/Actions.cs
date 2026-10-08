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
}

/// <summary>What happened, in order: the client animates these, the tests read them.</summary>
public abstract record FightEvent;

public sealed record TurnStarted(int Fighter, int Round) : FightEvent;

public sealed record Moved(int Fighter, IReadOnlyList<Cell> Path) : FightEvent;

public sealed record SpellCast(int Fighter, string Spell, Cell Target) : FightEvent;

public sealed record Damaged(int Fighter, int Amount, int HpLeft) : FightEvent;

public sealed record Died(int Fighter) : FightEvent;

/// <summary>The end of the fight; <see cref="WinningTeam"/> is null for a draw at the round limit.</summary>
public sealed record FightEnded(int? WinningTeam) : FightEvent;
