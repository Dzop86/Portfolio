using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The bestiary as the game shows it (sprint 154; by blocks since sprint 171, D87): what each step opens, nothing more.</summary>
public class BestiaryPageTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly MonsterFamily Boars = Data.Families["boar"];
    private static readonly MonsterSpec Charger = Data.Monsters["boar-charger"];

    private static BestiaryMonster Card(int wins, string lang = "fr", IReadOnlySet<string>? quests = null, string id = "boar-charger") =>
        BestiaryPage.Monster(Data, Data.Families.Values.First(f => f.Monsters.Any(m => m.Id == id)), Data.Monsters[id],
            wins < 0 ? new Dictionary<string, int>() : new Dictionary<string, int> { [id] = wins }, new Texts(lang), quests);

    [Fact]
    public void EveryMonster_HasItsCard_TheFamiliesInTheOrderOfTheWorld()
    {
        IReadOnlyList<BestiaryFamily> page = BestiaryPage.Families(Data, new Dictionary<string, int>(), new Texts("fr"));
        Assert.Equal(Data.Monsters.Keys.Order(StringComparer.Ordinal), page.SelectMany(f => f.Monsters).Select(m => m.Id).Order(StringComparer.Ordinal));
        Assert.Equal("boar", page[0].Id);
        Assert.All(page.SelectMany(f => f.Monsters), m => Assert.Equal(("???", Knowledge.Unknown), (m.Name, m.Known)));
        Assert.All(page, f => Assert.Equal(0, f.Met));
    }

    [Fact]
    public void AnUnknownMonster_ShowsWhereToGo_AndNothingOfItself()
    {
        BestiaryMonster unknown = Card(-1);
        Assert.Equal(("???", MonsterRole.Melee, Charger.Level), (unknown.Name, unknown.Role, unknown.Level));
        Assert.Equal(new Texts("fr").ZoneName(Boars.Zone, Data), unknown.Zone);
        Assert.Null(unknown.Numbers);
        Assert.Null(unknown.Maps);
        Assert.Null(unknown.Resistances);
        // One line a step not reached, each its own: no sentence repeated.
        Assert.All(unknown.Sections, s => Assert.False(s.Reached));
        Assert.Equal(unknown.Sections.Count, unknown.Sections.Select(s => s.Locked).Distinct().Count());
        Assert.Equal("Étudié à 1 victoire(s) : ses résistances et son butin", unknown.Sections[1].Locked);
    }

    [Fact]
    public void EachStep_OpensItsBlocks_AndOnlyThem()
    {
        BestiaryMonster met = Card(0);
        Assert.Equal("Sanglier chargeur", met.Name);
        Assert.Equal(new BestiaryNumbers(Charger.Hp, Charger.Ap, Charger.Mp, Charger.Initiative), met.Numbers);
        Assert.NotEmpty(met.Maps!);
        Assert.Null(met.Resistances);
        Assert.Equal((Knowledge.Studied, 1), met.Next);
        BestiaryMonster studied = Card(1);
        Assert.Equal(Element.Air, studied.Weakness);
        Assert.Equal(BestiaryPage.Elements, studied.Resistances!.Keys);
        Assert.Equal(-15, studied.Resistances[Element.Air]);
        Assert.NotEmpty(studied.Loot!);
        Assert.Null(studied.Spells);
        BestiaryMonster known = Card(5, "en");
        Assert.Equal(Charger.Spells.Count, known.Spells!.Count);
        Assert.Equal("Rushes at the nearest.", known.Behaviour);
        Assert.Equal((Knowledge.Mastered, 20), known.Next);
        Assert.True(Card(25).Sections.All(s => s.Reached));
    }

    [Fact]
    public void TheLoot_HasTheItemsCardsRate_AndAQuestsItemOnlyWithItsQuest()
    {
        BestiaryMonster studied = Card(1);
        var info = new ItemInfo(Data);
        Assert.All(studied.Loot!, l => Assert.Contains(info.Drops(l.Item), d => d.Monster.Id == Charger.Id && d.Percent == l.Percent));
        Assert.Equal(studied.Loot!.Select(l => l.Percent).OrderDescending(), studied.Loot!.Select(l => l.Percent));
        // A monster that leaves an item for a quest.
        MonsterSpec giver = Data.Monsters.Values.First(m => (m.Loot ?? []).Any(l => l.Quest is not null));
        string quest = giver.Loot!.First(l => l.Quest is not null).Quest!;
        Assert.DoesNotContain(Card(1, id: giver.Id).Loot!, l => l.Quest is not null);
        Assert.Contains(Card(1, quests: new HashSet<string> { quest }, id: giver.Id).Loot!, l => l.Quest == quest);
    }
}
