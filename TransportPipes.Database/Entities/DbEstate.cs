namespace TransportPipes.Database.Entities;

public class DbEstate
{
    public int DistrictX { get; set; }
    public int DistrictY { get; set; }
    public int Slot { get; set; }
    public long? OwnerId { get; set; }
    public int UnlockedParcels { get; set; }

    /// <summary>The outskirts parcels bought (1.4 §5): bit n for parcel n of the nine</summary>
    public int OutskirtsParcels { get; set; }
}
