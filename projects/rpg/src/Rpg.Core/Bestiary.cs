namespace Rpg.Core;

/// <summary>
/// How much a character knows of a monster (sprint 154, D83): since sprint 168 (D87, <c>docs/bestiaire-encyclopedie.md</c> §3)
/// the five steps of a hunting log, opened by the same count of victories, so that what was won before still counts.
/// </summary>
public enum Knowledge
{
    /// <summary>Never fought: a silhouette, its role, level and zone, to go and find it.</summary>
    Unknown,

    /// <summary>Fought, even lost: its name, family and numbers.</summary>
    Met,

    /// <summary>A first victory: its resistances, weakness and loot.</summary>
    Studied,

    /// <summary>Five victories (two for a chief): its spells and its family's lesson.</summary>
    Known,

    /// <summary>Twenty-five victories (five for a chief): its story, a golden frame, its family's hunter.</summary>
    Mastered,
}

/// <summary>How often an item falls (sprint 154): the game designer's words, never the exact rate (question 80 a).</summary>
public enum Rarity
{
    Common,
    Rare,
    VeryRare,
}

/// <summary>
/// The bestiary's rules (sprint 154, D83, the game designer's §12.4): the server counts, for each character, the victories
/// over each monster (every monster of a won fight, once each time it stood in it), a monster fought and not beaten counting
/// as met; the game shows what the count allows. Kept as "monster:victories" separated by commas.
/// </summary>
public static class Bestiary
{
    /// <summary>The victories each step asks (studied, known, mastered): fewer for a chief, met once a fight.</summary>
    public static (int Studied, int Known, int Mastered) Steps(MonsterSpec monster) =>
        (monster ?? throw new ArgumentNullException(nameof(monster))).Role == MonsterRole.Boss ? (1, 2, 5) : (1, 5, 25);

    public static Knowledge Of(IReadOnlyDictionary<string, int> victories, MonsterSpec monster)
    {
        ArgumentNullException.ThrowIfNull(victories);
        (int studied, int known, int mastered) = Steps(monster);
        return !victories.TryGetValue(monster.Id, out int wins) ? Knowledge.Unknown
            : wins >= mastered ? Knowledge.Mastered
            : wins >= known ? Knowledge.Known
            : wins >= studied ? Knowledge.Studied
            : Knowledge.Met;
    }

    /// <summary>The next step and the victories it still asks; none once mastered (met: a fight, even lost).</summary>
    public static (Knowledge Step, int Left)? Next(IReadOnlyDictionary<string, int> victories, MonsterSpec monster)
    {
        ArgumentNullException.ThrowIfNull(victories);
        (int studied, int known, int mastered) = Steps(monster);
        int wins = victories.GetValueOrDefault(monster.Id);
        return Of(victories, monster) switch
        {
            Knowledge.Unknown => (Knowledge.Met, 0),
            Knowledge.Met => (Knowledge.Studied, studied - wins),
            Knowledge.Studied => (Knowledge.Known, known - wins),
            Knowledge.Known => (Knowledge.Mastered, mastered - wins),
            _ => null,
        };
    }

    /// <summary>
    /// The account's hunting log (sprint 168, D87, B7): each monster at the best count of the account's characters, read
    /// as it is, nothing written.
    /// </summary>
    public static Dictionary<string, int> Best(IEnumerable<IReadOnlyDictionary<string, int>?> logs)
    {
        ArgumentNullException.ThrowIfNull(logs);
        var best = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (IReadOnlyDictionary<string, int>? log in logs)
            foreach ((string monster, int wins) in log ?? new Dictionary<string, int>())
                best[monster] = Math.Max(best.GetValueOrDefault(monster, int.MinValue), wins);
        return best;
    }

    /// <summary>The monsters a fight set against the hero: a world group's, a dungeon room's, in their order; none for anything else.</summary>
    public static IReadOnlyList<string> Faced(GameData data, string scenario)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(scenario);
        if (scenario.StartsWith(MonsterGroup.Prefix, StringComparison.Ordinal) && data.Groups.TryGetValue(scenario[MonsterGroup.Prefix.Length..], out MonsterGroup? group))
            return group.Monsters;
        if (Dungeons.Parse(scenario) is (string dungeon, int room) && data.Dungeons.TryGetValue(dungeon, out Dungeon? d) && data.Families.TryGetValue(d.Family, out MonsterFamily? f))
            return [.. Dungeons.RolesIn(room).Select(r => f[r].Id)];
        return [];
    }

    /// <summary>The count after a fight: each monster faced met, and a victory more for each time it stood in a fight won.</summary>
    public static Dictionary<string, int> AfterFight(IReadOnlyDictionary<string, int> victories, GameData data, Fight fight)
    {
        ArgumentNullException.ThrowIfNull(victories);
        ArgumentNullException.ThrowIfNull(fight);
        var after = new Dictionary<string, int>(victories, StringComparer.Ordinal);
        foreach (string monster in Faced(data, fight.Scenario.Id))
            after[monster] = after.GetValueOrDefault(monster) + (fight.WinningTeam == 0 ? 1 : 0);
        return after;
    }

    /// <summary>A monster's weakness: the element it resists least, below zero; none when it resists everything.</summary>
    public static Element? Weakness(MonsterSpec monster) =>
        (monster ?? throw new ArgumentNullException(nameof(monster))).Resistances is { Count: > 0 } r && r.MinBy(x => x.Value) is { Value: < 0 } weakest ? weakest.Key : null;

    public static Rarity RarityOf(int percent) => percent >= 25 ? Rarity.Common : percent >= 8 ? Rarity.Rare : Rarity.VeryRare;

    /// <summary>Every monster mastered (Plouf's "--bestiary full"): each of the game's at twenty-five victories.</summary>
    public static Dictionary<string, int> Full(GameData data) =>
        (data ?? throw new ArgumentNullException(nameof(data))).Families.Values.SelectMany(f => f.Monsters).ToDictionary(m => m.Id, _ => 25, StringComparer.Ordinal);

    public static string Write(IReadOnlyDictionary<string, int> victories) =>
        string.Join(',', (victories ?? throw new ArgumentNullException(nameof(victories))).OrderBy(v => v.Key, StringComparer.Ordinal)
            .Select(v => $"{v.Key}:{v.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

    public static Dictionary<string, int> Read(string? text) =>
        (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Split(':'))
            .Where(v => v.Length == 2 && int.TryParse(v[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out _))
            .ToDictionary(v => v[0], v => int.Parse(v[1], System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);
}
