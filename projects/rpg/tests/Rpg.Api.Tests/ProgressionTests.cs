using System.Net;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>Experience from fights the server replays, quests, levels and points (sprint 58).</summary>
public class ProgressionTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    /// <summary>Plays the fight the server started, the AI choosing for the hero, and records it.</summary>
    private static FightRecord Play(FightTicket ticket, Hero hero)
    {
        var fight = new Fight(GameData.Embedded, ticket.Scenario, ticket.Seed, hero);
        Ai.PlayOut(fight);
        return FightRecord.Of(fight);
    }

    [Fact]
    public async Task AFightTheServerStarted_EarnsItsExperience_Once_AndTheFirstWinDoesTheQuest()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary c = await server.CreateCharacter("Élise", "female-c", "sentinel", cancel: Cancel);
        Assert.Equal((0L, 1, 0), (c.Xp, c.Level, c.Quests?.Count ?? 0));
        long total = 0;
        bool questDone = false;
        for (int i = 0; i < 12; i++)
        {
            FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
            FightRecord record = Play(ticket, (await server.Characters(Cancel)).Single().Hero);
            FightResult result = await server.ReportFight(c.Id, ticket.Id, record, Cancel);
            bool won = record.Replay(GameData.Embedded).WinningTeam == 0;
            long expected = won ? Progression.MonsterXp(1) * 2 + (questDone ? 0 : 150) : 0;
            Assert.Equal(expected, result.Xp);
            Assert.Equal(won && !questDone ? "first-lesson" : null, result.Quest);
            questDone |= won;
            total += expected;
            Assert.Equal((total, Progression.LevelFor(total)), (result.TotalXp, result.Level));
            // The same ticket does not serve twice.
            var again = await Assert.ThrowsAsync<ServerException>(() => server.ReportFight(c.Id, ticket.Id, record, Cancel));
            Assert.Equal(HttpStatusCode.NotFound, again.Status);
        }
        Assert.True(questDone, "twelve training fights without a win");
        CharacterSummary after = (await server.Characters(Cancel)).Single();
        Assert.Equal((total, Progression.LevelFor(total)), (after.Xp, after.Level));
        Assert.Equal(["first-lesson"], after.Quests!);
    }

    [Fact]
    public async Task ARecordThatIsNotTheFightTheServerStarted_IsRefused_AndTheTicketIsUsed()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary c = await server.CreateCharacter("Élise", "female-c", "guard", cancel: Cancel);
        async Task Refused(Func<FightTicket, FightRecord> cheat, string why)
        {
            FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
            var e = await Assert.ThrowsAsync<ServerException>(() => server.ReportFight(c.Id, ticket.Id, cheat(ticket), Cancel));
            Assert.Equal(HttpStatusCode.BadRequest, e.Status);
            Assert.Contains(why, e.Message, StringComparison.Ordinal);
            var used = await Assert.ThrowsAsync<ServerException>(() => server.ReportFight(c.Id, ticket.Id, Play(ticket, c.Hero), Cancel));
            Assert.Equal(HttpStatusCode.NotFound, used.Status);
        }
        // Rolls of one's own choosing, a stronger hero, a duel, a fight left unfinished, a forged action.
        await Refused(t => Play(t with { Seed = t.Seed + 1 }, c.Hero), "Not the fight the server started.");
        await Refused(t => Play(t, c.Hero with { Level = 100 }), "Not this character's fight.");
        await Refused(t => Play(t with { Scenario = "duel" }, c.Hero), "Not the fight the server started.");
        await Refused(t => new FightRecord(FightRecord.CurrentVersion, t.Scenario, t.Seed, [new EndTurnAction()], c.Hero), "The fight is not over.");
        await Refused(t => Play(t, c.Hero) with { Actions = [new CastAction("titan-blow", new Cell(1, 1))] }, "titan-blow");
        Assert.Equal(0, (await server.Characters(Cancel)).Single().Xp);
    }

    [Fact]
    public async Task NobodyStartsAFightForSomeoneElse_NorInAnUnknownPlace()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary c = await server.CreateCharacter("Élise", "female-c", "mage", cancel: Cancel);
        GameServer other = await api.SignedIn("o");
        Assert.Equal(HttpStatusCode.NotFound, (await Assert.ThrowsAsync<ServerException>(() => other.StartFight(c.Id, "training", Cancel))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.StartFight(c.Id, "atlantis", Cancel))).Status);
    }

    [Fact]
    public async Task Points_AreSpentWithinTheLevel_AndTheNextFightUsesThem()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary c = await server.CreateCharacter("Élise", "female-c", "guard", cancel: Cancel);
        // Level 1: no point to spend yet.
        var e = await Assert.ThrowsAsync<ServerException>(() => server.SavePoints(c.Id, new Points(new Characteristics(Vitality: 1), null), Cancel));
        Assert.Contains("points: At most 0 characteristic points", e.Message, StringComparison.Ordinal);
        // Win until level 2 (the quest alone gives 150 experience).
        for (int i = 0; i < 12 && (await server.Characters(Cancel)).Single().Level < 2; i++)
        {
            FightTicket t = await server.StartFight(c.Id, "training", Cancel);
            await server.ReportFight(c.Id, t.Id, Play(t, (await server.Characters(Cancel)).Single().Hero), Cancel);
        }
        Assert.True((await server.Characters(Cancel)).Single().Level >= 2);
        var ranks = new Dictionary<string, int> { ["shield-bash"] = 2 };
        CharacterSummary spent = await server.SavePoints(c.Id, new Points(new Characteristics(Vitality: 4, Strength: 6), ranks), Cancel);
        Assert.Equal((new Characteristics(Vitality: 4, Strength: 6), 2), (spent.Stats, spent.Ranks!["shield-bash"]));
        Assert.Equal(spent, (await server.Characters(Cancel)).Single());
        foreach (Points wrong in new[] { new Points(new Characteristics(Vitality: 11), null), new Points(null, new Dictionary<string, int> { ["shield-bash"] = 3 }), new Points(null, new Dictionary<string, int> { ["ice-shard"] = 2 }) })
            Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePoints(c.Id, wrong, Cancel))).Status);
        // The server replays with the points: a record played without them is someone else's fight.
        FightTicket ticket = await server.StartFight(c.Id, "training", Cancel);
        var without = await Assert.ThrowsAsync<ServerException>(() => server.ReportFight(c.Id, ticket.Id, Play(ticket, spent.Hero with { Stats = null, Ranks = null }), Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, without.Status);
        ticket = await server.StartFight(c.Id, "training", Cancel);
        FightResult ok = await server.ReportFight(c.Id, ticket.Id, Play(ticket, spent.Hero), Cancel);
        Assert.True(ok.TotalXp > 0);
    }
}
