using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Rpg.Client.Tests;

/// <summary>The lobby against a stand-in server (the real one is in Rpg.Api.Tests): refusals, server down.</summary>
public partial class LobbyTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    /// <summary>Answers every request with <paramref name="answer"/>, and counts them.</summary>
    private sealed class StandIn(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add($"{request.Method} {request.RequestUri!.AbsolutePath}");
            return Task.FromResult(answer(request));
        }
    }

    private static (Lobby, StandIn) Make(Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        var handler = new StandIn(answer);
        return (new Lobby(new GameServer(new HttpClient(handler) { BaseAddress = new Uri("http://server.test/") })), handler);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage SignedInWithCharacters(HttpRequestMessage request, string characters = "[]") =>
        request.RequestUri!.AbsolutePath == "/api/tokens"
            ? Json(HttpStatusCode.OK, """{"token":"t","expiresAt":"2026-10-10T00:00:00Z"}""")
            : Json(HttpStatusCode.OK, characters);

    [Fact]
    public async Task AServerThatDoesNotAnswer_LeavesThePlayerOffline()
    {
        (Lobby lobby, _) = Make(_ => throw new HttpRequestException("connection refused"));
        Assert.False(await lobby.SignIn("ada", "correct horse battery", create: false, Cancel));
        Assert.Equal("lobby.unreachable", lobby.Problem);
        Assert.False(lobby.SignedIn);
    }

    [Theory]
    [InlineData("", "correct horse battery", false, "lobby.fill-in")]
    [InlineData("ada", "", false, "lobby.fill-in")]
    [InlineData("ada", "short", true, "lobby.password-short")]
    public async Task WhatTheGameCanCheck_IsRefusedWithoutAskingTheServer(string name, string password, bool create, string problem)
    {
        (Lobby lobby, StandIn server) = Make(r => throw new InvalidOperationException("no call expected"));
        Assert.False(await lobby.SignIn(name, password, create, Cancel));
        Assert.Equal(problem, lobby.Problem);
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task SigningIn_LoadsTheCharacters_AndASuccessClearsTheProblem()
    {
        (Lobby lobby, StandIn server) = Make(r => SignedInWithCharacters(r,
            """[{"id":"6f9619ff-8b86-d011-b42d-00c04fc964ff","name":"Élise","look":"female-c","createdAt":"2026-10-09T12:00:00Z"}]"""));
        Assert.False(await lobby.SignIn("", "x", create: false, Cancel));
        Assert.True(await lobby.SignIn("ada", "correct horse battery", create: false, Cancel));
        Assert.Null(lobby.Problem);
        Assert.Equal("Élise", Assert.Single(lobby.Characters).Name);
        Assert.Equal(["POST /api/tokens", "GET /api/characters"], server.Requests);
    }

    [Fact]
    public async Task TooManyTries_AreToldApart()
    {
        (Lobby lobby, _) = Make(_ => Json(HttpStatusCode.TooManyRequests, "{}"));
        Assert.False(await lobby.SignIn("ada", "correct horse battery", create: false, Cancel));
        Assert.Equal("lobby.too-many-tries", lobby.Problem);
    }

    [Fact]
    public async Task AnExpiredSession_SendsThePlayerBackToSignIn()
    {
        bool expired = false;
        (Lobby lobby, _) = Make(r => expired ? Json(HttpStatusCode.Unauthorized, "{}") : SignedInWithCharacters(r));
        Assert.True(await lobby.SignIn("ada", "correct horse battery", create: false, Cancel));
        expired = true;
        Assert.False(await lobby.Create("Élise", "female-c", Cancel));
        Assert.Equal("lobby.session-expired", lobby.Problem);
        Assert.False(lobby.SignedIn);
    }

    [Fact]
    public async Task AnInvalidCharacterName_IsRefusedAtOnce()
    {
        (Lobby lobby, StandIn server) = Make(r => SignedInWithCharacters(r));
        await lobby.SignIn("ada", "correct horse battery", create: false, Cancel);
        Assert.False(await lobby.Create("R2D2", "female-c", Cancel));
        Assert.Equal("lobby.name-invalid", lobby.Problem);
        Assert.Equal(2, server.Requests.Count);
    }

    [Fact]
    public async Task AServerError_IsNotMistakenForARefusal()
    {
        (Lobby lobby, _) = Make(_ => Json(HttpStatusCode.InternalServerError, """{"title":"boom"}"""));
        Assert.False(await lobby.SignIn("ada", "correct horse battery", create: false, Cancel));
        Assert.Equal("lobby.server-error", lobby.Problem);
    }

    [GeneratedRegex("\"(lobby\\.[a-z-]+)\"")]
    private static partial Regex LobbyKey();

    private static string SourceOf(string file, [CallerFilePath] string here = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "../../src/Rpg.Client", file));

    [Fact]
    public void EveryProblemTheLobbyCanGive_HasATextInBothLanguages()
    {
        string[] keys = [.. LobbyKey().Matches(SourceOf("Lobby.cs")).Select(m => m.Groups[1].Value).Distinct()];
        Assert.True(keys.Length >= 12, string.Join(", ", keys));
        foreach (string key in keys)
        {
            Assert.True(Texts.Fr.ContainsKey(key), key);
            Assert.True(Texts.En.ContainsKey(key), key);
        }
    }
}
