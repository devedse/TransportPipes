using Microsoft.Extensions.Diagnostics.HealthChecks;
using TransportPipes.Database;

namespace TransportPipes.Server.Services;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly TransportPipesDbFactory _transportPipesDbFactory;

    public DatabaseHealthCheck(TransportPipesDbFactory transportPipesDbFactory)
    {
        _transportPipesDbFactory = transportPipesDbFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _transportPipesDbFactory.CreateDbContext();
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Can't connect to the database");
    }
}
