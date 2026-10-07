namespace Rogue.Core;

/// <summary>
/// What happened during a turn. The library holds no text: each client words the events in its
/// own language.
/// </summary>
public abstract record GameEvent;

public sealed record PlayerAttacked(MonsterKind Target, int Damage) : GameEvent;

public sealed record PlayerMissed(MonsterKind Target) : GameEvent;

public sealed record MonsterKilled(MonsterKind Kind, int Value) : GameEvent;

public sealed record MonsterAttacked(MonsterKind Attacker, int Damage) : GameEvent;

public sealed record MonsterMissed(MonsterKind Attacker) : GameEvent;

public sealed record GoldPicked(int Amount) : GameEvent;

public sealed record PotionPicked : GameEvent;

public sealed record PotionDrunk(int Healed) : GameEvent;

public sealed record LevelGained(int Level) : GameEvent;

public sealed record FloorReached(int Depth) : GameEvent;

public sealed record PlayerDied(MonsterKind Killer) : GameEvent;

public sealed record DungeonEscaped : GameEvent;
