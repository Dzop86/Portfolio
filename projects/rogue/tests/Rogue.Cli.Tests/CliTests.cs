using Rogue.Client;
using Rogue.Core;

namespace Rogue.Cli.Tests;

public sealed class CliTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("rogue-cli-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static (int Code, string Out, string Err) Run(string keys, params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        int code = Cli.Run(args, new StringReader(keys), stdout, stderr, interactive: false);
        return (code, stdout.ToString(), stderr.ToString());
    }

    [Fact]
    public void Help_InBothLanguages()
    {
        Assert.Contains("--replay FICHIER", Run("", "--help").Out);
        Assert.Contains("--replay FILE", Run("", "--help", "--lang", "en").Out);
    }

    [Theory]
    [InlineData("--seed", "abc")]
    [InlineData("--seed", "-1")]
    [InlineData("--lang", "de")]
    [InlineData("--stats", "0")]
    [InlineData("--seed")]
    [InlineData("--colour")]
    public void BadOptions_AreReported(params string[] args)
    {
        (int code, string output, string error) = Run("", [.. args, "--lang", "en"]);
        Assert.Equal(2, code);
        Assert.Empty(output);
        Assert.StartsWith("Invalid option: ", error, StringComparison.Ordinal);
    }

    [Fact]
    public void APipedGame_IsPlayedSavedAndReplayed()
    {
        string save = Path.Combine(_dir, "run.json");
        (int code, string output, _) = Run("dd?s\nx", "--seed", "42", "--save", save);

        Assert.Equal(0, code);
        Assert.Contains("Étage 1/5", output, StringComparison.Ordinal);
        Assert.Contains("Graine 42", output, StringComparison.Ordinal);
        Assert.Contains("Partie abandonnée à l'étage 1.", output, StringComparison.Ordinal);
        // "?" and the line break are ignored; three moves were played.
        Assert.Equal("""{"format":"rogue-run","version":1,"seed":"42","actions":"ees"}""", File.ReadAllText(save).Trim());

        (int replayCode, string replay, _) = Run("", "--replay", save, "--lang", "en");
        Assert.Equal(0, replayCode);
        Assert.Equal("Valid run: unfinished, floor 1, 3 turns, 0 monsters killed, score 0.", replay.Trim());
    }

    [Fact]
    public void ImpossibleMoves_AreExplained_AndCostNoTurn()
    {
        // The player starts in the middle of a room: walk east until a wall stops them.
        string save = Path.Combine(_dir, "run.json");

        (_, string output, _) = Run(new string('l', 12) + "p", "--seed", "42", "--lang", "en", "--save", save);

        Assert.Contains("A wall blocks the way.", output, StringComparison.Ordinal);
        Assert.Contains("You have no potion.", output, StringComparison.Ordinal);
        string actions = RunRecord.Parse(File.ReadAllText(save)).Actions;
        Assert.InRange(actions.Length, 1, 11);
        Assert.Equal(new string('e', actions.Length), actions);
    }

    [Fact]
    public void TheAutopilotsRun_ReplaysToTheSameScore()
    {
        string save = Path.Combine(_dir, "bot.json");
        (int code, string output, _) = Run("", "--bot", "--seed", "7", "--lang", "en", "--save", save);
        Assert.Equal(0, code);
        string end = output.Split('\n').Single(l => l.StartsWith("You die", StringComparison.Ordinal) || l.StartsWith("Victory", StringComparison.Ordinal));
        string score = end[(end.LastIndexOf(' ') + 1)..].TrimEnd('.', '\r');

        (_, string replay, _) = Run("", "--replay", save, "--lang", "en");
        Assert.EndsWith($"score {score}.", replay.Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public void TamperedRuns_AreRejected_WithThePlaceOfTheFault()
    {
        string save = Path.Combine(_dir, "run.json");
        File.WriteAllText(save, """{"format":"rogue-run","version":1,"seed":"42","actions":"e>"}""");
        (int code, string output, _) = Run("", "--replay", save);
        Assert.Equal(1, code);
        Assert.Equal("Partie refusée : action impossible à ce moment (action n° 2).", output.Trim());

        File.WriteAllText(save, "{}");
        Assert.Equal("Run rejected: this is not a recorded run.", Run("", "--replay", save, "--lang", "en").Out.Trim());
    }

    [Fact]
    public void MissingFiles_AreReported()
    {
        (int code, _, string error) = Run("", "--replay", Path.Combine(_dir, "none.json"), "--lang", "en");
        Assert.Equal(2, code);
        Assert.StartsWith("Cannot read ", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Statistics_CountEveryRun()
    {
        (int code, string output, _) = Run("", "--stats", "20", "--lang", "en");
        Assert.Equal(0, code);
        Assert.StartsWith("Autopilot, 20 runs: ", output, StringComparison.Ordinal);
        string deaths = output.Split('\n')[1];
        int escaped = int.Parse(output.Split(": ")[1].Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
        int dead = deaths.Split(": ").Skip(2).Sum(part => int.Parse(part.Split(',', '.')[0], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(20, escaped + dead);
    }
}
