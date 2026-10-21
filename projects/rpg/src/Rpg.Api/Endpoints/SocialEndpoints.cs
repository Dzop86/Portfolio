using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api.Endpoints;

/// <summary>
/// Playing together (sprint 119, D74): the party (invite by name, accept, leave, see it) and the trade between two
/// players of the same map (ask, accept, offer, accept the offers, call off). What changes is told to the players
/// concerned on their map's socket (<see cref="PartyNews"/>, <see cref="TradeNews"/>).
/// </summary>
public static class SocialEndpoints
{
    public static void MapSocialEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/characters").WithTags("Playing together").RequireAuthorization();
        group.MapGet("/{id:guid}/party", Party).WithSummary("The party of one of the player's characters: its leader and its members.");
        group.MapPost("/{id:guid}/party/invite", Invite).WithSummary($"Invites a character in play, by its name, into the party (its leader invites, {PartyRules.Most} at most).");
        group.MapPost("/{id:guid}/party/accept", Join).WithSummary("Joins the party of a character who invited this one, within a minute.");
        group.MapPost("/{id:guid}/party/leave", LeaveParty).WithSummary("Leaves the party; the next member leads if the leader leaves.");
        group.MapPost("/{id:guid}/trade/ask", Ask).WithSummary("Asks a character in play on the same map to trade.");
        group.MapPost("/{id:guid}/trade/accept", Open).WithSummary("Opens the trade a character on the same map asked for, within a minute.");
        group.MapPut("/{id:guid}/trade", Offer).WithSummary("Puts items owned and not worn, and Éclats, on the table; both have to accept again.");
        group.MapPost("/{id:guid}/trade/ready", Ready).WithSummary("Accepts the offers as they stand; once both have, the trade is made, all of it or nothing.");
        group.MapPost("/{id:guid}/trade/cancel", Cancel).WithSummary("Calls the trade off.");
    }

    private static ValidationProblem Refused(string field, string why) => TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [why] });

    private static Task<Character?> Mine(Guid id, ClaimsPrincipal user, GameDb db, CancellationToken cancel)
    {
        Guid account = CharacterEndpoints.AccountId(user);
        return db.Characters.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == account, cancel);
    }

    // The party as its members are told: the leader, then each member's name, level, class and map.
    private static async Task<PartyNews> PartyOf(Guid character, Parties parties, ChatHub hub, GameDb db, CancellationToken cancel)
    {
        if (parties.Of(character) is not (Guid leader, IReadOnlyList<Guid> ids))
            return new PartyNews(PartyNewsKind.Left);
        var found = await db.Characters.AsNoTracking().Where(c => ids.Contains(c.Id)).Select(c => new { c.Id, c.Name, c.Level, c.Class }).ToListAsync(cancel);
        return new PartyNews(PartyNewsKind.Changed, Leader: leader,
            Members: [.. ids.Select(i => found.First(f => f.Id == i)).Select(f => new PartyMember(f.Id, f.Name, f.Level, f.Class, hub.MapOf(f.Id)))]);
    }

    private static async Task TellParty(IEnumerable<Guid> members, Parties parties, ChatHub hub, GameDb db, CancellationToken cancel)
    {
        foreach (Guid m in members)
            await hub.TellCharacter(m, await PartyOf(m, parties, hub, db, cancel), cancel);
    }

    internal static async Task<Results<Ok<PartyNews>, NotFound>> Party(Guid id, ClaimsPrincipal user, GameDb db, Parties parties, ChatHub hub, CancellationToken cancel) =>
        await Mine(id, user, db, cancel) is null ? TypedResults.NotFound() : TypedResults.Ok(await PartyOf(id, parties, hub, db, cancel));

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Invite(
        Guid id, PartyInvite request, ClaimsPrincipal user, GameDb db, Parties parties, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        var to = await db.Characters.AsNoTracking().Where(c => c.Name == request.Name && c.Server == me.Server).Select(c => new { c.Id }).FirstOrDefaultAsync(cancel);
        if (to is null || hub.MapOf(to.Id) is null)
            return Refused("name", "Nobody in play by that name.");
        if (parties.Invite(id, to.Id) is string why)
            return Refused("name", why);
        await hub.TellCharacter(to.Id, new PartyNews(PartyNewsKind.Invited, id, me.Name), cancel);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Join(
        Guid id, PartyAnswer request, ClaimsPrincipal user, GameDb db, Parties parties, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        if (parties.Accept(id, request.From) is string why)
            return Refused("from", why);
        await TellParty(parties.Of(id)!.Value.Members, parties, hub, db, cancel);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> LeaveParty(Guid id, ClaimsPrincipal user, GameDb db, Parties parties, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        IReadOnlyList<Guid> before = parties.Leave(id);
        await TellParty(before, parties, hub, db, cancel);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Ask(
        Guid id, PartyAnswer request, ClaimsPrincipal user, GameDb db, Trades trades, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (hub.MapOf(id) is not string map || hub.MapOf(request.From) != map)
            return Refused("from", "Nobody in play by them on this map.");
        if (trades.Ask(id, request.From) is string why)
            return Refused("from", why);
        await hub.TellCharacter(request.From, new TradeNews(TradeNewsKind.Asked, id, me.Name), cancel);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Open(
        Guid id, PartyAnswer request, ClaimsPrincipal user, GameDb db, Trades trades, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        if (hub.MapOf(id) is not string map || hub.MapOf(request.From) != map)
            return Refused("from", "Nobody in play by them on this map.");
        if (trades.Accept(id, request.From) is string why)
            return Refused("from", why);
        await TellTrade(id, TradeNewsKind.Opened, trades, hub, db, cancel);
        return TypedResults.NoContent();
    }

    // Both players told of the trade as each sees it.
    private static async Task TellTrade(Guid one, TradeNewsKind kind, Trades trades, ChatHub hub, GameDb db, CancellationToken cancel)
    {
        if (trades.With(one) is not Guid other)
            return;
        var names = await db.Characters.AsNoTracking().Where(c => c.Id == one || c.Id == other).Select(c => new { c.Id, c.Name }).ToListAsync(cancel);
        foreach (Guid who in new[] { one, other })
        {
            var v = trades.View(who)!.Value;
            await hub.TellCharacter(who, new TradeNews(kind, View: new TradeView(v.With, names.First(n => n.Id == v.With).Name, v.Mine, v.Theirs, v.MineReady, v.TheirsReady)), cancel);
        }
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Offer(
        Guid id, TradeOffer request, ClaimsPrincipal user, GameDb db, Trades trades, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (trades.With(id) is null)
            return Refused("trade", "Not trading.");
        var offer = new TradeOffer([.. request?.Items ?? []], request?.Shards ?? 0);
        if (Trading.WhyNot(offer, me.InventoryList, me.WornList, me.Shards, GameData.Embedded) is string why)
            return Refused("trade", why);
        trades.Offer(id, offer);
        await TellTrade(id, TradeNewsKind.Changed, trades, hub, db, cancel);
        return TypedResults.NoContent();
    }

    /// <summary>The offers accepted; the second to accept makes the trade: both checked again, both changed in one save.</summary>
    internal static async Task<Results<Ok<CharacterSummary>, NotFound, ValidationProblem>> Ready(
        Guid id, ClaimsPrincipal user, GameDb db, Trades trades, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is not Character me)
            return TypedResults.NotFound();
        if (trades.With(id) is not Guid otherId)
            return Refused("trade", "Not trading.");
        if (!trades.Ready(id))
        {
            await TellTrade(id, TradeNewsKind.Changed, trades, hub, db, cancel);
            return TypedResults.Ok(CharacterEndpoints.Summary(me));
        }
        var v = trades.View(id)!.Value;
        Character other = await db.Characters.FirstAsync(c => c.Id == otherId, cancel);
        string? why = hub.MapOf(id) is not string map || hub.MapOf(otherId) != map ? "Not on the same map any more."
            : Trading.WhyNot(v.Mine, me.InventoryList, me.WornList, me.Shards, GameData.Embedded)
                ?? Trading.WhyNot(v.Theirs, other.InventoryList, other.WornList, other.Shards, GameData.Embedded);
        trades.End(id);
        if (why is not null)
        {
            foreach (Guid who in new[] { id, otherId })
                await hub.TellCharacter(who, new TradeNews(TradeNewsKind.Cancelled), cancel);
            return Refused("trade", why);
        }
        Give(me, other, v.Mine);
        Give(other, me, v.Theirs);
        await db.SaveChangesAsync(cancel);
        foreach (Guid who in new[] { id, otherId })
            await hub.TellCharacter(who, new TradeNews(TradeNewsKind.Done), cancel);
        return TypedResults.Ok(CharacterEndpoints.Summary(me));
    }

    private static void Give(Character from, Character to, TradeOffer offer)
    {
        foreach (ItemCount i in offer.Items ?? [])
            from.Spend(i);
        to.Receive(offer.Items ?? []);
        from.Shards -= offer.Shards;
        to.Shards += offer.Shards;
    }

    internal static async Task<Results<NoContent, NotFound>> Cancel(Guid id, ClaimsPrincipal user, GameDb db, Trades trades, ChatHub hub, CancellationToken cancel)
    {
        if (await Mine(id, user, db, cancel) is null)
            return TypedResults.NotFound();
        if (trades.End(id) is Guid other)
        {
            foreach (Guid who in new[] { id, other })
                await hub.TellCharacter(who, new TradeNews(TradeNewsKind.Cancelled), cancel);
        }
        return TypedResults.NoContent();
    }
}
