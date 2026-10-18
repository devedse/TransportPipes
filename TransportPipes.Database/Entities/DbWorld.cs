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

    /// <summary>The hub markets of all districts (jsonb)</summary>
    public string Markets { get; set; } = "[]";

    /// <summary>The Exchange: open orders and shipments on their way (jsonb)</summary>
    public string Exchange { get; set; } = "{}";

    /// <summary>Planned and running events, and the days they were planned for (jsonb)</summary>
    public string Events { get; set; } = "{}";

    /// <summary>The Era Wonder, the District Wonders and when the Era began (jsonb)</summary>
    public string Wonders { get; set; } = "{}";

    /// <summary>Outposts in the wilderness and the deposits Discoveries left (jsonb)</summary>
    public string Outposts { get; set; } = "{}";
}
