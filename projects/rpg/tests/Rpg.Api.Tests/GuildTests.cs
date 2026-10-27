using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The guilds (sprint 145, D79), with two or three game clients: founding refused without the Éclats or away from the
/// registrar, an invitation accepted, the guild's name seen on the map, <c>/gu</c> heard from another map, ranks given,
/// a member sent out, the title handed over when the leader leaves, the guild gone with its last member.
/// </summary>
public class GuildTests
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

    private static void GiveShards(ApiFactory api, string name, long shards) => api.WithDb(db =>
    {
        db.Characters.Single(c => c.Name == name).Shards = shards;
        return db.SaveChanges();
    });

    private static HttpStatusCode Status(Exception e) => ((ServerException)e).Status;

    [Fact]
    public async Task AGuild_FoundedAtTheRegistrar_Invites_TalksOnGu_AndShowsItsNameOnTheMap()
    {
        await using var api = new ApiFactory();
        GameServer sa = await api.SignedIn("gu"), sb = await api.SignedIn("gu");
        CharacterSummary aube = await sa.CreateCharacter("Aube", "female-c", "guard", cancel: Cancel);
        CharacterSummary brume = await sb.CreateCharacter("Brume", "female-c", "mage", cancel: Cancel);
        string square = GuildRules.RegistrarMap(GameData.Embedded)!;
        Assert.Equal("clairval", square);
        ChatClient a = await Connect(api, sa, aube.Id), b = await Connect(api, sb, brume.Id);
        // Without the Éclats, refused; away from the registrar too.
        GiveShards(api, "Aube", GuildRules.Price - 1);
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sa.FoundGuild(aube.Id, "Les Veilleurs", Cancel))));
        GiveShards(api, "Aube", GuildRules.Price + 5);
        Town woods = GameData.Embedded.Towns.Values.First(t => t.Zone is not null && !Shop.InVillage(t.Id) && GameData.Embedded.QuestToReach(t.Id) is null);
        await sa.SavePlace(aube.Id, new Place(woods.Id, woods.Spawn.X, woods.Spawn.Y), Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sa.FoundGuild(aube.Id, "Les Veilleurs", Cancel))));
        // At the registrar: founded, the Éclats paid, Aube its leader; a bad name refused first.
        Town town = GameData.Embedded.Towns[square];
        await sa.SavePlace(aube.Id, new Place(square, town.Spawn.X, town.Spawn.Y), Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sa.FoundGuild(aube.Id, "les veilleurs", Cancel))));
        CharacterSummary paid = await sa.FoundGuild(aube.Id, "Les Veilleurs", Cancel);
        Assert.Equal(5, paid.Shards);
        GuildView founded = (await Next(a, e => e.Guild is not null))!.Guild!.View!;
        Assert.Equal(("Les Veilleurs", "Aube", GuildRank.Leader), (founded.Name, founded.Members.Single().Name, founded.Members.Single().Rank));
        // Brume, on the same map, sees the guild's name under Aube's.
        Assert.Equal("Les Veilleurs", (await Next(b, e => e.News is { Kind: MapNewsKind.Here, Name: "Aube", Guild: not null }))!.News!.Guild);
        // The same name, whatever its case, cannot be founded twice.
        GiveShards(api, "Brume", GuildRules.Price);
        await sb.SavePlace(brume.Id, new Place(square, town.Spawn.X, town.Spawn.Y), Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sb.FoundGuild(brume.Id, "LES VEILLEURS", Cancel))));
        // Invited, Brume joins; a member cannot invite.
        await sa.InviteToGuild(aube.Id, "Brume", Cancel);
        GuildNews invited = (await Next(b, e => e.Guild?.Guild == GuildNewsKind.Invited))!.Guild!;
        Assert.Equal((aube.Id, "Aube", "Les Veilleurs"), (invited.From, invited.Name, invited.GuildName));
        await sb.JoinGuild(brume.Id, aube.Id, Cancel);
        GuildView two = (await Next(b, e => e.Guild?.View?.Members.Count == 2))!.Guild!.View!;
        Assert.Equal(["Aube", "Brume"], two.Members.Select(m => m.Name));
        Assert.All(two.Members, m => Assert.NotNull(m.LastSeen));
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sb.InviteToGuild(brume.Id, "Aube", Cancel))));
        // /gu from another map: heard by both, not by someone of the map outside the guild.
        GameServer sc = await api.SignedIn("gu");
        CharacterSummary cendre = await sc.CreateCharacter("Cendre", "male-c", "mage", cancel: Cancel);
        ChatClient c = await Connect(api, sc, cendre.Id);
        await sb.SavePlace(brume.Id, new Place(woods.Id, woods.Spawn.X, woods.Spawn.Y), Cancel);
        Assert.True(await a.Say("on se retrouve ?", ChatChannels.Guild, Cancel));
        ChatLine heard = (await Next(b, e => e.Line is not null))!.Line!;
        Assert.Equal((ChatChannels.Guild, "Aube", "on se retrouve ?"), (heard.Channel, heard.From, heard.Text));
        Assert.Equal(ChatChannels.Guild, (await Next(a, e => e.Line is not null))!.Line!.Channel);
        Assert.Null(await Next(c, e => e.Line is not null));
        // Out of a guild, /gu is refused.
        Assert.True(await c.Say("allô", ChatChannels.Guild, Cancel));
        Assert.Equal("Not in a guild.", (await Next(c, e => e.Refusal is not null))!.Refusal);
        await a.DisposeAsync();
        await b.DisposeAsync();
        await c.DisposeAsync();
    }

    [Fact]
    public async Task Ranks_Kicks_AndTheTitleHandedOver_UpToTheLastMemberWhoEndsTheGuild()
    {
        await using var api = new ApiFactory();
        GameServer sa = await api.SignedIn("gr"), sb = await api.SignedIn("gr"), sc = await api.SignedIn("gr");
        CharacterSummary aube = await sa.CreateCharacter("Aube", "female-c", "guard", cancel: Cancel);
        CharacterSummary brume = await sb.CreateCharacter("Brume", "female-c", "mage", cancel: Cancel);
        CharacterSummary cendre = await sc.CreateCharacter("Cendre", "male-c", "mage", cancel: Cancel);
        ChatClient a = await Connect(api, sa, aube.Id), b = await Connect(api, sb, brume.Id), c = await Connect(api, sc, cendre.Id);
        GiveShards(api, "Aube", GuildRules.Price);
        await sa.FoundGuild(aube.Id, "Cœur-de-Chêne", Cancel);
        foreach ((GameServer s, CharacterSummary who, ChatClient chat) in new[] { (sb, brume, b), (sc, cendre, c) })
        {
            await sa.InviteToGuild(aube.Id, who.Name, Cancel);
            Assert.NotNull(await Next(chat, e => e.Guild?.Guild == GuildNewsKind.Invited));
            await s.JoinGuild(who.Id, aube.Id, Cancel);
            api.Clock.Advance(TimeSpan.FromMinutes(1));
        }
        // An invitation is used once.
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sc.JoinGuild(cendre.Id, aube.Id, Cancel))));
        // Cendre, who joined last, made an officer; an officer cannot give ranks nor send another officer out.
        await sa.SetGuildRank(aube.Id, cendre.Id, GuildRank.Officer, Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sc.SetGuildRank(cendre.Id, brume.Id, GuildRank.Officer, Cancel))));
        Assert.Equal(HttpStatusCode.BadRequest, Status(await Assert.ThrowsAsync<ServerException>(() => sc.KickFromGuild(cendre.Id, aube.Id, Cancel))));
        // Aube leaves: the officer leads, though Brume joined before.
        await sa.LeaveGuild(aube.Id, Cancel);
        Assert.Equal(GuildNewsKind.Left, (await Next(a, e => e.Guild?.Guild == GuildNewsKind.Left))!.Guild!.Guild);
        GuildView after = (await sb.Guild(brume.Id, Cancel)).View!;
        Assert.Equal([("Cendre", GuildRank.Leader), ("Brume", GuildRank.Member)], after.Members.Select(m => (m.Name, m.Rank)));
        // The new leader hands its title to Brume and stays an officer; Brume, leading now, sends Cendre out.
        await sc.SetGuildRank(cendre.Id, brume.Id, GuildRank.Leader, Cancel);
        Assert.Equal(GuildRank.Officer, (await sc.Guild(cendre.Id, Cancel)).View!.Members.Single(m => m.Name == "Cendre").Rank);
        await sb.KickFromGuild(brume.Id, cendre.Id, Cancel);
        Assert.NotNull(await Next(c, e => e.Guild?.Guild == GuildNewsKind.Left));
        Assert.Equal(GuildNewsKind.Left, (await sc.Guild(cendre.Id, Cancel)).Guild);
        // The last one leaves: the guild is gone, its name free again.
        await sb.LeaveGuild(brume.Id, Cancel);
        Assert.Equal(0, api.WithDb(db => db.Guilds.Count()));
        GiveShards(api, "Cendre", GuildRules.Price);
        await sc.FoundGuild(cendre.Id, "Cœur-de-Chêne", Cancel);
        await a.DisposeAsync();
        await b.DisposeAsync();
        await c.DisposeAsync();
    }

    [Fact]
    public async Task ALeaderDeleted_HandsTheTitleOver_AsOneWhoLeaves()
    {
        await using var api = new ApiFactory();
        GameServer sa = await api.SignedIn("gd"), sb = await api.SignedIn("gd");
        CharacterSummary aube = await sa.CreateCharacter("Aube", "female-c", "guard", cancel: Cancel);
        CharacterSummary brume = await sb.CreateCharacter("Brume", "female-c", "mage", cancel: Cancel);
        ChatClient b = await Connect(api, sb, brume.Id);
        GiveShards(api, "Aube", GuildRules.Price);
        await sa.FoundGuild(aube.Id, "Les Veilleurs", Cancel);
        await sa.InviteToGuild(aube.Id, "Brume", Cancel);
        await sb.JoinGuild(brume.Id, aube.Id, Cancel);
        var lobby = new Lobby(sa);
        Assert.True(await lobby.Delete(aube.Id, Cancel));
        GuildView left = (await sb.Guild(brume.Id, Cancel)).View!;
        Assert.Equal(("Brume", GuildRank.Leader), (left.Members.Single().Name, left.Members.Single().Rank));
        Assert.Single(api.WithDb(db => db.GuildMembers.ToList()));
        await b.DisposeAsync();
    }

    [Fact]
    public async Task EightAcceptancesAtOnce_IntoAGuildOf45_LetFiveIn()
    {
        // Sprint 152 (D83, the limit of T112): the guild's row locked while it is counted.
        await using var api = new ApiFactory();
        var guild = new Rpg.Api.Data.GuildRecord { Name = "Les Veilleurs", NormalizedName = "LES VEILLEURS", CreatedAt = DateTimeOffset.UnixEpoch };
        Guid leader = Guid.Empty;
        api.WithDb(db =>
        {
            var account = new Rpg.Api.Data.Account { Name = "veille", NormalizedName = "VEILLE" };
            db.Accounts.Add(account);
            db.Guilds.Add(guild);
            for (int i = 0; i < 45; i++)
            {
                // Names of letters only, one a member.
                string name = "Veilleur" + new string((char)('a' + (i / 26)), 1) + (char)('a' + (i % 26));
                var c = new Rpg.Api.Data.Character { AccountId = account.Id, Name = name, NormalizedName = name.ToUpperInvariant(), Look = "female-c", Class = "guard" };
                db.Characters.Add(c);
                db.GuildMembers.Add(new Rpg.Api.Data.GuildMemberRecord { CharacterId = c.Id, GuildId = guild.Id, Rank = i == 0 ? GuildRank.Leader : GuildRank.Member, JoinedAt = DateTimeOffset.UnixEpoch.AddMinutes(i) });
                if (i == 0)
                    leader = c.Id;
            }
            return db.SaveChanges();
        });
        var invites = api.Services.GetRequiredService<GuildInvites>();
        var joiners = new List<(GameServer Server, Guid Id)>();
        foreach (string name in new[] { "Aube", "Brume", "Cendre", "Dune", "Ecume", "Flamme", "Givre", "Houle" })
        {
            GameServer server = await api.SignedIn(name.ToLowerInvariant());
            CharacterSummary c = await server.CreateCharacter(name, "female-c", "mage", cancel: Cancel);
            invites.Invite(leader, c.Id, guild.Id);
            joiners.Add((server, c.Id));
        }
        Task[] accepts = [.. joiners.Select(j => j.Server.JoinGuild(j.Id, leader, Cancel))];
        try
        {
            await Task.WhenAll(accepts);
        }
        catch (ServerException)
        {
            // The three too late are refused; counted below.
        }
        Assert.Equal(5, accepts.Count(t => t.IsCompletedSuccessfully));
        Assert.Equal(GuildRules.Most, api.WithDb(db => db.GuildMembers.Count(m => m.GuildId == guild.Id)));
    }
}
