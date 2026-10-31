using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The bestiary's search and filters (sprint 175, D87, B3): what the player may read is found, each filter alone and together, in the order of the world.</summary>
public class BestiaryQueryTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static IReadOnlyList<BestiaryHit> Find(BestiaryFilter filter, Dictionary<string, int>? log = null, string lang = "fr", IReadOnlySet<string>? quests = null)
    {
        var texts = new Texts(lang);
        log ??= Bestiary.Full(Data);
        return BestiaryQuery.Find(Data, BestiaryPage.Families(Data, log, texts, quests), filter, texts, quests);
    }

    private static string[] Ids(IEnumerable<BestiaryHit> hits) => [.. hits.Select(h => h.Monster.Id)];

    [Fact]
    public void Croc_FindsTheMonstersThatLeaveAFang_AQuestsOnlyWithTheQuest()
    {
        IReadOnlyList<BestiaryHit> fangs = Find(new BestiaryFilter("croc"));
        Assert.Equal(["grey-wolf", "howling-wolf", "great-wolf", "pack-leader"], Ids(fangs));
        Assert.All(fangs, h => Assert.Contains("wolf-fang", h.Items));
        // The marked fang falls for "restless-beasts" only: with it, the she-wolf leaves one too.
        Assert.Contains("she-wolf", Ids(Find(new BestiaryFilter("croc"), quests: new HashSet<string> { "restless-beasts" })));
        // In English, without accents nor case: the boars' tusks.
        Assert.Equal(["boar-charger", "old-loner", "boar-king"], Ids(Find(new BestiaryFilter("TUSK"), lang: "en")));
        Assert.Equal(Ids(Find(new BestiaryFilter("Défense"))), Ids(Find(new BestiaryFilter("defense"))));
    }

    [Fact]
    public void TheWords_LookInWhatThePlayerMayRead()
    {
        // Its name once met, its family's always, every word somewhere.
        Assert.Empty(Find(new BestiaryFilter("solitaire"), new Dictionary<string, int>()));
        Assert.Equal(["old-loner"], Ids(Find(new BestiaryFilter("solitaire"), new Dictionary<string, int> { ["old-loner"] = 0 })));
        Assert.Equal(5, Find(new BestiaryFilter("loups"), new Dictionary<string, int>()).Count);
        Assert.Equal(["pack-leader"], Ids(Find(new BestiaryFilter("chef meute"))));
        Assert.Empty(Find(new BestiaryFilter("croc licorne")));
        // An item's name finds it before its step shows the loot: the item's card already says who leaves it.
        Assert.Equal(4, Find(new BestiaryFilter("croc"), new Dictionary<string, int>()).Count);
    }

    [Fact]
    public void EachFilter_AloneAndTogether()
    {
        Assert.Equal(Data.Monsters.Count, Find(new BestiaryFilter()).Count);
        IReadOnlyList<BestiaryHit> lake = Find(new BestiaryFilter(Zone: "lake"));
        Assert.Equal(10, lake.Count);
        Assert.All(lake, h => Assert.True(h.Family.Id is "frog" or "crab", h.Family.Id));
        Assert.All(Find(new BestiaryFilter(MinLevel: 16, MaxLevel: 18)), h => Assert.InRange(h.Monster.Level, 16, 18));
        Assert.Equal(8, Find(new BestiaryFilter(MinLevel: 16, MaxLevel: 18)).Count);
        Assert.Equal(Data.Families.Count, Find(new BestiaryFilter(Role: MonsterRole.Boss)).Count);
        Assert.Equal(["boar-king", "pack-leader"], Ids(Find(new BestiaryFilter(Zone: "woods", Role: MonsterRole.Boss))));
        Assert.Equal(["boar-king"], Ids(Find(new BestiaryFilter("defense", "woods", 5, 0, MonsterRole.Boss))));
    }

    [Fact]
    public void TheStepFilter_KeepsWhatIsLeftToFind_ToStudy_OrToMaster()
    {
        var log = new Dictionary<string, int> { ["boar-charger"] = 0, ["grey-wolf"] = 1, ["she-wolf"] = 5, ["boar-king"] = 5 };
        Assert.Equal(Data.Monsters.Count - 4, Find(new BestiaryFilter(Step: BestiaryStepFilter.ToFind), log).Count);
        Assert.Equal(["boar-charger"], Ids(Find(new BestiaryFilter(Step: BestiaryStepFilter.ToStudy), log)));
        // The chief is mastered at five: it is left out.
        Assert.Equal(["grey-wolf", "she-wolf"], Ids(Find(new BestiaryFilter(Step: BestiaryStepFilter.ToMaster), log)));
    }

    [Fact]
    public void TheList_KeepsTheOrderOfTheWorld_AndTheZonesTheirs()
    {
        IReadOnlyList<BestiaryFamily> page = BestiaryPage.Families(Data, Bestiary.Full(Data), new Texts("fr"));
        Assert.Equal(page.SelectMany(f => f.Monsters).Select(m => m.Id), Ids(Find(new BestiaryFilter())));
        Assert.Equal(["woods", "mouldy", "lake", "fields", "ancestors"], BestiaryQuery.Zones(Data, new Texts("fr")).Select(z => z.Id));
        Assert.Equal("Lac des Joncs", BestiaryQuery.Zones(Data, new Texts("fr"))[2].Name);
    }

    [Fact]
    public void AFilter_IsEmpty_OnlyWhenItAsksNothing()
    {
        Assert.True(new BestiaryFilter().IsEmpty);
        Assert.True(new BestiaryFilter("   ").IsEmpty);
        Assert.False(new BestiaryFilter("croc").IsEmpty);
        Assert.False(new BestiaryFilter(Role: MonsterRole.Melee).IsEmpty);
        Assert.False(new BestiaryFilter(Step: BestiaryStepFilter.ToFind).IsEmpty);
        Assert.Equal("l arbre a eclats", BestiaryQuery.Fold("L'Arbre-à-Éclats"));
    }
}
