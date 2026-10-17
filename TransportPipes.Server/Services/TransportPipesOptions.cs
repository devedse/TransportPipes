namespace TransportPipes.Server.Services;

public class TransportPipesOptions
{
    public const string SectionName = "TransportPipes";

    /// <summary>
    /// How often the Actionizer executes the queued actions (building, deleting, ...)
    /// </summary>
    public double ActionizerIntervalSeconds { get; set; } = 1;

    /// <summary>
    /// The gold a newly registered player starts with
    /// </summary>
    public double StartingGold { get; set; } = 500;

    /// <summary>
    /// Creates the users "admin" and "Devedse" (with <see cref="TestUserPassword"/>) when there are no users yet.
    /// Only meant for development.
    /// </summary>
    public bool SeedTestUsers { get; set; }

    public string TestUserPassword { get; set; } = "a";
}
