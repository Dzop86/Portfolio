using System.Text;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>
/// The items shared in the chat (sprint 117, D77), on the game's side: typed as "[Copper Ring]" in the field, sent as
/// <see cref="ChatLinks.Of"/>, shown in the item's colour as a link that opens its card (Godot's BBCode, the player's
/// own brackets escaped so that nobody writes markup).
/// </summary>
public static class ChatMarkup
{
    /// <summary>The item as the field shows it once shared: its name in brackets.</summary>
    public static string Shown(Item item, string lang) => $"[{(item ?? throw new ArgumentNullException(nameof(item))).Name.In(lang)}]";

    /// <summary>The line typed, each item shared turned back into its link (the first "[Name]" not yet turned, in order).</summary>
    public static string Encode(string typed, IEnumerable<string> shared, GameData data, string lang)
    {
        ArgumentNullException.ThrowIfNull(typed);
        ArgumentNullException.ThrowIfNull(shared);
        ArgumentNullException.ThrowIfNull(data);
        string line = typed;
        int from = 0;
        foreach (string id in shared)
        {
            if (!data.Items.TryGetValue(id, out Item? item))
                continue;
            string name = Shown(item, lang);
            int at = line.IndexOf(name, from, StringComparison.Ordinal);
            if (at < 0)
                continue;
            string link = ChatLinks.Of(id);
            line = string.Concat(line.AsSpan(0, at), link, line.AsSpan(at + name.Length));
            from = at + link.Length;
        }
        return line;
    }

    /// <summary>The line in BBCode: its text escaped, each known item's link as "[url=id]" around its coloured name.</summary>
    public static string BbCode(string line, GameData data, string lang)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(data);
        var code = new StringBuilder();
        foreach ((string text, string? id) in ChatLinks.Pieces(line))
        {
            if (id is not null && data.Items.TryGetValue(id, out Item? item))
                code.Append($"[url={id}][color={ItemStyle.Colour(item)}]{Escape(Shown(item, lang))}[/color][/url]");
            else
                code.Append(Escape(text));
        }
        return code.ToString();
    }

    // "[" opens a tag in BBCode: written as "[lb]".
    private static string Escape(string text) => text.Replace("[", "[lb]", StringComparison.Ordinal);
}
