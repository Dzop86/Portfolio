using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Rogue.Api.Endpoints;

namespace Rogue.Api.Tests;

public sealed class AccountTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task SignUpThenSignIn_GivesATokenThatOpensTheRuns()
    {
        PlayerClient player = await api.SignedInPlayer();
        HttpResponseMessage response = await player.Http.GetAsync("/api/runs", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SignUp_AnswersCreated()
    {
        HttpClient http = api.CreateClient();
        HttpResponseMessage response = await http.PostAsJsonAsync("/api/accounts", new Credentials("Ada_Lovelace", "analytical engine"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/players/Ada_Lovelace", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Names_AreUnique_WhateverTheCase()
    {
        HttpClient http = api.CreateClient();
        CancellationToken cancel = TestContext.Current.CancellationToken;
        (await http.PostAsJsonAsync("/api/accounts", new Credentials("Grace-Hopper", "first compiler"), cancel)).EnsureSuccessStatusCode();
        HttpResponseMessage again = await http.PostAsJsonAsync("/api/accounts", new Credentials("grace-hopper", "another password"), cancel);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Theory]
    [InlineData(null, "long enough password", "name")]
    [InlineData("ab", "long enough password", "name")]
    [InlineData("a name with spaces", "long enough password", "name")]
    [InlineData("éric", "long enough password", "name")]
    [InlineData("abcdefghijklmnopqrstu", "long enough password", "name")]
    [InlineData("valid_name", "short", "password")]
    [InlineData("valid_name", null, "password")]
    public async Task BadNamesAndPasswords_AreRefused(string? name, string? password, string field)
    {
        HttpResponseMessage response = await api.CreateClient().PostAsJsonAsync("/api/accounts", new Credentials(name, password), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Contains(field, problem!.Errors.Keys);
    }

    [Fact]
    public async Task Passwords_AreStoredHashed()
    {
        await api.CreateClient().PostAsJsonAsync("/api/accounts", new Credentials("Hashed", "my secret password"), TestContext.Current.CancellationToken);
        string hash = api.WithDb(db => db.Players.Single(p => p.Name == "Hashed").PasswordHash);
        Assert.DoesNotContain("my secret password", hash, StringComparison.Ordinal);
        // Identity's version 3 format: a marker byte, then PBKDF2 parameters, salt and subkey, in base64.
        Assert.Equal(1, Convert.FromBase64String(hash)[0]);
    }

    [Fact]
    public async Task WrongPasswordsAndUnknownNames_GetTheSameAnswer()
    {
        HttpClient http = api.CreateClient();
        CancellationToken cancel = TestContext.Current.CancellationToken;
        await http.PostAsJsonAsync("/api/accounts", new Credentials("Alan", "imitation game"), cancel);
        HttpResponseMessage wrong = await http.PostAsJsonAsync("/api/tokens", new Credentials("Alan", "imitation gamE"), cancel);
        HttpResponseMessage unknown = await http.PostAsJsonAsync("/api/tokens", new Credentials("Nobody", "imitation game"), cancel);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        ProblemDetails? a = await wrong.Content.ReadFromJsonAsync<ProblemDetails>(cancel);
        ProblemDetails? b = await unknown.Content.ReadFromJsonAsync<ProblemDetails>(cancel);
        Assert.Equal("Unknown name or wrong password.", a!.Title);
        Assert.Equal(a.Title, b!.Title);
    }

    [Fact]
    public async Task Tokens_Expire()
    {
        PlayerClient player = await api.SignedInPlayer();
        api.Clock.Advance(TimeSpan.FromHours(12) + TimeSpan.FromMinutes(1));
        try
        {
            HttpResponseMessage response = await player.Http.GetAsync("/api/runs", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            api.Clock.Advance(-(TimeSpan.FromHours(12) + TimeSpan.FromMinutes(1)));
        }
    }

    [Fact]
    public async Task ForgedTokens_AreRefused()
    {
        HttpClient http = api.CreateClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", "eyJhbGciOiJub25lIn0.eyJzdWIiOiIxIn0.");
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/runs", TestContext.Current.CancellationToken)).StatusCode);
    }
}

public sealed class RateLimitTests
{
    [Fact]
    public async Task SignUpsAndSignIns_AreLimitedPerMinute()
    {
        await using var api = new ApiFactory { AccountsPerMinute = 3 };
        HttpClient http = api.CreateClient();
        CancellationToken cancel = TestContext.Current.CancellationToken;
        var statuses = new List<HttpStatusCode>();
        for (int i = 0; i < 4; i++)
            statuses.Add((await http.PostAsJsonAsync("/api/tokens", new Credentials("nobody", "wrong password"), cancel)).StatusCode);
        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests], statuses);
        // The leaderboard is not limited.
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/scores", cancel)).StatusCode);
    }
}
