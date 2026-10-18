namespace TransportPipes.Database.Entities;

public class DbBuilding
{
    /// <summary>Assigned by the game world</summary>
    public long Id { get; set; }

    public string Type { get; set; } = string.Empty;
    public long OwnerId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Size { get; set; }
    public int Level { get; set; }
    public DateTime? ConstructionEndsAt { get; set; }

    /// <summary>Storage contents per resource (jsonb)</summary>
    public string Contents { get; set; } = "{}";

    /// <summary>Trading posts: the resources imported from the hub (jsonb)</summary>
    public string Imports { get; set; } = "[]";

    /// <summary>A number the owner sets (the toll of a Toll Meter)</summary>
    public double Setting { get; set; }

    /// <summary>Valves: the resources that pass (jsonb)</summary>
    public string Filter { get; set; } = "[]";

    /// <summary>Storm damage: half speed until repaired</summary>
    public bool Damaged { get; set; }
}
