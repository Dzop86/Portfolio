using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>Compounds that consume their parents by the API, on PostgreSQL (sprint 187, D94): refused from any client, heroes of the old rule brought back to a legal state once.</summary>
public class CompoundsConsumeTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task Level(ApiFactory api, string account, int level)
    {
        using IServiceScope scope = api.Services.CreateScope();
        await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, level, []), Cancel);
    }

    [Fact]
    public async Task TwoCompoundsSharingAnElement_AreRefusedByTheServer()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("cc1");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "mage", cancel: Cancel);
        await Level(api, account, 100);
        ServerException refused = await Assert.ThrowsAsync<ServerException>(() => server.SavePoints(c.Id, new Points(null, null, Compounds: [Element.Lightning, Element.Magma]), Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, refused.Status);
        Assert.Equal([Element.Lightning, Element.Mud], (await server.SavePoints(c.Id, new Points(null, null, Compounds: [Element.Lightning, Element.Mud]), Cancel)).Compounds);
    }

    [Fact]
    public async Task HeroesOfTheOldRule_AreBroughtBackToALegalState_Once_TheirTalentsKept()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("cc2");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary plop = await server.CreateCharacter("Plop", "female-a", "mage", cancel: Cancel);
        CharacterSummary plif = await server.CreateCharacter("Plif", "female-a", "sentinel", cancel: Cancel);
        await Level(api, account, 100);
        string[] plifDeck = ["lightning-fire-arrow", "fire-arrow", "stone-arrow"];
        api.WithDb(db =>
        {
            Character mage = db.Characters.Single(x => x.Id == plop.Id), sentinel = db.Characters.Single(x => x.Id == plif.Id);
            (mage.Compounds, mage.Talents, mage.TalentsVersion) = ("Sand,Lightning,Ice,Steam,Mud,Ash,Salt,Magma,Sap,Ether", "first-gesture", 4);
            (sentinel.Compounds, sentinel.Deck, sentinel.TalentsVersion) = ("Lightning", string.Join(',', plifDeck), 4);
            return db.SaveChanges();
        });
        IReadOnlyList<CharacterSummary> list = await server.Characters(Cancel);
        CharacterSummary m = list.Single(x => x.Name == "Plop"), s = list.Single(x => x.Name == "Plif");
        Assert.Equal([Element.Sand, Element.Steam], m.Compounds);
        Assert.Equal(["first-gesture"], m.Talents);
        Assert.False(m.TalentsReset);
        Assert.True(m.CompoundsReset);
        Assert.Null(m.Hero.Problem(GameData.Embedded));
        Assert.Equal([Element.Lightning], s.Compounds);
        Assert.Equal(["lightning-fire-arrow", "stone-arrow"], s.Deck);
        Assert.Null(s.Hero.Problem(GameData.Embedded));
        // Once.
        Assert.All(await server.Characters(Cancel), x => Assert.False(x.CompoundsReset));
        Assert.Equal(Talents.Version, api.WithDb(db => db.Characters.Single(x => x.Id == plop.Id).TalentsVersion));
    }

    [Fact]
    public async Task TheOperators_CompoundsAll_GivesOnlyWhatFits()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("cc3");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        await server.CreateCharacter("Plop", "female-a", "mage", cancel: Cancel);
        await Level(api, account, 100);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), Admin.Parse(["admin", account, "--compounds", "all"]).Order!, Cancel);
        CharacterSummary c = (await server.Characters(Cancel)).Single();
        Assert.Null(c.Hero.Problem(GameData.Embedded));
        Assert.NotEmpty(c.Compounds!);
    }
}
