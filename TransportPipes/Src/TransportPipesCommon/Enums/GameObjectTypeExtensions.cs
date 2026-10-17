using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon.Enums;

namespace TransportPipesCommon.Enums
{
    public static class GenericGameObjectTypeHelper
    {
        public static bool IsPipe(this GameObjectType gameObjectType)
        {
            return (int)gameObjectType >= 10000;
        }
    }
}
