using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The guild window's data (sprint 160, D84), with three game clients: an application by the guild's name, seen and
/// accepted by the leader alone, the applicant a member and its other applications gone; another refused; the log of
/// arrivals, ranks and departures; a member's note; the points each brought; the others told when a member comes into play.
/// </summary>
public class GuildRecruitmentTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

    private static Func<Uri, AuthenticationHeaderValue?, CancellationToken, Task<WebSocket>> Through(ApiFactory api) => (uri, auth, cancel) =>
    {
        var client = api.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization = auth?.ToString();
        return client.ConnectAsync(uri, cancel);
    };

    private static async Task<ChatClient> Connect(ApiFactory api, GameServer server, Guid id)
    {
        ChatClient chat = await server.OpenChat(id, Through(api), Cancel);
        var hub = api.Services.GetRequiredService<ChatHub>();
        for (int i = 0; i < 100 && hub.MapOf(id) is null; i++)
            await Task.Delay(20, Cancel);
        return chat;
    }

    private static async Task<ChatEvent?> Next(ChatClient chat, Func<ChatEvent, bool> wanted)
    {
        while (await chat.Next(Wait, Cancel) is ChatEvent e)
        {
            if (wanted(e))
                return e;
        }
        return null;
    }

    private static HttpStatusCode Status(Exception e) => ((ServerException)e).Status;

    [Fact]
    public async Task AnApplication_IsAnsweredByTheLeader_AndTheLog_TheNotes_AndTheComingsAreKept()
    {
        await using var api = new ApiFactory();
        GameServer sa = await api.SignedIn("rc"), sb = await api.SignedIn("rc"), sc = await api.SignedIn("rc");
        CharacterSummary aube = await sa.CreateCharacter("Aube", "female-c", "guard", cancel: Cancel);
        CharacterSummary brume = await sb.CreateCharacter("Brume", "female-c", "mage", cancel: Cancel);
        CharacterSummary cendre = await sc.CreateCharacter("Cendre", "male-c", "mage", cancel: Cancel);
        ChatClient a = await Connect(api, sa, aube.Id);
        api.WithDb(db =>
        {
            foreach (Rpg.Api.Data.Character ch in db.Characters)
                ch.Shards = GuildRules.Price;
            return db.SaveChanges();
        });
        await sa.FoundGuild(aube.Id, "Les Veilleurs", Cancel);
        await sc.FoundGuild(cendre.Id, "Les Brumes", Cancel);
        // Brume applies to both: an unknown name refused, the same guild twice too.
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sb.ApplyToGuild(brume.Id, "Nulle Part", null, Cancel))));
        await sb.ApplyToGuild(brume.Id, "les veilleurs", "Je connais le bois.", Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sb.ApplyToGuild(brume.Id, "Les Veilleurs", null, Cancel))));
        api.Clock.Advance(TimeSpan.FromMinutes(1));
        await sb.ApplyToGuild(brume.Id, "Les Brumes", null, Cancel);
        // The leader in play is told; it sees the application with its word; Brume sees its two.
        GuildNews applied = (await Next(a, e => e.Guild?.Guild == GuildNewsKind.Applied))!.Guild!;
        Assert.Equal((brume.Id, "Brume"), (applied.From, applied.Name));
        GuildApplication seen = Assert.Single(await sa.GuildApplications(aube.Id, Cancel));
        Assert.Equal(("Brume", "Je connais le bois."), (seen.Name, seen.Message));
        Assert.Equal(["Les Veilleurs", "Les Brumes"], (await sb.GuildApplications(brume.Id, Cancel)).Select(x => x.GuildName));
        // Accepted: Brume a member, its other application gone; Cendre's guild has none left.
        await sa.AnswerApplication(aube.Id, brume.Id, true, Cancel);
        GuildView two = (await sb.Guild(brume.Id, Cancel)).View!;
        Assert.Equal(["Aube", "Brume"], two.Members.Select(m => m.Name));
        Assert.Empty(await sc.GuildApplications(cendre.Id, Cancel));
        // A member cannot see nor answer the applications.
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sb.GuildApplications(brume.Id, Cancel))));
        // Its note, its own; the leader may clear it, not write it.
        await sb.SetGuildNote(brume.Id, brume.Id, "Cueilleuse", Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sa.SetGuildNote(aube.Id, brume.Id, "Autre", Cancel))));
        Assert.Equal("Cueilleuse", (await sa.Guild(aube.Id, Cancel)).View!.Members.Single(m => m.Name == "Brume").Note);
        await sa.SetGuildNote(aube.Id, brume.Id, null, Cancel);
        Assert.Null((await sa.Guild(aube.Id, Cancel)).View!.Members.Single(m => m.Name == "Brume").Note);
        // Brume comes into play: Aube is told.
        ChatClient b = await Connect(api, sb, brume.Id);
        Assert.Equal("Brume", (await Next(a, e => e.Guild?.Guild == GuildNewsKind.Online))!.Guild!.Name);
        // A rank given, then Brume leaves: the log says it all, the newest first.
        await sa.SetGuildRank(aube.Id, brume.Id, GuildRank.Officer, Cancel);
        await sb.LeaveGuild(brume.Id, Cancel);
        IReadOnlyList<GuildEvent> log = await sa.GuildLog(aube.Id, Cancel);
        Assert.Equal([GuildEventKind.Left, GuildEventKind.Rank, GuildEventKind.Joined, GuildEventKind.Joined], log.Select(e => e.Kind));
        Assert.Equal(("Brume", "Aube"), (log[2].Who, log[2].By));
        // Cendre applies and is refused: nothing left, still out of a guild... it leads its own, so it cannot apply at all.
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sc.ApplyToGuild(cendre.Id, "Les Veilleurs", null, Cancel))));
        await sb.ApplyToGuild(brume.Id, "Les Veilleurs", null, Cancel);
        await sa.AnswerApplication(aube.Id, brume.Id, false, Cancel);
        Assert.Empty(await sa.GuildApplications(aube.Id, Cancel));
        Assert.Equal(GuildNewsKind.Left, (await sb.Guild(brume.Id, Cancel)).Guild);
        // An application lasts a week.
        await sb.ApplyToGuild(brume.Id, "Les Brumes", null, Cancel);
        api.Clock.Advance(GuildRules.ApplicationFor + TimeSpan.FromMinutes(1));
        Assert.Empty(await sc.GuildApplications(cendre.Id, Cancel));
        await a.DisposeAsync();
        await b.DisposeAsync();
    }

    [Fact]
    public async Task EachMember_ShowsThePointsItBrought_AndItsAchievementPoints()
    {
        await using var api = new ApiFactory();
        GameServer sa = await api.SignedIn("pt");
        CharacterSummary aube = await sa.CreateCharacter("Aube", "female-c", "guard", cancel: Cancel);
        api.WithDb(db =>
        {
            Rpg.Api.Data.Character ch = db.Characters.Single();
            ch.Shards = GuildRules.Price;
            return db.SaveChanges();
        });
        await sa.FoundGuild(aube.Id, "Les Veilleurs", Cancel);
        // Ten contracts handed back later: two achievements more for the guild (contracts-10 and the waystone of nothing else).
        api.WithDb(db =>
        {
            db.Characters.Single().ContractsDone = 10;
            return db.SaveChanges();
        });
        GuildMember me = (await sa.Guild(aube.Id, Cancel)).View!.Members.Single();
        int points = Achievements.Points(["contracts-10", "guild-member"]);
        Assert.Equal(points, me.AchievementPoints);
        Assert.Equal(GuildRules.AchievementPoints, me.Contributed);
    }
}
