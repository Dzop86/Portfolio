using System.Text.RegularExpressions;

namespace Rpg.Core;

/// <summary>A member's rank in a guild (sprint 145, D79): its leader, its officers, its members.</summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<GuildRank>))]
public enum GuildRank
{
    Leader,
    Officer,
    Member,
}

/// <summary>
/// One member of a guild as the others see it (sprint 145): its name, rank, level and class, the zone it is in when in
/// play (never the map, as for friends: <see cref="Contacts.ZoneOf"/>), and when it last came into play.
/// </summary>
public sealed record GuildMember(Guid Id, string Name, GuildRank Rank, int Level, string? Class, string? Zone, DateTimeOffset? LastSeen);

/// <summary>
/// A guild as its members see it: its name, its members (the leader first, then the officers, then the others); since
/// sprint 156, where it stands (level, week, message, crest).
/// </summary>
public sealed record GuildView(Guid Id, string Name, IReadOnlyList<GuildMember> Members, GuildStanding? Standing = null);

/// <summary>
/// What the server tells a player of its guild (sprint 145), on the map's socket: someone invites it
/// (<see cref="GuildNewsKind.Invited"/>, their id and name, the guild's name), the guild as it now is, or it is no longer
/// in one (it left, was sent out, or the guild is gone).
/// </summary>
public sealed record GuildNews(GuildNewsKind Guild, Guid? From = null, string? Name = null, string? GuildName = null, GuildView? View = null);

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<GuildNewsKind>))]
public enum GuildNewsKind
{
    Invited,
    Changed,
    Left,
}

/// <summary>A guild to found, by its name.</summary>
public sealed record NewGuild(string? Name);

/// <summary>A member of one's guild to send out, or to give a rank (<see cref="GuildRank.Leader"/>: the title handed over).</summary>
public sealed record GuildOrder(Guid Member, GuildRank Rank = GuildRank.Member);

/// <summary>
/// The guilds' rules (sprint 145, D79), shared by the game and the server: founded for <see cref="Price"/> Éclats at
/// the registrar of Clairval (<see cref="Registrar"/>), a name of 3 to 20 characters unique on the server, three ranks
/// and <see cref="Most"/> members at most. The leader and the officers invite; the leader sends anyone out, an officer
/// only members; the leader alone names officers and hands its title over; a leader who leaves hands it to the oldest
/// officer, else the oldest member, and the last one to leave ends the guild.
/// </summary>
public static partial class GuildRules
{
    public const long Price = 1000;
    public const int Most = 50;
    public static readonly TimeSpan InviteFor = TimeSpan.FromMinutes(1);

    /// <summary>The NPC who founds the guilds, and the map it stands on (Clairval's square).</summary>
    public const string Registrar = "registrar";

    /// <summary>
    /// A guild's name: a capital, then letters, with single spaces, hyphens or apostrophes inside, 3 to 20 characters in
    /// all, as the characters' names but for the words: "Les Veilleurs", "Cœur-de-Chêne".
    /// </summary>
    [GeneratedRegex(@"^\p{Lu}\p{L}*(?:[ '-]\p{L}+)*$")]
    private static partial Regex NamePattern();

    /// <summary>The map the registrar stands on, where guilds are founded; null if the data have none.</summary>
    public static string? RegistrarMap(GameData data) =>
        (data ?? throw new ArgumentNullException(nameof(data))).Towns.Values.FirstOrDefault(t => t.Npcs.Any(n => n.Id == Registrar))?.Id;

    public static bool IsValidName(string? name) => name is { Length: >= 3 and <= 20 } && NamePattern().IsMatch(name);

    /// <summary>The name as the server compares it: two guilds cannot differ only by case.</summary>
    public static string Normalize(string name) => (name ?? throw new ArgumentNullException(nameof(name))).ToUpperInvariant();

    /// <summary>Why a character cannot found that guild, or null.</summary>
    public static string? WhyNotFound(string? name, bool taken, long shards, bool inGuild)
    {
        return inGuild ? "Already in a guild: leave it first."
            : !IsValidName(name) ? "A guild's name has 3 to 20 letters, the first a capital, words parted by a space, a hyphen or an apostrophe."
            : taken ? "A guild already has that name."
            : shards < Price ? $"Founding a guild costs {Price} Éclats."
            : null;
    }

    /// <summary>Why a member of that rank cannot invite into a guild of that many, or null.</summary>
    public static string? WhyNotInvite(GuildRank by, int count, bool themInGuild)
    {
        return by == GuildRank.Member ? "Only the leader and the officers invite."
            : themInGuild ? "Already in a guild."
            : count >= Most ? $"The guild is full ({Most})."
            : null;
    }

    /// <summary>Why a member of rank <paramref name="by"/> cannot send out one of rank <paramref name="them"/>, or null.</summary>
    public static string? WhyNotKick(GuildRank by, GuildRank them, bool oneself)
    {
        return oneself ? "Not oneself: leave the guild instead."
            : by == GuildRank.Member ? "Only the leader and the officers send members out."
            : by == GuildRank.Officer && them != GuildRank.Member ? "An officer sends out members only."
            : null;
    }

    /// <summary>Why a member of rank <paramref name="by"/> cannot give <paramref name="rank"/> to one of rank <paramref name="them"/>, or null.</summary>
    public static string? WhyNotRank(GuildRank by, GuildRank them, GuildRank rank, bool oneself)
    {
        return by != GuildRank.Leader ? "Only the leader gives ranks."
            : oneself ? "Not oneself: hand the title over to another."
            : them == rank ? "Already of that rank."
            : null;
    }

    /// <summary>
    /// Who leads once the leader goes (sprint 145): the officer who joined first, else the member who joined first; null
    /// when nobody is left. <paramref name="others"/> are the members left, with their rank and when they joined.
    /// </summary>
    public static Guid? Successor(IEnumerable<(Guid Id, GuildRank Rank, DateTimeOffset Joined)> others)
    {
        ArgumentNullException.ThrowIfNull(others);
        return others.OrderBy(o => o.Rank == GuildRank.Officer ? 0 : 1).ThenBy(o => o.Joined).ThenBy(o => o.Id)
            .Select(o => (Guid?)o.Id).FirstOrDefault();
    }

    /// <summary>The members in the order the guild's tab lists them: by rank, then those in play, then by name.</summary>
    public static IReadOnlyList<GuildMember> Ordered(IEnumerable<GuildMember> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        return [.. members.OrderBy(m => m.Rank).ThenBy(m => m.Zone is null).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)];
    }
}
