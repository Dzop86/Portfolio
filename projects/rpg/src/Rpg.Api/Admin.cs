using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api;

/// <summary>What the server's operator asks for the characters of an account (sprint 79).</summary>
/// <remarks>Since sprint 146 (D82, I6 b), <see cref="Builds"/> gives each character its class's reference build of Matter.</remarks>
public sealed record AdminOrder(string Account, int? Level, IReadOnlyList<ItemCount> Give, IReadOnlyList<NewHero>? Create = null, IReadOnlyList<Renaming>? Rename = null,
    bool Builds = false);

/// <summary>A character of the account the operator renames (sprint 94, Charles): its name, then the new one.</summary>
public sealed record Renaming(string From, string To);

/// <summary>A character the operator creates for an account (sprint 93): its class and its name.</summary>
public sealed record NewHero(string Class, string Name);

/// <summary>
/// The server's administration command, run where the server runs and never over HTTP:
/// <c>Rpg.Api admin ACCOUNT [--create CLASS:Name]... [--rename Old:New]... [--level N] [--give ITEM[:COUNT]]... [--builds]</c> first creates
/// characters for the account (the names and the limit of the sign-up apply), renames some (a name of the
/// sign-up's rule, free on the server), then brings every character of the
/// account to a level (its experience with it; the points spent are given back if the level goes down)
/// and gives them items of the data files; with <c>--builds</c> (sprint 146), each one's points, deck and talents become
/// its class's reference build of Matter (<see cref="Progression.Builds"/>). For the operator's tests (D65): no account, password or
/// character is written in the repository.
/// </summary>
public static class Admin
{
    public const string Verb = "admin";

    public const string Usage = "Rpg.Api admin ACCOUNT [--create CLASS:Name]... [--rename Old:New]... [--level N] [--give ITEM[:COUNT]]... [--builds]";

    /// <summary>The order, or the reason it cannot be one.</summary>
    public static (AdminOrder? Order, string? Error) Parse(IReadOnlyList<string> args)
    {
        if (args.Count < 2 || args[0] != Verb || args[1].StartsWith("--", StringComparison.Ordinal))
            return (null, Usage);
        int? level = null;
        var give = new List<ItemCount>();
        var create = new List<NewHero>();
        var rename = new List<Renaming>();
        bool builds = false;
        for (int i = 2; i < args.Count; i++)
        {
            string value = i + 1 < args.Count ? args[i + 1] : "";
            switch (args[i])
            {
                case "--level" when int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int l) && l is >= 1 and <= Hero.MaxLevel:
                    level = l;
                    i++;
                    break;
                case "--level":
                    return (null, $"--level: 1 to {Hero.MaxLevel}.");
                case "--give":
                    string[] parts = value.Split(':');
                    int count = 1;
                    if (!GameData.Embedded.Items.ContainsKey(parts[0])
                        || parts.Length > 2
                        || (parts.Length == 2 && (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out count) || count < 1)))
                        return (null, $"--give: an item of the data files, and a count of 1 or more ('{value}').");
                    give.Add(new ItemCount(parts[0], count));
                    i++;
                    break;
                case "--create":
                    string[] hero = value.Split(':');
                    if (hero.Length != 2 || GameData.Embedded.Class(hero[0]) is null || !Hero.IsValidName(hero[1]))
                        return (null, $"--create: a class of the data files and a name of 3 to 20 letters, the first a capital ('{value}').");
                    create.Add(new NewHero(hero[0], hero[1]));
                    i++;
                    break;
                case "--rename":
                    string[] names = value.Split(':');
                    if (names.Length != 2 || names[0].Length == 0 || !Hero.IsValidName(names[1]))
                        return (null, $"--rename: a character's name and a new name of 3 to 20 letters, the first a capital ('{value}').");
                    rename.Add(new Renaming(names[0], names[1]));
                    i++;
                    break;
                case "--builds":
                    builds = true;
                    break;
                default:
                    return (null, $"Unknown: '{args[i]}'. {Usage}");
            }
        }
        if (level is null && give.Count == 0 && create.Count == 0 && rename.Count == 0 && !builds)
            return (null, "Nothing to do: --create, --rename, --level, --give or --builds.");
        return (new AdminOrder(args[1], level, give, create, rename, builds), null);
    }

    /// <summary>Applies the order; says what it did, one line per character.</summary>
    public static async Task<IReadOnlyList<string>> Apply(GameDb db, AdminOrder order, CancellationToken cancel = default)
    {
        string normalized = order.Account.ToUpperInvariant();
        Account? account = await db.Accounts.Include(a => a.Characters).FirstOrDefaultAsync(a => a.NormalizedName == normalized, cancel);
        if (account is null)
            return [$"No account named '{order.Account}'."];
        var done = new List<string>();
        foreach (NewHero hero in order.Create ?? [])
        {
            string name = hero.Name.ToUpperInvariant();
            if (account.Characters.Count >= Accounts.MaxCharacters)
                done.Add($"{hero.Name}: not created, at most {Accounts.MaxCharacters} characters per account.");
            else if (await db.Characters.AnyAsync(c => c.NormalizedName == name, cancel))
                done.Add($"{hero.Name}: not created, the name is taken.");
            else
                // Added to the context itself: its id is already drawn, and through the collection alone it would be taken for an update.
                db.Characters.Add(new Character { Account = account, AccountId = account.Id, Name = hero.Name, NormalizedName = name, Look = "female-d", Class = hero.Class, CreatedAt = DateTimeOffset.UtcNow });
        }
        foreach (Renaming r in order.Rename ?? [])
        {
            string from = r.From.ToUpperInvariant(), to = r.To.ToUpperInvariant();
            Character? c = account.Characters.FirstOrDefault(c => c.NormalizedName == from);
            if (c is null)
                done.Add($"{r.From}: no such character on '{account.Name}'.");
            // Its own name in other letters ("plop" to "Plop") is no conflict.
            else if (await db.Characters.AnyAsync(o => o.NormalizedName == to && o.Id != c.Id, cancel))
                done.Add($"{r.From}: not renamed, '{r.To}' is taken.");
            else
                (c.Name, c.NormalizedName) = (r.To, to);
        }
        foreach (Character c in account.Characters.OrderBy(c => c.CreatedAt).ThenBy(c => c.Name))
        {
            if (order.Level is int level)
            {
                if (level < c.Level)
                {
                    // Fewer points than were spent: all of them come back, to be spent again.
                    (c.Vitality, c.Earth, c.Fire, c.Water, c.Air, c.Ranks, c.Deck, c.Talents) = (0, 0, 0, 0, 0, "", "", "");
                }
                c.Xp = Progression.XpToReach(level);
                c.Level = level;
            }
            c.Receive(order.Give);
            if (order.Builds)
            {
                // Its class's reference build of Matter at its level: its points, ranks, deck and talents.
                Hero built = Progression.Talented(c.Hero with { Stats = null, Ranks = null, Deck = null, Talents = null }, $"{c.Class}-matter", GameData.Embedded, c.WornList);
                Characteristics st = built.Stats ?? Characteristics.None;
                (c.Vitality, c.Earth, c.Fire, c.Water, c.Air) = (st.Vitality, st.Earth, st.Fire, st.Water, st.Air);
                c.Ranks = string.Join(',', (built.Ranks ?? new Dictionary<string, int>()).Where(r => r.Value > 1).OrderBy(r => r.Key, StringComparer.Ordinal)
                    .Select(r => $"{r.Key}:{r.Value.ToString(CultureInfo.InvariantCulture)}"));
                c.Deck = built.Deck is { Count: > 0 } deck ? string.Join(',', deck) : Character.EmptyDeck;
                c.Talents = string.Join(',', built.Talents ?? []);
                c.TalentsVersion = Talents.Version;
            }
            done.Add($"{c.Name} ({c.Class}): level {c.Level}, {c.InventoryList.Sum(i => i.Count)} item(s)" + (order.Builds ? $", talents {c.Talents}." : "."));
        }
        await db.SaveChangesAsync(cancel);
        return done.Count == 0 ? [$"'{account.Name}' has no character."] : done;
    }
}
