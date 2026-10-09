using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

public class CharacterTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task APlayerCreatesListsAndDeletesCharacters()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary elise = await server.CreateCharacter("Élise", "female-c", "sentinel", cancel: Cancel);
        api.Clock.Advance(TimeSpan.FromMinutes(1));
        CharacterSummary jean = await server.CreateCharacter("Jean-Luc", "male-a", "sentinel", cancel: Cancel);
        Assert.Equal([elise, jean], await server.Characters(Cancel));
        Assert.Equal(new Hero("Élise", "female-c", "sentinel"), elise.Hero);
        await server.DeleteCharacter(elise.Id, Cancel);
        Assert.Equal([jean], await server.Characters(Cancel));
        var e = await Assert.ThrowsAsync<ServerException>(() => server.DeleteCharacter(elise.Id, Cancel));
        Assert.Equal(HttpStatusCode.NotFound, e.Status);
    }

    [Theory]
    [InlineData("Él", "female-a", "sentinel", 0, "name")]
    [InlineData("R2D2", "female-a", "sentinel", 0, "name")]
    [InlineData("Élise", "orc", "sentinel", 0, "look")]
    [InlineData("Élise", "", "sentinel", 0, "look")]
    [InlineData("Élise", "female-a", "dragon", 0, "class")]
    [InlineData("Élise", "female-a", "", 0, "class")]
    [InlineData("Élise", "female-a", "mage", 7, "colour")]
    [InlineData("Élise", "female-a", "mage", -1, "colour")]
    public async Task InvalidCharacters_AreRefused_WithTheFieldAtFault(string name, string look, string heroClass, int colour, string field)
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        var e = await Assert.ThrowsAsync<ServerException>(() => server.CreateCharacter(name, look, heroClass, colour, cancel: Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, e.Status);
        Assert.Contains(field + ":", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACharacterNameIsUniqueOnTheServer_WhateverItsCase()
    {
        await using var api = new ApiFactory();
        GameServer first = await api.SignedIn("a");
        GameServer second = await api.SignedIn("b");
        await first.CreateCharacter("Élise", "female-c", "sentinel", cancel: Cancel);
        var e = await Assert.ThrowsAsync<ServerException>(() => second.CreateCharacter("ÉLISE", "male-b", "sentinel", cancel: Cancel));
        Assert.Equal(HttpStatusCode.Conflict, e.Status);
    }

    [Fact]
    public async Task AnAccountHasAtMostFiveCharacters()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        string[] names = ["Anne", "Bruno", "Chloé", "Damien", "Elsa"];
        foreach (string n in names)
            await server.CreateCharacter(n, "male-d", "sentinel", cancel: Cancel);
        var e = await Assert.ThrowsAsync<ServerException>(() => server.CreateCharacter("Fanny", "male-d", "sentinel", cancel: Cancel));
        Assert.Equal(HttpStatusCode.Conflict, e.Status);
        Assert.Equal(Accounts.MaxCharacters, (await server.Characters(Cancel)).Count);
    }

    [Fact]
    public async Task APlayerSeesAndDeletesOnlyTheirOwnCharacters()
    {
        await using var api = new ApiFactory();
        GameServer alice = await api.SignedIn("a");
        GameServer bob = await api.SignedIn("b");
        CharacterSummary hers = await alice.CreateCharacter("Alice", "female-b", "sentinel", cancel: Cancel);
        Assert.Empty(await bob.Characters(Cancel));
        var e = await Assert.ThrowsAsync<ServerException>(() => bob.DeleteCharacter(hers.Id, Cancel));
        Assert.Equal(HttpStatusCode.NotFound, e.Status);
        Assert.Single(await alice.Characters(Cancel));
    }

    /// <summary>End to end: the character made on the server fights in the rules, and its record replays.</summary>
    [Fact]
    public async Task ACharacterFromTheServer_FightsAsTheHero()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary c = await server.CreateCharacter("Ondine", "female-e", "sentinel", cancel: Cancel);
        var fight = new Fight(GameData.Embedded, "training", 11, (await server.Characters(Cancel)).Single().Hero);
        Ai.PlayOut(fight);
        Fight again = FightRecord.FromJson(FightRecord.Of(fight).ToJson()).Replay(GameData.Embedded);
        Assert.Equal(c.Name, again.Fighters.First(f => f.Team == 0).Name.Fr);
        Assert.Equal(fight.WinningTeam, again.WinningTeam);
    }

    [Fact]
    public async Task TheClassAndColour_AreKept_AndTheHeroFightsWithTheClassesSpells()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary made = await server.CreateCharacter("Élise", "female-c", "mage", 3, cancel: Cancel);
        CharacterSummary listed = Assert.Single(await server.Characters(Cancel));
        Assert.Equal(made, listed);
        Assert.Equal(new Hero("Élise", "female-c", "mage", 3), listed.Hero);
        var fight = new Fight(GameData.Embedded, "training", 5, listed.Hero);
        Assert.Equal(GameData.Embedded.Class("mage")!.Spells.Where(id => GameData.Embedded.Spells[id].Level == 1), fight.Fighters.First(f => f.Team == 0).Spells.Select(s => s.Id));
    }

    /// <summary>A database of sprint 47 moves on: its characters, written without a class, become sentinels.</summary>
    [Fact]
    public async Task CharactersMadeBeforeClasses_BecomeSentinels()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        Guid account = api.WithDb(db => db.Accounts.Single().Id);
        // The insert of sprint 47, which did not know the columns.
        api.WithDb(db => db.Database.ExecuteSql($"""
            INSERT INTO characters ("Id", "AccountId", "Name", "NormalizedName", "Look", "CreatedAt")
            VALUES ({Guid.NewGuid()}, {account}, 'Ondine', 'ONDINE', 'female-e', now())
            """));
        CharacterSummary old = Assert.Single(await server.Characters(Cancel));
        Assert.Equal(("sentinel", 0, "osmeria", 1), (old.Class, old.Colour, old.Server, old.Level));
        Assert.Null(old.Hero.Problem(GameData.Embedded));
    }

    [Fact]
    public async Task WhereACharacterStands_IsSaved_IfTheyCouldWalkThere()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary c = await server.CreateCharacter("Élise", "female-c", "mage", 3, cancel: Cancel);
        Assert.Null(c.Place);
        await server.SavePlace(c.Id, new Place("clairval", 1, 6), Cancel);
        Assert.Equal(new Place("clairval", 1, 6), Assert.Single(await server.Characters(Cancel)).Place);
        // A house, someone else's cell, outside the town, an unknown town: refused, the place kept.
        foreach (Place wrong in new[] { new Place("clairval", 4, 0), new Place("clairval", 8, 9), new Place("clairval", 40, 2), new Place("atlantis", 1, 1) })
        {
            var e = await Assert.ThrowsAsync<ServerException>(() => server.SavePlace(c.Id, wrong, Cancel));
            Assert.Equal(HttpStatusCode.BadRequest, e.Status);
        }
        Assert.Equal(new Place("clairval", 1, 6), Assert.Single(await server.Characters(Cancel)).Place);
        // Another zone of the world (sprint 60): the place is kept there too.
        await server.SavePlace(c.Id, new Place("misty-heath", 6, 8), Cancel);
        Assert.Equal(new Place("misty-heath", 6, 8), Assert.Single(await server.Characters(Cancel)).Place);
        Assert.Equal(HttpStatusCode.BadRequest, (await Assert.ThrowsAsync<ServerException>(() => server.SavePlace(c.Id, new Place("misty-heath", 0, 0), Cancel))).Status);
        await server.SavePlace(c.Id, new Place("clairval", 1, 6), Cancel);
        // Nobody moves someone else's character.
        GameServer other = await api.SignedIn("o");
        var notMine = await Assert.ThrowsAsync<ServerException>(() => other.SavePlace(c.Id, new Place("clairval", 2, 6), Cancel));
        Assert.Equal(HttpStatusCode.NotFound, notMine.Status);
    }

    [Fact]
    public async Task TheListSays_OnlyWhatIsStored()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        CharacterSummary c = await server.CreateCharacter("Élise", "female-c", "mage", 3, cancel: Cancel);
        await server.SavePlace(c.Id, new Place("clairval", 1, 6), Cancel);
        using JsonDocument list = JsonDocument.Parse(await server.Http.GetStringAsync(new Uri("api/characters", UriKind.Relative), Cancel));
        JsonElement only = list.RootElement.EnumerateArray().Single();
        Assert.Equal(["id", "name", "look", "class", "colour", "createdAt", "place", "server", "level", "hair", "skin", "height", "build", "xp", "stats", "ranks", "quests", "inventory", "worn"], only.EnumerateObject().Select(p => p.Name));
        Assert.Equal(["town", "x", "y"], only.GetProperty("place").EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public async Task TheWholeAppearance_IsKept_AndCheckedFieldByField()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn();
        var hero = new Hero("Élise", "female-c", "mage", 3, Hair: 5, Skin: 2, Height: 1, Build: -2);
        await server.CreateCharacter(hero, cancel: Cancel);
        Assert.Equal(hero, Assert.Single(await server.Characters(Cancel)).Hero);
        foreach ((Hero wrong, string field) in new[] { (hero with { Name = "Anne", Hair = 8 }, "hair"), (hero with { Name = "Anne", Skin = 5 }, "skin"), (hero with { Name = "Anne", Height = 3 }, "height"), (hero with { Name = "Anne", Build = -3 }, "build") })
        {
            var e = await Assert.ThrowsAsync<ServerException>(() => server.CreateCharacter(wrong, cancel: Cancel));
            Assert.Equal(HttpStatusCode.BadRequest, e.Status);
            Assert.Contains(field + ":", e.Message, StringComparison.Ordinal);
        }
    }
}
