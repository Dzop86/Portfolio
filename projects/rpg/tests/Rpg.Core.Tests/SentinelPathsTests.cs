using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The Sentinel's three branches and six lineages (sprint 191, D95, docs/elements-specialisations.md §v4.4), evolution by evolution, on the real data
/// and small boards; with the bricks they ask for: a creature fed by its summoner (E), a spell bound to a creature (F), several glyphs of a spell (G),
/// a compound's glyph that lights nothing (H), the Feint (I).
/// </summary>
public class SentinelPathsTests
{
    private static readonly Spell Blow = new("t-blow", Text, 1, 1, 1, true, false, 10, 10, 9);
    private static readonly Spell Far = new("t-far", Text, 1, 2, 9, false, false, 10, 10, 9);

    private static Hero Sentinel(string? spec, string? fork, int level, params string[] deck) =>
        new("Ondine", "female-a", "sentinel", Level: level, Stats: new Characteristics(Earth: 90, Fire: 20), Spec: spec, Fork: fork, Deck: deck.Length == 0 ? null : deck);

    /// <summary>A board where the hero plays first against the given monsters.</summary>
    private static Fight On(string[] rows, Hero hero, params FighterSpec[] monsters)
    {
        FighterSpec seat = Spec(0, initiative: 9999);
        var data = new GameData([.. Real.Spells.Values, Blow, Far], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", [seat, .. monsters])], Real.Classes,
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values, specs: Real.Specs.Values);
        return new Fight(data, "s", 1, hero);
    }

    private static Spell Compiled(Hero hero, string spell) => new Fight(Real, "training", 1, hero with { Deck = [spell] }).HeroFighter!.Spells.Single(s => s.Id == spell);

    private static FighterSpec Foe(int start = 0, int hp = 900, params string[] spells) => Spec(1, start, hp: hp, ap: 9, mp: 3, initiative: 1, spells: spells);

    [Fact]
    public void EveryLeaf_HasItsThreeSpells_TwoAtThirty_OneAtFifty()
    {
        foreach (Specialisation s in Real.Specs.Values.Where(s => s.Class == "sentinel"))
        {
            Assert.Equal(2, s.Base.Spells!.Count);
            Assert.All(s.Forks, f =>
            {
                Assert.Single(f.Tiers[0].Spells!);
                Assert.Null(f.Tiers[1].Spells);
                Assert.Equal(3, s.SpellsWith(f.Id).Count());
            });
        }
        Assert.Equal("Dernière semonce", Specs.TitleOf(Sentinel("sightline", "marksman", 100), Real)!.Fr);
        Assert.Equal("Sentinelle de l'Affût", Specs.TitleOf(Sentinel("ambush", null, 40), Real)!.Fr);
    }

    [Fact]
    public void TheCalmEye_LightsWithNoEnemyNear_ThenLonger()
    {
        static int? Bonus(Hero hero, int cells)
        {
            Fight f = On(["A" + new string('.', cells - 1) + "B"], hero, Foe());
            return f.Current.Statuses.FirstOrDefault(s => s.Stat == Stat.Damage)?.Value;
        }
        Assert.Equal(15, Bonus(Sentinel("sightline", null, 30), 4));
        Assert.Null(Bonus(Sentinel("sightline", null, 30), 3));
        Assert.Equal(25, Bonus(Sentinel("sightline", "marksman", 100), 4));
        Assert.Equal(15, Bonus(Sentinel("sightline", "barrage", 50), 3));
        Assert.Null(Bonus(Sentinel("sightline", "barrage", 50), 2));
        Assert.Null(Bonus(Sentinel(null, null, 50), 9));
        Assert.Equal(["spec-calm-eye-near"], Specs.PassivesOf(Sentinel("sightline", "barrage", 50), Real));
        Assert.Equal(["spec-calm-eye-long"], Specs.PassivesOf(Sentinel("sightline", "marksman", 100), Real));
    }

    [Fact]
    public void TheSightline_ReachesFarther_InSight_MoreFromSixCells()
    {
        // Stone Arrow reaches 6, Gale needs no sight.
        Assert.Equal(7, Compiled(Sentinel("sightline", null, 50), "stone-arrow").MaxRange);
        Assert.Equal(8, Compiled(Sentinel("sightline", "marksman", 50), "stone-arrow").MaxRange);
        Assert.Equal(7, Compiled(Sentinel("sightline", "barrage", 50), "stone-arrow").MaxRange);
        Assert.Equal(Real.Spells["gale"].MaxRange, Compiled(Sentinel("sightline", "marksman", 50), "gale").MaxRange);
    }

    [Fact]
    public void AtAHundred_TheAimedShot_TouchesTheCellBehind_AndTheBarrage_LeavesAGlyph()
    {
        Assert.Equal(AreaShape.Point, Compiled(Sentinel("sightline", "marksman", 50), "path-aimed-shot").Zone.Shape);
        Assert.Equal(new Area(AreaShape.Line, 1), Compiled(Sentinel("sightline", "marksman", 100), "path-aimed-shot").Zone);
        Spell barrage = Compiled(Sentinel("sightline", "barrage", 100), "path-barrage");
        Assert.Equal(1, barrage.Cooldown);
        GlyphEffect glyph = barrage.AllEffects.OfType<GlyphEffect>().Single();
        Assert.Equal((1, Stat.Mp, -1), (glyph.Turns, glyph.Status, glyph.Value));
        Assert.Empty(Compiled(Sentinel("sightline", "barrage", 50), "path-barrage").AllEffects.OfType<GlyphEffect>());
    }

    [Fact]
    public void TheWeakSpot_TakesAResistance_OffAnyoneStruckAfter()
    {
        Fight f = On(["A..B......"], Sentinel("sightline", null, 40, "path-weak-spot"), Foe() with { Resistances = new Dictionary<Element, int> { [Element.Earth] = 30 } });
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-weak-spot", new Cell(3, 0))));
        Assert.Equal(15, f.Fighters[1].Resistance(Element.Earth));
    }

    [Fact]
    public void AClassHawk_KeepsItsNumbers_AFedOne_ASharesOfItsSummoner_InItsElement()
    {
        static Fighter Called(Hero hero)
        {
            Fight f = On(["A.........B"], hero, Foe());
            Assert.Equal(ActionError.None, f.Apply(new CastAction("hawk", new Cell(1, 0))));
            return f.Fighters.Single(x => x.SummonKind == "hawk");
        }
        Fighter plain = Called(Sentinel(null, null, 50, "hawk"));
        Assert.Equal(30, plain.MaxHp);
        Assert.Equal(Element.Air, plain.Spells.Single().Element);
        Assert.Equal(Characteristics.None, plain.Spec.Characteristics);

        Hero ambush = Sentinel("ambush", null, 50, "hawk", "stone-arrow");
        Fight fight = On(["A.........B"], ambush, Foe());
        Fighter fed = Called(ambush);
        Assert.Equal(fight.HeroFighter!.MaxHp * 12 / 100, fed.MaxHp);
        Assert.Equal(45, fed.Spec.Characteristics.Earth);
        Assert.Equal(0, fed.Spec.Characteristics.Vitality);
        Assert.Equal(Element.Earth, fed.Spells.Single().Element);

        Assert.Equal(fight.HeroFighter.MaxHp * 20 / 100, Called(Sentinel("ambush", "falconer", 50, "hawk", "stone-arrow")).MaxHp);
    }

    [Fact]
    public void TheDive_WaitsForAHawk_AndTheChangeover_AimsOnlyAtIt()
    {
        Fight f = On(["A....B....."], Sentinel("ambush", "falconer", 50, "hawk", "path-dive", "path-changeover"), Foe());
        f.Fighters[0].Ap = 30;
        Assert.Equal(ActionError.NoSummon, f.Check(new CastAction("path-dive", new Cell(5, 0))));
        Assert.Equal(ActionError.NoValidTarget, f.Check(new CastAction("path-changeover", new Cell(5, 0))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("hawk", new Cell(1, 0))));
        Assert.Equal(ActionError.None, f.Check(new CastAction("path-dive", new Cell(5, 0))));
        Assert.Equal(ActionError.NoValidTarget, f.Check(new CastAction("path-changeover", new Cell(5, 0))));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-changeover", new Cell(1, 0))));
        Assert.Equal(new Cell(1, 0), f.Fighters[0].Cell);
        Assert.Equal(new Cell(0, 0), f.Fighters.Single(x => x.SummonKind == "hawk").Cell);
    }

    [Fact]
    public void AnotherFightersHawk_DoesNotCount()
    {
        Fight f = On(["A....B....."], Sentinel("ambush", null, 50, "path-dive"), Foe() with { Spells = ["hawk"] });
        f.Apply(new EndTurnAction());
        Assert.Equal(ActionError.None, f.Apply(new CastAction("hawk", new Cell(4, 0))));
        f.Apply(new EndTurnAction());
        Assert.NotEqual(f.Fighters[0], f.Current);
    }

    [Fact]
    public void TwinWings_TwoHawksAtOnce_AndADiveEachTurn()
    {
        Assert.Equal(1, Compiled(Sentinel("ambush", "falconer", 50), "hawk").AllEffects.OfType<SummonEffect>().Single().Max);
        Assert.Equal(2, Compiled(Sentinel("ambush", "falconer", 100), "hawk").AllEffects.OfType<SummonEffect>().Single().Max);
        Spell dive = Compiled(Sentinel("ambush", "falconer", 100), "path-dive");
        Assert.Equal((0, 1), (dive.Cooldown, dive.PerTurn));
    }

    [Fact]
    public void TheAmbush_KeepsItsHawkInTheAutomaticDeck_AndItsGlyphsLastATurnMore()
    {
        Assert.Contains("hawk", Sentinel("ambush", null, 40).DeckOf(Real));
        Spell jaws = Compiled(Sentinel("ambush", null, 40), "path-jaws");
        GlyphEffect glyph = jaws.AllEffects.OfType<GlyphEffect>().Single();
        Assert.Equal(3, glyph.Turns);
        Assert.Equal(Element.Neutral, Real.Spells["path-jaws"].AllEffects.OfType<GlyphEffect>().Single().Element);
    }

    [Fact]
    public void AnAdaptiveGlyph_TakesTheElement_ACompoundAtFourFifths()
    {
        Spell jaws = Real.Spells["path-jaws"];
        Assert.Equal((Element.Earth, 11, 15), Of(Specs.Adapted(jaws, Element.Earth)));
        Assert.Equal((Element.Ice, 9, 12), Of(Specs.Adapted(jaws, Element.Ice)));
        static (Element, int, int) Of(Spell s) => s.AllEffects.OfType<GlyphEffect>().Single() is var g ? (g.Element, g.DamageMin, g.DamageMax) : default;
    }

    [Fact]
    public void ACompoundsGlyph_DoesNotLastLonger()
    {
        Spell twin = Real.Spells.Values.First(s => s.Twin is not null && s.Element == Element.Ash && s.AllEffects.Any(e => e is GlyphEffect));
        int before = twin.AllEffects.OfType<GlyphEffect>().Single().Turns;
        Assert.Equal(before, Talents.Compile(twin, [Specs.AsTalent([new GlyphTurnsRule(1)])]).AllEffects.OfType<GlyphEffect>().Single().Turns);
    }

    [Fact]
    public void TheTrapper_ComesBackSooner_ThenLaysTwoJaws_TheThirdTakingTheOldestsPlace()
    {
        Assert.Equal(1, Compiled(Sentinel("ambush", "trapper", 50), "path-jaws").Cooldown);
        Fight f = On(["A........B."], Sentinel("ambush", "trapper", 100, "path-jaws"), Foe());
        f.Fighters[0].Ap = 30;
        for (int i = 2; i <= 4; i++)
        {
            Assert.Equal(ActionError.None, f.Apply(new CastAction("path-jaws", new Cell(i, 0))));
            f.Fighters[0].Cooldowns.Clear();
            f.Fighters[0].CastsThisTurn.Clear();
        }
        Assert.Equal([new Cell(3, 0), new Cell(4, 0)], f.Glyphs.Select(g => g.Cells[0]));
    }

    [Fact]
    public void ASecondGlyph_OfASpellWithoutMax_ReplacesTheFirst()
    {
        Fight f = On(["A........B."], Sentinel("ambush", "trapper", 50, "path-jaws"), Foe());
        f.Apply(new CastAction("path-jaws", new Cell(2, 0)));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-jaws", new Cell(3, 0))));
        Assert.Equal(new Cell(3, 0), f.Glyphs.Single().Cells[0]);
    }

    [Fact]
    public void TheDeadfall_HitsHarderInItsGlyphs_NotInACompoundsOwn()
    {
        Spell ash = Real.Spells.Values.First(s => s.Twin is not null && s.Element == Element.Ash && s.AllEffects.Any(e => e is GlyphEffect) && s.Zone.Radius == 0 && s.MaxRange >= 3);
        Hero hero = Sentinel("ambush", "trapper", 100, "path-jaws", ash.Id) with
        {
            Compounds = [Element.Ash],
            Stats = new Characteristics(Earth: 40, Fire: 40, Air: 40),
        };
        Fight f = On(["A..B......."], hero, Foe());
        Fighter foe = f.Fighters[1];
        Assert.Equal(ActionError.None, f.Apply(new CastAction(ash.Id, foe.Cell)));
        Assert.False(f.ContextOf(f.HeroFighter!, null, f.HeroFighter!.Cell, foe).InMyGlyph);
        f.Fighters[0].Ap = 9;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-jaws", foe.Cell)));
        Assert.True(f.ContextOf(f.HeroFighter!, null, f.HeroFighter!.Cell, foe).InMyGlyph);
        Assert.Equal(15, Talents.Percent(f.HeroFighter!.Talents, f.HeroFighter.Cell, foe.Cell, Element.Earth, new HitContext(InMyGlyph: true)));
    }

    [Fact]
    public void ABait_TakesAnActionPoint_FromWhoStartsItsTurnThere()
    {
        Fight f = On(["A..B......."], Sentinel("ambush", "trapper", 50, "path-bait"), Foe());
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-bait", new Cell(3, 0))));
        // Standing there as it was laid, spared for one turn.
        f.Apply(new EndTurnAction());
        Assert.Equal(9, f.Fighters[1].Ap);
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(8, f.Fighters[1].Ap);
    }

    [Fact]
    public void TheQuickHeels_GivePoints_WhenStruckUpClose_ThreeAtAHundred()
    {
        foreach ((Hero hero, int more) in new[] { (Sentinel("trail", null, 40, "stone-arrow"), 2), (Sentinel("trail", "leaper", 100, "stone-arrow"), 3) })
        {
            Fight f = On(["AB........."], hero, Foe(spells: ["t-blow"]));
            int mp = f.Fighters[0].Spec.Mp;
            f.Apply(new EndTurnAction());
            Assert.Equal(ActionError.None, f.Apply(new CastAction("t-blow", f.Fighters[0].Cell)));
            f.Apply(new EndTurnAction());
            Assert.Equal(mp + more, f.Fighters[0].Mp);
        }
    }

    [Fact]
    public void SawItComing_ShieldsOnce_AFight()
    {
        Fight f = On(["AB........."], Sentinel("trail", "leaper", 100, "stone-arrow"), Foe(spells: ["t-blow"]));
        f.Apply(new EndTurnAction());
        f.Apply(new CastAction("t-blow", f.Fighters[0].Cell));
        Assert.Equal(40, f.Fighters[0].Shield);
        f.Apply(new CastAction("t-blow", f.Fighters[0].Cell));
        Assert.Equal(1, f.Events.OfType<PassiveTriggered>().Count(p => p.Passive == "spec-saw-it-coming"));
        Assert.DoesNotContain("spec-saw-it-coming", Specs.PassivesOf(Sentinel("trail", "leaper", 50), Real));
    }

    [Fact]
    public void AFeint_CostsTheNextToStrikeUpClose_OnceOnly_NotFromAfar()
    {
        Fight f = On(["AB........."], Sentinel("trail", "leaper", 50, "path-feint"), Foe(spells: ["t-blow", "t-far"]));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-feint", f.Fighters[0].Cell)));
        Assert.Contains(f.Fighters[0].Statuses, s => s.Stat == Stat.Feint);
        f.Apply(new EndTurnAction());
        Fighter foe = f.Fighters[1];
        foe.Cell = new Cell(5, 0);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-far", f.Fighters[0].Cell)));
        Assert.Contains(f.Fighters[0].Statuses, s => s.Stat == Stat.Feint);
        foe.Cell = new Cell(1, 0);
        int mp = foe.Mp;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-blow", f.Fighters[0].Cell)));
        Assert.Equal(mp - 2, foe.Mp);
        Assert.DoesNotContain(f.Fighters[0].Statuses, s => s.Stat == Stat.Feint);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("t-blow", f.Fighters[0].Cell)));
        Assert.Equal(mp - 2, foe.Mp);
    }

    [Fact]
    public void TheVault_LeapsWithoutSight_ThenWithoutACooldown_ThenGivesDamage()
    {
        Assert.False(Compiled(Sentinel("trail", null, 40), "path-vault").LineOfSight);
        Assert.Equal(1, Compiled(Sentinel("trail", null, 40), "path-vault").Cooldown);
        Spell at50 = Compiled(Sentinel("trail", "leaper", 50), "path-vault");
        Assert.Equal((0, 1), (at50.Cooldown, at50.PerTurn));
        Assert.Empty(at50.AllEffects.OfType<StatusEffect>());
        StatusEffect damage = Compiled(Sentinel("trail", "leaper", 100), "path-vault").AllEffects.OfType<StatusEffect>().Single();
        Assert.Equal((Stat.Damage, 10, 1), (damage.Stat, damage.Value, damage.Turns));
    }

    [Theory]
    [InlineData(50, 1, 3)]
    [InlineData(50, 5, 15)]
    [InlineData(50, 9, 15)]
    [InlineData(100, 1, 5)]
    [InlineData(100, 5, 25)]
    [InlineData(100, 9, 25)]
    public void TheRunner_HitsHarder_ByTheCellsWalked(int level, int cells, int percent)
    {
        TalentSpec path = Specs.AsTalent(Specs.RulesOf(Sentinel("trail", "runner", level), Real));
        Assert.Equal(percent, Talents.Percent([path], new Cell(0, 0), new Cell(3, 0), Element.Earth, new HitContext(CellsSinceSpell: cells)));
    }

    [Fact]
    public void TheNeverCaught_LeavesAnEcho_FromAHundred()
    {
        Assert.Empty(Specs.RulesOf(Sentinel("trail", "runner", 50), Real).OfType<EchoRule>());
        Assert.Equal(new EchoRule(50, 3), Specs.RulesOf(Sentinel("trail", "runner", 100), Real).OfType<EchoRule>().Single());
    }

    [Fact]
    public void TheRunningShot_GivesMovementPoints_ForTheTurn()
    {
        Fight f = On(["A...B......"], Sentinel("trail", "runner", 50, "path-running-shot", "stone-arrow"), Foe());
        int mp = f.Fighters[0].Mp;
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-running-shot", new Cell(4, 0))));
        Assert.Equal(mp + 2, f.Fighters[0].Mp);
        Assert.Equal(Element.Earth, f.Fighters[0].Spells.Single(s => s.Id == "path-running-shot").Element);
    }

    [Fact]
    public void ChangingLineage_HandsItsRankOn_AndItsPlaceInTheDeck()
    {
        Hero hero = Sentinel("sightline", "marksman", 60) with { Ranks = new Dictionary<string, int> { ["path-bullseye"] = 3, ["path-aimed-shot"] = 2 }, Deck = ["path-aimed-shot", "path-bullseye"] };
        Hero moved = Specs.Transferred(hero, "sightline", "barrage", Real);
        Assert.Equal(3, moved.Ranks!["path-barrage"]);
        Assert.Equal(2, moved.Ranks["path-aimed-shot"]);
        Assert.Equal(["path-aimed-shot", "path-barrage"], moved.Deck!);
        Assert.Null(moved.Problem(Real));
    }

    public static TheoryData<string, string?, int> Leaves()
    {
        var leaves = new TheoryData<string, string?, int>();
        foreach (Specialisation s in Real.Specs.Values.Where(s => s.Class == "sentinel"))
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
        Hero hero = Progression.Built(new Hero("Ondine", "female-a", "sentinel", Level: level, Spec: spec, Fork: fork), Real);
        Assert.Null(hero.Problem(Real));
        var fight = new Fight(Real, "training", 5, hero);
        Ai.PlayOut(fight);
        Assert.True(fight.IsOver);
        FightRecord record = FightRecord.FromJson(FightRecord.Of(fight).ToJson());
        Assert.Equal(Fingerprint(fight), Fingerprint(record.Replay(Real)));
    }
}
