using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The quests in the town and the journal (sprint 88): offered in a talk, taken, followed, handed back.</summary>
public class QuestBookTests
{
    private static readonly GameData Data = GameData.Embedded;
    private static readonly Quest Edge = Data.Quests["edge-of-the-woods"];

    // Aubin's welcome (sprint 107): talk to Garance, who takes it back herself.
    private static readonly Quest Welcome = Data.Quests["welcome"];

    private static TownController Village(QuestBook book) => new(Data, "clairval") { Book = book };

    [Fact]
    public void Aubin_OffersHisQuest_ItsWords_AndAcceptingTakesIt()
    {
        TownController town = Village(new QuestBook(Data, 1, [], [], []));
        Npc aubin = town.Town.Npcs.Single(n => n.Id == "aubin");
        Assert.NotNull(town.Click(aubin.At));
        Conversation talk = town.Talk!;
        DialogueLine first = talk.Line;
        Assert.Equal(Texts.Both("quest.offer", Welcome.Name), first.Answers[0].Text);
        // The dialogue's own answers come after.
        Assert.Equal(Data.Dialogues[aubin.Dialogue].Lines[talk.Dialogue.Start].Answers, first.Answers.Skip(1));
        town.Answer(0);
        Assert.Equal(Welcome.Text, town.Talk!.Line.Text);
        town.Answer(0);
        Assert.Null(town.Talk);
        Assert.Equal(("welcome", (string?)null), (town.QuestTaken, town.QuestHandedIn));
        town.QuestSeenTo();
        Assert.Null(town.QuestTaken);
    }

    [Fact]
    public void Later_TakesNothing_AndTheWelcome_LeadsToGarance_WhoSaysHerLine_AndTakesItBack()
    {
        TownController town = Village(new QuestBook(Data, 1, [], [], []));
        Npc aubin = town.Town.Npcs.Single(n => n.Id == "aubin");
        town.Click(aubin.At);
        town.Answer(0);
        town.Answer(1);
        Assert.Null(town.QuestTaken);
        var book = new QuestBook(Data, 1, [], [Quests.Start(Welcome)], []);
        Assert.Empty(book.Offers("clairval/aubin"));
        Assert.Empty(book.HandIns("clairval/aubin"));
        Assert.Equal([Welcome], book.Talks("clairval/garance"));
        Assert.Empty(book.Talks("clairval/rose"));
        // Garance at the West Gate's drill yard (sprint 102): her own line for the welcome instead of her greeting
        // (sprint 107), and the quest to hand back to her at once, the talk counted as it opens.
        town = new TownController(Data, "clairval-0-1") { Book = book };
        town.Click(town.Town.Npcs.Single(n => n.Id == "garance").At);
        Assert.Equal([Welcome], town.QuestTalks);
        Assert.Equal(Welcome.Objectives![0].Line, town.Talk!.Line.Text);
        Assert.Equal(Texts.Both("quest.handin", Welcome.Name), town.Talk.Line.Answers[0].Text);
        town.Answer(0);
        Assert.Equal(Welcome.Thanks, town.Talk!.Line.Text);
        town.Answer(0);
        Assert.Equal("welcome", town.QuestHandedIn);
        // Talked to without a quest that asks for her, she greets as always.
        town = new TownController(Data, "clairval-0-1") { Book = new QuestBook(Data, 1, ["welcome"], [], []) };
        town.Click(town.Town.Npcs.Single(n => n.Id == "garance").At);
        Assert.Equal(Data.Dialogues["trainer"].Lines["offer"].Text, town.Talk!.Line.Text);
    }

    [Fact]
    public void AnInhabitant_NeverHasMoreThanTwoQuestsUnderWay_TheLowestOffered()
    {
        // At level 30, nothing done: Garance has more to give, offered two at a time, the lowest first (Charles, sprint 102).
        Quest[] open = [.. Data.Quests.Values.Where(q => q.Giver == "clairval/garance" && Quests.WhyNot(q, 30, [], []) is null).OrderBy(q => q.Level)];
        Assert.True(open.Length > QuestBook.MostFromOne);
        Assert.Equal(open.Take(QuestBook.MostFromOne), new QuestBook(Data, 30, [], [], []).Offers("clairval/garance"));
        // One under way: one more only.
        Assert.Single(new QuestBook(Data, 30, [], [Quests.Start(open[0])], []).Offers("clairval/garance"));
        // A player who takes all they are offered, level by level, each giver's oldest quest done at each level.
        var done = new List<string>();
        var active = new List<QuestProgress>();
        string[] givers = [.. Data.Quests.Values.Select(q => q.Giver).OfType<string>().Distinct()];
        for (int level = 1; level <= 30; level++)
        {
            foreach (string giver in givers)
            {
                active.AddRange(new QuestBook(Data, level, done, active, []).Offers(giver).Select(Quests.Start));
                Assert.InRange(active.Count(p => Data.Quests[p.Quest].Giver == giver), 0, QuestBook.MostFromOne);
            }
            foreach (string giver in givers)
            {
                if (active.FirstOrDefault(p => Data.Quests[p.Quest].Giver == giver) is QuestProgress oldest)
                {
                    active.Remove(oldest);
                    done.Add(oldest.Quest);
                }
            }
        }
        // Every quest given by someone is offered on the way.
        Assert.Equal(Data.Quests.Values.Where(q => q.Giver is not null).Select(q => q.Id).Order(StringComparer.Ordinal), done.Where(d => Data.Quests[d].IsGiven).Concat(active.Select(p => p.Quest)).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AQuestDone_IsHandedBack_ToItsGiver_WithItsThanks()
    {
        // Garance's since sprint 107, at the West Gate.
        var progress = new QuestProgress(Edge.Id, [3, 0]);
        var book = new QuestBook(Data, 4, [], [progress], [new ItemCount("boar-bristle", 2)]);
        Assert.Equal([Edge], book.HandIns("clairval/garance"));
        Assert.Empty(new QuestBook(Data, 4, [], [progress], [new ItemCount("boar-bristle", 1)]).HandIns("clairval/garance"));
        var town = new TownController(Data, "clairval-0-1") { Book = book };
        town.Click(town.Town.Npcs.Single(n => n.Id == "garance").At);
        Assert.Equal(Texts.Both("quest.handin", Edge.Name), town.Talk!.Line.Answers[0].Text);
        town.Answer(0);
        Assert.Equal(Edge.Thanks, town.Talk!.Line.Text);
        town.Answer(0);
        Assert.Equal(Edge.Id, town.QuestHandedIn);
        // Done: neither offered nor followed any more.
        Assert.DoesNotContain(Edge, new QuestBook(Data, 4, [Edge.Id], [], []).Offers("clairval/garance"));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheJournal_SortsTheQuestsByZone_WithTheirObjectives(string lang)
    {
        var texts = new Texts(lang);
        Assert.Empty(new QuestBook(Data, 1, [], [], []).Journal(texts));
        var book = new QuestBook(Data, 1, [], [new QuestProgress(Edge.Id, [1, 0])], [new ItemCount("boar-bristle", 5)]);
        JournalZone woods = Assert.Single(book.Journal(texts));
        Assert.Equal(Data.Towns["woods-0-0"].Name.In(lang), woods.Name);
        JournalQuest q = Assert.Single(woods.Active);
        Assert.Equal((Edge.Name.In(lang), "Garance", false, false), (q.Name, q.Giver, q.Done, q.Ready));
        Assert.Equal([(texts.Objective(Edge.Objectives![0], 1), false), (texts.Objective(Edge.Objectives[1], 2), true)], q.Objectives);
        Assert.Contains("1/3", q.Objectives[0].Line, StringComparison.Ordinal);
        Assert.Contains(Data.Families["boar"].Name.In(lang), q.Objectives[0].Line, StringComparison.Ordinal);
        // Done, under "done".
        JournalZone after = Assert.Single(new QuestBook(Data, 1, [Edge.Id], [], []).Journal(texts));
        Assert.Equal((0, 1), (after.Active.Count, after.Done.Count));
        foreach (string key in new[] { "journal.title", "journal.active", "journal.done", "journal.empty", "quest.taken", "quest.accept", "quest.later", "bar.Journal" })
            Assert.NotEqual(key, texts[key, "x"]);
    }

    [Fact]
    public void OverEachInhabitant_AnExclamationToTake_AQuestionToHandBack()
    {
        // Sprint 105 (D75): "!" a quest to take, "?" one to hand back, a grey "?" one under way.
        var fresh = new QuestBook(Data, 1, [], [], []);
        Assert.Equal(QuestMark.Offer, fresh.Mark("clairval/aubin"));
        Assert.Equal(QuestMark.None, fresh.Mark("clairval/gaspard"));
        QuestProgress stone = Quests.Start(Data.Quests["whispering-stone"]);
        var underWay = new QuestBook(Data, 1, [], [stone], []);
        Assert.Equal(QuestMark.Waiting, underWay.Mark("clairval/ysaline"));
        var ready = new QuestBook(Data, 1, [], [stone with { Counts = [1, 1, 1] }], []);
        Assert.Equal(QuestMark.Ready, ready.Mark("clairval/ysaline"));
        // The ring worn, Garance's "Kitted out" is to hand back (she has it to offer after the first lesson).
        Assert.Equal(QuestMark.Offer, new QuestBook(Data, 1, ["first-lesson"], [], []).Mark("clairval/garance"));
        QuestProgress kit = Quests.Start(Data.Quests["well-equipped"]);
        Assert.Empty(new QuestBook(Data, 1, ["first-lesson"], [kit], [new ItemCount("copper-ring", 1)]).HandIns("clairval/garance"));
        Assert.Equal(QuestMark.Ready, new QuestBook(Data, 1, ["first-lesson"], [kit], [new ItemCount("copper-ring", 1)], ["copper-ring"]).Mark("clairval/garance"));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public void TheJournal_SaysWhereToGo_TheNextObjectivesMap_ThenTheGivers(string lang)
    {
        var texts = new Texts(lang);
        string Title(string map) => texts.MapTitle(Data.Towns[map]);
        QuestProgress stone = Quests.Start(Data.Quests["whispering-stone"]);
        var book = new QuestBook(Data, 1, [], [stone], []);
        // The old stone first, on the square; then Aubin, there too; then Odalric, at the chapel; then back to Ysaline.
        Assert.Equal(Title("clairval"), book.Where(stone, texts));
        Assert.Equal(Title("clairval-2-2"), book.Where(stone with { Counts = [1, 1, 0] }, texts));
        Assert.Equal(Title("clairval"), book.Where(stone with { Counts = [1, 1, 1] }, texts));
        Assert.Equal(Title("clairval"), book.Journal(texts).Single().Active.Single().Where);
        // Monsters: where their family roams; a dungeon: its door's map; an item to wear: the inventory.
        QuestProgress edge = Quests.Start(Edge);
        Assert.StartsWith(Data.Towns[Data.Groups.Values.Where(g => g.Family == "boar" && !Data.Towns[g.Map].IsHall).Select(g => g.Map).Order(StringComparer.Ordinal).First()].Name.In(lang),
            new QuestBook(Data, 1, [], [edge], []).Where(edge, texts), StringComparison.Ordinal);
        QuestProgress wallow = Quests.Start(Data.Quests["rumble-under-the-wallow"]);
        Assert.Equal(Title(Data.Dungeons["boar-wallow"].Map), new QuestBook(Data, 5, [], [wallow], []).Where(wallow, texts));
        QuestProgress kit = Quests.Start(Data.Quests["well-equipped"]);
        Assert.Equal(texts["journal.inventory"], new QuestBook(Data, 1, [], [kit], []).Where(kit, texts));
        Assert.Contains(texts.Objective(Data.Quests["whispering-stone"].Objectives![0], 0), book.Journal(texts).Single().Active.Single().Objectives.Select(o => o.Line));
    }

    [Fact]
    public void TheOldStone_IsWalkedUpTo_AndExamined_ForTheQuestThatAsks()
    {
        QuestProgress stone = Quests.Start(Data.Quests["whispering-stone"]);
        TownController town = Village(new QuestBook(Data, 1, [], [stone], []));
        (_, Prop prop) = Data.FindProp("old-stone")!.Value;
        Assert.False(town.Town.IsFree(town.Board, prop.At));
        TownStep step = town.Click(prop.At)!;
        // Beside it, a diagonal step away at most.
        Assert.Equal(1, Math.Max(Math.Abs(town.Position.X - prop.At.X), Math.Abs(town.Position.Y - prop.At.Y)));
        Assert.Equal(prop, town.Examining);
        Assert.Null(step.TalkTo);
        Assert.Equal(["whispering-stone"], town.Book!.Interacts("old-stone").Select(q => q.Id));
        town.ExaminingSeenTo();
        Assert.Null(town.Examining);
        // Done already: nothing more to find there.
        Assert.Empty(new QuestBook(Data, 1, [], [stone with { Counts = [1, 0, 0] }], []).Interacts("old-stone"));
    }
}
