namespace Rpg.Core.Tests;

/// <summary>The guilds (sprint 145, D79): the name, the rights of each rank, the title handed over when the leader goes, /gu.</summary>
public class GuildTests
{
    [Theory]
    [InlineData("Les Veilleurs")]
    [InlineData("Cœur-de-Chêne")]
    [InlineData("L'Accord")]
    [InlineData("Osmose")]
    public void AGuildsName_IsACapitalThenWords(string name) => Assert.True(GuildRules.IsValidName(name));

    [Theory]
    [InlineData("os")]
    [InlineData("les veilleurs")]
    [InlineData("Les  Veilleurs")]
    [InlineData("Les Veilleurs ")]
    [InlineData("Veilleurs42")]
    [InlineData("Les Veilleurs du Nord")]
    [InlineData(null)]
    public void AGuildsName_WithoutACapital_OrTwoSpaces_OrDigits_OrTooLong_IsRefused(string? name) => Assert.False(GuildRules.IsValidName(name));

    [Fact]
    public void AGuild_IsFoundedOutOfAGuild_UnderAFreeName_ForAThousandEclats()
    {
        Assert.Null(GuildRules.WhyNotFound("Les Veilleurs", taken: false, shards: GuildRules.Price, inGuild: false));
        Assert.NotNull(GuildRules.WhyNotFound("Les Veilleurs", taken: false, shards: GuildRules.Price - 1, inGuild: false));
        Assert.NotNull(GuildRules.WhyNotFound("Les Veilleurs", taken: true, shards: GuildRules.Price, inGuild: false));
        Assert.NotNull(GuildRules.WhyNotFound("Les Veilleurs", taken: false, shards: GuildRules.Price, inGuild: true));
        Assert.NotNull(GuildRules.WhyNotFound("x", taken: false, shards: GuildRules.Price, inGuild: false));
        // Two names that differ only by case are the same guild.
        Assert.Equal(GuildRules.Normalize("Les Veilleurs"), GuildRules.Normalize("LES veilleurs"));
    }

    [Fact]
    public void TheLeaderAndTheOfficers_Invite_UpToFifty()
    {
        Assert.Null(GuildRules.WhyNotInvite(GuildRank.Leader, GuildRules.Most - 1, themInGuild: false));
        Assert.Null(GuildRules.WhyNotInvite(GuildRank.Officer, 1, themInGuild: false));
        Assert.NotNull(GuildRules.WhyNotInvite(GuildRank.Member, 1, themInGuild: false));
        Assert.NotNull(GuildRules.WhyNotInvite(GuildRank.Leader, GuildRules.Most, themInGuild: false));
        Assert.NotNull(GuildRules.WhyNotInvite(GuildRank.Leader, 1, themInGuild: true));
    }

    [Theory]
    [InlineData(GuildRank.Leader, GuildRank.Officer, true)]
    [InlineData(GuildRank.Leader, GuildRank.Member, true)]
    [InlineData(GuildRank.Officer, GuildRank.Member, true)]
    [InlineData(GuildRank.Officer, GuildRank.Officer, false)]
    [InlineData(GuildRank.Officer, GuildRank.Leader, false)]
    [InlineData(GuildRank.Member, GuildRank.Member, false)]
    public void TheLeader_SendsAnyoneOut_AnOfficer_MembersOnly(GuildRank by, GuildRank them, bool may)
    {
        Assert.Equal(may, GuildRules.WhyNotKick(by, them, oneself: false) is null);
        Assert.NotNull(GuildRules.WhyNotKick(by, them, oneself: true));
    }

    [Theory]
    [InlineData(GuildRank.Leader, GuildRank.Member, GuildRank.Officer, true)]
    [InlineData(GuildRank.Leader, GuildRank.Officer, GuildRank.Member, true)]
    [InlineData(GuildRank.Leader, GuildRank.Member, GuildRank.Leader, true)]
    [InlineData(GuildRank.Leader, GuildRank.Officer, GuildRank.Officer, false)]
    [InlineData(GuildRank.Officer, GuildRank.Member, GuildRank.Officer, false)]
    [InlineData(GuildRank.Member, GuildRank.Member, GuildRank.Leader, false)]
    public void TheLeaderAlone_GivesRanks_AndHandsItsTitleOver(GuildRank by, GuildRank them, GuildRank rank, bool may) =>
        Assert.Equal(may, GuildRules.WhyNotRank(by, them, rank, oneself: false) is null);

    [Fact]
    public void ALeaderWhoGoes_HandsTheTitleToTheOldestOfficer_ElseTheOldestMember_ElseTheGuildEnds()
    {
        DateTimeOffset t0 = DateTimeOffset.UnixEpoch;
        Guid oldMember = Guid.NewGuid(), newOfficer = Guid.NewGuid(), oldOfficer = Guid.NewGuid(), newMember = Guid.NewGuid();
        Assert.Equal(oldOfficer, GuildRules.Successor([(oldMember, GuildRank.Member, t0), (newOfficer, GuildRank.Officer, t0.AddDays(3)),
            (oldOfficer, GuildRank.Officer, t0.AddDays(1)), (newMember, GuildRank.Member, t0.AddDays(4))]));
        Assert.Equal(oldMember, GuildRules.Successor([(newMember, GuildRank.Member, t0.AddDays(4)), (oldMember, GuildRank.Member, t0)]));
        Assert.Null(GuildRules.Successor([]));
    }

    [Fact]
    public void TheGuildsTab_ListsByRank_ThenThoseInPlay_ThenByName()
    {
        GuildMember M(string name, GuildRank rank, string? zone) => new(Guid.NewGuid(), name, rank, 30, "guard", zone, null);
        IReadOnlyList<GuildMember> ordered = GuildRules.Ordered([M("Zélie", GuildRank.Member, "clairval"), M("Anne", GuildRank.Member, null),
            M("Brune", GuildRank.Officer, null), M("Odon", GuildRank.Leader, null), M("Basile", GuildRank.Member, "woods")]);
        Assert.Equal(["Odon", "Brune", "Basile", "Zélie", "Anne"], ordered.Select(m => m.Name));
    }

    [Theory]
    [InlineData("/gu on y va ?", ChatChannels.Guild, "on y va ?")]
    [InlineData("/GU salut", ChatChannels.Guild, "salut")]
    [InlineData("/g salut", ChatChannels.Party, "salut")]
    [InlineData("/p salut", ChatChannels.Party, "salut")]
    [InlineData("/gus", ChatChannels.Map, "/gus")]
    [InlineData("bonjour", ChatChannels.Map, "bonjour")]
    public void ALineTyped_GoesToItsChannel(string line, string channel, string text) =>
        Assert.Equal((channel, text), ChatCommands.Route(line));

    [Fact]
    public void Edmée_SaysThePriceTheRulesAsk()
    {
        // Her line writes the price out in words (the screenwriter's review, after sprint 151): it must follow GuildRules.
        Assert.Equal(1000, GuildRules.Price);
        LocalizedText hello = GameData.Embedded.Dialogues["registrar"].Lines["hello"].Text;
        Assert.Contains("mille Éclats", hello.Fr, StringComparison.Ordinal);
        Assert.Contains("a thousand Éclats", hello.En, StringComparison.Ordinal);
    }

    [Fact]
    public void AnApplication_ANote_AndAnAnswer_FollowTheirRules()
    {
        // Out of a guild, to one that exists, not twice, three waiting at most, not to a full one, a short word.
        Assert.Null(GuildRules.WhyNotApply(false, true, 2, false, GuildRules.Most - 1, new string('a', GuildRules.ApplicationWordLength)));
        Assert.NotNull(GuildRules.WhyNotApply(true, true, 0, false, 1, null));
        Assert.NotNull(GuildRules.WhyNotApply(false, false, 0, false, 0, null));
        Assert.NotNull(GuildRules.WhyNotApply(false, true, 0, true, 1, null));
        Assert.NotNull(GuildRules.WhyNotApply(false, true, GuildRules.MostApplications, false, 1, null));
        Assert.NotNull(GuildRules.WhyNotApply(false, true, 0, false, GuildRules.Most, null));
        Assert.NotNull(GuildRules.WhyNotApply(false, true, 0, false, 1, new string('a', GuildRules.ApplicationWordLength + 1)));
        // The leader alone answers (Charles: « le chef a accès aux candidatures »).
        Assert.Null(GuildRules.WhyNotAnswer(GuildRank.Leader));
        Assert.NotNull(GuildRules.WhyNotAnswer(GuildRank.Officer));
        // One's own note; another's only cleared, by the leader or an officer.
        Assert.Null(GuildRules.WhyNotNote(GuildRank.Member, true, "Cueilleuse"));
        Assert.NotNull(GuildRules.WhyNotNote(GuildRank.Member, true, new string('a', GuildRules.NoteLength + 1)));
        Assert.Null(GuildRules.WhyNotNote(GuildRank.Officer, false, null));
        Assert.NotNull(GuildRules.WhyNotNote(GuildRank.Officer, false, "Autre"));
        Assert.NotNull(GuildRules.WhyNotNote(GuildRank.Member, false, null));
    }
}
