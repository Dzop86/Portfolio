using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>Friends, ignored characters and whispers (sprint 126, D79).</summary>
public class ContactTests
{
    [Theory]
    [InlineData("/w Plop salut toi", "Plop", "salut toi")]
    [InlineData("  /W plop  on y va ? ", "plop", "on y va ?")]
    [InlineData("/whisper Plouf ok", "Plouf", "ok")]
    public void AWhisper_IsANameThenItsText(string line, string name, string text) =>
        Assert.Equal((name, text), Whispers.Parse(line));

    [Theory]
    [InlineData("salut")]
    [InlineData("/w Plop")]
    [InlineData("/w Plop   ")]
    [InlineData("/wPlop salut")]
    [InlineData(null)]
    public void ALineThatIsNotAWhisper_IsLeftAlone(string? line) => Assert.Null(Whispers.Parse(line));

    [Fact]
    public void AContact_IsNeverOneself_NorTwice_AndAHundredAtMost()
    {
        Assert.Null(Contacts.WhyNotAdd(ownCharacter: false, already: false, count: Contacts.Most - 1));
        Assert.NotNull(Contacts.WhyNotAdd(ownCharacter: true, already: false, count: 0));
        Assert.NotNull(Contacts.WhyNotAdd(ownCharacter: false, already: true, count: 0));
        Assert.NotNull(Contacts.WhyNotAdd(ownCharacter: false, already: false, count: Contacts.Most));
    }

    [Fact]
    public void AFriendsZone_IsTheVillageOrTheZone_NeverTheMap()
    {
        Assert.All(Real.Towns.Values.Where(t => Shop.InVillage(t.Id)), t => Assert.Equal(Shop.Village, Contacts.ZoneOf(t.Id, Real)));
        Town woods = Real.Towns.Values.First(t => t.Zone is not null && !Shop.InVillage(t.Id));
        Assert.Equal(woods.Zone, Contacts.ZoneOf(woods.Id, Real));
        // Every map of a zone gives the same answer.
        Assert.Single(Real.Towns.Values.Where(t => t.Zone == woods.Zone).Select(t => Contacts.ZoneOf(t.Id, Real)).Distinct());
    }
}
