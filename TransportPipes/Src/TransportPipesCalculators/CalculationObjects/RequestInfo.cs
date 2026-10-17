using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;
using TransportPipesDatabase.Poco;

namespace TransportPipesCalculators.CalculationObjects
{
    public class RequestInfo
    {
        public DbGameObject Requester { get; private set; }
        public List<BackendResourceInfo> ResourcesRequested { get; private set; }
        public List<DbGameObject> OtherProviders { get; private set; }

        public RequestInfo(DbGameObject Requester, List<BackendResourceInfo> resourcesRequested, List<DbGameObject> otherProviders)
        {
            this.Requester = Requester;
            this.ResourcesRequested = resourcesRequested;
            this.OtherProviders = otherProviders;
        }

        public override string ToString()
        {
            return String.Join(",", ResourcesRequested.Select(t => " " + t.ResourceType + ": " + t.Value));
        }
    }
}