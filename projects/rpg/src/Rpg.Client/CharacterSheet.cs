using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// One line of the characteristics sheet: the points put in by the player, those of what the hero
/// wears, their total, and what the total gives: for an element, 2 more damage for every 10 points
/// (<see cref="Characteristics.Bonus"/>) and the resistance to it; for Vitality, hit points.
/// </summary>
public sealed record SheetLine(Characteristic Stat, Element? Element, int Invested, int Worn, int Total, int Bonus, int Resistance);

/// <summary>
/// The characteristics sheet, without any engine (sprint 64): a hero, saved or a draft, as a fight
/// will count it (<see cref="Fight.HeroTotals"/>), with the points the level still gives.
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
            new(Characteristic.Vitality, null, own.Vitality, n.Worn.Vitality, n.Stats.Vitality, n.Stats.Vitality, 0),
            Line(Characteristic.Strength, Element.Earth, own.Strength, n.Worn.Strength),
            Line(Characteristic.Intelligence, Element.Fire, own.Intelligence, n.Worn.Intelligence),
            Line(Characteristic.Chance, Element.Water, own.Chance, n.Worn.Chance),
            Line(Characteristic.Agility, Element.Air, own.Agility, n.Worn.Agility),
        ];
        MaxHp = n.Hp + n.Stats.Vitality;
        (Ap, Mp, Initiative) = (n.Ap, n.Mp, n.Initiative);
        PointsLeft = Progression.CharacteristicPoints(hero.Level) - (own.Vitality + own.Strength + own.Intelligence + own.Chance + own.Agility);
    }

    // Heroes have no resistance of their own yet, and items give none: the column says 0 until they do.
    private static SheetLine Line(Characteristic stat, Element element, int invested, int worn) =>
        new(stat, element, invested, worn, invested + worn, Characteristics.Bonus(invested + worn), 0);

    public Hero Hero { get; }
    public IReadOnlyList<SheetLine> Lines { get; }
    public int MaxHp { get; }
    public int Ap { get; }
    public int Mp { get; }
    public int Initiative { get; }
    public int PointsLeft { get; }

    /// <summary>The line of a characteristic.</summary>
    public SheetLine this[Characteristic stat] => Lines.Single(l => l.Stat == stat);
}
