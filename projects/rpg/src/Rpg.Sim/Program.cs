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
/// --class ID (the hero of team A plays that class of data/classes.json).
/// Exit code 0 on success, 1 for a record that does not replay, 2 for wrong arguments.
/// </summary>
public static class Program
{
    public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

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
            if (!data.Scenarios.ContainsKey(options.Scenario))
            {
                error.WriteLine($"Unknown scenario '{options.Scenario}'.");
                return 2;
            }
            Hero? hero = null;
            if (options.Class is string classId)
            {
                if (data.Class(classId) is not HeroClass c)
                {
                    error.WriteLine($"Unknown class '{classId}'.");
                    return 2;
                }
                hero = new Hero(c.Name.In(options.Lang), "female-d", c.Id);
            }
            if (options.Record is string recordPath)
            {
                var fight = new Fight(data, options.Scenario, options.Seed, hero);
                Ai.PlayOut(fight);
                File.WriteAllText(recordPath, FightRecord.Of(fight).ToJson());
                output.WriteLine(text.Summary(fight));
                return 0;
            }
            Simulate(data, options, hero, text, output);
            return 0;
        }
        catch (InvalidFightRecordException e)
        {
            error.WriteLine(e.Message);
            return 1;
        }
    }

    private static void Simulate(GameData data, Options options, Hero? hero, Texts text, TextWriter output)
    {
        int[] wins = new int[2];
        int draws = 0;
        long rounds = 0, actions = 0;
        for (int i = 0; i < options.Simulate; i++)
        {
            var fight = new Fight(data, options.Scenario, options.Seed + (ulong)i, hero);
            actions += Ai.PlayOut(fight);
            rounds += fight.Round;
            if (fight.WinningTeam is int team)
                wins[team]++;
            else
                draws++;
        }
        output.WriteLine(text.Stats(data.Scenarios[options.Scenario], options.Simulate, wins, draws,
            (double)rounds / options.Simulate, (double)actions / options.Simulate));
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
            sb.Append(CultureInfo.InvariantCulture, $"{(char)((f.Team == 0 ? 'a' : 'A') + f.Id)} {f.Name.In(lang)} {f.Hp}/{f.Spec.Hp}\n");
        return sb.ToString();
    }

    private sealed record Options(string? Data, string Lang, string Scenario, ulong Seed, int Simulate, string? Record, string? Replay, bool Show, string? Class = null)
    {
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
                    _ => throw new ArgumentException($"Unknown option {args[i]}."),
                };
            }
            int modes = (o.Simulate > 0 ? 1 : 0) + (o.Record is null ? 0 : 1) + (o.Replay is null ? 0 : 1);
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

        public string Stats(Scenario s, int n, int[] wins, int draws, double rounds, double actions)
        {
            string r = rounds.ToString("0.0", CultureInfo.InvariantCulture), a = actions.ToString("0.0", CultureInfo.InvariantCulture);
            return Fr
                ? $"{s.Name.Fr}, {n} combats : équipe A {wins[0]}, équipe B {wins[1]}, nuls {draws} ; {r} tours et {a} actions en moyenne."
                : $"{s.Name.En}, {n} fights: team A {wins[0]}, team B {wins[1]}, draws {draws}; {r} rounds and {a} actions on average.";
        }
    }
}
