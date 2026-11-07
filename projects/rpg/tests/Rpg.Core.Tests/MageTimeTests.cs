using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Mage's first half of four ranks (sprint 199, docs/classes-mage.md): the three effects of time, each on the arena (points borrowed from the next turn, a place in
/// the order of play, cooldowns shortened), then the Arcanist's and the Chronomancer's nodes.
/// </summary>
public class MageTimeTests
{
    private static readonly Spell Late = new("t-late", Text, 1, 1, 9, false, false, 0, 0, 9, Effects: [new DelayEffect(Affects.Enemies, 1)]);
    private static readonly Spell Soon = new("t-soon", Text, 1, 0, 9, false, false, 0, 0, 9, Effects: [new DelayEffect(Affects.Allies, -2)]);
    private static readonly Spell Rush = new("t-rush", Text, 1, 0, 9, false, false, 0, 0, 9, Effects: [new HasteEffect(Affects.Allies, 1)]);
    private static readonly Spell Wait = new("t-wait", Text, 1, 0, 0, false, false, 0, 0, 9, Cooldown: 3);

    private static Hero Mage(string? spec, string? fork, int level = 60, params string[] deck) =>
        new("Ondine", "female-a", "mage", Level: level, Stats: new Characteristics(Fire: 90), Spec: spec, Fork: fork, Deck: deck.Length == 0 ? null : deck);

    private static GameData With(string[] rows, FighterSpec[] fighters) =>
        new([.. Real.Spells.Values, Late, Soon, Rush, Wait], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], Real.Classes,
            summons: Real.Summons.Values, items: Real.Items.Values, sets: Real.Sets.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);

    /// <summary>The hero plays first, then the monsters by their initiative.</summary>
    private static Fight On(string[] rows, Hero hero, params FighterSpec[] monsters) => new(With(rows, [Spec(0, initiative: 9999), .. monsters]), "s", 1, hero);

    private static Fight Monsters(params FighterSpec[] fighters) => new(With(["AA.BBB....", ".........."], fighters), "s", 1);

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    private static int[] Order(Fight f) => [.. f.TurnOrder.Select(x => x.Id)];

    // ---- The three effects (§3) ----

    [Fact]
    public void BorrowedTime_GivesItsPointsAtOnce_AndTakesThemBackAtTheNextTurn()
    {
        Fight f = On(["A.....B..."], Mage("chronomancer", null, 40, "path-borrowed-time"), Spec(1, hp: 900, initiative: 1));
        Fighter me = f.HeroFighter!;
        int ap = me.Ap;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-borrowed-time", me.Cell)));
        Assert.Equal(ap - 1 + 3, me.Ap);
        // The two points left are kept by the Chronomancer's reserve, the three borrowed are paid back.
        me.Ap = 0;
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(me, f.Current);
        // Its +1 a turn, less the three.
        Assert.Equal(me.Spec.Ap + 1 - 3, me.Ap);
    }

    [Fact]
    public void ADelay_PutsAnEnemyLater_AsFarAsTheRoundGoes_NeverPastTheOneWhoPlays()
    {
        // Initiatives 40, 30, 20, 10: A plays, then B, C, D.
        Fight f = Monsters(Spec(0, 0, initiative: 40, spells: ["t-late"]), Spec(1, 0, initiative: 30), Spec(1, 1, initiative: 20), Spec(1, 2, initiative: 10));
        Assert.Equal([0, 1, 2, 3], Order(f));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-late", f.Fighters[1].Cell)));
        Assert.Equal([0, 2, 1, 3], Order(f));
        Assert.Contains(f.Events, e => e is OrderChanged { Fighter: 1, Places: 1 });
        // As far as the round goes: D last, it stays last.
        f.Fighters[0].CastsThisTurn.Clear();
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-late", f.Fighters[3].Cell)));
        Assert.Equal([0, 2, 1, 3], Order(f));
        // A fighter that has played stays before the one playing: C plays, and A, delayed by it, stays first.
        f.Apply(new EndTurnAction());
        Assert.Equal(f.Fighters[2], f.Current);
        Assert.Equal([2, 1, 3, 0], f.NextTurns(4).Select(x => x.Id));
    }

    [Fact]
    public void ADelay_OnTheOneWhoPlays_OrPastTheDead_ChangesNothingWrong()
    {
        Fight f = Monsters(Spec(0, 0, initiative: 40, spells: ["t-late", "t-soon"]), Spec(1, 0, hp: 1, initiative: 30), Spec(1, 1, initiative: 20), Spec(0, 1, initiative: 10));
        f.Fighters[2].Hp = 0;
        // B dead: C passes over it? C was after B; B, dead, is skipped: C goes one living place later, after D.
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-late", f.Fighters[1].Cell)));
        Assert.Equal([0, 2, 3, 1], Order(f));
        // An ally hurried by two places: D, yet to play, would only pass the dead C, which counts for nothing; it never comes before A.
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-soon", f.Fighters[3].Cell)));
        Assert.Equal([0, 2, 3, 1], Order(f));
        Assert.DoesNotContain(f.Events, e => e is OrderChanged { Fighter: 3 });
        Assert.Equal([0, 3, 1], f.NextTurns(3).Select(x => x.Id));
    }

    [Fact]
    public void AHaste_ShortensAnAllysCooldowns_OneDownToNothingIsOver()
    {
        Fight f = Monsters(Spec(0, 0, initiative: 40, spells: ["t-rush"]), Spec(0, 1, initiative: 30, spells: ["t-wait"]), Spec(1, 0, initiative: 10));
        f.Fighters[1].Cooldowns["t-wait"] = 2;
        f.Fighters[1].Cooldowns["t-other"] = 1;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-rush", f.Fighters[1].Cell)));
        Assert.Equal(1, f.Fighters[1].Cooldowns["t-wait"]);
        Assert.False(f.Fighters[1].Cooldowns.ContainsKey("t-other"));
    }

    [Fact]
    public void TheEffectsOfTime_OutOfTheirBounds_AreRefused()
    {
        GameData Made(SpellEffect e) => new([new Spell("t-bad", Text, 1, 1, 1, true, false, 0, 0, 1, Effects: [e])], [new MapSpec("m", Text, ["AB"])],
            [new Scenario("s", Text, "m", [Spec(0), Spec(1)])]);
        Assert.NotNull(Made(new BorrowEffect(Affects.Caster, 2)));
        Assert.Throws<InvalidDataException>(() => Made(new BorrowEffect(Affects.Enemies, 2)));
        Assert.Throws<InvalidDataException>(() => Made(new BorrowEffect(Affects.Caster, 5)));
        Assert.Throws<InvalidDataException>(() => Made(new DelayEffect(Affects.Enemies, 0)));
        Assert.Throws<InvalidDataException>(() => Made(new DelayEffect(Affects.Caster, 1)));
        Assert.Throws<InvalidDataException>(() => Made(new HasteEffect(Affects.Allies, 4)));
    }

    // ---- The nodes (§1, §2) ----

    [Fact]
    public void TheTree_IsArcanistIllusionistChronomancer_EachLineageWithItsThreeSpells()
    {
        Assert.Equal(["Arcaniste", "Illusionniste", "Chronomancien"], Real.Specs.Values.Where(s => s.Class == "mage").Select(s => s.Name.Fr));
        Assert.Equal(["rune-weaver", "scholar", "mirage-master", "mentalist", "time-weaver", "distorter"], Real.Specs.Values.Where(s => s.Class == "mage").SelectMany(s => s.Forks).Select(f => f.Id));
        // The Illusionist's since sprint 200.
        Assert.All(Real.Specs.Values.Where(s => s.Class == "mage").Select(s => s.Id), id => Assert.All(Real.Specs[id].Forks, f => Assert.Equal(3, Real.Specs[id].SpellsWith(f.Id).Count())));
    }

    [Fact]
    public void TheArcanist_GathersAPoint_WhenItStoodStill_AndItsCometComesBackSooner()
    {
        Fight f = On(["A.......B."], Mage("arcanist", null, 40), Spec(1, hp: 900, initiative: 1));
        Fighter me = f.HeroFighter!;
        Assert.Equal(me.Spec.Ap + 1, me.Ap);
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(me.Spec.Ap + 1, me.Ap);
        Assert.Equal(ActionError.None, f.Apply(new MoveAction(new Cell(1, 0))));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(me.Spec.Ap, me.Ap);
        Assert.Equal(2, Compiled(Mage("arcanist", null, 40), "path-comet").Cooldown);
        Assert.Equal(3, Real.Spells["path-comet"].Cooldown);
    }

    [Fact]
    public void TheRune_MarksTheGround_AndTheArcaneShaperHitsHarderOnIt()
    {
        GlyphEffect rune = Real.Spells["path-rune"].AllEffects.OfType<GlyphEffect>().Single();
        Assert.True(rune.Marks);
        Assert.Equal((Affects.Enemies, 10, 14), (rune.Affects, rune.DamageMin, rune.DamageMax));
        TalentSpec path = Specs.AsTalent(Specs.RulesOf(Mage("arcanist", "rune-weaver"), Real));
        Assert.Equal(10, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext(InMyGlyph: true)));
        Assert.Equal(0, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext()));
    }

    [Fact]
    public void TheScholar_HitsHarderWithItsBigSpells_AndItsRayStrikesALineOfFour()
    {
        TalentSpec path = Specs.AsTalent(Specs.RulesOf(Mage("arcanist", "scholar"), Real));
        Assert.Equal(10, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext(ApCost: 7)));
        Assert.Equal(0, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Fire, new HitContext(ApCost: 5)));
        Spell ray = Real.Spells["path-ray"];
        Assert.Equal((new Area(AreaShape.Line, 3), true, false), (ray.Zone, ray.InLine, ray.LineOfSight));
        Assert.Equal(4, ray.Zone.Cells(new Cell(0, 0), new Cell(1, 0)).Count());
    }

    [Fact]
    public void TheChronomancer_KeepsTwoPoints_TheTimeWeaverThree()
    {
        Assert.Equal(2, Talents.Reserve([Specs.AsTalent(Specs.RulesOf(Mage("chronomancer", null, 40), Real))], 8));
        Assert.Equal(3, Talents.Reserve([Specs.AsTalent(Specs.RulesOf(Mage("chronomancer", "time-weaver"), Real))], 8));
        Assert.Equal(2, Compiled(Mage("chronomancer", "distorter"), "path-borrowed-time").Cooldown);
    }

    [Fact]
    public void TheOffbeat_StrikesAndPutsTheEnemyLater()
    {
        Fight f = On(["A...BB...."], Mage("chronomancer", null, 40, "path-offbeat"), Spec(1, 0, hp: 900, initiative: 2), Spec(1, 1, hp: 900, initiative: 1));
        int hp = f.Fighters[1].Hp;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-offbeat", f.Fighters[1].Cell)));
        Assert.True(f.Fighters[1].Hp < hp);
        Assert.Equal([0, 2, 1], Order(f));
    }

    [Fact]
    public void TheQuicken_HurriesAnAlly_AndGivesItsCooldownsBack()
    {
        // The Mage first (boots of 25 initiative), the monsters, then its ally.
        Hero ally = new("Brume", "female-b", "warrior", Level: 60);
        Hero mage = Mage("chronomancer", "time-weaver", deck: ["path-quicken"]) with { Worn = new Dictionary<Slot, string> { [Slot.Boots] = "bee-boots" } };
        Fight f = new(With(["AA........", "...BB....."], [Spec(0, 0, initiative: 9999), Spec(1, 0, hp: 900, initiative: 20), Spec(1, 1, hp: 900, initiative: 15)]),
            "s", 1, mage, allies: [ally]);
        Fighter brume = f.Fighters.Single(x => x.Name.Fr == "Brume");
        brume.Cooldowns["x"] = 3;
        Assert.Equal(f.TurnOrder.Count - 1, f.TurnOrder.ToList().IndexOf(brume));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-quicken", brume.Cell)));
        Assert.Equal(f.TurnOrder.Count - 2, f.TurnOrder.ToList().IndexOf(brume));
        Assert.Equal(2, brume.Cooldowns["x"]);
    }

    [Fact]
    public void TheCrescendo_AtTenPoints_IsWithinReach_OfTheReserveAndTheLoan()
    {
        Spell crescendo = Real.Spells["path-crescendo"];
        Assert.Equal((10, new Area(AreaShape.Circle, 2)), (crescendo.ApCost, crescendo.Zone));
        Fight f = On(["A....B...."], Mage("chronomancer", "distorter", deck: ["path-borrowed-time", "path-crescendo"]), Spec(1, hp: 900, initiative: 1));
        Fighter me = f.HeroFighter!;
        Assert.True(me.Ap < 10);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-borrowed-time", me.Cell)));
        Assert.True(me.Ap >= 10, $"{me.Ap} AP");
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-crescendo", f.Fighters[1].Cell)));
    }

    public static TheoryData<string, string?, int> Leaves()
    {
        var leaves = new TheoryData<string, string?, int>();
        foreach (string id in Real.Specs.Values.Where(s => s.Base.Spells is not null && s.Class == "mage").Select(s => s.Id))
        {
            leaves.Add(id, null, 40);
            foreach (SpecFork f in Real.Specs[id].Forks)
                leaves.Add(id, f.Id, 60);
        }
        return leaves;
    }

    [Theory]
    [MemberData(nameof(Leaves))]
    public void EveryLeaf_PlaysOut_AndReplaysTheSame(string spec, string? fork, int level)
    {
        Hero hero = Progression.Built(new Hero("Ondine", "female-a", "mage", Level: level, Spec: spec, Fork: fork), Real);
        Assert.Null(hero.Problem(Real));
        var fight = new Fight(Real, "training", 5, hero);
        Ai.PlayOut(fight);
        Assert.True(fight.IsOver);
        FightRecord record = FightRecord.FromJson(FightRecord.Of(fight).ToJson());
        Assert.Equal(Fingerprint(fight), Fingerprint(record.Replay(Real)));
    }
}
