using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>
/// The element a spell strikes in. Each of the four has its own characteristic (sprint 55) and
/// fighters resist them separately; a neutral spell ignores resistances.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Element>))]
public enum Element
{
    Neutral,
    Earth,
    Fire,
    Water,
    Air,
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

    /// <summary>Action points added (or removed) at the start of each turn.</summary>
    Ap,

    /// <summary>Movement points added (or removed) at the start of each turn.</summary>
    Mp,

    /// <summary>Damage the fighter deals, in percent.</summary>
    Damage,

    /// <summary>Resistance to the status's element (to every element when neutral), in percent.</summary>
    Resistance,
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
public abstract record SpellEffect(Affects Affects);

/// <summary>Gives back hit points, never above the maximum.</summary>
public sealed record HealEffect(Affects Affects, int Min, int Max) : SpellEffect(Affects);

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

/// <summary>A status on a fighter during a fight.</summary>
public sealed record Status(Stat Stat, int Value, int TurnsLeft, Element Element, int Source);
