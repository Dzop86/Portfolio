using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Rpg.Client;

/// <summary>Seals and opens a secret for this player on this computer (the password remembered).</summary>
public interface ISecretBox
{
    byte[] Seal(byte[] secret);

    /// <summary>The secret, or null when it was sealed elsewhere (another computer, another user).</summary>
    byte[]? Open(byte[] sealedSecret);
}

/// <summary>
/// AES-GCM with a key of 32 bytes: the box of the platforms without a store of their own for the
/// user's secrets (the game derives the key from the computer's id; see D65 for the limits).
/// </summary>
public sealed class AesSecretBox(byte[] key) : ISecretBox
{
    private readonly byte[] _key = key is { Length: 32 } ? key : throw new ArgumentException("A key of 32 bytes.", nameof(key));

    /// <summary>A key derived from a text the computer keeps (its id, the user's name).</summary>
    public static AesSecretBox FromText(string text) => new(SHA256.HashData(Encoding.UTF8.GetBytes("osmose sign-in " + text)));

    public byte[] Seal(byte[] secret)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        byte[] cipher = new byte[secret.Length];
        using var aes = new AesGcm(_key, tag.Length);
        aes.Encrypt(nonce, secret, cipher, tag);
        return [.. nonce, .. tag, .. cipher];
    }

    public byte[]? Open(byte[] sealedSecret)
    {
        int n = AesGcm.NonceByteSizes.MaxSize, t = AesGcm.TagByteSizes.MaxSize;
        if (sealedSecret.Length < n + t)
            return null;
        byte[] plain = new byte[sealedSecret.Length - n - t];
        try
        {
            using var aes = new AesGcm(_key, t);
            aes.Decrypt(sealedSecret.AsSpan(0, n), sealedSecret.AsSpan(n + t), sealedSecret.AsSpan(n, t), plain);
            return plain;
        }
        catch (AuthenticationTagMismatchException)
        {
            return null;
        }
    }
}

/// <summary>What the sign-in screen remembers: the account's name, and its password if asked (sealed).</summary>
public sealed record RememberedSignIn(string Name, string Password, bool RememberName, bool RememberPassword)
{
    public static readonly RememberedSignIn None = new("", "", false, false);
}

/// <summary>
/// The sign-in screen's memory, as the launcher's (sprint 51) in the game itself (sprint 79, D65):
/// the name kept if "Remember my account name" is ticked, the password too, sealed by an
/// <see cref="ISecretBox"/>, if "Remember my password" is; remembering the password remembers the
/// name. Kept in the player's settings, written after a sign-in only.
/// </summary>
public static class SignInMemory
{
    public const string FileName = "sign-in.json";

    private sealed record Stored(string? Name, string? Password);

    /// <summary>What to write after signing in; nothing ticked, nothing kept.</summary>
    public static string Write(RememberedSignIn sign, ISecretBox box)
    {
        bool name = sign.RememberName || sign.RememberPassword;
        string? password = sign.RememberPassword && sign.Password.Length > 0
            ? Convert.ToBase64String(box.Seal(Encoding.UTF8.GetBytes(sign.Password)))
            : null;
        return JsonSerializer.Serialize(new Stored(name ? sign.Name : null, password));
    }

    /// <summary>What the screen starts with; a file damaged, or sealed elsewhere, gives what can still be read.</summary>
    public static RememberedSignIn Read(string? json, ISecretBox box)
    {
        Stored? stored;
        try
        {
            stored = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<Stored>(json);
        }
        catch (JsonException)
        {
            stored = null;
        }
        if (stored?.Name is not { Length: > 0 } name)
            return RememberedSignIn.None;
        byte[]? password = null;
        if (stored.Password is string sealedPassword)
        {
            try
            {
                password = box.Open(Convert.FromBase64String(sealedPassword));
            }
            catch (FormatException)
            {
            }
        }
        return password is null
            ? new RememberedSignIn(name, "", true, false)
            : new RememberedSignIn(name, Encoding.UTF8.GetString(password), true, true);
    }
}
