using TransportPipes.Server.Dtos;

namespace TransportPipes.Server.Hubs;

/// <summary>
/// The methods the server can call on the clients
/// </summary>
public interface ITransportClient
{
    /// <summary>
    /// Tiles of this chunk changed (something was built, deleted or is under construction)
    /// </summary>
    Task ChunkUpdated(ChunkData chunk);

    Task GoldUpdated(GoldInfo gold);

    Task Notify(Notification notification);
}
