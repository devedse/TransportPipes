using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipesV1.Enums;

namespace TransportPipesV1.Database
{
    public static class GameObjectsMockDb
    {
        private static Dictionary<String, GameObjectsEnumDb[,]> gameObjectMap = null;
        public static Dictionary<String, GameObjectsEnumDb[,]> GameObjectMap
        {
            get
            {
                if (gameObjectMap == null)
                {
                    gameObjectMap = new Dictionary<string, GameObjectsEnumDb[,]>();
                }
                return gameObjectMap;
            }
        }
    }
}