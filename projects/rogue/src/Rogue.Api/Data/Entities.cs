using Rogue.Core;

namespace Rogue.Api.Data;

public sealed class Player
{
    public Guid Id { get; set; }

    /// <summary>The name as typed at sign-up, shown on the leaderboard.</summary>
    public required string Name { get; set; }

    /// <summary>Upper-case name: two accounts cannot differ only by case.</summary>
    public required string NormalizedName { get; set; }

    /// <summary>PBKDF2 hash from ASP.NET Core Identity's password hasher, never the password.</summary>
    public string PasswordHash { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }

    public List<RankedRun> Runs { get; } = [];
}

public enum RunStatus
{
    /// <summary>Seed handed out, nothing submitted yet.</summary>
    Open,

    /// <summary>Replayed by the server, score recorded.</summary>
    Scored,

    /// <summary>Submitted but refused (see <see cref="RankedRun.Rejection"/>).</summary>
    Rejected,
}

/// <summary>A ranked run: the server draws its seed, and accepts one submission for it before it expires.</summary>
public sealed class RankedRun
{
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public ulong Seed { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public RunStatus Status { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    /// <summary>The actions as submitted, kept so that any score can be replayed again later.</summary>
    public string? Actions { get; set; }

    public string? Rejection { get; set; }

    public GameState? Outcome { get; set; }

    public int? Score { get; set; }

    public int? Depth { get; set; }

    public int? Turns { get; set; }

    public int? Kills { get; set; }

    /// <summary>PostgreSQL's row version (xmin): of two submissions read at the same time, only the first is saved.</summary>
    public uint Version { get; set; }
}
