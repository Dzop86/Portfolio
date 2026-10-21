namespace Rpg.Core.Tests;

/// <summary>The chat's rules (sprint 84): lines cleaned and bounded, paced per speaker.</summary>
public class ChatTests
{
    [Fact]
    public void ALine_IsCleaned_AndCutAtItsLength()
    {
        Assert.Equal("Bonjour à tous !", ChatRules.Clean("  Bonjour \t à\ntous  ! "));
        Assert.Null(ChatRules.Clean("   \n\t "));
        Assert.Null(ChatRules.Clean(null));
        Assert.Equal(ChatRules.MaxLength, ChatRules.Clean(new string('a', 500))!.Length);
        Assert.Equal("a b", ChatRules.Clean("a\u0007b"));
    }

    [Fact]
    public void ASpeaker_SaysFiveLinesInTenSeconds_AtMost()
    {
        var pace = new ChatPace();
        var t = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < ChatRules.Burst; i++)
            Assert.True(pace.TryTake(t + TimeSpan.FromSeconds(i)));
        Assert.False(pace.TryTake(t + TimeSpan.FromSeconds(5)));
        // Ten seconds after the first, one more.
        Assert.True(pace.TryTake(t + ChatRules.Window));
        Assert.False(pace.TryTake(t + ChatRules.Window));
    }

    [Fact]
    public void AnItemsLink_IsCutOutOfTheLine_AndChecked()
    {
        // Sprint 117 (D77): the links shared, the text around them kept.
        string line = $"Vends {ChatLinks.Of("copper-ring")} et {ChatLinks.Of("bread")}";
        Assert.Equal([("Vends ", null), ("[item:copper-ring]", "copper-ring"), (" et ", null), ("[item:bread]", "bread")], ChatLinks.Pieces(line));
        Assert.Equal([("[item:Bad] [item:]", (string?)null)], ChatLinks.Pieces("[item:Bad] [item:]"));
        Assert.Empty(ChatLinks.Pieces(""));
        Assert.Null(ChatLinks.WhyNot(line, Arena.Real));
        Assert.Null(ChatLinks.WhyNot("rien à voir", Arena.Real));
        Assert.NotNull(ChatLinks.WhyNot(ChatLinks.Of("no-such-thing"), Arena.Real));
        Assert.NotNull(ChatLinks.WhyNot(string.Concat(Enumerable.Repeat(ChatLinks.Of("bread"), ChatLinks.Most + 1)), Arena.Real));
        Assert.Null(ChatLinks.WhyNot(string.Concat(Enumerable.Repeat(ChatLinks.Of("bread"), ChatLinks.Most)), Arena.Real));
    }
}
