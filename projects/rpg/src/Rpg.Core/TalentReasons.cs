using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>Why a talent cannot be taken (sprint 147, D82), for the window to say it in the player's language.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RefusalKind>))]
public enum RefusalKind
{
    /// <summary>The tree opens at level 30.</summary>
    Opens,

    /// <summary>The talent opens at a level of its own (<see cref="TalentRefusal.Number"/>: a key's 45).</summary>
    Level,

    /// <summary>It needs a door first: a pair or a Heart.</summary>
    Door,

    /// <summary>It needs <see cref="TalentRefusal.Other"/>, or one of <see cref="TalentRefusal.Any"/> (the floor below it in its style).</summary>
    Parent,

    /// <summary>It cannot be taken with <see cref="TalentRefusal.Other"/>.</summary>
    Excludes,

    /// <summary>A pair taken (<see cref="TalentRefusal.Other"/>) already has its <see cref="TalentRefusal.Element"/>.</summary>
    Pair,

    /// <summary>Its style has a key already (<see cref="TalentRefusal.Other"/>).</summary>
    KeyPerStyle,

    /// <summary>The tree has its big switch already (<see cref="TalentRefusal.Other"/>).</summary>
    Switch,

    /// <summary>Two elements out of the four are already in the grimoire.</summary>
    Elements,

    /// <summary>Not enough points left (<see cref="TalentRefusal.Number"/>: what it costs).</summary>
    Points,
}

/// <summary>Why a talent cannot be taken: its kind, the talent (or talents) or the element concerned, a number.</summary>
public sealed record TalentRefusal(RefusalKind Kind, string? Other = null, Element? Element = null, int Number = 0, IReadOnlyList<string>? Any = null);

/// <summary>The reason a node of the tree is refused, before the click (sprint 147, D82, H6 a).</summary>
public static class TalentReasons
{
    /// <summary>
    /// Why <paramref name="id"/> cannot join <paramref name="taken"/> at <paramref name="level"/>; null when it can. The
    /// first rule it breaks, in the order a player meets them.
    /// </summary>
    public static TalentRefusal? WhyNot(IReadOnlyList<string> taken, string id, int level, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        TalentSpec t = data.Talents[id];
        List<string> after = Talents.With(taken, id, data);
        if (Talents.Problem(after, level, data) is null)
            return null;
        TalentSpec[] others = [.. after.Where(o => o != id).Select(o => data.Talents[o])];
        if (level < Talents.Opens)
            return new(RefusalKind.Opens, Number: Talents.Opens);
        if (level < t.Level)
            return new(RefusalKind.Level, Number: t.Level);
        if ((t.Requires ?? []).FirstOrDefault(r => !after.Contains(r)) is string missing)
            return new(RefusalKind.Parent, missing);
        if (t.RequiresAny is { Count: > 0 } any && !any.Any(after.Contains))
            return any.All(a => data.Talents[a].IsFork) ? new(RefusalKind.Door) : new(RefusalKind.Parent, Any: any);
        if (others.FirstOrDefault(o => (t.Excludes ?? []).Contains(o.Id) || (o.Excludes ?? []).Contains(t.Id)) is TalentSpec excluded)
            return new(RefusalKind.Excludes, excluded.Id);
        if (t.IsPair && Talents.Clash([.. others, t], data) is (Element shared, TalentSpec pair))
            return new(RefusalKind.Pair, pair.Id, shared);
        if (t.Tier == Tier.Key && others.FirstOrDefault(o => o.Tier == Tier.Key && o.Branch == t.Branch) is TalentSpec key)
            return new(RefusalKind.KeyPerStyle, key.Id);
        if (t.IsSwitch && others.FirstOrDefault(o => o.IsSwitch) is TalentSpec other)
            return new(RefusalKind.Switch, other.Id);
        if (Talents.ElementsOpened([.. others, t]) > Talents.MaxOpened)
            return new(RefusalKind.Elements);
        return new(RefusalKind.Points, Number: t.Cost);
    }
}
