using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TransportPipesCommon.GameLogicObjects.GameObjectInfo
{
    public static class BackendResourceWorthDeterminer
    {
        private static Dictionary<BackendResourceType, BackendResourceWorthInfo> _allResourceTypeValues;
        public static Dictionary<BackendResourceType, BackendResourceWorthInfo> AllResourceTypeValues
        {
            get
            {
                if (_allResourceTypeValues == null)
                {
                    _allResourceTypeValues = new Dictionary<BackendResourceType, BackendResourceWorthInfo>();

                    AddNewInfo(_allResourceTypeValues, 1, 3, 2, BackendResourceType.Energy);
                    AddNewInfo(_allResourceTypeValues, 2, 6, 4, BackendResourceType.IronOre);
                    AddNewInfo(_allResourceTypeValues, 6, 24, 12, BackendResourceType.Iron);
                }
                return _allResourceTypeValues;
            }
        }

        private static void AddNewInfo(Dictionary<BackendResourceType, BackendResourceWorthInfo> allResourceTypeValues, double sellValueToMarket, double buyValueFromMarket, double valueForPlayers, BackendResourceType resourceType)
        {
            allResourceTypeValues.Add(resourceType, new BackendResourceWorthInfo(resourceType, sellValueToMarket, buyValueFromMarket, valueForPlayers));
        }
    }
}