namespace TransportPipes.Database.Entities;

public class DbCorporation
{
    /// <summary>Assigned by the game world</summary>
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int DistrictX { get; set; }
    public int DistrictY { get; set; }
    public DateTime CreatedAt { get; set; }
    public double Treasury { get; set; }
    public double Tax { get; set; }
    public int HqLevel { get; set; }
    public string? PerkResource { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Applications, invites and the weekly goal (jsonb)</summary>
    public string Data { get; set; } = "{}";
}
