using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The notice board as the game shows it (sprint 153, D83): the screenwriter's notices filled in, in both languages.</summary>
public class ContractsPageTests
{
    private static readonly GameData Data = GameData.Embedded;

    [Fact]
    public void EachNotice_IsWrittenFromItsModel_ItsTargetNamed_InBothLanguages()
    {
        var board = new ContractBoard(1, 0, [new Contract(ContractKind.Hunt, "boar", 6, 5, 2), new Contract(ContractKind.Boss, "wolf-den", 1, 5)]);
        IReadOnlyList<ContractLine> fr = ContractsPage.Lines(board, 5, [], new Texts("fr"), Data);
        Assert.Equal("Chasse aux sangliers", fr[0].Title);
        Assert.StartsWith("Les sangliers s'enhardissent", fr[0].Text, StringComparison.Ordinal);
        Assert.Contains("Prime à qui en vaincra 6.", fr[0].Text, StringComparison.Ordinal);
        Assert.Equal(("2 / 6", "+125 XP, +25 Éclats", false, false), (fr[0].Progress, fr[0].Reward, fr[0].Group, fr[0].CanHandIn));
        Assert.True(fr[1].Group);
        Assert.Equal("+250 XP, +50 Éclats", fr[1].Reward);
        Assert.EndsWith($"Lieu : {Data.Dungeons["wolf-den"].Name.Fr}.", fr[1].Text, StringComparison.Ordinal);
        IReadOnlyList<ContractLine> en = ContractsPage.Lines(board, 5, [], new Texts("en"), Data);
        Assert.Equal("Hunt: boars", en[0].Title);
        Assert.StartsWith("The boars are growing bold", en[0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void AGathering_IsCountedInTheBag_AndTheChangeSaysItsPrice()
    {
        var gather = new Contract(ContractKind.Gather, "oak-log", 10, 20);
        var board = new ContractBoard(1, 0, [gather]);
        ContractLine line = Assert.Single(ContractsPage.Lines(board, 20, [new ItemCount("oak-log", 10)], new Texts("fr"), Data));
        Assert.True(line.CanHandIn);
        Assert.Equal("10 / 10", line.Progress);
        Assert.Equal("Changer cet avis (gratuit)", line.Change);
        Assert.Equal("Changer cet avis (100 Éclats)", ContractsPage.Lines(board with { Changes = 1 }, 20, [], new Texts("fr"), Data)[0].Change);
    }

    [Fact]
    public void EveryNotice_StaysUnder140Characters_WithTheRealNames()
    {
        // The screenwriter's ceiling, checked on every target the board can draw, in both languages.
        foreach (string lang in new[] { "fr", "en" })
        {
            var texts = new Texts(lang);
            Contract[] all = [.. Data.Families.Keys.SelectMany(f => new[] { new Contract(ContractKind.Hunt, f, 6, 30), new Contract(ContractKind.Horde, f, 1, 30) }),
                .. Data.Dungeons.Keys.SelectMany(d => new[] { new Contract(ContractKind.Dungeon, d, 1, 30), new Contract(ContractKind.Boss, d, 1, 30) })];
            foreach (ContractLine line in ContractsPage.Lines(new ContractBoard(1, 0, all), 30, [], texts, Data))
                Assert.True(line.Text.Length <= 170, $"{line.Text.Length}: {line.Text}");
        }
    }
}
