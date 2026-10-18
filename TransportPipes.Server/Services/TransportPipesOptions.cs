namespace TransportPipes.Server.Services;

public class TransportPipesOptions
{
    public const string SectionName = "TransportPipes";

    /// <summary>
    /// How often the world is simulated and clients get updates
    /// </summary>
    public double TickSeconds { get; set; } = 2;

    /// <summary>
    /// How often gold, storage contents and XP are saved (commands are saved within a tick)
    /// </summary>
    public double SaveSeconds { get; set; } = 30;

    /// <summary>
    /// The seed of a new world (only used when the database has no world yet)
    /// </summary>
    public int WorldSeed { get; set; } = 20261018;

    /// <summary>
    /// The gold a new company starts with; empty means the value from the content files (progression.yaml)
    /// </summary>
    public double? StartingGold { get; set; }

    /// <summary>
    /// Creates the users "admin" and "Devedse" (with <see cref="TestUserPassword"/>) when there are no users yet.
    /// Only meant for development.
    /// </summary>
    public bool SeedTestUsers { get; set; }

    public string TestUserPassword { get; set; } = "a";
}
