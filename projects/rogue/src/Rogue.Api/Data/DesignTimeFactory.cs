using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rogue.Api.Data;

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without a database or a configured application.</summary>
internal sealed class DesignTimeFactory : IDesignTimeDbContextFactory<ScoresDb>
{
    public ScoresDb CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ScoresDb>().UseNpgsql("Host=localhost;Database=rogue").Options);
}
