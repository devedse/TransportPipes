namespace TransportPipes.Database.Entities;

public class DbUser
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Lower-cased <see cref="UserName"/>, unique, so user names are case-insensitive like they always were.
    /// </summary>
    public string NormalizedUserName { get; set; } = string.Empty;

    /// <summary>
    /// ASP.NET Core Identity password hash (PBKDF2).
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool IsAdmin { get; set; }

    /// <summary>Legacy points from earlier seasons (GDD §8.8) and the titles they brought (jsonb)</summary>
    public int Legacy { get; set; }

    public string LegacyTitles { get; set; } = "[]";

    public static string NormalizeUserName(string userName) => userName.Trim().ToLowerInvariant();

    public override string ToString() => $"{Id}:{UserName}";
}
