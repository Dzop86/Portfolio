using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The contacts window as it lays its lists out, without any engine (Charles, after sprint 144, the way Dofus does it):
/// the friends in play first, then those offline, each part by name; how many contacts of the hundred allowed.
/// </summary>
public static class ContactsView
{
    /// <summary>The friends in play, by name.</summary>
    public static IReadOnlyList<Contact> InPlay(ContactList list) => Sorted(Friends(list).Where(c => c.Zone is not null));

    /// <summary>The friends offline, by name.</summary>
    public static IReadOnlyList<Contact> Offline(ContactList list) => Sorted(Friends(list).Where(c => c.Zone is null));

    /// <summary>Those ignored, by name.</summary>
    public static IReadOnlyList<Contact> Ignored(ContactList list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return Sorted(list.Ignored);
    }

    /// <summary>The contacts kept, friends and ignored together, against the most an account may keep (<see cref="Contacts.Most"/>).</summary>
    public static (int Count, int Most) Room(ContactList list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return (list.Friends.Count + list.Ignored.Count, Contacts.Most);
    }

    private static IReadOnlyList<Contact> Friends(ContactList list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return list.Friends;
    }

    private static Contact[] Sorted(IEnumerable<Contact> contacts) => [.. contacts.OrderBy(c => c.Name, StringComparer.InvariantCultureIgnoreCase)];
}
