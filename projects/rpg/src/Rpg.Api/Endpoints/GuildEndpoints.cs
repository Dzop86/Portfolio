using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api.Endpoints;

/// <summary>
/// The guilds (sprint 145, D79): founded at Clairval's registrar for <see cref="GuildRules.Price"/> Éclats, three ranks,
/// <see cref="GuildRules.Most"/> members at most; invite (leader and officers), accept within a minute, leave, send out,
/// give a rank or hand the title over. All of it in the database; what changes is told to the members in play on their
/// map's socket (<see cref="GuildNews"/>), and the guild's name to the map of whoever joins or leaves one.
/// </summary>
public static class GuildEndpoints
{
    public static void MapGuildEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/characters").WithTags("Guilds").RequireAuthorization();
        group.MapGet("/{id:guid}/guild", GuildOf).WithSummary("The guild of one of the player's characters: its name and its members, or none.");
        group.MapPost("/{id:guid}/guild", Found).WithSummary($"Founds a guild at Clairval's registrar, for {GuildRules.Price} Éclats, under a name unique on the server.");
        group.MapPost("/{id:guid}/guild/invite", Invite).WithSummary("Invites a character in play, by its name, into the guild (its leader and officers invite).");
        group.MapPost("/{id:guid}/guild/accept", Join).WithSummary("Joins the guild of a member who invited this character, within a minute.");
        group.MapPost("/{id:guid}/guild/leave", Leave).WithSummary("Leaves the guild; a leader hands its title to the oldest officer, else the oldest member; the last one ends it.");
        group.MapPost("/{id:guid}/guild/kick", Kick).WithSummary("Sends a member out: the leader anyone, an officer members only.");
        group.MapPost("/{id:guid}/guild/rank", Rank).WithSummary("The leader names an officer or a member, or hands its title over.");
        group.MapPost("/{id:guid}/guild/notice", Notice).WithSummary("The leader writes the guild's message and the week's goal.");
        group.MapPost("/{id:guid}/guild/crest", Crest).WithSummary("The leader composes the guild's crest: a shape, a pictogram, two colours of the charter.");
        group.MapGet("/{id:guid}/guild/chest", Chest).WithSummary("The guild's chest at Edmée's: its tabs, the 100 last movements, one's rank.");
        group.MapPost("/{id:guid}/guild/chest/deposit", Deposit).WithSummary("Puts items of the bag in a tab of the chest, at Edmée's (every member).");
        group.MapPost("/{id:guid}/guild/chest/withdraw", Withdraw).WithSummary("Takes items out of a tab, at Edmée's: the leader and the officers, the members from a tab opened to them.");
        group.MapPost("/{id:guid}/guild/chest/tab", OpenTab).WithSummary("The leader opens a tab of the chest to the members, or closes it.");
        group.MapPost("/{id:guid}/guild/note", Note).WithSummary("Writes one's note in the guild's list; the leader and the officers clear another's.");
        group.MapGet("/{id:guid}/guild/log", Log).WithSummary("The guild's log: its last hundred events, the newest first.");
        group.MapPost("/{id:guid}/guild/apply", Apply).WithSummary("Applies to a guild by its name, with a word (out of any guild, three applications waiting at most).");
        group.MapGet("/{id:guid}/guild/applications", Applications).WithSummary("The applications to one's guild (its leader), or one's own (out of a guild).");
        group.MapPost("/{id:guid}/guild/applications/answer", Answer).WithSummary("The leader accepts an application (the character joins) or refuses it.");
        group.MapPost("/{id:guid}/guild/applications/withdraw", WithdrawApplication).WithSummary("Withdraws one's application to a guild.");
    }

    private static ValidationProblem Refused(string field, string why) => TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [why] });

    private static Task<Character?> Mine(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = CharacterEndpoints.AccountId(user);
        return db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
    }

    private static Task<GuildMemberRecord?> MembershipOf(Guid character, GameDb db, CancellationToken cancel) =>
        db.GuildMembers.FirstOrDefaultAsync(m => m.CharacterId == character, cancel);

    // The guild as its members see it: each member's rank, level, class, zone when in play and last time in play; where
    // the guild stands (sprint 156).
    private static async Task<GuildView?> ViewOf(Guid guild, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await db.Guilds.AsNoTracking().FirstOrDefaultAsync(g => g.Id == guild, cancel) is not GuildRecord record)
            return null;
        // Since sprint 160 (D84): each one's note, the points it brought, its achievement points.
        var rows = await (from m in db.GuildMembers.AsNoTracking()
                          join c in db.Characters.AsNoTracking() on m.CharacterId equals c.Id
                          where m.GuildId == guild
                          select new { Character = c, m.Rank, m.Note, m.Contributed }).ToListAsync(cancel);
        return new GuildView(guild, record.Name, GuildRules.Ordered(rows.Select(r => new GuildMember(r.Character.Id, r.Character.Name, r.Rank, r.Character.Level, r.Character.Class,
                hub.MapOf(r.Character.Id) is string map ? Contacts.ZoneOf(map, GameData.Embedded) : null, r.Character.LastSeenAt)
            {
                Note = r.Note,
                Contributed = r.Contributed,
                AchievementPoints = Achievements.Points(Achievements.Earned(AchievementEndpoints.StateOf(r.Character, true), GameData.Embedded)),
            })),
            GuildRules.Standing(record.Points, record.WeekPoints, record.Week, GuildRules.Week(clock.GetUtcNow()), record.Goal, record.Message, GuildCrests.Read(record.Crest)));
    }

    // The guild as it now is, told to each of its members in play.
    private static async Task TellGuild(Guid guild, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await ViewOf(guild, db, hub, clock, cancel) is not GuildView view)
            return;
        var news = new GuildNews(GuildNewsKind.Changed, View: view);
        foreach (GuildMember m in view.Members)
            await hub.TellCharacter(m.Id, news, cancel);
    }

    internal static async Task<Results<Ok<GuildNews>, NotFound>> GuildOf(Guid id, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        // Its achievements not yet counted for the guild count now (sprint 156).
        await CreditAchievements(me, db, hub, clock, cancel);
        return TypedResults.Ok(await MembershipOf(id, db, cancel) is GuildMemberRecord m && await ViewOf(m.GuildId, db, hub, clock, cancel) is GuildView view
            ? new GuildNews(GuildNewsKind.Changed, View: view)
            : new GuildNews(GuildNewsKind.Left));
    }

    /// <summary>Founds a guild: at the registrar's map, out of any guild, a free name, the Éclats paid; its founder leads it.</summary>
    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Found(
        Guid id, NewGuild request, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        // A character that never walked stands on the village square (as its socket says, sprint 84).
        if ((me.Town ?? "clairval") != GuildRules.RegistrarMap(GameData.Embedded))
            return Refused("name", "Guilds are founded at Clairval's registrar.");
        string? name = request?.Name?.Trim();
        bool taken = name is not null && await db.Guilds.AnyAsync(g => g.NormalizedName == GuildRules.Normalize(name), cancel);
        if (GuildRules.WhyNotFound(name, taken, me.Shards, await MembershipOf(id, db, cancel) is not null) is string why)
            return Refused("name", why);
        DateTimeOffset now = clock.GetUtcNow();
        var guild = new GuildRecord { Name = name!, NormalizedName = GuildRules.Normalize(name!), CreatedAt = now };
        db.Guilds.Add(guild);
        // Its achievements so far are its own, not the guild's (sprint 156): only the next ones count.
        db.GuildMembers.Add(new GuildMemberRecord { CharacterId = id, GuildId = guild.Id, Rank = GuildRank.Leader, JoinedAt = now, Credited = await EarnedInGuild(me, db, cancel) });
        me.Shards -= GuildRules.Price;
        try
        {
            await db.SaveChangesAsync(cancel);
        }
        catch (DbUpdateException)
        {
            // The same name founded at the same moment by someone else: the unique index decides.
            return Refused("name", "A guild already has that name.");
        }
        await AppliedNoMore(id, db, cancel);
        await Record(guild.Id, GuildEventKind.Joined, me.Name, null, null, db, clock, cancel);
        await hub.SetGuild(id, guild.Id, guild.Name, GuildCrests.Write(GuildCrests.Default), cancel);
        await TellGuild(guild.Id, db, hub, clock, cancel);
        return TypedResults.Ok(CharacterEndpoints.Summary(me));
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Invite(
        Guid id, PartyInvite request, ClaimsPrincipal user, GameDb db, GuildInvites invites, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (await MembershipOf(id, db, cancel) is not GuildMemberRecord mine)
            return Refused("name", "Not in a guild.");
        var to = await db.Characters.AsNoTracking().Where(c => c.Name == request.Name && c.Server == me.Server).Select(c => new { c.Id }).FirstOrDefaultAsync(cancel);
        if (to is null || hub.MapOf(to.Id) is null)
            return Refused("name", "Nobody in play by that name.");
        if (await SocialEndpoints.Ignores(db, to.Id, id, cancel))
            return Refused("name", "They take no invitation from this character.");
        int count = await db.GuildMembers.CountAsync(m => m.GuildId == mine.GuildId, cancel);
        if (GuildRules.WhyNotInvite(mine.Rank, count, await db.GuildMembers.AnyAsync(m => m.CharacterId == to.Id, cancel)) is string why)
            return Refused("name", why);
        string guildName = await db.Guilds.Where(g => g.Id == mine.GuildId).Select(g => g.Name).FirstAsync(cancel);
        invites.Invite(id, to.Id, mine.GuildId);
        await hub.TellCharacter(to.Id, new GuildNews(GuildNewsKind.Invited, id, me.Name, guildName), cancel);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Join(
        Guid id, PartyAnswer request, ClaimsPrincipal user, GameDb db, GuildInvites invites, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (invites.Take(id, request?.From ?? Guid.Empty) is not Guid guild
            || await db.Guilds.AsNoTracking().Where(g => g.Id == guild).Select(g => new { g.Name, g.Crest }).FirstOrDefaultAsync(cancel) is not { } joined)
            return Refused("from", "No invitation from them, or too late.");
        if (await MembershipOf(id, db, cancel) is not null)
            return Refused("from", "Already in a guild.");
        string? by = await db.Characters.Where(c => c.Id == request!.From).Select(c => c.Name).FirstOrDefaultAsync(cancel);
        return await Enter(me, guild, joined.Name, joined.Crest, by, db, hub, clock, cancel) is string why ? Refused("from", why) : TypedResults.NoContent();
    }

    /// <summary>
    /// A character into a guild (invited, or its application accepted, sprint 160): the guild's row locked while it is counted
    /// (sprint 152, D83: two acceptances at once no longer make 51); its applications gone; the log, its map and the guild told.
    /// </summary>
    private static async Task<string?> Enter(Character me, Guid guild, string name, string crest, string? by, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        await using (var transaction = await db.Database.BeginTransactionAsync(cancel))
        {
            await db.Guilds.FromSql($"SELECT * FROM guilds WHERE \"Id\" = {guild} FOR UPDATE").ToListAsync(cancel);
            if (await db.GuildMembers.CountAsync(m => m.GuildId == guild, cancel) >= GuildRules.Most)
                return $"The guild is full ({GuildRules.Most}).";
            if (await db.GuildMembers.AnyAsync(m => m.CharacterId == me.Id, cancel))
                return "Already in a guild.";
            db.GuildMembers.Add(new GuildMemberRecord { CharacterId = me.Id, GuildId = guild, Rank = GuildRank.Member, JoinedAt = clock.GetUtcNow(), Credited = await EarnedInGuild(me, db, cancel) });
            await db.SaveChangesAsync(cancel);
            await transaction.CommitAsync(cancel);
        }
        await AppliedNoMore(me.Id, db, cancel);
        await Record(guild, GuildEventKind.Joined, me.Name, by, null, db, clock, cancel);
        await hub.SetGuild(me.Id, guild, name, GuildCrests.Write(GuildCrests.Read(crest)), cancel);
        await TellGuild(guild, db, hub, clock, cancel);
        return null;
    }

    // A character in a guild has no application waiting any more.
    private static Task<int> AppliedNoMore(Guid character, GameDb db, CancellationToken cancel) =>
        db.GuildApplications.Where(a => a.CharacterId == character).ExecuteDeleteAsync(cancel);

    /// <summary>An event of the guild's log (sprint 160), written once; the oldest beyond the hundred are dropped.</summary>
    private static async Task Record(Guid guild, GuildEventKind kind, string who, string? by, string? value, GameDb db, TimeProvider clock, CancellationToken cancel)
    {
        db.GuildEvents.Add(new GuildEventRecord { GuildId = guild, Kind = kind, Who = who, By = by, Value = value, At = clock.GetUtcNow() });
        await db.SaveChangesAsync(cancel);
        long? keep = await db.GuildEvents.Where(e => e.GuildId == guild).OrderByDescending(e => e.Id).Skip(GuildRules.LogLength - 1).Select(e => (long?)e.Id).FirstOrDefaultAsync(cancel);
        if (keep is long oldest)
            await db.GuildEvents.Where(e => e.GuildId == guild && e.Id < oldest).ExecuteDeleteAsync(cancel);
    }

    /// <summary>
    /// A character out of its guild (it leaves, or is deleted): a leader hands its title over (<see cref="GuildRules.Successor"/>),
    /// the last one ends the guild; it is told it left, the others the guild as it is. Nothing when in no guild.
    /// </summary>
    internal static async Task Depart(Guid character, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel, string? sentOutBy = null)
    {
        if (await MembershipOf(character, db, cancel) is not GuildMemberRecord mine)
            return;
        string who = await db.Characters.Where(c => c.Id == character).Select(c => c.Name).FirstOrDefaultAsync(cancel) ?? "?";
        db.GuildMembers.Remove(mine);
        List<GuildMemberRecord> others = await db.GuildMembers.Where(m => m.GuildId == mine.GuildId && m.CharacterId != character).ToListAsync(cancel);
        if (mine.Rank == GuildRank.Leader && GuildRules.Successor(others.Select(o => (o.CharacterId, o.Rank, o.JoinedAt))) is Guid next)
            others.First(o => o.CharacterId == next).Rank = GuildRank.Leader;
        await db.SaveChangesAsync(cancel);
        if (others.Count == 0)
            await db.Guilds.Where(g => g.Id == mine.GuildId).ExecuteDeleteAsync(cancel);
        else
            await Record(mine.GuildId, sentOutBy is null ? GuildEventKind.Left : GuildEventKind.Kicked, who, sentOutBy, null, db, clock, cancel);
        await hub.SetGuild(character, null, null, null, cancel);
        await hub.TellCharacter(character, new GuildNews(GuildNewsKind.Left), cancel);
        await TellGuild(mine.GuildId, db, hub, clock, cancel);
    }

    internal static async Task<Results<NoContent, NotFound>> Leave(Guid id, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        await Depart(id, db, hub, clock, cancel);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Kick(
        Guid id, GuildOrder request, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (await MembershipOf(id, db, cancel) is not GuildMemberRecord mine)
            return Refused("member", "Not in a guild.");
        Guid member = request?.Member ?? Guid.Empty;
        if (await MembershipOf(member, db, cancel) is not GuildMemberRecord them || them.GuildId != mine.GuildId)
            return Refused("member", "Not a member of the guild.");
        if (GuildRules.WhyNotKick(mine.Rank, them.Rank, member == id) is string why)
            return Refused("member", why);
        await Depart(member, db, hub, clock, cancel, me.Name);
        return TypedResults.NoContent();
    }

    /// <summary>The leader gives a rank: an officer or a member; the leader's rank hands its title over, the leader an officer from then on.</summary>
    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Rank(
        Guid id, GuildOrder request, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (await MembershipOf(id, db, cancel) is not GuildMemberRecord mine)
            return Refused("member", "Not in a guild.");
        Guid member = request?.Member ?? Guid.Empty;
        GuildRank rank = request?.Rank ?? GuildRank.Member;
        if (await MembershipOf(member, db, cancel) is not GuildMemberRecord them || them.GuildId != mine.GuildId)
            return Refused("member", "Not a member of the guild.");
        if (GuildRules.WhyNotRank(mine.Rank, them.Rank, rank, member == id) is string why)
            return Refused("member", why);
        them.Rank = rank;
        if (rank == GuildRank.Leader)
            mine.Rank = GuildRank.Officer;
        await db.SaveChangesAsync(cancel);
        string who = await db.Characters.Where(c => c.Id == member).Select(c => c.Name).FirstAsync(cancel);
        await Record(mine.GuildId, rank == GuildRank.Leader ? GuildEventKind.Leader : GuildEventKind.Rank, who, me.Name, rank.ToString(), db, clock, cancel);
        await TellGuild(mine.GuildId, db, hub, clock, cancel);
        return TypedResults.NoContent();
    }

    // The achievements a character has once in a guild (that of joining one counted): those it brings, not the guild's.
    private static async Task<int> EarnedInGuild(Character c, GameDb db, CancellationToken cancel) =>
        Achievements.Earned(await AchievementEndpoints.StateOf(c, db, cancel) with { InGuild = true }, GameData.Embedded).Count;

    /// <summary>
    /// Points for the guild of a character (sprint 156, D83): a contract, an achievement, a dungeon done together. One
    /// statement, so that two members at once both count; then the members told where the guild stands.
    /// </summary>
    internal static async Task Credit(Guid character, int points, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (points <= 0 || await db.GuildMembers.AsNoTracking().Where(m => m.CharacterId == character).Select(m => (Guid?)m.GuildId).FirstOrDefaultAsync(cancel) is not Guid guild)
            return;
        // The member's own share (sprint 160), in the same single statements.
        await db.GuildMembers.Where(m => m.CharacterId == character).ExecuteUpdateAsync(m => m.SetProperty(p => p.Contributed, p => p.Contributed + points), cancel);
        await CreditGuild(guild, points, db, hub, clock, cancel);
    }

    /// <summary>Points for a guild itself (a dungeon finished by two of its members or more).</summary>
    internal static async Task CreditGuild(Guid guild, int points, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        int week = GuildRules.Week(clock.GetUtcNow());
        long before = await db.Guilds.Where(g => g.Id == guild).Select(g => g.Points).FirstOrDefaultAsync(cancel);
        await db.Guilds.Where(g => g.Id == guild).ExecuteUpdateAsync(g => g
            .SetProperty(p => p.Points, p => p.Points + points)
            .SetProperty(p => p.WeekPoints, p => p.Week == week ? p.WeekPoints + points : points)
            .SetProperty(p => p.Week, week), cancel);
        // A level reached goes to the log (sprint 160).
        if (GuildRules.LevelFor(before + points) is int level && level > GuildRules.LevelFor(before))
            await Record(guild, GuildEventKind.Level, level.ToString(System.Globalization.CultureInfo.InvariantCulture), null, null, db, clock, cancel);
        await TellGuild(guild, db, hub, clock, cancel);
    }

    /// <summary>
    /// The achievements a member earned since last counted bring its guild 5 points each (sprint 156); looked at after a
    /// fight, a contract, and when the guild's or the achievements' window asks. Counted once, even asked twice at once.
    /// </summary>
    internal static async Task CreditAchievements(Character c, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await db.GuildMembers.AsNoTracking().FirstOrDefaultAsync(m => m.CharacterId == c.Id, cancel) is not GuildMemberRecord m)
            return;
        int earned = Achievements.Earned(await AchievementEndpoints.StateOf(c, db, cancel), GameData.Embedded).Count;
        if (earned <= m.Credited
            || await db.GuildMembers.Where(x => x.CharacterId == c.Id && x.Credited == m.Credited).ExecuteUpdateAsync(x => x.SetProperty(p => p.Credited, earned), cancel) == 0)
            return;
        int points = GuildRules.AchievementPoints * (earned - m.Credited);
        await db.GuildMembers.Where(x => x.CharacterId == c.Id).ExecuteUpdateAsync(x => x.SetProperty(p => p.Contributed, p => p.Contributed + points), cancel);
        await CreditGuild(m.GuildId, points, db, hub, clock, cancel);
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Notice(
        Guid id, GuildNotice request, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (await MembershipOf(id, db, cancel) is not GuildMemberRecord mine)
            return Refused("message", "Not in a guild.");
        string? message = string.IsNullOrWhiteSpace(request?.Message) ? null : request.Message.Trim();
        if (GuildRules.WhyNotNotice(mine.Rank, message, request?.Goal ?? 0) is string why)
            return Refused("message", why);
        string? before = await db.Guilds.Where(g => g.Id == mine.GuildId).Select(g => g.Message).FirstAsync(cancel);
        await db.Guilds.Where(g => g.Id == mine.GuildId).ExecuteUpdateAsync(g => g.SetProperty(p => p.Message, message).SetProperty(p => p.Goal, request!.Goal), cancel);
        if (before != message)
            await Record(mine.GuildId, GuildEventKind.Message, me.Name, null, null, db, clock, cancel);
        await TellGuild(mine.GuildId, db, hub, clock, cancel);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Crest(
        Guid id, GuildCrest request, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (await MembershipOf(id, db, cancel) is not GuildMemberRecord mine)
            return Refused("crest", "Not in a guild.");
        if (GuildRules.WhyNotCrest(mine.Rank, request) is string why)
            return Refused("crest", why);
        string crest = GuildCrests.Write(request);
        await db.Guilds.Where(g => g.Id == mine.GuildId).ExecuteUpdateAsync(g => g.SetProperty(p => p.Crest, crest), cancel);
        await hub.SetCrest(mine.GuildId, crest, cancel);
        await Record(mine.GuildId, GuildEventKind.Crest, me.Name, null, null, db, clock, cancel);
        await TellGuild(mine.GuildId, db, hub, clock, cancel);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<ChestView>, NotFound, ValidationProblem>> Chest(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        if (await MembershipOf(id, db, cancel) is not GuildMemberRecord mine)
            return Refused("tab", "Not in a guild.");
        GuildRecord guild = await db.Guilds.AsNoTracking().FirstAsync(g => g.Id == mine.GuildId, cancel);
        return TypedResults.Ok(new ChestView(GuildChests.Read(guild.Chest, guild.Level), mine.Rank));
    }

    internal static Task<Results<Ok<ChestView>, NotFound, ValidationProblem>> Deposit(Guid id, ChestOrder request, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel) =>
        AtTheChest(id, user, db, (me, mine, chest) =>
        {
            int owned = me.InventoryList.FirstOrDefault(i => i.Item == request?.Item)?.Count ?? 0;
            if (GuildChests.WhyNotDeposit(chest, request, owned, GameData.Embedded) is string why)
                return (null, why);
            me.Spend(new ItemCount(request!.Item!, request.Count));
            return (GuildChests.Move(chest, request.Tab, request.Item!, request.Count, me.Name, clock.GetUtcNow()), null);
        }, cancel);

    internal static Task<Results<Ok<ChestView>, NotFound, ValidationProblem>> Withdraw(Guid id, ChestOrder request, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel) =>
        AtTheChest(id, user, db, (me, mine, chest) =>
        {
            if (GuildChests.WhyNotWithdraw(chest, request, mine.Rank) is string why)
                return (null, why);
            me.Receive([new ItemCount(request.Item!, request.Count)]);
            return (GuildChests.Move(chest, request.Tab, request.Item!, -request.Count, me.Name, clock.GetUtcNow()), null);
        }, cancel);

    internal static Task<Results<Ok<ChestView>, NotFound, ValidationProblem>> OpenTab(Guid id, ChestTabOrder request, ClaimsPrincipal user, GameDb db, CancellationToken cancel) =>
        AtTheChest(id, user, db, (me, mine, chest) =>
            GuildChests.WhyNotOpen(chest, request, mine.Rank) is string why ? (null, why) : (GuildChests.Open(chest, request.Tab, request.Open), null), cancel);

    /// <summary>
    /// One change to the chest at Edmée's (the registrar's map), the guild's row locked while it is read and written:
    /// two members at once neither lose a movement nor take the same stack twice.
    /// </summary>
    private static async Task<Results<Ok<ChestView>, NotFound, ValidationProblem>> AtTheChest(
        Guid id, ClaimsPrincipal user, GameDb db, Func<Character, GuildMemberRecord, GuildChest, (GuildChest? Chest, string? Why)> change, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (await MembershipOf(id, db, cancel) is not GuildMemberRecord mine)
            return Refused("tab", "Not in a guild.");
        if ((me.Town ?? "clairval") != GuildRules.RegistrarMap(GameData.Embedded))
            return Refused("tab", "The guild's chest is at Edmée's, on Clairval's square.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancel);
        GuildRecord guild = (await db.Guilds.FromSql($"SELECT * FROM guilds WHERE \"Id\" = {mine.GuildId} FOR UPDATE").ToListAsync(cancel)).Single();
        (GuildChest? chest, string? why) = change(me, mine, GuildChests.Read(guild.Chest, guild.Level));
        if (chest is null)
            return Refused("tab", why!);
        guild.Chest = GuildChests.Write(chest);
        await db.SaveChangesAsync(cancel);
        await transaction.CommitAsync(cancel);
        return TypedResults.Ok(new ChestView(chest, mine.Rank, CharacterEndpoints.Summary(me)));
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Note(
        Guid id, GuildNote request, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        if (await MembershipOf(id, db, cancel) is not GuildMemberRecord mine)
            return Refused("note", "Not in a guild.");
        Guid member = request?.Member ?? Guid.Empty;
        if (await MembershipOf(member, db, cancel) is not GuildMemberRecord them || them.GuildId != mine.GuildId)
            return Refused("note", "Not a member of the guild.");
        string? note = string.IsNullOrWhiteSpace(request?.Note) ? null : request.Note.Trim();
        if (GuildRules.WhyNotNote(mine.Rank, member == id, note) is string why)
            return Refused("note", why);
        them.Note = note;
        await db.SaveChangesAsync(cancel);
        await TellGuild(mine.GuildId, db, hub, clock, cancel);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<IReadOnlyList<GuildEvent>>, NotFound, ValidationProblem>> Log(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        if (await MembershipOf(id, db, cancel) is not GuildMemberRecord mine)
            return Refused("log", "Not in a guild.");
        IReadOnlyList<GuildEvent> log = await db.GuildEvents.AsNoTracking().Where(e => e.GuildId == mine.GuildId).OrderByDescending(e => e.Id).Take(GuildRules.LogLength)
            .Select(e => new GuildEvent(e.Kind, e.Who, e.By, e.Value, e.At)).ToListAsync(cancel);
        return TypedResults.Ok(log);
    }

    /// <summary>
    /// An application (sprint 160, D84, Charles: « un joueur peut postuler dans la guilde, le chef a accès aux candidatures »):
    /// out of any guild, to a guild of its server by its name, three waiting at most, a week long; its leader told if in play.
    /// </summary>
    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Apply(
        Guid id, GuildApply request, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        DateTimeOffset now = clock.GetUtcNow();
        await db.GuildApplications.Where(a => a.At < now - GuildRules.ApplicationFor).ExecuteDeleteAsync(cancel);
        string name = request?.Name?.Trim() ?? "";
        GuildRecord? guild = name.Length == 0 ? null : await db.Guilds.AsNoTracking().FirstOrDefaultAsync(g => g.NormalizedName == GuildRules.Normalize(name), cancel);
        string? word = string.IsNullOrWhiteSpace(request?.Message) ? null : request.Message.Trim();
        int waiting = await db.GuildApplications.CountAsync(a => a.CharacterId == id, cancel);
        bool already = guild is not null && await db.GuildApplications.AnyAsync(a => a.CharacterId == id && a.GuildId == guild.Id, cancel);
        int count = guild is null ? 0 : await db.GuildMembers.CountAsync(m => m.GuildId == guild.Id, cancel);
        if (GuildRules.WhyNotApply(await MembershipOf(id, db, cancel) is not null, guild is not null, waiting, already, count, word) is string why)
            return Refused("name", why);
        db.GuildApplications.Add(new GuildApplicationRecord { CharacterId = id, GuildId = guild!.Id, Message = word, At = now });
        try
        {
            await db.SaveChangesAsync(cancel);
        }
        catch (DbUpdateException)
        {
            return Refused("name", "Already applied to that guild.");
        }
        if (await db.GuildMembers.AsNoTracking().Where(m => m.GuildId == guild.Id && m.Rank == GuildRank.Leader).Select(m => (Guid?)m.CharacterId).FirstOrDefaultAsync(cancel) is Guid leader)
            await hub.TellCharacter(leader, new GuildNews(GuildNewsKind.Applied, id, me.Name, guild.Name), cancel);
        return TypedResults.NoContent();
    }

    /// <summary>The applications to one's guild, for its leader; out of a guild, one's own; the oldest first, a week long.</summary>
    internal static async Task<Results<Ok<IReadOnlyList<GuildApplication>>, NotFound, ValidationProblem>> Applications(
        Guid id, ClaimsPrincipal user, GameDb db, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        DateTimeOffset since = clock.GetUtcNow() - GuildRules.ApplicationFor;
        GuildMemberRecord? mine = await MembershipOf(id, db, cancel);
        if (mine is not null && GuildRules.WhyNotAnswer(mine.Rank) is string why)
            return Refused("guild", why);
        IReadOnlyList<GuildApplication> list = await (from a in db.GuildApplications.AsNoTracking()
                                                      join c in db.Characters.AsNoTracking() on a.CharacterId equals c.Id
                                                      join g in db.Guilds.AsNoTracking() on a.GuildId equals g.Id
                                                      where a.At >= since && (mine == null ? a.CharacterId == id : a.GuildId == mine.GuildId)
                                                      orderby a.At
                                                      select new GuildApplication(c.Id, c.Name, c.Level, c.Class, g.Id, g.Name, a.Message, a.At)).ToListAsync(cancel);
        return TypedResults.Ok(list);
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Answer(
        Guid id, GuildAnswer request, ClaimsPrincipal user, GameDb db, ChatHub hub, TimeProvider clock, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (await MembershipOf(id, db, cancel) is not GuildMemberRecord mine)
            return Refused("character", "Not in a guild.");
        if (GuildRules.WhyNotAnswer(mine.Rank) is string why)
            return Refused("character", why);
        Guid applicant = request?.Character ?? Guid.Empty;
        DateTimeOffset since = clock.GetUtcNow() - GuildRules.ApplicationFor;
        if (await db.GuildApplications.FirstOrDefaultAsync(a => a.CharacterId == applicant && a.GuildId == mine.GuildId && a.At >= since, cancel) is not GuildApplicationRecord application)
            return Refused("character", "No such application.");
        db.GuildApplications.Remove(application);
        await db.SaveChangesAsync(cancel);
        if (request!.Accept)
        {
            Character them = await db.Characters.FirstAsync(c => c.Id == applicant, cancel);
            GuildRecord guild = await db.Guilds.AsNoTracking().FirstAsync(g => g.Id == mine.GuildId, cancel);
            if (await Enter(them, guild.Id, guild.Name, guild.Crest, me.Name, db, hub, clock, cancel) is string full)
                return Refused("character", full);
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> WithdrawApplication(Guid id, GuildWithdraw request, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        Guid guild = request?.Guild ?? Guid.Empty;
        await db.GuildApplications.Where(a => a.CharacterId == id && a.GuildId == guild).ExecuteDeleteAsync(cancel);
        return TypedResults.NoContent();
    }

    /// <summary>A member comes into play (sprint 160): the others in play are told, its name for those who asked.</summary>
    internal static async Task TellOnline(Guid character, string name, GameDb db, ChatHub hub, CancellationToken cancel)
    {
        if (await MembershipOf(character, db, cancel) is not GuildMemberRecord mine)
            return;
        var news = new GuildNews(GuildNewsKind.Online, character, name);
        foreach (Guid other in await db.GuildMembers.AsNoTracking().Where(m => m.GuildId == mine.GuildId && m.CharacterId != character).Select(m => m.CharacterId).ToListAsync(cancel))
            if (hub.MapOf(other) is not null)
                await hub.TellCharacter(other, news, cancel);
    }
}
