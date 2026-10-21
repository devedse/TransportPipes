namespace TransportPipes.Database.Entities;

/// <summary>A tile whose terrain changed since the world was generated (a cleared forest, terraforming)</summary>
public class DbTerrainOverride
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Terrain { get; set; }
}
