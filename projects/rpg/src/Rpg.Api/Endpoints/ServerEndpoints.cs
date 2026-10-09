using Rpg.Core;

namespace Rpg.Api.Endpoints;

/// <summary>
/// The game servers a player chooses from, from the configuration (<c>Servers:0:Id</c>,
/// <c>Servers:0:Name</c>...); one, "Osméria", when none is configured.
/// </summary>
public sealed class GameServers(IConfiguration configuration)
{
    public IReadOnlyList<ServerInfo> All { get; } =
        configuration.GetSection("Servers").GetChildren()
            .Select(s => new ServerInfo(s["Id"] ?? "", s["Name"] ?? ""))
            .Where(s => s.Id.Length is > 0 and <= 20 && s.Name.Length > 0)
            .ToArray() is { Length: > 0 } configured
            ? configured
            : [new ServerInfo(Servers.Default, "Osméria")];

    public bool Exists(string? id) => All.Any(s => s.Id == id);
}

public static class ServerEndpoints
{
    public static void MapServerEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/servers", (GameServers servers) => TypedResults.Ok(servers.All))
            .WithTags("Servers")
            .WithSummary("The game servers, to choose from before one's characters.");
}
