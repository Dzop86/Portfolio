using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Necromancer (sprint 214, docs/classes-necromancien.md): the corpses, the spells that take them and the AI that aims at them on small boards;
/// its kit, its risen dead and its Master of the Dead on the real data.
/// </summary>
public class NecromancerTests
{
    private static readonly Spell Claw = new("claw", Text, 3, 1, 1, true, false, 10, 10, 9);
    private static readonly Spell Rise = new("rise", Text, 1, 1, 9, false, false, 0, 0, 9, Effects: [new SummonEffect(Affects.All, "dead", Max: 1)], Corpse: true);
    private static readonly Spell Blast = new("blast", Text, 1, 1, 9, false, false, 20, 20, 9, Area: new Area(AreaShape.Circle, 1), Corpse: true);
    private static readonly Spell Call = new("call", Text, 1, 1, 1, false, false, 0, 0, 9, Effects: [new SummonEffect(Affects.All, "pet", Max: 1)]);
    private static readonly SummonSpec Dead = new("dead", Text, "zombie-tank", 40, 6, 3, 50, ["claw"], WearsCorpse: true);
    private static readonly SummonSpec Pet = new("pet", Text, "wolf-melee", 40, 6, 3, 50, ["claw"]);

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData([.. Spells, Claw, Rise, Blast, Call], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], summons: [Dead, Pet]), "s", 1);

    private static FighterSpec Raiser() => Spec(0, ap: 99, mp: 0, hp: 500, initiative: 999, spells: ["rise", "blast", "sacrifice", "call", "lob"]);

    private static FighterSpec Foe(int start, int hp = 50) => Spec(1, start, hp: hp, mp: 0, initiative: 1) with { Look = "orc" };

    /// <summary>Fells <paramref name="f"/> with a hit of 100 from next to it.</summary>
    private static void Fell(Fight fight, Fighter f)
    {
        Fighter me = fight.Current;
        me.CastsThisTurn.Clear();
        f.Hp = 1;
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("lob", f.Cell)));
        Assert.False(f.IsAlive);
    }

    [Fact]
    public void AFallenMonster_LeavesACorpse_ACreatureNone()
    {
        Fight f = Play(["A.B.B"], Raiser(), Foe(0), Foe(1));
        Assert.Empty(f.Corpses);
        Fighter foe = f.Fighters[1];
        Fell(f, foe);
        Assert.Equal([foe], f.Corpses);
        Assert.Same(foe, f.CorpseAt(f.Fighters[0], foe.Cell));
        // A creature leaves none.
        Assert.Equal(ActionError.None, f.Apply(new CastAction("call", new Cell(1, 0))));
        Fighter pet = f.Fighters[^1];
        Assert.False(Fight.LeavesCorpse(pet));
    }

    [Fact]
    public void AHero_LeavesNoCorpse_ItIsNotDead()
    {
        // docs/classes-canon.md: a fallen hero is raised by the Cleric, never by the Necromancer.
        Fight f = new(Real, "training", 1, new Hero("Ondine", "female-c", "necromancer"));
        Assert.False(Fight.LeavesCorpse(f.HeroFighter!));
        Assert.All(f.Fighters.Where(x => x.Team == 1), x => Assert.True(Fight.LeavesCorpse(x)));
    }

    [Fact]
    public void ASpellThatTakesACorpse_AsksForAnEnemysOnItsCell_AndUsesItUp()
    {
        Fight f = Play(["A.B.B"], Raiser(), Foe(0), Foe(1));
        Fighter me = f.Fighters[0];
        Fighter foe = f.Fighters[1];
        Assert.Equal(ActionError.NoCorpse, f.Check(new CastAction("blast", foe.Cell)));
        Fell(f, foe);
        Assert.Equal(ActionError.NoCorpse, f.Check(new CastAction("blast", new Cell(1, 0))));
        Assert.Equal(ActionError.None, f.Check(new CastAction("blast", foe.Cell)));
        f.Apply(new CastAction("blast", foe.Cell));
        Assert.Contains(new CorpseTaken(foe.Id, foe.Cell), f.Events);
        Assert.Empty(f.Corpses);
        me.Cooldowns.Clear();
        me.CastsThisTurn.Clear();
        Assert.Equal(ActionError.NoCorpse, f.Check(new CastAction("blast", foe.Cell)));
    }

    [Fact]
    public void ACorpseOfOnesOwnSide_IsNotTaken()
    {
        // A monster of the raiser's side falls: its corpse is there, but the raiser takes only its enemies'.
        Fight f = Play(["AAB"], Raiser(), Spec(0, 1, hp: 5, mp: 0, initiative: 2), Foe(0, hp: 900));
        Fighter friend = f.Fighters[1];
        Fell(f, friend);
        Assert.Contains(friend, f.Corpses);
        Assert.Null(f.CorpseAt(f.Fighters[0], friend.Cell));
        Assert.Equal(ActionError.NoCorpse, f.Check(new CastAction("rise", friend.Cell)));
    }

    [Fact]
    public void TheDeadRises_OnTheCorpse_AndWearsIt()
    {
        Fight f = Play(["A.B.B.B"], Raiser(), Foe(0), Foe(1), Foe(2, hp: 900));
        Fighter foe = f.Fighters[1];
        Fell(f, foe);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("rise", foe.Cell)));
        Fighter risen = f.Fighters[^1];
        Assert.Equal(("dead", foe.Cell, "orc", 0), (risen.SummonKind, risen.Cell, risen.Spec.Look, risen.Team));
        // One at a time.
        Fighter other = f.Fighters[2];
        Fell(f, other);
        Assert.Equal(ActionError.TooManySummons, f.Check(new CastAction("rise", other.Cell)));
    }

    [Fact]
    public void ACorpseUnderALivingFighter_BlowsUp_ButRaisesNoDead()
    {
        Fight f = Play(["A.B.B"], Raiser(), Foe(0), Foe(1, hp: 900));
        Fighter foe = f.Fighters[1];
        Fell(f, foe);
        Fighter standing = f.Fighters[2];
        standing.Cell = foe.Cell;
        Assert.Equal(ActionError.Occupied, f.Check(new CastAction("rise", foe.Cell)));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("blast", foe.Cell)));
        Assert.Equal(880, standing.Hp);
    }

    [Fact]
    public void TheAI_AimsAtACorpse_ToRaiseItsDead()
    {
        // The corpse 3 cells away, nothing else to do but raise.
        Fight f = Play(["A..B..B"], Spec(0, ap: 6, mp: 0, hp: 500, initiative: 999, spells: ["rise", "lob"]), Foe(0), Foe(1, hp: 900));
        Fighter foe = f.Fighters[1];
        foe.Hp = 0;
        CastAction cast = Assert.IsType<CastAction>(Ai.Decide(f));
        Assert.Equal(("rise", foe.Cell), (cast.Spell, cast.Target));
    }

    [Fact]
    public void TheAI_WeighsACorpseBlownUp_ByTheEnemiesAroundIt()
    {
        Fight f = Play(["A..BB.."], Raiser(), Foe(0), Foe(1, hp: 900));
        Fighter me = f.Fighters[0];
        Fighter foe = f.Fighters[1];
        foe.Hp = 0;
        // The living enemy next to the corpse takes the blow.
        Assert.True(Ai.Value(f, me, Blast, me.Cell, foe.Cell) >= 20);
        f.Fighters[2].Cell = new Cell(6, 0);
        Assert.Equal(0, Ai.Value(f, me, Blast, me.Cell, foe.Cell));
    }

    private static Hero Necromancer(int level, params string[] deck) =>
        new("Ondine", "female-c", "necromancer", Level: level, Stats: new Characteristics(Earth: 100), Deck: deck.Length == 0 ? null : deck);

    private static Hero Master(string? fork, int level, string? final = null, params string[] deck) =>
        Necromancer(level, deck) with { Spec = "dead-master", Fork = fork, Final = final };

    /// <summary>A training fight on the real data, the hero to play with all the points it needs and a corpse two cells from it.</summary>
    private static (Fight Fight, Fighter Corpse) Training(Hero hero)
    {
        Fight f = new(Real, "training", 1, hero);
        while (f.Current != f.HeroFighter)
            f.Apply(new EndTurnAction());
        Fighter me = f.HeroFighter!;
        me.Ap = 99;
        Fighter fallen = f.Fighters.First(x => x.Team == 1);
        fallen.Hp = 0;
        fallen.Cell = me.Cell.Neighbours().SelectMany(c => c.Neighbours()).First(c => c.DistanceTo(me.Cell) == 2 && f.IsFree(c));
        return (f, fallen);
    }

    [Fact]
    public void ItsDead_IsFedByItsHitPointsAndCharacteristics_AndStrikesInItsElement()
    {
        (Fight f, Fighter corpse) = Training(Necromancer(30, "raise-dead"));
        Fighter me = f.HeroFighter!;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("raise-dead", corpse.Cell)));
        Fighter risen = f.Fighters[^1];
        Assert.Equal(("risen", me.MaxHp * 35 / 100, me.Spec.Characteristics.Earth * 45 / 100), (risen.SummonKind, risen.MaxHp, risen.Spec.Characteristics.Earth));
        Assert.Equal(Element.Earth, risen.Spells.Single().Element);
        // It wears the monster it rose from.
        Assert.Equal(corpse.Spec.Look, risen.Spec.Look);
    }

    [Fact]
    public void TheFeast_EatsACorpse_ToHealItsCaster()
    {
        (Fight f, Fighter corpse) = Training(Necromancer(30, "feast"));
        Fighter me = f.HeroFighter!;
        me.Hp = me.MaxHp / 2;
        int before = me.Hp;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("feast", corpse.Cell)));
        Assert.InRange(me.Hp - before, 18, 30);
        Assert.Empty(f.Corpses);
    }

    [Fact]
    public void TheNecromancersSheet_AndItsKit()
    {
        HeroClass n = Real.Class("necromancer")!;
        Assert.Equal((60, 3, 8, 4, 10), (n.Hp, n.HpPerLevel, n.Ap, n.Mp, n.Initiative));
        Assert.Equal([new SummonRule("risen", 35, 45)], n.Rules);
        Spell[] kit = [.. n.Spells.Select(id => Real.Spells[id])];
        Assert.Equal(24, kit.Length);
        Assert.Equal([1, 1, 1, 1, 1, .. Enumerable.Range(1, 19).Select(i => 5 * i)], kit.Select(k => k.Level));
        // From level 1, it raises one dead on a corpse; four spells take one: raise, blow up, eat, wear.
        Spell raise = Real.Spells["raise-dead"];
        Assert.Equal((1, true, "risen", 1), (raise.Level, raise.Corpse, raise.AllEffects.OfType<SummonEffect>().Single().Summon, raise.AllEffects.OfType<SummonEffect>().Single().Max));
        Assert.Equal(["raise-dead", "corpse-blast", "feast", "bone-armor"], kit.Where(k => k.Corpse).Select(k => k.Id));
        Assert.True(Real.Summons["risen"].WearsCorpse);
        // Its blows from 1 to 6 cells, poisons among them; at 0.85 of the budget: the Grave Dust against the Mage's Pebble (1).
        Assert.All(kit.Where(k => k.DamageMax > 0), k => Assert.InRange(k.MaxRange, 4, 6));
        Assert.Equal(3, kit.Count(k => k.AllEffects.Any(e => e is StatusEffect { Stat: Stat.Poison })));
        static double Average(string id) => (Real.Spells[id].DamageMin + Real.Spells[id].DamageMax) / 2.0;
        Assert.InRange(Average("grave-dust") / Average("pebble"), 0.78, 0.92);
        // A corpse is worth half a blow more: the Detonation above the Grave Dust, an area at the same cost.
        Assert.True(Average("corpse-blast") > Average("grave-dust"));
        Assert.All(Real.Summons["risen"].Spells, id => Assert.True(Real.Spells[id].Adaptive, id));
    }

    private static IEnumerable<string> PathSpells(Specialisation s) => (s.Base.Spells ?? []).Concat(s.Forks.SelectMany(f => f.Tiers.SelectMany(t => t.Spells ?? [])));

    [Fact]
    public void WhatSetsTheNecromancerApart_FromTheSummoner()
    {
        // docs/classes-conception.md §1.4: the Necromancer's dead asks for a corpse; no spell of the Summoner reads one.
        string[] necromancer = [.. Real.Class("necromancer")!.Spells, .. Real.Specs.Values.Where(s => s.Class == "necromancer").SelectMany(PathSpells)];
        Assert.All(necromancer.Select(id => Real.Spells[id]).Where(s => s.AllEffects.Any(e => e is SummonEffect)), s => Assert.True(s.Corpse, s.Id));
        string[] summoner = [.. Real.Class("summoner")!.Spells, .. Real.Specs.Values.Where(s => s.Class == "summoner").SelectMany(PathSpells)];
        Assert.DoesNotContain(summoner, id => Real.Spells[id].Corpse);
        // No other class's spell, nor any monster's, takes a corpse.
        Assert.All(Real.Spells.Values.Where(s => s.Corpse), s => Assert.Contains(s.Id, necromancer));
    }

    [Fact]
    public void TheMasterOfTheDead_ItsLineagesAndFinals()
    {
        Specialisation spec = Real.Specs["dead-master"];
        Assert.Equal("necromancer", spec.Class);
        Assert.Equal(["path-stitch", "path-bone-rampart"], spec.Base.Spells);
        Assert.Equal(["lich", "bone-lord"], spec.Forks.Select(x => x.Id));
        TalentRule[] rules = [.. Specs.RulesOf(Master(null, 40), Real)];
        Assert.Contains(new SummonRule("risen", 45, 55), rules);
        Assert.Contains(new GrowthRule(5, 25, "risen"), rules);
        // The Bone Lord's growth adds to the branch's: 15 a turn, 50 at most; its Titan's, 20 and 75.
        GrowthRule[] lord = [.. Specs.RulesOf(Master("bone-lord", 60), Real).OfType<GrowthRule>()];
        Assert.Equal((15, 50), (lord.Sum(r => r.Percent), lord.Sum(r => r.Max)));
        GrowthRule[] titan = [.. Specs.RulesOf(Master("bone-lord", 100, "bone-titan"), Real).OfType<GrowthRule>()];
        Assert.Equal((20, 75), (titan.Sum(r => r.Percent), titan.Sum(r => r.Max)));
        Assert.Equal(1, Compiled(Master("bone-lord", 100, "undead-colossus"), "path-marrow").Cooldown);
        Assert.Contains(Compiled(Master("lich", 100, "archlich"), "path-unlife").AllEffects, e => e is StatusEffect { Stat: Stat.Rage, Turns: 99 });
        Assert.Contains(Compiled(Master("lich", 100, "damned-king"), "path-unlife").AllEffects, e => e is StatusEffect { Stat: Stat.Ap, Value: 2, Affects: Affects.Caster });
        Assert.All(spec.Forks, x => Assert.NotEqual(x.Finals![0].Template, x.Finals[1].Template));
        // Stitching, Unlife and Marrow aim at the dead; the Bone Rampart takes a corpse.
        Assert.Equal(("risen", "risen", "risen", true), (Real.Spells["path-stitch"].Aims, Real.Spells["path-unlife"].Aims, Real.Spells["path-marrow"].Aims, Real.Spells["path-bone-rampart"].Corpse));
    }

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    [Fact]
    public void TheMastersDead_IsFedMore_AndGrowsInARealFight()
    {
        (Fight f, Fighter corpse) = Training(Master("bone-lord", 60, null, "raise-dead"));
        Fighter me = f.HeroFighter!;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("raise-dead", corpse.Cell)));
        Fighter risen = f.Fighters[^1];
        Assert.Equal(me.MaxHp * 45 / 100, risen.MaxHp);
        f.Apply(new EndTurnAction());
        Assert.Same(risen, f.Current);
        Assert.Equal(15, risen.Statuses.Where(s => s.Stat == Stat.Grown).Sum(s => s.Value));
    }

    [Fact]
    public void TheBoneRampart_MakesACorpseIntoShields_ForTheAlliesAroundIt()
    {
        (Fight f, Fighter corpse) = Training(Master(null, 40, null, "path-bone-rampart"));
        Fighter me = f.HeroFighter!;
        // The hero two cells from the corpse is out of its circle; one cell from it, in.
        Cell near = corpse.Cell.Neighbours().First(c => c.DistanceTo(me.Cell) == 1 && f.IsFree(c));
        me.Cell = near;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-bone-rampart", corpse.Cell)));
        Assert.Equal(20, me.Shield);
        Assert.Empty(f.Corpses);
    }

    [Fact]
    public void TheLich_TakesItsDeadIn()
    {
        (Fight f, Fighter corpse) = Training(Master("lich", 60, null, "raise-dead", "path-unlife"));
        Fighter me = f.HeroFighter!;
        f.Apply(new CastAction("raise-dead", corpse.Cell));
        Fighter risen = f.Fighters[^1];
        int left = risen.Hp;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-unlife", risen.Cell)));
        Assert.False(risen.IsAlive);
        Assert.Equal(left, me.Shield);
        Assert.Contains(me.Statuses, s => s is { Stat: Stat.Damage, Value: 30 });
        // A dead taken in leaves no corpse: it was a creature.
        Assert.Empty(f.Corpses);
    }
}
