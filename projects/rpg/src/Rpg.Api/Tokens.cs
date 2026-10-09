using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Rpg.Api.Data;

namespace Rpg.Api;

/// <summary>
/// Signs the access tokens (JWT, HMAC-SHA256). The key comes from the configuration (<c>Jwt:Key</c>,
/// base64, at least 32 bytes, set through an environment variable); without one, a random key is
/// drawn at start-up and tokens do not survive a restart.
/// </summary>
public sealed partial class Tokens
{
    public const string Issuer = "rpg-api";
    public const string Audience = "rpg-players";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

    private readonly TimeProvider _clock;

    public Tokens(IConfiguration configuration, TimeProvider clock, ILogger<Tokens> logger)
    {
        _clock = clock;
        string? configured = configuration["Jwt:Key"];
        byte[] key;
        if (string.IsNullOrEmpty(configured))
        {
            key = RandomNumberGenerator.GetBytes(32);
            LogRandomKey(logger);
        }
        else
        {
            key = Convert.FromBase64String(configured);
            if (key.Length < 32)
                throw new InvalidOperationException("Jwt:Key must hold at least 32 bytes (256 bits).");
        }
        SigningKey = new SymmetricSecurityKey(key);
    }

    public SymmetricSecurityKey SigningKey { get; }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No Jwt:Key configured: tokens are signed with a random key and will not survive a restart.")]
    private static partial void LogRandomKey(ILogger logger);

    public (string Token, DateTimeOffset ExpiresAt) Issue(Account account)
    {
        DateTimeOffset now = _clock.GetUtcNow();
        DateTimeOffset expires = now + Lifetime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, account.Name),
            ]),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    public TokenValidationParameters ValidationParameters() => new()
    {
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = SigningKey,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        NameClaimType = JwtRegisteredClaimNames.Name,
        ClockSkew = TimeSpan.FromSeconds(30),
        LifetimeValidator = (notBefore, expires, _, parameters) =>
        {
            DateTime now = _clock.GetUtcNow().UtcDateTime;
            return (notBefore is null || notBefore <= now + parameters.ClockSkew) && expires is not null && now - parameters.ClockSkew < expires;
        },
    };
}
