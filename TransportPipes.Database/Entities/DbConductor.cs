namespace TransportPipes.Database.Entities;

/// <summary>A pipe or gate tile</summary>
public class DbConductor
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Kind { get; set; }
    public int Tier { get; set; }
    public long OwnerId { get; set; }

    /// <summary>A Copper or Steel Pipe widened for gold: ×2 or ×4 (0 or 1: not widened)</summary>
    public int Widen { get; set; }
}
