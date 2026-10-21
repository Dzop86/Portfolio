using System.Globalization;
using System.Text;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>The journal's tabs (sprint 116, D77): the quests under way, those done, those the player hid.</summary>
public enum JournalTab
{
    Active,
    Done,
    Hidden,
}

/// <summary>A zone of the journal's list, its quests shown (sprint 116).</summary>
public sealed record JournalSection(string Name, IReadOnlyList<JournalQuest> Quests);

/// <summary>
/// What the journal shows, as the player left it (sprint 116, D77): its tab, the words searched, the levels and
/// kinds kept, the quest whose card is open, the zones folded. Kept between two openings of the journal.
/// </summary>
public sealed class JournalView
{
    public JournalTab Tab { get; set; } = JournalTab.Active;
    public string Search { get; set; } = "";
    public int MinLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = Hero.MaxLevel;
    public HashSet<QuestKind> Kinds { get; } = [.. Enum.GetValues<QuestKind>()];
    public HashSet<string> Folded { get; } = new(StringComparer.Ordinal);
    public string? Selected { get; set; }

    /// <summary>
    /// The tab's quests that pass the search (in the name, the giver, the zone, the words or an objective, whatever
    /// the case and the accents), the levels and the kinds, zone by zone (the village first), the lowest levels first.
    /// A hidden quest shows only under <see cref="JournalTab.Hidden"/>.
    /// </summary>
    public IReadOnlyList<JournalSection> Page(IReadOnlyList<JournalZone> journal, IReadOnlySet<string> hidden)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(hidden);
        string[] words = Fold(Search).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool Shown(JournalQuest q) =>
            q.Level >= MinLevel && q.Level <= MaxLevel && Kinds.Contains(q.Kind)
            && words.All(w => Fold(string.Join(' ', [q.Name, q.Giver, q.Zone, q.Text, .. q.Objectives.Select(o => o.Line)])).Contains(w, StringComparison.Ordinal));
        return [.. journal.Select(z => new JournalSection(z.Name, [.. InTab(z, hidden).Where(Shown).OrderBy(q => q.Level).ThenBy(q => q.Name, StringComparer.CurrentCulture)]))
            .Where(s => s.Quests.Count > 0)];
    }

    /// <summary>How many quests a tab holds, before any search or filter.</summary>
    public IReadOnlyDictionary<JournalTab, int> Counts(IReadOnlyList<JournalZone> journal, IReadOnlySet<string> hidden)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(hidden);
        return Enum.GetValues<JournalTab>().ToDictionary(t => t, t => journal.Sum(z => InTab(z, hidden, t).Count()));
    }

    /// <summary>The quest whose card shows: the one chosen if the page still shows it, else the page's first.</summary>
    public JournalQuest? Card(IReadOnlyList<JournalSection> page)
    {
        ArgumentNullException.ThrowIfNull(page);
        JournalQuest[] all = [.. page.SelectMany(s => s.Quests)];
        return all.FirstOrDefault(q => q.Id == Selected) ?? all.FirstOrDefault();
    }

    private IEnumerable<JournalQuest> InTab(JournalZone zone, IReadOnlySet<string> hidden, JournalTab? tab = null) => (tab ?? Tab) switch
    {
        JournalTab.Active => zone.Active.Where(q => !hidden.Contains(q.Id)),
        JournalTab.Done => zone.Done.Where(q => !hidden.Contains(q.Id)),
        _ => zone.Active.Concat(zone.Done).Where(q => hidden.Contains(q.Id)),
    };

    // Lower case without accents: "Écorce" is found by "ecorce".
    private static string Fold(string text)
    {
        var folded = new StringBuilder(text.Length);
        foreach (char c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                folded.Append(char.ToLowerInvariant(c));
        }
        return folded.ToString();
    }
}

/// <summary>
/// The quests a player hid in the journal (sprint 116), kept on their computer, one line "character quest" each,
/// as the quest followed is: every character its own.
/// </summary>
public static class HiddenQuests
{
    /// <summary>The character's hidden quests in the kept text.</summary>
    public static HashSet<string> Read(string? kept, Guid character) =>
        [.. (kept ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Where(p => p.Length == 2 && p[0] == character.ToString()).Select(p => p[1])];

    /// <summary>The kept text with the character's hidden quests now these, the other characters' left as they were.</summary>
    public static string Write(string? kept, Guid character, IEnumerable<string> hidden)
    {
        ArgumentNullException.ThrowIfNull(hidden);
        IEnumerable<string> others = (kept ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !l.StartsWith(character + " ", StringComparison.Ordinal));
        return string.Concat(others.Concat(hidden.Order(StringComparer.Ordinal).Select(q => $"{character} {q}")).Select(l => l + "\n"));
    }
}
