using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TransportPipes.Database;

public class TransportPipesDbFactory
{
    private readonly DbContextOptions<TransportPipesDbContext> _options;

    public string ConnectionString { get; }

    public TransportPipesDbFactory(string connectionString, ILoggerFactory? loggerFactory = null)
    {
        ConnectionString = connectionString;
        _options = CreateOptions(connectionString, loggerFactory);
    }

    public static DbContextOptions<TransportPipesDbContext> CreateOptions(string connectionString, ILoggerFactory? loggerFactory = null)
    {
        var builder = new DbContextOptionsBuilder<TransportPipesDbContext>()
            .UseNpgsql(connectionString)
            // The game world lives in memory and is saved as snapshots, queries only load it
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        if (loggerFactory != null)
        {
            builder.UseLoggerFactory(loggerFactory);
        }

        return builder.Options;
    }

    public TransportPipesDbContext CreateDbContext()
    {
        return new TransportPipesDbContext(_options);
    }
}
