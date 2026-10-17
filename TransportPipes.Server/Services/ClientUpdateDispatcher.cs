using Microsoft.AspNetCore.SignalR;
using TransportPipes.Common;
using TransportPipes.Common.Helpers;
using TransportPipes.Database.Entities.ClientUpdateDetails;
using TransportPipes.Server.Hubs;

namespace TransportPipes.Server.Services;

/// <summary>
/// The Actionizer stores the updates clients need (changed tiles and incomes) in the database, this sends them to the clients.
/// (This used to be triggered by a SqlDependency on the DbClientUpdate table, now the Actionizer runs in this process.)
/// </summary>
public class ClientUpdateDispatcher
{
    private readonly GameFrontEndService _game;
    private readonly ConnectedUsers _connectedUsers;
    private readonly IHubContext<TransportHub, ITransportClient> _hubContext;

    public ClientUpdateDispatcher(GameFrontEndService game, ConnectedUsers connectedUsers, IHubContext<TransportHub, ITransportClient> hubContext)
    {
        _game = game;
        _connectedUsers = connectedUsers;
        _hubContext = hubContext;
    }

    public async Task DispatchPendingClientUpdatesAsync()
    {
        var dbClientUpdates = _game.ObtainAndDeleteOpenClientUpdates();

        var chunksThatNeedToBeUpdated = new HashSet<Point>();
        var usersWithChangedIncome = new HashSet<long>();

        foreach (var clientUpdate in dbClientUpdates)
        {
            switch (clientUpdate.ClientUpdateDetails)
            {
                case ClientUpdateDetailsTileNeedsRefresh tileNeedsRefresh:
                    AddChunksThatShowThisTile(chunksThatNeedToBeUpdated, tileNeedsRefresh.X, tileNeedsRefresh.Y);
                    break;
                case ClientUpdateDetailsIncomeChanged incomeChanged:
                    usersWithChangedIncome.UnionWith(incomeChanged.DbUserIds);
                    break;
            }
        }

        foreach (var userId in usersWithChangedIncome)
        {
            var connections = _connectedUsers.GetConnectionsOfUser(userId);
            if (connections.Count != 0)
            {
                var gold = _game.GetGoldForUser(connections[0].User);
                await _hubContext.Clients.Clients(connections.Select(t => t.ConnectionId).ToList()).GoldUpdated(gold);
            }
        }

        foreach (var chunkPos in chunksThatNeedToBeUpdated)
        {
            await _hubContext.Clients.All.ChunkUpdated(_game.GetChunkData(chunkPos.X, chunkPos.Y));
        }
    }

    /// <summary>
    /// Pipes are drawn connected to their neighbours, so a tile at the edge of a chunk also changes the neighbouring chunk
    /// </summary>
    internal static void AddChunksThatShowThisTile(HashSet<Point> chunks, int x, int y)
    {
        var chunkXY = GenericMathHelper.GetChunkForPos(x, y);
        chunks.Add(chunkXY);

        var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(x, y);
        if (posInChunk.X == 0)
        {
            chunks.Add(new Point(chunkXY.X - 1, chunkXY.Y));
        }
        if (posInChunk.X == StaticConfigInfo.ChunkWidth - 1)
        {
            chunks.Add(new Point(chunkXY.X + 1, chunkXY.Y));
        }
        if (posInChunk.Y == 0)
        {
            chunks.Add(new Point(chunkXY.X, chunkXY.Y - 1));
        }
        if (posInChunk.Y == StaticConfigInfo.ChunkHeight - 1)
        {
            chunks.Add(new Point(chunkXY.X, chunkXY.Y + 1));
        }
    }
}
