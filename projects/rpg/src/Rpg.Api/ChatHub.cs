using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
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
/// character's new look (what it wears) is told to its map.
/// </summary>
public sealed class ChatHub(TimeProvider clock, Hunts hunts, Parties parties, Trades trades)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed class Speaker(Guid character, string name, Hero look, string map, Cell at, WebSocket socket)
    {
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

    private static MapNews Here(Speaker s) => new(MapNewsKind.Here, s.Character, s.Name, s.Look, s.At);

    /// <summary>Serves one character's socket until it closes: what it says goes to its map.</summary>
    public async Task Serve(Guid character, string name, Hero look, string map, Cell at, WebSocket socket, CancellationToken cancel)
    {
        var me = new Speaker(character, name, look, map, at, socket);
        // A second connection of the same character replaces the first.
        if (_speakers.TryGetValue(character, out Speaker? old))
            await Close(old);
        _speakers[character] = me;
        parties.Back(character);
        await Arrive(me, cancel);
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
        // The party's channel (sprint 119): its members wherever they are.
        IReadOnlyList<Guid>? party = parties.Of(me.Character)?.Members;
        string? refusal = text is null ? "Nothing to say."
            : channel is not (ChatChannels.Map or ChatChannels.Party) ? "The map's channel, or the party's."
            : channel == ChatChannels.Party && party is null ? "Not in a party."
            : ChatLinks.WhyNot(text, GameData.Embedded) is string links ? links
            : !me.Pace.TryTake(clock.GetUtcNow()) ? $"At most {ChatRules.Burst} lines in {ChatRules.Window.TotalSeconds:0} seconds."
            : null;
        if (refusal is not null)
        {
            await Send(me, JsonSerializer.Serialize(new ChatRefusal(refusal), Json), cancel);
            return;
        }
        string line = JsonSerializer.Serialize(new ChatLine(channel, me.Name, text!, clock.GetUtcNow()), Json);
        foreach (Speaker other in _speakers.Values.Where(s => channel == ChatChannels.Party ? party!.Contains(s.Character) : s.Map == me.Map))
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
