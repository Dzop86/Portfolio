using System.Security.Cryptography;

namespace Rpg.Client.Tests;

/// <summary>The sign-in screen's memory (sprint 79): the name, the password sealed, nothing unticked.</summary>
public class SignInMemoryTests
{
    private static readonly AesSecretBox Box = AesSecretBox.FromText("this computer");

    [Fact]
    public void TheNameAndThePassword_ComeBack_ThePasswordNeverInClear()
    {
        string json = SignInMemory.Write(new RememberedSignIn("Plume", "a long password", true, true), Box);
        Assert.DoesNotContain("a long password", json);
        Assert.Equal(new RememberedSignIn("Plume", "a long password", true, true), SignInMemory.Read(json, Box));
    }

    [Fact]
    public void TheNameAlone_OrNothing_IsKeptAsTicked()
    {
        Assert.Equal(new RememberedSignIn("Plume", "", true, false),
            SignInMemory.Read(SignInMemory.Write(new RememberedSignIn("Plume", "a long password", true, false), Box), Box));
        Assert.Equal(RememberedSignIn.None,
            SignInMemory.Read(SignInMemory.Write(new RememberedSignIn("Plume", "a long password", false, false), Box), Box));
        // Remembering the password remembers the name it goes with.
        Assert.Equal(new RememberedSignIn("Plume", "a long password", true, true),
            SignInMemory.Read(SignInMemory.Write(new RememberedSignIn("Plume", "a long password", false, true), Box), Box));
    }

    [Fact]
    public void APasswordSealedElsewhere_OrAFileDamaged_GivesWhatCanStillBeRead()
    {
        string json = SignInMemory.Write(new RememberedSignIn("Plume", "a long password", true, true), Box);
        Assert.Equal(new RememberedSignIn("Plume", "", true, false), SignInMemory.Read(json, AesSecretBox.FromText("another computer")));
        Assert.Equal(RememberedSignIn.None, SignInMemory.Read("{ not json", Box));
        Assert.Equal(RememberedSignIn.None, SignInMemory.Read(null, Box));
        Assert.Equal(new RememberedSignIn("Plume", "", true, false), SignInMemory.Read("""{"Name":"Plume","Password":"%%%"}""", Box));
    }

    [Fact]
    public void TheBox_SealsDifferentlyEachTime_AndRefusesATamperedSecret()
    {
        byte[] secret = "secret"u8.ToArray();
        byte[] a = Box.Seal(secret), b = Box.Seal(secret);
        Assert.NotEqual(a, b);
        Assert.Equal(secret, Box.Open(a));
        a[^1] ^= 1;
        Assert.Null(Box.Open(a));
        Assert.Null(Box.Open([1, 2, 3]));
        Assert.Throws<ArgumentException>(() => new AesSecretBox(RandomNumberGenerator.GetBytes(16)));
    }
}
