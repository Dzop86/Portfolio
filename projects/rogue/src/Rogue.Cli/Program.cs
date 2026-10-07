using System.Globalization;
using System.Text;
using Rogue.Cli;
using Rogue.Core;

Console.OutputEncoding = Encoding.UTF8;
return Cli.Run(args, Console.In, Console.Out, Console.Error, interactive: !Console.IsInputRedirected && !Console.IsOutputRedirected);

namespace Rogue.Cli
{
    internal static class Cli
    {
        public static int Run(string[] args, TextReader stdin, TextWriter stdout, TextWriter stderr, bool interactive)
        {
            Options options;
            try
            {
                options = Options.Parse(args);
            }
            catch (FormatException e)
            {
                var texts = new Texts(Options.GuessLanguage(args));
                stderr.WriteLine(texts.BadOption(e.Message));
                stderr.WriteLine(texts.Usage);
                return 2;
            }
            var t = new Texts(options.Language);
            if (options.Help)
            {
                stdout.WriteLine(t.Usage);
                return 0;
            }
            if (options.ReplayPath is string replayPath)
            {
                string json;
                try
                {
                    json = File.ReadAllText(replayPath);
                }
                catch (IOException e)
                {
                    stderr.WriteLine(t.CannotRead(replayPath, e.Message));
                    return 2;
                }
                ReplayResult result = Replay.Run(json);
                stdout.WriteLine(t.Replayed(result));
                return result.IsValid ? 0 : 1;
            }
            if (options.StatsRuns is int runs)
            {
                stdout.WriteLine(Statistics(t, runs));
                return 0;
            }

            var game = new Game(options.Seed ?? (ulong)Random.Shared.NextInt64(long.MinValue, long.MaxValue));
            IInput input = interactive ? new KeyboardInput() : new TextInput(stdin);
            var session = new Session(game, t, input, stdout, interactive);
            if (options.Bot)
                session.Watch();
            else
                session.Play();
            if (options.SavePath is string savePath)
            {
                File.WriteAllText(savePath, RunRecord.From(game).ToJson() + "\n");
                stdout.WriteLine(t.Saved(savePath));
            }
            return 0;
        }

        private static string Statistics(Texts texts, int runs)
        {
            var games = new Game[runs];
            Parallel.For(0, runs, i =>
            {
                var game = new Game((ulong)i);
                while (game.State == GameState.Playing)
                    game.Apply(Autopilot.Choose(game));
                games[i] = game;
            });
            int[] deaths = Enumerable.Range(1, Game.MaxDepth)
                .Select(d => games.Count(g => g.State == GameState.Dead && g.Depth == d)).ToArray();
            return texts.Stats(runs, games.Count(g => g.State == GameState.Escaped), games.Average(g => g.Score), games.Average(g => g.Turn), deaths);
        }
    }

    internal sealed record Options(ulong? Seed, Language Language, string? SavePath, string? ReplayPath, bool Bot, int? StatsRuns, bool Help)
    {
        /// <exception cref="FormatException">An unknown option or a bad value.</exception>
        public static Options Parse(string[] args)
        {
            var o = new Options(null, Language.French, null, null, false, null, false);
            for (int i = 0; i < args.Length; i++)
            {
                string Value() => i + 1 < args.Length ? args[++i] : throw new FormatException($"{args[i]} ?");
                o = args[i] switch
                {
                    "--seed" => o with
                    {
                        Seed = ulong.TryParse(Value(), NumberStyles.None, CultureInfo.InvariantCulture, out ulong s) ? s : throw new FormatException($"--seed {args[i]}"),
                    },
                    "--lang" => o with { Language = ParseLanguage(Value()) ?? throw new FormatException($"--lang {args[i]}") },
                    "--save" => o with { SavePath = Value() },
                    "--replay" => o with { ReplayPath = Value() },
                    "--bot" => o with { Bot = true },
                    "--stats" => o with
                    {
                        StatsRuns = int.TryParse(Value(), NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : throw new FormatException($"--stats {args[i]}"),
                    },
                    "--help" or "-h" => o with { Help = true },
                    _ => throw new FormatException(args[i]),
                };
            }
            return o;
        }

        /// <summary>The language to complain in when the options themselves are wrong.</summary>
        public static Language GuessLanguage(string[] args)
        {
            Language language = Language.French;
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "--lang" && ParseLanguage(args[i + 1]) is Language l)
                    language = l;
            return language;
        }

        private static Language? ParseLanguage(string value) => value switch
        {
            "fr" => Language.French,
            "en" => Language.English,
            _ => null,
        };
    }
}
