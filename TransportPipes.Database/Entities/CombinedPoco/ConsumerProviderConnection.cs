namespace TransportPipes.Database.Entities.CombinedPoco;

/// <summary>
/// A provider -> consumer pair within one connection (a join of DbProviderConnection and DbConsumerConnection)
/// </summary>
public class ConsumerProviderConnection
{
    public int ConnectionId { get; set; }
    public long SourceId { get; set; }
    public long DestinationId { get; set; }
    public int GlobalConnectionId { get; set; }

    public override string ToString()
    {
        return $"{ConnectionId}: ({SourceId} -> {DestinationId}) {{{GlobalConnectionId}}}";
    }
}
