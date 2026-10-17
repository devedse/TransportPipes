using Microsoft.AspNetCore.SignalR;
using TransportPipes.Common.Enums;
using TransportPipes.Common.Helpers;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.ActionDetails;
using TransportPipes.Server.Dtos;
using TransportPipes.Server.Services;

namespace TransportPipes.Server.Hubs;

public class TransportHub : Hub<ITransportClient>
{
    public const string Path = "/hubs/transport";

    private readonly GameFrontEndService _game;
    private readonly ConnectedUsers _connectedUsers;

    public TransportHub(GameFrontEndService game, ConnectedUsers connectedUsers)
    {
        _game = game;
        _connectedUsers = connectedUsers;
    }

    public LoginResult Login(string userName, string password)
    {
        var foundUser = _game.Login(userName, password);
        if (foundUser == null)
        {
            return LoginResult.Failed("Error: invalid username or password...");
        }

        _connectedUsers.SetUser(Context.ConnectionId, foundUser);
        return LoginResult.Succeeded(foundUser);
    }

    public LoginResult Register(string userName, string password, string email)
    {
        var (createdUser, error) = _game.Register(userName, password, email);
        if (createdUser == null)
        {
            return LoginResult.Failed(error ?? "Error: could not register...");
        }

        _connectedUsers.SetUser(Context.ConnectionId, createdUser);
        return LoginResult.Succeeded(createdUser);
    }

    public IReadOnlyList<GameObjectInfo> GetGameObjectInfos()
    {
        return _game.GetGameObjectInfos();
    }

    public ChunkData RequestChunk(int chunkX, int chunkY)
    {
        return _game.GetChunkData(chunkX, chunkY);
    }

    public async Task BuildGameObject(int x, int y, int gameObjectType)
    {
        var currentUser = await GetLoggedInUser();
        if (currentUser == null)
        {
            return;
        }

        var buildingType = (GameObjectType)gameObjectType;
        if (buildingType == GameObjectType.None || !Enum.IsDefined(buildingType))
        {
            await Clients.Caller.Notify(Notification.Error($"Unknown building type {gameObjectType}."));
            return;
        }

        var errorMessage = _game.SetDbGameObject(buildingType, currentUser, x, y);
        if (errorMessage != null)
        {
            await Clients.Caller.Notify(Notification.Warning(errorMessage));
        }

        await Clients.Caller.GoldUpdated(_game.GetGoldForUser(currentUser));

        //Send the chunk again to everyone because there is a new under construction action
        var chunkToRequest = GenericMathHelper.GetChunkForPos(x, y);
        await Clients.All.ChunkUpdated(_game.GetChunkData(chunkToRequest.X, chunkToRequest.Y));
    }

    public async Task DeleteGameObject(int x, int y)
    {
        var currentUser = await GetLoggedInUser();
        if (currentUser == null)
        {
            return;
        }

        var result = _game.AddDbAction(new DbAction()
        {
            TransportAction = TransportAction.DeleteBuilding,
            DateToBeExecuted = DateTime.UtcNow,
            ExecutedById = currentUser.Id,
            X = x,
            Y = y,
            ActionDetails = new ActionDetailsDeleteBuilding()
        });

        if (!result)
        {
            await Clients.Caller.Notify(Notification.Warning("Could not enqueue Action because there is already an action queued for this tile."));
        }
    }

    public string? GetStatusOfGameObject(int x, int y)
    {
        var status = _game.GetStatusOfGameObject(x, y);
        return string.IsNullOrWhiteSpace(status) ? null : status;
    }

    public async Task CalculateMyIncome()
    {
        var currentUser = await GetLoggedInUser();
        if (currentUser != null)
        {
            await Clients.Caller.GoldUpdated(_game.GetGoldForUser(currentUser));
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _connectedUsers.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    private async Task<DbUser?> GetLoggedInUser()
    {
        var user = _connectedUsers.GetUser(Context.ConnectionId);
        if (user == null)
        {
            await Clients.Caller.Notify(Notification.Error("Please log in first."));
        }
        return user;
    }
}
