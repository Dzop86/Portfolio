using System.Globalization;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>Why a build's code is refused (sprint 151, D82, N4 a).</summary>
public enum CodeRefusal
{
    /// <summary>Not a build's code at all.</summary>
    Unreadable,

    /// <summary>A code of an older tree (<see cref="BuildCode.Read"/> gives its version).</summary>
    OldVersion,

    /// <summary>Its talents do not hold at the hero's level (<see cref="BuildCode.Read"/> gives the level they ask).</summary>
    Level,
}

/// <summary>A code read: its talents, or why it is refused and a number (the old version, the level asked).</summary>
public sealed record CodeRead(IReadOnlyList<string>? Talents, CodeRefusal? Refusal = null, int Number = 0);

/// <summary>
/// A build as a code to copy and paste (sprint 151, D82, N4 a): <c>OSM3-</c>, the tree's version, then two letters a talent,
/// its place among the talents by id. A new tree (its <see cref="Talents.Version"/>) refuses the codes of the old one.
/// </summary>
public static class BuildCode
{
    public const string Prefix = "OSM";

    public static string Write(IReadOnlyList<string> talents, GameData data)
    {
        ArgumentNullException.ThrowIfNull(talents);
        ArgumentNullException.ThrowIfNull(data);
        string[] ids = Ids(data);
        return $"{Prefix}{Talents.Version}-" + string.Concat(talents.Select(id => Array.IndexOf(ids, id)).Order().Select(i => Base36(i)));
    }

    /// <summary>The talents of a code, if it is one of this tree and they hold at <paramref name="level"/>.</summary>
    public static CodeRead Read(string code, int level, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        string text = (code ?? "").Trim().ToUpperInvariant();
        int dash = text.IndexOf('-', StringComparison.Ordinal);
        if (!text.StartsWith(Prefix, StringComparison.Ordinal) || dash < 0 || !int.TryParse(text[Prefix.Length..dash], NumberStyles.None, CultureInfo.InvariantCulture, out int version))
            return new(null, CodeRefusal.Unreadable);
        if (version != Talents.Version)
            return new(null, CodeRefusal.OldVersion, version);
        string body = text[(dash + 1)..];
        string[] ids = Ids(data);
        if (body.Length % 2 != 0)
            return new(null, CodeRefusal.Unreadable);
        var talents = new List<string>();
        for (int i = 0; i < body.Length; i += 2)
        {
            if (FromBase36(body.Substring(i, 2)) is not int index || index >= ids.Length || talents.Contains(ids[index]))
                return new(null, CodeRefusal.Unreadable);
            talents.Add(ids[index]);
        }
        // In an order a player can take them, each node holding when it comes (Prism after the Conversion it asks for).
        if (Ordered(talents, level, data) is List<string> ordered)
            return new(ordered);
        int asked = Enumerable.Range(Talents.Opens, Hero.MaxLevel - Talents.Opens + 1).FirstOrDefault(l => Ordered(talents, l, data) is not null);
        return asked == 0 ? new(null, CodeRefusal.Unreadable) : new(null, CodeRefusal.Level, asked);
    }

    private static List<string>? Ordered(IReadOnlyList<string> talents, int level, GameData data)
    {
        var taken = new List<string>();
        var left = talents.Order(StringComparer.Ordinal).ToList();
        while (left.FirstOrDefault(id => Talents.Problem([.. taken, id], level, data) is null) is string next)
        {
            taken.Add(next);
            left.Remove(next);
        }
        return left.Count == 0 ? taken : null;
    }

    private static string[] Ids(GameData data) => [.. data.Talents.Keys.Order(StringComparer.Ordinal)];

    private const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private static string Base36(int i) => $"{Digits[i / 36]}{Digits[i % 36]}";

    private static int? FromBase36(string two) =>
        Digits.IndexOf(two[0], StringComparison.Ordinal) is int a and >= 0 && Digits.IndexOf(two[1], StringComparison.Ordinal) is int b and >= 0 ? (a * 36) + b : null;
}

/// <summary>
/// Planning the tree further than one's level (sprint 151, D82, L8 a, N2 a, N3 a): the path a node asks from what is
/// taken and its cost, a plan at a higher level whose nodes beyond the draft are ghosts that cannot be saved, the
/// reference builds of a class to load in the draft, and builds drawn at random for the measures out of CI.
/// </summary>
public static class TreePlan
{
    /// <summary>The level one plans at.</summary>
    public const int PlanLevel = 60;

    /// <summary>
    /// The nodes to take, parents first, for <paramref name="id"/> to join <paramref name="taken"/> (L8 a): for a parent
    /// among several, the cheapest way; a door, the first pair that shares no element with those taken. Null when no way
    /// leads there (an exclusion, a clash). Levels and points aside: the window says the cost.
    /// </summary>
    public static IReadOnlyList<string>? Path(IReadOnlyList<string> taken, string id, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        if (taken.Contains(id))
            return [];
        List<string>? best = null;
        void Walk(List<string> have, List<string> path, string node)
        {
            if (have.Contains(node))
                return;
            TalentSpec t = data.Talents[node];
            foreach (string r in t.Requires ?? [])
                Walk(have, path, r);
            if (t.RequiresAny is { Count: > 0 } any && !any.Any(have.Contains))
            {
                // The cheapest of the parents that leads somewhere.
                List<string>? cheapest = null;
                foreach (string option in any)
                {
                    var tryHave = new List<string>(have);
                    var tryPath = new List<string>();
                    Walk(tryHave, tryPath, option);
                    if (Talents.Problem([.. tryHave], Hero.MaxLevel, data) is null && (cheapest is null || Cost(tryPath, data) < Cost(cheapest, data)))
                        cheapest = tryPath;
                }
                if (cheapest is null)
                    return;
                foreach (string p in cheapest)
                {
                    have.Add(p);
                    path.Add(p);
                }
            }
            have.Add(node);
            path.Add(node);
        }
        var have = new List<string>(taken);
        var path = new List<string>();
        Walk(have, path, id);
        if (path.LastOrDefault() == id && Talents.Problem(have, Hero.MaxLevel, data) is null)
            best = path;
        return best;
    }

    /// <summary>The points a path costs: the sum of its nodes'.</summary>
    public static int Cost(IEnumerable<string> path, GameData data) => path.Sum(id => data.Talents[id].Cost);

    /// <summary>The nodes of a plan beyond the draft (N2 a): drawn as ghosts, never saved.</summary>
    public static IReadOnlyList<string> Ghosts(IReadOnlyList<string> plan, IReadOnlyList<string> draft) => [.. plan.Where(id => !draft.Contains(id))];

    /// <summary>A class's reference builds (N3 a): of Matter, of Manner and of Pure element, by id.</summary>
    public static IReadOnlyList<string> Suggested(string? heroClass) =>
        [.. Progression.Builds.Keys.Where(k => heroClass is not null && k.StartsWith(heroClass + "-", StringComparison.Ordinal))];
}
