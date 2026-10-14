using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api;

/// <summary>What the server's operator asks for the characters of an account (sprint 79).</summary>
public sealed record AdminOrder(string Account, int? Level, IReadOnlyList<ItemCount> Give);

/// <summary>
/// The server's administration command, run where the server runs and never over HTTP:
/// <c>Rpg.Api admin ACCOUNT [--level N] [--give ITEM[:COUNT]]...</c> brings every character of the
/// account to a level (its experience with it; the points spent are given back if the level goes down)
/// and gives them items of the data files. For the operator's tests (D65): no account, password or
/// character is written in the repository.
/// </summary>
public static class Admin
{
    public const string Verb = "admin";

    public const string Usage = "Rpg.Api admin ACCOUNT [--level N] [--give ITEM[:COUNT]]...";

    /// <summary>The order, or the reason it cannot be one.</summary>
    public static (AdminOrder? Order, string? Error) Parse(IReadOnlyList<string> args)
    {
        if (args.Count < 2 || args[0] != Verb || args[1].StartsWith("--", StringComparison.Ordinal))
            return (null, Usage);
        int? level = null;
        var give = new List<ItemCount>();
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
                default:
                    return (null, $"Unknown: '{args[i]}'. {Usage}");
            }
        }
        if (level is null && give.Count == 0)
            return (null, "Nothing to do: --level or --give.");
        return (new AdminOrder(args[1], level, give), null);
    }

    /// <summary>Applies the order; says what it did, one line per character.</summary>
    public static async Task<IReadOnlyList<string>> Apply(GameDb db, AdminOrder order, CancellationToken cancel = default)
    {
        string normalized = order.Account.ToUpperInvariant();
        Account? account = await db.Accounts.Include(a => a.Characters).FirstOrDefaultAsync(a => a.NormalizedName == normalized, cancel);
        if (account is null)
            return [$"No account named '{order.Account}'."];
        var done = new List<string>();
        foreach (Character c in account.Characters.OrderBy(c => c.CreatedAt).ThenBy(c => c.Name))
        {
            if (order.Level is int level)
            {
                if (level < c.Level)
                {
                    // Fewer points than were spent: all of them come back, to be spent again.
                    (c.Vitality, c.Earth, c.Fire, c.Water, c.Air, c.Ranks, c.Deck) = (0, 0, 0, 0, 0, "", "");
                }
                c.Xp = Progression.XpToReach(level);
                c.Level = level;
            }
            c.Receive(order.Give);
            done.Add($"{c.Name} ({c.Class}): level {c.Level}, {c.InventoryList.Sum(i => i.Count)} item(s).");
        }
        await db.SaveChangesAsync(cancel);
        return done.Count == 0 ? [$"'{account.Name}' has no character."] : done;
    }
}
