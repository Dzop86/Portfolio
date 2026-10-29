using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>A recorded fight that cannot be replayed: unknown format, scenario or refused action.</summary>
public sealed class InvalidFightRecordException(string message) : Exception(message);

/// <summary>
/// A whole fight in about a kilobyte: the scenario, the seed, the player's hero and the accepted actions. Replaying
/// them gives back the same fight, roll for roll, on every system; the server will use this to
/// check a fight it did not see. The seed is written as a string: a 64-bit integer does not fit
/// in a JavaScript number.
/// </summary>
public sealed record FightRecord(
    int Version,
    string Scenario,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)] ulong Seed,
    IReadOnlyList<FightAction> Actions,
    Hero? Hero = null,
    Hero? Rival = null,
    int? HeroHp = null,
    IReadOnlyList<Hero>? Allies = null,
    IReadOnlyList<int?>? AlliesHp = null)
{
    /// <summary>
    /// Version 2 (sprint 120) adds the hero's party; a record of version 1 is a fight alone, read the same. Version 3
    /// (sprint 161): a hero without a chosen deck fights with the automatic deck that grows after level 40; a record of
    /// version 2 or less replays with its first spells (<see cref="Hero.FirstSpells"/>).
    /// </summary>
    public const int CurrentVersion = 3;

    private static readonly JsonSerializerOptions Compact = new(GameData.Json) { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public static FightRecord Of(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        return new FightRecord(CurrentVersion, fight.Scenario.Id, fight.Seed, [.. fight.History], fight.Hero, fight.Rival, fight.StartHeroHp,
            fight.Allies.Count == 0 ? null : fight.Allies, fight.Allies.Count == 0 ? null : fight.AlliesHp);
    }

    public string ToJson() => JsonSerializer.Serialize(this, Compact);

    public static FightRecord FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<FightRecord>(json, GameData.Json)
                ?? throw new InvalidFightRecordException("Empty record.");
        }
        catch (JsonException e)
        {
            throw new InvalidFightRecordException($"Unreadable record: {e.Message}");
        }
    }

    /// <summary>Plays the actions again from the start; refuses a record the rules would not have accepted.</summary>
    public Fight Replay(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (Version is < 1 or > CurrentVersion || (Version == 1 && Allies is { Count: > 0 }))
            throw new InvalidFightRecordException($"Unknown record version {Version}.");
        Hero? Then(Hero? h) => Version < 3 && h is { Deck: null } ? h with { Deck = h.FirstSpells(data) } : h;
        Hero? hero = Then(Hero), rival = Then(Rival);
        IReadOnlyList<Hero>? allies = Allies?.Select(a => Then(a)!).ToList();
        if (data.FindScenario(Scenario) is null)
            throw new InvalidFightRecordException($"Unknown scenario '{Scenario}'.");
        if ((hero?.Problem(data) ?? rival?.Problem(data) ?? (allies ?? []).Select(a => a.Problem(data)).FirstOrDefault(p => p is not null)) is string problem)
            throw new InvalidFightRecordException($"Invalid hero: {problem}");
        Fight fight;
        try
        {
            fight = new Fight(data, Scenario, Seed, hero, rival, HeroHp, allies, AlliesHp);
        }
        catch (ArgumentException e)
        {
            throw new InvalidFightRecordException(e.Message);
        }
        for (int i = 0; i < Actions.Count; i++)
        {
            ActionError error = fight.Apply(Actions[i]);
            if (error != ActionError.None)
                throw new InvalidFightRecordException($"Action {i} ({Actions[i]}) refused: {error}.");
        }
        return fight;
    }
}
