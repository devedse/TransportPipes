using System.Collections.Concurrent;

namespace TransportPipes.Server.Services;

/// <summary>Which users are online (a user can have several tabs open)</summary>
public sealed class ConnectedUsers
{
    private readonly ConcurrentDictionary<long, int> _connections = new();

    public IEnumerable<long> OnlineUsers => _connections.Keys;

    public bool IsOnline(long userId) => _connections.ContainsKey(userId);

    public void Connected(long userId) => _connections.AddOrUpdate(userId, 1, (_, count) => count + 1);

    /// <summary>Returns true when this was the user's last connection</summary>
    public bool Disconnected(long userId)
    {
        while (_connections.TryGetValue(userId, out var count))
        {
            if (count <= 1)
            {
                if (_connections.TryRemove(new KeyValuePair<long, int>(userId, count)))
                {
                    return true;
                }
            }
            else if (_connections.TryUpdate(userId, count - 1, count))
            {
                return false;
            }
        }
        return true;
    }
}
