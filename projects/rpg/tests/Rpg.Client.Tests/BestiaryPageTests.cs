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

    [Fact]
    public void TheDecksSpells_HitTheWeakness_OrMeetTheResistances_OnceTheMonsterIsStudied()
    {
        // The charging boar: Air −15 (its weakness), Earth +10; a spell that hurts nothing does not count.
        string heal = Data.Spells.Values.First(s => s.DamageMax == 0).Id;
        string[] deck = ["arrow", "sling", "strike", "spear", "fireball", heal, "unknown-spell"];
        Assert.Null(BestiaryPage.YourSpells(Data, Card(0), deck));
        BestiarySpells spells = BestiaryPage.YourSpells(Data, Card(1), deck)!;
        Assert.Equal(Element.Air, spells.Weakness);
        Assert.Equal(["arrow", "sling"], spells.Weak);
        Assert.Equal(["strike", "spear"], spells.Resisted);
        Assert.Equal(deck.Count(id => Data.Spells.TryGetValue(id, out Spell? s) && s.DamageMax > 0), spells.Hurting);
        // The same spell twice counts once; an empty deck hurts nothing.
        Assert.Single(BestiaryPage.YourSpells(Data, Card(1), ["arrow", "arrow"])!.Weak);
        Assert.Equal(0, BestiaryPage.YourSpells(Data, Card(1), [])!.Hurting);
    }

    [Fact]
    public void AMonstersStory_IsTold_OnceMastered_AndAFamilys_OnceItsFiveAreMet()
    {
        Assert.Null(Card(24).Story);
        Assert.Equal(Charger.Lore!.Fr, Card(25).Story);
        Assert.Equal(Charger.Lore.En, Card(25, "en").Story);
        BestiaryFamily Family(Dictionary<string, int> log) => BestiaryPage.Families(Data, log, new Texts("en")).First(f => f.Id == "boar");
        var four = Boars.Monsters.Skip(1).ToDictionary(m => m.Id, _ => 0);
        Assert.Null(Family(four).Story);
        four[Charger.Id] = 0;
        Assert.Equal(Boars.Lore!.En, Family(four).Story);
        Assert.False(Family(four).Hunted);
        Assert.True(Family(Bestiary.Full(Data)).Hunted);
    }

    [Fact]
    public void EveryFamilyAndMonster_HasItsStory_InBothLanguages_WithoutWhatTheCanonKeepsForLater()
    {
        string[] later = ["Accord", "Dhoram", "Névara", "Kaor", "Élyon", "Disjonction", "Disjunction", "cinquième", "fifth"];
        foreach (MonsterFamily f in Data.Families.Values)
        {
            Assert.True(f.Lore is { Fr.Length: > 40, En.Length: > 40 }, f.Id);
            Assert.All(f.Monsters, m => Assert.True(m.Lore is { Fr.Length: > 10 and <= 140, En.Length: > 10 and <= 140 }, m.Id));
            Assert.All(f.Monsters.Select(m => m.Lore!).Append(f.Lore!), l => Assert.DoesNotContain(later, w => l.Fr.Contains(w, StringComparison.Ordinal) || l.En.Contains(w, StringComparison.Ordinal)));
        }
    }

    [Fact]
    public void TheMapsAMonsterLivesOn_AreTheMapsOfItsGroups()
    {
        IReadOnlyList<string> maps = BestiaryPage.MapsOf(Data, Charger.Id);
        Assert.NotEmpty(maps);
        Assert.All(maps, id => Assert.Contains(Data.Groups.Values, g => g.Map == id && g.Monsters.Contains(Charger.Id)));
        Assert.Contains(maps, id => Data.Towns[id].Zone == Boars.Zone);
        Assert.Equal(Card(0).Maps!.Count, maps.Select(id => new Texts("fr").MapTitle(Data.Towns[id])).Distinct().Count());
        Assert.Empty(BestiaryPage.MapsOf(Data, "no-such-monster"));
    }
}
