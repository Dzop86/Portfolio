using System.Text.Json.Serialization;
using Rogue.Core;

namespace Rogue.Api.Endpoints;

/// <summary>A name (3 to 20 letters, digits, - or _) and a password (10 to 128 characters).</summary>
public sealed record Credentials(string? Name, string? Password);

public sealed record AccountCreated(string Name);

/// <summary>A bearer token to send in the <c>Authorization</c> header.</summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>A ranked run to play: its seed (a decimal string, as in the run format) and its deadline.</summary>
public sealed record RunTicket(Guid Id, string Seed, DateTimeOffset ExpiresAt);

/// <summary>A run in the format written by the game: <c>{"format":"rogue-run","version":1,"seed":"…","actions":"…"}</c>.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RunSubmission(string? Format, int? Version, string? Seed, string? Actions);

/// <summary>The outcome computed by the server when it replayed the run.</summary>
public sealed record RunResult(Guid Id, GameState Outcome, int Score, int Depth, int Turns, int Kills);

public sealed record ScoreEntry(int Rank, string Player, int Score, GameState Outcome, int Depth, int Turns, DateTimeOffset PlayedAt);

public sealed record MyRun(Guid Id, string Seed, Data.RunStatus Status, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt, GameState? Outcome, int? Score, string? Rejection);
