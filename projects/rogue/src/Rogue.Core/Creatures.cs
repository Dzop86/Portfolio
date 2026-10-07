namespace Rogue.Core;

public enum MonsterKind
{
    Rat,
    Goblin,
    Orc,
    Troll,
}

public sealed class Monster
{
    internal Monster(MonsterKind kind, Point position)
    {
        Kind = kind;
        Position = position;
        (MaxHp, Attack, Defense, Value) = kind switch
        {
            MonsterKind.Rat => (5, 2, 0, 5),
            MonsterKind.Goblin => (10, 4, 1, 10),
            MonsterKind.Orc => (16, 6, 2, 25),
            MonsterKind.Troll => (26, 8, 3, 50),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        Hp = MaxHp;
    }

    public MonsterKind Kind { get; }

    public Point Position { get; internal set; }

    public int Hp { get; internal set; }

    public int MaxHp { get; }

    public int Attack { get; }

    public int Defense { get; }

    /// <summary>Points scored, and experience gained, by killing it.</summary>
    public int Value { get; }

    /// <summary>A monster sleeps until the player first sees it, then chases the player.</summary>
    public bool Awake { get; internal set; }
}

public sealed class Player
{
    public const int StartHp = 30;

    public Point Position { get; internal set; }

    public int Hp { get; internal set; } = StartHp;

    public int MaxHp { get; internal set; } = StartHp;

    public int Attack { get; internal set; } = 4;

    public int Defense { get; internal set; } = 1;

    public int Level { get; internal set; } = 1;

    /// <summary>Experience gathered since the last level.</summary>
    public int Xp { get; internal set; }

    /// <summary>Experience needed to reach the next level.</summary>
    public int XpToNextLevel => 30 * Level;

    public int Potions { get; internal set; }
}

public enum ItemKind
{
    Potion,
    Gold,
}

/// <summary>An item lying on the floor; <see cref="Amount"/> is the number of coins for gold.</summary>
public readonly record struct Item(ItemKind Kind, int Amount = 1);
