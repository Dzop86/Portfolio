using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// One line of the characteristics sheet: the points put in by the player, those of what the hero
/// wears, their total, and what the total gives: for an element, 2 more damage for every 10 points
/// (<see cref="Characteristics.Bonus"/>) of those that count (<see cref="Effective"/>, a quarter past 3 × the level + 5 since
/// sprint 162) and the resistance to it; for Vitality, hit points.
/// </summary>
public sealed record SheetLine(Characteristic Stat, Element? Element, int Invested, int Worn, int Total, int Bonus, int Resistance, int Effective);

/// <summary>
/// The characteristics sheet, without any engine (sprint 64): a hero, saved or a draft, as a fight
/// will count it (<see cref="Fight.HeroTotals"/>), with the points the level still gives. Since sprint 146 (D82, O7 a),
/// what the tree's trunk adds: its damage, its resistance (in each element's line) and its hit points (in the maximum).
/// </summary>
public sealed class CharacterSheet
{
    public CharacterSheet(Hero hero, GameData data)
    {
        Hero = hero ?? throw new ArgumentNullException(nameof(hero));
        ArgumentNullException.ThrowIfNull(data);
        HeroNumbers n = Fight.HeroTotals(hero, data) ?? throw new ArgumentException("A hero without a class has no sheet.", nameof(hero));
        Characteristics own = hero.Stats ?? Characteristics.None;
        Lines =
        [
            new(Characteristic.Vitality, null, own.Vitality, n.Worn.Vitality, n.Stats.Vitality, n.Stats.Vitality, 0, n.Stats.Vitality),
            Line(Characteristic.Earth, Element.Earth, own.Earth, n.Worn.Earth, hero.Level),
            Line(Characteristic.Fire, Element.Fire, own.Fire, n.Worn.Fire, hero.Level),
            Line(Characteristic.Water, Element.Water, own.Water, n.Worn.Water, hero.Level),
            Line(Characteristic.Air, Element.Air, own.Air, n.Worn.Air, hero.Level),
        ];
        // As a fight counts them: the talents' percent (Nomad) and the trunk's, of the maximum with Vitality.
        int hpPercent = (hero.Talents ?? []).Where(data.Talents.ContainsKey).SelectMany(id => data.Talents[id].Rules).OfType<HpRule>().Sum(r => r.Percent) + Talents.TrunkHp(hero.Level);
        MaxHp = (n.Hp + n.Stats.Vitality) * (100 + hpPercent) / 100;
        (TreeDamage, TreeResistance, TreeHp) = (Talents.TrunkDamage(hero.Level), Talents.TrunkResistance(hero.Level), Talents.TrunkHp(hero.Level));
        (Ap, Mp, Initiative) = (n.Ap, n.Mp, n.Initiative);
        PointsLeft = Progression.CharacteristicPoints(hero.Level) - (own.Vitality + own.Earth + own.Fire + own.Water + own.Air);
    }

    // Items give no resistance: the column says the trunk's (sprint 146), 0 before level 35.
    private static SheetLine Line(Characteristic stat, Element element, int invested, int worn, int level)
    {
        int effective = Characteristics.Effective(invested + worn, Characteristics.Threshold(level));
        return new(stat, element, invested, worn, invested + worn, Characteristics.Bonus(effective), Talents.TrunkResistance(level), effective);
    }

    public Hero Hero { get; }
    public IReadOnlyList<SheetLine> Lines { get; }
    public int MaxHp { get; }
    public int Ap { get; }
    public int Mp { get; }
    public int Initiative { get; }
    public int PointsLeft { get; }

    /// <summary>The trunk's damage in percent, on every spell (sprint 146): 0 before level 30.</summary>
    public int TreeDamage { get; }

    /// <summary>The trunk's resistance in percent, to each element: 0 before level 35.</summary>
    public int TreeResistance { get; }

    /// <summary>The trunk's hit points in percent of the maximum, counted in <see cref="MaxHp"/>: 0 before level 40.</summary>
    public int TreeHp { get; }

    /// <summary>The line of a characteristic.</summary>
    public SheetLine this[Characteristic stat] => Lines.Single(l => l.Stat == stat);
}
