namespace TransportPipes.Database.Entities;

public class DbCompany
{
    /// <summary>Assigned by the game world</summary>
    public long Id { get; set; }

    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public double Gold { get; set; }
    public int Level { get; set; }
    public double LifetimeEarnings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsNpc { get; set; }
    public int DistrictX { get; set; }
    public int DistrictY { get; set; }
    public int Slot { get; set; }

    /// <summary>Prices, trade settings and built types (jsonb)</summary>
    public string Settings { get; set; } = "{}";

    /// <summary>The report of what happened while the player was away (jsonb)</summary>
    public string Report { get; set; } = "{}";
}
