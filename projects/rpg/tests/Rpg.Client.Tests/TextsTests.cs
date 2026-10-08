using System.Text.RegularExpressions;
using Rpg.Core;

namespace Rpg.Client.Tests;

public partial class TextsTests
{
    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    [Fact]
    public void BothLanguages_HaveTheSameKeysAndPlaceholders()
    {
        Assert.Equal(Texts.Fr.Keys.Order(StringComparer.Ordinal), Texts.En.Keys.Order(StringComparer.Ordinal));
        foreach (string key in Texts.Fr.Keys)
        {
            Assert.Equal(Placeholder().Matches(Texts.Fr[key]).Select(m => m.Value).Order(StringComparer.Ordinal),
                Placeholder().Matches(Texts.En[key]).Select(m => m.Value).Order(StringComparer.Ordinal));
        }
    }

    [Fact]
    public void EveryErrorAPlayerCanMeet_HasAMessage()
    {
        foreach (ActionError e in Enum.GetValues<ActionError>().Where(e => e != ActionError.None))
        {
            Assert.False(string.IsNullOrWhiteSpace(new Texts("fr").Error(e)), e.ToString());
            Assert.False(string.IsNullOrWhiteSpace(new Texts("en").Error(e)), e.ToString());
        }
    }

    [Fact]
    public void TheLog_TellsTheFightInThePlayersLanguage()
    {
        var fight = new Fight(GameData.Embedded, "training", 7);
        Ai.PlayOut(fight);
        var fr = new Texts("fr");
        string[] lines = [.. fight.Events.Select(e => fr.Describe(e, fight)).OfType<string>()];
        Assert.Contains(lines, l => l.StartsWith("Héroïne lance ", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.EndsWith(" est vaincu(e).", StringComparison.Ordinal));
        Assert.Equal("Round 3", new Texts("en")["round", 3]);
        Assert.Equal("Flèche (4 PA, portée 2-6)", fr.Spell(GameData.Embedded.Spells["arrow"]));
        Assert.Equal("en", new Texts("de").Lang);
    }
}
