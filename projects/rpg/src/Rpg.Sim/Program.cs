using System.Globalization;
using System.Text;
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
/// unless --level says otherwise; each rate with its 95 % interval), --talents (since sprint 151, D82, with --simulate N: the
/// talent tree out of CI, each class with each pair, N fights a combination, each with a build drawn at random from the pair
/// at --level, 45 by default; then the talents fewer than 5 % of the builds take).
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

    /// <summary>
    /// The tree out of CI (sprint 151, J5 b, J6 a): the 18 combinations of a class and a pair, N fights each, every fight a
    /// build drawn at random from the pair (<see cref="TreeDraws.Random"/>), the same seeds for each; then the rare talents.
    /// </summary>
    private static void SimulateTree(GameData data, Options options, Texts text, TextWriter output)
    {
        // A zone (zone:ancestors): its groups of three monsters at most, one fight after the other, as the balance measures do.
        IReadOnlyList<string> fights = data.FindScenario(options.Scenario) is null ? SmallGroups(data, options.Scenario) : [options.Scenario];
        Scenario scenario = data.FindScenario(fights[0])!;
        int level = options.LevelSet ? options.Level : 45;
        IReadOnlyDictionary<Slot, string>? worn = ZoneOf(data, fights[0]) is string zone ? Progression.Kit(options.Kit, zone, level, data) : null;
        string[] classes = [.. data.Classes.Select(c => c.Id)];
        string[] pairs = [.. data.Talents.Values.Where(t => t.IsPair).Select(t => t.Id)];
        (string Class, string Pair)[] combos = [.. classes.SelectMany(c => pairs.Select(p => (c, p)))];
        int[] wins = new int[combos.Length];
        var builds = new IReadOnlyList<string>[combos.Length * options.Simulate];
        Parallel.For(0, combos.Length, k =>
        {
            for (int i = 0; i < options.Simulate; i++)
            {
                // A draw of its own for each combination and each fight, the same from one run to the next.
                IReadOnlyList<string> build = TreeDraws.Random(combos[k].Pair, level, new Random(unchecked((int)options.Seed * 1_000_003 + (k * 7_919) + i)), data);
                builds[(k * options.Simulate) + i] = build;
                Hero hero = Progression.Talented(new Hero("Aube", "female-c", combos[k].Class, Level: level), build, data, worn);
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
        output.WriteLine();
        IReadOnlyList<(string Talent, double Share)> rare = TreeDraws.Rare(builds, data);
        output.WriteLine(text.Rare(builds.Length));
        foreach ((string talent, double share) in rare)
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"- {talent} : {share:0.0} %"));
        if (rare.Count == 0)
            output.WriteLine(text.NoneRare);
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

    private sealed record Options(string? Data, string Lang, string Scenario, ulong Seed, int Simulate, string? Record, string? Replay, bool Show, string? Class = null, string? Rival = null, int Level = 1, string Kit = "none", IReadOnlyList<string>? Allies = null, bool Parties = false, bool LevelSet = false, bool Tree = false)
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
                    _ => throw new ArgumentException($"Unknown option {args[i]}."),
                };
            }
            int modes = (o.Simulate > 0 ? 1 : 0) + (o.Record is null ? 0 : 1) + (o.Replay is null ? 0 : 1);
            if (o.Parties && (o.Simulate == 0 || o.Class is not null || o.Rival is not null || o.Party.Count > 0))
                throw new ArgumentException("--parties goes with --simulate N, without --class, --rival or --party.");
            if (o.Tree && (o.Simulate == 0 || o.Parties || o.Class is not null || o.Rival is not null || o.Party.Count > 0))
                throw new ArgumentException("--talents goes with --simulate N, without --parties, --class, --rival or --party.");
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
            ? $"# L'arbre des talents hors CI : {fights}, niveau {level}, équipement {kit}, {n} combats par classe et par paire, un build tiré au hasard par combat"
            : $"# The talent tree out of CI: {fights}, level {level}, kit {kit}, {n} fights a class and a pair, a build drawn at random each fight";

        public string TreeColumns => Fr ? "| Classe | Paire | Victoires | Intervalle à 95 % |" : "| Class | Pair | Wins | 95 % interval |";

        public string Rare(int builds) => Fr
            ? $"Talents pris par moins de 5 % des {builds} builds tirés (une fausse décision à revoir) :"
            : $"Talents taken by fewer than 5 % of the {builds} builds drawn (a false decision to look at):";

        public string NoneRare => Fr ? "- aucun" : "- none";

        public string Stats(Scenario s, int n, int[] wins, int draws, double rounds, double actions)
        {
            string r = rounds.ToString("0.0", CultureInfo.InvariantCulture), a = actions.ToString("0.0", CultureInfo.InvariantCulture);
            return Fr
                ? $"{s.Name.Fr}, {n} combats : équipe A {wins[0]}, équipe B {wins[1]}, nuls {draws} ; {r} tours et {a} actions en moyenne."
                : $"{s.Name.En}, {n} fights: team A {wins[0]}, team B {wins[1]}, draws {draws}; {r} rounds and {a} actions on average.";
        }
    }
}
