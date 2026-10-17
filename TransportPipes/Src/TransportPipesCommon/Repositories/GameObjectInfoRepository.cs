using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon.Enums;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;

namespace TransportPipesCommon.Repositories
{
    public static class GameResourcesRepository
    {
        private static object _lockject = new object();
        private static Dictionary<GameObjectType, BackendGameObjectInfo> _predeterminedGameObjectInfos;
        private static BackendGameObjectInfo _noGameObjectInfo;


        public static Dictionary<GameObjectType, BackendGameObjectInfo> PredeterminedGameObjectInfos
        {
            get
            {
                if (_predeterminedGameObjectInfos == null)
                {
                    lock (_lockject)
                    {
                        if (_predeterminedGameObjectInfos == null)
                        {
                            PredetermineGameObjectInfos();
                        }
                    }
                }
                return _predeterminedGameObjectInfos;
            }
        }

        private const double _defaultTicksMeasure = 3600;

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
            BackendGameObjectInfo foundGameObjectInfo;
            var foundResource = PredeterminedGameObjectInfos.TryGetValue(gameObjectType, out foundGameObjectInfo);

            if (foundResource)
            {
                return foundGameObjectInfo;
            }
            else
            {
                return _noGameObjectInfo;
            }
        }

        private static void PredetermineGameObjectInfos()
        {
            _predeterminedGameObjectInfos = new Dictionary<GameObjectType, BackendGameObjectInfo>();

            AddGameObjectInfo(GameObjectType.FactorySmall,
                200,
                new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 6 / _defaultTicksMeasure), new BackendResourceInfo(BackendResourceType.IronOre, 5 / _defaultTicksMeasure) },
                new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Iron, 5 / _defaultTicksMeasure) },
                120);
            AddGameObjectInfo(GameObjectType.IronMine,
                150,
                new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 5 / _defaultTicksMeasure) },
                new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.IronOre, 5 / _defaultTicksMeasure) },
                60);
            AddGameObjectInfo(GameObjectType.WindMoily,
                100,
                new List<BackendResourceInfo> { },
                new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 10 / _defaultTicksMeasure) },
                60);
            AddGameObjectInfo(GameObjectType.SolarPanel,
                50,
                new List<BackendResourceInfo> { },
                new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 5 / _defaultTicksMeasure) },
                30);

            //Storage building will request exactly the same amount as the total provided by this connection
            AddGameObjectInfo(GameObjectType.Storage,
                500,
                new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 1000), new BackendResourceInfo(BackendResourceType.Iron, 1000), new BackendResourceInfo(BackendResourceType.IronOre, 1000) },
                new List<BackendResourceInfo> { },
                100);
            //Trading building will request exactly the same amount as the total provided by this connection
            AddGameObjectInfo(GameObjectType.TradingBuilding,
                1000,
                new List<BackendResourceInfo> { new BackendResourceInfo(BackendResourceType.Energy, 1000), new BackendResourceInfo(BackendResourceType.Iron, 1000), new BackendResourceInfo(BackendResourceType.IronOre, 1000) },
                new List<BackendResourceInfo> { },
                300);

            _noGameObjectInfo = new BackendGameObjectInfo(
                GameObjectType.None,
                0,
                new List<BackendResourceInfo>(),
                new List<BackendResourceInfo>(),
                0);
        }

        private static void AddGameObjectInfo(GameObjectType gameObjectType, int price, List<BackendResourceInfo> consumptionInfo, List<BackendResourceInfo> productionInfo, int buildTimeInSeconds)
        {
            var newGameObjectInfo = new BackendGameObjectInfo(gameObjectType, price, consumptionInfo, productionInfo, buildTimeInSeconds);
            _predeterminedGameObjectInfos.Add(newGameObjectInfo.GameObjectType, newGameObjectInfo);
        }
    }
}
