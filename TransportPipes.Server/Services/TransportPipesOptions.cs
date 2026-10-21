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
    /// NPC tenants on the estates nobody has taken yet (GDD §4.7)
    /// </summary>
    public bool Tenants { get; set; } = true;

    /// <summary>Weather and hub events (GDD §15)</summary>
    public bool Events { get; set; } = true;

    /// <summary>Loady's guide for new players (GDD §10.2)</summary>
    public bool Tutorial { get; set; } = true;

    /// <summary>The starter kit: surveyors add copper, coal, stone and a pond where an estate lacks them (v1.1)</summary>
    public bool Surveys { get; set; } = true;

    /// <summary>Days between the end of a chapter (its ceremony) and the next one (GDD §8.8)</summary>
    public double SeasonBreakDays { get; set; } = 1;

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

    /// <summary>
    /// Allows the Cheat hub method (gold, levels, research points, finishing constructions) for testing. Never in production.
    /// </summary>
    public bool Cheats { get; set; }

    /// <summary>How many guests (players without an account yet) one address may start a day</summary>
    public int GuestsPerAddressPerDay { get; set; } = 5;
}
