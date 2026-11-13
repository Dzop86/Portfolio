using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Necromancer's other two branches (sprint 215, docs/classes-necromancien-2.md): the Macabre Caller and its Ghouls, three at most, each on a
/// corpse, the Offering that spares the Ghoul it is cast on; the Occultist, who raises nothing and whose poisons stack. On the real data.
/// </summary>
public class NecromancerBranchesTests
{
    private static Hero Necromancer(string spec, string? fork, int level, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "necromancer", Level: level, Stats: new Characteristics(Earth: 100), Deck: deck.Length == 0 ? null : deck) { Spec = spec, Fork = fork, Final = final };

    /// <summary>A training fight, the hero to play with all the points it needs, one monster fallen two cells from it, the other standing.</summary>
    private static (Fight Fight, Fighter Corpse, Fighter Standing) Training(Hero hero)
    {
        Fight f = new(Real, "training", 1, hero);
        while (f.Current != f.HeroFighter)
            f.Apply(new EndTurnAction());
        Fighter me = f.HeroFighter!;
        me.Ap = 99;
        Fighter[] foes = [.. f.Fighters.Where(x => x.Team == 1)];
        foes[0].Hp = 0;
        foes[0].Cell = me.Cell.Neighbours().SelectMany(c => c.Neighbours()).First(c => c.DistanceTo(me.Cell) == 2 && f.IsFree(c));
        return (f, foes[0], foes[1]);
    }

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    [Fact]
    public void TheMacabreCaller_ItsLineagesAndFinals()
    {
        Specialisation spec = Real.Specs["macabre-caller"];
        Assert.Equal("necromancer", spec.Class);
        Assert.Equal(["path-ghoul", "path-quarry"], spec.Base.Spells);
        Assert.Equal(["ghoul-master", "dead-commander"], spec.Forks.Select(x => x.Id));
        // Three Ghouls at most, each on a corpse: many dead, frail and thrown away.
        SummonEffect call = Real.Spells["path-ghoul"].AllEffects.OfType<SummonEffect>().Single();
        Assert.Equal(("ghoul", 3, true), (call.Summon, call.Max, Real.Spells["path-ghoul"].Corpse));
        Assert.True(Real.Summons["ghoul"].WearsCorpse);
        Assert.Contains(new SummonRule("ghoul", 12, 30), Specs.RulesOf(Necromancer("macabre-caller", null, 40), Real));
        Assert.Contains(new SummonRule("ghoul", 18, 40), Specs.RulesOf(Necromancer("macabre-caller", "ghoul-master", 60), Real));
        Assert.Equal(1, Compiled(Necromancer("macabre-caller", "ghoul-master", 100, "ghoul-lord"), "path-ghoul").Cooldown);
        Assert.Contains(Compiled(Necromancer("macabre-caller", "ghoul-master", 100, "soul-devourer"), "path-gorging").AllEffects, e => e is StatusEffect { Stat: Stat.Regen });
        Assert.Contains(Compiled(Necromancer("macabre-caller", "dead-commander", 100, "dead-general"), "path-offering").AllEffects, e => e is StatusEffect { Stat: Stat.Mp, Value: -1 });
        Assert.Equal(1, Compiled(Necromancer("macabre-caller", "dead-commander", 100, "doom-emperor"), "path-offering").Cooldown);
        Assert.All(spec.Forks, x => Assert.NotEqual(x.Finals![0].Template, x.Finals[1].Template));
        // The Gorging eats a corpse; the Offering aims at a Ghoul.
        Assert.Equal((true, "ghoul"), (Real.Spells["path-gorging"].Corpse, Real.Spells["path-offering"].Aims));
    }

    [Fact]
    public void AGhoul_RisesOnACorpse_FedByTheBranch_AndTheGhoulMastersMore()
    {
        foreach ((string? fork, int level, int hp) in new[] { ((string?)null, 40, 12), ("ghoul-master", 60, 18) })
        {
            (Fight f, Fighter corpse, _) = Training(Necromancer("macabre-caller", fork, level, null, "path-ghoul"));
            Fighter me = f.HeroFighter!;
            Assert.Equal(ActionError.NoCorpse, f.Check(new CastAction("path-ghoul", me.Cell.Neighbours().First(f.IsFree))));
            Assert.Equal(ActionError.None, f.Apply(new CastAction("path-ghoul", corpse.Cell)));
            Fighter ghoul = f.Fighters[^1];
            Assert.Equal(("ghoul", corpse.Spec.Look, me.MaxHp * hp / 100), (ghoul.SummonKind, ghoul.Spec.Look, ghoul.MaxHp));
            Assert.Empty(f.Corpses);
        }
    }

    [Fact]
    public void TheOffering_StrikesAroundItsGhoul_AndSparesIt()
    {
        (Fight f, Fighter corpse, Fighter standing) = Training(Necromancer("macabre-caller", "dead-commander", 60, null, "path-ghoul", "path-offering"));
        Fighter me = f.HeroFighter!;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-ghoul", corpse.Cell)));
        Fighter ghoul = f.Fighters[^1];
        standing.Cell = ghoul.Cell.Neighbours().First(c => f.IsFree(c) && c != me.Cell);
        (int ghoulHp, int foeHp) = (ghoul.Hp, standing.Hp);
        Assert.True(Fight.Spares(Real.Spells["path-offering"], me, ghoul.Cell, ghoul));
        Assert.False(Fight.Spares(Real.Spells["path-offering"], me, ghoul.Cell, standing));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-offering", ghoul.Cell)));
        Assert.Equal(ghoulHp, ghoul.Hp);
        Assert.True(standing.Hp < foeHp);
    }

    [Fact]
    public void TheAI_DoesNotCountTheGhoul_ItCastsTheOfferingOn()
    {
        (Fight f, Fighter corpse, Fighter standing) = Training(Necromancer("macabre-caller", "dead-commander", 60, null, "path-ghoul", "path-offering"));
        Fighter me = f.HeroFighter!;
        f.Apply(new CastAction("path-ghoul", corpse.Cell));
        Fighter ghoul = f.Fighters[^1];
        Spell offering = me.Spells.Single(s => s.Id == "path-offering");
        // No enemy around the Ghoul: the blow is worth nothing, its Ghoul does not count against it.
        standing.Cell = Enumerable.Range(0, 20).SelectMany(x => Enumerable.Range(0, 20).Select(y => new Cell(x, y)))
            .First(c => f.IsFree(c) && c.DistanceTo(ghoul.Cell) > 3);
        Assert.Equal(0, Ai.Value(f, me, offering, me.Cell, ghoul.Cell));
        standing.Cell = ghoul.Cell.Neighbours().First(c => f.IsFree(c) && c != me.Cell);
        Assert.True(Ai.Value(f, me, offering, me.Cell, ghoul.Cell) > 0);
    }

    [Fact]
    public void TheOccultist_RaisesNothing_AndItsPoisonsStack()
    {
        Specialisation spec = Real.Specs["occultist"];
        Assert.Equal(["path-hex", "path-blight"], spec.Base.Spells);
        Assert.Equal(["sanguimancer", "thanaturge"], spec.Forks.Select(x => x.Id));
        string[] spells = [.. spec.Base.Spells!, .. spec.Forks.SelectMany(x => x.Tiers.SelectMany(t => t.Spells ?? []))];
        Assert.DoesNotContain(spells, id => Real.Spells[id].AllEffects.Any(e => e is SummonEffect) || Real.Spells[id].Corpse || Real.Spells[id].Aims is not null);
        Assert.Equal(3, Fight.StackOf(new Fight(Real, "training", 1, Necromancer("occultist", null, 40)).HeroFighter!));
        Assert.Null(Fight.StackOf(new Fight(Real, "training", 1, Necromancer("dead-master", null, 40)).HeroFighter!));
        // Three Blights, three doses on the enemy, in one poison of its element; a fourth adds nothing.
        (Fight f, _, Fighter standing) = Training(Necromancer("occultist", null, 40, null, "path-blight"));
        Fighter me = f.HeroFighter!;
        standing.Cell = me.Cell.Neighbours().First(f.IsFree);
        int dose = Real.Spells["path-blight"].AllEffects.OfType<StatusEffect>().Single(e => e.Stat == Stat.Poison).Value;
        for (int i = 0; i < 4; i++)
        {
            me.Cooldowns.Clear();
            me.CastsThisTurn.Clear();
            standing.Hp = 999;
            Assert.Equal(ActionError.None, f.Apply(new CastAction("path-blight", standing.Cell)));
        }
        Status poison = Assert.Single(standing.Statuses, s => s.Stat == Stat.Poison);
        Assert.Equal(3 * dose, poison.Value);
    }

    [Fact]
    public void TheSanguimancer_PaysInBlood_AndTheThanaturgeExecutes()
    {
        Spell blood = Real.Spells["path-boiling-blood"];
        Assert.Equal((10, 2), (blood.HpCost, blood.Cooldown));
        Assert.Contains(blood.AllEffects, e => e is DrainEffect { Percent: 20 });
        Assert.Contains(new ExecuteRule(30, 25), Specs.RulesOf(Necromancer("occultist", "thanaturge", 60), Real));
        Assert.DoesNotContain(Specs.RulesOf(Necromancer("occultist", "sanguimancer", 60), Real), r => r is ExecuteRule);
        Assert.Contains(Compiled(Necromancer("occultist", "sanguimancer", 100, "blood-lord"), "path-boiling-blood").AllEffects, e => e is StatusEffect { Stat: Stat.Damage, Value: 8, Turns: 2 });
        Assert.Contains(Compiled(Necromancer("occultist", "sanguimancer", 100, "hemarch"), "path-boiling-blood").AllEffects, e => e is StatusEffect { Stat: Stat.Rage, Value: 8, Turns: 99 });
        Assert.Contains(Compiled(Necromancer("occultist", "thanaturge", 100, "soul-master"), "path-reaping").AllEffects, e => e is DrainEffect { Percent: 30 });
        Assert.Equal(1, Compiled(Necromancer("occultist", "thanaturge", 100, "eternal-reaper"), "path-reaping").Cooldown);
        // In a fight: 10 hit points paid, a share of the blow given back.
        (Fight f, _, Fighter standing) = Training(Necromancer("occultist", "sanguimancer", 60, null, "path-boiling-blood"));
        Fighter me = f.HeroFighter!;
        standing.Cell = me.Cell.Neighbours().First(f.IsFree);
        standing.Hp = 999;
        me.Hp = me.MaxHp / 2;
        int before = me.Hp;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-boiling-blood", standing.Cell)));
        Assert.Contains(f.Events, e => e is HpPaid { Amount: 10 });
        Assert.True(me.Hp > before - 10);
    }
}
