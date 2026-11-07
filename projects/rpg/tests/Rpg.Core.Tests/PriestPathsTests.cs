using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Priest's paths (sprint 202, docs/classes-pretre-2.md): the resurrection and the heals of the Cleric on small boards, then the tree of
/// three branches, six lineages and twelve final classes on the real data.
/// </summary>
public class PriestPathsTests
{
    private static readonly Spell Raise = new("raise", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new ReviveEffect(Affects.Allies, 30)]);
    private static readonly Spell Slay = new("slay", Text, 1, 0, 6, false, false, 100, 100, 9, Element.Neutral);
    private static readonly Spell Mend = new("mend", Text, 1, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new HealEffect(Affects.Allies, 10, 10)]);
    private static readonly Spell[] All = [.. Spells, Raise, Slay, Mend];

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    private static FighterSpec Caster(params string[] spells) => Spec(0, ap: 99, mp: 0, initiative: 999, spells: spells);

    // ---- The resurrection (§2) ----

    [Fact]
    public void AFallenAlly_RisesOnItsCell_WithAShareOfItsHitPoints_OncePerFight()
    {
        Fight f = Play(["A.A..B"], Caster("slay", "raise"), Spec(0, 1, hp: 100, initiative: 10), Spec(1, hp: 900, initiative: 1));
        Fighter ally = f.Fighters[1];
        Cell where = ally.Cell;
        Assert.Equal(ActionError.NoValidTarget, f.CheckCast(f.Current, f.Current.Spells.Single(s => s.Id == "raise"), f.Current.Cell, where));
        f.Apply(new CastAction("slay", where));
        Assert.False(ally.IsAlive);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("raise", where)));
        Assert.True(ally.IsAlive);
        // Struck down for 100, it keeps 90 of its hit points at most (the erosion): 27 of them.
        Assert.Equal((90, 27), (ally.MaxHp, ally.Hp));
        Assert.Equal(where, ally.Cell);
        Assert.Contains(f.Events.OfType<Revived>(), e => e.Fighter == 1 && e.Hp == ally.Hp);
        // It plays again in its turn.
        f.Apply(new EndTurnAction());
        Assert.Equal(ally, f.Current);
        // Once a fight, whoever falls.
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        f.Apply(new CastAction("slay", where));
        Assert.Equal(ActionError.NoValidTarget, f.Apply(new CastAction("raise", where)));
    }

    [Fact]
    public void NeitherAnEnemyNorASummon_NorAFallenAllyUnderSomeone_Rises()
    {
        Fight f = Play(["A.A.BB"], Caster("slay", "raise"), Spec(0, 1, hp: 100, initiative: 10), Spec(1, hp: 50, initiative: 1), Spec(1, 1, hp: 900, initiative: 0));
        Fighter enemy = f.Fighters[2];
        f.Apply(new CastAction("slay", enemy.Cell));
        Assert.False(enemy.IsAlive);
        Assert.Equal(ActionError.NoValidTarget, f.Apply(new CastAction("raise", enemy.Cell)));
    }

    [Fact]
    public void TheRevive_IsData_ItsShareOneToAHundred_OnAllies()
    {
        Spell bad = Raise with { Id = "bad", Effects = [new ReviveEffect(Affects.Allies, 0)] };
        Spell foe = Raise with { Id = "foe", Effects = [new ReviveEffect(Affects.Enemies, 30)] };
        foreach (Spell s in new[] { bad, foe })
            Assert.Throws<InvalidDataException>(() => new GameData([.. All, s], [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])]));
    }

    [Fact]
    public void TheAi_RaisesAFallenFriend_BeforeAnythingElse()
    {
        Fight f = Play(["A.AA.B"], Caster("slay"), Spec(0, 1, hp: 100, ap: 4, mp: 0, initiative: 500, spells: ["raise", "mend"]), Spec(0, 2, hp: 100, mp: 0, initiative: 400),
            Spec(1, hp: 900, ap: 0, mp: 0, initiative: 1));
        Fighter fallen = f.Fighters[2];
        f.Apply(new CastAction("slay", fallen.Cell));
        f.Apply(new EndTurnAction());
        Assert.Equal(new CastAction("raise", fallen.Cell), Ai.Decide(f));
    }

    // ---- The Cleric's heals grow (§2), on the real data ----

    private static GameData Board(string[] rows, params FighterSpec[] fighters) =>
        new([.. Real.Spells.Values], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], Real.Classes,
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);

    private static Hero Priest(string? spec, string? fork = null, int level = 60, string? final = null, params string[] deck) =>
        new("Ondine", "female-c", "priest", Level: level, Stats: new Characteristics(Water: 90), Spec: spec, Fork: fork, Final: final, Deck: deck.Length == 0 ? null : deck);

    /// <summary>What Mend, cast on the hero at full hit points, shields it with (its whole roll), the same seed each time.</summary>
    private static int MendShield(Hero hero)
    {
        Fight f = new(Board(["A..B"], Spec(0, initiative: 9999), Spec(1, hp: 900, initiative: 1)), "s", 7, hero);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("mend", f.HeroFighter!.Cell)));
        return f.Events.OfType<Shielded>().Single().Amount;
    }

    [Fact]
    public void TheClerics_HealsAreFifteenPercentBigger_TheHealersTwentyFive()
    {
        int bare = MendShield(Priest(null, level: 40, deck: "mend"));
        Assert.Equal(bare * 115 / 100, MendShield(Priest("cleric", level: 40, deck: "mend")));
        int bareAt50 = MendShield(Priest(null, level: 50, deck: "mend"));
        Assert.Equal(bareAt50 * 125 / 100, MendShield(Priest("cleric", "healer", level: 50, deck: "mend")));
    }

    // ---- The tree (§1), its spells and finals ----

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    [Fact]
    public void TheTree_IsClericInquisitorOracle_TwoLineagesEach_TwelveFinals()
    {
        Specialisation[] branches = [.. Real.Specs.Values.Where(s => s.Class == "priest")];
        Assert.Equal(["cleric", "inquisitor", "oracle"], branches.Select(b => b.Id));
        Assert.Equal(["healer", "sacred-medic", "exorcist", "fanatic", "prophet", "mystic"], branches.SelectMany(b => b.Forks).Select(f => f.Id));
        Assert.Equal(12, branches.SelectMany(b => b.Forks).SelectMany(f => f.Finals!).Count());
        // Two spells at 30, one at 50, each of no class, never a blow from next to its target.
        Assert.All(branches, b => Assert.Equal(2, b.SpellsWith(null).Count()));
        Spell[] paths = [.. branches.SelectMany(b => b.Forks.SelectMany(f => b.SpellsWith(f.Id))).Distinct().Select(id => Real.Spells[id])];
        Assert.Equal(12, paths.Length);
        Assert.All(paths.Where(sp => sp.DamageMax > 0), sp => Assert.True(sp.MinRange >= 2, sp.Id));
    }

    [Fact]
    public void TheOraclesOmen_StrikesOnceOnTheTargetsNextTurn()
    {
        Hero oracle = Priest("oracle", level: 40, deck: ["path-omen", "path-foresight"]);
        Fight f = new(Board(["A....B"], Spec(0, initiative: 9999), Spec(1, hp: 900, mp: 0, ap: 0, initiative: 1)), "s", 1, oracle);
        Fighter enemy = f.Fighters[1];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-omen", enemy.Cell)));
        Assert.Equal(900, enemy.Hp);
        f.Apply(new EndTurnAction());
        Assert.Equal(900 - Real.Spells["path-omen"].AllEffects.OfType<StatusEffect>().Single().Value, enemy.Hp);
        // Once: gone as its turn ends.
        f.Apply(new EndTurnAction());
        Assert.DoesNotContain(enemy.Statuses, st => st.Stat == Stat.Poison);
    }

    [Fact]
    public void TheFinals_ChangeWhatTheirLineageHas()
    {
        Spell wave = Compiled(Priest("cleric", "healer", 100, "saint"), "path-healing-wave");
        Assert.Equal((7, new Area(AreaShape.Circle, 3)), (wave.MaxRange, wave.Area));
        Assert.Equal(1, Compiled(Priest("cleric", "healer", 100, "living-miracle"), "path-healing-wave").Cooldown);
        Spell remedy = Compiled(Priest("cleric", "sacred-medic", 100, "divine-surgeon"), "path-remedy");
        Assert.Equal((0, 2), (remedy.Cooldown, remedy.PerTurn));
        Assert.Equal(6, Compiled(Priest("cleric", "sacred-medic", 100, "life-warden"), "path-resurrect").MaxRange);
        Assert.Equal(7, Compiled(Priest("inquisitor", "exorcist", 100, "purifier"), "path-exorcism").MaxRange);
        Assert.Equal(1, Compiled(Priest("inquisitor", "fanatic", 100, "zealot"), "path-zeal").Cooldown);
        Assert.Equal(8, Compiled(Priest("oracle", "prophet", 100, "supreme-seer"), "path-prophecy").MaxRange);
        Assert.Equal(1, Compiled(Priest("oracle", "mystic", 100, "divine-sage"), "path-vision").Cooldown);
        // Rules and passives on the lineage's.
        Assert.Equal(20, Specs.RulesOf(Priest("inquisitor", "exorcist", 100, "demon-hunter"), Real).OfType<TargetStateRule>().Sum(r => r.Percent));
        Assert.Equal(2, Specs.RulesOf(Priest("oracle", "mystic", 100, "augur"), Real).OfType<ReserveRule>().Sum(r => r.Max));
        Assert.Contains("spec-steady-mind", Specs.PassivesOf(Priest("oracle", "mystic", 100, "augur"), Real));
        Assert.Contains("spec-not-today", Specs.PassivesOf(Priest("cleric", "sacred-medic", 100, "life-warden"), Real));
    }
}
