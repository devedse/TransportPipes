namespace TransportPipes.Database.Entities;

/// <summary>A pipe or gate tile</summary>
public class DbConductor
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Kind { get; set; }
    public int Tier { get; set; }
    public long OwnerId { get; set; }
    public int? Side { get; set; }
}
