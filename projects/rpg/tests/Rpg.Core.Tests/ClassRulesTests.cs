using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The rules the three classes of sprint 56 brought: cooldowns, erosion, statuses renewed rather
/// than stacked, hit points by level, class duels; and the AI that plans its turn.
/// </summary>
public class ClassRulesTests
{
    // Fixed numbers where a test counts hit points.
    private static readonly Spell Blow = new("blow", Text, 2, 1, 6, false, false, 10, 10, 9);
    private static readonly Spell Nova = new("nova", Text, 2, 1, 6, false, false, 20, 20, 9, Cooldown: 2);
    private static readonly Spell Mend = new("mend", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new HealEffect(Affects.Allies, 15, 15)]);
    private static readonly Spell Venom = new("venom", Text, 2, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Poison, 6, 2, Element.Water)]);
    // A heavy hit leaves two action points of six unused; two jabs are worth more.
    private static readonly Spell Heavy = new("heavy", Text, 4, 1, 1, true, false, 9, 9, 1);
    private static readonly Spell Jab = new("jab", Text, 3, 1, 1, true, false, 6, 6, 2);
    private static readonly Spell Poke = new("poke", Text, 3, 1, 1, true, false, 3, 3, 2);
    private static readonly Spell Shot = new("shot", Text, 3, 3, 5, true, false, 10, 10, 2);
    private static readonly Spell[] All = [.. Spells, Blow, Nova, Mend, Venom, Heavy, Jab, Poke, Shot];

    private static Fight Play(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", 1);

    private static FighterSpec Caster(params string[] spells) => Spec(0, ap: 99, mp: 0, initiative: 999, spells: spells);

    [Fact]
    public void ACooldown_SkipsTheCastersNextTurns_ThenTheSpellIsBack()
    {
        Fight f = Play(["A.B"], Caster("nova", "blow"), Spec(1, hp: 999));
        Spell nova = f.Fighters[0].Spells[0];
        Assert.Equal(ActionError.None, f.Apply(new CastAction("nova", new Cell(2, 0))));
        // Not again this turn, nor during the next two turns of the caster; other spells are free.
        Assert.Equal(ActionError.Cooldown, f.Check(new CastAction("nova", new Cell(2, 0))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("blow", new Cell(2, 0))));
        for (int turn = 2; turn >= 1; turn--)
        {
            f.Apply(new EndTurnAction());
            f.Apply(new EndTurnAction());
            Assert.Equal((turn, ActionError.Cooldown), (f.Fighters[0].CooldownLeft(nova), f.Check(new CastAction("nova", new Cell(2, 0)))));
        }
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal((0, ActionError.None), (f.Fighters[0].CooldownLeft(nova), f.Apply(new CastAction("nova", new Cell(2, 0)))));
    }

    [Fact]
    public void Erosion_TakesATenthOfTheDamage_FromTheMaximum_HealsCannotGiveItBack()
    {
        Fight f = Play(["A.B"], Caster("blow"), Spec(1, hp: 100, spells: ["mend"]));
        f.Apply(new CastAction("blow", new Cell(2, 0)));
        f.Apply(new CastAction("blow", new Cell(2, 0)));
        Fighter b = f.Fighters[1];
        Assert.Equal((80, 98, 2), (b.Hp, b.MaxHp, b.Eroded));
        f.Apply(new EndTurnAction());
        // 15 healed would make 95, under the eroded maximum; a second heal stops at 98.
        f.Apply(new CastAction("mend", new Cell(2, 0)));
        f.Apply(new CastAction("mend", new Cell(2, 0)));
        Assert.Equal(98, b.Hp);
    }

    [Fact]
    public void TheSameStatus_FromTheSameCaster_IsRenewed_FromAnotherOneItAddsUp()
    {
        Fight f = Play(["A.B", "A.."], Caster("venom"), Spec(0, 1, ap: 99, mp: 0, initiative: 998, spells: ["venom"]), Spec(1, hp: 99));
        f.Apply(new CastAction("venom", new Cell(2, 0)));
        f.Apply(new CastAction("venom", new Cell(2, 0)));
        Assert.Single(f.Fighters[2].Statuses);
        f.Apply(new EndTurnAction());
        f.Apply(new CastAction("venom", new Cell(2, 0)));
        Assert.Equal([0, 1], f.Fighters[2].Statuses.Select(s => s.Source));
    }

    [Fact]
    public void AHero_GainsHitPointsWithItsLevel_AtItsClassesPace()
    {
        foreach (HeroClass c in Real.Classes)
        {
            foreach (int level in new[] { 1, 50, 100 })
            {
                var f = new Fight(Real, "duel", 1, new Hero("Ondine", "female-e", c.Id, Level: level));
                Assert.Equal(c.Hp + c.HpPerLevel * (level - 1), f.Fighters[0].MaxHp);
            }
        }
        // Without a pace of its own, a class takes the rules'.
        Assert.Equal(Fight.HpPerLevel, new HeroClass("x", Text, Text, 50, 6, 3, 100, ["strike"]).HpPerLevel);
    }

    [Fact]
    public void ARival_PlaysTeamB_AndTheRecordKeepsIt()
    {
        var f = new Fight(Real, "duel", 6, new Hero("Ondine", "female-e", "guard", Level: 40), new Hero("Élise", "female-c", "mage", Level: 40));
        Fighter rival = f.Fighters.First(x => x.Team == 1);
        HeroClass mage = Real.Class("mage")!;
        Assert.Equal(("Élise", mage.Ap, mage.Hp + mage.HpPerLevel * 39), (rival.Name.Fr, rival.Spec.Ap, rival.MaxHp));
        Assert.Equal(mage.Spells.Where(id => Real.Spells[id].Level <= 40), rival.Spells.Select(s => s.Id));
        Ai.PlayOut(f);
        FightRecord r = FightRecord.FromJson(FightRecord.Of(f).ToJson());
        Assert.Equal(f.Rival, r.Rival);
        Assert.Equal(Fingerprint(f), Fingerprint(r.Replay(Real)));
        Assert.Throws<InvalidFightRecordException>(() => (r with { Rival = r.Rival! with { Class = "dragon" } }).Replay(Real));
        Assert.Throws<ArgumentException>(() => new Fight(Real, "duel", 1, rival: new Hero("Élise", "female-c", "mage", Level: 0)));
    }

    [Fact]
    public void TheAi_PlansItsTurn_TwoJabsBeatAHeavyHitAndTwoWastedPoints()
    {
        Fight f = Play(["AB"], Spec(0, ap: 6, spells: ["heavy", "jab"]), Spec(1, hp: 50));
        Assert.Equal(new CastAction("jab", new Cell(1, 0)), Ai.Decide(f));
        // With seven points the heavy hit and a jab fit: the stronger goes first.
        Fight g = Play(["AB"], Spec(0, ap: 7, spells: ["jab", "heavy"]), Spec(1, hp: 50));
        Assert.Equal(new CastAction("heavy", new Cell(1, 0)), Ai.Decide(g));
    }

    [Fact]
    public void InMelee_ARangedFighter_StepsBack_UnlessItCanKillNow()
    {
        Fight f = Play(["...AB."], Spec(0, mp: 3, spells: ["poke", "shot"]), Spec(1, hp: 50));
        Assert.Equal(new MoveAction(new Cell(1, 0)), Ai.Decide(f));
        Fight g = Play(["...AB."], Spec(0, mp: 3, spells: ["poke", "shot"]), Spec(1, hp: 3));
        Assert.Equal(new CastAction("poke", new Cell(4, 0)), Ai.Decide(g));
    }

    [Fact]
    public void TheAi_FinishesOffAWeakEnemy_RatherThanHitAHealthyOneHarder()
    {
        // Strike does 10: 8 on the enemy with 8 hit points, who then hits back no more.
        Fight f = Play(["BAB"], Spec(0, spells: ["strike"]), Spec(1, 0, hp: 8), Spec(1, 1, hp: 50));
        Assert.Equal(new CastAction("strike", new Cell(0, 0)), Ai.Decide(f));
    }

    [Fact]
    public void WithItsActionPointsSpent_TheAiStays_InsteadOfWalkingIn()
    {
        Assert.IsType<EndTurnAction>(Ai.Decide(Play(["A....B"], Spec(0, ap: 2, mp: 3, spells: ["strike"]), Spec(1))));
        Assert.IsType<MoveAction>(Ai.Decide(Play(["A....B"], Spec(0, ap: 3, mp: 3, spells: ["strike"]), Spec(1))));
    }

    [Fact]
    public void TheAi_DoesNotRenewAPoisonThatIsStillRunning()
    {
        Fight f = Play(["A.B"], Caster("venom", "lob"), Spec(1, hp: 99));
        Assert.Equal(new CastAction("venom", new Cell(2, 0)), Ai.Decide(f));
        f.Apply(Ai.Decide(f));
        Assert.Equal(new CastAction("lob", new Cell(2, 0)), Ai.Decide(f));
    }
}
