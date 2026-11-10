using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// What a hero's AI counts a trap, movement taken, a pull and a bonus to its side for (sprint 211, D99, docs/jalon-2.md Q6): with their flat worths of before,
/// the Wrench, the Iron Ring, the Rune, the Jaws and the Clan Cry were never played. A monster keeps them.
/// </summary>
public class DeferredAiTests
{
    private static readonly Spell Hold = new("hold", Text, 2, 1, 9, false, false, 0, 0, 1, Effects: [new StatusEffect(Affects.Enemies, Stat.Mp, -2, 1)]);
    private static readonly Spell Trap = new("trap", Text, 2, 1, 9, false, false, 0, 0, 1, Effects: [new GlyphEffect(Affects.Enemies, 2, 10, 10)]);
    private static readonly Spell Hook = new("hook", Text, 2, 1, 9, false, false, 0, 0, 1, Effects: [new PullEffect(Affects.Enemies, 2)]);
    private static readonly Spell Cheer = new("cheer", Text, 2, 0, 0, false, false, 0, 0, 1, Effects: [new StatusEffect(Affects.Allies, Stat.Damage, 20, 2)]);

    /// <summary>
    /// A hero of the real data against the training's Orc (its Club, from next to its target, 3 movement points), the Orc set <paramref name="cells"/> away in
    /// a straight line of free cells, the other monsters far off.
    /// </summary>
    private static (Fight Fight, Fighter Hero, Fighter Orc) Training(string cls, int cells)
    {
        var fight = new Fight(Real, "training", 1, Progression.Built(new Hero("Ondine", "female-a", cls, Level: 35), Real));
        Fighter hero = fight.HeroFighter!;
        // The training's other monsters out of the way, off the board.
        Fighter[] monsters = [.. fight.Fighters.Where(f => f.Team != hero.Team)];
        Fighter orc = monsters[0];
        for (int i = 0; i < monsters.Length; i++)
            monsters[i].Cell = new Cell(-100 - (10 * i), -100);
        Cell away = orc.Cell;
        Cell step = Cell.Steps.First(d => Enumerable.Range(1, cells).All(i => fight.IsFree(hero.Cell + new Cell(d.X * i, d.Y * i))));
        orc.Cell = hero.Cell + new Cell(step.X * cells, step.Y * cells);
        Assert.NotEqual(away, orc.Cell);
        return (fight, hero, orc);
    }

    [Fact]
    public void MovementTaken_IsWorthTheBlowItKeepsFromTheHero_TheFlatWorthOtherwise()
    {
        // Four cells away, the Orc reaches the hero with its 3 movement points; with 1, it does not: its Club is kept from it.
        (Fight fight, Fighter hero, Fighter orc) = Training("mage", 4);
        double club = Fight.Expected(Real.Spells["club"].AverageDamage, orc, hero, Element.Earth);
        Assert.Equal(club, Ai.Value(fight, hero, Hold, hero.Cell, orc.Cell), 9);
        // Next to it, it strikes all the same: the flat 2 a point.
        (fight, hero, orc) = Training("mage", 1);
        Assert.Equal(4, Ai.Value(fight, hero, Hold, hero.Cell, orc.Cell));
        // A monster keeps its flat worth.
        Assert.Equal(4, Ai.Value(fight, orc, Hold, orc.Cell, hero.Cell), 9);
    }

    [Fact]
    public void ATrap_UnderAnEnemyWhoCanAlreadyStrike_IsNearlySure()
    {
        (Fight fight, Fighter hero, Fighter orc) = Training("mage", 1);
        double hit = Math.Min(Fight.Expected(10, hero, orc, Element.Neutral), orc.Hp);
        Assert.Equal(Ai.StayOdds * hit, Ai.Value(fight, hero, Trap, hero.Cell, orc.Cell), 9);
        // From afar, it walks off it as likely as not.
        (fight, hero, orc) = Training("mage", 5);
        Assert.Equal(Ai.GlyphOdds * hit, Ai.Value(fight, hero, Trap, hero.Cell, orc.Cell), 9);
    }

    [Fact]
    public void APull_IsWorthTheBlowItBringsWithinReach()
    {
        // The Warrior strikes from next to its target: pulled from 3 cells to 1, the Orc is in reach of its best blow.
        (Fight fight, Fighter hero, Fighter orc) = Training("warrior", 3);
        double best = hero.Spells.Where(s => s.DamageMax > 0 && s.MinRange <= 1 && hero.MaxRangeOf(s) == 1 && s.ApCost <= hero.Ap - Hook.ApCost)
            .Max(s => Math.Min(Fight.Expected(s.AverageDamage, hero, orc, s.Element), orc.Hp + orc.Shield));
        Assert.Equal(1 + best, Ai.Value(fight, hero, Hook, hero.Cell, orc.Cell), 9);
        // Already next to it, a pull brings nothing.
        (fight, hero, orc) = Training("warrior", 1);
        Assert.Equal(1, Ai.Value(fight, hero, Hook, hero.Cell, orc.Cell));
    }

    [Fact]
    public void ADamageBonus_ToTheHerosSide_IsAShareOfItsBlows()
    {
        (Fight fight, Fighter hero, Fighter orc) = Training("mage", 3);
        double flat = 0.05 * 20 * 2;
        double worth = Ai.Value(fight, hero, Cheer, hero.Cell, hero.Cell);
        Assert.True(worth > 3 * flat, $"{worth}");
        // A monster's own stays flat.
        Assert.Equal(flat, Ai.Value(fight, orc, Cheer, orc.Cell, orc.Cell), 9);
    }
}
