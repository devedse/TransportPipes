using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TransportPipes.Database;
using TransportPipes.Database.Entities;

namespace TransportPipes.Server.Services;

/// <summary>
/// Creates / upgrades the database on startup and optionally adds test users
/// </summary>
public class DatabaseInitializer
{
    private readonly TransportPipesDbFactory _transportPipesDbFactory;
    private readonly IPasswordHasher<DbUser> _passwordHasher;
    private readonly TransportPipesOptions _options;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(TransportPipesDbFactory transportPipesDbFactory, IPasswordHasher<DbUser> passwordHasher, IOptions<TransportPipesOptions> options, ILogger<DatabaseInitializer> logger)
    {
        _transportPipesDbFactory = transportPipesDbFactory;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        //The database container might still be starting, so retry for a while
        var maxAttempts = 30;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                using var transportDatabase = _transportPipesDbFactory.CreateDatabaseWithOpenConnection();
                transportDatabase.MigrateDatabase();

                if (_options.SeedTestUsers && !transportDatabase.DbContext.Users.Any())
                {
                    AddTestUsers(transportDatabase);
                }

                _logger.LogInformation("Database is up to date");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && ex is System.Net.Sockets.SocketException or Npgsql.NpgsqlException or TimeoutException)
            {
                _logger.LogWarning("Could not connect to the database (attempt {Attempt}/{MaxAttempts}): {Message}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    private void AddTestUsers(TransportDatabase transportDatabase)
    {
        foreach (var (userName, email) in new[] { ("admin", "admin@test.com"), ("Devedse", "devedse@test.com") })
        {
            var user = new DbUser { UserName = userName, Email = email, Gold = _options.StartingGold };
            user.PasswordHash = _passwordHasher.HashPassword(user, _options.TestUserPassword);
            transportDatabase.AddUser(user);
        }

        _logger.LogInformation("Added the test users admin and Devedse");
    }
}
