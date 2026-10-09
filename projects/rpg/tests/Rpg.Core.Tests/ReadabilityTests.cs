using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>What the client shows to make a fight readable (sprint 57): forecasts, the timeline, the element of each hit.</summary>
public class ReadabilityTests
{
    private static readonly Spell Blaze = new("blaze", Text, 2, 1, 6, false, false, 6, 10, 9, Element.Fire, new Area(AreaShape.Cross, 1));
    private static readonly Spell Mend = new("mend", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new HealEffect(Affects.Allies, 10, 14)]);
    private static readonly Spell Drain = new("drain", Text, 2, 1, 6, false, false, 5, 5, 9, Element.Water, null, [new HealEffect(Affects.Caster, 8, 8)]);
    private static readonly Spell Venom = new("venom", Text, 2, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new StatusEffect(Affects.Enemies, Stat.Poison, 6, 2, Element.Water)]);
    private static readonly Spell Shove = new("shove", Text, 2, 1, 6, false, false, 0, 0, 9, Element.Neutral, null, [new PushEffect(Affects.Enemies, 3)]);
    private static readonly Spell[] All = [.. Spells, Blaze, Mend, Drain, Venom, Shove];

    private static Fight Play(ulong seed, string[] rows, params FighterSpec[] fighters) =>
        new(new GameData(All, [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", fighters)]), "s", seed);

    private static FighterSpec Caster(params string[] spells) =>
        Spec(0, ap: 99, mp: 0, initiative: 999, spells: spells) with { Stats = new Characteristics(Intelligence: 50) };

    private static readonly FighterSpec Resistant = Spec(1, hp: 99) with { Resistances = new Dictionary<Element, int> { [Element.Fire] = 25 } };

    [Fact]
    public void AForecast_BoundsEveryRoll_AndReachesBoth()
    {
        var seen = new HashSet<int>();
        for (ulong seed = 0; seed < 60; seed++)
        {
            Fight f = Play(seed, ["A.B"], Caster("blaze"), Resistant);
            Forecast fc = Assert.Single(f.Foresee(f.Fighters[0].Spells[0], new Cell(2, 0)));
            // 6 to 10, +50 % Intelligence, -25 % resistance.
            Assert.Equal((f.Fighters[1], 6, 11, Element.Fire), (fc.Fighter, fc.DamageMin, fc.DamageMax, fc.Element));
            int events = f.Events.Count;
            Assert.Equal(events, f.Events.Count);
            f.Apply(new CastAction("blaze", new Cell(2, 0)));
            Damaged hit = f.Events.OfType<Damaged>().Single();
            Assert.InRange(hit.Amount, fc.DamageMin, fc.DamageMax);
            seen.Add(hit.Amount);
        }
        Assert.Contains(6, seen);
        Assert.Contains(11, seen);
    }

    [Fact]
    public void AForecast_ListsTheWholeArea_AlliesIncluded_AndHealsWithinTheMissingHitPoints()
    {
        Fight f = Play(1, ["A....", "...AB"], Caster("blaze", "mend", "drain"), Spec(0, 1, hp: 50), Spec(1, hp: 50));
        f.Fighters[1].Hp = 45;
        IReadOnlyList<Forecast> cross = f.Foresee(f.Fighters[0].Spells[0], new Cell(3, 1));
        Assert.Equal([1, 2], cross.Select(c => c.Fighter.Id));
        // The heal (10 to 14, +50 %) stops at the 5 hit points missing; the enemy in reach is not healed.
        Forecast heal = Assert.Single(f.Foresee(f.Fighters[0].Spells[1], new Cell(3, 1)));
        Assert.Equal((1, 0, 0, 5, 5), (heal.Fighter.Id, heal.DamageMin, heal.DamageMax, heal.HealMin, heal.HealMax));
        // A spell that heals its caster shows both: the hit on the target, the caster's healing.
        f.Fighters[0].Hp = 10;
        IReadOnlyList<Forecast> drain = f.Foresee(f.Fighters[0].Spells[2], new Cell(4, 1));
        Assert.Equal([(2, 5, 5, 0), (0, 0, 0, 12)], drain.Select(d => (d.Fighter.Id, d.DamageMin, d.DamageMax, d.HealMax)));
    }

    [Fact]
    public void ASureKill_IsOneThatEvenTheLowestRollGetsThroughTheShields()
    {
        Fight f = Play(1, ["A.B"], Caster("blaze"), Spec(1, hp: 9));
        Assert.True(f.Foresee(f.Fighters[0].Spells[0], new Cell(2, 0))[0].SureKill);
        f.Fighters[1].Hp = 10;
        Assert.False(f.Foresee(f.Fighters[0].Spells[0], new Cell(2, 0))[0].SureKill);
    }

    [Fact]
    public void TheTimeline_StartsWithTheCurrentFighter_SkipsTheDead_AndGoesRoundAfterRound()
    {
        Fight f = Play(1, ["A.B", "..B"], Spec(0, initiative: 300, spells: ["strike"]), Spec(1, 0, initiative: 200), Spec(1, 1, initiative: 100));
        Assert.Equal([0, 1, 2, 0, 1], f.NextTurns(5).Select(x => x.Id));
        f.Apply(new EndTurnAction());
        Assert.Equal([1, 2, 0, 1], f.NextTurns(4).Select(x => x.Id));
        f.Fighters[2].Hp = 0;
        Assert.Equal([1, 0, 1, 0], f.NextTurns(4).Select(x => x.Id));
    }

    [Fact]
    public void EveryHit_SaysItsElement_ACollisionIsNeutral()
    {
        Fight f = Play(1, ["A.B#"], Caster("blaze", "venom", "shove"), Spec(1, hp: 99));
        f.Apply(new CastAction("blaze", new Cell(2, 0)));
        f.Apply(new CastAction("shove", new Cell(2, 0)));
        f.Apply(new CastAction("venom", new Cell(2, 0)));
        f.Apply(new EndTurnAction());
        Assert.Equal([Element.Fire, Element.Neutral, Element.Water], f.Events.OfType<Damaged>().Select(d => d.Element));
    }
}
