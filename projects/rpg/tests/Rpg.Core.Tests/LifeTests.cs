using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The life kept between fights (sprint 101, D73, docs/suite-apres-d69.md §5 bis).</summary>
public class LifeTests
{
    private static readonly DateTimeOffset T = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TheLife_GrowsBackOnePercentASecond_AtLeastOne_TwiceAsFastInTheVillage()
    {
        Assert.Equal((1d, 1d, 3d), (Life.PerSecond(50), Life.PerSecond(100), Life.PerSecond(300)));
        // Charles's example: 20 of 100, then one a second.
        Assert.Equal(20, Life.Now(100, 20, T, false, "woods-0-0", T));
        Assert.Equal(30, Life.Now(100, 20, T, false, "woods-0-0", T.AddSeconds(10)));
        Assert.Equal(40, Life.Now(100, 20, T, false, Shop.Village, T.AddSeconds(10)));
        Assert.Equal(100, Life.Now(100, 20, T, false, "woods-0-0", T.AddMinutes(5)));
        Assert.Equal(100, Life.Now(100, null, null, false, "woods-0-0", T));
    }

    [Fact]
    public void InAFight_TheLifeIsFrozen_AndAFightNeverReported_IsLost()
    {
        Assert.Equal(20, Life.Now(100, 20, T, true, "woods-0-0", T.AddMinutes(10)));
        // Abandoned: 1 hit point at the thirtieth minute, growing back from there.
        Assert.Equal(1, Life.Now(100, 20, T, true, "woods-0-0", T + Life.Abandoned));
        Assert.Equal(11, Life.Now(100, null, T, true, "woods-0-0", T + Life.Abandoned + TimeSpan.FromSeconds(10)));
        Assert.True(Life.Low(29, 100));
        Assert.False(Life.Low(30, 100));
    }

    [Fact]
    public void TheMaximum_IsTheFightersOutOfAFight()
    {
        var hero = Progression.Built(new Hero("Élise", "female-c", "guard", Level: 10), Real);
        var fight = new Fight(Real, "training", 1, hero);
        Assert.Equal(fight.HeroFighter!.MaxHp, Life.MaxHp(hero, Real));
        Assert.Equal(40, Real.Items["bread"].Heal);
        Assert.Equal(10L, Shop.SellPrice(Real.Items["bread"], Real));
    }
}
