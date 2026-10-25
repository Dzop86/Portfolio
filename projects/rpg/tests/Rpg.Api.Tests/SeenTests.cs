using System.Net;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Api.Tests;

/// <summary>
/// What an account sees once (sprint 149, D82, M6 a), on PostgreSQL: the tree's tutorial marked seen stays seen at the next
/// sign-in, on the account, not on another; an unknown flag is refused.
/// </summary>
public class SeenTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TheTreesTutorial_SeenOnce_StaysSeen_OnThatAccountOnly()
    {
        await using var api = new ApiFactory();
        GameServer server = await api.SignedIn("seen");
        Assert.Empty(await server.SeenFlags(Cancel));
        await server.MarkSeen(Seen.TreeTutorial, Cancel);
        await server.MarkSeen(Seen.TreeTutorial, Cancel);
        Assert.Equal([Seen.TreeTutorial], await server.SeenFlags(Cancel));
        Assert.Equal(Seen.TreeTutorial, api.WithDb(db => db.Accounts.Single(a => a.Name == api.Accounts[0]).Seen));
        await server.SignIn(api.Accounts[0], "correct horse battery", Cancel);
        Assert.Equal([Seen.TreeTutorial], await server.SeenFlags(Cancel));
        // Another account has seen nothing; a flag the server does not know is refused.
        GameServer other = await api.SignedIn("other");
        Assert.Empty(await other.SeenFlags(Cancel));
        ServerException refused = await Assert.ThrowsAsync<ServerException>(() => other.MarkSeen("everything", Cancel));
        Assert.Equal(HttpStatusCode.BadRequest, refused.Status);
    }
}
