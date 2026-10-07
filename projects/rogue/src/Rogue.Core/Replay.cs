using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Rogue.Core;

/// <summary>
/// A recorded run: the seed and the actions played, one character each (see <see cref="ActionCodec"/>).
/// Its JSON form is <c>{"format":"rogue-run","version":1,"seed":"42","actions":"nnee>"}</c>; the
/// seed is a string because a 64-bit integer does not fit a JavaScript number.
/// </summary>
public sealed record RunRecord(ulong Seed, string Actions)
{
    public const string FormatName = "rogue-run";
    public const int CurrentVersion = 1;
    public const int MaxActions = 100_000;

    public static RunRecord From(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return new RunRecord(game.Seed, ActionCodec.Encode(game.History));
    }

    public string ToJson()
    {
        using var stream = new MemoryStream();
        // The default encoder writes ">" as "\u003E" (safe inside HTML); the relaxed one keeps it readable.
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteString("format", FormatName);
            writer.WriteNumber("version", CurrentVersion);
            writer.WriteString("seed", Seed.ToString(CultureInfo.InvariantCulture));
            writer.WriteString("actions", Actions);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Reads a run; the structure is checked here, the actions when the run is replayed.</summary>
    /// <exception cref="RunFormatException">Not a run of a version this library knows.</exception>
    public static RunRecord Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException e)
        {
            throw new RunFormatException(ReplayError.BadFormat, "Not JSON: " + e.Message);
        }
        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new RunFormatException(ReplayError.BadFormat, "A run is a JSON object.");
            string? format = null, seed = null, actions = null;
            int? version = null;
            foreach (JsonProperty property in root.EnumerateObject())
            {
                JsonElement value = property.Value;
                switch (property.Name)
                {
                    case "format" when value.ValueKind == JsonValueKind.String && format is null:
                        format = value.GetString();
                        break;
                    case "version" when value.ValueKind == JsonValueKind.Number && version is null && value.TryGetInt32(out int v):
                        version = v;
                        break;
                    case "seed" when value.ValueKind == JsonValueKind.String && seed is null:
                        seed = value.GetString();
                        break;
                    case "actions" when value.ValueKind == JsonValueKind.String && actions is null:
                        actions = value.GetString();
                        break;
                    default:
                        throw new RunFormatException(ReplayError.BadFormat, $"Unexpected or repeated property \"{property.Name}\".");
                }
            }
            return FromParts(format, version, seed, actions);
        }
    }

    /// <summary>Checks the four fields of a run, read from JSON by the caller (the score API binds them itself).</summary>
    /// <exception cref="RunFormatException">Not a run of a version this library knows.</exception>
    public static RunRecord FromParts(string? format, int? version, string? seed, string? actions)
    {
        if (format != FormatName)
            throw new RunFormatException(ReplayError.BadFormat, $"The format must be \"{FormatName}\".");
        if (version != CurrentVersion)
            throw new RunFormatException(ReplayError.UnsupportedVersion, $"Only version {CurrentVersion} is supported.");
        if (!TryParseSeed(seed, out ulong parsed))
            throw new RunFormatException(ReplayError.BadSeed, "The seed must be an unsigned 64-bit integer written in decimal.");
        if (actions is null)
            throw new RunFormatException(ReplayError.BadFormat, "The actions are missing.");
        return new RunRecord(parsed, actions);
    }

    /// <summary>Digits only: no sign, spaces or leading zeros, so that a seed has a single spelling.</summary>
    public static bool TryParseSeed(string? text, out ulong seed)
    {
        seed = 0;
        return text is { Length: > 0 and <= 20 } && text.All(char.IsAsciiDigit)
            && (text.Length == 1 || text[0] != '0')
            && ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out seed);
    }
}

public enum ReplayError
{
    None,
    BadFormat,
    UnsupportedVersion,
    BadSeed,
    TooLong,
    UnknownAction,
    IllegalAction,
    ActionAfterEnd,
}

public sealed class RunFormatException : Exception
{
    public RunFormatException(ReplayError error, string message)
        : base(message) => Error = error;

    public RunFormatException()
    {
    }

    public RunFormatException(string message)
        : base(message)
    {
    }

    public RunFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ReplayError Error { get; } = ReplayError.BadFormat;
}

/// <summary>
/// The outcome of a replay. <see cref="ActionIndex"/> is the position of the faulty action when the
/// run is rejected. A valid run may be unfinished (<see cref="GameState.Playing"/>).
/// </summary>
public sealed record ReplayResult(ReplayError Error, int? ActionIndex, GameState State, int Score, int Depth, int Turns, int Kills)
{
    public bool IsValid => Error == ReplayError.None;
}

public static class Replay
{
    /// <summary>Replays a run from its JSON form and recomputes its outcome.</summary>
    public static ReplayResult Run(string json)
    {
        RunRecord run;
        try
        {
            run = RunRecord.Parse(json);
        }
        catch (RunFormatException e)
        {
            return new ReplayResult(e.Error, null, GameState.Playing, 0, 0, 0, 0);
        }
        return Run(run);
    }

    /// <summary>Plays the actions from the seed, refusing any action the rules do not allow at that point.</summary>
    public static ReplayResult Run(RunRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var game = new Game(run.Seed);
        ReplayResult Result(ReplayError error, int? index) =>
            new(error, index, game.State, game.Score, game.Depth, game.Turn, game.Kills);

        if (run.Actions.Length > RunRecord.MaxActions)
            return Result(ReplayError.TooLong, RunRecord.MaxActions);
        for (int i = 0; i < run.Actions.Length; i++)
        {
            if (!ActionCodec.TryParse(run.Actions[i], out GameAction action))
                return Result(ReplayError.UnknownAction, i);
            if (game.State != GameState.Playing)
                return Result(ReplayError.ActionAfterEnd, i);
            if (!game.CanApply(action))
                return Result(ReplayError.IllegalAction, i);
            game.Apply(action);
        }
        return Result(ReplayError.None, null);
    }
}
