using System.Net;
using System.Net.Http.Json;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

public class AccountTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SignUp_ThenSignIn_GivesATokenThatOpensTheCharacters()
    {
        await using var api = new ApiFactory();
        var server = new GameServer(api.CreateClient());
        await server.SignUp("ada_lovelace", "correct horse battery", Cancel);
        Assert.False(server.SignedIn);
        await server.SignIn("ADA_LOVELACE", "correct horse battery", Cancel);
        Assert.True(server.SignedIn);
        Assert.Empty(await server.Characters(Cancel));
        // The password is stored hashed, never as is.
        string hash = api.WithDb(db => db.Accounts.Single().PasswordHash);
        Assert.DoesNotContain("correct horse battery", hash, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ab", "correct horse battery", "name")]
    [InlineData("with space", "correct horse battery", "name")]
    [InlineData("ada_lovelace", "short", "password")]
    public async Task InvalidAccounts_AreRefused_WithTheFieldAtFault(string name, string password, string field)
    {
        await using var api = new ApiFactory();
        var e = await Assert.ThrowsAsync<ServerException>(() => new GameServer(api.CreateClient()).SignUp(name, password, Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, e.Status);
        Assert.Contains(field + ":", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ANameIsTaken_WhateverItsCase()
    {
        await using var api = new ApiFactory();
        var server = new GameServer(api.CreateClient());
        await server.SignUp("Ada_Lovelace", "correct horse battery", Cancel);
        var e = await Assert.ThrowsAsync<ServerException>(() => server.SignUp("ada_lovelace", "another password!", Cancel));
        Assert.Equal(HttpStatusCode.Conflict, e.Status);
    }

    [Fact]
    public async Task AWrongPasswordAndAnUnknownName_GiveTheSameAnswer()
    {
        await using var api = new ApiFactory();
        var server = new GameServer(api.CreateClient());
        await server.SignUp("ada_lovelace", "correct horse battery", Cancel);
        var wrong = await Assert.ThrowsAsync<ServerException>(() => server.SignIn("ada_lovelace", "wrong password!!", Cancel));
        var unknown = await Assert.ThrowsAsync<ServerException>(() => server.SignIn("nobody_here", "correct horse battery", Cancel));
        Assert.Equal((HttpStatusCode.Unauthorized, wrong.Message), (unknown.Status, unknown.Message));
    }

    [Fact]
    public async Task WithoutAValidToken_TheCharactersAreClosed()
    {
        await using var api = new ApiFactory();
        HttpClient anonymous = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(new Uri("/api/characters", UriKind.Relative), Cancel)).StatusCode);
        GameServer server = await api.SignedIn();
        api.Clock.Advance(TimeSpan.FromHours(13));
        var e = await Assert.ThrowsAsync<ServerException>(() => server.Characters(Cancel));
        Assert.Equal(HttpStatusCode.Unauthorized, e.Status);
    }

    [Fact]
    public async Task SigningIn_IsLimitedPerAddress()
    {
        await using var api = new ApiFactory { AccountsPerMinute = 3 };
        HttpClient http = api.CreateClient();
        var statuses = new List<HttpStatusCode>();
        for (int i = 0; i < 5; i++)
            statuses.Add((await http.PostAsJsonAsync("/api/tokens", new Credentials("nobody_here", "correct horse battery"), Cancel)).StatusCode);
        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests], statuses);
    }

    [Fact]
    public async Task TheApiDescribesItself()
    {
        await using var api = new ApiFactory();
        HttpClient http = api.CreateClient();
        string openApi = await http.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative), Cancel);
        foreach (string path in new[] { "/api/accounts", "/api/tokens", "/api/characters", "/api/characters/{id}" })
            Assert.Contains($"\"{path}\"", openApi, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync(new Uri("/health", UriKind.Relative), Cancel)).StatusCode);
    }
}
