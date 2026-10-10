using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>Characteristics, spell ranks and unlocking levels, summons (sprint 55).</summary>
public class ProgressRulesTests
{
    private static readonly Spell Rock = new("rock", Text, 2, 1, 6, false, false, 10, 10, 9, Element.Earth);
    private static readonly Spell Flame = new("flame", Text, 2, 1, 6, false, false, 10, 10, 9, Element.Fire);
    private static readonly Spell Wave = new("wave", Text, 2, 1, 6, false, false, 10, 10, 9, Element.Water);
    private static readonly Spell Gust = new("gust", Text, 2, 1, 6, false, false, 10, 10, 9, Element.Air);
    private static readonly Spell Mend = new("mend", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new HealEffect(Affects.All, 20, 20)]);
    private static readonly Spell Ranked = new("ranked", Text, 3, 1, 2, false, false, 4, 6, 1, Element.Neutral, null, null, Level: 1,
        Ranks: [new SpellRank(6, 8), new SpellRank(8, 10, ApCost: 2), new SpellRank(10, 12, MaxRange: 4, PerTurn: 2)]);
    private static readonly Spell Late = new("late", Text, 2, 1, 6, false, false, 30, 30, 9, Element.Neutral, null, null, Level: 20);
    private static readonly Spell Call = new("call", Text, 3, 1, 3, false, false, 0, 0, 9, Element.Neutral, null, [new SummonEffect(Affects.All, "wolf", 1)]);
    private static readonly SummonSpec Wolf = new("wolf", new LocalizedText("Loup", "Wolf"), "male-d", 20, 6, 3, 50, ["strike"]);
    private static readonly Spell[] All = [.. Spells, Rock, Flame, Wave, Gust, Mend, Ranked, Late, Call];

    private static GameData Data(string[] rows, FighterSpec[] fighters, HeroClass[]? classes = null) =>
        new(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)], classes, null, null, [Wolf]);

    private static Fight Play(string[] rows, params FighterSpec[] fighters) => new(Data(rows, fighters), "s", 1);

    private static FighterSpec Caster(Characteristics stats, params string[] spells) =>
        Spec(0, ap: 99, mp: 0, initiative: 999, spells: spells) with { Stats = stats };

    [Fact]
    public void Vitality_AddsHitPoints()
    {
        Fight f = Play(["AB"], Spec(0, hp: 50) with { Stats = new Characteristics(Vitality: 30) }, Spec(1));
        Assert.Equal((80, 80), (f.Fighters[0].Hp, f.Fighters[0].MaxHp));
    }

    [Theory]
    [InlineData("rock", 40, 0, 0, 0)]
    [InlineData("flame", 0, 40, 0, 0)]
    [InlineData("wave", 0, 0, 40, 0)]
    [InlineData("gust", 0, 0, 0, 40)]
    public void EachElement_GrowsWithItsCharacteristic_TwoMoreForEveryTenPoints(string spell, int earth, int fire, int water, int air)
    {
        var stats = new Characteristics(0, earth, fire, water, air);
        Fight f = Play(["AB"], Caster(stats, spell), Spec(1, hp: 99));
        f.Apply(new CastAction(spell, new Cell(1, 0)));
        Assert.Equal(99 - 18, f.Fighters[1].Hp);
        // The other characteristics do nothing for this element.
        Fight other = Play(["AB"], Caster(new Characteristics(0, 40 - earth, 40 - fire, 40 - water, 40 - air), spell), Spec(1, hp: 99));
        other.Apply(new CastAction(spell, new Cell(1, 0)));
        Assert.Equal(99 - 10, other.Fighters[1].Hp);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9, 0)]
    [InlineData(10, 2)]
    [InlineData(19, 2)]
    [InlineData(55, 10)]
    [InlineData(1000, 200)]
    public void TheBonus_IsTwoForEveryWholeTenPoints(int points, int bonus) => Assert.Equal(bonus, Characteristics.Bonus(points));

    [Theory]
    [InlineData(10, 2)]
    [InlineData(26, 5)]
    [InlineData(12, 2)]
    [InlineData(13, 3)]
    [InlineData(0, 0)]
    public void ACriticalHit_AddsAFifthOfTheTop_Rounded(int max, int bonus) => Assert.Equal(bonus, Spell.CritBonus(max));

    [Fact]
    public void ACriticalHit_RaisesTheRollAndTheHeal_AndIsTold()
    {
        Spell sure = Rock with { Id = "sure", Crit = 100 };
        Spell sureMend = Mend with { Id = "sure-mend", Crit = 100 };
        var data = new GameData([.. All, sure, sureMend], [new MapSpec("m", Text, ["AA.B"])],
            [new Scenario("s", Text, "m", [Caster(Characteristics.None, "sure", "sure-mend"), Spec(0, 1, hp: 99), Spec(1, hp: 99)])], null, null, null, [Wolf]);
        var f = new Fight(data, "s", 1);
        f.Apply(new CastAction("sure", new Cell(3, 0)));
        Assert.Equal(99 - 12, f.Fighters[2].Hp);
        Assert.True(f.Events.OfType<SpellCast>().Last().Critical);
        f.Fighters[1].Hp = 10;
        f.Apply(new CastAction("sure-mend", new Cell(1, 0)));
        Assert.Equal(10 + 24, f.Fighters[1].Hp);
        // Never with a chance of 0: the fixed rock always does its 10.
        Fight never = Play(["AB"], Caster(Characteristics.None, "rock"), Spec(1, hp: 99));
        never.Apply(new CastAction("rock", new Cell(1, 0)));
        Assert.Equal((99 - 10, false), (never.Fighters[1].Hp, never.Events.OfType<SpellCast>().Single().Critical));
    }

    [Fact]
    public void ACriticalHit_ComesAboutAsOftenAsTheSpellSays()
    {
        Spell often = Rock with { Id = "often", Crit = 25 };
        var data = new GameData([.. All, often], [new MapSpec("m", Text, ["AB"])],
            [new Scenario("s", Text, "m", [Caster(Characteristics.None, "often"), Spec(1, hp: 9999)])], null, null, null, [Wolf]);
        int critical = 0;
        for (ulong seed = 0; seed < 2000; seed++)
        {
            var f = new Fight(data, "s", seed);
            f.Apply(new CastAction("often", new Cell(1, 0)));
            critical += f.Events.OfType<SpellCast>().Single().Critical ? 1 : 0;
        }
        Assert.InRange(critical, 440, 560);
    }

    [Fact]
    public void ASpellSureOfItsCriticalHit_LandsOneEveryTime()
    {
        Spell sure = Rock with { Id = "sure", Crit = 100 };
        var data = new GameData([.. All, sure], [new MapSpec("m", Text, ["AB"])],
            [new Scenario("s", Text, "m", [Caster(Characteristics.None, "sure"), Spec(1, hp: 9999)])], null, null, null, [Wolf]);
        for (ulong seed = 0; seed < 1000; seed++)
        {
            var f = new Fight(data, "s", seed);
            f.Apply(new CastAction("sure", new Cell(1, 0)));
            Assert.True(f.Events.OfType<SpellCast>().Single().Critical, $"seed {seed}");
        }
    }

    [Fact]
    public void NeutralDamage_AndHealing_GrowWithNoCharacteristic()
    {
        // D62: every characteristic gives its element's damage, and nothing else.
        Fight f = Play(["AA.B"], Caster(new Characteristics(10, 50, 50, 50, 50), "mend", "strike"), Spec(0, 1, hp: 99), Spec(1, 0, hp: 99));
        f.Fighters[1].Hp = 10;
        f.Apply(new CastAction("mend", new Cell(1, 0)));
        Assert.Equal(30, f.Fighters[1].Hp);
        Fight n = Play(["AB"], Caster(new Characteristics(0, 50, 50, 50, 50), "strike"), Spec(1, hp: 99));
        n.Apply(new CastAction("strike", new Cell(1, 0)));
        Assert.Equal(99 - 10, n.Fighters[1].Hp);
    }

    [Fact]
    public void ARank_ReplacesTheNumbersItGives_AndKeepsTheOthers()
    {
        Assert.Equal(4, Ranked.MaxRank);
        Assert.Same(Ranked, Ranked.AtRank(1));
        Assert.Equal((6, 8, 3, 2, 1), (Ranked.AtRank(2).DamageMin, Ranked.AtRank(2).DamageMax, Ranked.AtRank(2).ApCost, Ranked.AtRank(2).MaxRange, Ranked.AtRank(2).PerTurn));
        Assert.Equal((8, 10, 2), (Ranked.AtRank(3).DamageMin, Ranked.AtRank(3).DamageMax, Ranked.AtRank(3).ApCost));
        Assert.Equal((10, 12, 4, 2), (Ranked.AtRank(4).DamageMin, Ranked.AtRank(4).DamageMax, Ranked.AtRank(4).MaxRange, Ranked.AtRank(4).PerTurn));
        // A fighter's spells come at the ranks its description gives.
        Fight f = Play(["AB"], Spec(0, spells: ["ranked"]) with { SpellRanks = new Dictionary<string, int> { ["ranked"] = 3 } }, Spec(1));
        Assert.Equal(2, f.Fighters[0].Spells.Single().ApCost);
    }

    [Fact]
    public void AHero_GetsTheSpellsItsLevelHasUnlocked()
    {
        var hunter = new HeroClass("hunter", Text, Text, 50, 6, 3, 100, ["strike", "late"]);
        GameData data = Data(["AB"], [Spec(0), Spec(1)], [hunter]);
        Assert.Equal(["strike"], new Fight(data, "s", 1, new Hero("Élise", "female-c", "hunter", Level: 19)).Fighters[0].Spells.Select(s => s.Id));
        Assert.Equal(["strike", "late"], new Fight(data, "s", 1, new Hero("Élise", "female-c", "hunter", Level: 20)).Fighters[0].Spells.Select(s => s.Id));
        Assert.NotNull(new Hero("Élise", "female-c", "hunter", Level: 101).Problem(data));
        Assert.NotNull(new Hero("Élise", "female-c", "hunter", Level: 0).Problem(data));
    }

    [Fact]
    public void ASummon_JoinsTheTeam_PlaysRightAfter_AndCountsAgainstTheLimit()
    {
        Fight f = Play(["A..B"], Caster(Characteristics.None, "call"), Spec(1, initiative: 500));
        Assert.Equal(ActionError.None, f.Apply(new CastAction("call", new Cell(1, 0))));
        Fighter wolf = f.Fighters[2];
        Assert.Equal((0, 0, "wolf", 20), (wolf.Team, wolf.Summoner, wolf.SummonKind, wolf.Hp));
        Assert.Equal(new Summoned(2, 0, new Cell(1, 0)), f.Events.OfType<Summoned>().Single());
        Assert.Equal([0, 2, 1], f.TurnOrder.Select(x => x.Id));
        Assert.Equal(ActionError.TooManySummons, f.Check(new CastAction("call", new Cell(2, 0))));
        Assert.Equal(ActionError.Occupied, f.Check(new CastAction("call", new Cell(3, 0))));
        f.Apply(new EndTurnAction());
        Assert.Equal(2, f.Current.Id);
    }

    [Fact]
    public void Summons_DieWithTheirSummoner()
    {
        // The enemy strikes the summoner dead: the wolf goes with it, and team A has lost.
        Fight f = Play(["AB.."], Spec(0, hp: 5, ap: 99, mp: 0, initiative: 999, spells: ["call"]), Spec(1, initiative: 10, spells: ["strike"]));
        f.Apply(new CastAction("call", new Cell(2, 0)));
        f.Apply(new EndTurnAction());
        f.Apply(new EndTurnAction());
        f.Apply(new CastAction("strike", new Cell(0, 0)));
        Assert.False(f.Fighters[2].IsAlive);
        Assert.True(f.IsOver);
        Assert.Equal(1, f.WinningTeam);
    }

    [Fact]
    public void AFightWithSummons_ReplaysTheSame()
    {
        GameData data = Data(["A....B", "......"], [Caster(new Characteristics(Earth: 20), "call", "strike"), Spec(1, hp: 80, spells: ["strike", "bow"])]);
        var f = new Fight(data, "s", 3);
        Ai.PlayOut(f);
        Assert.Contains(f.Events, e => e is Summoned);
        Assert.Equal(Fingerprint(f), Fingerprint(FightRecord.FromJson(FightRecord.Of(f).ToJson()).Replay(data)));
    }

    [Fact]
    public void InconsistentRanksAndSummons_AreRefused()
    {
        GameData With(params Spell[] spells) => new([.. Spells, .. spells], [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])], null, null, null, [Wolf]);
        Assert.Throws<InvalidDataException>(() => With(Call with { Effects = [new SummonEffect(Affects.All, "dragon")] }));
        Assert.Throws<InvalidDataException>(() => With(Ranked with { Ranks = [.. Ranked.Ranks!, new SpellRank(1, 2), new SpellRank(1, 2)] }));
        Assert.Throws<InvalidDataException>(() => With(Ranked with { Ranks = [new SpellRank(5, 2)] }));
        Assert.Throws<InvalidDataException>(() => With(Late with { Level = 0 }));
        Assert.Throws<InvalidDataException>(() => new GameData(Spells, [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])], null, null, null, [Wolf with { Spells = ["nothing"] }]));
    }
}
