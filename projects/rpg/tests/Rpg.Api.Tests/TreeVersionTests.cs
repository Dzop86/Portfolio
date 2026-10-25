using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The tree of sprint 146 (D82) by the API, on PostgreSQL: talents of the first tree given back once, for free, at the
/// sign-in, the deck keeping what it can; a tree the new rules refuse (two pairs sharing an element, a Conversion before
/// 45) refused from any client; the operator's <c>--builds</c> giving each class its reference build, legal.
/// </summary>
public class TreeVersionTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static async Task<HttpStatusCode> Refused(Func<Task> call) => (await Assert.ThrowsAsync<ServerException>(call)).Status;

    private static async Task Level(ApiFactory api, string account, int level)
    {
        using IServiceScope scope = api.Services.CreateScope();
        await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, level, []), Cancel);
    }

    [Fact]
    public async Task TalentsOfTheFirstTree_AreGivenBackOnce_AtTheSignIn_TheDeckKeepingWhatItCan()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("tv");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "sentinel", cancel: Cancel);
        CharacterSummary other = await server.CreateCharacter("Brume", "female-c", "mage", cancel: Cancel);
        await Level(api, account, 45);
        // A character of before sprint 146: the opening of Lightning, its twin in the deck; another without talents.
        api.WithDb(db =>
        {
            Character old = db.Characters.Single(x => x.Id == c.Id);
            (old.Talents, old.Deck, old.TalentsVersion) = ("open-lightning,prism", "lightning-fire-arrow,stone-arrow", 1);
            db.Characters.Single(x => x.Id == other.Id).TalentsVersion = 1;
            return db.SaveChanges();
        });
        CharacterSummary[] first = [.. await server.Characters(Cancel)];
        CharacterSummary given = first.Single(x => x.Id == c.Id);
        Assert.True(given.TalentsReset);
        Assert.Null(given.Talents);
        Assert.Equal(["stone-arrow"], given.Deck!);
        Assert.Null(given.Hero.Problem(GameData.Embedded));
        // Nothing to give back: nothing said; and only once.
        Assert.False(first.Single(x => x.Id == other.Id).TalentsReset);
        Assert.All(await server.Characters(Cancel), x => Assert.False(x.TalentsReset));
        Assert.Equal(Talents.Version, api.WithDb(db => db.Characters.Single(x => x.Id == c.Id).TalentsVersion));
        // Free: no Éclats taken, at 45, out of a village.
        Assert.Equal(c.Shards, given.Shards);
        // A new character is of the new tree.
        Assert.Equal(Talents.Version, api.WithDb(db => db.Characters.Single(x => x.Id == other.Id).TalentsVersion));
    }

    [Fact]
    public async Task ATreeTheNewRulesRefuse_IsRefused_FromAnyClient()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("tr");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "guard", cancel: Cancel);
        await Level(api, account, 44);
        // Mud and Ice share Water; a Conversion before 45; Prism without a Transmutation; both Transmutations.
        foreach (string[] tree in new[] { new[] { "bogging", "chilling" }, ["electrifying", "convert-lightning"], ["electrifying", "prism"],
                     ["electrifying", "transmute-fire-lightning", "transmute-air-lightning"] })
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Talents: tree), Cancel)));
        // Lightning and Mud make a couple.
        CharacterSummary saved = await server.SavePoints(c.Id, new Points(null, null, Talents: ["electrifying", "transmute-fire-lightning", "prism", "bogging"]), Cancel);
        Assert.Equal(4, saved.Talents!.Count);
    }

    [Fact]
    public async Task TheOperatorsBuilds_GiveEachClassItsReferenceBuild_Legal()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("tb");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        foreach ((string cls, string name) in new[] { ("mage", "Plop"), ("guard", "Pluf"), ("sentinel", "Plif") })
            await server.CreateCharacter(name, "female-a", cls, cancel: Cancel);
        Assert.Null(Admin.Parse(["admin", account, "--builds"]).Error);
        AdminOrder order = Admin.Parse(["admin", account, "--level", "100", "--builds"]).Order!;
        Assert.True(order.Builds);
        IReadOnlyList<string> said;
        using (IServiceScope scope = api.Services.CreateScope())
            said = await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), order, Cancel);
        Assert.All(said, line => Assert.Contains("convert-lightning", line, StringComparison.Ordinal));
        foreach (CharacterSummary s in await server.Characters(Cancel))
        {
            Assert.Null(s.Hero.Problem(GameData.Embedded));
            Assert.Equal(Progression.Talented(new Hero(s.Name, s.Look, s.Class, Level: 100), $"{s.Class}-matter", GameData.Embedded).Talents!.Order(StringComparer.Ordinal),
                s.Talents!.Order(StringComparer.Ordinal));
            Assert.Contains(s.Hero.DeckOf(GameData.Embedded), id => GameData.Embedded.Spells[id].Element == Element.Lightning);
        }
    }
}
