using TransportPipes.Common.Enums;
using TransportPipes.Common.GameLogicObjects.GameObjectInfo;

namespace TransportPipes.Common.Repositories;

public static class GameResourcesRepository
{
    private const double _defaultTicksMeasure = 3600;

    private static readonly BackendGameObjectInfo _noGameObjectInfo = new BackendGameObjectInfo(
        GameObjectType.None,
        0,
        new List<BackendResourceInfo>(),
        new List<BackendResourceInfo>(),
        0);

    public static IReadOnlyDictionary<GameObjectType, BackendGameObjectInfo> PredeterminedGameObjectInfos { get; } = PredetermineGameObjectInfos();

    public static double DetermineResourceOfTypeProduced(GameObjectType gameObjectType, BackendResourceType backendResourceType)
    {
        var backendResourcesInfo = DetermineGameObjectInfo(gameObjectType);
        if (backendResourcesInfo != null)
        {
            var foundResource = backendResourcesInfo.ProductionInfo.FirstOrDefault(t => t.ResourceType == backendResourceType);
            if (foundResource != null)
            {
                return foundResource.Value;
            }
        }
        return 0;
    }

    public static double DetermineResourceOfTypeConsumed(GameObjectType gameObjectType, BackendResourceType backendResourceType)
    {
        var backendResourcesInfo = DetermineGameObjectInfo(gameObjectType);
        if (backendResourcesInfo != null)
        {
            var foundResource = backendResourcesInfo.ConsumptionInfo.FirstOrDefault(t => t.ResourceType == backendResourceType);
            if (foundResource != null)
            {
                return foundResource.Value;
            }
        }
        return 0;
    }

    public static BackendGameObjectInfo DetermineGameObjectInfo(GameObjectType gameObjectType)
    {
        return PredeterminedGameObjectInfos.TryGetValue(gameObjectType, out var foundGameObjectInfo) ? foundGameObjectInfo : _noGameObjectInfo;
    }

    private static Dictionary<GameObjectType, BackendGameObjectInfo> PredetermineGameObjectInfos()
    {
        var gameObjectInfos = new Dictionary<GameObjectType, BackendGameObjectInfo>();

        AddGameObjectInfo(gameObjectInfos, GameObjectType.FactorySmall,
            200,
            new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 6 / _defaultTicksMeasure), new BackendResourceInfo(BackendResourceType.IronOre, 5 / _defaultTicksMeasure) },
            new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Iron, 5 / _defaultTicksMeasure) },
            120);
        AddGameObjectInfo(gameObjectInfos, GameObjectType.IronMine,
            150,
            new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 5 / _defaultTicksMeasure) },
            new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.IronOre, 5 / _defaultTicksMeasure) },
            60);
        AddGameObjectInfo(gameObjectInfos, GameObjectType.WindMoily,
            100,
            new List<BackendResourceInfo> { },
            new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 10 / _defaultTicksMeasure) },
            60);
        AddGameObjectInfo(gameObjectInfos, GameObjectType.SolarPanel,
            50,
            new List<BackendResourceInfo> { },
            new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 5 / _defaultTicksMeasure) },
            30);

        //Storage building will request exactly the same amount as the total provided by this connection
        AddGameObjectInfo(gameObjectInfos, GameObjectType.Storage,
            500,
            new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 1000), new BackendResourceInfo(BackendResourceType.Iron, 1000), new BackendResourceInfo(BackendResourceType.IronOre, 1000) },
            new List<BackendResourceInfo> { },
            100);
        //Trading building will request exactly the same amount as the total provided by this connection
        AddGameObjectInfo(gameObjectInfos, GameObjectType.TradingBuilding,
            1000,
            new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 1000), new BackendResourceInfo(BackendResourceType.Iron, 1000), new BackendResourceInfo(BackendResourceType.IronOre, 1000) },
            new List<BackendResourceInfo> { },
            300);

        return gameObjectInfos;
    }

    private static void AddGameObjectInfo(Dictionary<GameObjectType, BackendGameObjectInfo> gameObjectInfos, GameObjectType gameObjectType, int price, List<BackendResourceInfo> consumptionInfo, List<BackendResourceInfo> productionInfo, int buildTimeInSeconds)
    {
        var newGameObjectInfo = new BackendGameObjectInfo(gameObjectType, price, consumptionInfo, productionInfo, buildTimeInSeconds);
        gameObjectInfos.Add(newGameObjectInfo.GameObjectType, newGameObjectInfo);
    }
}
