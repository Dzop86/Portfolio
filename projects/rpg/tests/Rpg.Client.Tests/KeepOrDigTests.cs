using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// "Keep or dig" at the miners' camp (sprint 232, D103): at 46 Bérold offers the wall and Berthe the widening, the
/// "I accept" naming the quest given up for good; once a side is taken the other is no longer offered, and the
/// greetings change with it.
/// </summary>
public class KeepOrDigTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static Conversation TalkTo(QuestBook book, string who)
    {
        var town = new TownController(Data, "ancestors-1-1") { Book = book };
        Npc npc = town.Town.Npcs.Single(n => n.Id == who);
        Assert.NotNull(town.Click(npc.At));
        return town.Talk!;
    }

    [Fact]
    public void BothSides_AreOffered_AndAcceptingOne_SaysWhatIsGivenUp()
    {
        var book = new QuestBook(Data, 46, ["roots-of-the-mother-tree"], [], []);
        Quest wall = Data.Quests["wall-the-passage"], widen = Data.Quests["widen-the-passage"];
        Assert.Contains(wall, book.Offers("ancestors/berold"));
        Assert.Contains(widen, book.Offers("ancestors/berthe"));
        Conversation talk = TalkTo(book, "berold");
        int offer = talk.Line.Answers.ToList().FindIndex(a => a.Text == Texts.Both("quest.offer", wall.Name));
        talk.Choose(offer);
        Assert.Equal(wall.Text, talk.Line.Text);
        Assert.Equal(Texts.Both("quest.accept.rival", widen.Name), talk.Line.Answers[0].Text);
        Assert.Contains("Élargir le passage", talk.Line.Answers[0].Text.Fr, StringComparison.Ordinal);
        Assert.Contains("Widen the Passage", talk.Line.Answers[0].Text.En, StringComparison.Ordinal);
        talk.Choose(0);
        Assert.Equal("wall-the-passage", talk.Taken);
        // A quest without rivals keeps the plain "I accept".
        Assert.DoesNotContain(new Texts("fr")["quest.accept"], new Texts("fr")["quest.accept.rival", "x"], StringComparison.Ordinal);
    }

    [Fact]
    public void TheSideTaken_ShutsTheOther_AndTheGreetingsFollowIt()
    {
        var walling = new QuestBook(Data, 46, ["roots-of-the-mother-tree"], [Quests.Start(Data.Quests["wall-the-passage"])], []);
        Assert.DoesNotContain(Data.Quests["widen-the-passage"], walling.Offers("ancestors/berthe"));
        var walled = new QuestBook(Data, 51, ["roots-of-the-mother-tree", "wall-the-passage"], [], []);
        Assert.DoesNotContain(Data.Quests["widen-the-passage"], walled.Offers("ancestors/berthe"));
        Assert.Contains(Data.Quests["the-boundary-stone"], walled.Offers("ancestors/berold"));
        // The first lines change once the side is taken, not before.
        Dialogue berthe = Data.Dialogues["berthe"], berold = Data.Dialogues["berold"];
        Assert.Equal(berthe.Lines[berthe.Start].Text, TalkTo(walling, "berthe").Line.Text);
        Assert.Equal(berthe.Greet(["wall-the-passage"]), TalkTo(walled, "berthe").Line.Text);
        Assert.Equal(berold.Greet(["wall-the-passage"]), TalkTo(walled, "berold").Line.Text);
        Assert.NotEqual(berold.Lines[berold.Start].Text, TalkTo(walled, "berold").Line.Text);
    }
}
