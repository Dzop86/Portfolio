using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>A text shown to the player, in both languages of the site.</summary>
public sealed record LocalizedText(string Fr, string En)
{
    public string In(string lang) => lang == "fr" ? Fr : En;
}

/// <summary>
/// A spell, as described in <c>data/spells.json</c>. Ranges count steps (see <see cref="Cell"/>);
/// a spell may be cast on an empty cell, it then hits nobody.
/// </summary>
public sealed record Spell(
    string Id,
    LocalizedText Name,
    int ApCost,
    int MinRange,
    int MaxRange,
    bool LineOfSight,
    bool InLine,
    int DamageMin,
    int DamageMax,
    int PerTurn)
{
    public double AverageDamage => (DamageMin + DamageMax) / 2.0;
}

/// <summary>A fighter of a scenario: its team (0 or 1), its characteristics and the index of its starting cell.</summary>
public sealed record FighterSpec(
    LocalizedText Name,
    int Team,
    int Hp,
    int Ap,
    int Mp,
    int Initiative,
    int Start,
    IReadOnlyList<string> Spells);

/// <summary>A board, as described in <c>data/maps/*.json</c>.</summary>
public sealed record MapSpec(string Id, LocalizedText Name, IReadOnlyList<string> Rows);

/// <summary>A fight to play: a map and its fighters, as described in <c>data/scenarios/*.json</c>.</summary>
public sealed record Scenario(string Id, LocalizedText Name, string Map, IReadOnlyList<FighterSpec> Fighters);

/// <summary>
/// Everything the rules read from the data folder: spells, maps and scenarios. Adding a spell, a map
/// or a monster is a change of data, not of code; <see cref="Load"/> refuses inconsistent data.
/// </summary>
public sealed class GameData
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    public IReadOnlyDictionary<string, Spell> Spells { get; }
    public IReadOnlyDictionary<string, MapSpec> Maps { get; }
    public IReadOnlyDictionary<string, Scenario> Scenarios { get; }

    public GameData(IEnumerable<Spell> spells, IEnumerable<MapSpec> maps, IEnumerable<Scenario> scenarios)
    {
        Spells = Index(spells, s => s.Id, "spell");
        Maps = Index(maps, m => m.Id, "map");
        Scenarios = Index(scenarios, s => s.Id, "scenario");
        foreach (Spell s in Spells.Values)
            Check(s);
        foreach (Scenario s in Scenarios.Values)
            Check(s);
    }

    /// <summary>Reads <c>spells.json</c>, <c>maps/*.json</c> and <c>scenarios/*.json</c> from a folder.</summary>
    public static GameData Load(string folder)
    {
        T Read<T>(string path) =>
            JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json)
            ?? throw new InvalidDataException($"{path} is empty.");
        IEnumerable<T> ReadAll<T>(string sub) =>
            Directory.GetFiles(Path.Combine(folder, sub), "*.json").Order(StringComparer.Ordinal).Select(Read<T>);
        return new GameData(
            Read<List<Spell>>(Path.Combine(folder, "spells.json")),
            ReadAll<MapSpec>("maps"),
            ReadAll<Scenario>("scenarios"));
    }

    public Board Board(string mapId) =>
        Maps.TryGetValue(mapId, out MapSpec? map)
            ? Core.Board.Parse(map.Rows)
            : throw new KeyNotFoundException($"Unknown map '{mapId}'.");

    private static Dictionary<string, T> Index<T>(IEnumerable<T> items, Func<T, string> id, string kind)
    {
        var index = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (T item in items)
        {
            if (!index.TryAdd(id(item), item))
                throw new InvalidDataException($"Two {kind}s are called '{id(item)}'.");
        }
        return index;
    }

    private static void Check(Spell s)
    {
        if (s.ApCost < 1 || s.MinRange < 0 || s.MaxRange < s.MinRange || s.DamageMin < 0
            || s.DamageMax < s.DamageMin || s.PerTurn < 1)
            throw new InvalidDataException($"Spell '{s.Id}': cost, ranges, damage or casts per turn out of bounds.");
    }

    private void Check(Scenario s)
    {
        Board board = Board(s.Map);
        var used = new HashSet<(int, int)>();
        foreach (FighterSpec f in s.Fighters)
        {
            string who = $"Scenario '{s.Id}', fighter '{f.Name.En}'";
            if (f.Team is not (0 or 1))
                throw new InvalidDataException($"{who}: team must be 0 or 1.");
            if (f.Hp < 1 || f.Ap < 0 || f.Mp < 0)
                throw new InvalidDataException($"{who}: hit points, action or movement points out of bounds.");
            if (f.Start < 0 || f.Start >= board.Starts[f.Team].Count || !used.Add((f.Team, f.Start)))
                throw new InvalidDataException($"{who}: no free starting cell {f.Start} for team {f.Team}.");
            foreach (string spell in f.Spells)
            {
                if (!Spells.ContainsKey(spell))
                    throw new InvalidDataException($"{who}: unknown spell '{spell}'.");
            }
        }
        for (int team = 0; team < 2; team++)
        {
            if (!s.Fighters.Any(f => f.Team == team))
                throw new InvalidDataException($"Scenario '{s.Id}': team {team} has nobody.");
        }
    }
}
