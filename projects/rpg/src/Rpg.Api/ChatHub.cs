using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Rpg.Api.Data;
using Rpg.Core;

namespace Rpg.Api;

/// <summary>
/// The proximity chat (sprint 84, D65): one WebSocket per character in play, the map it stands on as the
/// server knows it (its place, saved after each walk and each way taken); a line goes to every
/// character on the same map, its sender included. Lines are cleaned and paced by <see cref="ChatRules"/>;
/// nothing is kept once sent. One server process holds them all (see the README's limits). Since sprint 118 (D74),
/// the same socket tells the players of a map who is there and where they walk (<see cref="MapNews"/>), and
/// the endpoints tell them of the groups of monsters fought or beaten and of the spots gathered. Since sprint 124, a
/// game that closes its connection calls its trade off and starts its ten minutes out of play for its party, and a
/// character's new look (what it wears) is told to its map. Since sprint 126, a whisper goes to one character by its name,
/// wherever it is, unless it ignores the sender; and a character coming into play is told to the friends in play
/// (<see cref="ContactNews"/>). Since sprint 145, each character in play carries its guild: its name goes with it to
/// its map's players, and the guild's channel reaches its members in play wherever they are.
/// </summary>
public sealed class ChatHub(TimeProvider clock, Hunts hunts, Parties parties, Trades trades, IServiceScopeFactory scopes)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed class Speaker(Guid character, string name, Hero look, string map, Cell at, WebSocket socket, Guid? guild, string? guildName, string? title, string? crest)
    {
        public string? Crest { get; set; } = crest;
        public string? Title { get; set; } = title;
        public Guid? Guild { get; set; } = guild;
        public string? GuildName { get; set; } = guildName;
        public Guid Character { get; } = character;
        public string Name { get; } = name;
        public Hero Look { get; set; } = look;
        public string Map { get; set; } = map;
        public Cell At { get; set; } = at;
        public WebSocket Socket { get; } = socket;
        public ChatPace Pace { get; } = new();
        public SemaphoreSlim Sending { get; } = new(1, 1);
    }

    private readonly ConcurrentDictionary<Guid, Speaker> _speakers = new();

    /// <summary>The map a character in play is on, or null when its game is not connected (sprint 119).</summary>
    public string? MapOf(Guid character) => _speakers.TryGetValue(character, out Speaker? s) ? s.Map : null;

    /// <summary>Tells one character some news of its party or of a trade (sprint 119), wherever it is.</summary>
    public async Task TellCharacter(Guid character, object news, CancellationToken cancel = default)
    {
        if (_speakers.TryGetValue(character, out Speaker? s))
            await Send(s, JsonSerializer.Serialize(news, news.GetType(), Json), cancel);
    }

    /// <summary>A character's look changed (it wears something else, sprint 124): its map's players see it so.</summary>
    public async Task Restyle(Guid character, Hero look, CancellationToken cancel = default)
    {
        if (!_speakers.TryGetValue(character, out Speaker? s) || s.Look == look)
            return;
        s.Look = look;
        await Tell(s.Map, Here(s), cancel: cancel, but: character);
    }

    /// <summary>A character joined a guild, or left it (null), sprint 145: its map's players see it under its new name, with the crest (sprint 156).</summary>
    public async Task SetGuild(Guid character, Guid? guild, string? name, string? crest, CancellationToken cancel = default)
    {
        if (!_speakers.TryGetValue(character, out Speaker? s))
            return;
        (s.Guild, s.GuildName, s.Crest) = (guild, name, crest);
        await Tell(s.Map, Here(s), cancel: cancel, but: character);
    }

    /// <summary>A guild's new crest (sprint 156): each of its members in play seen with it on its map.</summary>
    public async Task SetCrest(Guid guild, string crest, CancellationToken cancel = default)
    {
        foreach (Speaker s in _speakers.Values.Where(s => s.Guild == guild))
        {
            s.Crest = crest;
            await Tell(s.Map, Here(s), cancel: cancel, but: s.Character);
        }
    }

    /// <summary>A character wears another title, or none (sprint 155): its map's players see it under its name.</summary>
    public async Task SetTitle(Guid character, string? title, CancellationToken cancel = default)
    {
        if (!_speakers.TryGetValue(character, out Speaker? s))
            return;
        s.Title = title;
        await Tell(s.Map, Here(s), cancel: cancel, but: character);
    }

    /// <summary>The characters connected on a map (for the tests).</summary>
    public IReadOnlyList<Guid> On(string map) => [.. _speakers.Values.Where(s => s.Map == map).Select(s => s.Character)];

    /// <summary>
    /// The character's place saved (after a walk, or a way taken): on another map, gone from the old one's players,
    /// here for the new one's, who it learns of with the map's groups; on the same map, where it stands now.
    /// </summary>
    public async Task Move(Guid character, string map, Cell at, CancellationToken cancel = default)
    {
        if (!_speakers.TryGetValue(character, out Speaker? s))
            return;
        string from = s.Map;
        (s.Map, s.At) = (map, at);
        if (from == map)
        {
            await Tell(map, new MapNews(MapNewsKind.Walk, character, At: at), cancel: cancel, but: character);
            return;
        }
        await Tell(from, new MapNews(MapNewsKind.Gone, character), cancel: cancel, but: character);
        await Arrive(s, cancel);
    }

    /// <summary>Tells the players of a map some news (a group fought or beaten, a spot gathered), but one of them if said.</summary>
    public async Task Tell(string map, MapNews news, Guid? but = null, CancellationToken cancel = default)
    {
        string json = JsonSerializer.Serialize(news, Json);
        foreach (Speaker other in _speakers.Values.Where(o => o.Map == map && o.Character != but))
            await Send(other, json, cancel);
    }

    // On a map: the others there learn of it, it learns of them and of the groups fought or beaten.
    private async Task Arrive(Speaker me, CancellationToken cancel)
    {
        await Tell(me.Map, Here(me), cancel: cancel, but: me.Character);
        foreach (MapNews news in _speakers.Values.Where(o => o.Map == me.Map && o.Character != me.Character).Select(Here).Concat(hunts.On(me.Map)))
            await Send(me, JsonSerializer.Serialize(news, Json), cancel);
    }

    private static MapNews Here(Speaker s) => new(MapNewsKind.Here, s.Character, s.Name, s.Look, s.At, Guild: s.GuildName, Title: s.Title, Crest: s.Crest);

    // The accounts that have it as a friend learn it came into play, on whichever of their characters is (sprint 126).
    private async Task TellFriends(Speaker me, CancellationToken cancel)
    {
        Guid[] online = [.. _speakers.Keys.Where(k => k != me.Character)];
        if (online.Length == 0)
            return;
        using IServiceScope scope = scopes.CreateScope();
        GameDb db = scope.ServiceProvider.GetRequiredService<GameDb>();
        List<Guid> friends = await db.Characters.AsNoTracking()
            .Where(c => online.Contains(c.Id) && db.Contacts.Any(k => k.AccountId == c.AccountId && k.CharacterId == me.Character && !k.Ignore))
            .Select(c => c.Id).ToListAsync(cancel);
        foreach (Guid friend in friends)
            await TellCharacter(friend, new ContactNews(ContactNewsKind.Online, me.Name, Contacts.ZoneOf(me.Map, GameData.Embedded)), cancel);
    }

    // A whisper (sprint 126): to the one named, in play and not ignoring the sender; the sender sees it too.
    private async Task Whisper(Speaker me, string? whom, string text, CancellationToken cancel)
    {
        Speaker? to = _speakers.Values.FirstOrDefault(s => string.Equals(s.Name, whom, StringComparison.OrdinalIgnoreCase));
        bool ignored = false;
        if (to is not null)
        {
            using IServiceScope scope = scopes.CreateScope();
            ignored = await Endpoints.SocialEndpoints.Ignores(scope.ServiceProvider.GetRequiredService<GameDb>(), to.Character, me.Character, cancel);
        }
        if (to is null || ignored)
        {
            // Ignored: the same answer as not in play, so that nobody learns they are ignored.
            await Send(me, JsonSerializer.Serialize(new ChatRefusal($"{whom} is not in play."), Json), cancel);
            return;
        }
        string line = JsonSerializer.Serialize(new ChatLine(ChatChannels.Whisper, me.Name, text, clock.GetUtcNow(), to.Name), Json);
        await Send(to, line, cancel);
        if (to != me)
            await Send(me, line, cancel);
    }

    /// <summary>Serves one character's socket until it closes: what it says goes to its map; since sprint 145, with its guild.</summary>
    public async Task Serve(Guid character, string name, Hero look, string map, Cell at, WebSocket socket, CancellationToken cancel,
        Guid? guild = null, string? guildName = null, string? title = null, string? crest = null)
    {
        var me = new Speaker(character, name, look, map, at, socket, guild, guildName, title, crest);
        // A second connection of the same character replaces the first.
        if (_speakers.TryGetValue(character, out Speaker? old))
            await Close(old);
        _speakers[character] = me;
        parties.Back(character);
        await Arrive(me, cancel);
        await TellFriends(me, cancel);
        byte[] buffer = new byte[4096];
        try
        {
            while (socket.State == WebSocketState.Open && !cancel.IsCancellationRequested)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, cancel);
                    if (result.MessageType == WebSocketMessageType.Close)
                        return;
                    message.Write(buffer, 0, result.Count);
                    if (message.Length > 4 * ChatRules.MaxLength * 4)
                        return;
                }
                while (!result.EndOfMessage);
                await Said(me, Encoding.UTF8.GetString(message.ToArray()), cancel);
            }
        }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException)
        {
            // Gone: the game was closed or the connection cut.
        }
        finally
        {
            if (_speakers.TryRemove(new KeyValuePair<Guid, Speaker>(character, me)))
            {
                await Tell(me.Map, new MapNews(MapNewsKind.Gone, character), cancel: CancellationToken.None);
                // Its trade cannot go on without it; its party waits ten minutes for it (sprint 124).
                parties.Gone(character);
                if (trades.End(character) is Guid other)
                    await TellCharacter(other, new TradeNews(TradeNewsKind.Cancelled), CancellationToken.None);
            }
            await Close(me);
        }
    }

    private async Task Said(Speaker me, string json, CancellationToken cancel)
    {
        ChatSay? say;
        try
        {
            say = JsonSerializer.Deserialize<ChatSay>(json, Json);
        }
        catch (JsonException)
        {
            say = null;
        }
        // A walk begun (sprint 118): to a cell one can stand on, told to the map's other players.
        if (say?.To is Cell to)
        {
            if (GameData.Embedded.Towns.TryGetValue(me.Map, out Town? town) && town.CanStand(to))
            {
                me.At = to;
                await Tell(me.Map, new MapNews(MapNewsKind.Walk, me.Character, At: to), cancel: cancel, but: me.Character);
            }
            return;
        }
        string? text = ChatRules.Clean(say?.Text);
        string channel = say?.Channel ?? ChatChannels.Map;
        // The party's channel (sprint 119): its members wherever they are; the guild's (sprint 145) likewise.
        IReadOnlyList<Guid>? party = parties.Of(me.Character)?.Members;
        string? refusal = text is null ? "Nothing to say."
            : channel is not (ChatChannels.Map or ChatChannels.Party or ChatChannels.Whisper or ChatChannels.Guild) ? "The map's channel, the party's, the guild's, or a whisper."
            : channel == ChatChannels.Party && party is null ? "Not in a party."
            : channel == ChatChannels.Guild && me.Guild is null ? "Not in a guild."
            : channel == ChatChannels.Whisper && string.IsNullOrWhiteSpace(say?.Whom) ? "A whisper is for someone."
            : ChatLinks.WhyNot(text, GameData.Embedded) is string links ? links
            : !me.Pace.TryTake(clock.GetUtcNow()) ? $"At most {ChatRules.Burst} lines in {ChatRules.Window.TotalSeconds:0} seconds."
            : null;
        if (refusal is not null)
        {
            await Send(me, JsonSerializer.Serialize(new ChatRefusal(refusal), Json), cancel);
            return;
        }
        if (channel == ChatChannels.Whisper)
        {
            await Whisper(me, say!.Whom, text!, cancel);
            return;
        }
        string line = JsonSerializer.Serialize(new ChatLine(channel, me.Name, text!, clock.GetUtcNow()), Json);
        foreach (Speaker other in _speakers.Values.Where(s => channel switch
        {
            ChatChannels.Party => party!.Contains(s.Character),
            ChatChannels.Guild => s.Guild == me.Guild,
            _ => s.Map == me.Map,
        }))
            await Send(other, line, cancel);
    }

    private static async Task Send(Speaker to, string json, CancellationToken cancel)
    {
        await to.Sending.WaitAsync(cancel);
        try
        {
            if (to.Socket.State == WebSocketState.Open)
                await to.Socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, cancel);
        }
        catch (WebSocketException)
        {
            // That one is gone; its own loop cleans up.
        }
        finally
        {
            to.Sending.Release();
        }
    }

    private static async Task Close(Speaker s)
    {
        try
        {
            // Asked to close by the game: answer, or the game waits for ever; else close ourselves.
            if (s.Socket.State == WebSocketState.CloseReceived)
                await s.Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            else if (s.Socket.State == WebSocketState.Open)
                await s.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }
        catch (WebSocketException)
        {
        }
    }
}
