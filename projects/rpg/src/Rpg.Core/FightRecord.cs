using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>A recorded fight that cannot be replayed: unknown format, scenario or refused action.</summary>
public sealed class InvalidFightRecordException(string message) : Exception(message);

/// <summary>
/// A whole fight in a few hundred bytes: the scenario, the seed and the accepted actions. Replaying
/// them gives back the same fight, roll for roll, on every system; the server will use this to
/// check a fight it did not see. The seed is written as a string: a 64-bit integer does not fit
/// in a JavaScript number.
/// </summary>
public sealed record FightRecord(
    int Version,
    string Scenario,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)] ulong Seed,
    IReadOnlyList<FightAction> Actions)
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Compact = new(GameData.Json) { WriteIndented = false };

    public static FightRecord Of(Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        return new FightRecord(CurrentVersion, fight.Scenario.Id, fight.Seed, [.. fight.History]);
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
        if (Version != CurrentVersion)
            throw new InvalidFightRecordException($"Unknown record version {Version}.");
        if (!data.Scenarios.ContainsKey(Scenario))
            throw new InvalidFightRecordException($"Unknown scenario '{Scenario}'.");
        var fight = new Fight(data, Scenario, Seed);
        for (int i = 0; i < Actions.Count; i++)
        {
            ActionError error = fight.Apply(Actions[i]);
            if (error != ActionError.None)
                throw new InvalidFightRecordException($"Action {i} ({Actions[i]}) refused: {error}.");
        }
        return fight;
    }
}
