using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The guild's tab (sprint 145): what each rank may do to each member, who invites, when a member was last seen.</summary>
public class GuildPageTests
{
    private static readonly GuildMember Odon = M("Odon", GuildRank.Leader), Brune = M("Brune", GuildRank.Officer), Anne = M("Anne", GuildRank.Member);
    private static readonly GuildView Guild = new(Guid.NewGuid(), "Les Veilleurs", [Odon, Brune, Anne]);

    private static GuildMember M(string name, GuildRank rank) => new(Guid.NewGuid(), name, rank, 30, "guard", null, null);

    [Fact]
    public void TheLeader_NamesOfficers_SendsOut_AndHandsTheTitleOver_NeverToItself()
    {
        Assert.Equal(new GuildActions(true, false, true, true), GuildPage.Actions(Guild, Odon.Id, Anne));
        Assert.Equal(new GuildActions(false, true, true, true), GuildPage.Actions(Guild, Odon.Id, Brune));
        Assert.Equal(new GuildActions(false, false, false, false), GuildPage.Actions(Guild, Odon.Id, Odon));
    }

    [Fact]
    public void AnOfficer_SendsMembersOut_Only_AndAMember_DoesNothing()
    {
        Assert.Equal(new GuildActions(false, false, true, false), GuildPage.Actions(Guild, Brune.Id, Anne));
        Assert.Equal(new GuildActions(false, false, false, false), GuildPage.Actions(Guild, Brune.Id, Odon));
        Assert.Equal(new GuildActions(false, false, false, false), GuildPage.Actions(Guild, Anne.Id, Brune));
        // Someone out of the guild may do nothing at all.
        Assert.Equal(new GuildActions(false, false, false, false), GuildPage.Actions(Guild, Guid.NewGuid(), Anne));
    }

    [Fact]
    public void TheLeaderAndTheOfficers_Invite_WhileThereIsRoom()
    {
        Assert.True(GuildPage.CanInvite(Guild, Odon.Id));
        Assert.True(GuildPage.CanInvite(Guild, Brune.Id));
        Assert.False(GuildPage.CanInvite(Guild, Anne.Id));
        var full = Guild with { Members = [Odon, .. Enumerable.Range(0, GuildRules.Most - 1).Select(i => M("Membre" + i, GuildRank.Member))] };
        Assert.False(GuildPage.CanInvite(full, Odon.Id));
        Assert.Equal((GuildRules.Most, 50), GuildPage.Room(full));
    }

    [Fact]
    public void LastSeen_IsInDays_ElseHours_ElseMinutes()
    {
        DateTimeOffset now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal((3, "days"), GuildPage.Ago(now.AddDays(-3).AddHours(-5), now));
        Assert.Equal((5, "hours"), GuildPage.Ago(now.AddHours(-5).AddMinutes(-20), now));
        Assert.Equal((20, "minutes"), GuildPage.Ago(now.AddMinutes(-20), now));
        Assert.Equal((1, "minutes"), GuildPage.Ago(now.AddSeconds(-10), now));
        Assert.Null(GuildPage.Ago(null, now));
    }

    private static GuildStanding Standing(long points, int week = 0, int goal = GuildRules.DefaultGoal) =>
        GuildRules.Standing(points, week, 1, 1, goal, null, GuildCrests.Default);

    private static string Rpg([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    [Fact]
    public void OnlyTheLeader_WritesTheMessage_TheGoal_AndTheCrest()
    {
        Assert.True(GuildPage.CanLead(Guild, Odon.Id));
        Assert.False(GuildPage.CanLead(Guild, Brune.Id));
        Assert.False(GuildPage.CanLead(Guild, Anne.Id));
    }

    [Fact]
    public void TheBars_SayHowFarTowardTheNextLevel_AndTheWeeksGoal()
    {
        Assert.Equal(0.5, GuildPage.Toward(Standing(75)));
        Assert.Equal(0.25, GuildPage.Toward(Standing(225)));
        Assert.Equal(1, GuildPage.Toward(Standing(7000)));
        Assert.Equal(0.25, GuildPage.TowardGoal(Standing(0, 30)));
        Assert.Equal(1, GuildPage.TowardGoal(Standing(0, 500)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 3)]
    [InlineData(2, 6)]
    [InlineData(3, 9)]
    public void EachTab_OpensAtItsLevel(int tab, int level) => Assert.Equal(level, GuildPage.TabLevel(tab));

    [Fact]
    public void AMember_TakesOnlyFromATabOpenedToIt_AnOfficerFromAny()
    {
        var chest = new GuildChest([new GuildTab(false, []), new GuildTab(true, [])], []);
        Assert.False(GuildPage.CanTake(new ChestView(chest, GuildRank.Member), 0));
        Assert.True(GuildPage.CanTake(new ChestView(chest, GuildRank.Member), 1));
        Assert.True(GuildPage.CanTake(new ChestView(chest, GuildRank.Officer), 0));
        Assert.False(GuildPage.CanTake(new ChestView(chest, GuildRank.Leader), 2));
    }

    [Fact]
    public void EachPartOfACrest_IsNamed_InBothLanguages()
    {
        foreach (string lang in new[] { "fr", "en" })
        {
            var texts = new Texts(lang);
            Assert.All(GuildCrests.Shapes, id => Assert.NotEmpty(texts["guild.crest.shape." + id]));
            Assert.All(GuildCrests.Icons, id => Assert.NotEmpty(texts["guild.crest.icon." + id]));
            Assert.All(GuildCrests.Colours.Keys, id => Assert.NotEmpty(texts["guild.crest.colour." + id]));
        }
    }

    [Fact]
    public void EachPictogram_IsDrawn_AndCredited_AndEachColour_IsTheCharters()
    {
        string icons = Path.Combine(Rpg(), "godot", "assets", "icons", "crest");
        string credits = File.ReadAllText(Path.Combine(icons, "credits.json"));
        Assert.All(GuildCrests.Icons, id =>
        {
            Assert.True(File.Exists(Path.Combine(icons, id + ".svg")), id);
            Assert.Contains($"\"{id}\":", credits, StringComparison.Ordinal);
        });
        string tokens = File.ReadAllText(Path.Combine(Rpg(), "..", "..", "src", "assets", "tokens.css"));
        Assert.All(GuildCrests.Colours, c => Assert.Contains($"--{c.Key}: {c.Value};", tokens, StringComparison.Ordinal));
        Assert.Equal("#5a3a22", GuildPage.Colour("chocolat"));
    }

    [Fact]
    public void ThePlayersOfTheMap_CarryTheirGuildsCrest()
    {
        var crowd = new MapCrowd();
        Guid aube = Guid.NewGuid();
        crowd.Apply(new MapNews(MapNewsKind.Here, aube, "Aube", new Hero("Aube", "female-c", null, 0), new Cell(3, 4), Guild: "Les Veilleurs", Crest: "banner/wolf-head/gris-fond/osmose-cost"), Guid.NewGuid());
        OtherPlayer seen = Assert.Single(crowd.Others);
        Assert.Equal(new GuildCrest("banner", "wolf-head", "gris-fond", "osmose-cost"), GuildPage.CrestOf(seen.Crest));
        Assert.Null(GuildPage.CrestOf(null));
    }

    [Fact]
    public void Edmee_OpensTheGuildsChest_FromHerFirstLine()
    {
        Town square = GameData.Embedded.Towns[GuildRules.RegistrarMap(GameData.Embedded)!];
        Npc edmee = square.Npcs.Single(n => n.Id == GuildRules.Registrar);
        var talk = new Conversation(edmee, GameData.Embedded.Dialogues[edmee.Dialogue]);
        int chest = talk.Line.Answers.ToList().FindIndex(a => a.Chest);
        Assert.True(chest >= 0);
        talk.Choose(chest);
        Assert.True(talk.IsOver);
        Assert.True(talk.Chest);
    }
}
