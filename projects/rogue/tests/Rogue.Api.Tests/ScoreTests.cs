using System.Net;
using System.Net.Http.Json;
using Rogue.Api.Endpoints;
using Rogue.Core;

namespace Rogue.Api.Tests;

public sealed class ScoreTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TheLeaderboard_ShowsEachPlayersBestRun_BestFirst()
    {
        Assert.Empty((await api.CreateClient().GetFromJsonAsync<List<ScoreEntry>>("/api/scores", Json.Options, Cancel))!);

        var best = new Dictionary<string, Game>();
        foreach (int runs in new[] { 3, 1, 2 })
        {
            PlayerClient player = await api.SignedInPlayer();
            for (int i = 0; i < runs; i++)
            {
                RunTicket ticket = await player.StartRun();
                Game game = Runs.Play(ticket);
                (await player.Submit(ticket.Id, Runs.Json(game))).EnsureSuccessStatusCode();
                if (!best.TryGetValue(player.Name, out Game? previous) || game.Score > previous.Score
                    || (game.Score == previous.Score && game.Turn < previous.Turn))
                    best[player.Name] = game;
            }
            // A refused run never reaches the leaderboard.
            RunTicket refused = await player.StartRun();
            await player.Submit(refused.Id, new RunSubmission("rogue-run", 1, refused.Seed, ".>"));
        }

        List<ScoreEntry> board = (await api.CreateClient().GetFromJsonAsync<List<ScoreEntry>>("/api/scores", Json.Options, Cancel))!;

        var expected = best.OrderByDescending(b => b.Value.Score).ThenBy(b => b.Value.Turn)
            .Select((b, i) => (i + 1, b.Key, b.Value.Score, b.Value.State, b.Value.Depth, b.Value.Turn)).ToList();
        Assert.Equal(expected, board.Select(e => (e.Rank, e.Player, e.Score, e.Outcome, e.Depth, e.Turns)).ToList());

        List<ScoreEntry> top = (await api.CreateClient().GetFromJsonAsync<List<ScoreEntry>>("/api/scores?limit=1", Json.Options, Cancel))!;
        Assert.Equal(board[0], Assert.Single(top));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task TheLimit_IsBounded(int limit)
    {
        HttpResponseMessage response = await api.CreateClient().GetAsync($"/api/scores?limit={limit}", Cancel);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class DocumentationTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TheOpenApiDocument_DescribesEveryEndpoint_AndTheBearerScheme()
    {
        string document = await api.CreateClient().GetStringAsync("/openapi/v1.json", Cancel);
        using var json = System.Text.Json.JsonDocument.Parse(document);
        var paths = json.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(["/api/accounts", "/api/runs", "/api/runs/{id}/submission", "/api/scores", "/api/tokens"], paths);
        Assert.Equal("bearer", json.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        Assert.True(json.RootElement.GetProperty("paths").GetProperty("/api/runs").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(json.RootElement.GetProperty("paths").GetProperty("/api/scores").GetProperty("get").TryGetProperty("security", out _));
    }

    [Fact]
    public async Task SwaggerUi_AndHealth_Answer()
    {
        HttpClient http = api.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/swagger/index.html", Cancel)).StatusCode);
        Assert.Equal("""{"status":"ok"}""", await http.GetStringAsync("/health", Cancel));
    }
}
