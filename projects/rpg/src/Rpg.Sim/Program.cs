using System.Globalization;
using System.Text;
using System.Text.Json;
using Rpg.Core;

namespace Rpg.Sim;

/// <summary>
/// Plays fights between AIs, records them and replays them, from the terminal:
///   rpg-sim --simulate 500 --scenario training [--seed 1]
///   rpg-sim --record fight.json --scenario duel --seed 7
///   rpg-sim --replay fight.json [--show]
/// Options: --data (a data folder; by default the data built into Rpg.Core), --lang fr|en (default en),
/// --class ID (the hero of team A plays that class of data/classes.json), --rival ID (team B's first
/// fighter too, for class duels), --level N (both heroes' level, 1 by default, their points spent
/// the simplest way: Progression.Built), --party ID,ID... (since sprint 121, up to three allies of these classes beside the
/// hero, at the same level with the same kit, all played by the AI), --parties (since sprint 122, with --simulate N:
/// every party of two, three and four heroes of the classes, N fights each, at the level of the scenario's monsters
/// unless --level says otherwise; each rate with its 95 % interval), --matrix (since sprint 193, D96, with --simulate N, optionally --class,
/// --level, --decks FILE and, since sprint 210, --unranked-paths: the bench of each class's paths, their path spells ranked as a player would (D98) or, with --unranked-paths, at rank 1 as before; a Markdown table), --talents (since sprint 151, D82, with --simulate N: the
/// talent tree out of CI, each class with no compound or one created, N fights a combination, each with a build drawn at
/// random at --level, 45 by default; then the talents fewer than 5 % of the builds take; since sprint 152, D83: the bare
/// heroes, mixed and of each element, on the same fights, the rare talents counted among the builds aiming at their branch
/// (20 %), and --trio for three heroes of the three classes, one with the compound, two without; since sprint 182, D93, the
/// elements left the tree: a combination is a compound created, no longer a pair taken, and --no-braid is gone).
/// Exit code 0 on success, 1 for a record that does not replay, 2 for wrong arguments.
/// </summary>
public static class Program
{
    public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    // The allies' names (--party), a name being letters only.
    private static readonly string[] AllyNames = ["Brume", "Cendre", "Dune"];

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        Options options;
        try
        {
            options = Options.Parse(args);
        }
        catch (ArgumentException e)
        {
            error.WriteLine(e.Message);
            return 2;
        }
        GameData data = options.Data is null ? GameData.Embedded : GameData.Load(options.Data);
        var text = new Texts(options.Lang);
        try
        {
            if (options.Replay is string replayPath)
            {
                Fight fight = FightRecord.FromJson(File.ReadAllText(replayPath)).Replay(data);
                output.WriteLine(text.Summary(fight));
                if (options.Show)
                    output.Write(Draw(fight, options.Lang));
                return 0;
            }
            if (data.FindScenario(options.Scenario) is null && !(options.Tree && SmallGroups(data, options.Scenario).Count > 0))
            {
                error.WriteLine($"Unknown scenario '{options.Scenario}'.");
                return 2;
            }
            if (options.Matrix)
            {
                if (options.Class is string only && data.Class(only) is null)
                {
                    error.WriteLine($"Unknown class '{only}'.");
                    return 2;
                }
                SimulateMatrix(data, options, output);
                return 0;
            }
            if (options.Tree)
            {
                SimulateTree(data, options, text, output);
                return 0;
            }
            if (options.Parties)
            {
                SimulateParties(data, options, text, output);
                return 0;
            }
            Hero? hero = null, rival = null;
            var allies = new List<Hero>();
            foreach ((string? id, bool isRival) in new[] { (options.Class, false), (options.Rival, true) })
            {
                if (id is null)
                    continue;
                if (data.Class(id) is not HeroClass c)
                {
                    error.WriteLine($"Unknown class '{id}'.");
                    return 2;
                }
                // Its points spent the simplest way (Progression.Built), as a player of that level would have them;
                // wearing the kit asked for (sprint 95), from the sets of the zone of the scenario's monsters.
                IReadOnlyDictionary<Slot, string>? worn = ZoneOf(data, options.Scenario) is string zone ? Progression.Kit(options.Kit, zone, options.Level, data) : null;
                Hero h = Progression.Built(new Hero(c.Name.In(options.Lang), isRival ? "male-d" : "female-d", c.Id, Level: options.Level), data, worn);
                if (isRival)
                    rival = h;
                else
                    hero = h;
            }
            foreach (string id in options.Party)
            {
                if (data.Class(id) is not HeroClass c || hero is null)
                {
                    error.WriteLine(hero is null ? "--party needs --class." : $"Unknown class '{id}'.");
                    return 2;
                }
                IReadOnlyDictionary<Slot, string>? worn = ZoneOf(data, options.Scenario) is string zone ? Progression.Kit(options.Kit, zone, options.Level, data) : null;
                allies.Add(Progression.Built(new Hero(AllyNames[allies.Count], "female-c", c.Id, Level: options.Level), data, worn));
            }
            if (options.Record is string recordPath)
            {
                var fight = new Fight(data, options.Scenario, options.Seed, hero, rival, null, allies);
                Ai.PlayOut(fight);
                File.WriteAllText(recordPath, FightRecord.Of(fight).ToJson());
                output.WriteLine(text.Summary(fight));
                return 0;
            }
            Simulate(data, options, hero, rival, allies, text, output);
            return 0;
        }
        catch (InvalidFightRecordException e)
        {
            error.WriteLine(e.Message);
            return 1;
        }
    }

    private static void Simulate(GameData data, Options options, Hero? hero, Hero? rival, IReadOnlyList<Hero> allies, Texts text, TextWriter output)
    {
        int[] wins = new int[2];
        int draws = 0;
        long rounds = 0, actions = 0;
        for (int i = 0; i < options.Simulate; i++)
        {
            var fight = new Fight(data, options.Scenario, options.Seed + (ulong)i, hero, rival, null, allies);
            actions += Ai.PlayOut(fight);
            rounds += fight.Round;
            if (fight.WinningTeam is int team)
                wins[team]++;
            else
                draws++;
        }
        output.WriteLine(text.Stats(data.FindScenario(options.Scenario)!, options.Simulate, wins, draws,
            (double)rounds / options.Simulate, (double)actions / options.Simulate));
    }

    /// <summary>
    /// Every party of two, three and four heroes (sprint 122): the classes' combinations with repetition, in the
    /// order of the data (6, 10 and 15 with three classes), the first class playing the hero; the same seeds for each.
    /// </summary>
    internal static IEnumerable<IReadOnlyList<string>> Parties(IReadOnlyList<string> classes, int size)
    {
        IEnumerable<IReadOnlyList<string>> From(int first, int left) =>
            left == 0 ? [[]] : Enumerable.Range(first, classes.Count - first).SelectMany(i => From(i, left - 1).Select(rest => (IReadOnlyList<string>)[classes[i], .. rest]));
        return From(0, size);
    }

    /// <summary>The 95 % Wilson interval of <paramref name="wins"/> out of <paramref name="n"/>, in percent.</summary>
    internal static (double Low, double High) Wilson(int wins, int n)
    {
        const double z = 1.96;
        double p = (double)wins / n, d = 1 + z * z / n;
        double centre = (p + z * z / (2 * n)) / d, half = z * Math.Sqrt(p * (1 - p) / n + z * z / (4.0 * n * n)) / d;
        return (100 * Math.Max(0, centre - half), 100 * Math.Min(1, centre + half));
    }

    /// <summary>The measuring bench (D96): each class's paths at each level, as a Markdown table for `docs/mesures-classes.md`.</summary>
    private static void SimulateMatrix(GameData data, Options options, TextWriter output)
    {
        string[] classes = options.Class is string only ? [only] : [.. data.Classes.Select(c => c.Id).Where(c => data.Specs.Values.Any(s => s.Class == c))];
        int[] levels = options.LevelSet ? [options.Level] : [35, 45, 50];
        Dictionary<string, string[]> decks = options.Decks is string file
            ? JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(file)) ?? []
            : [];
        output.WriteLine($"{options.Simulate} fights per group, {Bench.MiddleGroups(new SyntheticFamily(levels[0])).Count} groups of 4 and 5 monsters, bare heroes, the same seeds{(options.UnrankedPaths ? ", path spells at rank 1" : ", path spells ranked")}.");
        output.WriteLine();
        output.WriteLine("| Class | Level | Path | Points | Over no path | From the mean | Reads |");
        output.WriteLine("|---|---|---|---|---|---|---|");
        foreach (int level in levels)
        {
            var family = new SyntheticFamily(level);
            foreach (string cls in classes)
            {
                BenchReport report = Bench.Run(family, cls, options.Simulate, deck: decks.GetValueOrDefault(cls), rankedPaths: !options.UnrankedPaths);
                output.WriteLine(FormattableString.Invariant($"| {cls} | {level} | (none) | {report.Plain.Points:0.0} | | | |"));
                foreach (BenchRow row in report.Paths)
                {
                    // Every path spell's casts a fight (sprint 218), not only those under the bench's least.
                    string reads = options.Casts ? string.Join(", ", row.Casts.Select(c => FormattableString.Invariant($"{c.Key} {c.Value:0.0}"))) : Reads(row);
                    output.WriteLine(FormattableString.Invariant($"| {cls} | {level} | {row.Path} | {row.Points:0.0} | {row.Points - report.Plain.Points:+0.0;-0.0;0.0} | {row.Points - report.Mean:+0.0;-0.0;0.0} | {reads} |"));
                }
            }
        }
    }

    /// <summary>
    /// The tree out of CI (sprint 151, J5 b, J6 a): the combinations of a class and a compound created (or none, sprint 182,
    /// D93), N fights each, every fight a build drawn at random (<see cref="TreeDraws.Random"/>), the same seeds for each;
    /// then the rare talents.
    /// </summary>
    private static void SimulateTree(GameData data, Options options, Texts text, TextWriter output)
    {
        // A zone (zone:ancestors): its groups of three monsters at most, one fight after the other, as the balance measures do.
        IReadOnlyList<string> fights = data.FindScenario(options.Scenario) is null ? SmallGroups(data, options.Scenario) : [options.Scenario];
        Scenario scenario = data.FindScenario(fights[0])!;
        int level = options.LevelSet ? options.Level : 45;
        IReadOnlyDictionary<Slot, string>? worn = ZoneOf(data, fights[0]) is string zone ? Progression.Kit(options.Kit, zone, level, data) : null;
        string[] classes = [.. data.Classes.Select(c => c.Id)];
        string[] pairs = Compounds;
        (string Class, string Pair)[] combos = [.. classes.SelectMany(c => pairs.Select(p => (c, p)))];
        int[] wins = new int[combos.Length];
        var builds = new IReadOnlyList<string>[combos.Length * options.Simulate];
        Parallel.For(0, combos.Length, k =>
        {
            for (int i = 0; i < options.Simulate; i++)
            {
                // A draw of its own for each combination and each fight, the same from one run to the next.
                IReadOnlyList<string> build = TreeDraws.Random(level, new Random(unchecked((int)options.Seed * 1_000_003 + (k * 7_919) + i)), data);
                builds[(k * options.Simulate) + i] = build;
                Hero hero = Progression.Talented(new Hero("Aube", "female-c", combos[k].Class, Level: level), build, data, worn, Created(combos[k].Pair));
                var fight = new Fight(data, fights[i % fights.Count], options.Seed + (ulong)i, hero);
                Ai.PlayOut(fight);
                if (fight.WinningTeam == 0)
                    wins[k]++;
            }
        });
        output.WriteLine(text.TreeHeader(fights.Count > 1 ? options.Scenario : scenario.Name.In(options.Lang), level, options.Kit, options.Simulate));
        output.WriteLine();
        output.WriteLine(text.TreeColumns);
        output.WriteLine("|---|---|---|---|");
        for (int k = 0; k < combos.Length; k++)
        {
            (double low, double high) = Wilson(wins[k], options.Simulate);
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"| {combos[k].Class} | {combos[k].Pair} | {100.0 * wins[k] / options.Simulate:0.0} % | {low:0.0} – {high:0.0} % |"));
        }
        // The bare heroes on the same fights (sprint 152, D83, T110): mixed, then of each element; the tree is held to the best.
        string[] bares = ["mixed", .. Enum.GetValues<Element>().Where(e => e is Element.Earth or Element.Fire or Element.Water or Element.Air).Select(e => e.ToString().ToLowerInvariant())];
        foreach (string cls in classes)
        {
            foreach (string bare in bares)
            {
                var hero = new Hero("Aube", "female-c", cls, Level: level);
                Hero built = bare == "mixed" ? Progression.Built(hero, data, worn) : Progression.OfOne(hero, Enum.Parse<Element>(bare, ignoreCase: true), data, worn);
                int won = 0;
                Parallel.For(0, options.Simulate, () => 0, (i, _, w) =>
                {
                    var fight = new Fight(data, fights[i % fights.Count], options.Seed + (ulong)i, built);
                    Ai.PlayOut(fight);
                    return fight.WinningTeam == 0 ? w + 1 : w;
                }, w => Interlocked.Add(ref won, w));
                (double low, double high) = Wilson(won, options.Simulate);
                output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"| {cls} | {text.Bare(bare)} | {100.0 * won / options.Simulate:0.0} % | {low:0.0} – {high:0.0} % |"));
            }
        }
        if (options.Trio)
            SimulateTrios(data, options, text, output, fights, level, worn, classes, pairs);
        output.WriteLine();
        IReadOnlyList<(string Talent, double Share)> rare = TreeDraws.Rare(builds, data);
        output.WriteLine(text.Rare(builds.Length));
        foreach ((string talent, double share) in rare)
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"- {talent} : {share:0.0} %"));
        if (rare.Count == 0)
            output.WriteLine(text.NoneRare);
    }

    /// <summary>The compounds a measure of the tree compares (sprint 182, D93): none, then each one the game has.</summary>
    private static string[] Compounds => ["none", .. Elements.Compounds.Select(e => e.ToString())];

    /// <summary>The compounds of a combination: none, or the one named.</summary>
    private static Element[]? Created(string compound) => compound == "none" ? null : [Enum.Parse<Element>(compound)];

    /// <summary>
    /// Trios (sprint 152, D83, the game designer's §12.3 step 4): for each compound (or none), N fights of three heroes, one
    /// of each class in turn holding it, the two others without (sprint 182, D93), every build drawn as a player would.
    /// </summary>
    private static void SimulateTrios(GameData data, Options options, Texts text, TextWriter output, IReadOnlyList<string> fights, int level,
        IReadOnlyDictionary<Slot, string>? worn, string[] classes, string[] pairs)
    {
        int[] wins = new int[pairs.Length];
        Parallel.For(0, pairs.Length, k =>
        {
            for (int i = 0; i < options.Simulate; i++)
            {
                var rng = new Random(unchecked((int)options.Seed * 1_000_003 + (k * 7_919) + i));
                Hero Drawn(string cls, string pair, string name) =>
                    Progression.Talented(new Hero(name, "female-c", cls, Level: level), TreeDraws.Random(level, rng, data), data, worn, Created(pair));
                // Three heroes of three classes (sprint 204): the lead in turn, and the two classes after it, whatever the number of classes.
                string lead = classes[i % classes.Length];
                Hero hero = Drawn(lead, pairs[k], "Aube");
                Hero[] allies = [.. Enumerable.Range(1, Math.Min(2, classes.Length - 1)).Select(j => Drawn(classes[(i + j) % classes.Length], "none", AllyNames[j - 1]))];
                var fight = new Fight(data, fights[i % fights.Count], options.Seed + (ulong)i, hero, null, null, allies);
                Ai.PlayOut(fight);
                if (fight.WinningTeam == 0)
                    wins[k]++;
            }
        });
        output.WriteLine();
        output.WriteLine(text.TrioHeader(options.Simulate));
        output.WriteLine();
        output.WriteLine(text.TrioColumns);
        output.WriteLine("|---|---|---|");
        for (int k = 0; k < pairs.Length; k++)
        {
            (double low, double high) = Wilson(wins[k], options.Simulate);
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"| {pairs[k]} | {100.0 * wins[k] / options.Simulate:0.0} % | {low:0.0} – {high:0.0} % |"));
        }
    }

    /// <summary>
    /// The fights of a zone's groups: <c>zone:ID</c> its groups of three monsters at most, <c>hordes:ID</c> those of six or
    /// more (made for playing together), by id; none for anything else.
    /// </summary>
    private static IReadOnlyList<string> SmallGroups(GameData data, string scenario)
    {
        (string? zone, Func<int, bool> size) = scenario.Split(':') switch
        {
            ["zone", string z] => (z, n => n <= 3),
            ["hordes", string z] => (z, n => n >= 6),
            _ => ((string?)null, (Func<int, bool>)(_ => false)),
        };
        return zone is null ? [] : [.. data.Groups.Values.Where(g => g.Fight is null && size(g.Monsters.Count) && data.Towns.TryGetValue(g.Map, out Town? t) && t.Zone == zone)
            .OrderBy(g => g.Id, StringComparer.Ordinal).Select(g => g.Scenario)];
    }

    private static void SimulateParties(GameData data, Options options, Texts text, TextWriter output)
    {
        Scenario scenario = data.FindScenario(options.Scenario)!;
        int level = options.LevelSet ? options.Level : Math.Max(1, scenario.Fighters.Where(f => f.Team == 1).Select(f => f.Level).DefaultIfEmpty(1).Max());
        IReadOnlyDictionary<Slot, string>? worn = ZoneOf(data, options.Scenario) is string zone ? Progression.Kit(options.Kit, zone, level, data) : null;
        Hero Built(string cls, string name) => Progression.Built(new Hero(name, "female-c", cls, Level: level), data, worn);
        string[] classes = [.. data.Classes.Select(c => c.Id)];
        output.WriteLine(text.PartiesHeader(scenario, level, options.Kit, options.Simulate));
        for (int size = 2; size <= 4; size++)
        {
            IReadOnlyList<string>[] parties = [.. Parties(classes, size)];
            int[] wins = new int[parties.Length];
            // Each party on its own thread: the fights only read the data, and every result goes to its own place.
            Parallel.For(0, parties.Length, k =>
            {
                Hero hero = Built(parties[k][0], "Aube");
                Hero[] allies = [.. parties[k].Skip(1).Select((c, i) => Built(c, AllyNames[i]))];
                for (int i = 0; i < options.Simulate; i++)
                {
                    var fight = new Fight(data, options.Scenario, options.Seed + (ulong)i, hero, null, null, allies);
                    Ai.PlayOut(fight);
                    if (fight.WinningTeam == 0)
                        wins[k]++;
                }
            });
            output.WriteLine(text.PartiesTitle(size));
            for (int k = 0; k < parties.Length; k++)
                output.WriteLine(Row(string.Join('+', parties[k]), wins[k], options.Simulate));
            output.WriteLine(Row(text.All, wins.Sum(), options.Simulate * parties.Length));
        }
    }

    private static string Row(string name, int wins, int n)
    {
        (double low, double high) = Wilson(wins, n);
        return string.Create(CultureInfo.InvariantCulture, $"  {name,-36} {wins,5}/{n,-5} {100.0 * wins / n,5:0} %  [{low:0}-{high:0}]");
    }

    /// <summary>The board as text: '.' floor, '#' obstacle, '~' hole, a letter per living fighter (a, b... team A; upper case team B).</summary>
    internal static string Draw(Fight fight, string lang)
    {
        var sb = new StringBuilder();
        for (int y = 0; y < fight.Board.Height; y++)
        {
            for (int x = 0; x < fight.Board.Width; x++)
            {
                var cell = new Cell(x, y);
                sb.Append(fight.At(cell) is Fighter f
                    ? (char)((f.Team == 0 ? 'a' : 'A') + f.Id)
                    : fight.Board[cell] switch { Terrain.Obstacle => '#', Terrain.Hole => '~', _ => '.' });
            }
            sb.Append('\n');
        }
        foreach (Fighter f in fight.Fighters)
            sb.Append(CultureInfo.InvariantCulture, $"{(char)((f.Team == 0 ? 'a' : 'A') + f.Id)} {f.Name.In(lang)} {f.Hp}/{f.MaxHp}\n");
        return sb.ToString();
    }

    /// <summary>The zone of a group's or a dungeon room's monsters, or null.</summary>
    private static string? ZoneOf(GameData data, string scenario) =>
        scenario.StartsWith(MonsterGroup.Prefix, StringComparison.Ordinal) && data.Groups.TryGetValue(scenario[MonsterGroup.Prefix.Length..], out MonsterGroup? g) ? data.Families[g.Family].Zone
        : Dungeons.Parse(scenario) is (string d, _) && data.Dungeons.TryGetValue(d, out Dungeon? dungeon) ? data.Families[dungeon.Family].Zone
        : null;

    private sealed record Options(string? Data, string Lang, string Scenario, ulong Seed, int Simulate, string? Record, string? Replay, bool Show, string? Class = null, string? Rival = null, int Level = 1, string Kit = "none", IReadOnlyList<string>? Allies = null, bool Parties = false, bool LevelSet = false, bool Tree = false, bool Trio = false, bool Matrix = false, string? Decks = null, bool UnrankedPaths = false, bool Casts = false)
    {
        public IReadOnlyList<string> Party => Allies ?? [];

        public static Options Parse(string[] args)
        {
            var o = new Options(null, "en", "training", 1, 0, null, null, false);
            for (int i = 0; i < args.Length; i++)
            {
                string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");
                o = args[i] switch
                {
                    "--data" => o with { Data = Value() },
                    "--lang" => o with { Lang = Value() is "fr" or "en" ? args[i] : throw new ArgumentException("--lang is fr or en.") },
                    "--scenario" => o with { Scenario = Value() },
                    "--seed" => o with { Seed = ulong.TryParse(Value(), CultureInfo.InvariantCulture, out ulong s) ? s : throw new ArgumentException("--seed is a whole number.") },
                    "--simulate" => o with { Simulate = int.TryParse(Value(), CultureInfo.InvariantCulture, out int n) && n > 0 ? n : throw new ArgumentException("--simulate is a positive number.") },
                    "--record" => o with { Record = Value() },
                    "--replay" => o with { Replay = Value() },
                    "--show" => o with { Show = true },
                    "--class" => o with { Class = Value() },
                    "--rival" => o with { Rival = Value() },
                    "--party" => o with { Allies = Value().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is { Length: >= 1 and <= 3 } ids ? ids : throw new ArgumentException("--party is one to three classes, with commas.") },
                    "--kit" => o with { Kit = Progression.Kits.Contains(Value()) ? args[i] : throw new ArgumentException($"--kit is one of: {string.Join(", ", Progression.Kits)}.") },
                    "--level" => o with { Level = int.TryParse(Value(), CultureInfo.InvariantCulture, out int l) && l is >= 1 and <= Hero.MaxLevel ? l : throw new ArgumentException("--level is 1 to 100."), LevelSet = true },
                    "--parties" => o with { Parties = true },
                    "--talents" => o with { Tree = true },
                    "--trio" => o with { Trio = true },
                    "--matrix" => o with { Matrix = true },
                    "--decks" => o with { Decks = Value() },
                    "--unranked-paths" => o with { UnrankedPaths = true },
                    "--casts" => o with { Casts = true },
                    _ => throw new ArgumentException($"Unknown option {args[i]}."),
                };
            }
            int modes = (o.Simulate > 0 ? 1 : 0) + (o.Record is null ? 0 : 1) + (o.Replay is null ? 0 : 1);
            if (o.Parties && (o.Simulate == 0 || o.Class is not null || o.Rival is not null || o.Party.Count > 0))
                throw new ArgumentException("--parties goes with --simulate N, without --class, --rival or --party.");
            if (o.Tree && (o.Simulate == 0 || o.Parties || o.Class is not null || o.Rival is not null || o.Party.Count > 0))
                throw new ArgumentException("--talents goes with --simulate N, without --parties, --class, --rival or --party.");
            if (o.Matrix && (o.Simulate == 0 || o.Parties || o.Tree || o.Rival is not null || o.Party.Count > 0))
                throw new ArgumentException("--matrix goes with --simulate N, without --parties, --talents, --rival or --party.");
            if (o.Decks is not null && !o.Matrix)
                throw new ArgumentException("--decks goes with --matrix.");
            if (o.UnrankedPaths && !o.Matrix)
                throw new ArgumentException("--unranked-paths goes with --matrix.");
            if (o.Casts && !o.Matrix)
                throw new ArgumentException("--casts goes with --matrix.");
            if (o.Trio && !o.Tree)
                throw new ArgumentException("--trio goes with --talents.");
            return modes == 1 ? o : throw new ArgumentException("Choose one of --simulate N, --record FILE or --replay FILE.");
        }
    }

    private sealed class Texts(string lang)
    {
        private bool Fr => lang == "fr";

        public string Summary(Fight f)
        {
            string name = f.Scenario.Name.In(lang);
            if (!f.IsOver)
                return Fr ? $"{name}, graine {f.Seed} : combat interrompu au tour {f.Round}, {f.History.Count} actions." : $"{name}, seed {f.Seed}: fight stopped in round {f.Round}, {f.History.Count} actions.";
            if (f.WinningTeam is not int team)
                return Fr ? $"{name}, graine {f.Seed} : match nul au tour {f.Round}, {f.History.Count} actions." : $"{name}, seed {f.Seed}: draw in round {f.Round}, {f.History.Count} actions.";
            char t = (char)('A' + team);
            return Fr ? $"{name}, graine {f.Seed} : l'équipe {t} gagne au tour {f.Round}, après {f.History.Count} actions." : $"{name}, seed {f.Seed}: team {t} wins in round {f.Round}, after {f.History.Count} actions.";
        }

        public string All => Fr ? "toutes" : "all";

        public string PartiesHeader(Scenario s, int level, string kit, int n) => Fr
            ? $"{s.Name.Fr}, niveau {level}, équipement {kit}, {n} combats par groupe (victoires, taux, intervalle à 95 %) :"
            : $"{s.Name.En}, level {level}, kit {kit}, {n} fights per party (wins, rate, 95 % interval):";

        public string PartiesTitle(int size) => Fr ? $"{size} héros" : $"{size} heroes";

        public string TreeHeader(string fights, int level, string kit, int n) => Fr
            ? $"# L'arbre des talents hors CI : {fights}, niveau {level}, équipement {kit}, {n} combats par classe et par composé, un build tiré au hasard par combat"
            : $"# The talent tree out of CI: {fights}, level {level}, kit {kit}, {n} fights a class and a compound, a build drawn at random each fight";

        public string TreeColumns => Fr ? "| Classe | Composé | Victoires | Intervalle à 95 % |" : "| Class | Compound | Wins | 95 % interval |";

        public string Rare(int builds) => Fr
            ? $"Talents pris par moins de 20 % des builds qui visent leur branche, sur {builds} builds tirés (une fausse décision à revoir) :"
            : $"Talents taken by fewer than 20 % of the builds aiming at their branch, of {builds} builds drawn (a false decision to look at):";

        public string Bare(string kind) => (Fr, kind) switch
        {
            (true, "mixed") => "nu, mixte",
            (false, "mixed") => "bare, mixed",
            (true, _) => $"nu, {kind switch { "earth" => "Terre", "fire" => "Feu", "water" => "Eau", _ => "Air" }} seul",
            _ => $"bare, {kind} only",
        };

        public string TrioHeader(int n) => Fr
            ? $"## Trios : trois héros de trois classes, l'un avec le composé, les deux autres sans, {n} combats par composé"
            : $"## Trios: three heroes of three classes, one with the compound, the two others without, {n} fights a compound";

        public string TrioColumns => Fr ? "| Composé du premier | Victoires | Intervalle à 95 % |" : "| First hero's compound | Wins | 95 % interval |";

        public string NoneRare => Fr ? "- aucun" : "- none";

        public string Stats(Scenario s, int n, int[] wins, int draws, double rounds, double actions)
        {
            string r = rounds.ToString("0.0", CultureInfo.InvariantCulture), a = actions.ToString("0.0", CultureInfo.InvariantCulture);
            return Fr
                ? $"{s.Name.Fr}, {n} combats : équipe A {wins[0]}, équipe B {wins[1]}, nuls {draws} ; {r} tours et {a} actions en moyenne."
                : $"{s.Name.En}, {n} fights: team A {wins[0]}, team B {wins[1]}, draws {draws}; {r} rounds and {a} actions on average.";
        }
    }

    /// <summary>A matrix row's reading: a path without a spell of its own is never called conclusive (sprint 199), whatever its points.</summary>
    internal static string Reads(BenchRow row) => row.Casts.Count == 0 ? "no spell yet" : row.Conclusive ? "conclusive"
        : "NOT CONCLUSIVE: " + string.Join(", ", row.Unplayed.Select(s => FormattableString.Invariant($"{s} {row.Casts[s]:0.0}")));
}
