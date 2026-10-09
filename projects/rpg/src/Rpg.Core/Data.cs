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
/// a spell may be cast on an empty cell, it then hits nobody. Its damage strikes the enemies in its
/// <see cref="Area"/> (one cell by default) in its <see cref="Element"/>; its other
/// <see cref="Effects"/> follow, in order. After a cast, the caster waits <see cref="Cooldown"/> of
/// its turns before casting it again (0: no wait, only the limit per turn).
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
    int PerTurn,
    Element Element = Element.Neutral,
    Area? Area = null,
    IReadOnlyList<SpellEffect>? Effects = null,
    int Level = 1,
    IReadOnlyList<SpellRank>? Ranks = null,
    int Cooldown = 0)
{
    /// <summary>The highest rank: 1 plus the ranks listed.</summary>
    public int MaxRank => 1 + (Ranks?.Count ?? 0);

    /// <summary>The spell at a rank (1 is as described; above, the rank's numbers replace the base ones).</summary>
    public Spell AtRank(int rank)
    {
        if (rank <= 1 || Ranks is null)
            return this;
        SpellRank r = Ranks[Math.Min(rank, MaxRank) - 2];
        return this with
        {
            DamageMin = r.DamageMin,
            DamageMax = r.DamageMax,
            ApCost = r.ApCost ?? ApCost,
            MaxRange = r.MaxRange ?? MaxRange,
            PerTurn = r.PerTurn ?? PerTurn,
        };
    }

    public double AverageDamage => (DamageMin + DamageMax) / 2.0;

    public Area Zone => Area ?? Core.Area.One;

    public IReadOnlyList<SpellEffect> AllEffects => Effects ?? [];
}

/// <summary>What a spell's rank 2 to 5 changes: its damage, and if given its cost, range and casts per turn.</summary>
public sealed record SpellRank(int DamageMin, int DamageMax, int? ApCost = null, int? MaxRange = null, int? PerTurn = null);

/// <summary>
/// The five characteristics: Vitality adds hit points, each other one adds 1 % of damage per point
/// in its element (Strength: earth and neutral; Intelligence: fire, and healing; Chance: water;
/// Agility: air).
/// </summary>
public sealed record Characteristics(int Vitality = 0, int Strength = 0, int Intelligence = 0, int Chance = 0, int Agility = 0)
{
    public static readonly Characteristics None = new();

    /// <summary>The characteristic that strengthens an element's damage.</summary>
    public int For(Element element) => element switch
    {
        Element.Fire => Intelligence,
        Element.Water => Chance,
        Element.Air => Agility,
        _ => Strength,
    };
}

/// <summary>
/// A quest, as described in <c>data/quests.json</c>: done the first time the hero wins a fight of
/// its <see cref="Scenario"/>, which the server checks by replaying that fight; worth <see cref="Xp"/>, once.
/// </summary>
public sealed record Quest(string Id, LocalizedText Name, long Xp, string Scenario);

/// <summary>A creature a spell can summon, as described in <c>data/summons.json</c>.</summary>
public sealed record SummonSpec(string Id, LocalizedText Name, string Look, int Hp, int Ap, int Mp, int Initiative, IReadOnlyList<string> Spells);

/// <summary>
/// A fighter of a scenario: its look (the 3D model the client shows; the rules ignore it), its team
/// (0 or 1), its characteristics and the index of its starting cell.
/// </summary>
public sealed record FighterSpec(
    LocalizedText Name,
    string Look,
    int Team,
    int Hp,
    int Ap,
    int Mp,
    int Initiative,
    int Start,
    IReadOnlyList<string> Spells,
    IReadOnlyDictionary<Element, int>? Resistances = null,
    Characteristics? Stats = null,
    IReadOnlyDictionary<string, int>? SpellRanks = null,
    int Level = 1)
{
    public Characteristics Characteristics => Stats ?? Characteristics.None;

    /// <summary>The fighter's resistance to an element, in percent (neutral damage ignores resistances).</summary>
    public int Resistance(Element element) => element == Element.Neutral ? 0 : Resistances?.GetValueOrDefault(element) ?? 0;
}

/// <summary>
/// A class the player can choose for their hero, as described in <c>data/classes.json</c>: its
/// characteristics and spells replace those of the scenario's hero; the hero gains
/// <see cref="HpPerLevel"/> hit points with each level and gets the spells its level has unlocked.
/// </summary>
public sealed record HeroClass(
    string Id,
    LocalizedText Name,
    LocalizedText Description,
    int Hp,
    int Ap,
    int Mp,
    int Initiative,
    IReadOnlyList<string> Spells,
    int HpPerLevel = Fight.HpPerLevel);

/// <summary>A board, as described in <c>data/maps/*.json</c>.</summary>
public sealed record MapSpec(string Id, LocalizedText Name, IReadOnlyList<string> Rows);

/// <summary>A fight to play: a map and its fighters, as described in <c>data/scenarios/*.json</c>.</summary>
public sealed record Scenario(string Id, LocalizedText Name, string Map, IReadOnlyList<FighterSpec> Fighters);

/// <summary>
/// Everything the rules read from the data folder: spells, maps, scenarios, the heroes' classes,
/// towns and dialogues. Adding a spell, a map, a monster, a class, a town or a line of dialogue is a
/// change of data, not of code; <see cref="Load"/> refuses inconsistent data.
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

    /// <summary>The classes, in the order of the data file (the order the creation screen shows).</summary>
    public IReadOnlyList<HeroClass> Classes { get; }

    public IReadOnlyDictionary<string, Town> Towns { get; }
    public IReadOnlyDictionary<string, Dialogue> Dialogues { get; }
    public IReadOnlyDictionary<string, SummonSpec> Summons { get; }
    public IReadOnlyDictionary<string, Quest> Quests { get; }

    public GameData(IEnumerable<Spell> spells, IEnumerable<MapSpec> maps, IEnumerable<Scenario> scenarios, IEnumerable<HeroClass>? classes = null,
        IEnumerable<Town>? towns = null, IEnumerable<Dialogue>? dialogues = null, IEnumerable<SummonSpec>? summons = null, IEnumerable<Quest>? quests = null)
    {
        Spells = Index(spells, s => s.Id, "spell");
        Maps = Index(maps, m => m.Id, "map");
        Scenarios = Index(scenarios, s => s.Id, "scenario");
        Classes = [.. Index(classes ?? [], c => c.Id, "class").Values];
        Summons = Index(summons ?? [], s => s.Id, "summon");
        foreach (Spell s in Spells.Values)
        {
            Check(s);
            foreach (SummonEffect summon in s.AllEffects.OfType<SummonEffect>())
            {
                if (!Summons.ContainsKey(summon.Summon))
                    throw new InvalidDataException($"Spell '{s.Id}': unknown summon '{summon.Summon}'.");
            }
        }
        foreach (SummonSpec m in Summons.Values)
        {
            if (m.Hp < 1 || m.Ap < 0 || m.Mp < 0 || m.Spells.Any(id => !Spells.ContainsKey(id)))
                throw new InvalidDataException($"Summon '{m.Id}': hit points, points or spells out of bounds.");
        }
        foreach (Scenario s in Scenarios.Values)
            Check(s);
        foreach (HeroClass c in Classes)
            Check(c);
        Dialogues = Index(dialogues ?? [], d => d.Id, "dialogue");
        Towns = Index(towns ?? [], t => t.Id, "town");
        foreach (Dialogue d in Dialogues.Values)
            Check(d);
        foreach (Town t in Towns.Values)
            Check(t);
        Quests = Index(quests ?? [], q => q.Id, "quest");
        foreach (Quest q in Quests.Values)
        {
            if (q.Xp < 1 || !Scenarios.ContainsKey(q.Scenario))
                throw new InvalidDataException($"Quest '{q.Id}': no experience, or an unknown scenario '{q.Scenario}'.");
        }
    }

    public HeroClass? Class(string? id) => Classes.FirstOrDefault(c => c.Id == id);

    /// <summary>Reads <c>spells.json</c>, <c>classes.json</c> (if there is one), <c>maps/*.json</c> and <c>scenarios/*.json</c> from a folder.</summary>
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
            ReadAll<Scenario>("scenarios"),
            File.Exists(Path.Combine(folder, "classes.json")) ? Read<List<HeroClass>>(Path.Combine(folder, "classes.json")) : null,
            Directory.Exists(Path.Combine(folder, "towns")) ? ReadAll<Town>("towns") : null,
            Directory.Exists(Path.Combine(folder, "dialogues")) ? ReadAll<Dialogue>("dialogues") : null,
            File.Exists(Path.Combine(folder, "summons.json")) ? Read<List<SummonSpec>>(Path.Combine(folder, "summons.json")) : null,
            File.Exists(Path.Combine(folder, "quests.json")) ? Read<List<Quest>>(Path.Combine(folder, "quests.json")) : null);
    }

    /// <summary>
    /// The data files built into this library (<c>data/</c> of the project, see Rpg.Core.csproj): what
    /// the game, the server and the tests read, whatever folder they run from.
    /// </summary>
    public static GameData Embedded { get; } = LoadEmbedded();

    private static GameData LoadEmbedded()
    {
        var assembly = typeof(GameData).Assembly;
        var files = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("data/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToDictionary(n => n, n =>
            {
                using var reader = new StreamReader(assembly.GetManifestResourceStream(n)!);
                return reader.ReadToEnd();
            });
        T Parse<T>(string name, string json) =>
            JsonSerializer.Deserialize<T>(json, Json) ?? throw new InvalidDataException($"{name} is empty.");
        IEnumerable<T> All<T>(string prefix) =>
            files.Where(f => f.Key.StartsWith(prefix, StringComparison.Ordinal)).Select(f => Parse<T>(f.Key, f.Value));
        return new GameData(Parse<List<Spell>>("spells.json", files["data/spells.json"]), All<MapSpec>("data/maps/"), All<Scenario>("data/scenarios/"),
            Parse<List<HeroClass>>("classes.json", files["data/classes.json"]), All<Town>("data/towns/"), All<Dialogue>("data/dialogues/"),
            files.TryGetValue("data/summons.json", out string? summons) ? Parse<List<SummonSpec>>("summons.json", summons) : null,
            files.TryGetValue("data/quests.json", out string? quests) ? Parse<List<Quest>>("quests.json", quests) : null);
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
            || s.DamageMax < s.DamageMin || s.PerTurn < 1 || s.Cooldown < 0)
            throw new InvalidDataException($"Spell '{s.Id}': cost, ranges, damage or casts per turn out of bounds.");
        if (s.Level is < 1 or > 100 || (s.Ranks?.Count ?? 0) > 4
            || (s.Ranks ?? []).Any(r => r.DamageMin < 0 || r.DamageMax < r.DamageMin || r.ApCost < 1 || r.MaxRange < s.MinRange || r.PerTurn < 1))
            throw new InvalidDataException($"Spell '{s.Id}': level 1 to 100, at most 5 ranks, each consistent.");
        if (s.Zone.Radius is < 0 or > 5 || (s.Zone.Shape == AreaShape.Point && s.Zone.Radius != 0))
            throw new InvalidDataException($"Spell '{s.Id}': an area's radius is 1 to 5 (0 for a single cell).");
        foreach (SpellEffect e in s.AllEffects)
        {
            bool ok = e switch
            {
                HealEffect h => h.Min >= 0 && h.Max >= h.Min && h.Max > 0,
                ShieldEffect sh => sh.Amount > 0 && sh.Turns is >= 1 and <= 10,
                PushEffect p => p.Cells is >= 1 and <= 5 && p.Affects != Affects.Caster,
                PullEffect p => p.Cells is >= 1 and <= 5 && p.Affects != Affects.Caster,
                StatusEffect st => st.Value != 0 && st.Turns is >= 1 and <= 10 && (st.Stat != Stat.Poison || st.Value > 0),
                SummonEffect sm => sm.Max is >= 1 and <= 4,
                _ => false,
            };
            if (!ok)
                throw new InvalidDataException($"Spell '{s.Id}': effect {e} out of bounds.");
        }
    }

    private void Check(HeroClass c)
    {
        string who = $"Class '{c.Id}'";
        if (c.Hp < 1 || c.Ap < 0 || c.Mp < 0 || c.HpPerLevel < 0)
            throw new InvalidDataException($"{who}: hit points, action or movement points out of bounds.");
        if (c.Spells.Count is < 1 or > 30)
            throw new InvalidDataException($"{who}: 1 to 30 spells (the keys 1 to 9 choose the first nine of the bar).");
        foreach (string spell in c.Spells)
        {
            if (!Spells.ContainsKey(spell))
                throw new InvalidDataException($"{who}: unknown spell '{spell}'.");
        }
    }

    private void Check(Dialogue d)
    {
        string who = $"Dialogue '{d.Id}'";
        if (!d.Lines.ContainsKey(d.Start))
            throw new InvalidDataException($"{who}: no line '{d.Start}' to start with.");
        foreach ((string id, DialogueLine line) in d.Lines)
        {
            if (line.Answers.Count is < 1 or > 4)
                throw new InvalidDataException($"{who}, line '{id}': 1 to 4 answers.");
            foreach (DialogueAnswer a in line.Answers)
            {
                if (a.Next is not null && !d.Lines.ContainsKey(a.Next))
                    throw new InvalidDataException($"{who}, line '{id}': no line '{a.Next}'.");
                if (a.Fight is not null && (a.Next is not null || !Scenarios.ContainsKey(a.Fight)))
                    throw new InvalidDataException($"{who}, line '{id}': a fight ends the talk, in a known scenario.");
            }
        }
        // Every line can be reached from the start, and from every line the talk can end.
        var reached = Closure([d.Start], id => d.Lines[id].Answers.Select(a => a.Next).OfType<string>());
        if (d.Lines.Keys.FirstOrDefault(id => !reached.Contains(id)) is string lost)
            throw new InvalidDataException($"{who}: line '{lost}' cannot be reached.");
        var ending = Closure(d.Lines.Where(l => l.Value.Answers.Any(a => a.Next is null)).Select(l => l.Key),
            id => d.Lines.Where(l => l.Value.Answers.Any(a => a.Next == id)).Select(l => l.Key));
        if (d.Lines.Keys.FirstOrDefault(id => !ending.Contains(id)) is string endless)
            throw new InvalidDataException($"{who}: from line '{endless}' the talk never ends.");
    }

    private static HashSet<string> Closure(IEnumerable<string> start, Func<string, IEnumerable<string>> next)
    {
        var seen = new HashSet<string>(start, StringComparer.Ordinal);
        var queue = new Queue<string>(seen);
        while (queue.TryDequeue(out string? id))
        {
            foreach (string n in next(id))
            {
                if (seen.Add(n))
                    queue.Enqueue(n);
            }
        }
        return seen;
    }

    private void Check(Town t)
    {
        string who = $"Town '{t.Id}'";
        Board board;
        try
        {
            if (t.Rows.SelectMany(r => r).FirstOrDefault(c => !Town.Decor.ContainsKey(c)) is char unknown and not '\0')
                throw new InvalidDataException($"{who}: unknown cell '{unknown}'.");
            board = t.Board();
        }
        catch (FormatException e)
        {
            throw new InvalidDataException($"{who}: {e.Message}");
        }
        // Houses and fountains fill 2 × 2 cells: an upper-case corner, three lower-case cells.
        foreach (Cell c in board.Cells())
        {
            char k = t[c];
            if (k is 'H' or 'F')
            {
                char rest = char.ToLowerInvariant(k);
                if (t[c + new Cell(1, 0)] != rest || t[c + new Cell(0, 1)] != rest || t[c + new Cell(1, 1)] != rest)
                    throw new InvalidDataException($"{who}: the {Town.Decor[k]} at {c} does not fill 2 × 2 cells.");
            }
            else if (k is 'h' or 'f')
            {
                char corner = char.ToUpperInvariant(k);
                if (!new[] { new Cell(-1, 0), new Cell(0, -1), new Cell(-1, -1) }.Any(d => t[c + d] == corner))
                    throw new InvalidDataException($"{who}: the {Town.Decor[k]} cell {c} has no corner.");
            }
        }
        if (!t.IsFree(board, t.Spawn))
            throw new InvalidDataException($"{who}: the arrival {t.Spawn} is not a free floor cell.");
        HashSet<Cell> reach = [.. Pathfinding.Reachable(board, t.Spawn, board.Width * board.Height, c => t.IsFree(board, c)).Keys];
        var cells = new HashSet<Cell>();
        foreach (Npc n in t.Npcs)
        {
            string npc = $"{who}, {n.Id}";
            if (!board.IsFloor(n.At) || !cells.Add(n.At))
                throw new InvalidDataException($"{npc}: not on a floor cell of its own.");
            if (!Dialogues.ContainsKey(n.Dialogue))
                throw new InvalidDataException($"{npc}: unknown dialogue '{n.Dialogue}'.");
            if (!Hero.Looks.Contains(n.Look) || n.Colour is < 0 or >= Hero.Colours)
                throw new InvalidDataException($"{npc}: unknown look or colour.");
            if (!n.At.Neighbours().Any(reach.Contains))
                throw new InvalidDataException($"{npc}: nobody can walk up to them from the arrival.");
        }
        foreach (TownExit e in t.Exits)
        {
            if (!reach.Contains(e.At) || !cells.Add(e.At))
                throw new InvalidDataException($"{who}: the exit at {e.At} cannot be reached.");
            if (!Scenarios.ContainsKey(e.Scenario))
                throw new InvalidDataException($"{who}: the exit at {e.At} leads to an unknown scenario '{e.Scenario}'.");
        }
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
            if (string.IsNullOrWhiteSpace(f.Look))
                throw new InvalidDataException($"{who}: no look.");
            if (f.Hp < 1 || f.Ap < 0 || f.Mp < 0 || f.Level is < 1 or > Hero.MaxLevel)
                throw new InvalidDataException($"{who}: hit points, action or movement points or level out of bounds.");
            if (f.Resistances is not null && f.Resistances.Any(r => r.Key == Element.Neutral || r.Value is < -100 or > 90))
                throw new InvalidDataException($"{who}: resistances are per element, from -100 to 90 percent.");
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
