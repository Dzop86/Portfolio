using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// An achievement as the game shows it (sprint 155): its words, how far, its points, its title if it gives one; since sprint 158
/// (D84), its category and group, the icon in relief it is drawn with (<c>assets/icons/bar</c> or <c>relief</c>), and how far of how much.
/// </summary>
public sealed record AchievementLine(string Id, string Name, string Text, string Progress, int Points, bool Earned, string? Title,
    string Category = "", string? Group = null, string Icon = "achievements", int Reached = 0, int Count = 1);

/// <summary>A share earned, as the window shows it: the whole percent below (99 % until the last one).</summary>
public sealed record AchievementShare(int Earned, int Count)
{
    public int Percent => Count == 0 ? 0 : Earned * 100 / Count;
}

/// <summary>A group of a category (sprint 158): its name and its share.</summary>
public sealed record AchievementGroup(string Id, string Name, AchievementShare Share);

/// <summary>A category in the window's tree (sprint 158): its name, its icon, its share, its groups (none for some).</summary>
public sealed record AchievementBranch(string Id, string Name, string Icon, AchievementShare Share, IReadOnlyList<AchievementGroup> Groups);

/// <summary>The window's general progress (sprint 158): the share of every achievement, the points, each category.</summary>
public sealed record AchievementOverview(AchievementShare Share, int Points, IReadOnlyList<AchievementBranch> Categories);

/// <summary>A category of achievements, with how many are earned.</summary>
public sealed record AchievementCategory(string Id, string Name, int Earned, IReadOnlyList<AchievementLine> Lines);

/// <summary>
/// The achievements as the game shows them (sprint 155, D83), without any engine: by category in the data's order, each
/// line with its progress; the titles one may wear; a title's words in the player's language.
/// </summary>
public static class AchievementsPage
{
    /// <summary>The icon in relief of each category (sprint 158): the Menus panel's, and the dungeon gate's for the dungeons.</summary>
    public static string Icon(string category) => category switch
    {
        "Exploration" => "map",
        "Hunt" => "bestiary",
        "Dungeons" => "dungeons",
        "Jobs" => "jobs",
        "Quests" => "journal",
        "Tree" => "talents",
        "Together" => "contacts",
        _ => "achievements",
    };

    /// <summary>The general progress and the tree of categories and groups, in the data's order.</summary>
    public static AchievementOverview Overview(AchievementView view, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(texts);
        var earned = view.Progress.Where(p => p.Earned).Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        AchievementShare Share(IEnumerable<AchievementSpec> some) => new(some.Count(a => earned.Contains(a.Id)), some.Count());
        AchievementBranch[] branches = [.. Achievements.All.GroupBy(a => a.Category).Select(c => new AchievementBranch(c.Key,
            texts["achievements.category." + c.Key], Icon(c.Key), Share(c),
            [.. c.Where(a => a.Group is not null).GroupBy(a => a.Group!).Select(g => new AchievementGroup(g.Key, texts["achievements.group." + g.Key], Share(g)))]))];
        return new AchievementOverview(Share(Achievements.All), view.Points, branches);
    }

    /// <summary>
    /// The achievements of a category (and of one of its groups), or of every category when one searches: those whose name
    /// or sentence holds the words searched, accents and capitals aside; those earned left out when the player hides them.
    /// </summary>
    public static IReadOnlyList<AchievementLine> Lines(AchievementView view, Texts texts, string? category, string? group = null, string? search = null, bool hideEarned = false)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(texts);
        var progress = view.Progress.ToDictionary(p => p.Id, StringComparer.Ordinal);
        string wanted = Plain(search ?? "");
        return [.. Achievements.All
            .Where(a => wanted.Length > 0 || (a.Category == category && (group is null || a.Group == group)))
            .Select(a => Line(a, progress.GetValueOrDefault(a.Id) ?? new AchievementProgress(a.Id, 0, a.Count), texts))
            .Where(l => wanted.Length == 0 || Plain(l.Name).Contains(wanted, StringComparison.Ordinal) || Plain(l.Text).Contains(wanted, StringComparison.Ordinal))
            .Where(l => !hideEarned || !l.Earned)];
    }

    private static AchievementLine Line(AchievementSpec a, AchievementProgress p, Texts texts) =>
        new(a.Id, a.Name.In(texts.Lang), a.Text.In(texts.Lang), texts["achievements.progress", p.Reached, p.Count], a.Points, p.Earned, a.Title?.In(texts.Lang),
            a.Category, a.Group, Icon(a.Category), p.Reached, p.Count);

    // The letters of French and English with an accent, and theirs without: a table, since the game runs without ICU
    // (invariant globalisation), where string.Normalize leaves the accents on.
    private const string Accented = "àâäáãåçéèêëîïíìôöóòõùûüúÿñ", Bare = "aaaaaaceeeeiiiiooooouuuuyn";

    /// <summary>
    /// A number grouped by thousands as the player reads it: a narrow space in French (14 861), a comma in English (14,861).
    /// Written by hand: the game runs without cultures (invariant globalisation).
    /// </summary>
    public static string Grouped(long n, string lang)
    {
        string digits = Math.Abs(n).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var grouped = new System.Text.StringBuilder();
        for (int i = 0; i < digits.Length; i++)
        {
            if (i > 0 && (digits.Length - i) % 3 == 0)
                grouped.Append(lang == "fr" ? '\u202f' : ',');
            grouped.Append(digits[i]);
        }
        return (n < 0 ? "-" : "") + grouped;
    }

    /// <summary>Words to compare: lower case, without their accents, the ligatures written out.</summary>
    public static string Plain(string words)
    {
        var plain = new System.Text.StringBuilder();
        foreach (char ch in (words ?? throw new ArgumentNullException(nameof(words))).Trim().ToLowerInvariant())
        {
            int i = Accented.IndexOf(ch, StringComparison.Ordinal);
            plain.Append(ch switch { 'œ' => "oe", 'æ' => "ae", _ => i >= 0 ? Bare[i].ToString() : ch.ToString() });
        }
        return plain.ToString();
    }

    public static IReadOnlyList<AchievementCategory> Categories(AchievementView view, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(texts);
        var progress = view.Progress.ToDictionary(p => p.Id, StringComparer.Ordinal);
        return [.. Achievements.All.GroupBy(a => a.Category).Select(g =>
        {
            AchievementLine[] lines = [.. g.Select(a =>
            {
                AchievementProgress p = progress.GetValueOrDefault(a.Id) ?? new AchievementProgress(a.Id, 0, a.Count);
                return Line(a, p, texts);
            })];
            return new AchievementCategory(g.Key, texts["achievements.category." + g.Key], lines.Count(l => l.Earned), lines);
        })];
    }

    /// <summary>The titles one may wear: those of the achievements earned.</summary>
    public static IReadOnlyList<(string Achievement, string Title)> Titles(AchievementView view, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(texts);
        return [.. Achievements.Titled.Where(a => view.Progress.Any(p => p.Id == a.Id && p.Earned)).Select(a => (a.Id, a.Title!.In(texts.Lang)))];
    }

    /// <summary>A title's words, from its achievement's id, or null for none or an unknown one.</summary>
    public static string? TitleOf(string? achievement, string lang) =>
        achievement is null ? null : Achievements.All.FirstOrDefault(a => a.Id == achievement)?.Title?.In(lang);

    /// <summary>The achievements earned since the last view (for the word said when one is earned).</summary>
    public static IReadOnlyList<string> NewlyEarned(AchievementView? before, AchievementView after)
    {
        ArgumentNullException.ThrowIfNull(after);
        if (before is null)
            return [];
        var had = before.Progress.Where(p => p.Earned).Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        return [.. after.Progress.Where(p => p.Earned && !had.Contains(p.Id)).Select(p => p.Id)];
    }
}
