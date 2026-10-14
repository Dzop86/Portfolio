namespace Rpg.Core;

/// <summary>
/// The channels of the chat (sprint 84, D65): the players of the same map first; the group's, the
/// guild's and the world's are kept for later.
/// </summary>
public static class ChatChannels
{
    public const string Map = "map";
}

/// <summary>A line of the chat: the channel, who said it (a character's name), what, when.</summary>
public sealed record ChatLine(string Channel, string From, string Text, DateTimeOffset At);

/// <summary>What a player says: the text, on a channel (the map's when none).</summary>
public sealed record ChatSay(string? Text, string? Channel = null);

/// <summary>Why a line was not sent, as the server tells the one who wrote it.</summary>
public sealed record ChatRefusal(string Reason);

/// <summary>
/// The rules of the chat, shared by the game and the server: a line of 1 to <see cref="MaxLength"/>
/// characters, spaces gathered and control characters left out; at most <see cref="Burst"/> lines in
/// <see cref="Window"/>, so that nobody floods a map.
/// </summary>
public static class ChatRules
{
    public const int MaxLength = 200;
    public const int Burst = 5;
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

    /// <summary>The line as it will be shown, or null if nothing is left of it.</summary>
    public static string? Clean(string? text)
    {
        if (text is null)
            return null;
        string flat = string.Join(' ', new string([.. text.Select(c => char.IsControl(c) ? ' ' : c)])
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (flat.Length == 0)
            return null;
        return flat.Length <= MaxLength ? flat : flat[..MaxLength];
    }
}

/// <summary>Counts the lines of one speaker: <see cref="ChatRules.Burst"/> at most in any <see cref="ChatRules.Window"/>.</summary>
public sealed class ChatPace
{
    private readonly Queue<DateTimeOffset> _said = new();

    /// <summary>Whether a line said at <paramref name="now"/> may go; if so, it counts.</summary>
    public bool TryTake(DateTimeOffset now)
    {
        while (_said.Count > 0 && now - _said.Peek() >= ChatRules.Window)
            _said.Dequeue();
        if (_said.Count >= ChatRules.Burst)
            return false;
        _said.Enqueue(now);
        return true;
    }
}
