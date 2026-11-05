using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Guard's three branches and six lineages (sprint 192, D95, docs/elements-specialisations.md §v4.4), with the bricks they ask for: shields worth more on allies (J), an
/// unmovable fighter (K), a rescue (L), a shield spent in a blow (M), longer pulls (N), longer losses (O), a bonus on a held target (P).
/// </summary>
public class GuardPathsTests
{
    private static readonly Spell Blow = new("t-blow", Text, 1, 1, 1, true, false, 10, 10, 9);
    private static readonly Spell Slam = new("t-slam", Text, 1, 1, 1, true, false, 500, 500, 9);
    private static readonly Spell Shove = new("t-shove", Text, 1, 1, 3, false, false, 0, 0, 9, Effects: [new PushEffect(Affects.Enemies, 2)]);

    private static Hero Guard(string? spec, string? fork, int level, params string[] deck) =>
        new("Ondine", "female-a", "guard", Level: level, Stats: new Characteristics(Earth: 50), Spec: spec, Fork: fork, Deck: deck.Length == 0 ? null : deck);

    private static GameData With(string[] rows, FighterSpec[] fighters) =>
        new([.. Real.Spells.Values, Blow, Slam, Shove], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], Real.Classes,
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);

    /// <summary>The hero plays first, against a monster that plays after it; a second hero stands on team A if given.</summary>
    private static Fight On(string[] rows, Hero hero, FighterSpec[] monsters, Hero? ally = null)
    {
        FighterSpec seat = Spec(0, initiative: 9999);
        return new Fight(With(rows, [seat, .. monsters]), "s", 1, hero, allies: ally is null ? null : [ally]);
    }

    private static FighterSpec Foe(int start = 0, int hp = 900, params string[] spells) => Spec(1, start, hp: hp, ap: 9, mp: 3, initiative: 1, spells: spells);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    [Fact]
    public void EveryLeaf_HasItsThreeSpells_AndItsTitle()
    {
        foreach (Specialisation s in Real.Specs.Values.Where(s => s.Class == "guard"))
        {
            Assert.Equal(2, s.Base.Spells!.Count);
            Assert.All(s.Forks, f => Assert.Equal(3, s.SpellsWith(f.Id).Count()));
        }
        Assert.Equal("Muraille", Specs.TitleOf(Guard("palisade", "rampart", 100), Real)!.Fr);
    }

    [Fact]
    public void TheClosedRanks_ShieldItsBearer_AndItsAllies_WorthMoreOnThem()
    {
        Fight f = On(["AA.......B"], Guard("palisade", null, 40), [Foe()], Guard(null, null, 40) with { Name = "Brume" });
        Assert.Equal(9, f.HeroFighter!.Shield);
        Assert.Equal(11, f.HeroFighters[1].Shield);
        // Lasting through the enemies' turns.
        f.Apply(new EndTurnAction());
        Assert.Equal(9, f.HeroFighter.Shield);
    }

    [Fact]
    public void TheClosedRanks_GrowAndReachFarther_OnTheRampart()
    {
        Assert.Equal(["spec-close-ranks-12"], Specs.PassivesOf(Guard("palisade", "rampart", 60), Real));
        Assert.Equal(["spec-close-ranks-17", "spec-not-today"], Specs.PassivesOf(Guard("palisade", "rampart", 100), Real));
        Assert.Equal(["spec-close-ranks-9"], Specs.PassivesOf(Guard("palisade", "bastion", 100), Real).Where(p => p != "spec-thorns"));
        Assert.Equal(new Area(AreaShape.Circle, 2), Real.Passives["spec-close-ranks-12"].Area);
    }

    [Fact]
    public void TheShieldWall_Gives30_And37OnAnAlly()
    {
        Fight f = On(["AA.......B"], Guard("palisade", "rampart", 50, "path-shield-wall"), [Foe()], Guard(null, null, 50) with { Name = "Brume" });
        int mine = f.HeroFighter!.Shield, theirs = f.HeroFighters[1].Shield;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-shield-wall", f.HeroFighter.Cell)));
        Assert.Equal(mine + 30, f.HeroFighter.Shield);
        Assert.Equal(theirs + 37, f.HeroFighters[1].Shield);
    }

    [Fact]
    public void StandFast_NothingMovesIt_NoCollisionEither()
    {
        Fight f = On([".AB......."], Guard("palisade", null, 40, "path-stand-fast"), [Foe(spells: ["t-shove"])]);
        Cell at = f.HeroFighter!.Cell;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-stand-fast", at)));
        Assert.Equal(15, f.HeroFighter.Statuses.Single(s => s.Stat == Stat.Resistance).Value);
        f.Apply(new EndTurnAction());
        int hp = f.HeroFighter.Hp;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-shove", at)));
        Assert.Equal((at, hp), (f.HeroFighter.Cell, f.HeroFighter.Hp));
        // And without it, it moves.
        Fight g = On([".AB......."], Guard("palisade", null, 40), [Foe(spells: ["t-shove"])]);
        g.Apply(new EndTurnAction());
        g.Apply(new CastAction("t-shove", g.HeroFighter!.Cell));
        Assert.NotEqual(at, g.HeroFighter.Cell);
    }

    [Fact]
    public void TheInterpose_TradesPlacesWithAnAlly_AndShieldsItsCaster()
    {
        Fight f = On(["AA.......B"], Guard("palisade", null, 40, "path-interpose"), [Foe()], Guard(null, null, 40) with { Name = "Brume" });
        (Cell a, Cell b) = (f.HeroFighter!.Cell, f.HeroFighters[1].Cell);
        int shield = f.HeroFighter.Shield;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-interpose", b)));
        Assert.Equal((b, a), (f.HeroFighter.Cell, f.HeroFighters[1].Cell));
        Assert.Equal(shield + 15, f.HeroFighter.Shield);
        Assert.Equal(1, Compiled(Guard("palisade", "rampart", 100), "path-interpose").Cooldown);
    }

    [Fact]
    public void NotToday_KeepsAnAllyAtOneHitPoint_OnceAFight_WithinTwoCells()
    {
        Hero ally = Guard(null, null, 100) with { Name = "Brume" };
        Fight f = On(["AA.B......"], Guard("palisade", "rampart", 100), [Foe(spells: ["t-slam"])], ally);
        Fighter friend = f.HeroFighters[1];
        friend.Shields.Clear();
        f.Fighters.Single(x => x.Team == 1).Cell = friend.Cell + new Cell(1, 0);
        while (f.Current.Team != 1)
            f.Apply(new EndTurnAction());
        friend.Shields.Clear();
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-slam", friend.Cell)));
        Assert.Equal(1, friend.Hp);
        Assert.Single(f.Events.OfType<PassiveTriggered>(), p => p.Passive == "spec-not-today");
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-slam", friend.Cell)));
        Assert.False(friend.IsAlive);
    }

    [Fact]
    public void NotToday_DoesNotSaveTheBearer_NorAnAllyTooFar()
    {
        Fight self = On(["AB.........."], Guard("palisade", "rampart", 100), [Foe(spells: ["t-slam"])]);
        self.Apply(new EndTurnAction());
        self.HeroFighter!.Shields.Clear();
        self.Apply(new CastAction("t-slam", self.HeroFighter.Cell));
        Assert.False(self.HeroFighter.IsAlive);

        Fight far = On(["A......AB.."], Guard("palisade", "rampart", 100), [Foe(spells: ["t-slam"])], Guard(null, null, 100) with { Name = "Brume" });
        while (far.Current.Team != 1)
            far.Apply(new EndTurnAction());
        far.HeroFighters[1].Shields.Clear();
        far.Apply(new CastAction("t-slam", far.HeroFighters[1].Cell));
        Assert.False(far.HeroFighters[1].IsAlive);
    }

    [Fact]
    public void TheRetaliation_SpendsTheWholeShield_UpToItsLimit()
    {
        Hero hero = Guard("palisade", "bastion", 50, "path-retaliation");
        static int Dealt(Fight f) => f.Events.OfType<Damaged>().Last().Amount;
        int With(int shield)
        {
            Fight f = On(["AB........"], hero, [Foe()]);
            f.HeroFighter!.Shields.Clear();
            if (shield > 0)
                f.HeroFighter.Shields.Add((shield, 2));
            Assert.Equal(ActionError.None, f.Apply(new CastAction("path-retaliation", new Cell(1, 0))));
            Assert.Equal(0, f.HeroFighter.Shield);
            return Dealt(f);
        }
        // The same seed, the same roll: more with more shield, up to its limit.
        Assert.True(With(8) > With(0));
        Assert.True(With(19) > With(8));
        Assert.Equal(With(19), With(500));
    }

    [Fact]
    public void TheRetaliation_OnNobody_KeepsTheShield()
    {
        Fight f = On(["A.......B."], Guard("palisade", "bastion", 50, "path-retaliation"), [Foe()]);
        int shield = f.HeroFighter!.Shield;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-retaliation", new Cell(1, 0))));
        Assert.Equal(shield, f.HeroFighter.Shield);
    }

    [Fact]
    public void TheBastion_ShieldsItsStand_ThenStrikesBehind_AndPoisons()
    {
        Assert.Contains(Compiled(Guard("palisade", "bastion", 50), "path-stand-fast").AllEffects, e => e is ShieldEffect { Amount: 15 });
        Assert.DoesNotContain(Compiled(Guard("palisade", null, 40), "path-stand-fast").AllEffects, e => e is ShieldEffect);
        Spell at100 = Compiled(Guard("palisade", "bastion", 100), "path-retaliation");
        Assert.Equal(new Area(AreaShape.Line, 1), at100.Zone);
        Assert.Equal(Real.Spells["path-retaliation"].DamageMax * 80 / 100, at100.DamageMax);
        Fight f = On(["AB........"], Guard("palisade", "bastion", 100, "stone-skin"), [Foe(spells: ["t-blow"])]);
        f.Apply(new EndTurnAction());
        f.Apply(new CastAction("t-blow", f.HeroFighter!.Cell));
        Assert.Contains(f.Fighters.Single(x => x.Team == 1).Statuses, s => s is { Stat: Stat.Poison, Value: 8, TurnsLeft: 2 });
    }

    [Fact]
    public void TheBreach_PullsFarther_OnlyWhatPullsEnemies()
    {
        Assert.Equal(4, Compiled(Guard("breach", null, 40), "taunt").AllEffects.OfType<PullEffect>().Single().Cells);
        Assert.Equal(3, Compiled(Guard(null, null, 40), "taunt").AllEffects.OfType<PullEffect>().Single().Cells);
        Assert.Equal(4, Compiled(Guard("breach", "harpooner", 50), "path-harpoon").AllEffects.OfType<PullEffect>().Single().Cells);
    }

    [Fact]
    public void TheAssault_LandsOnAFreeCell_AndStrikesAroundIt()
    {
        Fight f = On(["A...B......"], Guard("breach", null, 40, "path-assault"), [Foe()]);
        Assert.Equal(ActionError.TargetNotFree, f.Check(new CastAction("path-assault", new Cell(4, 0))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-assault", new Cell(3, 0))));
        Assert.Equal(new Cell(3, 0), f.HeroFighter!.Cell);
        Assert.True(f.Fighters.Single(x => x.Team == 1).Hp < 900);
        Assert.Equal(1, Compiled(Guard("breach", "ram", 100), "path-assault").Cooldown);
    }

    [Theory]
    [InlineData("breach", null, 40, 1, 0)]
    [InlineData("breach", "ram", 60, 1, 1)]
    [InlineData("breach", "ram", 100, 1, 2)]
    public void AnEnemyFinishedOff_GivesItsPoints(string spec, string? fork, int level, int mp, int ap)
    {
        Fight f = On(["AB.B......"], Guard(spec, fork, level, "shield-bash"), [Foe(0, hp: 1), Foe(1)]);
        Fighter me = f.HeroFighter!;
        (int beforeAp, int beforeMp) = (me.Ap, me.Mp);
        Spell bash = me.Spells.Single();
        Assert.Equal(ActionError.None, f.Apply(new CastAction("shield-bash", new Cell(1, 0))));
        Assert.False(f.Fighters[1].IsAlive);
        Assert.Equal((beforeAp - bash.ApCost + ap, beforeMp + mp), (me.Ap, me.Mp));
    }

    [Fact]
    public void TheHarpooner_HitsHarderUpClose_AndItsCollisionsHurt()
    {
        TalentSpec at50 = Specs.AsTalent(Specs.RulesOf(Guard("breach", "harpooner", 50), Real));
        Assert.Equal(15, Talents.Percent([at50], new Cell(0, 0), new Cell(1, 0)));
        Assert.Equal(0, Talents.Percent([at50], new Cell(0, 0), new Cell(2, 0)));
        Assert.Equal(Fight.CollisionDamage, Talents.Collision([at50]));
        TalentSpec at100 = Specs.AsTalent(Specs.RulesOf(Guard("breach", "harpooner", 100), Real));
        Assert.Equal(15, Talents.Collision([at100]));
        Spell harpoon = Compiled(Guard("breach", "harpooner", 100), "path-harpoon");
        Assert.Equal((0, 1), (harpoon.Cooldown, harpoon.PerTurn));
    }

    [Fact]
    public void TheGrip_MakesItsLossesLast_ButNotACompoundsNorOtherStates()
    {
        Spell hold = Compiled(Guard("grip", null, 40), "path-hold");
        Assert.Equal(3, hold.AllEffects.OfType<StatusEffect>().Single().Turns);
        Assert.Equal(2, Compiled(Guard("grip", null, 40), "ice-blade").AllEffects.OfType<StatusEffect>().Single().Turns);
        var rule = Specs.AsTalent([new StatusTurnsRule(1, [Stat.Ap, Stat.Mp])]);
        Spell twin = Real.Spells.Values.First(s => s.Twin is not null && s.AllEffects.Any(e => e is StatusEffect { Stat: Stat.Mp, Affects: Affects.Enemies }));
        int turns = twin.AllEffects.OfType<StatusEffect>().First(e => e.Stat == Stat.Mp).Turns;
        Assert.Equal(turns, Talents.Compile(twin, [rule]).AllEffects.OfType<StatusEffect>().First(e => e.Stat == Stat.Mp).Turns);
    }

    [Fact]
    public void TheLock_TakesAllItsMovement_ForItsNextTurn()
    {
        Fight f = On(["AB........"], Guard("grip", null, 40, "path-lock"), [Foe()]);
        Fighter foe = f.Fighters.Single(x => x.Team == 1);
        Assert.False(foe.HeldStill);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-lock", foe.Cell)));
        Assert.True(foe.HeldStill);
        f.Apply(new EndTurnAction());
        Assert.Equal(0, foe.Mp);
        Assert.Equal(2, Compiled(Guard("grip", "jailer", 100), "path-lock").Cooldown);
    }

    [Fact]
    public void HandsOff_TakesAPoint_AndOnTheJailerAnActionPoint_OncePerTurn()
    {
        Fight f = On(["AB........"], Guard("grip", "jailer", 60, "stone-skin"), [Foe(spells: ["t-blow"])]);
        Fighter foe = f.Fighters.Single(x => x.Team == 1);
        f.Apply(new EndTurnAction());
        (int ap, int mp) = (foe.Ap, foe.Mp);
        f.Apply(new CastAction("t-blow", f.HeroFighter!.Cell));
        Assert.Equal((ap - 2, mp - 1), (foe.Ap, foe.Mp));
        f.Apply(new CastAction("t-blow", f.HeroFighter.Cell));
        Assert.Equal((ap - 3, mp - 2), (foe.Ap, foe.Mp));
    }

    [Fact]
    public void TheCrush_HitsHarderOnAHeldTarget_PastTheCeiling()
    {
        static int Bonus(Hero hero, bool still, string spell) =>
            Talents.Percent([Specs.AsTalent(Specs.RulesOf(hero, Real))], new Cell(0, 0), new Cell(1, 0), Element.Earth, new HitContext(TargetStill: still, Spell: spell));
        Assert.Equal(30, Bonus(Guard("grip", "jailer", 60), true, "path-crush"));
        Assert.Equal(50, Bonus(Guard("grip", "jailer", 100), true, "path-crush"));
        Assert.Equal(0, Bonus(Guard("grip", "jailer", 100), false, "path-crush"));
        Assert.Equal(0, Bonus(Guard("grip", "jailer", 100), true, "path-hold"));
        // In a fight: the Lock, then the Crush.
        Fight f = On(["AB........"], Guard("grip", "jailer", 60, "path-lock", "path-crush"), [Foe()]);
        Fighter foe = f.Fighters.Single(x => x.Team == 1);
        Assert.False(f.ContextOf(f.HeroFighter!, f.HeroFighter!.Spells[1], f.HeroFighter.Cell, foe).TargetStill);
        f.Apply(new CastAction("path-lock", foe.Cell));
        HitContext held = f.ContextOf(f.HeroFighter, f.HeroFighter.Spells.Single(s => s.Id == "path-crush"), f.HeroFighter.Cell, foe);
        Assert.True(held.TargetStill);
        Assert.Equal("path-crush", held.Spell);
        Assert.True(held.TargetStates >= 1);
    }

    [Fact]
    public void ThePincer_ReachesFurther_ThenHoldsThoseNextToIt()
    {
        Assert.Equal(7, Compiled(Guard("grip", "pincer", 50), "taunt").MaxRange);
        Assert.Equal(5, Compiled(Guard("grip", "jailer", 50), "taunt").MaxRange);
        Assert.Equal(1, Compiled(Guard("grip", "pincer", 100), "path-iron-ring").Cooldown);
        Fight ring = On(["AB.......B"], Guard("grip", "pincer", 50, "path-iron-ring"), [Foe(0), Foe(1)]);
        Assert.Equal(ActionError.None, ring.Apply(new CastAction("path-iron-ring", ring.HeroFighter!.Cell)));
        Assert.Contains(ring.Fighters[1].Statuses, s => s.Stat == Stat.Mp);
        Assert.DoesNotContain(ring.Fighters[2].Statuses, s => s.Stat == Stat.Mp);
        // At the start of its turn, the passive of 100: its own side untouched.
        Fight f = On(["AB........"], Guard("grip", "pincer", 100, "stone-skin"), [Foe()]);
        Assert.Contains(f.Fighters.Single(x => x.Team == 1).Statuses, s => s.Stat == Stat.Mp);
        Assert.DoesNotContain(f.HeroFighter!.Statuses, s => s.Stat == Stat.Mp);
    }

    [Theory]
    [InlineData("guard", "breach", null, 35, "quake")]
    [InlineData("guard", "grip", "jailer", 50, "quake")]
    [InlineData("sentinel", "trail", null, 35, "rockfall")]
    [InlineData("sentinel", "sightline", "marksman", 50, "rockfall")]
    public void APathsSpells_DoNotPushTheClasssBestAreaOutOfTheAutomaticDeck(string cls, string spec, string? fork, int level, string area)
    {
        // Sprint 193 (T152): they took the places of the last spells unlocked, Quake and Rockfall, 3 wins of 80 against 16 for a Guard of the Breach.
        // Built, ranks and all: with no rank, the first version of this rule looked right and did nothing in play.
        Hero hero = Progression.Built(new Hero("Ondine", "female-a", cls, Level: level), Real);
        Assert.NotEmpty(hero.Ranks!);
        Assert.Contains(area, hero.DeckOf(Real));
        IReadOnlyList<string> deck = (hero with { Spec = spec, Fork = fork }).DeckOf(Real);
        Assert.Contains(area, deck);
        Assert.Equal(Hero.DeckSize, deck.Count);
        Assert.All(Specs.SpellsOf(hero with { Spec = spec, Fork = fork }, Real), id => Assert.Contains(id, deck));
    }

    [Theory]
    [InlineData("ambush", null, 35)]
    [InlineData("trail", null, 45)]
    [InlineData("ambush", "trapper", 50)]
    [InlineData("trail", "leaper", 50)]
    public void APathsSpells_LeaveTheSpellsOfTheHerosBestElement(string spec, string? fork, int level)
    {
        // Sprint 194 (T154): a Sentinel built on Earth lost Heavy Shot on every path; given back, it won 4 to 20 points more of 100.
        Hero hero = Progression.Built(new Hero("Ondine", "female-a", "sentinel", Level: level), Real);
        Assert.True(hero.Stats!.Earth > hero.Stats.Fire);
        Assert.Contains("heavy-shot", (hero with { Spec = spec, Fork = fork }).DeckOf(Real));
    }

    [Fact]
    public void APathsSpells_LeaveWhatTheHeroHasPointsIn_NotAFixedList()
    {
        // The same path, the same level: all the points in Fire, and Heavy Shot is no longer what is kept over Fire's spells.
        Hero built = Progression.Built(new Hero("Ondine", "female-a", "sentinel", Level: 50), Real);
        Hero fire = built with { Stats = new Characteristics(Fire: built.Stats!.Earth + built.Stats.Fire + built.Stats.Water + built.Stats.Air), Spec = "ambush", Fork = "trapper" };
        IReadOnlyList<string> deck = fire.DeckOf(Real);
        Assert.Contains("fire-arrow", deck);
        Assert.DoesNotContain("heavy-shot", deck);
    }

    [Fact]
    public void WithoutAPath_TheAutomaticDeck_DoesNotChange()
    {
        var hero = new Hero("Ondine", "female-a", "guard", Level: 60);
        Assert.Equal(hero.FirstSpells(Real).Count, hero.DeckOf(Real).Count);
        Assert.Equal(Hero.DeckSize, hero.DeckOf(Real).Count);
    }

    public static TheoryData<string, string?, int> Leaves()
    {
        var leaves = new TheoryData<string, string?, int>();
        foreach (Specialisation s in Real.Specs.Values.Where(s => s.Class == "guard"))
        {
            leaves.Add(s.Id, null, 40);
            foreach (SpecFork f in s.Forks)
            {
                leaves.Add(s.Id, f.Id, 60);
                leaves.Add(s.Id, f.Id, 100);
            }
        }
        return leaves;
    }

    [Theory]
    [MemberData(nameof(Leaves))]
    public void EveryLeaf_PlaysOut_AndReplaysTheSame(string spec, string? fork, int level)
    {
        Hero hero = Progression.Built(new Hero("Ondine", "female-a", "guard", Level: level, Spec: spec, Fork: fork), Real);
        Assert.Null(hero.Problem(Real));
        var fight = new Fight(Real, "training", 5, hero);
        Ai.PlayOut(fight);
        Assert.True(fight.IsOver);
        FightRecord record = FightRecord.FromJson(FightRecord.Of(fight).ToJson());
        Assert.Equal(Fingerprint(fight), Fingerprint(record.Replay(Real)));
    }
}
