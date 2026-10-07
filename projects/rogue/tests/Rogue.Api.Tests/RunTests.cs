using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Rogue.Api.Data;
using Rogue.Api.Endpoints;
using Rogue.Core;

namespace Rogue.Api.Tests;

public sealed class RunTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Runs_NeedASignedInPlayer()
    {
        HttpClient anonymous = api.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/api/runs", null, Cancel)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/runs", Cancel)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync($"/api/runs/{Guid.NewGuid()}/submission", new { }, Cancel)).StatusCode);
    }

    [Fact]
    public async Task TheServer_DrawsTheSeed_ForADay()
    {
        PlayerClient player = await api.SignedInPlayer();
        RunTicket first = await player.StartRun();
        RunTicket second = await player.StartRun();
        Assert.True(RunRecord.TryParseSeed(first.Seed, out _));
        Assert.NotEqual(first.Seed, second.Seed);
        Assert.Equal(api.Clock.GetUtcNow() + TimeSpan.FromHours(24), first.ExpiresAt);
        Assert.Equal(RunStatus.Open, api.WithDb(db => db.Runs.Single(r => r.Id == first.Id).Status));
    }

    [Fact]
    public async Task APlayedRun_IsScoredByTheServer()
    {
        PlayerClient player = await api.SignedInPlayer();
        RunTicket ticket = await player.StartRun();
        Game game = Runs.Play(ticket);

        HttpResponseMessage response = await player.Submit(ticket.Id, Runs.Json(game));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        RunResult? result = await response.Content.ReadFromJsonAsync<RunResult>(Json.Options, Cancel);
        Assert.Equal(new RunResult(ticket.Id, game.State, game.Score, game.Depth, game.Turn, game.Kills), result);
        RankedRun stored = api.WithDb(db => db.Runs.Single(r => r.Id == ticket.Id));
        Assert.Equal((RunStatus.Scored, game.Score, ActionCodec.Encode(game.History)), (stored.Status, stored.Score, stored.Actions));

        List<MyRun>? mine = await player.Http.GetFromJsonAsync<List<MyRun>>("/api/runs", Json.Options, Cancel);
        Assert.Equal(game.Score, Assert.Single(mine!).Score);
    }

    [Fact]
    public async Task ARun_IsSubmittedOnlyOnce()
    {
        PlayerClient player = await api.SignedInPlayer();
        RunTicket ticket = await player.StartRun();
        string json = Runs.Json(Runs.Play(ticket));
        Assert.Equal(HttpStatusCode.OK, (await player.Submit(ticket.Id, json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await player.Submit(ticket.Id, json)).StatusCode);
    }

    [Fact]
    public async Task SimultaneousSubmissions_RecordOneVerdict()
    {
        PlayerClient player = await api.SignedInPlayer();
        RunTicket ticket = await player.StartRun();
        string json = Runs.Json(Runs.Play(ticket));
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => player.Submit(ticket.Id, json)));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.OK), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Fact]
    public async Task SomebodyElsesRun_IsNotFound()
    {
        PlayerClient owner = await api.SignedInPlayer();
        PlayerClient other = await api.SignedInPlayer();
        RunTicket ticket = await owner.StartRun();
        Assert.Equal(HttpStatusCode.NotFound, (await other.Submit(ticket.Id, Runs.Json(Runs.Play(ticket)))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.Submit(Guid.NewGuid(), Runs.Json(Runs.Play(ticket)))).StatusCode);
    }

    [Fact]
    public async Task ExpiredRuns_AreRefused()
    {
        PlayerClient player = await api.SignedInPlayer();
        RunTicket ticket = await player.StartRun();
        string json = Runs.Json(Runs.Play(ticket));
        api.Clock.Advance(TimeSpan.FromHours(24));
        try
        {
            // The player's token (12 hours) has expired too: sign in again.
            PlayerClient sameAccount = player with { Http = await SignIn(player.Name) };
            Assert.Equal(HttpStatusCode.Gone, (await sameAccount.Submit(ticket.Id, json)).StatusCode);
        }
        finally
        {
            api.Clock.Advance(TimeSpan.FromHours(-24));
        }
    }

    [Fact]
    public async Task AnotherSeed_IsRefused_AndTheRunStaysOpen()
    {
        PlayerClient player = await api.SignedInPlayer();
        RunTicket ticket = await player.StartRun();
        ulong seed = ulong.Parse(ticket.Seed, CultureInfo.InvariantCulture);
        var elsewhere = new Game(seed + 1);
        while (elsewhere.State == GameState.Playing)
            elsewhere.Apply(Autopilot.Choose(elsewhere));

        HttpResponseMessage refused = await player.Submit(ticket.Id, Runs.Json(elsewhere));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Equal("SeedMismatch", await Error(refused));

        Assert.Equal(HttpStatusCode.OK, (await player.Submit(ticket.Id, Runs.Json(Runs.Play(ticket)))).StatusCode);
    }

    [Fact]
    public async Task UnfinishedRuns_AreRefused_AndTheRunStaysOpen()
    {
        PlayerClient player = await api.SignedInPlayer();
        RunTicket ticket = await player.StartRun();
        HttpResponseMessage refused = await player.Submit(ticket.Id, new RunSubmission("rogue-run", 1, ticket.Seed, ""));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Equal("Unfinished", await Error(refused));
        Assert.Equal(RunStatus.Open, api.WithDb(db => db.Runs.Single(r => r.Id == ticket.Id).Status));
    }

    [Fact]
    public async Task ARunTheRulesRefuse_IsRejected_WithThePlaceOfTheFault()
    {
        PlayerClient player = await api.SignedInPlayer();
        RunTicket ticket = await player.StartRun();
        // Nobody starts on the stairs.
        HttpResponseMessage refused = await player.Submit(ticket.Id, new RunSubmission("rogue-run", 1, ticket.Seed, ".>"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        var problem = await refused.Content.ReadFromJsonAsync<ProblemDetails>(Cancel);
        Assert.Equal("IllegalAction", problem!.Extensions["error"]?.ToString());
        Assert.Equal("1", problem.Extensions["actionIndex"]?.ToString());
        RankedRun stored = api.WithDb(db => db.Runs.Single(r => r.Id == ticket.Id));
        Assert.Equal((RunStatus.Rejected, "IllegalAction at action 1", null), (stored.Status, stored.Rejection, stored.Score));
        // One verdict per seed: no second try after a refused run.
        Assert.Equal(HttpStatusCode.Conflict, (await player.Submit(ticket.Id, Runs.Json(Runs.Play(ticket)))).StatusCode);
    }

    [Fact]
    public async Task MalformedSubmissions_AreRefused()
    {
        PlayerClient player = await api.SignedInPlayer();
        RunTicket ticket = await player.StartRun();
        string seed = ticket.Seed;
        Assert.Equal(HttpStatusCode.BadRequest, (await player.Submit(ticket.Id, "not json")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await player.Submit(ticket.Id, $$"""{"format":"rogue-run","version":1,"seed":"{{seed}}","actions":"","score":99999}""")).StatusCode);
        HttpResponseMessage version = await player.Submit(ticket.Id, new RunSubmission("rogue-run", 2, seed, ""));
        Assert.Equal(HttpStatusCode.BadRequest, version.StatusCode);
        Assert.Equal("UnsupportedVersion", await Error(version));
        Assert.Equal(HttpStatusCode.BadRequest, (await player.Submit(ticket.Id, new RunSubmission("rogue-run", 1, "0" + seed, ""))).StatusCode);
        Assert.Equal(RunStatus.Open, api.WithDb(db => db.Runs.Single(r => r.Id == ticket.Id).Status));
    }

    [Fact]
    public async Task OverlongRuns_AreRejectedWithoutBeingPlayed()
    {
        // Kestrel refuses bodies over 256 kB (413, checked by the Docker smoke test: the test server has no such limit).
        PlayerClient player = await api.SignedInPlayer();
        RunTicket ticket = await player.StartRun();
        HttpResponseMessage response = await player.Submit(ticket.Id, new RunSubmission("rogue-run", 1, ticket.Seed, new string('.', RunRecord.MaxActions + 1)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("TooLong", await Error(response));
    }

    private async Task<HttpClient> SignIn(string name)
    {
        HttpClient http = api.CreateClient();
        HttpResponseMessage response = await http.PostAsJsonAsync("/api/tokens", new Credentials(name, "correct horse battery"), Cancel);
        AccessToken token = (await response.Content.ReadFromJsonAsync<AccessToken>(Json.Options, Cancel))!;
        http.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);
        return http;
    }

    private static async Task<string?> Error(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>(Cancel))!.Extensions["error"]?.ToString();
}
