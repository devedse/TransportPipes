namespace TransportPipes.Database.Entities;

public class DbGlobalConnection
{
    public long Id { get; set; }

    public int GlobalConnectionId { get; set; }

    public DateTime? LastCalculated { get; set; }
}
