using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The paths' engine (sprint 186, D93, docs/elements-specialisations.md §v3.4): the nine branches of the data, a hero who has not
/// chosen, the three evolutions that come alone with the level, the trigger <see cref="Trigger.Kill"/>, the replay. Their spells
/// and passives come in sprints 191 to 193: the engine is tried on two paths written here, over the real data.
/// </summary>
public class SpecsTests
{
    private static readonly Spell Shot = new("t-shot", Text, 3, 2, 6, true, false, 10, 14, 1, Level: 30, Adaptive: true);
    private static readonly Spell Calm = new("t-calm", Text, 2, 0, 0, false, false, 0, 0, 1, Level: 30);
    private static readonly Spell Volley = new("t-volley", Text, 4, 2, 6, true, false, 12, 16, 1, Level: 50, Adaptive: true);
    private static readonly Spell Near = new("o-near", Text, 3, 1, 2, true, false, 10, 14, 1, Level: 30);
    private static readonly Spell Step = new("o-step", Text, 2, 2, 4, true, false, 0, 0, 1, Effects: [new LeapEffect(Affects.Caster)], Level: 30);
    private static readonly Spell Hush = new("t-hush", Text, 2, 1, 4, true, false, 5, 6, 1, Level: 50);
    private static readonly Spell PlainShot = new("o-plain-shot", Text, 2, 1, 4, true, false, 5, 6, 1, Level: 50);
    private static readonly Spell Trap = new("o-trap", Text, 2, 1, 4, true, false, 0, 0, 1, Effects: [new GlyphEffect(Affects.Enemies, 2, 5, 6, Element.Earth)], Level: 50);

    private static readonly PassiveSpec Held = new("t-held", Text, Text, Trigger.TurnStart, [new StatusEffect(Affects.Caster, Stat.Damage, 15, 1)], Clear: 3);
    private static readonly PassiveSpec Steady = new("t-steady", Text, Text, Trigger.TurnStart, [new StatusEffect(Affects.Caster, Stat.Damage, 25, 2)], Clear: 3);
    private static readonly PassiveSpec Run = new("t-run", Text, Text, Trigger.Kill, [new StatusEffect(Affects.Caster, Stat.Mp, 1, 1)]);

    private static SpecTier Tier(int level, string[]? spells = null, string[]? passives = null, string[]? replaces = null, TalentRule[]? rules = null) =>
        new(level, new LocalizedText($"titre {level}", $"title {level}"), Text, passives, replaces, rules, spells);

    private static readonly SpecFork Quiet = new("t-quiet", [Tier(50, passives: ["t-run"]), Tier(100)]);

    private static readonly Specialisation Aim = new("t-aim", "archer", Text, Tier(30, ["t-shot", "t-calm"], ["t-held"], rules: [new RangeRule(1, 2, Sight: true)]),
        [new SpecFork("t-loud", [Tier(50, ["t-volley"], ["t-run"]), Tier(100, passives: ["t-steady"], replaces: ["t-held"])]), new SpecFork("t-quiet", [Tier(50, ["t-hush"]), Tier(100)])],
        Keeps: ["hawk"]);

    private static readonly Specialisation Other = new("o-other", "archer", Text, Tier(30, ["o-near", "o-step"], rules: [new LeapSightRule()]),
        [new SpecFork("o-traps", [Tier(50, ["o-trap"], rules: [new GlyphTurnsRule(1)]), Tier(100)]), new SpecFork("o-plain", [Tier(50, ["o-plain-shot"]), Tier(100, rules: [new PatchRule("t-shot", DamagePercent: 50)])])]);

    private static GameData With(params Specialisation[] specs) =>
        new([.. Real.Spells.Values, Shot, Calm, Volley, Hush, PlainShot, Near, Step, Trap], Real.Maps.Values, Real.Scenarios.Values, Real.Classes, Real.Towns.Values, Real.Dialogues.Values, Real.Summons.Values, Real.Quests.Values,
            Real.Items.Values, Real.Sets.Values, Real.Families.Values, Real.Dungeons.Values, Real.Jobs.Values, [.. Real.Passives.Values, Held, Steady, Run], Real.Talents.Values, Real.Reactions.Values, specs);

    private static readonly GameData Two = With(Aim, Other);

    private static Hero Sentinel(string? spec, int level = 50, string? fork = null) =>
        new("Ondine", "female-a", "archer", Level: level, Stats: new Characteristics(Earth: 60, Fire: 40), Spec: spec,
            Fork: fork ?? (level >= 50 ? spec switch { "t-aim" => "t-loud", "o-other" => "o-traps", _ => null } : null));

    private static Fighter Fought(Hero hero, GameData? data = null) => new Fight(data ?? Two, "training", 1, hero).HeroFighter!;

    [Fact]
    public void TheData_HasThreeBranchesPerClass_TwoLineagesEach_FifteenEvolutions_InBothLanguages()
    {
        Assert.Equal(9, Real.Specs.Count);
        Assert.All(Real.Classes, c => Assert.Equal(3, Real.Specs.Values.Count(s => s.Class == c.Id)));
        Assert.All(Real.Specs.Values, s =>
        {
            Assert.Equal(2, s.Forks.Count);
            // A lineage ends in its evolution at 100, or in two final classes once its class is converted (sprint 197: the Warrior).
            Assert.All(s.Forks, f => Assert.Equal(f.Finals is null ? [50, 100] : [50], f.Tiers.Select(t => t.Level)));
            Assert.All(s.Forks, f => Assert.Equal(s.Class == "warrior" ? 2 : 0, f.Finals?.Count ?? 0));
            Assert.All(s.Forks.SelectMany(f => f.Tiers.Concat((f.Finals ?? []).Select(x => x.Tier))).Prepend(s.Base).SelectMany(t => new[] { t.Title, t.Description }).Append(s.Name),
                x => Assert.False(string.IsNullOrWhiteSpace(x.Fr) || string.IsNullOrWhiteSpace(x.En)));
        });
        Assert.Equal(18, Real.Specs.Values.SelectMany(s => s.Forks).Select(f => f.Id).Distinct().Count());
    }

    [Fact]
    public void BadPaths_AreRefusedByTheData()
    {
        Assert.Throws<InvalidDataException>(() => With(Aim with { Class = "dragon" }));
        Assert.Throws<InvalidDataException>(() => With(Aim with { Forks = [Aim.Forks[0]] }));
        Assert.Throws<InvalidDataException>(() => With(Aim with { Forks = [Aim.Forks[0], Quiet with { Tiers = [Tier(50), Tier(90)] }] }));
        // A leaf has 3 spells, or none while its content is to come.
        Assert.Throws<InvalidDataException>(() => With(Aim with { Forks = [Aim.Forks[0], Quiet] }));
        Assert.Throws<InvalidDataException>(() => With(Aim with { Base = Tier(30, ["t-volley", "t-calm"]) }));
        Assert.Throws<InvalidDataException>(() => With(Aim with { Base = Tier(30, ["no-spell", "t-calm"]) }));
        Assert.Throws<InvalidDataException>(() => With(Aim with { Base = Tier(30, ["t-shot", "t-calm"], ["no-passive"]) }));
        Assert.Throws<InvalidDataException>(() => With(Aim with { Base = Tier(30, ["t-shot", "t-calm"], rules: [new HpRule(10)]) }));
        Assert.Throws<InvalidDataException>(() => With(Aim with { Base = Tier(30, ["t-shot", "t-calm"], rules: [new PatchRule("no-spell")]) }));
        Assert.Throws<InvalidDataException>(() => With(Aim with { Keeps = ["fireball-of-no-class"] }));
    }

    [Fact]
    public void APath_IsRefusedBeforeThirty_OrOfAnotherClass_AndMakesAnotherHero()
    {
        Assert.Null(Sentinel("t-aim", 30).Problem(Two));
        // A lineage: of its branch, from 50, never without a branch; a branch without one stays legal.
        Assert.Null(Sentinel("t-aim", 60, "t-quiet").Problem(Two));
        Assert.Null((Sentinel("t-aim", 60) with { Fork = null }).Problem(Two));
        Assert.Equal("'o-traps' is not a lineage of 't-aim'.", Sentinel("t-aim", 60, "o-traps").Problem(Two));
        Assert.Equal("Lineages open at level 50.", Sentinel("t-aim", 49, "t-loud").Problem(Two));
        Assert.Equal("A lineage needs its branch.", (Sentinel(null) with { Fork = "t-loud" }).Problem(Two));
        Assert.NotEqual(Sentinel("t-aim", 60, "t-loud"), Sentinel("t-aim", 60, "t-quiet"));
        Assert.Equal("Paths open at level 30.", Sentinel("t-aim", 29).Problem(Two));
        Assert.Equal("Unknown path 'nowhere'.", Sentinel("nowhere").Problem(Two));
        Assert.Equal("'t-aim' is not a path of this class.", (Sentinel("t-aim") with { Class = "warrior" }).Problem(Two));
        // The server compares the hero of a record with its own: another path is another hero.
        Assert.NotEqual(Sentinel("t-aim"), Sentinel("o-other"));
        Assert.NotEqual(Sentinel("t-aim"), Sentinel(null));
        Assert.Equal(Sentinel("t-aim"), Sentinel("t-aim"));
    }

    [Fact]
    public void TheEvolutions_ComeAloneWithTheLevel_AndAll_ToWhoChoosesLate()
    {
        Assert.Equal([30], Specs.TiersOf(Sentinel("t-aim", 60) with { Fork = null }, Two).Select(t => t.Level));
        Assert.Equal(["t-shot", "t-calm", "t-hush"], Specs.SpellsOf(Sentinel("t-aim", 60, "t-quiet"), Two));
        Assert.Equal([30], Specs.TiersOf(Sentinel("t-aim", 49), Two).Select(t => t.Level));
        Assert.DoesNotContain("t-volley", Talents.GrimoireOf(Sentinel("t-aim", 49), Two).Spells);
        Assert.Equal([30, 50], Specs.TiersOf(Sentinel("t-aim", 50), Two).Select(t => t.Level));
        Assert.Contains("t-volley", Talents.GrimoireOf(Sentinel("t-aim", 50), Two).Spells);
        Assert.Equal([30, 50, 100], Specs.TiersOf(Sentinel("t-aim", 100), Two).Select(t => t.Level));
        // Staying of its class is only not having chosen yet: nothing; a hero of 60 who chooses has I and II at once.
        Assert.Empty(Specs.TiersOf(Sentinel(null, 60), Two));
        Assert.DoesNotContain("t-shot", Talents.GrimoireOf(Sentinel(null, 60), Two).Spells);
        Assert.Null(Specs.TitleOf(Sentinel(null, 60), Two));
        Assert.Equal([30, 50], Specs.TiersOf(Sentinel("t-aim", 60), Two).Select(t => t.Level));
        Assert.Contains("t-shot", Talents.GrimoireOf(Sentinel("t-aim", 60), Two).Spells);
        Assert.Contains("t-volley", Talents.GrimoireOf(Sentinel("t-aim", 60), Two).Spells);
        Assert.Equal("titre 50", Specs.TitleOf(Sentinel("t-aim", 60), Two)!.Fr);
    }

    [Fact]
    public void ALaterEvolution_TakesThePlaceOfAPassive()
    {
        Assert.Equal(["t-held"], Specs.PassivesOf(Sentinel("t-aim", 30), Two));
        Assert.Equal(["t-held", "t-run"], Specs.PassivesOf(Sentinel("t-aim", 50), Two));
        Assert.Equal(["t-run", "t-steady"], Specs.PassivesOf(Sentinel("t-aim", 100), Two));
        Assert.Equal(["t-held", "t-run"], Fought(Sentinel("t-aim", 50)).Spec.Passives!);
    }

    [Fact]
    public void ItsSpells_CloseTheAutomaticDeck_WithWhatItKeeps_AndHaveRanks()
    {
        Hero aim = Sentinel("t-aim");
        IReadOnlyList<string> deck = aim.DeckOf(Two);
        Assert.True(deck.Count <= Hero.DeckSize);
        Assert.Equal(["t-shot", "t-calm", "t-volley", "hawk"], deck.TakeLast(4));
        Assert.DoesNotContain("t-shot", Sentinel(null).DeckOf(Two));
        Assert.Null((aim with { Ranks = new Dictionary<string, int> { ["t-shot"] = 1 } }).Problem(Two));
        Assert.Equal("The spell 't-shot' is not one this hero has.", (Sentinel("o-other") with { Ranks = new Dictionary<string, int> { ["t-shot"] = 1 } }).Problem(Two));
    }

    [Fact]
    public void ItsRules_CompileIntoItsSpells()
    {
        Spell Of(Hero h, string id) => new Fight(Two, "training", 1, h with { Deck = [id] }).HeroFighter!.Spells.Single(s => s.Id == id);
        // In sight, +1 range; without its path, not.
        Assert.Equal(Real.Spells["stone-arrow"].MaxRange + 1, Of(Sentinel("t-aim"), "stone-arrow").MaxRange);
        Assert.Equal(Real.Spells["stone-arrow"].MaxRange, Of(Sentinel("o-other"), "stone-arrow").MaxRange);
        Assert.Equal(Real.Spells["stone-arrow"].MaxRange, Of(Sentinel(null), "stone-arrow").MaxRange);
        // The Sight flag: a spell that asks for none does not gain it.
        Spell blind = Talents.Compile(Shot with { LineOfSight = false }, [Specs.AsTalent([new RangeRule(1, 2, Sight: true)])]);
        Assert.Equal(Shot.MaxRange, blind.MaxRange);
        // Leaps without sight, glyphs a turn more, only from its evolution II.
        Assert.False(Of(Sentinel("o-other"), "o-step").LineOfSight);
        Assert.Equal(3, Of(Sentinel("o-other"), "o-trap").AllEffects.OfType<GlyphEffect>().Single().Turns);
        Assert.Equal(2, Talents.Compile(Trap, [Specs.AsTalent(Specs.RulesOf(Sentinel("o-other", 49), Two))]).AllEffects.OfType<GlyphEffect>().Single().Turns);
    }

    [Fact]
    public void AnAdaptiveSpell_StrikesInTheHerosStrongestElement_ACompoundAtFourFifths()
    {
        Spell[] grimoire = [Shot, Real.Spells["stone-arrow"], Real.Spells["fire-arrow"]];
        Assert.Equal(Element.Earth, Specs.AdaptiveElement(new Characteristics(Earth: 60, Fire: 40), grimoire, 0));
        Assert.Equal(Element.Fire, Specs.AdaptiveElement(new Characteristics(Earth: 40, Fire: 60), grimoire, 0));
        // A tie, the enum's order; nothing to follow, neutral.
        Assert.Equal(Element.Earth, Specs.AdaptiveElement(new Characteristics(Earth: 50, Fire: 50), grimoire, 0));
        Assert.Equal(Element.Neutral, Specs.AdaptiveElement(new Characteristics(Earth: 50), [Shot], 0));
        Assert.Equal(Element.Earth, Specs.Adapted(Shot, Element.Earth).Element);
        Spell storm = Specs.Adapted(Shot, Element.Lightning);
        Assert.Equal((Element.Lightning, 8, 11), (storm.Element, storm.DamageMin, storm.DamageMax));
        // Only an adaptive spell; a real fight gives it the hero's element.
        Assert.Equal(Element.Neutral, Specs.Adapted(Calm, Element.Earth).Element);
        Assert.Equal(Element.Earth, new Fight(Two, "training", 1, Sentinel("t-aim") with { Deck = ["t-shot", "stone-arrow"] }).HeroFighter!.Spells.Single(s => s.Id == "t-shot").Element);
    }

    [Fact]
    public void ChangingPath_MovesRanksAndDeck_OneForOne_AndCosts()
    {
        Hero hero = Sentinel("t-aim") with { Ranks = new Dictionary<string, int> { ["t-shot"] = 1, ["t-volley"] = 1, ["stone-arrow"] = 2 }, Deck = ["stone-arrow", "t-shot", "t-volley"] };
        Hero moved = Specs.Transferred(hero, "o-other", "o-traps", Two);
        Assert.Equal("o-other", moved.Spec);
        Assert.Equal(["stone-arrow", "o-near", "o-trap"], moved.Deck!);
        Assert.Equal(2, moved.Ranks!["stone-arrow"]);
        Assert.Equal(1, moved.Ranks!["o-near"]);
        Assert.False(moved.Ranks!.ContainsKey("t-shot"));
        Assert.Null(moved.Problem(Two));
        // A first choice moves nothing; a hero with no deck chosen keeps none.
        Hero first = Specs.Transferred(Sentinel(null) with { Deck = ["stone-arrow"] }, "t-aim", "t-loud", Two);
        Assert.Equal(["stone-arrow"], first.Deck!);
        Assert.Null(Specs.Transferred(Sentinel("t-aim"), "o-other", "o-traps", Two).Deck);
        // Another lineage moves only the spell of 50.
        Hero loud = Sentinel("t-aim", 60, "t-loud") with { Deck = ["t-shot", "t-volley"], Ranks = null };
        Assert.Equal(["t-shot", "t-hush"], Specs.Transferred(loud, "t-aim", "t-quiet", Two).Deck!);
        // No heir under 50 or without a lineage: it leaves the deck.
        Assert.Equal(["o-near"], Specs.Transferred(Sentinel("t-aim", 60, "t-loud") with { Deck = ["t-shot", "t-volley"] }, "o-other", null, Two).Deck!);
        // The first branch and lineage free, another lineage 10 × the level, another branch 30 ×.
        Assert.Equal(0, Specs.ChangeCost(Sentinel(null, 100), "t-aim", "t-loud"));
        Assert.Equal(0, Specs.ChangeCost(Sentinel("t-aim", 100) with { Fork = null }, "t-aim", "t-loud"));
        Assert.Equal(1000, Specs.ChangeCost(Sentinel("t-aim", 100, "t-loud"), "t-aim", "t-quiet"));
        Assert.Equal(0, Specs.ChangeCost(Sentinel("t-aim", 100, "t-loud"), "t-aim", "t-loud"));
        Assert.Equal(3000, Specs.ChangeCost(Sentinel("t-aim", 100, "t-loud"), "o-other", "o-traps"));
        Assert.Equal(1500, Specs.ChangeCost(Sentinel("t-aim", 50, "t-loud"), "o-other", "o-traps"));
    }

    [Fact]
    public void ATierPassed_IsTheHighestOfAGain()
    {
        Assert.Null(Specs.TierPassed(28, 29));
        Assert.Equal(30, Specs.TierPassed(29, 30));
        Assert.Null(Specs.TierPassed(30, 49));
        Assert.Equal(50, Specs.TierPassed(49, 52));
        Assert.Equal(100, Specs.TierPassed(1, 100));
        Assert.Null(Specs.TierPassed(100, 100));
    }

    // The trigger, on small boards.
    private static readonly Spell Hit = new("hit", Text, 1, 1, 6, false, false, 10, 10, 9);
    private static readonly Spell Venom = new("venom", Text, 1, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Poison, 20, 2)]);

    private static Fight Small(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData([.. Spells, Hit, Venom], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], passives: [Run, Held]), "s", 1);

    private static FighterSpec Killer(params string[] passives) => Spec(0, ap: 99, initiative: 999, spells: ["hit", "venom"]) with { Passives = passives };

    [Fact]
    public void Kill_FiresWhenTheBearerFellsAnEnemy_NotWhenItEndsTheFight()
    {
        Fight f = Small(["ABB", "..."], Killer("t-run"), Spec(1, 0, hp: 10), Spec(1, 1, hp: 50));
        f.Apply(new CastAction("hit", new Cell(1, 0)));
        Assert.Single(f.Events.OfType<PassiveTriggered>(), p => p.Passive == "t-run");
        Fight last = Small(["AB"], Killer("t-run"), Spec(1, 0, hp: 10));
        last.Apply(new CastAction("hit", new Cell(1, 0)));
        Assert.True(last.IsOver);
        Assert.Empty(last.Events.OfType<PassiveTriggered>());
    }

    [Fact]
    public void Kill_CountsItsPoison_AndNotAnotherFightersKill()
    {
        Fight f = Small(["ABB", "..."], Killer("t-run"), Spec(1, 0, hp: 10), Spec(1, 1, hp: 50));
        f.Apply(new CastAction("venom", new Cell(1, 0)));
        f.Apply(new EndTurnAction());
        Assert.Single(f.Events.OfType<PassiveTriggered>(), p => p.Passive == "t-run" && p.Fighter == 0);
        // An ally that fells it gives the bearer nothing.
        Fight other = Small(["AAB", "..B"], Spec(0, 0, ap: 99, initiative: 999, spells: ["hit"]), Spec(0, 1, initiative: 1) with { Passives = ["t-run"] }, Spec(1, 0, hp: 10), Spec(1, 1, hp: 50));
        other.Apply(new CastAction("hit", new Cell(2, 0)));
        Assert.Empty(other.Events.OfType<PassiveTriggered>());
    }

    [Fact]
    public void APatch_ChangesItsSpell_AndItsTwins()
    {
        Spell origin = Real.Spells["fire-arrow"], twin = Real.Spells["lightning-fire-arrow"];
        var patch = new PatchRule("fire-arrow", Cooldown: 1, ApCost: 2, MaxRange: 9, PerTurn: 1, Area: new Area(AreaShape.Cross, 1), DamagePercent: 50, AddEffects: [new StatusEffect(Affects.Enemies, Stat.Mp, -1, 1)]);
        TalentSpec[] talents = [Specs.AsTalent([patch])];
        Spell s = Talents.Compile(origin, talents);
        Assert.Equal((1, 2, 9, 1, origin.DamageMax / 2), (s.Cooldown, s.ApCost, s.MaxRange, s.PerTurn, s.DamageMax));
        Assert.Equal(AreaShape.Cross, s.Zone.Shape);
        Assert.Contains(s.AllEffects, e => e is StatusEffect { Stat: Stat.Mp });
        Assert.Equal(2, Talents.Compile(twin, talents).ApCost);
        Assert.Equal(Real.Spells["frost-arrow"], Talents.Compile(Real.Spells["frost-arrow"], talents));
    }

    [Fact]
    public void ALivingRule_OfAPath_PlaysLikeATalents()
    {
        var talent = Specs.AsTalent([new FirstCastRule(15, 0)]);
        Assert.Single(talent.Rules);
        Hero hero = Sentinel("o-other", 100, "o-plain") with { Deck = ["stone-arrow"] };
        Fighter f = new Fight(Two, "training", 1, hero).HeroFighter!;
        // Its rules reach the fighter as one talent.
        Assert.Contains(f.Talents, t => t.Rules.OfType<PatchRule>().Any());
    }

    private static readonly PassiveSpec Twice = new("t-twice", Text, Text, Trigger.Hit, [new ShieldEffect(Affects.Caster, 1, 1)], PerFight: 2);
    private static readonly PassiveSpec OnceATurn = new("t-once", Text, Text, Trigger.Hit, [new ShieldEffect(Affects.Caster, 1, 1)], OncePerTurn: true);
    private static readonly PassiveSpec Calmly = new("t-calmly", Text, Text, Trigger.TurnStart, [new ShieldEffect(Affects.Caster, 1, 1)], Still: true);

    private static Fight Limited(string[] rows, params FighterSpec[] fighters) =>
        new(new GameData([.. Spells, Hit, Venom], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], passives: [Twice, OnceATurn, Calmly]), "s", 1);

    [Fact]
    public void APassive_KeepsItsLimits_PerFight_AndPerTurn()
    {
        Fight twice = Limited(["AB"], Spec(0, ap: 99, initiative: 999, spells: ["hit"]), Spec(1, 0, hp: 500) with { Passives = ["t-twice"] });
        for (int i = 0; i < 4; i++)
            twice.Apply(new CastAction("hit", new Cell(1, 0)));
        Assert.Equal(2, twice.Events.OfType<PassiveTriggered>().Count());
        // Its bearer's turn opens a new count: only one until then.
        Fight once = Limited(["AB"], Spec(0, ap: 99, initiative: 999, spells: ["hit"]), Spec(1, 0, hp: 500) with { Passives = ["t-once"] });
        once.Apply(new CastAction("hit", new Cell(1, 0)));
        once.Apply(new CastAction("hit", new Cell(1, 0)));
        Assert.Single(once.Events.OfType<PassiveTriggered>());
        once.Apply(new EndTurnAction());
        once.Apply(new EndTurnAction());
        once.Apply(new CastAction("hit", new Cell(1, 0)));
        Assert.Equal(2, once.Events.OfType<PassiveTriggered>().Count());
    }

    [Fact]
    public void AStillPassive_WaitsForATurnWithoutWalking()
    {
        Fight f = Limited(["A...B", "....."], Spec(0, mp: 3, initiative: 999, spells: ["hit"]) with { Passives = ["t-calmly"] }, Spec(1, 0, hp: 500));
        Assert.Single(f.Events.OfType<PassiveTriggered>());
        f.Apply(new MoveAction(new Cell(1, 0)));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        // It walked: nothing more this turn.
        Assert.Single(f.Events.OfType<PassiveTriggered>());
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        Assert.Equal(2, f.Events.OfType<PassiveTriggered>().Count());
    }

    [Fact]
    public void AClearPassive_FiresOnlyWithNoEnemyThatClose()
    {
        Fight near = Small(["A.B....."], Spec(0, initiative: 999) with { Passives = ["t-held"] }, Spec(1));
        Assert.Empty(near.Events.OfType<PassiveTriggered>());
        Fight far = Small(["A...B..."], Spec(0, initiative: 999) with { Passives = ["t-held"] }, Spec(1));
        Assert.Single(far.Events.OfType<PassiveTriggered>());
    }

    [Fact]
    public void AFightOnAPath_ReplaysTheSame_AndARecordWithoutOneHasNone()
    {
        Hero aim = Sentinel("t-aim");
        var fight = new Fight(Two, "training", 3, aim);
        Ai.PlayOut(fight);
        FightRecord record = FightRecord.FromJson(FightRecord.Of(fight).ToJson());
        Assert.Equal(("t-aim", "t-loud"), (record.Hero!.Spec, record.Hero.Fork));
        Assert.Equal(Fingerprint(fight), Fingerprint(record.Replay(Two)));
        Assert.Null(FightRecord.FromJson(FightRecord.Of(new Fight(Real, "training", 3, Sentinel(null))).ToJson()).Hero!.Spec);
    }

    [Fact]
    public void WithoutAPath_AFightIsTheSameAsBefore_EvenWithPathsInTheData()
    {
        Hero plain = Sentinel(null);
        var there = new Fight(Two, "training", 3, plain);
        var none = new Fight(With(), "training", 3, plain);
        Ai.PlayOut(there);
        Ai.PlayOut(none);
        Assert.Equal(Fingerprint(none), Fingerprint(there));
    }

    [Fact]
    public void TheQuest_IsAtFourWays_AtThirty_AfterTheRoadsPart()
    {
        Quest road = Real.Quests[Specs.Quest];
        Assert.Equal((30, "four-ways/anatole"), (road.Level, road.Giver));
        Assert.Null(Quests.WhyNot(road, 30, ["where-roads-part"], []));
        Assert.Equal("From level 30.", Quests.WhyNot(road, 29, ["where-roads-part"], []));
        Assert.Equal("After the quest 'where-roads-part'.", Quests.WhyNot(road, 30, [], []));
    }

    // Sprint 196, D96: Anatole and Josette speak of ten ways of fighting, and name neither a branch nor the three classes that went.
    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheRoadsScene_SpeaksOfTenClasses_AndNamesNoBranch(string lang)
    {
        Dialogue anatole = Real.Dialogues["anatole"];
        Quest road = Real.Quests[Specs.Quest];
        string[] said = [.. anatole.Lines.Values.SelectMany(l => l.Answers.Select(a => a.Text).Prepend(l.Text)).Concat([road.Text!, road.Thanks!]).Concat(road.Objectives!.Select(o => o.Line!))
            .Select(t => t.In(lang))];
        string[] names = [.. Real.Specs.Values.SelectMany(s => s.Forks.SelectMany(f => f.Tiers).Prepend(s.Base).Select(t => t.Title).Prepend(s.Name)).Select(t => t.In(lang)),
            .. lang == "fr" ? new[] { "Garde", "Sentinelle", "trois façons" } : ["Guard", "Sentinel", "three ways"]];
        Assert.All(said, line => Assert.DoesNotContain(names, n => line.Contains(n, StringComparison.Ordinal)));
        Assert.Contains(said, line => line.Contains(lang == "fr" ? "dix façons de se battre" : "ten ways of fighting", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(anatole.Lines.Keys, k => k.StartsWith("ways-", StringComparison.Ordinal));
        Assert.Contains("ways", anatole.Lines["own"].Answers.Select(a => a.Next));
    }
}
