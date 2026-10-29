using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The elements' returns (sprint 162, D85): past 3 × the level + 5, an element's points count a quarter.</summary>
public class ElementReturnsTests
{
    [Theory]
    [InlineData(60, 50, 60)]
    [InlineData(155, 50, 155)]
    [InlineData(245, 50, 177)]
    [InlineData(158, 50, 155)]
    [InlineData(159, 50, 156)]
    [InlineData(245, 0, 245)]
    public void ThePointsPastTheThreshold_CountAQuarter(int points, int level, int effective) =>
        Assert.Equal(effective, Characteristics.Effective(points, level == 0 ? 0 : Characteristics.Threshold(level)));

    [Fact]
    public void AtFifty_AHeroOfOneElement_Strikes34_TheHeroOfFour_AsBefore()
    {
        // The game designer's numbers (docs/suite-apres-d69.md §12): half of 490 points in one element, +48 before; half past
        // the threshold left it at 4.4 hordes of 10, a quarter past 3 × level + 5 (T129) strikes +34.
        Assert.Equal(48, Characteristics.Bonus(245));
        Assert.Equal(34, Characteristics.Bonus(Characteristics.Effective(245, Characteristics.Threshold(50))));
        Assert.Equal(Characteristics.Bonus(60), Characteristics.Bonus(Characteristics.Effective(60, Characteristics.Threshold(50))));
    }

    private static Hero OfEarth(int earth) => new("Élise", "female-c", "guard", Level: 50, Stats: new Characteristics(Earth: earth));

    [Fact]
    public void AFight_CountsAHerosPointsWithTheCurve_AndAMonstersWithout()
    {
        var fight = new Fight(Real, "training", 1, OfEarth(245));
        Fighter me = fight.Fighters[0], target = fight.Fighters[1];
        Assert.Equal((155, 0), (me.Spec.Threshold, target.Spec.Threshold));
        var linear = new Fight(Real, "training", 1, OfEarth(245), linearReturns: true);
        Assert.Equal(0, linear.Fighters[0].Spec.Threshold);
        // The same roll, the same target: 14 points of bonus fewer, scaled by what the hero's trunk and the target add.
        Assert.True(Fight.Damage(10, me, target, Element.Earth) < Fight.Damage(10, linear.Fighters[0], linear.Fighters[1], Element.Earth));
        // Under the threshold, nothing changes.
        var spread = new Fight(Real, "training", 1, OfEarth(150));
        var spreadBefore = new Fight(Real, "training", 1, OfEarth(150), linearReturns: true);
        Assert.Equal(Fight.Damage(10, spreadBefore.Fighters[0], spreadBefore.Fighters[1], Element.Earth),
            Fight.Damage(10, spread.Fighters[0], spread.Fighters[1], Element.Earth));
    }

    [Fact]
    public void ARecordOfVersionFour_ReplaysTheCurve_OneOfVersionThree_TheLinearBonus()
    {
        Hero hero = Progression.OfOne(Progression.Built(OfEarth(0), Real), Element.Earth, Real);
        var now = new Fight(Real, "training", 7, hero);
        Ai.PlayOut(now);
        FightRecord r = FightRecord.FromJson(FightRecord.Of(now).ToJson());
        Assert.Equal(4, r.Version);
        Assert.Equal(Fingerprint(now), Fingerprint(r.Replay(Real)));
        var before = new Fight(Real, "training", 7, hero, linearReturns: true);
        Ai.PlayOut(before);
        FightRecord old = FightRecord.FromJson(FightRecord.Of(before).ToJson()) with { Version = 3 };
        Assert.Equal(Fingerprint(before), Fingerprint(old.Replay(Real)));
        Assert.Equal(0, old.Replay(Real).Fighters[0].Spec.Threshold);
    }
}
