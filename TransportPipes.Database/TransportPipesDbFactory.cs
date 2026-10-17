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
            // The game logic was written for a micro ORM: every query returns fresh objects and all updates are
            // executed directly in the database, so the change tracker is not used for queries.
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

    public TransportDatabase CreateDatabaseWithOpenConnection()
    {
        var dbContext = CreateDbContext();
        dbContext.Database.OpenConnection();
        return new TransportDatabase(dbContext);
    }
}
