using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The tree of sprint 146 (D82, <c>docs/talents-arbre-questions.md</c>): the points (since sprint 182, D93, 1 at 30, then one
/// every 3 levels), the trunk lit by itself (+3 % damage at 30, lowered from 5 by the Disjoint's measure (J7 b) on every spell
/// and outside the talents' ceiling, +3 % resistances at 35 under the ceiling of 90, +5 % hit points at 40 with Vitality), and
/// the styles open with no door.
/// </summary>
public class TrunkTests
{
    // A hundred of damage, fixed, neutral and of Fire; a class with no hit points by level, to read the trunk's alone.
    private static readonly Spell Hundred = new("hundred", Text, 1, 1, 6, false, false, 100, 100, 9);
    private static readonly Spell Flame = new("flame", Text, 1, 1, 6, false, false, 100, 100, 9, Element.Fire);
    private static readonly HeroClass Tester = new("tester", Text, Text, 200, 99, 6, 999, ["hundred", "flame"], HpPerLevel: 0);

    private static Fight Play(int level, Characteristics? stats = null, FighterSpec? monster = null) =>
        new(new GameData([.. Spells, Hundred, Flame], [new MapSpec("m", Text, ["A..B"])],
                [new Scenario("s", Text, "m", [Spec(0, initiative: 999, spells: ["strike"]), monster ?? Spec(1, hp: 1000, initiative: 1, spells: ["hundred", "flame"])])], [Tester]),
            "s", 1, new Hero("Ondine", "female-a", "tester", Level: level, Stats: stats));

    [Theory]
    [InlineData(29, 0)]
    [InlineData(30, 1)]
    [InlineData(33, 2)]
    [InlineData(45, 6)]
    [InlineData(60, 11)]
    [InlineData(100, 24)]
    public void ThePoints_AreOneAtThirty_ThenOneEveryThreeLevels(int level, int points) => Assert.Equal(points, Talents.Points(level));

    [Fact]
    public void TheTrunk_LightsUpByItself_AtThirty_ThirtyFive_AndForty()
    {
        Assert.Equal([(30, TrunkGain.Damage, 3), (35, TrunkGain.Resistance, 3), (40, TrunkGain.Hp, 5)], Talents.Trunk.Select(n => (n.Level, n.Gain, n.Percent)));
        Assert.Equal((0, 0, 0), (Talents.TrunkDamage(29), Talents.TrunkResistance(29), Talents.TrunkHp(29)));
        Assert.Equal((3, 0, 0), (Talents.TrunkDamage(34), Talents.TrunkResistance(34), Talents.TrunkHp(34)));
        Assert.Equal((3, 3, 0), (Talents.TrunkDamage(39), Talents.TrunkResistance(39), Talents.TrunkHp(39)));
        Assert.Equal((3, 3, 5), (Talents.TrunkDamage(100), Talents.TrunkResistance(100), Talents.TrunkHp(100)));
    }

    [Fact]
    public void TheTrunksDamage_GoesToEverySpell_NeutralIncluded()
    {
        int Hit(int level, string spell)
        {
            Fight f = Play(level);
            f.Apply(new CastAction(spell, new Cell(3, 0)));
            return 1000 - f.Fighters[1].Hp;
        }
        Assert.Equal((100, 100), (Hit(29, "hundred"), Hit(29, "flame")));
        Assert.Equal((103, 103), (Hit(30, "hundred"), Hit(30, "flame")));
        Assert.Equal(3, Play(30).HeroFighter!.Spec.TreeDamage);
    }

    [Fact]
    public void TheTrunksDamage_IsOutsideTheTalentsCeiling()
    {
        // Up close (+10), Firm Footing (+15 standing still) and First Gesture (+20): 45, held to the ceiling of 35; the
        // trunk's 3 % on top: 100 × 1.35 × 1.03.
        // No door since sprint 182; a neutral spell sets nothing off.
        Hero hero = new("Ondine", "female-a", "tester", Level: 36, Talents: ["up-close", "firm-footing", "first-gesture"]);
        Assert.Null(Talents.Problem(hero.Talents!, 36, Real));
        var data = new GameData([.. Spells, .. TreeSpells, Hundred, Flame], [new MapSpec("m", Text, ["A..B"])],
            [new Scenario("s", Text, "m", [Spec(0, initiative: 999, spells: ["strike"]), Spec(1, hp: 1000, initiative: 1, spells: ["hundred"])])], [Tester],
            summons: TreeSummons, talents: Real.Talents.Values, reactions: Real.Reactions.Values);
        var f = new Fight(data, "s", 1, hero);
        f.Apply(new CastAction("hundred", new Cell(3, 0)));
        Assert.Equal(139, 1000 - f.Fighters[1].Hp);
    }

    [Fact]
    public void TheTrunksResistance_IsAddedToEachElement_AndToTheCompounds()
    {
        Fighter at34 = Play(34).HeroFighter!, at35 = Play(35).HeroFighter!;
        Assert.All(new[] { Element.Earth, Element.Fire, Element.Water, Element.Air, Element.Lightning }, e =>
        {
            Assert.Equal(0, at34.Resistance(e));
            Assert.Equal(3, at35.Resistance(e));
        });
        Assert.Equal(0, at35.Resistance(Element.Neutral));
        // A monster's Fire hit of 100 on the hero: 97.
        Fight f = Play(35);
        Assert.Equal(97, Fight.Damage(100, f.Fighters[1], f.HeroFighter!, Element.Fire));
    }

    [Fact]
    public void TheTrunksHitPoints_AreFivePercentOfTheMaximum_VitalityIncluded()
    {
        Assert.Equal(200, Play(39).HeroFighter!.MaxHp);
        Assert.Equal(210, Play(40).HeroFighter!.MaxHp);
        Assert.Equal((200 + 100) * 105 / 100, Play(40, new Characteristics(Vitality: 100)).HeroFighter!.MaxHp);
    }

    [Fact]
    public void TheStyles_OpenWithNoDoor_AndTheTreeHoldsNoElement()
    {
        // Sprint 182 (D93): 41 talents, six styles, no pair, no Heart, no braid; a first floor of each style at 30, alone.
        Assert.Equal(41, Real.Talents.Count);
        Assert.All(Real.Talents.Values, t => Assert.Contains(t.Branch, new[] { Branch.Alchemy, Branch.Melee, Branch.Distance, Branch.Terrain, Branch.Tempo, Branch.Support }));
        foreach (TalentSpec first in Real.Talents.Values.Where(t => t.Tier == Tier.First))
            Assert.Null(Talents.Problem([first.Id], 30, Real));
        Assert.Equal("At most 1 talent points at level 30.", Talents.Problem(["side-step", "backed"], 30, Real));
    }

    [Fact]
    public void TheReferenceBuilds_AreLegal_AndTheMattersCreateLightning()
    {
        foreach ((string build, IReadOnlyList<string> talents) in Progression.Builds)
        {
            Hero cls = new("Ondine", "female-a", build.Split('-')[0] is "support" ? "warrior" : build.Split('-')[0], Level: 45);
            Hero at45 = Progression.Talented(cls, build, Real);
            Assert.Null(at45.Problem(Real));
            Assert.Equal(build.EndsWith("-matter", StringComparison.Ordinal) ? [Element.Lightning] : null, at45.Compounds);
            if (at45.Compounds is not null)
                Assert.Contains(at45.Deck!, id => Real.Spells[id].Element == Element.Lightning);
            // Under 30, no compound: Lightning waits for the Awakening.
            Assert.Null(Progression.Talented(cls with { Level = 29 }, build, Real).Compounds);
        }
    }
}
