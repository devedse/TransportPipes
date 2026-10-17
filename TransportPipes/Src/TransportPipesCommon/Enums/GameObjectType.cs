using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TransportPipesCommon.Enums
{
    public enum GameObjectType
    {
        None = 0,

        //Buildings Production
        FactorySmall = 1,
        IronMine = 2,


        //Buildings Energy
        WindMoily = 200,
        SolarPanel = 201,


        Storage = 400,
        TradingBuilding = 450,


        //Pipes (All pipes should be above 10000), we also check on this in the GenericGameObjectTypeHelper
        GreyPipe = 10000
    }
}