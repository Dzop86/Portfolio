namespace Rpg.Core;

/// <summary>A quest under way (sprint 88): how far each of its objectives has gone, in their order (items brought are counted when handed back).</summary>
public sealed record QuestProgress(string Quest, IReadOnlyList<int> Counts)
{
    public bool Equals(QuestProgress? other) => other is not null && Quest == other.Quest && Counts.SequenceEqual(other.Counts);

    public override int GetHashCode() => Quest.GetHashCode(StringComparison.Ordinal);
}

/// <summary>
/// The rules of the quests given by inhabitants (sprint 88, D65): which one can be taken, how a fight,
/// a talk or a dungeon moves it on, when it can be handed back. The server applies them and keeps the
/// progress; the client shows them. Items to bring are not counted on the way: they must be owned when
/// the quest is handed back, and are taken then.
/// </summary>
public static class Quests
{
    /// <summary>At most this many quests under way at once.</summary>
    public const int MaxActive = 20;

    /// <summary>Why the quest cannot be taken now, or null: not done, not under way, from its level, after the quests it follows.</summary>
    public static string? WhyNot(Quest quest, int level, IReadOnlyCollection<string> done, IReadOnlyList<QuestProgress> active)
    {
        ArgumentNullException.ThrowIfNull(quest);
        ArgumentNullException.ThrowIfNull(done);
        ArgumentNullException.ThrowIfNull(active);
        return !quest.IsGiven ? "Not a quest an inhabitant gives."
            : done.Contains(quest.Id) ? "Already done."
            : active.Any(p => p.Quest == quest.Id) ? "Already under way."
            : active.Count >= MaxActive ? $"At most {MaxActive} quests under way."
            : level < quest.Level ? $"From level {quest.Level}."
            : (quest.After ?? []).FirstOrDefault(a => !done.Contains(a)) is string before ? $"After the quest '{before}'."
            : null;
    }

    /// <summary>The quest just taken: nothing done yet.</summary>
    public static QuestProgress Start(Quest quest) => new((quest ?? throw new ArgumentNullException(nameof(quest))).Id, [.. quest.Objectives!.Select(_ => 0)]);

    /// <summary>The monsters of each family a won fight defeated: a group of the world's, or a dungeon's room; none otherwise.</summary>
    public static IReadOnlyDictionary<string, int> Defeated(GameData data, Fight fight)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(fight);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        if (fight.WinningTeam != 0)
            return counts;
        string id = fight.Scenario.Id;
        int beaten = fight.Fighters.Count(f => f.Team != 0 && !f.IsSummon);
        if (id.StartsWith(MonsterGroup.Prefix, StringComparison.Ordinal) && data.Groups.TryGetValue(id[MonsterGroup.Prefix.Length..], out MonsterGroup? group))
            counts[group.Family] = beaten;
        else if (Dungeons.Parse(id) is (string dungeon, _) && data.Dungeons.TryGetValue(dungeon, out Dungeon? d))
            counts[d.Family] = beaten;
        return counts;
    }

    /// <summary>A quest after a won fight: its "defeat" objectives count the family's monsters, its "dungeon" one the dungeon finished.</summary>
    public static QuestProgress AfterFight(Quest quest, QuestProgress progress, IReadOnlyDictionary<string, int> defeated, string? dungeonDone)
    {
        ArgumentNullException.ThrowIfNull(quest);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(defeated);
        return progress with
        {
            Counts = [.. quest.Objectives!.Select((o, i) => o.Goal switch
            {
                QuestGoal.Defeat => Math.Min(o.Count, progress.Counts[i] + defeated.GetValueOrDefault(o.Target)),
                QuestGoal.Dungeon when o.Target == dungeonDone => 1,
                _ => progress.Counts[i],
            })],
        };
    }

    /// <summary>A quest after talking to an inhabitant ("town/npc"): its "talk" objective toward them done.</summary>
    public static QuestProgress AfterTalk(Quest quest, QuestProgress progress, string npc)
    {
        ArgumentNullException.ThrowIfNull(quest);
        ArgumentNullException.ThrowIfNull(progress);
        return progress with { Counts = [.. quest.Objectives!.Select((o, i) => o.Goal == QuestGoal.Talk && o.Target == npc ? 1 : progress.Counts[i])] };
    }

    /// <summary>How far an objective has gone, items to bring counted in what is owned now.</summary>
    public static int Reached(QuestObjective objective, int count, IReadOnlyList<ItemCount> owned)
    {
        ArgumentNullException.ThrowIfNull(objective);
        ArgumentNullException.ThrowIfNull(owned);
        return objective.Goal == QuestGoal.Bring
            ? Math.Min(objective.Count, owned.Where(i => i.Item == objective.Target).Sum(i => i.Count))
            : Math.Min(objective.Count, count);
    }

    /// <summary>Every objective reached: the quest can be handed back to its giver.</summary>
    public static bool IsComplete(Quest quest, QuestProgress progress, IReadOnlyList<ItemCount> owned)
    {
        ArgumentNullException.ThrowIfNull(quest);
        ArgumentNullException.ThrowIfNull(progress);
        return quest.Objectives!.Select((o, i) => Reached(o, progress.Counts[i], owned) >= o.Count).All(x => x);
    }

    /// <summary>The items handed back with the quest (those to bring).</summary>
    public static IReadOnlyList<ItemCount> Taken(Quest quest) =>
        [.. (quest ?? throw new ArgumentNullException(nameof(quest))).Objectives!.Where(o => o.Goal == QuestGoal.Bring).Select(o => new ItemCount(o.Target, o.Count))];

    /// <summary>"id:1.0.3,id2:0" as the server keeps the quests under way, and back.</summary>
    public static string Write(IEnumerable<QuestProgress> active) =>
        string.Join(',', (active ?? throw new ArgumentNullException(nameof(active))).Select(p => p.Quest + ":" + string.Join('.', p.Counts.Select(c => c.ToString(System.Globalization.CultureInfo.InvariantCulture)))));

    public static List<QuestProgress> Read(string? text) =>
        [.. (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split(':'))
            .Select(p => new QuestProgress(p[0], [.. p[1].Split('.').Select(c => int.Parse(c, System.Globalization.CultureInfo.InvariantCulture))]))];
}
