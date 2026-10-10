using Rpg.Core;

namespace Rpg.Client;

/// <summary>The five characteristics, in the order the screen lists them.</summary>
public enum Characteristic
{
    Vitality,
    Earth,
    Fire,
    Water,
    Air,
}

/// <summary>
/// The characteristics and spells screens, without any engine: a draft of the hero's points that
/// the + and − buttons change, never beyond what the level gives (<see cref="Progression"/>), and
/// that the server receives as <see cref="Points"/> when the player saves. Points already saved
/// can be taken back too: the server only checks the totals.
/// </summary>
public sealed class PointsEditor
{
    private readonly int[] _stats = new int[5];
    private readonly Dictionary<string, int> _ranks = new(StringComparer.Ordinal);

    public PointsEditor(Hero hero, GameData data)
    {
        Hero = hero ?? throw new ArgumentNullException(nameof(hero));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Characteristics st = hero.Stats ?? Characteristics.None;
        (_stats[0], _stats[1], _stats[2], _stats[3], _stats[4]) = (st.Vitality, st.Earth, st.Fire, st.Water, st.Air);
        foreach ((string spell, int rank) in hero.RanksGiven)
            _ranks[spell] = rank;
    }

    /// <summary>The hero as saved.</summary>
    public Hero Hero { get; }

    public GameData Data { get; }

    public int this[Characteristic stat] => _stats[(int)stat];

    public int CharacteristicPointsLeft => Progression.CharacteristicPoints(Hero.Level) - _stats.Sum();

    public int SpellPointsLeft => Progression.SpellPoints(Hero.Level) - _ranks.Values.Sum(Progression.RankCost);

    /// <summary>The class's spells the level has unlocked, in the class's order.</summary>
    public IReadOnlyList<Spell> Spells =>
        Data.Class(Hero.Class) is HeroClass c ? [.. c.Spells.Select(id => Data.Spells[id]).Where(s => s.Level <= Hero.Level)] : [];

    public int Rank(Spell spell) => _ranks.GetValueOrDefault((spell ?? throw new ArgumentNullException(nameof(spell))).Id, 1);

    /// <summary>The spell points the next rank of this spell costs; 0 at its last rank.</summary>
    public int NextRankCost(Spell spell) => Rank(spell) >= spell.MaxRank ? 0 : Progression.RankCost(Rank(spell) + 1) - Progression.RankCost(Rank(spell));

    public bool CanAdd(Characteristic stat, int amount = 1) => amount > 0 && amount <= CharacteristicPointsLeft;

    public bool CanRemove(Characteristic stat, int amount = 1) => amount > 0 && _stats[(int)stat] >= amount;

    public bool CanRaise(Spell spell) => Spells.Contains(spell) && NextRankCost(spell) is > 0 and var cost && cost <= SpellPointsLeft;

    public bool CanLower(Spell spell) => Rank(spell) > 1;

    /// <summary>The most a characteristic can hold now: what it has and every point left.</summary>
    public int Max(Characteristic stat) => _stats[(int)stat] + CharacteristicPointsLeft;

    /// <summary>Puts a characteristic at a value, kept between 0 and <see cref="Max"/> (the typed value of the screen).</summary>
    public void Set(Characteristic stat, int value) => _stats[(int)stat] = Math.Clamp(value, 0, Max(stat));

    /// <summary>Takes back every characteristic point, to spend them again (D62); the server only checks totals.</summary>
    public void ResetCharacteristics() => Array.Clear(_stats);

    /// <summary>Adds points to a characteristic (as many as asked and left).</summary>
    public void Add(Characteristic stat, int amount = 1)
    {
        if (CanAdd(stat, Math.Min(amount, CharacteristicPointsLeft)))
            _stats[(int)stat] += Math.Min(amount, CharacteristicPointsLeft);
    }

    public void Remove(Characteristic stat, int amount = 1)
    {
        if (CanRemove(stat, Math.Min(amount, _stats[(int)stat])))
            _stats[(int)stat] -= Math.Min(amount, _stats[(int)stat]);
    }

    public void Raise(Spell spell)
    {
        if (CanRaise(spell))
            _ranks[spell.Id] = Rank(spell) + 1;
    }

    public void Lower(Spell spell)
    {
        if (!CanLower(spell))
            return;
        if (Rank(spell) == 2)
            _ranks.Remove(spell.Id);
        else
            _ranks[spell.Id] = Rank(spell) - 1;
    }

    /// <summary>The draft as the hero would have it.</summary>
    public Hero Draft => Hero with
    {
        Stats = new Characteristics(_stats[0], _stats[1], _stats[2], _stats[3], _stats[4]),
        Ranks = new Dictionary<string, int>(_ranks, StringComparer.Ordinal),
    };

    /// <summary>Whether the draft differs from what is saved.</summary>
    public bool Changed => Draft != Hero;

    /// <summary>What the server receives.</summary>
    public Points ToPoints() => new(Draft.Stats, Draft.Ranks);
}
