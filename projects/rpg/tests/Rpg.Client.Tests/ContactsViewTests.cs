using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The contacts window's lists (Charles, after sprint 144): in play first, then offline, each by name.</summary>
public class ContactsViewTests
{
    private static Contact C(string name, string? zone) => new(Guid.NewGuid(), name, 10, "guard", zone);

    private static readonly ContactList List = new(
        [C("plouf", null), C("Zed", "village"), C("Ana", null), C("bob", "lake")],
        [C("Grincheux", null), C("Acide", null)]);

    [Fact]
    public void TheFriendsInPlay_ComeFirst_ThenThoseOffline_EachByName_CaseAside()
    {
        Assert.Equal(["bob", "Zed"], ContactsView.InPlay(List).Select(c => c.Name));
        Assert.Equal(["Ana", "plouf"], ContactsView.Offline(List).Select(c => c.Name));
        Assert.Equal(["Acide", "Grincheux"], ContactsView.Ignored(List).Select(c => c.Name));
    }

    [Fact]
    public void TheRoom_CountsFriendsAndIgnoredTogether_AgainstAHundred()
    {
        Assert.Equal((6, 100), ContactsView.Room(List));
        Assert.Equal((0, Contacts.Most), ContactsView.Room(new ContactList([], [])));
    }
}
