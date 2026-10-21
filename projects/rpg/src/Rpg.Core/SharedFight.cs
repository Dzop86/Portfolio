using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>
/// A fight with the party, led by the server (sprint 120, D74): what each game needs to build the same
/// <see cref="Fight"/> (the scenario, the seed, the heroes and their hit points to start with) and who plays which hero,
/// the one who started it first.
/// </summary>
public sealed record SharedFightStart(
    Guid Fight,
    string Scenario,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)] ulong Seed,
    Hero Hero,
    int? HeroHp,
    IReadOnlyList<Hero> Allies,
    IReadOnlyList<int?> AlliesHp,
    IReadOnlyList<Guid> Players)
{
    /// <summary>The same fight as the server's, before any action.</summary>
    public Fight Build(GameData data) => new(data, Scenario, Seed, Hero, null, HeroHp, Allies, AlliesHp);
}

/// <summary>What one player earned from a fight with the party (sprint 120): experience, Éclats, items, a level, a quest done.</summary>
public sealed record SharedFightResult(Guid Character, string Name, long Xp, long Shards, IReadOnlyList<ItemCount> Items, int Level, string? Quest);

/// <summary>
/// What the server tells the players of a fight with the party (sprint 120), on the map's socket: a party member opened
/// one (<see cref="SharedFightKind.Opened"/>, to join within <see cref="SharedRules.JoinFor"/>), it started (or, coming
/// back, everything so far: the start and the actions), an action accepted (numbered from 0), whose turn it is and until
/// when, the results once over.
/// </summary>
public sealed record SharedFightNews(
    SharedFightKind Shared,
    Guid Fight,
    string? Name = null,
    string? Scenario = null,
    SharedFightStart? Start = null,
    IReadOnlyList<FightAction>? Actions = null,
    int? Number = null,
    FightAction? Action = null,
    DateTimeOffset? TurnEnds = null,
    IReadOnlyList<SharedFightResult>? Results = null,
    int? Winner = null);

[JsonConverter(typeof(JsonStringEnumConverter<SharedFightKind>))]
public enum SharedFightKind
{
    Opened,
    Started,
    Acted,
    Over,
}

/// <summary>Opens a fight with the party: the scenario (a group of monsters of the world).</summary>
public sealed record NewSharedFight(string? Scenario);

/// <summary>The rules of a fight with the party (sprint 120, D74): fifteen seconds to join, thirty a turn.</summary>
public static class SharedRules
{
    public static readonly TimeSpan JoinFor = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan Turn = TimeSpan.FromSeconds(30);
}
