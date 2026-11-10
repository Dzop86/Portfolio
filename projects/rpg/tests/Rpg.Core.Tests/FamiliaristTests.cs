using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Summoner's Familiarist (sprint 212, docs/classes-invocateur-2.md): a creature that grows and the fusion on small boards; the branch, its lineages and
/// finals on the real data.
/// </summary>
public class FamiliaristTests
{
    private static readonly Spell Fang = new("fang", Text, 3, 1, 1, true, false, 10, 10, 9);
    private static readonly Spell Call = new("call", Text, 1, 1, 1, false, false, 0, 0, 9, Effects: [new SummonEffect(Affects.All, "pet", Max: 2)]);
    private static readonly Spell Fuse = new("fuse", Text, 1, 1, 9, false, false, 0, 0, 9, Effects: [new FuseEffect(Affects.Caster, 3, 20)], Aims: "pet");
    private static readonly Spell Wash = new("wash", Text, 1, 1, 9, false, false, 0, 0, 9, Effects: [new PurgeEffect(Affects.Enemies)]);
    private static readonly SummonSpec Pet = new("pet", Text, "male-d", 50, 6, 0, 50, ["fang"]);

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData([.. Spells, Fang, Call, Fuse, Wash], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], summons: [Pet]), "s", 1);

    private static FighterSpec Caller(params TalentRule[] rules) =>
        Spec(0, ap: 99, mp: 0, hp: 500, initiative: 999, spells: ["call", "fuse"]) with { SpecRules = rules };

    private static FighterSpec Foe() => Spec(1, hp: 900, mp: 0, initiative: 1, spells: "wash");

    private static int Grown(Fighter f) => f.Statuses.Where(s => s.Stat == Stat.Grown).Sum(s => s.Value);

    /// <summary>Everyone passes until <paramref name="who"/> starts its turn number <paramref name="turn"/>.</summary>
    private static void PassUntil(Fight f, Fighter who, int turn)
    {
        while (!(f.Current == who && who.Turn == turn))
            f.Apply(new EndTurnAction());
    }

    [Fact]
    public void ACreature_GrowsAtEachOfItsTurns_UpToItsMax_AndStrikesHarder()
    {
        Fight f = Play(["A.B"], Caller(new GrowthRule(10, 25, "pet")), Foe());
        Assert.Equal(ActionError.None, f.Apply(new CastAction("call", new Cell(1, 0))));
        Fighter pet = f.Fighters[^1];
        Assert.Equal(0, Grown(pet));
        PassUntil(f, pet, 1);
        Assert.Equal(10, Grown(pet));
        PassUntil(f, pet, 2);
        Assert.Equal(20, Grown(pet));
        PassUntil(f, pet, 3);
        Assert.Equal(25, Grown(pet));
        // 10 a fang, a quarter more.
        Fighter foe = f.Fighters[1];
        f.Apply(new CastAction("fang", foe.Cell));
        Assert.Equal(900 - 12, foe.Hp);
    }

    [Fact]
    public void AGrowthOfNoKind_GrowsEveryCreature_TheRulesAddUp_AndNoPurgeTakesIt()
    {
        Fight f = Play(["A.B"], Caller(new GrowthRule(10, 50, "pet"), new GrowthRule(5, 25)), Foe());
        f.Apply(new CastAction("call", new Cell(1, 0)));
        Fighter pet = f.Fighters[^1];
        PassUntil(f, pet, 1);
        Assert.Equal(15, Grown(pet));
        // The enemy's purge takes its boons, not its growth (30 by then).
        PassUntil(f, f.Fighters[1], 2);
        int grown = Grown(pet);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("wash", pet.Cell)));
        Assert.Equal((30, 30), (grown, Grown(pet)));
        // Without a rule, nothing grows.
        Fight bare = Play(["A.B"], Caller(), Foe());
        bare.Apply(new CastAction("call", new Cell(1, 0)));
        PassUntil(bare, bare.Fighters[^1], 2);
        Assert.Equal(0, Grown(bare.Fighters[^1]));
    }

    [Fact]
    public void TheFusion_KillsTheCreature_ItsHitPointsShieldTheCaster_ItsGrowthAddsToTheBonus()
    {
        Fight f = Play(["A.B"], Caller(new GrowthRule(10, 50, "pet")), Foe());
        Fighter me = f.Fighters[0];
        f.Apply(new CastAction("call", new Cell(1, 0)));
        Fighter pet = f.Fighters[^1];
        PassUntil(f, me, 3);
        Assert.Equal(20, Grown(pet));
        pet.Hp = 30;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("fuse", pet.Cell)));
        Assert.False(pet.IsAlive);
        Assert.Contains(f.Events.OfType<Died>(), e => e.Fighter == pet.Id);
        Assert.Equal(30, me.Shield);
        Assert.Contains(me.Statuses, s => s is { Stat: Stat.Damage, Value: 40, TurnsLeft: 3 });
        // Only one of its own creatures.
        Assert.Equal(ActionError.NoValidTarget, f.Apply(new CastAction("fuse", f.Fighters[1].Cell)));
    }

    [Fact]
    public void TheAI_TakesInACreatureAboutToFall_RatherThanOneSafe()
    {
        // The enemy next to the pet strikes it for 10 (Ai.Incoming, its best blow); the caster, out of its reach, strikes for 20 a round.
        Fight f = Play(["A.B"], Caller() with { Spells = ["call", "fuse", "strike"] }, Foe() with { Spells = ["strike"] });
        Fighter me = f.Fighters[0];
        f.Apply(new CastAction("call", new Cell(1, 0)));
        Fighter pet = f.Fighters[^1];
        double safe = Ai.Value(f, me, Fuse, me.Cell, pet.Cell);
        pet.Hp = 5;
        Assert.True(Ai.Value(f, me, Fuse, me.Cell, pet.Cell) > safe);
    }

    private static Hero Familiarist(string? fork, int level, string? final = null) =>
        new("Ondine", "female-c", "summoner", Level: level, Stats: new Characteristics(Earth: 90), Spec: "familiarist", Fork: fork, Final: final);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    [Fact]
    public void TheFamiliarist_ItsLineagesAndFinals()
    {
        Specialisation spec = Real.Specs["familiarist"];
        Assert.Equal("summoner", spec.Class);
        Assert.Equal(["path-familiar", "path-titbit"], spec.Base.Spells);
        Assert.Equal(["tamer", "fusionist"], spec.Forks.Select(x => x.Id));
        Assert.Contains(new SummonRule("familiar", 20, 30), Specs.RulesOf(Familiarist(null, 40), Real));
        // The Tamer's growth adds to the branch's: 12 a turn, 60 at most.
        GrowthRule[] tamer = [.. Specs.RulesOf(Familiarist("tamer", 60), Real).OfType<GrowthRule>()];
        Assert.Equal((12, 60), (tamer.Sum(r => r.Percent), tamer.Sum(r => r.Max)));
        Assert.Equal(2, Compiled(Familiarist("tamer", 100, "familiar-master"), "path-familiar").Cooldown);
        Assert.Contains(Specs.RulesOf(Familiarist("tamer", 100, "creature-king"), Real).OfType<GrowthRule>(), r => r.Summon is null);
        Assert.Contains(Compiled(Familiarist("fusionist", 100, "chimerist"), "path-fusion").AllEffects, e => e is StatusEffect { Stat: Stat.Mp, Value: 2 });
        Assert.Contains(Compiled(Familiarist("fusionist", 100, "fusion-avatar"), "path-fusion").AllEffects, e => e is StatusEffect { Stat: Stat.Rage, Turns: 99 });
        Assert.All(spec.Forks, x => Assert.NotEqual(x.Finals![0].Template, x.Finals[1].Template));
        // The Fusion and the Titbit aim at the Familiar.
        Assert.Equal(("familiar", "familiar", "familiar"), (Real.Spells["path-fusion"].Aims, Real.Spells["path-titbit"].Aims, Real.Spells["path-recall"].Aims));
    }

    [Fact]
    public void AFamiliarists_Familiar_IsFedAndGrowsInARealFight()
    {
        var fight = new Fight(Real, "training", 1, Familiarist("tamer", 60) with { Deck = ["path-familiar"] });
        while (fight.Current != fight.HeroFighter)
            fight.Apply(new EndTurnAction());
        Fighter me = fight.HeroFighter!;
        Assert.Equal(ActionError.None, fight.Apply(new CastAction("path-familiar", me.Cell.Neighbours().First(fight.IsFree))));
        Fighter familiar = fight.Fighters[^1];
        Assert.Equal(me.MaxHp * 20 / 100, familiar.MaxHp);
        fight.Apply(new EndTurnAction());
        Assert.Same(familiar, fight.Current);
        Assert.Equal(12, Grown(familiar));
    }
}
