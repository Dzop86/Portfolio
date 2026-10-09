using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rpg.Api.Data;

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without a database or a configured application.</summary>
internal sealed class DesignTimeFactory : IDesignTimeDbContextFactory<GameDb>
{
    public GameDb CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<GameDb>().UseNpgsql("Host=localhost;Database=rpg").Options);
}
