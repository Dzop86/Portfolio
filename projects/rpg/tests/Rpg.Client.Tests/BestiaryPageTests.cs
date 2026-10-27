using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The bestiary as the game shows it (sprint 154, D83): silhouettes, then what each step of victories tells.</summary>
public class BestiaryPageTests
{
    private static readonly GameData Data = GameData.Embedded;

    [Fact]
    public void EveryMonster_HasItsCard_TheFamiliesInTheOrderOfTheWorld()
    {
        // The game designer's criterion: each monster of monsters.json has its sheet.
        IReadOnlyList<BestiaryFamily> page = BestiaryPage.Families(Data, new Dictionary<string, int>(), new Texts("fr"));
        Assert.Equal(Data.Monsters.Keys.Order(StringComparer.Ordinal), page.SelectMany(f => f.Monsters).Select(m => m.Id).Order(StringComparer.Ordinal));
        Assert.Equal("boar", page[0].Id);
        Assert.All(page.SelectMany(f => f.Monsters), m => Assert.Equal(("???", Knowledge.Unknown), (m.Name, m.Known)));
    }

    [Fact]
    public void EachStep_TellsMore_InBothLanguages()
    {
        MonsterFamily boars = Data.Families["boar"];
        MonsterSpec charger = Data.Monsters["boar-charger"];
        BestiaryMonster met = BestiaryPage.Monster(Data, boars, charger, new Dictionary<string, int> { ["boar-charger"] = 0 }, new Texts("fr"));
        Assert.Equal("Sanglier chargeur", met.Name);
        Assert.StartsWith("Mêlée, niveau 2, ", met.Lines[0], StringComparison.Ordinal);
        Assert.DoesNotContain(met.Lines, l => l.StartsWith("Points de vie", StringComparison.Ordinal));
        Assert.Contains("Encore 1 victoire(s) pour en savoir plus.", met.Lines);
        BestiaryMonster one = BestiaryPage.Monster(Data, boars, charger, new Dictionary<string, int> { ["boar-charger"] = 1 }, new Texts("fr"));
        Assert.Contains($"Points de vie : {charger.Hp}", one.Lines);
        Assert.Contains("Faiblesse : Air", one.Lines);
        Assert.DoesNotContain(one.Lines, l => l.StartsWith("Sorts", StringComparison.Ordinal));
        BestiaryMonster ten = BestiaryPage.Monster(Data, boars, charger, new Dictionary<string, int> { ["boar-charger"] = 10 }, new Texts("en"));
        Assert.Contains(ten.Lines, l => l.StartsWith("Spells: ", StringComparison.Ordinal));
        Assert.Contains(ten.Lines, l => l.StartsWith("The family's lesson, ", StringComparison.Ordinal));
        Assert.DoesNotContain(ten.Lines, l => l.StartsWith("Loot", StringComparison.Ordinal));
        BestiaryMonster all = BestiaryPage.Monster(Data, boars, charger, new Dictionary<string, int> { ["boar-charger"] = 25 }, new Texts("en"));
        // Words for the rates, never the rates (question 80 a): the tusk at 40 % is common, the key at 2 % very rare.
        string loot = all.Lines.Single(l => l.StartsWith("Loot: ", StringComparison.Ordinal));
        Assert.Contains($"{Data.Items["boar-tusk"].Name.En} (common)", loot, StringComparison.Ordinal);
        Assert.Contains("(very rare)", loot, StringComparison.Ordinal);
        Assert.DoesNotContain("%", loot, StringComparison.Ordinal);
    }
}
