using TransportPipes.Common.GameLogicObjects.GameObjectInfo;
using TransportPipes.Database.Entities;

namespace TransportPipes.Server.Dtos;

// These records are the SignalR contract with the frontend (TransportPipes.Frontend/src/api/contracts.ts).
// SignalR serializes them as camelCase JSON.

public record LoginResult(bool Success, string Message, string UserName, long UserId)
{
    public static LoginResult Succeeded(DbUser user) => new(true, string.Empty, user.UserName, user.Id);

    public static LoginResult Failed(string message) => new(false, message, string.Empty, 0);
}

public record ResourceInfo(int ResourceType, string ResourceTypeName, double Value)
{
    public static ResourceInfo From(BackendResourceInfo resourceInfo) => new((int)resourceInfo.ResourceType, resourceInfo.ResourceTypeName, resourceInfo.Value);
}

public record GameObjectInfo(int GameObjectType, string GameObjectTypeName, int Price, IReadOnlyList<ResourceInfo> ConsumptionInfo, IReadOnlyList<ResourceInfo> ProductionInfo, int BuildTimeInSeconds)
{
    public static GameObjectInfo From(BackendGameObjectInfo info) => new(
        (int)info.GameObjectType,
        info.GameObjectTypeName,
        info.Price,
        info.ConsumptionInfo.Select(ResourceInfo.From).ToList(),
        info.ProductionInfo.Select(ResourceInfo.From).ToList(),
        info.BuildTimeInSeconds);
}

/// <summary>
/// All data of a chunk, every array is indexed as [x][y]
/// </summary>
/// <param name="Terrain">0 = grass, 1 = beach, 2 = water</param>
/// <param name="GameObjects">The name of the sprite to show, or "none"</param>
/// <param name="Owners">The id of the owner of the GameObject (0 = no owner)</param>
public record ChunkData(int ChunkX, int ChunkY, int[][] Terrain, string[][] GameObjects, long[][] Owners);

public record GoldInfo(string UserName, double CurrentGold, double GoldPerSecond);

/// <param name="Level">info, warning or error</param>
/// <param name="DurationMs">How long to show the notification, null means until the user dismisses it</param>
public record Notification(string Message, string Level, int? DurationMs)
{
    public static Notification Info(string message, int? durationMs = 3000) => new(message, "info", durationMs);

    public static Notification Warning(string message, int? durationMs = 3000) => new(message, "warning", durationMs);

    public static Notification Error(string message, int? durationMs = 5000) => new(message, "error", durationMs);
}
