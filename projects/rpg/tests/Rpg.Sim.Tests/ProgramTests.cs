using System.Text.Json;
using Rpg.Core;

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
    public void ARival_PlaysTeamB_AtTheLevelAsked_AndTheRecordKeepsIt()
    {
        string file = Path.Combine(Path.GetTempPath(), $"rpg-{Guid.NewGuid():N}.json");
        try
        {
            (int code, _, _) = Run("--record", file, "--scenario", "duel", "--class", "warrior", "--rival", "mage", "--level", "50");
            Assert.Equal(0, code);
            string json = File.ReadAllText(file);
            Assert.Contains("\"class\":\"warrior\",", json, StringComparison.Ordinal);
            Assert.Contains("\"rival\":{\"name\":\"Mage\",\"look\":\"male-d\",\"class\":\"mage\"", json, StringComparison.Ordinal);
            Assert.Contains("\"level\":50", json, StringComparison.Ordinal);
            Assert.Equal(0, Run("--replay", file).Code);
        }
        finally
        {
            File.Delete(file);
        }
        (int unknown, _, string error) = Run("--simulate", "5", "--rival", "dragon");
        Assert.Equal((2, "Unknown class 'dragon'."), (unknown, error.Trim()));
        Assert.Equal(2, Run("--simulate", "5", "--class", "mage", "--level", "101").Code);
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
    [InlineData("--talents", "--simulate", "1", "--class", "mage")]
    [InlineData("--talents", "--simulate", "1", "--scenario", "zone:nowhere")]
    [InlineData("--talents", "--simulate", "1", "--no-braid")]
    [InlineData("--simulate", "1", "--trio")]
    public void WrongArguments_ExitWith2(params string[] args) => Assert.Equal(2, Run(args).Code);

    [Fact]
    public void TheTree_IsMeasured_ForEachClassAndCompound_WithTheRareTalents()
    {
        // Sprint 151 (D82, J5 b, J6 a), sprint 182 (D93): each class without a compound and with each pair and trio (sprints 184 and 185), one fight each here.
        (int code, string output, _) = Run("--talents", "--simulate", "1", "--scenario", "zone:ancestors", "--lang", "fr");
        Assert.Equal(0, code);
        Assert.StartsWith("# L'arbre des talents hors CI : zone:ancestors, niveau 45", output, StringComparison.Ordinal);
        // Since sprint 152 (D83): five bare heroes a class beside the 6, mixed and of each element.
        Assert.Contains("| mage | Lightning |", output, StringComparison.Ordinal);
        Assert.Equal(6 + 15 + 27, output.Split('\n').Count(l => l.StartsWith("| archer |", StringComparison.Ordinal) || l.StartsWith("| warrior |", StringComparison.Ordinal) || l.StartsWith("| mage |", StringComparison.Ordinal)));
        Assert.Contains("| mage | nu, Feu seul |", output, StringComparison.Ordinal);
        // One build a class and a compound or none (sprint 206: whatever the number of classes).
        Assert.Contains($"Talents pris par moins de 20 % des builds qui visent leur branche, sur {11 * Rpg.Core.GameData.Embedded.Classes.Count} builds tirés", output, StringComparison.Ordinal);
    }

    [Fact]
    public void TheTrios_AreMeasured_ForEachCompound()
    {
        // Sprint 152 (D83, §12.3 step 4), sprint 182 (D93): a trio without a compound, and one whose first hero has Lightning.
        (int code, string output, _) = Run("--talents", "--trio", "--simulate", "1", "--scenario", "zone:ancestors");
        Assert.Equal(0, code);
        Assert.Contains("## Trios: three heroes of three classes", output, StringComparison.Ordinal);
        Assert.Equal(2, output.Split('\n').Count(l => l.StartsWith("| none |", StringComparison.Ordinal) || l.StartsWith("| Lightning |", StringComparison.Ordinal)));
    }

    [Fact]
    public void AKit_DressesTheHero_FromTheSetsOfTheMonstersZone()
    {
        // Sprint 95 (D71): "--kit fast" for the ceilings of T41, "none" by default.
        string group = Rpg.Core.GameData.Embedded.Groups.Values.First(g => g.Family == "wolf" && g.Fight is null).Id;
        string file = Path.Combine(Path.GetTempPath(), $"rpg-{Guid.NewGuid():N}.json");
        try
        {
            (int code, _, _) = Run("--record", file, "--scenario", "group:" + group, "--class", "warrior", "--level", "6", "--kit", "fast");
            Assert.Equal(0, code);
            Assert.Contains("wolf-boots", File.ReadAllText(file), StringComparison.Ordinal);
            (code, _, _) = Run("--record", file, "--scenario", "group:" + group, "--class", "warrior", "--level", "6");
            Assert.Equal(0, code);
            Assert.DoesNotContain("wolf-boots", File.ReadAllText(file), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(file);
        }
        Assert.Equal(2, Run("--simulate", "5", "--kit", "heavy").Code);
    }

    [Fact]
    public void AParty_FightsBesideTheHero_AndItsRecordKeepsIt()
    {
        // Sprint 121 (D74): up to three allies, played by the AI, at the hero's level with its kit.
        string file = Path.Combine(Path.GetTempPath(), $"rpg-{Guid.NewGuid():N}.json");
        try
        {
            (int code, _, _) = Run("--record", file, "--scenario", "training", "--class", "warrior", "--party", "mage,archer");
            Assert.Equal(0, code);
            string json = File.ReadAllText(file);
            Assert.Contains($"\"version\":{Rpg.Core.FightRecord.CurrentVersion}", json, StringComparison.Ordinal);
            Assert.Contains("\"allies\":[{\"name\":\"Brume\"", json, StringComparison.Ordinal);
            Assert.Equal(0, Run("--replay", file).Code);
        }
        finally
        {
            File.Delete(file);
        }
        Assert.Equal(2, Run("--simulate", "3", "--party", "mage").Code);
        Assert.Equal(2, Run("--simulate", "3", "--class", "warrior", "--party", "mage,mage,mage,mage").Code);
        Assert.Equal(2, Run("--simulate", "3", "--class", "warrior", "--party", "dragon").Code);
    }

    [Fact]
    public void ADungeonsLastRoom_IsWonByThreeOrFour_AtTheZonesLevel()
    {
        // The balance with the party (D74, §6.4): a dungeon's third room won by three heroes at least 70 % of the time,
        // by four at least 90 %; mixed classes, the zone's kit, the zone's level.
        Rpg.Core.GameData data = Rpg.Core.GameData.Embedded;
        Rpg.Core.Dungeon wallow = data.Dungeons["boar-wallow"];
        string level = data.Towns.Values.Where(t => t.Zone == data.Families[wallow.Family].Zone).Min(t => t.Level).ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach ((string party, int least) in new[] { ("mage,archer", 7), ("mage,archer,warrior", 9) })
        {
            (int code, string output, _) = Run("--simulate", "10", "--scenario", wallow.Scenario(3), "--class", "warrior", "--party", party, "--level", level, "--kit", "zone");
            Assert.Equal(0, code);
            int won = int.Parse(System.Text.RegularExpressions.Regex.Match(output, @"team A (\d+)").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(won >= least, $"{party}: {output}");
        }
    }

    [Fact]
    public void ThePartiesOfEachSize_AreTheClassesCombinations_HeroFirst()
    {
        // Sprint 122: 6 parties of two, 10 of three and 15 of four with three classes, none twice.
        string[] classes = ["warrior", "mage", "archer"];
        foreach ((int size, int count) in new[] { (2, 6), (3, 10), (4, 15) })
        {
            IReadOnlyList<string>[] parties = [.. Program.Parties(classes, size)];
            Assert.Equal(count, parties.Length);
            Assert.All(parties, p => Assert.Equal(size, p.Count));
            Assert.Equal(count, parties.Select(p => string.Join('+', p.Order(StringComparer.Ordinal))).Distinct().Count());
        }
        Assert.Equal(["warrior", "warrior"], Program.Parties(classes, 2).First());
    }

    [Theory]
    [InlineData(50, 100, 40.4, 59.6)]
    [InlineData(0, 20, 0, 16.1)]
    [InlineData(20, 20, 83.9, 100)]
    public void TheWilsonInterval_FramesTheRate(int wins, int n, double low, double high)
    {
        (double l, double h) = Program.Wilson(wins, n);
        Assert.Equal(low, l, 1);
        Assert.Equal(high, h, 1);
    }

    [Fact]
    public void Parties_PlayEveryPartyAtTheMonstersLevel_TheSameWayTwice()
    {
        // The queen's group of the Fields (T89), two fights per party: a table per size, then the same tables again.
        string[] args = ["--simulate", "2", "--scenario", "group:fields-0-4.0", "--parties", "--seed", "5"];
        (int code, string output, _) = Run(args);
        Assert.Equal(0, code);
        Assert.Matches(@"level 30, kit none, 2 fights per party", output);
        foreach (string size in new[] { "2 heroes", "3 heroes", "4 heroes" })
            Assert.Contains(size, output, StringComparison.Ordinal);
        Assert.Matches(@"archer\+warrior\+mage\+mage +\d/2 ", output);
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Count(output, @"  all "));
        Assert.Equal(output, Run(args).Out);
        Assert.Contains("niveau 12", Run([.. args, "--level", "12", "--lang", "fr"]).Out, StringComparison.Ordinal);
        Assert.Equal(2, Run("--simulate", "2", "--parties", "--class", "warrior").Code);
        Assert.Equal(2, Run("--record", "x.json", "--parties").Code);
    }

    [Fact]
    public void TheMatrix_IsATableOfAClasssPaths_WithWhatItsSpellsWereWorth()
    {
        (int code, string output, _) = Run("--matrix", "--simulate", "1", "--class", "warrior", "--level", "35");
        Assert.Equal(0, code);
        Assert.Contains("| warrior | 35 | (none) |", output, StringComparison.Ordinal);
        Assert.Equal(4, output.Split('\n').Count(l => l.StartsWith("| warrior | 35 |", StringComparison.Ordinal)));
        Assert.Contains("onclusive", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheMatrix_DoesNotCallConclusive_APathWithoutASpell()
    {
        // No path is without its spells since sprint 200: the reading itself, on rows made here.
        Assert.Equal("no spell yet", Program.Reads(new BenchRow("illusionist", 41.5, new Dictionary<string, double>())));
        Assert.Equal("conclusive", Program.Reads(new BenchRow("arcanist", 58.5, new Dictionary<string, double> { ["path-comet"] = 1.2 })));
        Assert.Equal("NOT CONCLUSIVE: path-rune 0.0", Program.Reads(new BenchRow("rune-weaver", 63.3, new Dictionary<string, double> { ["path-rune"] = 0 })));
        (int code, string output, _) = Run("--matrix", "--simulate", "1", "--class", "mage", "--level", "35");
        Assert.Equal(0, code);
        Assert.DoesNotContain("no spell yet", output, StringComparison.Ordinal);
    }

    [Fact]
    public void TheReferenceDecks_NameOnlyTheirClasssSpells_AndTheMatrixPlaysThem()
    {
        // The bench drops what a level has not unlocked: a misspelt spell would vanish without a word, so the file is checked here.
        string root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "Rpg.slnx")))
            root = Directory.GetParent(root)!.FullName;
        string file = Path.Combine(root, "tools", "bench-decks.json");
        Dictionary<string, string[]> decks = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(file))!;
        foreach (HeroClass cls in GameData.Embedded.Classes)
            Assert.All(decks[cls.Id], spell => Assert.Contains(spell, cls.Spells));
        (int code, string output, _) = Run("--matrix", "--simulate", "1", "--class", "warrior", "--level", "35", "--decks", file);
        Assert.Equal(0, code);
        Assert.Contains("| warrior | 35 | (none) |", output, StringComparison.Ordinal);
    }

    [Fact]
    public void TheMatrix_RanksThePathSpells_UnlessAskedNotTo_AndOnlyTheMatrix()
    {
        // Sprint 210 (D98): each path's hero spends its spell points knowing its path; --unranked-paths, the bench of sprints 193 to 209.
        (int code, string output, _) = Run("--matrix", "--simulate", "1", "--class", "warrior", "--level", "35");
        Assert.Equal(0, code);
        Assert.Contains("path spells ranked", output, StringComparison.Ordinal);
        (code, output, _) = Run("--matrix", "--simulate", "1", "--class", "warrior", "--level", "35", "--unranked-paths");
        Assert.Equal(0, code);
        Assert.Contains("path spells at rank 1", output, StringComparison.Ordinal);
        Assert.Contains("| warrior | 35 | palisade |", output, StringComparison.Ordinal);
        Assert.Equal(2, Run("--simulate", "1", "--unranked-paths").Code);
    }

    [Fact]
    public void TheMatrix_RefusesAnUnknownClass_AndGoesWithSimulate()
    {
        Assert.Equal(2, Run("--matrix", "--simulate", "1", "--class", "dragon").Code);
        Assert.Equal(2, Run("--matrix").Code);
        Assert.Equal(2, Run("--simulate", "1", "--decks", "x.json").Code);
    }
    [Fact]
    public void TheMatrix_CountsEveryPathSpellsCasts_WhenAsked()
    {
        // Sprint 218 (docs/classes-chaman.md §4): every path spell's casts a fight, not only those under the bench's least.
        (int code, string output, _) = Run("--matrix", "--simulate", "1", "--class", "warrior", "--level", "35", "--casts");
        Assert.Equal(0, code);
        Assert.DoesNotContain("onclusive", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path-", output, StringComparison.Ordinal);
        Assert.Equal(2, Run("--simulate", "1", "--casts").Code);
    }

    [Fact]
    public void TheDurations_TellEachClasssRounds_AloneAndThreeTogether()
    {
        // Sprint 219 (docs/jalon-3.md §3): the bounds B15 and B16, how long a fight lasts and what a round costs the rules and the AI.
        (int code, string output, _) = Run("--durations", "--simulate", "1", "--level", "35");
        Assert.Equal(0, code);
        Assert.Equal(GameData.Embedded.Classes.Count, output.Split('\n').Count(l => l.StartsWith("| 3 × ", StringComparison.Ordinal)));
        Assert.Contains("| priest + paladin |", output, StringComparison.Ordinal);
        Assert.Contains("| summoner | 4 and 5 |", output, StringComparison.Ordinal);
        Assert.Equal(2, Run("--durations", "--simulate", "1", "--matrix").Code);
        Assert.Equal(2, Run("--durations").Code);
    }
}
