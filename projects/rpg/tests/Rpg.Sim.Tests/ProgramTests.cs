namespace Rpg.Sim.Tests;

public class ProgramTests
{
    private static readonly string DataDir = Path.Combine(AppContext.BaseDirectory, "data");

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int code = Program.Run([.. args, "--data", DataDir], output, error);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public void Simulate_PrintsTheResultsOfEveryFight()
    {
        (int code, string output, _) = Run("--simulate", "20", "--scenario", "duel");
        Assert.Equal(0, code);
        Assert.Matches(@"^Duel, 20 fights: team A \d+, team B \d+, draws \d+; \d+\.\d rounds and \d+\.\d actions on average\.", output);
    }

    [Fact]
    public void AClass_PlaysTheHero_AndAnUnknownOneIsRefused()
    {
        string file = Path.Combine(Path.GetTempPath(), $"rpg-{Guid.NewGuid():N}.json");
        try
        {
            (int code, _, _) = Run("--record", file, "--scenario", "training", "--seed", "3", "--class", "mage", "--lang", "fr");
            Assert.Equal(0, code);
            Assert.Contains("\"hero\":{\"name\":\"Mage\",\"look\":\"female-d\",\"class\":\"mage\"", File.ReadAllText(file), StringComparison.Ordinal);
            (code, string shown, _) = Run("--replay", file, "--show", "--lang", "fr");
            Assert.Equal(0, code);
            Assert.Contains(" Mage ", shown, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(file);
        }
        (int unknown, _, string error) = Run("--simulate", "5", "--class", "dragon");
        Assert.Equal((2, "Unknown class 'dragon'."), (unknown, error.Trim()));
    }

    [Fact]
    public void ARecordedFight_ReplaysToTheSameSummary_InBothLanguages()
    {
        string file = Path.Combine(Path.GetTempPath(), $"rpg-{Guid.NewGuid():N}.json");
        try
        {
            (int code, string recorded, _) = Run("--record", file, "--scenario", "training", "--seed", "7");
            Assert.Equal(0, code);
            (code, string replayed, _) = Run("--replay", file, "--show");
            Assert.Equal(0, code);
            Assert.StartsWith(recorded.TrimEnd(), replayed, StringComparison.Ordinal);
            Assert.Contains("Hero", replayed, StringComparison.Ordinal);
            (_, string fr, _) = Run("--replay", file, "--lang", "fr");
            Assert.StartsWith("Entraînement, graine 7 : ", fr, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void ABrokenRecord_ExitsWith1()
    {
        string file = Path.Combine(Path.GetTempPath(), $"rpg-{Guid.NewGuid():N}.json");
        File.WriteAllText(file, """{"version":1,"scenario":"duel","seed":1,"actions":[{"type":"move","to":{"x":99,"y":0}}]}""");
        try
        {
            (int code, _, string error) = Run("--replay", file);
            Assert.Equal(1, code);
            Assert.Contains("OffBoard", error, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Theory]
    [InlineData]
    [InlineData("--simulate", "0")]
    [InlineData("--simulate", "3", "--replay", "x.json")]
    [InlineData("--lang", "de", "--simulate", "3")]
    [InlineData("--seed")]
    [InlineData("--fly")]
    [InlineData("--simulate", "3", "--scenario", "nowhere")]
    public void WrongArguments_ExitWith2(params string[] args) => Assert.Equal(2, Run(args).Code);
}
