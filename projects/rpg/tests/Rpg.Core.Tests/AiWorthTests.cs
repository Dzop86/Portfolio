using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>What a hero's AI counts a creature and a shield for (sprint 194, D96, T154): the flat values of before made it never cast them.</summary>
public class AiWorthTests
{
    private static Fight Training(string cls, int level) =>
        new(Real, "training", 1, Progression.Built(new Hero("Ondine", "female-a", cls, Level: level), Real));

    [Fact]
    public void AHerosCreature_IsWorthWhatItWouldStrike_AMonstersStaysAtItsFlatWorth()
    {
        Fight fight = Training("sentinel", 35);
        Fighter hero = fight.HeroFighter!;
        Fighter monster = fight.Fighters.First(f => f.Team != hero.Team);
        Spell hawk = Real.Spells["hawk"];
        Cell cell = hero.Cell.Neighbours().First(fight.IsFree);
        // The Peck: 6.5 twice a round, for two rounds.
        Assert.Equal(26, Ai.Value(fight, hero, hawk, hero.Cell, cell));
        Assert.Equal(8, Ai.Value(fight, monster, hawk, monster.Cell, cell));
    }

    [Fact]
    public void AHerosShield_IsWorthWhatTheBlowsInReachWouldTake_AtMostItsAmount()
    {
        Fight fight = Training("guard", 35);
        Fighter hero = fight.HeroFighter!;
        Spell spell = Real.Spells.Values.First(s => s.DamageMax == 0 && s.AllEffects.Count == 1 && s.AllEffects[0] is ShieldEffect { Affects: Affects.Caster });
        ShieldEffect shield = (ShieldEffect)spell.AllEffects[0];
        double incoming = Ai.Incoming(fight, hero);
        Assert.Equal(Math.Min(shield.Amount, incoming * shield.Turns), Ai.Value(fight, hero, spell, hero.Cell, hero.Cell), 6);
    }

    [Fact]
    public void WithNoBlowInReach_NothingIsComing()
    {
        Fight fight = Training("guard", 35);
        Fighter hero = fight.HeroFighter!;
        double all = Ai.Incoming(fight, hero);
        foreach (Fighter enemy in fight.Fighters.Where(f => f.Team != hero.Team))
            enemy.Cell = hero.Cell + new Cell(100, 100);
        Assert.True(all >= 0);
        Assert.Equal(0, Ai.Incoming(fight, hero));
    }

    [Fact]
    public void TheAmbush_NowCallsItsHawk_AndDives()
    {
        // Before: the Hawk 0.0 times a fight, so the Dive, which needs it, 0.0 too, and the line said nothing of either.
        BenchReport report = Bench.Run(new SyntheticFamily(35), "sentinel", 15, sample: 8);
        Assert.True(report.Paths.Single(p => p.Path == "ambush").Casts["path-dive"] >= BenchRow.LeastCasts);
    }

    [Fact]
    public void ASwapWithAnAlly_DoesNotMakeASoloLineNotConclusive()
    {
        BenchReport report = Bench.Run(new SyntheticFamily(35), "guard", 1, sample: 1);
        Assert.DoesNotContain("path-interpose", report.Paths.Single(p => p.Path == "palisade").Casts.Keys);
    }
}
