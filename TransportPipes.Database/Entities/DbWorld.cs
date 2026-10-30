namespace TransportPipes.Database.Entities;

/// <summary>The world itself (one row): its seed and the moment it was last simulated</summary>
public class DbWorld
{
    public const int SingleId = 1;

    public int Id { get; set; } = SingleId;
    public int Seed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastTick { get; set; }

    /// <summary>The version of the game's rules the world was made with: an older world is thrown away (accounts stay)</summary>
    public int Rules { get; set; }

    /// <summary>The Exchange: open orders and shipments on their way (jsonb)</summary>
    public string Exchange { get; set; } = "{}";

    /// <summary>Planned and running events, and the days they were planned for (jsonb)</summary>
    public string Events { get; set; } = "{}";

    /// <summary>The towns: their Eras and Era Wonders, and how many towns finished each Wonder (jsonb)</summary>
    public string Wonders { get; set; } = "{}";

    /// <summary>Outposts in the wilderness and the deposits Discoveries left (jsonb)</summary>
    public string Outposts { get; set; } = "{}";

    /// <summary>Crews, their held seats and invite links (jsonb)</summary>
    public string Crews { get; set; } = "[]";

    /// <summary>Links between companies' Yard gates, with their tanks (jsonb)</summary>
    public string Links { get; set; } = "[]";

    /// <summary>Saved and shared layouts (jsonb)</summary>
    public string Blueprints { get; set; } = "[]";
}
