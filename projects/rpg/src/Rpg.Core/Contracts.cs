using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>
/// What a contract of the notice board asks (sprint 153, D83, the game designer's §12.4): monsters of a family, resources
/// gathered, an item made, a dungeon's second room; a horde of a family or a dungeon's chief, made for playing together.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContractKind>))]
public enum ContractKind
{
    Hunt,
    Gather,
    Craft,
    Dungeon,
    Horde,
    Boss,
}

/// <summary>
/// A contract (sprint 153): its kind, its target (a family, an item, a dungeon), how many, the level it was drawn for and
/// how far it has gone. Its reward: a quarter of a level's experience and 5 × level Éclats, twice that for a group's.
/// </summary>
public sealed record Contract(ContractKind Kind, string Target, int Count, int Level, int Done = 0)
{
    /// <summary>A horde or a chief: made for playing together.</summary>
    [JsonIgnore]
    public bool IsGroup => Kind is ContractKind.Horde or ContractKind.Boss;

    [JsonIgnore]
    public long Xp => 25L * Level * (IsGroup ? 2 : 1);

    [JsonIgnore]
    public long Shards => 5L * Level * (IsGroup ? 2 : 1);

    /// <summary>The items it takes when handed back: the resources gathered, the item made; none for a fight.</summary>
    [JsonIgnore]
    public IReadOnlyList<ItemCount> Taken => Kind is ContractKind.Gather or ContractKind.Craft ? [new ItemCount(Target, Count)] : [];
}

/// <summary>
/// A character's notice board (sprint 153): the day it was last seen, the changes made that day (a group's not counted), the
/// contracts offered, and every change ever made (the draw of the next one).
/// </summary>
public sealed record ContractBoard(int Day, int Changes, IReadOnlyList<Contract> Offered, int Swaps = 0)
{
    public static readonly ContractBoard Empty = new(int.MinValue, 0, []);
}

/// <summary>
/// The rules of the contracts (sprint 153, D83): three a day and a character, renewed at dawn (4 h on the server's clock),
/// kept up to nine so that days away lose nothing; drawn from the families of its level (ten below at most) in the zones it
/// can reach, from its trades' resources and recipes; one in three at most made for a group. The same character on the same
/// day always gets the same board. The server keeps them; the game shows them.
/// </summary>
public static class Contracts
{
    public const int PerDay = 3;
    public const int Most = 9;

    /// <summary>The hour of the server's day the board is renewed.</summary>
    public static readonly TimeSpan Dawn = TimeSpan.FromHours(4);

    // The dungeons' keys are no contract's: they open a dungeon, not a notice.
    private const string Locksmith = "locksmith";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The day of the board at a moment: a new one starts at dawn.</summary>
    public static int Day(DateTimeOffset now) => (int)Math.Floor((now.ToUniversalTime() - Dawn).ToUnixTimeSeconds() / 86_400.0);

    /// <summary>The price of a change: the first of the day free, a group's always, then 5 × level Éclats.</summary>
    public static long ChangePrice(ContractBoard board, Contract contract, int level)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(contract);
        return board.Changes == 0 || contract.IsGroup ? 0 : 5L * level;
    }

    /// <summary>
    /// The board on <paramref name="day"/>: three more for each day since it was last seen, nine at most; the day's changes
    /// counted from nothing again. A first board starts with three.
    /// </summary>
    public static ContractBoard Renew(ContractBoard board, int day, Guid character, int level, Reach reach, GameData data)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(data);
        if (board.Day >= day)
            return board;
        int days = board.Day == int.MinValue ? 1 : Math.Min(Most, day - board.Day);
        var offered = board.Offered.ToList();
        for (int d = day - days + 1; d <= day && offered.Count < Most; d++)
            offered.AddRange(Draw(data, character, d, 0, level, reach).Take(Most - offered.Count));
        return board with { Day = day, Changes = 0, Offered = offered };
    }

    /// <summary>
    /// The board with one contract changed for another of the same sort (made for a group or not), drawn anew; its price
    /// (<see cref="ChangePrice"/>) is the server's to take. A group's change is not counted among the day's.
    /// </summary>
    public static ContractBoard Change(ContractBoard board, int index, Guid character, int level, Reach reach, GameData data)
    {
        ArgumentNullException.ThrowIfNull(board);
        Contract old = board.Offered[index];
        IReadOnlyList<Contract> drawn = Draw(data, character, board.Day, board.Swaps + 1, level, reach);
        Contract fresh = drawn.FirstOrDefault(c => c.IsGroup == old.IsGroup && c != old) ?? drawn.FirstOrDefault(c => c != old) ?? old;
        var offered = board.Offered.ToList();
        offered[index] = fresh;
        return board with { Offered = offered, Changes = board.Changes + (old.IsGroup ? 0 : 1), Swaps = board.Swaps + 1 };
    }

    /// <summary>What a character can be sent to: the zones it reaches and its trades' levels.</summary>
    public sealed record Reach(IReadOnlyCollection<string> Zones, IReadOnlyDictionary<string, int> Jobs);

    /// <summary>The zones a character reaches: those whose maps need no quest, or one it has done.</summary>
    public static IReadOnlyCollection<string> Zones(GameData data, IReadOnlyCollection<string> questsDone)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(questsDone);
        return [.. data.Towns.Values.Where(t => t.Zone is not null && (data.QuestToReach(t.Id) is not string gate || questsDone.Contains(gate)))
            .Select(t => t.Zone!).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Three contracts drawn for a character on a day (<paramref name="salt"/> for a change): two to do alone, the third
    /// of any kind; each of a kind that has something to draw, else of the first that has.
    /// </summary>
    public static IReadOnlyList<Contract> Draw(GameData data, Guid character, int day, int salt, int level, Reach reach)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(reach);
        var rng = new Random(Seed(character, day, salt));
        ContractKind[] alone = [ContractKind.Hunt, ContractKind.Gather, ContractKind.Craft, ContractKind.Dungeon];
        ContractKind[] all = [.. alone, ContractKind.Horde, ContractKind.Boss];
        var drawn = new List<Contract>();
        for (int i = 0; i < PerDay; i++)
        {
            ContractKind[] kinds = i < PerDay - 1 ? alone : all;
            ContractKind first = kinds[rng.Next(kinds.Length)];
            Contract? c = null;
            // From the kind drawn on, the first that has a target for this character.
            for (int k = 0; k < kinds.Length && c is null; k++)
                c = One(data, kinds[(Array.IndexOf(kinds, first) + k) % kinds.Length], level, reach, rng);
            if (c is not null)
                drawn.Add(c);
        }
        return drawn;
    }

    /// <summary>One contract of a kind for this character, or null when nothing of that kind fits it.</summary>
    private static Contract? One(GameData data, ContractKind kind, int level, Reach reach, Random rng)
    {
        T? Pick<T>(IReadOnlyList<T> from) where T : class => from.Count == 0 ? null : from[rng.Next(from.Count)];
        string[] families = [.. Families(data, level, reach.Zones)];
        switch (kind)
        {
            case ContractKind.Hunt:
                return Pick(families) is string f ? new Contract(kind, f, 6, level) : null;
            case ContractKind.Horde:
                return Pick([.. families.Where(f => Hordes(data, f).Any())]) is string h ? new Contract(kind, h, 1, level) : null;
            case ContractKind.Dungeon or ContractKind.Boss:
                return Pick([.. data.Dungeons.Values.Where(d => families.Contains(d.Family)).Select(d => d.Id).Order(StringComparer.Ordinal)]) is string d
                    ? new Contract(kind, d, 1, level) : null;
            case ContractKind.Gather:
                string[] resources = [.. data.Jobs.Values.Where(j => j.Gathers).OrderBy(j => j.Id, StringComparer.Ordinal)
                    .SelectMany(j => j.Harvests!.Where(h => h.Level <= reach.Jobs.GetValueOrDefault(j.Id, 1)).Select(h => h.Item)).Distinct(StringComparer.Ordinal)];
                return Pick(resources) is string r ? new Contract(kind, r, 10, level) : null;
            default:
                // Made at a workshop by a trade of the character's, at its level; never a dungeon's key (the locksmith's).
                string[] made = [.. data.Jobs.Values.Where(j => !j.Gathers && j.Id != Locksmith && reach.Jobs.ContainsKey(j.Id)).OrderBy(j => j.Id, StringComparer.Ordinal)
                    .SelectMany(j => j.Recipes.Where(r => r.Level <= reach.Jobs[j.Id] && r.Level > reach.Jobs[j.Id] - 20).Select(r => r.Item))
                    .Where(data.Items.ContainsKey).Distinct(StringComparer.Ordinal)];
                return Pick(made) is string m ? new Contract(kind, m, 1, level) : null;
        }
    }

    /// <summary>The families of a character's level, ten below at most, in the zones it reaches.</summary>
    public static IEnumerable<string> Families(GameData data, int level, IReadOnlyCollection<string> zones)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(zones);
        return data.Families.Values.Where(f => zones.Contains(f.Zone))
            .Where(f => f.Monsters.Where(m => m.Role != MonsterRole.Boss).Min(m => m.Level) <= level && f.Monsters.Where(m => m.Role != MonsterRole.Boss).Max(m => m.Level) >= level - 10)
            .Select(f => f.Id).Order(StringComparer.Ordinal);
    }

    /// <summary>A family's groups of six monsters and more in the world: its hordes.</summary>
    public static IEnumerable<MonsterGroup> Hordes(GameData data, string family) =>
        (data ?? throw new ArgumentNullException(nameof(data))).Groups.Values.Where(g => g.Family == family && g.Fight is null && g.Monsters.Count >= 6);

    /// <summary>
    /// A contract after a won fight: a hunt counts its family's monsters; a horde, a group of six and more of its family;
    /// a dungeon, its second room or later; a chief, its last room. Nothing for a fight lost.
    /// </summary>
    public static Contract AfterFight(Contract contract, GameData data, Fight fight, (string Dungeon, int Room)? roomWon)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(fight);
        if (fight.WinningTeam != 0)
            return contract;
        string id = fight.Scenario.Id;
        MonsterGroup? group = id.StartsWith(MonsterGroup.Prefix, StringComparison.Ordinal) && data.Groups.TryGetValue(id[MonsterGroup.Prefix.Length..], out MonsterGroup? g) ? g : null;
        int add = contract.Kind switch
        {
            ContractKind.Hunt => Quests.Defeated(data, fight).GetValueOrDefault(contract.Target),
            ContractKind.Horde when group is { Monsters.Count: >= 6 } && group.Family == contract.Target => 1,
            ContractKind.Dungeon when roomWon is var (d, room) && d == contract.Target && room >= 2 => 1,
            ContractKind.Boss when roomWon is var (d, room) && d == contract.Target && room == Core.Dungeons.RoomCount => 1,
            _ => 0,
        };
        return contract with { Done = Math.Min(contract.Count, contract.Done + add) };
    }

    /// <summary>How far a contract has gone: the items owned for a gathering or a making, else what the fights counted.</summary>
    public static int Reached(Contract contract, IReadOnlyList<ItemCount> owned)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(owned);
        return contract.Kind is ContractKind.Gather or ContractKind.Craft
            ? Math.Min(contract.Count, owned.Where(i => i.Item == contract.Target).Sum(i => i.Count))
            : contract.Done;
    }

    public static bool IsComplete(Contract contract, IReadOnlyList<ItemCount> owned) => Reached(contract, owned) >= (contract ?? throw new ArgumentNullException(nameof(contract))).Count;

    /// <summary>The board as the server keeps it (JSON), and back; an empty or unreadable text is an empty board.</summary>
    public static string Write(ContractBoard board) => JsonSerializer.Serialize(board ?? throw new ArgumentNullException(nameof(board)), Json);

    public static ContractBoard Read(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ContractBoard.Empty;
        try
        {
            return JsonSerializer.Deserialize<ContractBoard>(text, Json) ?? ContractBoard.Empty;
        }
        catch (JsonException)
        {
            return ContractBoard.Empty;
        }
    }

    // The same character on the same day, the same board: a seed of its id, the day and the change.
    private static int Seed(Guid character, int day, int salt)
    {
        byte[] b = character.ToByteArray();
        int h = BitConverter.ToInt32(b, 0) ^ BitConverter.ToInt32(b, 4) ^ BitConverter.ToInt32(b, 8) ^ BitConverter.ToInt32(b, 12);
        return unchecked((h * 31 + day) * 31 + salt);
    }
}
