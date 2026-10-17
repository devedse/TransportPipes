using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using TransportPipes.Database;

namespace TransportPipes.TestsCommon.Database;

/// <summary>
/// Starts one PostgreSQL container (Testcontainers, so Docker is required) per test run and creates a fresh database
/// for every test from a template database that already contains all migrations.
/// </summary>
public static class PostgresTestDatabase
{
    public const string Image = "postgres:18-alpine";
    private const string TemplateDatabaseName = "transportpipes_template";

    private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private static PostgreSqlContainer? _container;
    private static int _databaseCounter;

    /// <summary>
    /// Creates a new, empty (but migrated) database
    /// </summary>
    /// <returns>The connection string of the new database</returns>
    public static string CreateDatabase()
    {
        _lock.Wait();
        try
        {
            var container = EnsureStarted();
            var databaseName = $"transportpipes_test_{Interlocked.Increment(ref _databaseCounter)}";

            using (var connection = new NpgsqlConnection(GetConnectionString(container, "postgres")))
            {
                connection.Open();
                using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\" TEMPLATE \"{TemplateDatabaseName}\"", connection);
                command.ExecuteNonQuery();
            }

            return GetConnectionString(container, databaseName);
        }
        finally
        {
            _lock.Release();
        }
    }

    public static void DropDatabase(string connectionString)
    {
        var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (_container == null || databaseName == null)
        {
            return;
        }

        using var connection = new NpgsqlConnection(GetConnectionString(_container, "postgres"));
        connection.Open();
        using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", connection);
        command.ExecuteNonQuery();
    }

    public static async Task StopAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }

    private static PostgreSqlContainer EnsureStarted()
    {
        if (_container != null)
        {
            return _container;
        }

        var container = new PostgreSqlBuilder(Image)
            .Build();
        container.StartAsync().GetAwaiter().GetResult();

        //Create the template database once, every test database is a copy of it
        using (var dbContext = new TransportPipesDbContext(TransportPipesDbFactory.CreateOptions(GetConnectionString(container, TemplateDatabaseName))))
        {
            dbContext.Database.Migrate();
        }

        _container = container;
        return container;
    }

    private static string GetConnectionString(PostgreSqlContainer container, string databaseName)
    {
        //No pooling: every test has its own database, pooled connections would keep them (and the template) open
        return new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = databaseName,
            Pooling = false
        }.ConnectionString;
    }
}
