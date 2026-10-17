namespace TransportPipes.Database.Entities;

public class DbConsumerConnection
{
    public long Id { get; set; }

    public int ConnectionId { get; set; }

    public int GlobalConnectionId { get; set; }

    public DbGameObject? Destination { get; set; }
    public long DestinationId { get; set; }
}
