namespace Rpg.Core;

/// <summary>How much a character knows of a monster (sprint 154, D83): met, then its numbers, its spells, its loot.</summary>
public enum Knowledge
{
    /// <summary>Never fought: a silhouette.</summary>
    Unknown,

    /// <summary>Fought once: its name, role, level and zone.</summary>
    Met,

    /// <summary>One victory: its hit points, resistances and weakness.</summary>
    Numbers,

    /// <summary>Ten victories: its spells and its family's lesson (its passives).</summary>
    Spells,

    /// <summary>Twenty-five victories: the loot it drops, as common, rare or very rare.</summary>
    Loot,
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
    public const int ForNumbers = 1, ForSpells = 10, ForLoot = 25;

    public static Knowledge Of(IReadOnlyDictionary<string, int> victories, string monster)
    {
        ArgumentNullException.ThrowIfNull(victories);
        return !victories.TryGetValue(monster, out int wins) ? Knowledge.Unknown
            : wins >= ForLoot ? Knowledge.Loot
            : wins >= ForSpells ? Knowledge.Spells
            : wins >= ForNumbers ? Knowledge.Numbers
            : Knowledge.Met;
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

    /// <summary>Every monster known in full (Plouf's "--bestiary full"): each of the game's at the loot's count.</summary>
    public static Dictionary<string, int> Full(GameData data) =>
        (data ?? throw new ArgumentNullException(nameof(data))).Families.Values.SelectMany(f => f.Monsters).ToDictionary(m => m.Id, _ => ForLoot, StringComparer.Ordinal);

    public static string Write(IReadOnlyDictionary<string, int> victories) =>
        string.Join(',', (victories ?? throw new ArgumentNullException(nameof(victories))).OrderBy(v => v.Key, StringComparer.Ordinal)
            .Select(v => $"{v.Key}:{v.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

    public static Dictionary<string, int> Read(string? text) =>
        (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Split(':'))
            .Where(v => v.Length == 2 && int.TryParse(v[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out _))
            .ToDictionary(v => v[0], v => int.Parse(v[1], System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);
}
