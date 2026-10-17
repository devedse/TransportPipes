namespace TransportPipes.Database.Entities;

public class DbProviderConnection
{
    public long Id { get; set; }

    public int ConnectionId { get; set; }

    public int GlobalConnectionId { get; set; }

    public DbGameObject? Source { get; set; }
    public long SourceId { get; set; }
}
