namespace TransportPipes.Database.Entities;

/// <summary>The world itself (one row): its seed, Era and the moment it was last simulated</summary>
public class DbWorld
{
    public const int SingleId = 1;

    public int Id { get; set; } = SingleId;
    public int Seed { get; set; }
    public int Era { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime LastTick { get; set; }
}
