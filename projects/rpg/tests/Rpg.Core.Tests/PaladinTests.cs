using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Paladin (sprint 208, docs/classes-paladin.md): its ward on small boards, the Aegis taken from the Warrior, its kit and its Protector on the real data.
/// </summary>
public class PaladinTests
{
    private static readonly Spell Hit = new("hit", Text, 1, 1, 2, false, false, 20, 20, 9);
    private static readonly Spell[] All = [.. Spells, Hit];

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    private static FighterSpec Caster() => Spec(0, ap: 99, mp: 0, initiative: 999, spells: "hit") with { SpecRules = [new WardRule(30, 2)] };

    [Fact]
    public void ABlowFromNextToItsTarget_ShieldsTheMostWoundedOfItsSide_WithinTwoCells()
    {
        Fight f = Play(["BAA", "...", ".A."], Caster(), Spec(1, hp: 900, initiative: 1), Spec(0, 1, hp: 100, initiative: 0), Spec(0, 2, hp: 100, initiative: 0));
        Fighter me = f.Fighters[0];
        f.Fighters[2].Hp = 40;
        f.Fighters[3].Hp = 70;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("hit", f.Fighters[1].Cell)));
        // 20 dealt, 30 % of it: 6 on the one at 40 %.
        Assert.Equal((6, 0, 0), (f.Fighters[2].Shield, f.Fighters[3].Shield, me.Shield));
        Assert.Contains(f.Events.OfType<Shielded>(), e => e is { Fighter: 2, Amount: 6, Turns: Fight.WardTurns });
    }

    [Fact]
    public void ABlowFromAfar_ShieldsNobody()
    {
        Fight f = Play(["A.B"], Caster(), Spec(1, hp: 900, initiative: 1));
        f.Apply(new CastAction("hit", f.Fighters[1].Cell));
        Assert.Equal(0, f.Fighters[0].Shield);
        // Alone and next to its target, it shields itself.
        Fight near = Play(["AB"], Caster(), Spec(1, hp: 900, initiative: 1));
        near.Apply(new CastAction("hit", near.Fighters[1].Cell));
        Assert.Equal(6, near.Fighters[0].Shield);
    }

    [Fact]
    public void ThePaladinsSheet_AndItsKit_TheAegisTakenFromTheWarrior()
    {
        HeroClass p = Real.Class("paladin")!;
        Assert.Equal((80, 4, 8, 3, 10), (p.Hp, p.HpPerLevel, p.Ap, p.Mp, p.Initiative));
        Assert.Equal([new WardRule(30, 2)], p.Rules);
        Spell[] kit = [.. p.Spells.Select(id => Real.Spells[id])];
        Assert.Equal(24, kit.Length);
        Assert.Equal([1, 1, 1, 1, 1, .. Enumerable.Range(1, 19).Select(i => 5 * i)], kit.Select(s => s.Level));
        Assert.Contains("aegis", p.Spells);
        Assert.DoesNotContain("aegis", Real.Class("warrior")!.Spells);
        Assert.Contains("mass-taunt", Real.Class("warrior")!.Spells);
        Assert.Equal(90, Real.Spells["mass-taunt"].Level);
        // What sets it apart from the Priest (classes-conception.md §1.4): nothing past 2 cells but its spears, no heal but on a blow, and on itself.
        Assert.All(kit.Where(s => s.DamageMax == 0), s => Assert.InRange(s.MaxRange, 0, 2));
        Assert.All(kit.Where(s => s.AllEffects.Any(e => e is HealEffect)), s => Assert.True(s.DamageMax > 0 && s.AllEffects.OfType<HealEffect>().All(h => h.Affects == Affects.Caster), s.Id));
        // At 0.85 of the budget: the Hallowed Hammer against the Berserker's Cleaver (1.1), 4 action points from next to the target: 0.77.
        static double Average(string id) => (Real.Spells[id].DamageMin + Real.Spells[id].DamageMax) / 2.0;
        Assert.InRange(Average("holy-hammer") / Average("cleaver"), 0.72, 0.82);
    }

    private static Hero Paladin(string? spec, string? fork = null, int level = 60, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "paladin", Level: level, Stats: new Characteristics(Earth: 90), Spec: spec, Fork: fork, Final: final, Deck: deck.Length == 0 ? null : deck);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    [Fact]
    public void TheProtector_ItsLineagesAndFinals()
    {
        Specialisation protector = Real.Specs["protector"];
        Assert.Equal(["paladin-rampart", "defender"], protector.Forks.Select(f => f.Id));
        // Its ward 50 % in all: the class's 30 and the branch's 20.
        Hero bare = Paladin("protector", level: 40);
        Assert.Equal(50, Fight.Ward(new Fight(Real, "training", 1, bare).HeroFighter!)!.Percent);
        Assert.Equal(new Area(AreaShape.Circle, 2), Compiled(Paladin("protector", "paladin-rampart", 100, "holy-bastion"), "path-wall-of-faith").Area);
        Assert.Contains("spec-not-today", Specs.PassivesOf(Paladin("protector", "paladin-rampart", 100, "eternal-guardian"), Real));
        Assert.Equal(2, Compiled(Paladin("protector", "defender", 100, "paladin-aegis"), "path-shield-of-faith").PerTurn);
        Assert.Equal(15, Specs.RulesOf(Paladin("protector", "defender", 100, "protector-champion"), Real).OfType<StillRule>().Single().Still);
        Assert.All(protector.Forks, f => Assert.NotEqual(f.Finals![0].Template, f.Finals[1].Template));
    }
}
