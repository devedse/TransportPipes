using System.Collections.Concurrent;
using TransportPipes.Database.Entities;

namespace TransportPipes.Server.Services;

/// <summary>
/// Keeps track of which user is logged in on which SignalR connection
/// </summary>
public class ConnectedUsers
{
    private readonly ConcurrentDictionary<string, DbUser> _usersByConnectionId = new();

    public void SetUser(string connectionId, DbUser user)
    {
        _usersByConnectionId[connectionId] = user;
    }

    public DbUser? GetUser(string connectionId)
    {
        return _usersByConnectionId.TryGetValue(connectionId, out var user) ? user : null;
    }

    public void Remove(string connectionId)
    {
        _usersByConnectionId.TryRemove(connectionId, out _);
    }

    public IReadOnlyList<(string ConnectionId, DbUser User)> GetConnectionsOfUser(long userId)
    {
        return _usersByConnectionId
            .Where(t => t.Value.Id == userId)
            .Select(t => (t.Key, t.Value))
            .ToList();
    }
}
