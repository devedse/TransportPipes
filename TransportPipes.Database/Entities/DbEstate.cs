namespace TransportPipes.Database.Entities;

public class DbEstate
{
    public int DistrictX { get; set; }
    public int DistrictY { get; set; }
    public int Slot { get; set; }
    public long? OwnerId { get; set; }
    public int UnlockedParcels { get; set; }

    /// <summary>Gate policy per side (jsonb)</summary>
    public string GatePolicies { get; set; } = "{}";
}
