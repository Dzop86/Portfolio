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
        var hero = new Hero("Élise", "female-c", "guard", Level: 11, Stats: new Characteristics(Vitality: 40, Earth: 60), Ranks: new Dictionary<string, int> { ["strike"] = 3 });
        Fighter f = new Fight(Real, "duel", 1, hero).Fighters[0];
        Assert.Equal(guard.Hp + guard.HpPerLevel * 10 + 40, f.MaxHp);
        Assert.Equal(60, f.Spec.Characteristics.Earth);
        Assert.Equal(Real.Spells["strike"].AtRank(3).DamageMax, f.Spells.Single(s => s.Id == "strike").DamageMax);
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
                Assert.True(built.Ranks!.Values.Sum(Progression.RankCost) > Progression.SpellPoints(level) - 5, $"{c.Id} {level}");
            }
        }
    }

    [Fact]
    public void TheHerosPoints_TravelInTheRecord_AndCompareByContent()
    {
        var hero = new Hero("Élise", "female-c", "guard", Level: 11, Stats: new Characteristics(Vitality: 40), Ranks: new Dictionary<string, int> { ["strike"] = 3 });
        Assert.Equal(hero, hero with { Ranks = new Dictionary<string, int> { ["strike"] = 3 } });
        Assert.NotEqual(hero, hero with { Ranks = new Dictionary<string, int> { ["strike"] = 2 } });
        var f = new Fight(Real, "training", 3, hero);
        Ai.PlayOut(f);
        FightRecord r = FightRecord.FromJson(FightRecord.Of(f).ToJson());
        Assert.Equal(hero, r.Hero);
        Assert.Equal(Fingerprint(f), Fingerprint(r.Replay(Real)));
    }
}
