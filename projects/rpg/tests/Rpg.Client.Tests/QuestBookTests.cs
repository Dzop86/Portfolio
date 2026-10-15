using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The quests in the town and the journal (sprint 88): offered in a talk, taken, followed, handed back.</summary>
public class QuestBookTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly Quest Edge = Data.Quests["edge-of-the-woods"];

    private static TownController Village(QuestBook book) => new(Data, "clairval") { Book = book };

    [Fact]
    public void Aubin_OffersHisQuest_ItsWords_AndAcceptingTakesIt()
    {
        TownController town = Village(new QuestBook(Data, 1, [], [], []));
        Npc aubin = town.Town.Npcs.Single(n => n.Id == "aubin");
        Assert.NotNull(town.Click(aubin.At));
        Conversation talk = town.Talk!;
        DialogueLine first = talk.Line;
        Assert.Equal(Texts.Both("quest.offer", Edge.Name), first.Answers[0].Text);
        // The dialogue's own answers come after.
        Assert.Equal(Data.Dialogues[aubin.Dialogue].Lines[talk.Dialogue.Start].Answers, first.Answers.Skip(1));
        town.Answer(0);
        Assert.Equal(Edge.Text, town.Talk!.Line.Text);
        town.Answer(0);
        Assert.Null(town.Talk);
        Assert.Equal(("edge-of-the-woods", (string?)null), (town.QuestTaken, town.QuestHandedIn));
        town.QuestSeenTo();
        Assert.Null(town.QuestTaken);
    }

    [Fact]
    public void Later_TakesNothing_AndAQuestUnderWay_IsNotOfferedAgain_ButRoseIsCounted()
    {
        TownController town = Village(new QuestBook(Data, 1, [], [], []));
        Npc aubin = town.Town.Npcs.Single(n => n.Id == "aubin");
        town.Click(aubin.At);
        town.Answer(0);
        town.Answer(1);
        Assert.Null(town.QuestTaken);
        var book = new QuestBook(Data, 1, [], [Quests.Start(Edge)], []);
        Assert.Empty(book.Offers("clairval/aubin"));
        Assert.Empty(book.HandIns("clairval/aubin"));
        Assert.Equal([Edge], book.Talks("clairval/rose"));
        Assert.Empty(book.Talks("clairval/garance"));
        town = Village(book);
        town.Click(town.Town.Npcs.Single(n => n.Id == "rose").At);
        Assert.Equal([Edge], town.QuestTalks);
    }

    [Fact]
    public void AQuestDone_IsHandedBack_ToItsGiver_WithItsThanks()
    {
        var progress = new QuestProgress(Edge.Id, [3, 0, 1]);
        var book = new QuestBook(Data, 1, [], [progress], [new ItemCount("boar-bristle", 2)]);
        Assert.Equal([Edge], book.HandIns("clairval/aubin"));
        Assert.Empty(new QuestBook(Data, 1, [], [progress], [new ItemCount("boar-bristle", 1)]).HandIns("clairval/aubin"));
        TownController town = Village(book);
        town.Click(town.Town.Npcs.Single(n => n.Id == "aubin").At);
        Assert.Equal(Texts.Both("quest.handin", Edge.Name), town.Talk!.Line.Answers[0].Text);
        town.Answer(0);
        Assert.Equal(Edge.Thanks, town.Talk!.Line.Text);
        town.Answer(0);
        Assert.Equal(Edge.Id, town.QuestHandedIn);
        // Done: neither offered nor followed any more.
        Assert.Empty(new QuestBook(Data, 1, [Edge.Id], [], []).Offers("clairval/aubin"));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheJournal_SortsTheQuestsByZone_WithTheirObjectives(string lang)
    {
        var texts = new Texts(lang);
        Assert.Empty(new QuestBook(Data, 1, [], [], []).Journal(texts));
        var book = new QuestBook(Data, 1, [], [new QuestProgress(Edge.Id, [1, 0, 1])], [new ItemCount("boar-bristle", 5)]);
        JournalZone woods = Assert.Single(book.Journal(texts));
        Assert.Equal(Data.Towns["woods-0-0"].Name.In(lang), woods.Name);
        JournalQuest q = Assert.Single(woods.Active);
        Assert.Equal((Edge.Name.In(lang), "Aubin", false, false), (q.Name, q.Giver, q.Done, q.Ready));
        Assert.Equal(
            [(texts.Objective(Edge.Objectives![0], 1), false), (texts.Objective(Edge.Objectives[1], 2), true), (texts.Objective(Edge.Objectives[2], 1), true)],
            q.Objectives);
        Assert.Contains("1/3", q.Objectives[0].Line, StringComparison.Ordinal);
        Assert.Contains(Data.Families["boar"].Name.In(lang), q.Objectives[0].Line, StringComparison.Ordinal);
        Assert.Contains("Rose", q.Objectives[2].Line, StringComparison.Ordinal);
        // Done, under "done".
        JournalZone after = Assert.Single(new QuestBook(Data, 1, [Edge.Id], [], []).Journal(texts));
        Assert.Equal((0, 1), (after.Active.Count, after.Done.Count));
        foreach (string key in new[] { "journal.title", "journal.active", "journal.done", "journal.empty", "quest.taken", "quest.accept", "quest.later", "bar.Journal" })
            Assert.NotEqual(key, texts[key, "x"]);
    }
}
