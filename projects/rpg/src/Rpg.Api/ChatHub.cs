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
/// nothing is kept once sent. One server process holds them all (see the README's limits).
/// </summary>
public sealed class ChatHub(TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed class Speaker(Guid character, string name, string map, WebSocket socket)
    {
        public Guid Character { get; } = character;
        public string Name { get; } = name;
        public string Map { get; set; } = map;
        public WebSocket Socket { get; } = socket;
        public ChatPace Pace { get; } = new();
        public SemaphoreSlim Sending { get; } = new(1, 1);
    }

    private readonly ConcurrentDictionary<Guid, Speaker> _speakers = new();

    /// <summary>The characters connected on a map (for the tests).</summary>
    public IReadOnlyList<Guid> On(string map) => [.. _speakers.Values.Where(s => s.Map == map).Select(s => s.Character)];

    /// <summary>The character walked onto another map: its lines go there from now on.</summary>
    public void Move(Guid character, string map)
    {
        if (_speakers.TryGetValue(character, out Speaker? s))
            s.Map = map;
    }

    /// <summary>Serves one character's socket until it closes: what it says goes to its map.</summary>
    public async Task Serve(Guid character, string name, string map, WebSocket socket, CancellationToken cancel)
    {
        var me = new Speaker(character, name, map, socket);
        // A second connection of the same character replaces the first.
        if (_speakers.TryGetValue(character, out Speaker? old))
            await Close(old);
        _speakers[character] = me;
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
            _speakers.TryRemove(new KeyValuePair<Guid, Speaker>(character, me));
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
        string? text = ChatRules.Clean(say?.Text);
        string? refusal = text is null ? "Nothing to say."
            : (say!.Channel ?? ChatChannels.Map) != ChatChannels.Map ? "Only the map's channel for now."
            : ChatLinks.WhyNot(text, GameData.Embedded) is string links ? links
            : !me.Pace.TryTake(clock.GetUtcNow()) ? $"At most {ChatRules.Burst} lines in {ChatRules.Window.TotalSeconds:0} seconds."
            : null;
        if (refusal is not null)
        {
            await Send(me, JsonSerializer.Serialize(new ChatRefusal(refusal), Json), cancel);
            return;
        }
        string line = JsonSerializer.Serialize(new ChatLine(ChatChannels.Map, me.Name, text!, clock.GetUtcNow()), Json);
        foreach (Speaker other in _speakers.Values.Where(s => s.Map == me.Map))
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
