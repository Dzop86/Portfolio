using System.Net;
using System.Net.Http.Headers;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>Charles's try of sprint 80 (D66): a deck of any size saved, and a session run out replaced without a word.</summary>
public class DeckSaveTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FourFireSpells_OneSpell_OrNone_AreSaved_AndComeBackAsChosen()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("dk");
        CharacterSummary c = await server.CreateCharacter("Braise", "female-a", "mage", cancel: Cancel);
        HeroClass mage = GameData.Embedded.Class("mage")!;
        // Never chosen: the class's first spells (null on the wire).
        Assert.Null(c.Deck);
        Assert.Equal(mage.Spells.Where(id => GameData.Embedded.Spells[id].Level <= 1), c.Hero.DeckOf(GameData.Embedded));
        string[] fire = [.. mage.Spells.Where(id => GameData.Embedded.Spells[id].Element == Element.Fire && GameData.Embedded.Spells[id].Level <= 1)];
        Assert.NotEmpty(fire);
        foreach (string[] deck in new[] { fire, [fire[0]], [] })
        {
            CharacterSummary saved = await server.SavePoints(c.Id, new Points(null, null, deck), Cancel);
            Assert.Equal(deck, saved.Deck!);
            Assert.Equal(deck, (await server.Characters(Cancel)).Single().Hero.DeckOf(GameData.Embedded));
        }
        // Without a deck, back to the class's first spells.
        Assert.Null((await server.SavePoints(c.Id, new Points(null, null), Cancel)).Deck);
        // Thirteen, twice the same, or a locked one: refused.
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePoints(c.Id, new Points(null, null, [fire[0], fire[0]]), Cancel))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePoints(c.Id, new Points(null, null, [mage.Spells[^1]]), Cancel))).Status);
    }

    [Fact]
    public async Task ATokenNoLongerValid_IsReplaced_WithThisSessionsPassword()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("tk");
        CharacterSummary c = await server.CreateCharacter("Braise", "female-a", "mage", cancel: Cancel);
        // Twelve hours later, the token has run out: the next call signs in again by itself.
        api.Clock.Advance(Tokens.Lifetime + TimeSpan.FromMinutes(1));
        Assert.Equal(c.Id, (await server.Characters(Cancel)).Single().Id);
        // A token signed with another key (the server restarted with a new one): the same.
        server.Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.token");
        CharacterSummary saved = await server.SavePoints(c.Id, new Points(new Characteristics(Fire: 0), null, []), Cancel);
        Assert.Empty(saved.Deck!);
        // Signed in by the launcher's token, there is no password: the refusal stays.
        var launcher = new GameServer(api.CreateClient());
        launcher.UseToken("not.a.token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await Assert.ThrowsAsync<ServerException>(() => launcher.Characters(Cancel))).Status);
    }
}
