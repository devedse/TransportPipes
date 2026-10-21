namespace TransportPipes.Database.Entities;

public class DbContract
{
    /// <summary>Assigned by the game world</summary>
    public long Id { get; set; }

    public long Proposer { get; set; }
    public long Seller { get; set; }
    public long Buyer { get; set; }
    public string Resource { get; set; } = string.Empty;
    public double Rate { get; set; }
    public double Price { get; set; }
    public double Days { get; set; }
    public double PenaltyRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int State { get; set; }
    public double Delivered { get; set; }
    public double Expected { get; set; }
}
