using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// The tree's versions by the API, on PostgreSQL: talents of an older tree given back once, for free, at the sign-in, the
/// deck keeping what it can; since sprint 182 (D93), a tree of version 3 losing only its elements, Lightning created for who
/// had Electrifying; compounds the level does not open refused from any client; the operator's <c>--builds</c> giving each
/// class its reference build, legal, and <c>--compounds</c>.
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
        CharacterSummary second = await server.CreateCharacter("Cendre", "female-b", "guard", cancel: Cancel);
        await Level(api, account, 45);
        // A character of before sprint 146: the opening of Lightning, its twin in the deck; another without talents.
        api.WithDb(db =>
        {
            Character old = db.Characters.Single(x => x.Id == c.Id);
            (old.Talents, old.Deck, old.TalentsVersion) = ("open-lightning,prism", "lightning-fire-arrow,stone-arrow", 1);
            db.Characters.Single(x => x.Id == other.Id).TalentsVersion = 1;
            // One of the tree of sprint 146 (version 2): the graph of sprint 147 gives its talents back too.
            Character v2 = db.Characters.Single(x => x.Id == second.Id);
            (v2.Talents, v2.TalentsVersion) = ("bogging,up-close", 2);
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
        Assert.True(first.Single(x => x.Id == second.Id).TalentsReset);
        Assert.Null(first.Single(x => x.Id == second.Id).Talents);
        Assert.All(await server.Characters(Cancel), x => Assert.False(x.TalentsReset));
        Assert.Equal(Talents.Version, api.WithDb(db => db.Characters.Single(x => x.Id == c.Id).TalentsVersion));
        // Free: no Éclats taken, at 45, out of a village.
        Assert.Equal(c.Shards, given.Shards);
        // A new character is of the new tree.
        Assert.Equal(Talents.Version, api.WithDb(db => db.Characters.Single(x => x.Id == other.Id).TalentsVersion));
    }

    [Fact]
    public async Task ATreeOfVersionThree_LosesItsElementsOnly_LightningCreated_TheDeckKept_TheStylesWhileThePointsPay()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("t3");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary guard = await server.CreateCharacter("Ondine", "female-a", "guard", cancel: Cancel);
        CharacterSummary mage = await server.CreateCharacter("Brume", "female-c", "mage", cancel: Cancel);
        await Level(api, account, 45);
        string deck = "lightning-burning-blade,shield-bash";
        api.WithDb(db =>
        {
            // A Guard of Lightning: Electrifying, its Transmutation and Prism, a style of Melee; its twin in the deck.
            Character g = db.Characters.Single(x => x.Id == guard.Id);
            (g.Talents, g.Deck, g.TalentsVersion) = ("electrifying,transmute-fire-lightning,prism,up-close,firm-footing,heavy-push", deck, 3);
            // A Mage of Mud at 30: one point now, its two styles' nodes too many.
            Character m = db.Characters.Single(x => x.Id == mage.Id);
            (m.Talents, m.TalentsVersion, m.Level, m.Xp) = ("bogging,side-step,up-close", 3, 30, Progression.XpToReach(30));
            return db.SaveChanges();
        });
        CharacterSummary[] after = [.. await server.Characters(Cancel)];
        CharacterSummary g = after.Single(x => x.Id == guard.Id), m = after.Single(x => x.Id == mage.Id);
        Assert.True(g.TalentsReset && m.TalentsReset);
        Assert.Equal([Element.Lightning], g.Compounds);
        Assert.Equal(["up-close", "firm-footing", "heavy-push"], g.Talents!);
        Assert.Equal(deck.Split(','), g.Deck!);
        Assert.Null(g.Hero.Problem(GameData.Embedded));
        Assert.Null(m.Compounds);
        Assert.Equal(["side-step"], m.Talents!);
        Assert.Null(m.Hero.Problem(GameData.Embedded));
        // Once.
        Assert.All(await server.Characters(Cancel), x => Assert.False(x.TalentsReset));
        Assert.Equal(4, api.WithDb(db => db.Characters.Single(x => x.Id == guard.Id).TalentsVersion));
    }

    [Fact]
    public async Task CompoundsTheLevelDoesNotOpen_AreRefused_FromAnyClient()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("tr");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-a", "guard", cancel: Cancel);
        await Level(api, account, 29);
        Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Compounds: [Element.Lightning]), Cancel)));
        await Level(api, account, 30);
        // A base element, twice the same: refused.
        foreach (Element[] compounds in new[] { new[] { Element.Fire }, [Element.Lightning, Element.Lightning] })
            Assert.Equal(HttpStatusCode.BadRequest, await Refused(() => server.SavePoints(c.Id, new Points(null, null, Compounds: compounds), Cancel)));
        CharacterSummary saved = await server.SavePoints(c.Id, new Points(null, null, Compounds: [Element.Lightning]), Cancel);
        Assert.Equal([Element.Lightning], saved.Compounds);
        // A client that sends none keeps them.
        Assert.Equal([Element.Lightning], (await server.SavePoints(c.Id, new Points(null, null), Cancel)).Compounds);
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
        Assert.All(said, line => Assert.Contains("compounds Lightning", line, StringComparison.Ordinal));
        foreach (CharacterSummary s in await server.Characters(Cancel))
        {
            Assert.Null(s.Hero.Problem(GameData.Embedded));
            Assert.Equal(Progression.Talented(new Hero(s.Name, s.Look, s.Class, Level: 100), $"{s.Class}-matter", GameData.Embedded).Talents!.Order(StringComparer.Ordinal),
                s.Talents!.Order(StringComparer.Ordinal));
            Assert.Contains(s.Hero.DeckOf(GameData.Embedded), id => GameData.Embedded.Spells[id].Element == Element.Lightning);
        }
        // --compounds none, then all: as far as the level opens them, the deck keeping what its grimoire still has.
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), Admin.Parse(["admin", account, "--compounds", "none"]).Order!, Cancel);
        Assert.All(await server.Characters(Cancel), s => Assert.True(s.Compounds is null && s.Hero.Problem(GameData.Embedded) is null));
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), Admin.Parse(["admin", account, "--compounds", "all"]).Order!, Cancel);
        Assert.All(await server.Characters(Cancel), s => Assert.Equal(Elements.Compounds, s.Compounds!));
        Assert.Equal([Element.Lightning], Admin.Parse(["admin", account, "--compounds", "lightning"]).Order!.Compounds);
        Assert.NotNull(Admin.Parse(["admin", account, "--compounds", "fire"]).Error);
    }
}
