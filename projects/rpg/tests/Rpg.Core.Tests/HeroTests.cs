using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

public class HeroTests
{
    [Theory]
    [InlineData("Élise", true)]
    [InlineData("Jean-Luc", true)]
    [InlineData("O'Neil", true)]
    [InlineData("Bo", false)]
    [InlineData("Abcdefghijklmnopqrstu", false)]
    [InlineData("Jean--Luc", false)]
    [InlineData("-Luc", false)]
    [InlineData("Luc-", false)]
    [InlineData("R2D2", false)]
    [InlineData("Jean Luc", false)]
    [InlineData(null, false)]
    public void Names_AreLettersWithSingleHyphensOrApostrophesInside(string? name, bool valid) =>
        Assert.Equal(valid, Hero.IsValidName(name));

    [Fact]
    public void TheHero_TakesThePlaceOfTheFirstFighterOfTeamA_WithTheSameCharacteristics()
    {
        var plain = new Fight(Real, "training", 4);
        var withHero = new Fight(Real, "training", 4, new Hero("Élise", "male-c"));
        Fighter h = withHero.Fighters.First(f => f.Team == 0);
        Assert.Equal(("Élise", "Élise", "male-c"), (h.Name.Fr, h.Name.En, h.Spec.Look));
        Assert.Equal(plain.Fighters[h.Id].Spec with { Name = h.Name, Look = h.Spec.Look }, h.Spec);
        Ai.PlayOut(plain);
        Ai.PlayOut(withHero);
        Assert.Equal(plain.History, withHero.History);
    }

    [Fact]
    public void AnInvalidHero_CannotFight()
    {
        Assert.Throws<ArgumentException>(() => new Fight(Real, "training", 1, new Hero("Élise", "orc")));
        Assert.Throws<ArgumentException>(() => new Fight(Real, "training", 1, new Hero("X", "male-a")));
    }

    [Fact]
    public void TheRecord_KeepsTheHero_AndRefusesAnInvalidOne()
    {
        var f = new Fight(Real, "duel", 2, new Hero("Margaux", "female-a"));
        Ai.PlayOut(f);
        FightRecord r = FightRecord.FromJson(FightRecord.Of(f).ToJson());
        Assert.Equal(new Hero("Margaux", "female-a"), r.Hero);
        Assert.Equal("Margaux", r.Replay(Real).Fighters[0].Name.Fr);
        var cheat = r with { Hero = new Hero("Margaux", "orc") };
        Assert.Throws<InvalidFightRecordException>(() => cheat.Replay(Real));
        // Without a hero, nothing is written: the records of sprint 45 are unchanged.
        var plain = new Fight(Real, "duel", 2);
        Assert.DoesNotContain("hero", FightRecord.Of(plain).ToJson(), StringComparison.Ordinal);
    }
}
