using System.Net;
using System.Net.Http.Headers;

namespace Rpg.Api.Tests;

/// <summary>The game's files served to the launcher: whole, from a byte on, and nothing outside their folder.</summary>
public sealed class UpdateTests : IDisposable
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private readonly string _root = Directory.CreateTempSubdirectory("rpg-updates").FullName;

    public UpdateTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "linux"));
        File.WriteAllText(Path.Combine(_root, "linux", "manifest.json"), """{"version":"1.0.0","files":[]}""");
        File.WriteAllBytes(Path.Combine(_root, "linux", "rpg.pck"), [.. Enumerable.Range(0, 1000).Select(i => (byte)i)]);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public async Task TheLauncher_GetsTheManifest_AndResumesAFileWithARange()
    {
        await using var api = new ApiFactory { UpdatesRoot = _root };
        HttpClient http = api.CreateClient();
        Assert.Contains("1.0.0", await http.GetStringAsync(new Uri("/updates/linux/manifest.json", UriKind.Relative), Cancel), StringComparison.Ordinal);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/updates/linux/rpg.pck", UriKind.Relative));
        request.Headers.Range = new RangeHeaderValue(600, null);
        using HttpResponseMessage response = await http.SendAsync(request, Cancel);
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        byte[] rest = await response.Content.ReadAsByteArrayAsync(Cancel);
        Assert.Equal(400, rest.Length);
        Assert.Equal(600 % 256, rest[0]);
    }

    [Fact]
    public async Task NothingOutsideTheFolder_AndNothingWithoutOne()
    {
        await using var api = new ApiFactory { UpdatesRoot = _root };
        HttpClient http = api.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync(new Uri("/updates/../appsettings.json", UriKind.Relative), Cancel)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync(new Uri("/updates/linux/%2E%2E/%2E%2E/etc/passwd", UriKind.Relative), Cancel)).StatusCode);
        await using var none = new ApiFactory();
        Assert.Equal(HttpStatusCode.NotFound, (await none.CreateClient().GetAsync(new Uri("/updates/linux/manifest.json", UriKind.Relative), Cancel)).StatusCode);
    }
}
