using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>Experience, levels and points (sprint 58).</summary>
public class ProgressionTests
{
    private static readonly Spell Blow = new("blow", Text, 2, 1, 6, false, false, 99, 99, 9);

    private static Fight Won(int heroLevel, params int[] monsterLevels)
    {
        FighterSpec[] monsters = [.. monsterLevels.Select((l, i) => Spec(1, i, hp: 5) with { Level = l })];
        string[] rows = [string.Concat(Enumerable.Repeat("B", monsters.Length)) + ".A"];
        var data = new GameData([.. Spells, Blow], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", [Spec(0, ap: 99, initiative: 999, spells: ["blow"]), .. monsters])],
            [new HeroClass("hunter", Text, Text, 50, 99, 0, 999, ["blow"])]);
        var f = new Fight(data, "s", 1, new Hero("Élise", "female-c", "hunter", Level: heroLevel));
        foreach (Fighter m in f.Fighters.Where(x => x.Team == 1))
            f.Apply(new CastAction("blow", m.Cell));
        Assert.Equal(0, f.WinningTeam);
        return f;
    }

    [Fact]
    public void TheCurve_GoesFrom100XpForLevel2_To495000ForLevel100()
    {
        Assert.Equal((0L, 100L, 300L, 495_000L), (Progression.XpToReach(1), Progression.XpToReach(2), Progression.XpToReach(3), Progression.XpToReach(100)));
        Assert.Equal((1, 2, 2, 3, 100, 100), (Progression.LevelFor(99), Progression.LevelFor(100), Progression.LevelFor(299), Progression.LevelFor(300), Progression.LevelFor(495_000), Progression.LevelFor(long.MaxValue)));
        for (int l = 1; l < Hero.MaxLevel; l++)
            Assert.True(Progression.XpToReach(l + 1) - Progression.XpToReach(l) > Progression.XpToReach(l) - Progression.XpToReach(Math.Max(1, l - 1)) || l == 1);
    }

    [Fact]
    public void AWonFight_GivesEachMonstersWorth_NothingFromOne20LevelsBelow()
    {
        Assert.Equal(Progression.MonsterXp(1) * 2, Progression.FightXp(Won(1, 1, 1)));
        Assert.Equal(Progression.MonsterXp(31), Progression.FightXp(Won(50, 31, 30)));
        Assert.Equal(Progression.MonsterXp(80), Progression.FightXp(Won(10, 80)));
    }

    [Fact]
    public void ALostFight_ARivalAndSummons_GiveNothing_AlliesGiveTheGroupBonus()
    {
        var lost = new Fight(Real, "training", 1, new Hero("Élise", "female-c", "guard"));
        Ai.PlayOut(lost);
        Assert.Equal(lost.WinningTeam == 0 ? Progression.MonsterXp(1) * 2 : 0, Progression.FightXp(lost));
        var duel = new Fight(Real, "duel", 2, new Hero("Élise", "female-c", "guard"), new Hero("Ondine", "female-e", "mage"));
        Ai.PlayOut(duel);
        Assert.Equal(0, Progression.FightXp(duel));
        Assert.Equal(0, Progression.FightXp(new Fight(Real, "training", 1)));
        // Two heroes on the side that wins: 10 % more.
        var data = new GameData([.. Spells, Blow], [new MapSpec("m", Text, ["AAB"])], [new Scenario("s", Text, "m", [Spec(0, 0, ap: 99, initiative: 999, spells: ["blow"]), Spec(0, 1), Spec(1, hp: 5)])],
            [new HeroClass("hunter", Text, Text, 50, 99, 0, 999, ["blow"])]);
        var group = new Fight(data, "s", 1, new Hero("Élise", "female-c", "hunter"));
        group.Apply(new CastAction("blow", new Cell(2, 0)));
        Assert.Equal(Progression.MonsterXp(1) * 110 / 100, Progression.FightXp(group));
    }

    [Theory]
    [InlineData(1, 0, null)]
    [InlineData(1, 1, "characteristic")]
    [InlineData(11, 100, null)]
    [InlineData(11, 101, "characteristic")]
    public void CharacteristicPoints_AreTenALevel(int level, int vitality, string? problem)
    {
        string? p = new Hero("Élise", "female-c", "guard", Level: level, Stats: new Characteristics(Vitality: vitality)).Problem(Real);
        if (problem is null)
            Assert.Null(p);
        else
            Assert.Contains(problem, p, StringComparison.Ordinal);
        Assert.NotNull(new Hero("Élise", "female-c", "guard", Level: 50, Stats: new Characteristics(Earth: -1)).Problem(Real));
    }

    [Fact]
    public void SpellPoints_AreOneALevel_AndARankCostsMoreTheHigherItGoes()
    {
        Assert.Equal([0, 0, 1, 3, 6, 10], Enumerable.Range(0, 6).Select(Progression.RankCost));
        Hero Ranked(int level, string spell, int rank) => new("Élise", "female-c", "guard", Level: level, Ranks: new Dictionary<string, int> { [spell] = rank });
        Assert.Null(Ranked(4, "shield-bash", 3).Problem(Real));
        Assert.Contains("spell points", Ranked(3, "shield-bash", 3).Problem(Real), StringComparison.Ordinal);
        Assert.Contains("ranks 1 to 5", Ranked(50, "shield-bash", 6).Problem(Real), StringComparison.Ordinal);
        // Another class's spell, or one the level has not unlocked yet.
        Assert.Contains("not one this hero has", Ranked(50, "ice-shard", 2).Problem(Real), StringComparison.Ordinal);
        Assert.Contains("not one this hero has", Ranked(4, "bulwark", 2).Problem(Real), StringComparison.Ordinal);
    }

    [Fact]
    public void InAFight_TheHerosPointsCount()
    {
        HeroClass guard = Real.Class("guard")!;
        var hero = new Hero("Élise", "female-c", "guard", Level: 11, Stats: new Characteristics(Vitality: 40, Earth: 60), Ranks: new Dictionary<string, int> { ["shield-bash"] = 3 });
        Fighter f = new Fight(Real, "duel", 1, hero).Fighters[0];
        Assert.Equal(guard.Hp + guard.HpPerLevel * 10 + 40, f.MaxHp);
        Assert.Equal(60, f.Spec.Characteristics.Earth);
        Assert.Equal(Real.Spells["shield-bash"].AtRank(3).DamageMax, f.Spells.Single(s => s.Id == "shield-bash").DamageMax);
    }

    [Fact]
    public void TheSimplestBuild_SpendsWithinThePoints_ForEveryClassAndLevel()
    {
        foreach (HeroClass c in Real.Classes)
        {
            foreach (int level in new[] { 1, 2, 10, 50, 100 })
            {
                Hero built = Progression.Built(new Hero("Élise", "female-c", c.Id, Level: level), Real);
                Assert.Null(built.Problem(Real));
                Characteristics st = built.Stats!;
                Assert.Equal(Progression.CharacteristicPoints(level), st.Vitality + st.Earth + st.Fire + st.Water + st.Air);
                // Every point but a few, or (since sprint 128, the deck only) every damaging spell of the deck at its highest rank.
                bool full = built.DeckOf(Real).Select(id => Real.Spells[id]).Where(s => s.DamageMax > 0).All(s => built.Ranks!.GetValueOrDefault(s.Id, 1) == s.MaxRank);
                Assert.True(full || built.Ranks!.Values.Sum(Progression.RankCost) > Progression.SpellPoints(level) - 5, $"{c.Id} {level}");
            }
        }
    }

    [Fact]
    public void TheHerosPoints_TravelInTheRecord_AndCompareByContent()
    {
        var hero = new Hero("Élise", "female-c", "guard", Level: 11, Stats: new Characteristics(Vitality: 40), Ranks: new Dictionary<string, int> { ["shield-bash"] = 3 });
        Assert.Equal(hero, hero with { Ranks = new Dictionary<string, int> { ["shield-bash"] = 3 } });
        Assert.NotEqual(hero, hero with { Ranks = new Dictionary<string, int> { ["shield-bash"] = 2 } });
        var f = new Fight(Real, "training", 3, hero);
        Ai.PlayOut(f);
        FightRecord r = FightRecord.FromJson(FightRecord.Of(f).ToJson());
        Assert.Equal(hero, r.Hero);
        Assert.Equal(Fingerprint(f), Fingerprint(r.Replay(Real)));
    }

    [Fact]
    public void TheHero_FightsWithItsDeck_InItsOrder_WithinTheRules()
    {
        Hero H(int level, params string[] deck) => new("Élise", "female-c", "guard", Level: level, Deck: deck);
        // No deck: the class's first unlocked spells, twelve at most (the automatic deck since sprint 161).
        Assert.Equal(Real.Class("guard")!.Spells.Take(5), new Hero("Élise", "female-c", "guard").DeckOf(Real));
        Assert.Equal(12, new Hero("Élise", "female-c", "guard", Level: 95).DeckOf(Real).Count);
        // A deck is played as chosen, in its order.
        Fighter f = new Fight(Real, "duel", 1, H(10, "taunt", "slash")).Fighters[0];
        Assert.Equal(["taunt", "slash"], f.Spells.Select(s => s.Id));
        Assert.Null(H(10, "taunt", "slash").Problem(Real));
        Assert.Contains("each once", H(10, "slash", "slash").Problem(Real), StringComparison.Ordinal);
        Assert.Contains("at most 12", H(95, [.. Real.Class("guard")!.Spells.Take(13)]).Problem(Real), StringComparison.Ordinal);
        Assert.Contains("not one this hero has", H(4, "bulwark").Problem(Real), StringComparison.Ordinal);
        Assert.Contains("not one this hero has", H(10, "pebble").Problem(Real), StringComparison.Ordinal);
        // The deck travels with the hero and compares by content.
        Assert.Equal(H(10, "taunt"), H(10, "taunt"));
        Assert.NotEqual(H(10, "taunt"), H(10, "slash"));
    }

    [Theory]
    [InlineData("sentinel", 39, "")]
    [InlineData("sentinel", 40, "ember-volley>searing-shot")]
    [InlineData("sentinel", 45, "ember-volley>searing-shot tide-arrow>venom-arrow")]
    [InlineData("sentinel", 60, "ember-volley>searing-shot tide-arrow>venom-arrow")]
    [InlineData("guard", 41, "")]
    [InlineData("guard", 55, "")]
    [InlineData("guard", 60, "cleave>titan-blow")]
    [InlineData("mage", 55, "")]
    [InlineData("mage", 60, "roots>fault-line")]
    public void TheAutomaticDeck_GivesANewSpellTheCellOfTheOldestOfItsElement_ThatItBeatsOnATurn(string id, int level, string swaps)
    {
        // Sprint 161 (the game designer's decision, docs/suite-apres-d69.md §12): nothing changes before 40; a spell that
        // hurts takes in place the cell of the oldest of its element that it beats on a turn, or waits (Smite does not beat
        // Fire Lunge: the Guard keeps it); a spell that helps (Focus, War Cry, Renewal) waits.
        var hero = new Hero("Élise", "female-c", id, Level: level);
        string[] expected = [.. hero.FirstSpells(Real)];
        foreach (string[] swap in swaps.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('>')))
            expected[Array.IndexOf(expected, swap[0])] = swap[1];
        Assert.Equal(expected, hero.DeckOf(Real));
        Assert.Equal(12, expected.Length);
        Assert.Null((hero with { Deck = expected }).Problem(Real));
    }

    [Theory]
    [InlineData("ember-volley", 12, "searing-shot", 24)]
    [InlineData("tide-arrow", 21, "venom-arrow", 24)]
    [InlineData("piercing-arrow", 19, "gale", 12)]
    [InlineData("rockfall", 11.5, "stake", 35.0 / 6)]
    [InlineData("fire-lunge", 26, "smite", 20.5)]
    [InlineData("undertow", 17, "groundswell", 10.5)]
    [InlineData("wind-blade", 21, "cyclone-blade", 11)]
    [InlineData("cleave", 11.5, "titan-blow", 40.0 / 3)]
    [InlineData("burn", 22, "meteor", 14)]
    [InlineData("tidal-wave", 11.5, "freeze", 7.75)]
    [InlineData("bolt", 25, "cyclone", 6)]
    [InlineData("roots", 6.5, "fault-line", 16.5)]
    public void ASpellsWorthOnATurn_CountsItsCasts_ItsPoison_AndItsCooldown(string old, double was, string next, double worth)
    {
        // The average times the casts 8 points allow, the poison over its turns, over 1 + the cooldown; areas left out.
        Assert.Equal(was, Hero.TurnValue(Real.Spells[old]), 3);
        Assert.Equal(worth, Hero.TurnValue(Real.Spells[next]), 3);
    }

    [Fact]
    public void TheAutomaticDeck_KeepsTheSpellsOfLevelOne_TheRaisedOnes_TheFarReaching_AndNeverTouchesAChosenDeck()
    {
        // Ember Volley raised above rank 1 keeps its cell: Searing Shot has no other of its element to take, it waits.
        var hero = new Hero("Élise", "female-c", "sentinel", Level: 40);
        Hero raised = hero with { Ranks = new Dictionary<string, int> { [Real.Spells["ember-volley"].RankKey] = 2 } };
        Assert.Equal(hero.FirstSpells(Real), raised.DeckOf(Real));
        Assert.Contains("stone-arrow", (hero with { Level = 60 }).DeckOf(Real));
        // A spell that only touches, however strong, never takes the cell of one that reaches farther.
        Spell blow = Real.Spells["searing-shot"] with { MaxRange = 1, MinRange = 1, DamageMin = 90, DamageMax = 90 };
        GameData touch = new(Real.Spells.Values.Select(s => s.Id == "searing-shot" ? blow : s), Real.Maps.Values, Real.Scenarios.Values, Real.Classes,
            Real.Towns.Values, Real.Dialogues.Values, Real.Summons.Values, Real.Quests.Values, Real.Items.Values, Real.Sets.Values, Real.Families.Values,
            Real.Dungeons.Values, Real.Jobs.Values, Real.Passives.Values, Real.Talents.Values, Real.Reactions.Values);
        Assert.Equal(hero.FirstSpells(touch), hero.DeckOf(touch));
        // A deck saved once, even empty (D66), is the player's.
        string[] chosen = [.. hero.FirstSpells(Real)];
        Assert.Equal(chosen, (hero with { Deck = chosen }).DeckOf(Real));
        Assert.Empty((hero with { Deck = [] }).DeckOf(Real));
        // The simplest build spends its points on that deck, and fights with it.
        Hero built = Progression.Built(hero with { Level = 60 }, Real);
        Assert.Equal((hero with { Level = 60 }).DeckOf(Real), built.DeckOf(Real));
        Assert.True(built.Ranks!.GetValueOrDefault(Real.Spells["searing-shot"].RankKey, 1) > 1);
    }

    [Fact]
    public void ARecordOfVersionTwo_ReplaysAHeroWithoutADeck_WithItsFirstSpells()
    {
        // Sprint 161: a record of version 3 replays the automatic deck; one written before, the first twelve spells.
        var hero = Progression.Built(new Hero("Élise", "female-c", "guard", Level: 60), Real);
        var now = new Fight(Real, "training", 5, hero);
        Ai.PlayOut(now);
        FightRecord r = FightRecord.FromJson(FightRecord.Of(now).ToJson());
        Assert.Equal(3, r.Version);
        Assert.Null(r.Hero!.Deck);
        Assert.Equal(Fingerprint(now), Fingerprint(r.Replay(Real)));
        var before = new Fight(Real, "training", 5, hero with { Deck = hero.FirstSpells(Real) });
        Ai.PlayOut(before);
        FightRecord old = FightRecord.FromJson(FightRecord.Of(before).ToJson()) with { Version = 2, Hero = hero };
        Assert.Equal(Fingerprint(before), Fingerprint(old.Replay(Real)));
        Assert.Throws<InvalidFightRecordException>(() => (r with { Version = 4 }).Replay(Real));
    }

    [Fact]
    public void AHeroOfOneElement_HoldsNoMoreSpellsThanADeck()
    {
        foreach (string id in new[] { "sentinel", "guard", "mage" })
            foreach (Element e in new[] { Element.Earth, Element.Fire, Element.Water, Element.Air })
            {
                Hero one = Progression.OfOne(new Hero("Élise", "female-c", id, Level: 95), e, Real);
                Assert.True(one.Deck!.Count <= Hero.DeckSize);
                Assert.Null(one.Problem(Real));
                Assert.All(Real.Class(id)!.Spells.Where(s => Real.Spells[s].Element == e && Real.Spells[s].DamageMax > 0), s => Assert.Contains(s, one.Deck));
            }
    }
}
