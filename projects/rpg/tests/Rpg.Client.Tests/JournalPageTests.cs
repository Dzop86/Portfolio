using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The journal as in the games of the kind (sprint 116, D77): tabs under way, done and hidden, a search, filters of
/// level and kind, zones, and a card with the quest's words, objectives and rewards.
/// </summary>
public class JournalPageTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly HashSet<string> None = [];

    // Under way: Garance's edge of the woods (woods, 4), the old stone (village, 3) and Anselme's billhook (village, 3);
    // done: the welcome and the first lesson (village, 1).
    private static QuestBook Book() => new(Data, 5, ["welcome", "first-lesson"],
        [Quests.Start(Data.Quests["edge-of-the-woods"]), Quests.Start(Data.Quests["whispering-stone"]), Quests.Start(Data.Quests["first-billhook"])], []);

    private static string[] Ids(IReadOnlyList<JournalSection> page) => [.. page.SelectMany(s => s.Quests).Select(q => q.Id)];

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheTabs_HoldTheQuestsUnderWay_Done_AndHidden_ZoneByZone_TheLowestLevelsFirst(string lang)
    {
        var texts = new Texts(lang);
        IReadOnlyList<JournalZone> journal = Book().Journal(texts);
        var view = new JournalView();
        IReadOnlyList<JournalSection> page = view.Page(journal, None);
        Assert.Equal([texts["journal.village"], Data.Towns["woods-0-0"].Name.In(lang)], page.Select(s => s.Name));
        // The billhook and the stone are both of level 3: by their names.
        Assert.Equal(["first-billhook", "whispering-stone"], page[0].Quests.Select(q => q.Id).Order(StringComparer.Ordinal));
        Assert.Equal(["edge-of-the-woods"], page[1].Quests.Select(q => q.Id));
        view.Tab = JournalTab.Done;
        Assert.Equal(["welcome", "first-lesson"], Ids(view.Page(journal, None)).Order(StringComparer.Ordinal).Reverse());
        // Hidden: only under its own tab, under way or done.
        HashSet<string> hidden = ["whispering-stone", "welcome"];
        Assert.Equal(["first-lesson"], Ids(view.Page(journal, hidden)));
        view.Tab = JournalTab.Active;
        Assert.DoesNotContain("whispering-stone", Ids(view.Page(journal, hidden)));
        view.Tab = JournalTab.Hidden;
        Assert.Equal(["welcome", "whispering-stone"], Ids(view.Page(journal, hidden)));
        Assert.Equal(new Dictionary<JournalTab, int> { [JournalTab.Active] = 2, [JournalTab.Done] = 1, [JournalTab.Hidden] = 2 }, view.Counts(journal, hidden));
    }

    [Fact]
    public void TheSearch_FindsTheName_TheGiver_TheZone_OrAnObjective_WhateverTheCaseAndTheAccents()
    {
        var texts = new Texts("fr");
        IReadOnlyList<JournalZone> journal = Book().Journal(texts);
        var view = new JournalView { Search = "  " };
        Assert.Equal(3, Ids(view.Page(journal, None)).Length);
        // The giver, without her accent or capital: Garance gives the edge of the woods.
        view.Search = "garance";
        Assert.Equal(["edge-of-the-woods"], Ids(view.Page(journal, None)));
        // The quest's name in another case, its accents dropped.
        string stone = Data.Quests["whispering-stone"].Name.In("fr");
        view.Search = stone.ToUpperInvariant().Replace('É', 'E').Replace('È', 'E');
        Assert.Equal(["whispering-stone"], Ids(view.Page(journal, None)));
        // Every word, anywhere: the zone and an objective's monster.
        view.Search = Data.Towns["woods-0-0"].Name.In("fr").Split(' ')[^1] + " " + Data.Families["boar"].Name.In("fr");
        Assert.Equal(["edge-of-the-woods"], Ids(view.Page(journal, None)));
        view.Search = "zzzz";
        Assert.Empty(view.Page(journal, None));
    }

    [Fact]
    public void TheFilters_KeepTheLevels_AndTheKinds_Chosen()
    {
        var texts = new Texts("en");
        IReadOnlyList<JournalZone> journal = Book().Journal(texts);
        var view = new JournalView { MinLevel = 4 };
        Assert.Equal(["edge-of-the-woods"], Ids(view.Page(journal, None)));
        view.MinLevel = 1;
        view.MaxLevel = 3;
        Assert.Equal(2, Ids(view.Page(journal, None)).Length);
        view.MaxLevel = Hero.MaxLevel;
        view.Kinds.Clear();
        view.Kinds.Add(QuestKind.Trade);
        Assert.Equal(["first-billhook"], Ids(view.Page(journal, None)));
        view.Kinds.Add(QuestKind.Main);
        Assert.Equal(["first-billhook", "whispering-stone"], Ids(view.Page(journal, None)).Order(StringComparer.Ordinal));
        // The counts in the tabs' names stay those of the whole tab.
        Assert.Equal(3, view.Counts(journal, None)[JournalTab.Active]);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheCard_ShowsTheQuestChosen_ItsKind_ItsLevel_AndItsRewards(string lang)
    {
        var texts = new Texts(lang);
        IReadOnlyList<JournalZone> journal = Book().Journal(texts);
        var view = new JournalView { Selected = "edge-of-the-woods" };
        IReadOnlyList<JournalSection> page = view.Page(journal, None);
        JournalQuest card = view.Card(page)!;
        Quest edge = Data.Quests["edge-of-the-woods"];
        Assert.Equal((QuestKind.Tutorial, 4, Data.Towns["woods-0-0"].Name.In(lang)), (card.Kind, card.Level, card.Zone));
        Assert.Equal([texts["journal.xp", edge.Xp], texts["journal.shards", edge.Shards], texts["journal.item", Data.Items["bread"].Name.In(lang), 2]], card.Rewards!.Select(r => r.Text));
        // Typed for their tiles (sprint 167): the experience, the Éclats, then each item with its id and count.
        Assert.Equal([(RewardKind.Xp, edge.Xp, (string?)null), (RewardKind.Shards, edge.Shards, null), (RewardKind.Item, 2L, "bread")], card.Rewards!.Select(r => (r.Kind, r.Amount, r.Item)));
        Assert.Equal(QuestKind.Trade, journal.SelectMany(z => z.Active).Single(q => q.Id == "first-billhook").Kind);
        // The quest chosen gone from the page (a filter, or hidden): the page's first.
        view.Kinds.Remove(QuestKind.Tutorial);
        page = view.Page(journal, None);
        Assert.Equal(Ids(page)[0], view.Card(page)!.Id);
        Assert.Null(view.Card([]));
        string[] keys = ["journal.hidden", "journal.search", "journal.levels", "journal.nomatch", "journal.hide", "journal.show", "journal.rewards", "journal.objectives",
            .. Enum.GetValues<QuestKind>().Select(k => "questkind." + k)];
        foreach (string key in keys)
            Assert.NotEqual(key, texts[key]);
    }

    [Fact]
    public void TheHiddenQuests_AreKeptForEachCharacter()
    {
        Guid one = Guid.NewGuid(), two = Guid.NewGuid();
        Assert.Empty(HiddenQuests.Read(null, one));
        string kept = HiddenQuests.Write(null, one, ["welcome", "the-pack"]);
        kept = HiddenQuests.Write(kept, two, ["four-trails"]);
        Assert.Equal(["the-pack", "welcome"], HiddenQuests.Read(kept, one).Order(StringComparer.Ordinal));
        Assert.Equal(["four-trails"], HiddenQuests.Read(kept, two));
        // Shown again: gone from the character's lines, the other's untouched.
        kept = HiddenQuests.Write(kept, one, ["the-pack"]);
        Assert.Equal(["the-pack"], HiddenQuests.Read(kept, one));
        Assert.Equal(["four-trails"], HiddenQuests.Read(kept, two));
        Assert.Empty(HiddenQuests.Read("garbage\nmore of it here\n", one));
    }
}
