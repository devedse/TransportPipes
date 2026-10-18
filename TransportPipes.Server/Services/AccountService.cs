using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransportPipes.Database;
using TransportPipes.Database.Entities;

namespace TransportPipes.Server.Services;

/// <summary>Registering and checking user accounts</summary>
public sealed class AccountService
{
    public const int MinPasswordLength = 6;

    private readonly TransportPipesDbFactory _dbFactory;
    private readonly IPasswordHasher<DbUser> _passwordHasher;
    private readonly TimeProvider _time;

    public AccountService(TransportPipesDbFactory dbFactory, IPasswordHasher<DbUser> passwordHasher, TimeProvider time)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
        _time = time;
    }

    public static string? UserNameError(string userName)
    {
        userName = userName.Trim();
        if (userName.Length is < 3 or > 24)
        {
            return "A user name has 3 to 24 characters.";
        }
        return userName.All(t => char.IsLetterOrDigit(t) || t is '_' or '-' or '.') ? null : "Use letters, digits and _ - . in your user name.";
    }

    public async Task<(DbUser? User, string? Error)> RegisterAsync(string userName, string password, string? email)
    {
        if (UserNameError(userName) is { } error)
        {
            return (null, error);
        }
        if (password.Length < MinPasswordLength)
        {
            return (null, $"Use a password of at least {MinPasswordLength} characters.");
        }

        await using var db = _dbFactory.CreateDbContext();
        var normalized = DbUser.NormalizeUserName(userName);
        if (await db.Users.AnyAsync(t => t.NormalizedUserName == normalized))
        {
            return (null, "That user name is already taken.");
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var user = new DbUser { UserName = userName.Trim(), NormalizedUserName = normalized, Email = email?.Trim() ?? "", CreatedAt = now, LastLoginAt = now };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (null, "That user name is already taken.");
        }
        return (user, null);
    }

    public async Task<DbUser?> LoginAsync(string userName, string password)
    {
        await using var db = _dbFactory.CreateDbContext();
        var normalized = DbUser.NormalizeUserName(userName);
        var user = await db.Users.AsTracking().SingleOrDefaultAsync(t => t.NormalizedUserName == normalized);
        if (user == null)
        {
            return null;
        }
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
        }
        user.LastLoginAt = _time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ExistsAsync(long userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        return await db.Users.AnyAsync(t => t.Id == userId);
    }

    /// <summary>The Legacy of a user (points and titles from earlier seasons)</summary>
    public async Task<(int Legacy, IReadOnlyList<string> Titles)> LegacyAsync(long userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(t => t.Id == userId);
        return user == null ? (0, []) : (user.Legacy, System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.LegacyTitles) ?? []);
    }

    /// <summary>Adds the results of a season to the players' Legacy</summary>
    public async Task AddLegacyAsync(IEnumerable<TransportPipes.Game.State.SeasonResult> results)
    {
        await using var db = _dbFactory.CreateDbContext();
        foreach (var result in results)
        {
            var user = await db.Users.AsTracking().FirstOrDefaultAsync(t => t.Id == result.UserId);
            if (user == null)
            {
                continue;
            }
            user.Legacy += result.Legacy;
            if (result.Title != null)
            {
                var titles = System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.LegacyTitles) ?? [];
                titles.Add(result.Title);
                user.LegacyTitles = System.Text.Json.JsonSerializer.Serialize(titles);
            }
        }
        await db.SaveChangesAsync();
    }
}
