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
    /// ASP.NET Core Identity password hash (PBKDF2). The client sends the password as typed over the (TLS) connection.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public double Gold { get; set; }

    public static string NormalizeUserName(string userName) => userName.Trim().ToLowerInvariant();

    public override string ToString()
    {
        return string.Format("{0}:{1}", Id, UserName);
    }

    /// <summary>
    /// Required for Dictionary: users are equal when they have the same Id
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is DbUser other && other.Id == Id;
    }

    /// <summary>
    /// Required for Dictionary
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
