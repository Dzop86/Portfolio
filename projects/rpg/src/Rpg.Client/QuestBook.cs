using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// A quest as the journal shows it (sprint 88): its name, who gave it, its words, one line per objective with how
/// far it has gone; under way, where to go next (sprint 105, D75: the map of the next objective, or of its giver);
/// since sprint 116 (D77), its kind, level, zone and rewards ("120 XP", "20 Éclats", "Bread ×2") for its card.
/// </summary>
public sealed record JournalQuest(string Id, string Name, string Giver, string Text, IReadOnlyList<(string Line, bool Reached)> Objectives, bool Done, bool Ready, string? Where = null,
    QuestKind Kind = QuestKind.Side, int Level = 1, string Zone = "", IReadOnlyList<string>? Rewards = null);

/// <summary>What shows over an inhabitant (sprint 105, D75): "!" a quest to take, "?" a quest to hand back, grey "?" one under way.</summary>
public enum QuestMark
{
    None,
    Waiting,
    Offer,
    Ready,
}

/// <summary>The journal's quests of one zone (or of none: the village), under way and done.</summary>
public sealed record JournalZone(string Name, IReadOnlyList<JournalQuest> Active, IReadOnlyList<JournalQuest> Done);

/// <summary>
/// The character's quests without any engine (sprint 88): what each inhabitant offers or takes back,
/// whom a quest under way asks to see, and the journal (key Q), zone by zone. Built from what the
/// server says of the character (its level, quests done and under way, inventory).
/// </summary>
public sealed class QuestBook(GameData data, int level, IReadOnlyCollection<string> done, IReadOnlyList<QuestProgress> active, IReadOnlyList<ItemCount> owned,
    IEnumerable<string>? worn = null)
{
    public GameData Data { get; } = data ?? throw new ArgumentNullException(nameof(data));
    public IReadOnlyList<QuestProgress> Active { get; } = active ?? throw new ArgumentNullException(nameof(active));
    public IReadOnlyCollection<string> DoneQuests { get; } = done ?? throw new ArgumentNullException(nameof(done));

    private readonly IReadOnlyList<ItemCount> _owned = owned ?? throw new ArgumentNullException(nameof(owned));
    private readonly string[] _worn = [.. worn ?? []];

    /// <summary>The character's book, as the server keeps it; none for a hero without a character.</summary>
    public static QuestBook Of(CharacterSummary character, GameData data) =>
        new(data, (character ?? throw new ArgumentNullException(nameof(character))).Level, [.. character.Quests ?? []], Quests.Fit(data, character.QuestLog ?? []), character.Inventory ?? [],
            character.Worn?.Values);

    /// <summary>The most quests an inhabitant has under way with the character, offered ones included (Charles, sprint 102).</summary>
    public const int MostFromOne = 2;

    /// <summary>
    /// The quests this inhabitant ("town/npc") gives that the character can take now: the lowest levels first,
    /// so that it never has more than <see cref="MostFromOne"/> of theirs under way at once.
    /// </summary>
    public IReadOnlyList<Quest> Offers(string npc) =>
        [.. Data.Quests.Values.Where(q => q.Giver == npc && Quests.WhyNot(q, level, DoneQuests, Active) is null)
            .OrderBy(q => q.Level).Take(Math.Max(0, MostFromOne - Active.Count(p => Data.Quests.TryGetValue(p.Quest, out Quest? a) && a.Giver == npc)))];

    /// <summary>The quests under way this inhabitant takes back (its giver, or the one it names, sprint 107), their objectives all reached.</summary>
    public IReadOnlyList<Quest> HandIns(string npc) =>
        // The talk to them counted already (the server counts it as the talk opens): Garance takes back Aubin's welcome at once.
        [.. Active.Select(p => (Quest: Data.Quests[p.Quest], Progress: p)).Where(x => x.Quest.Taker == npc && Quests.IsComplete(x.Quest, Quests.AfterTalk(x.Quest, x.Progress, npc), _owned, _worn)).Select(x => x.Quest)];

    /// <summary>The quests under way that ask to talk to this inhabitant, not yet done.</summary>
    public IReadOnlyList<Quest> Talks(string npc) =>
        [.. Active.Select(p => (Quest: Data.Quests[p.Quest], Progress: p))
            .Where(x => x.Quest.Objectives!.Select((o, i) => o.Goal == QuestGoal.Talk && o.Target == npc && x.Progress.Counts[i] < 1).Any(b => b))
            .Select(x => x.Quest)];

    /// <summary>What this inhabitant says for the quests under way that ask to talk to them (sprint 107), in order.</summary>
    public IReadOnlyList<LocalizedText> TalkLines(string npc) =>
        [.. Talks(npc).SelectMany(q => q.Objectives!.Where(o => o.Goal == QuestGoal.Talk && o.Target == npc && o.Line is not null).Select(o => o.Line!))];

    /// <summary>The quest under way that a thing's fight is won for (sprint 107: the loft's hatch), if any.</summary>
    public Quest? FightFor(Prop prop)
    {
        ArgumentNullException.ThrowIfNull(prop);
        return prop.Fight is not string fight ? null
            : Active.Select(p => (Quest: Data.Quests[p.Quest], Progress: p))
                .FirstOrDefault(x => x.Quest.Objectives!.Select((o, i) => o.Goal == QuestGoal.Win && o.Target == fight && x.Progress.Counts[i] < 1).Any(b => b)).Quest;
    }

    /// <summary>The quests under way that ask to examine this thing (sprint 105), not yet done.</summary>
    public IReadOnlyList<Quest> Interacts(string prop) =>
        [.. Active.Select(p => (Quest: Data.Quests[p.Quest], Progress: p))
            .Where(x => x.Quest.Objectives!.Select((o, i) => o.Goal == QuestGoal.Interact && o.Target == prop && x.Progress.Counts[i] < 1).Any(b => b))
            .Select(x => x.Quest)];

    /// <summary>What shows over this inhabitant ("town/npc", sprint 105): a quest to hand back first, then one to take, then one under way.</summary>
    public QuestMark Mark(string npc) =>
        HandIns(npc).Count > 0 ? QuestMark.Ready
        : Offers(npc).Count > 0 ? QuestMark.Offer
        : Active.Any(p => Data.Quests[p.Quest].Giver == npc) ? QuestMark.Waiting
        : QuestMark.None;

    /// <summary>
    /// Where a quest under way leads next (sprint 105, D75): the map of its first objective not reached (the
    /// inhabitant to see, the thing to examine, the monsters of the family, the resource, the dungeon's door, the
    /// one who starts the fight), the inventory for an item to wear, its giver's map once all are reached.
    /// </summary>
    public string? Where(QuestProgress progress, Texts texts)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(texts);
        Quest q = Data.Quests[progress.Quest];
        if (Next(progress) is not QuestObjective goal)
            return Data.FindNpc(q.Taker!) is (Town giverMap, _) ? texts.MapTitle(giverMap) : null;
        if (goal.Goal == QuestGoal.Wear)
            return texts["journal.inventory"];
        string? map = goal.Goal switch
        {
            QuestGoal.Talk => Data.FindNpc(goal.Target)?.Town.Id,
            QuestGoal.Interact => Data.FindProp(goal.Target)?.Town.Id,
            QuestGoal.Defeat => FamilyMap(goal.Target),
            QuestGoal.Bring => QuestTrail.Source(Data, goal.Target)?.Map,
            QuestGoal.Dungeon => Data.Dungeons.TryGetValue(goal.Target, out Dungeon? d) ? d.Map : null,
            QuestGoal.Win when Data.Towns.Values.SelectMany(t => t.Props ?? []).FirstOrDefault(p => p.Fight == goal.Target) is Prop hatch => Data.FindProp(hatch.Id)!.Value.Town.Id,
            QuestGoal.Win => Data.Towns.Values.OrderBy(t => t.Id, StringComparer.Ordinal).SelectMany(t => t.Npcs.Select(n => (t, n)))
                .FirstOrDefault(x => Data.Dialogues[x.n.Dialogue].Lines.Values.Any(l => l.Answers.Any(a => a.Fight == goal.Target))).t?.Id,
            _ => null,
        };
        return map is not null && Data.Towns.TryGetValue(map, out Town? town) ? texts.MapTitle(town) : null;
    }

    /// <summary>The quest's first objective not reached yet, or null when all are (sprint 106).</summary>
    public QuestObjective? Next(QuestProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        IReadOnlyList<QuestObjective> goals = Data.Quests[progress.Quest].Objectives!;
        for (int i = 0; i < goals.Count; i++)
        {
            if (Quests.Reached(goals[i], progress.Counts[i], _owned, _worn) < goals[i].Count)
                return goals[i];
        }
        return null;
    }

    // The first map (by its name) where the family's monsters roam, out of the dungeons.
    // A family's first map, or since sprint 112 the first map of a group with that kind of monster (the old loners).
    private string? FamilyMap(string target) =>
        Data.Groups.Values.Where(g => (g.Family == target || g.Monsters.Contains(target)) && !Data.Towns[g.Map].IsHall).Select(g => g.Map).Order(StringComparer.Ordinal).FirstOrDefault();

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
                int reached = p is null ? o.Count : Quests.Reached(o, p.Counts[i], _owned, _worn);
                return (texts.Objective(o, reached), reached >= o.Count);
            }).ToList();
            List<string> rewards = [texts["journal.xp", q.Xp]];
            if (q.Shards > 0)
                rewards.Add(texts["journal.shards", q.Shards]);
            rewards.AddRange((q.Items ?? []).Select(i => texts["journal.item", Data.Items[i.Item].Name.In(texts.Lang), i.Count]));
            return new JournalQuest(q.Id, q.Name.In(texts.Lang), giver, q.Text!.In(texts.Lang), lines, p is null, p is not null && Quests.IsComplete(q, p, _owned, _worn),
                p is null ? null : Where(p, texts), q.Kind, q.Level, ZoneName(q.Zone), rewards);
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
