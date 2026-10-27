namespace Rpg.Core;

/// <summary>
/// The channels of the chat (sprint 84, D65): the players of the same map first; the party's (sprint 119), the
/// whispers (sprint 126) and the guild's (sprint 145) since; the world's is kept for later.
/// </summary>
public static class ChatChannels
{
    public const string Map = "map";

    /// <summary>The party's members, wherever they are (sprint 119).</summary>
    public const string Party = "party";

    /// <summary>One character, by its name, wherever it is (sprint 126): <c>/w Name text</c>.</summary>
    public const string Whisper = "whisper";

    /// <summary>The guild's members in play, wherever they are (sprint 145): <c>/gu text</c>.</summary>
    public const string Guild = "guild";
}

/// <summary>A line of the chat: the channel, who said it (a character's name), what, when; since sprint 126, to whom for a whisper.</summary>
public sealed record ChatLine(string Channel, string From, string Text, DateTimeOffset At, string? To = null);

/// <summary>
/// What a player says: the text, on a channel (the map's when none); since sprint 118, or the cell it walks to; since
/// sprint 126, on the whisper channel, the name of the one it is for.
/// </summary>
public sealed record ChatSay(string? Text, string? Channel = null, Cell? To = null, string? Whom = null);

/// <summary>A whisper typed in the chat (sprint 126): <c>/w Name text</c> (or <c>/whisper</c>).</summary>
public static class Whispers
{
    /// <summary>The name and the text of a whisper typed, or null when the line is not one (or has no text).</summary>
    public static (string Name, string Text)? Parse(string? line)
    {
        if (line is null)
            return null;
        string trimmed = line.TrimStart();
        string? rest = trimmed.StartsWith("/w ", StringComparison.OrdinalIgnoreCase) ? trimmed[3..]
            : trimmed.StartsWith("/whisper ", StringComparison.OrdinalIgnoreCase) ? trimmed[9..]
            : null;
        if (rest is null)
            return null;
        rest = rest.TrimStart();
        int space = rest.IndexOf(' ', StringComparison.Ordinal);
        if (space <= 0)
            return null;
        string text = rest[(space + 1)..].Trim();
        return text.Length == 0 ? null : (rest[..space], text);
    }
}

/// <summary>
/// The channel a line typed in the chat goes to (sprint 145): <c>/gu text</c> the guild's, <c>/g text</c> or
/// <c>/p text</c> the party's (sprint 119), anything else as typed on the map's (a whisper is read later, by its own rule).
/// </summary>
public static class ChatCommands
{
    public static (string Channel, string Text) Route(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (line.StartsWith("/gu ", StringComparison.OrdinalIgnoreCase))
            return (ChatChannels.Guild, line[4..]);
        if (line.Length > 3 && line[0] == '/' && line[1] is 'g' or 'p' or 'G' or 'P' && line[2] == ' ')
            return (ChatChannels.Party, line[3..]);
        return (ChatChannels.Map, line);
    }
}

/// <summary>
/// What the server tells the players of a map besides the chat (sprint 118, D74), on the same socket: someone here
/// (<see cref="MapNewsKind.Here"/>, their name, look and cell, since sprint 145 their guild's name), walking to a cell,
/// gone; a group of monsters fighting, beaten until a moment, back; a gathering spot taken, growing back until a moment.
/// </summary>
public sealed record MapNews(MapNewsKind Kind, Guid? Character = null, string? Name = null, Hero? Look = null, Cell? At = null,
    string? Group = null, string? Spot = null, DateTimeOffset? Until = null, string? Guild = null);

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<MapNewsKind>))]
public enum MapNewsKind
{
    Here,
    Walk,
    Gone,
    Fighting,
    Beaten,
    Back,
    Taken,
}

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

/// <summary>
/// An item shared in the chat (sprint 117, D77): "[item:copper-ring]" in a line, shown as the item's name that opens
/// its card. At most <see cref="Most"/> in a line, each of a known item: the server refuses the others.
/// </summary>
public static partial class ChatLinks
{
    public const int Most = 3;

    /// <summary>The link to an item, as a line carries it.</summary>
    public static string Of(string item) => $"[item:{item ?? throw new ArgumentNullException(nameof(item))}]";

    /// <summary>The line cut into its plain pieces and its links: (text, null) or (the link's text, the item's id).</summary>
    public static IReadOnlyList<(string Text, string? Item)> Pieces(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var pieces = new List<(string, string?)>();
        int at = 0;
        foreach (System.Text.RegularExpressions.Match m in Link().Matches(text))
        {
            if (m.Index > at)
                pieces.Add((text[at..m.Index], null));
            pieces.Add((m.Value, m.Groups[1].Value));
            at = m.Index + m.Length;
        }
        if (at < text.Length)
            pieces.Add((text[at..], null));
        return pieces;
    }

    /// <summary>Why the server refuses the line's links (too many, or an item it does not know), or null.</summary>
    public static string? WhyNot(string text, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        string[] items = [.. Pieces(text).Select(p => p.Item).OfType<string>()];
        return items.Length > Most ? $"At most {Most} items in a line."
            : items.FirstOrDefault(i => !data.Items.ContainsKey(i)) is string unknown ? $"No item '{unknown}'."
            : null;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\[item:([a-z0-9-]{1,64})\]")]
    private static partial System.Text.RegularExpressions.Regex Link();
}
