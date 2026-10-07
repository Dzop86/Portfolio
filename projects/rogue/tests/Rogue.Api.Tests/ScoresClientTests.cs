using System.Net;
using Rogue.Client;
using Rogue.Core;

namespace Rogue.Api.Tests;

/// <summary>The game clients' HTTP client, against the real API: the two sides cannot drift apart unnoticed.</summary>
public sealed class ScoresClientTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private static string UniqueName() => $"c{Guid.NewGuid():N}"[..20];

    [Fact]
    public async Task ARankedRun_FromSignUpToLeaderboard()
    {
        var client = new ScoresClient(api.CreateClient());
        string name = UniqueName();
        await client.SignUpAsync(name, "client password", Cancel);
        Assert.False(client.SignedIn);
        await client.SignInAsync(name, "client password", Cancel);
        Assert.True(client.SignedIn);

        Client.RunTicket ticket = await client.StartRunAsync(Cancel);
        var game = new Game(ticket.SeedValue);
        while (game.State == GameState.Playing)
            game.Apply(Autopilot.Choose(game));
        RunVerdict verdict = await client.SubmitAsync(ticket, RunRecord.From(game), Cancel);

        Assert.Equal(new RunVerdict(ticket.Id, game.State, game.Score, game.Depth, game.Turn, game.Kills), verdict);
        IReadOnlyList<ScoreLine> board = await client.LeaderboardAsync(100, Cancel);
        Assert.Contains(board, line => line.Player == name && line.Score == game.Score && line.Outcome == game.State);
    }

    [Fact]
    public async Task Refusals_CarryTheirReason()
    {
        var client = new ScoresClient(api.CreateClient());
        string name = UniqueName();
        await client.SignUpAsync(name, "client password", Cancel);

        ScoresException taken = await Assert.ThrowsAsync<ScoresException>(() => client.SignUpAsync(name.ToUpperInvariant(), "client password", Cancel));
        Assert.Equal((HttpStatusCode.Conflict, ScoresClient.NameTaken), (taken.Status, taken.Error));

        ScoresException wrong = await Assert.ThrowsAsync<ScoresException>(() => client.SignInAsync(name, "wrong password", Cancel));
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.Status);

        ScoresException anonymous = await Assert.ThrowsAsync<ScoresException>(() => client.StartRunAsync(Cancel));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.Status);

        await client.SignInAsync(name, "client password", Cancel);
        Client.RunTicket ticket = await client.StartRunAsync(Cancel);
        ScoresException refused = await Assert.ThrowsAsync<ScoresException>(() => client.SubmitAsync(ticket, new RunRecord(ticket.SeedValue, ".>"), Cancel));
        Assert.Equal((HttpStatusCode.UnprocessableEntity, "IllegalAction", 1), (refused.Status, refused.Error, refused.ActionIndex));

        var texts = new Texts(Language.French);
        Assert.Equal("Le serveur refuse la partie (IllegalAction, action n° 2).", texts.Problem(refused));
        Assert.Equal("Ce nom est déjà pris.", texts.Problem(taken));
    }
}
