using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipes.Backend.Calculations;
using TransportPipes.Backend.Database;
using TransportPipes.Backend.GameLogicObjects;
using TransportPipes.Backend.GameLogicObjects.Resources;
using TransportPipesCommon.Enums;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;

namespace TransportPipesCalculationTests
{
    public static class TestHelpers
    {
        //Ticks goes fine to up about 1 year (3600 * 24 * 365), when I do this * 10 we get negative int exceptions.
        public static int Ticks = 3600;

        public static List<BackendResourceInfo> SumResources(IEnumerable<BackendResourceInfo> resources)
        {
            var allResourcesTypes = resources.Select(t => t.ResourceType).Distinct();
            var resourcesTotal = new List<BackendResourceInfo>();
            foreach (var resourceType in allResourcesTypes)
            {
                resourcesTotal.Add(new BackendResourceInfo(resourceType, resources.Where(t => t.ResourceType == resourceType).Sum(t => t.Value)));
            }
            return resourcesTotal;
        }

        public static TotalCalculationObject ExecuteCalculationOnBackendGameObject(BackendGameObject backendGameObject)
        {
            var totalCalculation = new TotalCalculationObject();

            backendGameObject.SendOutMyRequestsIfNeeded(Ticks, totalCalculation);

            return totalCalculation;
        }

        public static DbGameObject GenerateDbGameObjectMock(GameObjectType gameObjectType)
        {
            var dbGameObject = new DbGameObject(0, 0, gameObjectType, null);
            return dbGameObject;
        }

        public static BackendGameObject SetConnectionsMock(DbGameObject dbGameObject, IEnumerable<DbGameObject> buildingsIGetFrom, IEnumerable<DbGameObject> buildingsIProvideTo)
        {
            if (buildingsIGetFrom == null)
            {
                buildingsIGetFrom = new List<DbGameObject>();
            }
            if (buildingsIProvideTo == null)
            {
                buildingsIProvideTo = new List<DbGameObject>();
            }

            var backendGameObject = new BackendGameObject(dbGameObject);

            dbGameObject.Destinations = new List<DbConnection>() { new DbConnection()
            {
                Sources = buildingsIGetFrom.ToList(),
                Destinations = new List<DbGameObject>() { dbGameObject }
            } };

            dbGameObject.Sources = new List<DbConnection>() { new DbConnection()
            {
                Sources = new List<DbGameObject>() { dbGameObject  },
                Destinations = buildingsIProvideTo.ToList()
            } };

            return backendGameObject;
        }
    }
}
