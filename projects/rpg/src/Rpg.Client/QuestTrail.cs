using Rpg.Core;

namespace Rpg.Client;

/// <summary>Where a quest followed leads (sprint 106): a map and a cell on it.</summary>
public sealed record TrailGoal(string Map, Cell At);

/// <summary>
/// Following a quest (sprint 106, D75: "pouvoir suivre une quête", shown on the left with an arrow and its place
/// on the map): the place of its next objective, or of its giver once all are reached (<see cref="Goal"/>), and,
/// from the map the player is on, the cell to head for (<see cref="Toward"/>): the goal itself on its map, else the
/// way out to the next map on the shortest walk there.
/// </summary>
public static class QuestTrail
{
    /// <summary>
    /// The place of the quest's first objective not reached: the inhabitant to see, the thing to examine, a group
    /// of the family's monsters, a spot where the resource grows or monsters that leave it, the dungeon's door, the
    /// one who starts the fight; its giver once all are reached; none for an item to wear (the inventory).
    /// </summary>
    public static TrailGoal? Goal(QuestBook book, QuestProgress progress)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(progress);
        GameData data = book.Data;
        Quest q = data.Quests[progress.Quest];
        QuestObjective? next = book.Next(progress);
        if (next is null)
            return data.FindNpc(q.Taker!) is (Town t, Npc n) ? new TrailGoal(t.Id, n.At) : null;
        return next.Goal switch
        {
            QuestGoal.Talk => data.FindNpc(next.Target) is (Town t, Npc n) ? new TrailGoal(t.Id, n.At) : null,
            QuestGoal.Interact => data.FindProp(next.Target) is (Town t, Prop p) ? new TrailGoal(t.Id, p.At) : null,
            QuestGoal.Defeat => GroupOf(data, next.Target),
            QuestGoal.Bring => Source(data, next.Target),
            QuestGoal.Dungeon => data.Dungeons.TryGetValue(next.Target, out Dungeon? d) ? new TrailGoal(d.Map, d.Door) : null,
            QuestGoal.Win when data.Towns.Values.SelectMany(t => t.Props ?? []).FirstOrDefault(p => p.Fight == next.Target) is Prop hatch
                => new TrailGoal(data.FindProp(hatch.Id)!.Value.Town.Id, hatch.At),
            QuestGoal.Win => data.Towns.Values.OrderBy(t => t.Id, StringComparer.Ordinal).SelectMany(t => t.Npcs.Select(n => (Town: t, Npc: n)))
                .Where(x => data.Dialogues[x.Npc.Dialogue].Lines.Values.Any(l => l.Answers.Any(a => a.Fight == next.Target)))
                .Select(x => new TrailGoal(x.Town.Id, x.Npc.At)).FirstOrDefault(),
            _ => null,
        };
    }

    /// <summary>
    /// Where an item comes from (sprints 106 and 107): a group of the monsters that leave it, the thing whose fight
    /// leaves it (the loft's hatch), a spot where it grows, the workshop that makes it, Rose's stall if she sells it.
    /// </summary>
    public static TrailGoal? Source(GameData data, string item)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Families.Values.FirstOrDefault(f => f.Monsters.Any(m => (m.Loot ?? []).Any(l => l.Item == item))) is MonsterFamily f)
            return GroupOf(data, f.Id);
        if (data.Towns.Values.OrderBy(t => t.Id, StringComparer.Ordinal).SelectMany(t => (t.Props ?? []).Select(p => (Town: t, Prop: p)))
            .FirstOrDefault(x => x.Prop.Fight is string fight && data.FindScenario(fight)!.Fighters.Any(s => (s.Loot ?? []).Any(l => l.Item == item))) is ({ } town, { } hatch))
            return new TrailGoal(town.Id, hatch.At);
        if (data.Nodes.Values.Where(n => n.Item == item).OrderBy(n => n.Map, StringComparer.Ordinal).ThenBy(n => n.Id, StringComparer.Ordinal).FirstOrDefault() is HarvestNode node)
            return new TrailGoal(node.Map, node.At);
        if (data.Jobs.Values.FirstOrDefault(j => j.RecipeFor(item) is not null) is Job job
            && data.Towns.Values.OrderBy(t => t.Id, StringComparer.Ordinal).SelectMany(t => (t.Workshops ?? []).Where(w => w.Job == job.Id).Select(w => new TrailGoal(t.Id, w.At))).FirstOrDefault() is TrailGoal bench)
            return bench;
        if (data.Items.TryGetValue(item, out Item? sold) && Shop.SellPrice(sold, data) is not null && data.FindNpc("clairval/rose") is (Town market, Npc rose))
            return new TrailGoal(market.Id, rose.At);
        return null;
    }

    // A group of the family, on the first map (by its name) where they roam, out of the dungeons.
    private static TrailGoal? GroupOf(GameData data, string family) =>
        data.Groups.Values.Where(g => g.Family == family && !data.Towns[g.Map].IsHall)
            .OrderBy(g => g.Map, StringComparer.Ordinal).ThenBy(g => g.Id, StringComparer.Ordinal)
            .Select(g => new TrailGoal(g.Map, g.Home)).FirstOrDefault();

    /// <summary>
    /// The cell to head for from the map <paramref name="here"/>: the goal's cell on its own map; else the middle of
    /// the way out to the next map on a shortest walk of maps to the goal's (fewest maps crossed, the first by name on
    /// a tie); none in a dungeon's hall, or when the goal's map cannot be walked to.
    /// </summary>
    public static Cell? Toward(GameData data, Town here, TrailGoal goal)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(here);
        ArgumentNullException.ThrowIfNull(goal);
        if (goal.Map == here.Id)
            return goal.At;
        if (here.IsHall || !data.Towns.TryGetValue(goal.Map, out Town? there) || there.IsHall)
            return null;
        IEnumerable<string> Next(Town t) => (t.Links ?? []).Select(l => l.To).Distinct()
            .Where(m => data.Towns.TryGetValue(m, out Town? to) && !to.IsHall).Order(StringComparer.Ordinal);
        // Backwards from the goal's map: how many maps each is from it.
        var far = new Dictionary<string, int>(StringComparer.Ordinal) { [goal.Map] = 0 };
        var todo = new Queue<string>([goal.Map]);
        var into = data.Towns.Values.Where(t => !t.IsHall).SelectMany(t => Next(t).Select(m => (From: t.Id, To: m)))
            .ToLookup(x => x.To, x => x.From, StringComparer.Ordinal);
        while (todo.Count > 0 && !far.ContainsKey(here.Id))
        {
            string m = todo.Dequeue();
            foreach (string from in into[m].Order(StringComparer.Ordinal))
            {
                if (far.TryAdd(from, far[m] + 1))
                    todo.Enqueue(from);
            }
        }
        if (!far.TryGetValue(here.Id, out int steps))
            return null;
        string next = Next(here).First(m => far.TryGetValue(m, out int f) && f == steps - 1);
        List<Cell> cells = [.. (here.Links ?? []).Where(l => l.To == next).Select(l => l.At).OrderBy(c => c.X).ThenBy(c => c.Y)];
        return cells[cells.Count / 2];
    }
}
