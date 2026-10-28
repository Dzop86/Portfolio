using Microsoft.Extensions.DependencyInjection;
using Rpg.Api.Data;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>The operator's command (sprint 79): levels and items for the characters of an account.</summary>
public class AdminTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public void TheOrder_IsReadFromTheCommandLine_AndRefusedWhenWrong()
    {
        AdminOrder order = Admin.Parse(["admin", "tester", "--level", "100", "--give", "copper-ring", "--give", "dagger:2"]).Order!;
        Assert.Equal(("tester", 100), (order.Account, order.Level));
        Assert.Equal([new ItemCount("copper-ring", 1), new ItemCount("dagger", 2)], order.Give);
        Assert.Null(Admin.Parse(["admin", "tester", "--level", "101"]).Order);
        Assert.Null(Admin.Parse(["admin", "tester", "--level", "0"]).Order);
        Assert.Null(Admin.Parse(["admin", "tester", "--give", "no-such-item"]).Order);
        Assert.Null(Admin.Parse(["admin", "tester", "--give", "dagger:0"]).Order);
        Assert.Null(Admin.Parse(["admin", "tester", "--fly"]).Order);
        Assert.Null(Admin.Parse(["admin", "tester"]).Order);
        Assert.Equal([new NewHero("guard", "Ondine")], Admin.Parse(["admin", "tester", "--create", "guard:Ondine"]).Order!.Create);
        Assert.Null(Admin.Parse(["admin", "tester", "--create", "dragon:Ondine"]).Order);
        Assert.Null(Admin.Parse(["admin", "tester", "--create", "guard:ondine"]).Order);
        Assert.Null(Admin.Parse(["admin", "tester", "--create", "guard"]).Order);
        Assert.Equal([new Renaming("plop", "Plop")], Admin.Parse(["admin", "tester", "--rename", "plop:Plop"]).Order!.Rename);
        Assert.Null(Admin.Parse(["admin", "tester", "--rename", "Plop:pl0p"]).Order);
        Assert.Null(Admin.Parse(["admin", "tester", "--rename", "Plop"]).Order);
        Assert.Null(Admin.Parse(["admin", "--level", "3"]).Order);
        Assert.Null(Admin.Parse(["serve"]).Order);
        // Sprint 154: every monster known in full, for Plouf.
        Assert.True(Admin.Parse(["admin", "tester", "--bestiary", "full"]).Order!.Bestiary);
        Assert.Null(Admin.Parse(["admin", "tester", "--bestiary", "half"]).Order);
        // Sprint 157: Éclats to test the shops, for Plouf.
        Assert.Equal(1_000_000, Admin.Parse(["admin", "tester", "--shards", "1000000"]).Order!.Shards);
        Assert.Null(Admin.Parse(["admin", "tester", "--shards", "-5"]).Order);
        Assert.Null(Admin.Parse(["admin", "tester", "--shards", "2000000000"]).Order);
        Assert.Null(Admin.Parse(["admin", "tester", "--shards"]).Order);
    }

    [Fact]
    public async Task EveryCharacterOfTheAccount_ReachesTheLevel_WithItsPoints_AndGetsTheItems()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("adm");
        // Its own database: the only account there.
        string account = api.WithDb(db => db.Accounts.Single().Name);
        foreach ((string name, string cls) in new[] { ("Aube", "sentinel"), ("Brume", "guard"), ("Cendre", "mage") })
            await server.CreateCharacter(name, "female-c", cls, cancel: Cancel);
        IReadOnlyList<string> said;
        using (IServiceScope scope = api.Services.CreateScope())
            said = await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), Admin.Parse(["admin", account.ToLowerInvariant(), "--level", "100", "--give", "dagger:2"]).Order!, Cancel);
        Assert.Equal(3, said.Count);
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), Admin.Parse(["admin", account, "--shards", "1000000"]).Order!, Cancel);
        Assert.All(api.WithDb(db => db.Characters.Select(c => c.Shards).ToList()), s => Assert.Equal(1_000_000, s));
        IReadOnlyList<CharacterSummary> all = await server.Characters(Cancel);
        Assert.All(all, c =>
        {
            Assert.Equal((100, Progression.XpToReach(100)), (c.Level, c.Xp));
            Assert.Equal([new ItemCount("dagger", 2)], c.Inventory!);
        });
        // The level's points are there to spend, as the server checks them.
        CharacterSummary first = all[0];
        await server.SavePoints(first.Id, new Points(new Characteristics(Progression.CharacteristicPoints(100), 0, 0, 0, 0), new Dictionary<string, int>(), []), Cancel);
        // Brought down again, the points spent come back.
        using (IServiceScope scope = api.Services.CreateScope())
            await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder(account, 5, []), Cancel);
        CharacterSummary down = (await server.Characters(Cancel))[0];
        Assert.Equal((5, Characteristics.None), (down.Level, down.Stats));
        using (IServiceScope scope = api.Services.CreateScope())
            Assert.Equal(["No account named 'nobody'."], await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), new AdminOrder("nobody", 3, []), Cancel));
    }

    [Fact]
    public async Task TheOperator_CreatesCharactersForAnAccount_UnderTheSignUpsRules()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("adm");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        await server.CreateCharacter("Aube", "female-c", "sentinel", cancel: Cancel);
        AdminOrder order = Admin.Parse(["admin", account, "--create", "guard:Brume", "--create", "mage:Aube", "--level", "100", "--give", "admin-keyring"]).Order!;
        IReadOnlyList<string> said;
        using (IServiceScope scope = api.Services.CreateScope())
            said = await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(), order, Cancel);
        // Aube is taken: the guard alone is created, and both reach the level with the keyring.
        Assert.Contains("Aube: not created, the name is taken.", said);
        IReadOnlyList<CharacterSummary> all = await server.Characters(Cancel);
        Assert.Equal([("Aube", "sentinel"), ("Brume", "guard")], all.Select(c => (c.Name, c.Class)));
        Assert.All(all, c =>
        {
            Assert.Equal(100, c.Level);
            Assert.Equal([new ItemCount("admin-keyring", 1)], c.Inventory!);
        });
    }

    [Fact]
    public async Task TheOperator_RenamesACharacter_ToAFreeNameOfTheRule()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("adm");
        string account = api.WithDb(db => db.Accounts.Single().Name);
        await server.CreateCharacter("Aube", "female-c", "sentinel", cancel: Cancel);
        await server.CreateCharacter("Brume", "female-c", "guard", cancel: Cancel);
        // A name of the old rule, as some characters still have: changed in the base for the test.
        api.WithDb(db =>
        {
            Character aube = db.Characters.Single(c => c.Name == "Aube");
            (aube.Name, aube.NormalizedName) = ("aube", "AUBE");
            return db.SaveChanges();
        });
        IReadOnlyList<string> said;
        using (IServiceScope scope = api.Services.CreateScope())
            said = await Admin.Apply(scope.ServiceProvider.GetRequiredService<GameDb>(),
                Admin.Parse(["admin", account, "--rename", "aube:Aube", "--rename", "Brume:Aube", "--rename", "Nobody:Cendre"]).Order!, Cancel);
        Assert.Contains("Brume: not renamed, 'Aube' is taken.", said);
        Assert.Contains($"Nobody: no such character on '{account}'.", said);
        Assert.Equal(["Aube", "Brume"], (await server.Characters(Cancel)).Select(c => c.Name));
    }
}
