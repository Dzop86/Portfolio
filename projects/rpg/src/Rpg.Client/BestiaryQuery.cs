using System.Text;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>Which steps of the hunting log the bestiary's list keeps (sprint 175, D87, B3): to find, to study, to master, or any.</summary>
public enum BestiaryStepFilter
{
    Any,

    /// <summary>Never fought.</summary>
    ToFind,

    /// <summary>Met, not beaten yet.</summary>
    ToStudy,

    /// <summary>Beaten, not mastered yet.</summary>
    ToMaster,
}

/// <summary>
/// What the bestiary's list asks (sprint 175, D87, <c>docs/bestiaire-encyclopedie.md</c> §4.2): words found in a monster's
/// name, its family's or the name of an item it leaves; its zone; its levels, from and to (0: no bound); its role; its step.
/// </summary>
public sealed record BestiaryFilter(string Search = "", string? Zone = null, int MinLevel = 0, int MaxLevel = 0, MonsterRole? Role = null,
    BestiaryStepFilter Step = BestiaryStepFilter.Any)
{
    /// <summary>Nothing asked: the bestiary shows its families.</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Search) && this with { Search = "" } == new BestiaryFilter();
}

/// <summary>A monster the list keeps, its family, and the items of its loot whose names the search found (none without words).</summary>
public sealed record BestiaryHit(BestiaryFamily Family, BestiaryMonster Monster, IReadOnlyList<string> Items);

/// <summary>
/// The bestiary's search and filters (sprint 175, D87, B3), without any engine. The words look in what the player may read:
/// a monster's name once met, its family's always, and the items it leaves, which every item's card already tells (the
/// rule of §2: nothing a step hides is found), an item left for a quest only once the quest is under way or done. The
/// monsters stay in the order of the world.
/// </summary>
public static class BestiaryQuery
{
    public static IReadOnlyList<BestiaryHit> Find(GameData data, IReadOnlyList<BestiaryFamily> page, BestiaryFilter filter, Texts texts, IReadOnlySet<string>? quests = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(texts);
        string[] words = Fold(filter.Search).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hits = new List<BestiaryHit>();
        foreach (BestiaryFamily family in page)
        {
            if (filter.Zone is string zone && data.Families[family.Id].Zone != zone)
                continue;
            foreach (BestiaryMonster m in family.Monsters)
            {
                if ((filter.MinLevel > 0 && m.Level < filter.MinLevel) || (filter.MaxLevel > 0 && m.Level > filter.MaxLevel)
                    || (filter.Role is MonsterRole role && m.Role != role) || !Keeps(filter.Step, m.Known))
                    continue;
                if (words.Length == 0)
                {
                    hits.Add(new BestiaryHit(family, m, []));
                    continue;
                }
                // Each word somewhere: in its name, its family's, or one of its items' (those found are kept).
                string[] items = [.. (data.Monsters[m.Id].Loot ?? []).Where(l => data.Items.ContainsKey(l.Item) && (l.Quest is null || quests?.Contains(l.Quest) == true))
                    .Select(l => l.Item).Distinct()];
                string own = Fold(m.Known == Knowledge.Unknown ? family.Name : m.Name + " " + family.Name);
                string[] found = [.. items.Where(i => words.Any(w => Fold(data.Items[i].Name.In(texts.Lang)).Contains(w, StringComparison.Ordinal)))];
                if (words.All(w => own.Contains(w, StringComparison.Ordinal) || found.Any(i => Fold(data.Items[i].Name.In(texts.Lang)).Contains(w, StringComparison.Ordinal))))
                    hits.Add(new BestiaryHit(family, m, found));
            }
        }
        return hits;
    }

    /// <summary>The zones of the bestiary, in the order of the world: their ids and names, for the zone's list.</summary>
    public static IReadOnlyList<(string Id, string Name)> Zones(GameData data, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(texts);
        return [.. data.Families.Values.GroupBy(f => f.Zone).OrderBy(z => z.Min(f => f.Monsters.Min(m => m.Level))).ThenBy(z => z.Key, StringComparer.Ordinal)
            .Select(z => (z.Key, texts.ZoneName(z.Key, data)))];
    }

    private static bool Keeps(BestiaryStepFilter step, Knowledge known) => step switch
    {
        BestiaryStepFilter.ToFind => known == Knowledge.Unknown,
        BestiaryStepFilter.ToStudy => known == Knowledge.Met,
        BestiaryStepFilter.ToMaster => known is Knowledge.Studied or Knowledge.Known,
        _ => true,
    };

    // The letters of both languages with their accents, and what they fold to (no Unicode tables: the game may run without them).
    private const string Accented = "àâäáãåçéèêëíìîïñóòôöõúùûüýÿ", Plain = "aaaaaaceeeeiiiinooooouuuuyy";

    /// <summary>Words as the search compares them: lower case, without accents, apostrophes and dashes as spaces.</summary>
    public static string Fold(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var folded = new StringBuilder(text.Length);
        foreach (char upper in text)
        {
            char c = char.ToLowerInvariant(upper);
            int accented = Accented.IndexOf(c, StringComparison.Ordinal);
            folded.Append(c switch
            {
                '\'' or '’' or '-' => " ",
                'œ' => "oe",
                'æ' => "ae",
                _ => accented >= 0 ? Plain[accented].ToString() : c.ToString(),
            });
        }
        return folded.ToString();
    }
}
