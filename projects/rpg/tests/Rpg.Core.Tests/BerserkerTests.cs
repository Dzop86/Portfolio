using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Berserker (sprint 206, docs/classes-berserker.md): blood (spells paid in hit points, its fury, life drawn) and its Rage on small boards; then its
/// kit and its Ravager on the real data.
/// </summary>
public class BerserkerTests
{
    // Fixed numbers where a test counts hit points.
    private static readonly Spell Pay = new("pay", Text, 1, 1, 1, false, false, 20, 20, 9, HpCost: 10);
    private static readonly Spell Hit = new("hit", Text, 1, 1, 1, false, false, 20, 20, 9);
    private static readonly Spell Drink = new("drink", Text, 1, 1, 1, false, false, 20, 20, 9, Element.Neutral, null, [new DrainEffect(Affects.Caster, 50)]);
    private static readonly Spell Rage = new("rage", Text, 1, 0, 0, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Caster, Stat.Rage, 25, 99)], HpCost: 5);
    private static readonly Spell Fury = new("fury", Text, 1, 1, 1, false, false, 20, 20, 9, Form: true);
    private static readonly Spell Purge = new("purge", Text, 1, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new PurgeEffect(Affects.Enemies)]);
    private static readonly Spell[] All = [.. Spells, Pay, Hit, Drink, Rage, Fury, Purge];

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    private static FighterSpec Caster(IReadOnlyList<TalentRule>? rules = null, int hp = 100, params string[] spells) =>
        Spec(0, hp: hp, ap: 99, mp: 0, initiative: 999, spells: spells) with { SpecRules = rules };

    // ---- Blood (§1) ----

    [Fact]
    public void ASpellPaidInHitPoints_CostsThemFirst_NeverTheLastOne()
    {
        Fight f = Play(["AB"], Caster(hp: 25, spells: ["pay"]), Spec(1, hp: 900, initiative: 1));
        Fighter me = f.Fighters[0];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("pay", f.Fighters[1].Cell)));
        Assert.Equal(15, me.Hp);
        Assert.Contains(f.Events.OfType<HpPaid>(), e => e is { Fighter: 0, Amount: 10, Hp: 15 });
        f.Apply(new CastAction("pay", f.Fighters[1].Cell));
        Assert.Equal(5, me.Hp);
        Assert.Equal(ActionError.NotEnoughHp, f.Apply(new CastAction("pay", f.Fighters[1].Cell)));
        Assert.Equal(5, me.Hp);
    }

    [Fact]
    public void TheFury_StrikesHarder_TheMoreHitPointsAreMissing()
    {
        // Half the hit points missing: half of 50 %, 25 % more; at the most, 40 %.
        Fight f = Play(["AB"], Caster([new FuryRule(50, 40)], spells: ["hit"]), Spec(1, hp: 900, initiative: 1));
        Fighter me = f.Fighters[0], target = f.Fighters[1];
        f.Apply(new CastAction("hit", target.Cell));
        Assert.Equal(880, target.Hp);
        me.Hp = me.MaxHp / 2;
        f.Apply(new CastAction("hit", target.Cell));
        Assert.Equal(855, target.Hp);
        me.Hp = 1;
        f.Apply(new CastAction("hit", target.Cell));
        Assert.Equal(827, target.Hp);
    }

    [Fact]
    public void LifeDrawn_IsAShareOfTheDamageDone_UpToWhatIsMissing()
    {
        Fight f = Play(["AB"], Caster(hp: 100, spells: ["drink"]), Spec(1, hp: 900, initiative: 1));
        Fighter me = f.Fighters[0];
        me.Hp = 50;
        f.Apply(new CastAction("drink", f.Fighters[1].Cell));
        Assert.Equal(60, me.Hp);
        Assert.Contains(f.Events.OfType<Healed>(), e => e is { Fighter: 0, Amount: 10 });
    }

    [Fact]
    public void ABlowAllAroundItsCaster_SparesIt_ASacrificeStillHurts()
    {
        Spell around = new("around", Text, 1, 0, 0, false, false, 20, 20, 9, Element.Neutral, new Area(AreaShape.Circle, 1));
        var data = new GameData([.. All, around], [new MapSpec("m", Text, [".B.", "BA."])], [new Scenario("s", Text, "m",
            [Caster(spells: ["around", "sacrifice"]), Spec(1, hp: 900, initiative: 1), Spec(1, 1, hp: 900, initiative: 0)])]);
        Fight f = new(data, "s", 1);
        Fighter me = f.Fighters[0];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("around", me.Cell)));
        // The two enemies around take 20 each, the caster nothing.
        Assert.Equal((880, 880, 100), (f.Fighters[1].Hp, f.Fighters[2].Hp, me.Hp));
        Assert.DoesNotContain(f.Foresee(around, me.Cell), fc => fc.Fighter == me && fc.DamageMax > 0);
        f.Apply(new CastAction("sacrifice", me.Cell));
        Assert.Equal(0, me.Hp);
    }

    // ---- The Rage (§2) ----

    [Fact]
    public void TheRage_OpensTheSpellsOfTheForm_StrikesHarder_AndNoPurgeTakesIt()
    {
        Fight f = Play(["A.B"], Caster(spells: ["rage", "fury"]), Spec(1, hp: 900, initiative: 1, spells: ["purge"]));
        Fighter me = f.Fighters[0];
        Assert.Equal(ActionError.NoForm, f.CheckCast(me, Fury, me.Cell, f.Fighters[1].Cell));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("rage", me.Cell)));
        Assert.True(me.Raging);
        Assert.Equal(25, me.DamageBonusFor(Element.Fire));
        f.Apply(new EndTurnAction());
        Assert.Equal(ActionError.None, f.Apply(new CastAction("purge", me.Cell)));
        Assert.True(me.Raging);
        Assert.False(Fight.Ailment(new Status(Stat.Rage, 25, 99, Element.Neutral, 0)));
    }

    // ---- The kit (§3) and the Ravager (§4) ----

    [Fact]
    public void TheBerserkersSheet_ItsFury_AndItsTwentyFourSpells()
    {
        HeroClass b = Real.Class("berserker")!;
        Assert.Equal((90, 5, 8, 5, 20), (b.Hp, b.HpPerLevel, b.Ap, b.Mp, b.Initiative));
        Assert.Equal([new FuryRule(50, 40)], b.Rules);
        Spell[] kit = [.. b.Spells.Select(id => Real.Spells[id])];
        Assert.Equal(24, kit.Length);
        Assert.Equal([1, 1, 1, 1, 1, .. Enumerable.Range(1, 19).Select(i => 5 * i)], kit.Select(s => s.Level));
        // What sets it apart from the Warrior (classes-conception.md §1.4): no shield, no pull, no movement taken from an enemy.
        Assert.DoesNotContain(kit.SelectMany(s => s.AllEffects), e => e is ShieldEffect or PullEffect || e is StatusEffect { Affects: Affects.Enemies, Stat: Stat.Mp });
        Assert.Contains(kit, s => s.HpCost > 0);
        Assert.Equal(["blood-fury", "war-whirl", "blood-tide", "earthshaker", "tempest-blow"], kit.Where(s => s.Form).Select(s => s.Id));
        Assert.Contains(Real.Spells["rage"].AllEffects, e => e is StatusEffect { Stat: Stat.Rage, Affects: Affects.Caster });
        // A spell paid in hit points hits harder than the budget of its cost alone.
        static double Average(string id) => (Real.Spells[id].DamageMin + Real.Spells[id].DamageMax) / 2.0;
        Assert.True(Average("blood-axe") > Average("tide-blow"));
    }

    private static Hero Berserker(string? spec, string? fork = null, int level = 60, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "berserker", Level: level, Stats: new Characteristics(Earth: 90), Spec: spec, Fork: fork, Final: final, Deck: deck.Length == 0 ? null : deck);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    [Fact]
    public void TheRavager_ItsLineagesAndFinals()
    {
        Specialisation ravager = Real.Specs["ravager"];
        Assert.Equal(["ravager", "barbarian", "berserker-gladiator"], Real.Specs.Values.Where(s => s.Class == "berserker").Select(s => s.Id));
        Assert.Equal(["skullcracker", "carnivore"], ravager.Forks.Select(f => f.Id));
        Assert.Equal(new FuryRule(25, 20), Specs.RulesOf(Berserker("ravager", level: 40), Real).OfType<FuryRule>().Single());
        Assert.All(ravager.SpellsWith(null).Concat(ravager.Forks.SelectMany(f => ravager.SpellsWith(f.Id))).Distinct().Where(id => id != "path-devour"), id => Assert.True(Real.Spells[id].HpCost > 0, id));
        Assert.Equal(new Area(AreaShape.Circle, 2), Compiled(Berserker("ravager", "skullcracker", 100, "ravager-destroyer"), "path-ravage").Area);
        Assert.Contains("spec-on-the-ropes", Specs.PassivesOf(Berserker("ravager", "skullcracker", 100, "brutal-titan"), Real));
        Assert.Contains("spec-bounty", Specs.PassivesOf(Berserker("ravager", "carnivore", 100, "butcher"), Real));
        Assert.Equal(0, Compiled(Berserker("ravager", "carnivore", 100, "devourer"), "path-devour").Cooldown);
        Assert.All(ravager.Forks, f => Assert.NotEqual(f.Finals![0].Template, f.Finals[1].Template));
    }
}
