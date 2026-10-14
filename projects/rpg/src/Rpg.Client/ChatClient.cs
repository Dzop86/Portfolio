using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>What came from the chat: a line said on the map, or the server's refusal of one of ours.</summary>
public sealed record ChatEvent(ChatLine? Line, string? Refusal);

/// <summary>
/// The game's side of the proximity chat (sprint 84): a WebSocket to the server, lines sent as
/// <see cref="ChatSay"/>, what comes back queued for the game to read each frame (<see cref="TryRead"/>),
/// whatever thread the socket answers on.
/// </summary>
public sealed class ChatClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly WebSocket _socket;
    private readonly ConcurrentQueue<ChatEvent> _received = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _loop;

    public ChatClient(WebSocket socket)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _loop = Task.Run(Receive);
    }

    public bool IsOpen => _socket.State == WebSocketState.Open;

    /// <summary>Says a line on the map; cleaned first, nothing sent if nothing is left.</summary>
    public async Task<bool> Say(string text, CancellationToken cancel = default)
    {
        if (ChatRules.Clean(text) is not string clean || !IsOpen)
            return false;
        await _socket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new ChatSay(clean), Json)), WebSocketMessageType.Text, true, cancel);
        return true;
    }

    public bool TryRead(out ChatEvent? e) => _received.TryDequeue(out e);

    /// <summary>Waits for the next event, for the tests and the self-test; null if none came in time.</summary>
    public async Task<ChatEvent?> Next(TimeSpan within, CancellationToken cancel = default)
    {
        DateTime until = DateTime.UtcNow + within;
        while (DateTime.UtcNow < until)
        {
            if (TryRead(out ChatEvent? e))
                return e;
            await Task.Delay(20, cancel);
        }
        return null;
    }

    private async Task Receive()
    {
        byte[] buffer = new byte[8192];
        try
        {
            while (_socket.State == WebSocketState.Open)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(buffer, _stop.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                        return;
                    message.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);
                using JsonDocument doc = JsonDocument.Parse(message.ToArray());
                _received.Enqueue(doc.RootElement.TryGetProperty("reason", out JsonElement reason)
                    ? new ChatEvent(null, reason.GetString())
                    : new ChatEvent(doc.RootElement.Deserialize<ChatLine>(Json), null));
            }
        }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException or JsonException or ObjectDisposedException)
        {
            // Closed, cut, or a line the game cannot read: the chat stops; the game says it is closed.
        }
    }

    public async ValueTask DisposeAsync()
    {
        // The close is only sent here: the receiving loop reads the server's answer and ends (two reads at
        // once on one socket are not allowed).
        try
        {
            if (_socket.State == WebSocketState.Open)
                await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }
        catch (Exception e) when (e is WebSocketException or ObjectDisposedException)
        {
        }
        await Task.WhenAny(_loop, Task.Delay(TimeSpan.FromSeconds(2)));
        await _stop.CancelAsync();
        try
        {
            await _loop;
        }
        catch (OperationCanceledException)
        {
        }
        _socket.Dispose();
        _stop.Dispose();
    }
}

/// <summary>The chat as the game shows it: the last lines, each with its channel and speaker, in the player's language.</summary>
public sealed class ChatLog(Texts texts)
{
    public const int Kept = 50;

    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public void Add(ChatEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _lines.Add(e.Line is ChatLine line ? texts["chat.line", texts["chat." + line.Channel], line.From, line.Text] : texts["chat.refused", e.Refusal ?? ""]);
        if (_lines.Count > Kept)
            _lines.RemoveAt(0);
    }

    public void Note(string text)
    {
        _lines.Add(text);
        if (_lines.Count > Kept)
            _lines.RemoveAt(0);
    }
}
