using Rpg.Core;

namespace Rpg.Client;

/// <summary>A quest as the journal shows it (sprint 88): its name, who gave it, its words, one line per objective with how far it has gone.</summary>
public sealed record JournalQuest(string Id, string Name, string Giver, string Text, IReadOnlyList<(string Line, bool Reached)> Objectives, bool Done, bool Ready);

/// <summary>The journal's quests of one zone (or of none: the village), under way and done.</summary>
public sealed record JournalZone(string Name, IReadOnlyList<JournalQuest> Active, IReadOnlyList<JournalQuest> Done);

/// <summary>
/// The character's quests without any engine (sprint 88): what each inhabitant offers or takes back,
/// whom a quest under way asks to see, and the journal (key Q), zone by zone. Built from what the
/// server says of the character (its level, quests done and under way, inventory).
/// </summary>
public sealed class QuestBook(GameData data, int level, IReadOnlyCollection<string> done, IReadOnlyList<QuestProgress> active, IReadOnlyList<ItemCount> owned)
{
    public GameData Data { get; } = data ?? throw new ArgumentNullException(nameof(data));
    public IReadOnlyList<QuestProgress> Active { get; } = active ?? throw new ArgumentNullException(nameof(active));
    public IReadOnlyCollection<string> DoneQuests { get; } = done ?? throw new ArgumentNullException(nameof(done));

    private readonly IReadOnlyList<ItemCount> _owned = owned ?? throw new ArgumentNullException(nameof(owned));

    /// <summary>The character's book, as the server keeps it; none for a hero without a character.</summary>
    public static QuestBook Of(CharacterSummary character, GameData data) =>
        new(data, (character ?? throw new ArgumentNullException(nameof(character))).Level, [.. character.Quests ?? []], character.QuestLog ?? [], character.Inventory ?? []);

    /// <summary>The most quests an inhabitant has under way with the character, offered ones included (Charles, sprint 102).</summary>
    public const int MostFromOne = 2;

    /// <summary>
    /// The quests this inhabitant ("town/npc") gives that the character can take now: the lowest levels first,
    /// so that it never has more than <see cref="MostFromOne"/> of theirs under way at once.
    /// </summary>
    public IReadOnlyList<Quest> Offers(string npc) =>
        [.. Data.Quests.Values.Where(q => q.Giver == npc && Quests.WhyNot(q, level, DoneQuests, Active) is null)
            .OrderBy(q => q.Level).Take(Math.Max(0, MostFromOne - Active.Count(p => Data.Quests.TryGetValue(p.Quest, out Quest? a) && a.Giver == npc)))];

    /// <summary>The quests under way this inhabitant gave, their objectives all reached: to hand back.</summary>
    public IReadOnlyList<Quest> HandIns(string npc) =>
        [.. Active.Select(p => (Quest: Data.Quests[p.Quest], Progress: p)).Where(x => x.Quest.Giver == npc && Quests.IsComplete(x.Quest, x.Progress, _owned)).Select(x => x.Quest)];

    /// <summary>The quests under way that ask to talk to this inhabitant, not yet done.</summary>
    public IReadOnlyList<Quest> Talks(string npc) =>
        [.. Active.Select(p => (Quest: Data.Quests[p.Quest], Progress: p))
            .Where(x => x.Quest.Objectives!.Select((o, i) => o.Goal == QuestGoal.Talk && o.Target == npc && x.Progress.Counts[i] < 1).Any(b => b))
            .Select(x => x.Quest)];

    /// <summary>
    /// The journal: a zone for each quest's zone (the village first, for quests of none), its quests under
    /// way then those done, each with its objectives as "Defeat Boar (1/3)".
    /// </summary>
    public IReadOnlyList<JournalZone> Journal(Texts texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        JournalQuest Entry(Quest q, QuestProgress? p)
        {
            string giver = Data.FindNpc(q.Giver!) is (_, Npc n) ? n.Name.In(texts.Lang) : q.Giver!;
            var lines = q.Objectives!.Select((o, i) =>
            {
                int reached = p is null ? o.Count : Quests.Reached(o, p.Counts[i], _owned);
                return (texts.Objective(o, reached), reached >= o.Count);
            }).ToList();
            return new JournalQuest(q.Id, q.Name.In(texts.Lang), giver, q.Text!.In(texts.Lang), lines, p is null, p is not null && Quests.IsComplete(q, p, _owned));
        }
        string ZoneName(string? zone) => zone is null ? texts["journal.village"]
            : Data.Towns.Values.FirstOrDefault(t => t.Zone == zone) is Town t ? t.Name.In(texts.Lang) : zone;
        var given = Data.Quests.Values.Where(q => q.IsGiven).ToList();
        return [.. given.Select(q => q.Zone).Distinct().OrderBy(z => z is not null).ThenBy(z => z, StringComparer.Ordinal)
            .Select(z => new JournalZone(ZoneName(z),
                [.. Active.Where(p => Data.Quests[p.Quest].Zone == z).Select(p => Entry(Data.Quests[p.Quest], p))],
                [.. given.Where(q => q.Zone == z && DoneQuests.Contains(q.Id)).Select(q => Entry(q, null))]))
            .Where(j => j.Active.Count + j.Done.Count > 0)];
    }
}
