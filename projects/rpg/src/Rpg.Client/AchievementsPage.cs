using Rpg.Core;

namespace Rpg.Client;

/// <summary>An achievement as the game shows it (sprint 155): its words, how far, its points, its title if it gives one.</summary>
public sealed record AchievementLine(string Id, string Name, string Text, string Progress, int Points, bool Earned, string? Title);

/// <summary>A category of achievements, with how many are earned.</summary>
public sealed record AchievementCategory(string Id, string Name, int Earned, IReadOnlyList<AchievementLine> Lines);

/// <summary>
/// The achievements as the game shows them (sprint 155, D83), without any engine: by category in the data's order, each
/// line with its progress; the titles one may wear; a title's words in the player's language.
/// </summary>
public static class AchievementsPage
{
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
                return new AchievementLine(a.Id, a.Name.In(texts.Lang), a.Text.In(texts.Lang), texts["achievements.progress", p.Reached, p.Count], a.Points, p.Earned, a.Title?.In(texts.Lang));
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
