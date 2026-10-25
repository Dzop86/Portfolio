using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The compounds in the engine and Lightning (sprint 138, D80, T104): its parents, its characteristic and resistance, its
/// generated spells, the grimoire that Transmutation and Conversion make (since sprint 146, D82: no compound spell
/// without one of them, Opening and Full Grimoire gone), the Arc, Prism and the Pressed Signature; on small boards written
/// here with a class of their own, then on the real data.
/// </summary>
public class LightningTests
{
    // Two spells of each parent and their twins, fixed damage, nine casts a turn, 1 to 6 cells.
    private static readonly Spell Ember = new("ember", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Fire);
    private static readonly Spell Blaze = new("blaze", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Fire);
    private static readonly Spell Draft = new("draft", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Air);
    private static readonly Spell Squall = new("squall", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Air);
    private static readonly Spell Clod = new("clod", Text, 1, 1, 6, false, false, 10, 10, 9, Element.Earth);
    private static readonly Spell[] Twins = [.. new[] { Ember, Blaze, Draft, Squall }.Select(s => s with { Id = "l-" + s.Id, Element = Element.Lightning, Twin = s.Id })];
    private static readonly HeroClass Tester = new("tester", Text, Text, 500, 99, 6, 999, ["ember", "draft", "clod", "blaze", "squall"], HpPerLevel: 0);

    private static GameData Data(string[] rows, params FighterSpec[] fighters) =>
        new([.. Spells, .. TreeSpells, Ember, Blaze, Draft, Squall, Clod, .. Twins], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], [Tester],
            summons: TreeSummons, talents: Real.Talents.Values, reactions: Real.Reactions.Values);

    private static readonly GameData Book = Data(["A.B"], Spec(0), Monster());

    private static Hero Hero(int level = 45, params string[] talents) =>
        new("Ondine", "female-a", "tester", Level: level, Talents: talents.Length == 0 ? null : talents);

    private static FighterSpec Monster(int start = 0, int hp = 200) => Spec(1, start, hp: hp, initiative: 1, spells: ["strike"]);

    private static Fight Play(string[] rows, Hero hero, params FighterSpec[] monsters) =>
        new(Data(rows, [Spec(0, initiative: 999, spells: ["strike"]), .. monsters.Length == 0 ? [Monster()] : monsters]), "s", 1, hero);

    private static Cell C(int x, int y = 0) => new(x, y);

    // ---- the element ----

    [Fact]
    public void Lightning_IsFireAndAir_StrikesOnTheHigher_OrConvertedOnTwiceTheirAverage_AndIsResistedByTheirAverage()
    {
        Assert.Equal([Element.Fire, Element.Air], Elements.Parents(Element.Lightning));
        Assert.Empty(Elements.Parents(Element.Fire));
        Assert.True(Elements.IsCompound(Element.Lightning) && !Elements.IsCompound(Element.Air));
        // Sprint 144 (docs/elements.md §3.4): opened, the higher of its parents; converted, twice their average.
        var stats = new Characteristics(Fire: 100, Air: 30, Earth: 500);
        Assert.Equal(100, stats.For(Element.Lightning));
        Assert.Equal(130, stats.For(Element.Lightning, converted: true));
        Assert.Equal(500, stats.For(Element.Earth, converted: true));
        FighterSpec m = Spec(1) with { Resistances = new Dictionary<Element, int> { [Element.Fire] = 30, [Element.Air] = -10 } };
        Assert.Equal(10, m.Resistance(Element.Lightning));
        // Given, it is its own.
        Assert.Equal(50, (m with { Resistances = new Dictionary<Element, int> { [Element.Lightning] = 50 } }).Resistance(Element.Lightning));
    }

    [Theory]
    [InlineData("sentinel")]
    [InlineData("guard")]
    [InlineData("mage")]
    public void EachClass_HasEightLightningSpells_TwinsOfItsFireAndAirOnes_AtFourFifthsOfTheBudget(string id)
    {
        // The Sentinel's since sprint 138, the Guard's and the Mage's since sprint 139.
        HeroClass c = Real.Class(id)!;
        Spell[] twins = [.. Real.Spells.Values.Where(s => s.Element == Element.Lightning && c.Spells.Contains(s.Twin!))];
        Assert.Equal(8, twins.Length);
        foreach (Spell t in twins)
        {
            Spell origin = Real.Spells[t.Twin!];
            Assert.Contains(origin.Id, c.Spells);
            Assert.DoesNotContain(t.Id, c.Spells);
            Assert.Contains(origin.Element, new[] { Element.Fire, Element.Air });
            Assert.Equal((origin.ApCost, origin.MinRange, origin.MaxRange, origin.Zone, origin.Level, origin.Cooldown, origin.LineOfSight, origin.InLine),
                (t.ApCost, t.MinRange, t.MaxRange, t.Zone, t.Level, t.Cooldown, t.LineOfSight, t.InLine));
            Assert.InRange(t.AverageDamage / origin.AverageDamage, 0.7, 0.9);
            Assert.False(string.IsNullOrWhiteSpace(t.Name.Fr) || string.IsNullOrWhiteSpace(t.Name.En));
            Assert.NotEqual(origin.Name.Fr, t.Name.Fr);
            Assert.Equal(t.RankKey, origin.Id);
        }
        Assert.Equal(8, twins.Select(t => t.Name.Fr).Distinct().Count());
    }

    [Fact]
    public void EverySpellOfTheClasses_HasANameOfItsOwn_AndTheBaseSpellsThatCarriedLightningsWordGaveItUp()
    {
        // Sprint 139: the Mage's Air "Éclair" is now "Trait de vent", its Lightning twin takes the name; the Guard's
        // "Cyclone" (the Mage's too) is a "Lame cyclone".
        Spell[] all = [.. Real.Classes.SelectMany(c => c.Spells).Select(id => Real.Spells[id]), .. Real.Spells.Values.Where(s => s.Twin is not null)];
        Assert.Equal(all.Length, all.Select(s => s.Name.Fr).Distinct().Count());
        Assert.Equal(all.Length, all.Select(s => s.Name.En).Distinct().Count());
        Assert.Equal(("Trait de vent", "Wind Dart"), (Real.Spells["bolt"].Name.Fr, Real.Spells["bolt"].Name.En));
        Assert.Equal(("Éclair", Element.Lightning), (Real.Spells["lightning-bolt"].Name.Fr, Real.Spells["lightning-bolt"].Element));
        Assert.DoesNotContain(all.Where(s => s.Element is Element.Fire or Element.Air), s => s.Name.Fr.Contains("foudre", StringComparison.OrdinalIgnoreCase) || s.Name.Fr.Contains("éclair", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("guard")]
    [InlineData("mage")]
    public void TheGuardsAndTheMagesTransmutation_TurnsTheirFireSpellsUnlocked_AndAConvertedHeroPlaysAndReplays(string id)
    {
        Hero hero = new("Ondine", "female-a", id, Level: 45, Talents: ["electrifying", "transmute-fire-lightning"]);
        string[] fire = [.. Real.Class(id)!.Spells.Where(s => Real.Spells[s].Element == Element.Fire && Real.Spells[s].Level <= 45)];
        Assert.Equal(fire, Talents.GrimoireOf(hero, Real).Spells.Where(s => Real.Spells[s].Twin is not null).Select(s => Real.Spells[s].Twin));
        Hero converted = Progression.Built(hero with { Talents = ["electrifying", "prism", "convert-lightning"] }, Real);
        Assert.Null(converted.Problem(Real));
        var f = new Fight(Real, "group:fields-0-4.0", 5, converted);
        Ai.PlayOut(f);
        Assert.True(f.IsOver);
        Assert.Equal(Fingerprint(f), Fingerprint(FightRecord.FromJson(FightRecord.Of(f).ToJson()).Replay(Real)));
    }

    [Fact]
    public void TheData_RefuseATwinOfAnotherElement_OrOfAnotherLevel()
    {
        Assert.Throws<InvalidDataException>(() => new GameData([Clod, Clod with { Id = "x", Element = Element.Lightning, Twin = "clod" }], [], []));
        Assert.Throws<InvalidDataException>(() => new GameData([Ember, Ember with { Id = "x", Element = Element.Lightning, Twin = "ember", Level = 5 }], [], []));
        Assert.Throws<InvalidDataException>(() => new GameData([Ember with { Id = "x", Element = Element.Lightning, Twin = "nothing" }], [], []));
    }

    // ---- the grimoire ----

    [Fact]
    public void NoCompoundSpell_WithoutATransmutationOrTheConversion()
    {
        // Sprint 146 (D82, R1): Charles, "not as long as the tree's passive has not changed the elements".
        Assert.Equal(["ember", "draft", "clod", "blaze", "squall"], Talents.GrimoireOf(Hero(45), Book).Spells);
        Assert.Equal(["ember", "draft", "clod", "blaze", "squall"], Talents.GrimoireOf(Hero(45, "electrifying"), Book).Spells);
        Assert.Equal(["l-ember", "draft", "clod", "l-blaze", "squall"], Talents.GrimoireOf(Hero(45, "electrifying", "transmute-fire-lightning"), Book).Spells);
        Assert.Equal(["l-ember", "l-draft", "clod", "l-blaze", "l-squall"], Talents.GrimoireOf(Hero(45, "electrifying", "convert-lightning"), Book).Spells);
        Assert.False(Real.Talents.ContainsKey("open-lightning") || Real.Talents.ContainsKey("full-grimoire"));
        Assert.Equal(52, Real.Talents.Count);
        // On the real Sentinel at 100, all eight of Lightning once converted; a twin is unlocked with its spell: at 45, not
        // the twins of Gale (50), Ember Rain (65) nor Skyfall (75).
        Hero sentinel = new("Ondine", "female-a", "sentinel", Level: 100, Talents: ["electrifying", "convert-lightning"]);
        Assert.Equal(8, Talents.GrimoireOf(sentinel, Real).Spells.Count(id => Real.Spells[id].Element == Element.Lightning));
        Assert.Equal(5, Talents.GrimoireOf(sentinel with { Level = 45 }, Real).Spells.Count(id => Real.Spells[id].Twin is not null));
    }

    [Fact]
    public void Transmutation_TurnsAParentsSpellsIntoTheirTwins_EvenInADeckChosenBefore()
    {
        Hero hero = Hero(45, "electrifying", "transmute-air-lightning") with { Deck = ["draft", "clod"] };
        Grimoire g = Talents.GrimoireOf(hero, Book);
        Assert.Equal(["ember", "l-draft", "clod", "blaze", "l-squall"], g.Spells);
        Assert.Equal("l-draft", g.Replaced["draft"]);
        Assert.Equal(["l-draft", "clod"], hero.DeckOf(Book));
        Assert.Null(hero.Problem(Book));
        // Without the talent, a twin is not one of the hero's spells.
        Assert.Contains("not one this hero has", (Hero(45) with { Deck = ["l-draft"] }).Problem(Book));
        // Its own spell and its twin: twice the same.
        Assert.Contains("each once", (hero with { Deck = ["draft", "l-draft"] }).Problem(Book));
    }

    [Fact]
    public void Conversion_TurnsBothParents_AtFortyFive_AndTakesThePlaceOfTheTransmutation_ForFree()
    {
        Hero converted = Hero(45, "electrifying", "prism", "convert-lightning");
        Assert.Null(converted.Problem(Book));
        Assert.Equal(["l-ember", "l-draft", "clod", "l-blaze", "l-squall"], Talents.GrimoireOf(converted, Book).Spells);
        // Its parent the pair's node, and level 45 (sprint 146, D82, G4 a and G5 a); no points below it asked.
        Assert.Contains("opens at level 45", Hero(44, "electrifying", "convert-lightning").Problem(Book));
        Assert.Contains("asks for 'electrifying'", Hero(45, "convert-lightning").Problem(Book));
        Assert.Contains("exclude each other", Hero(50, "electrifying", "transmute-air-lightning", "convert-lightning").Problem(Book));
        // The Conversion takes the Transmutation's place: its point comes back, and it is not giving a talent back (I2 a).
        Assert.Equal(["electrifying", "prism", "convert-lightning"], Talents.With(["electrifying", "transmute-fire-lightning", "prism"], "convert-lightning", Book));
        Assert.False(Talents.GivesBack(["electrifying", "transmute-fire-lightning"], ["electrifying", "convert-lightning"], Book));
        Assert.True(Talents.GivesBack(["electrifying", "transmute-fire-lightning"], ["electrifying"], Book));
    }

    [Fact]
    public void Transmutation_IsAChoice_OfFireOrOfAir_AfterElectrifying_AndPrismAsksForOneOfThem()
    {
        Assert.Null(Hero(30, "electrifying", "transmute-fire-lightning").Problem(Book));
        Assert.Contains("exclude each other", Hero(45, "electrifying", "transmute-fire-lightning", "transmute-air-lightning").Problem(Book));
        Assert.Contains("asks for 'electrifying'", Hero(30, "transmute-fire-lightning").Problem(Book));
        Assert.Equal(["electrifying"], Real.Talents["transmute-fire-lightning"].Requires!);
        // Pressed Signature and Prism: a Transmutation or the Conversion (G7 a), no point below them asked.
        Assert.Contains("asks for one of", Hero(30, "electrifying", "prism").Problem(Book));
        Assert.Null(Hero(33, "electrifying", "transmute-air-lightning", "prism").Problem(Book));
        Assert.Null(Hero(45, "electrifying", "convert-lightning", "strong-signature").Problem(Book));
    }

    [Fact]
    public void ATwin_HasItsClassSpellsRank()
    {
        Spell ranked = Ember with { Ranks = [new SpellRank(20, 20)] };
        GameData data = new([.. Spells, .. TreeSpells, ranked, Blaze, Draft, Squall, Clod, .. Twins.Select(t => t.Id == "l-ember" ? t with { Ranks = [new SpellRank(16, 16)] } : t)],
            [new MapSpec("m", Text, ["A..B"])], [new Scenario("s", Text, "m", [Spec(0, initiative: 999), Monster()])], [Tester], summons: TreeSummons, talents: Real.Talents.Values, reactions: Real.Reactions.Values);
        var f = new Fight(data, "s", 1, Hero(45, "electrifying", "transmute-fire-lightning") with { Ranks = new Dictionary<string, int> { ["ember"] = 2 }, Deck = ["l-ember"] });
        Assert.Equal(16, f.HeroFighter!.Spells.Single().DamageMin);
    }

    // ---- in a fight ----

    [Fact]
    public void TheArc_JumpsThirtyFivePercentToTheNearestEnemyWithinTwoCells_NotFarther()
    {
        Fight f = Play(["A..BB.B"], Hero(45, "electrifying", "transmute-fire-lightning") with { Deck = ["l-ember"] }, Monster(0), Monster(1), Monster(2));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("l-ember", C(3))));
        // 10 on the target, 35 % of 10 (3) on its neighbour; the third, 3 cells away, nothing.
        Assert.Equal([190, 197, 200], f.Fighters.Skip(1).Select(m => m.Hp));
        Assert.Contains(new Arced(1, 2), f.Events);
        // Alone, nothing jumps.
        Fight alone = Play(["A..B"], Hero(45, "electrifying", "transmute-fire-lightning") with { Deck = ["l-ember"] });
        alone.Apply(new CastAction("l-ember", C(3)));
        Assert.Empty(alone.Events.OfType<Arced>());
    }

    [Fact]
    public void PressedSignature_AndConversion_ChangeTheArcAndTheHit()
    {
        // Pressed Signature: the hit 10 % softer (9), the Arc 50 % stronger (52 % of 9: 4).
        Fight pressed = Play(["A..BB"], Hero(45, "electrifying", "transmute-fire-lightning", "strong-signature") with { Deck = ["l-ember"] }, Monster(0), Monster(1));
        pressed.Apply(new CastAction("l-ember", C(3)));
        Assert.Equal([191, 196], pressed.Fighters.Skip(1).Select(m => m.Hp));
        // Conversion: 15 % harder, and the trunk's 3 % at 45 (11), the Arc at 52 % of 11 (5); with Pressed Signature too, the
        // Arc does not grow more.
        Fight converted = Play(["A..BB"], Hero(45, "electrifying", "prism", "convert-lightning") with { Deck = ["l-ember"] }, Monster(0), Monster(1));
        converted.Apply(new CastAction("l-ember", C(3)));
        Assert.Equal([189, 195], converted.Fighters.Skip(1).Select(m => m.Hp));
        Assert.Equal(50, Talents.SignatureBonus([Real.Talents["strong-signature"], Real.Talents["convert-lightning"]]));
    }

    [Fact]
    public void ATransmutedLightning_StrikesOnTheHigherParent_AConvertedOne_OnBoth()
    {
        // Sprint 144: with the sum, a mere opening gave a mixed hero its full strength for 1 point; since sprint 146, a
        // Transmutation strikes as an opening did.
        var stats = new Characteristics(Fire: 100, Air: 30);
        int Hit(Hero hero)
        {
            Fight f = Play(["A..B"], hero with { Deck = ["l-ember"] });
            f.Apply(new CastAction("l-ember", C(3)));
            return 200 - f.Fighters[1].Hp;
        }
        // The trunk's 3 % at 45 on top (sprint 146).
        int plain = Hit(Hero(45, "electrifying", "transmute-fire-lightning"));
        Assert.Equal(10, plain);
        Assert.Equal((10 + Characteristics.Bonus(100)) * (100 + Talents.TrunkDamage(45)) / 100, Hit(Hero(45, "electrifying", "transmute-fire-lightning") with { Stats = stats }));
        Hero converting = Hero(45, "electrifying", "prism", "convert-lightning");
        Assert.True(Talents.Converts([.. converting.Talents!.Select(id => Real.Talents[id])], Element.Lightning));
        Assert.False(Talents.Converts([Real.Talents["transmute-fire-lightning"]], Element.Lightning));
        Assert.True(Hit(converting with { Stats = stats }) - Hit(converting) >= Characteristics.Bonus(130));
    }

    [Fact]
    public void ALightningHit_LeavesNoMark_UnlessPrism_ThenFiresMark_AndNeverReacts()
    {
        Fight plain = Play(["A..B"], Hero(45, "electrifying", "transmute-fire-lightning") with { Deck = ["l-ember"] });
        plain.Apply(new CastAction("l-ember", C(3)));
        Assert.Null(plain.Fighters[1].Mark);
        Fight prism = Play(["A..B"], Hero(45, "electrifying", "transmute-fire-lightning", "prism", "bogging") with { Deck = ["clod", "l-ember"] });
        prism.Apply(new CastAction("clod", C(3)));
        prism.Apply(new CastAction("l-ember", C(3)));
        Assert.Equal(Element.Fire, prism.Fighters[1].Mark!.Element);
        Assert.Empty(prism.Events.OfType<ReactionTriggered>());
    }

    [Fact]
    public void AConvertedSentinel_PlaysAndReplaysTheSame_AndItsPointsGoToFireAndAir()
    {
        Hero sentinel = Progression.Built(new Rpg.Core.Hero("Ondine", "female-a", "sentinel", Level: 45, Talents: ["electrifying", "prism", "convert-lightning"]), Real);
        Assert.Null(sentinel.Problem(Real));
        Assert.Contains(sentinel.DeckOf(Real), id => Real.Spells[id].Element == Element.Lightning);
        Assert.DoesNotContain(sentinel.DeckOf(Real), id => Real.Spells[id].Element is Element.Fire or Element.Air);
        Assert.True(sentinel.Stats!.Fire > 0 && sentinel.Stats.Air > 0);
        Assert.All(sentinel.Ranks!.Keys, k => Assert.Contains(k, Real.Class("sentinel")!.Spells));
        var f = new Fight(Real, "group:fields-0-4.0", 3, sentinel);
        Ai.PlayOut(f);
        Assert.Contains(f.Events, e => e is Damaged { Element: Element.Lightning });
        Assert.Equal(Fingerprint(f), Fingerprint(FightRecord.FromJson(FightRecord.Of(f).ToJson()).Replay(Real)));
    }
}
