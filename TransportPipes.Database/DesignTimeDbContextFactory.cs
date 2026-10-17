using Microsoft.EntityFrameworkCore.Design;

namespace TransportPipes.Database;

/// <summary>
/// Used by the dotnet-ef tool: dotnet ef migrations add &lt;Name&gt; --project TransportPipes.Database
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TransportPipesDbContext>
{
    public TransportPipesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TransportPipes")
            ?? "Host=localhost;Database=transportpipes;Username=transportpipes;Password=transportpipes";
        return new TransportPipesDbContext(TransportPipesDbFactory.CreateOptions(connectionString));
    }
}
