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

    /// <summary>A corporation block: kept for a corporation's members until then</summary>
    public long? ReservedFor { get; set; }
    public DateTime? ReservedUntil { get; set; }
}
