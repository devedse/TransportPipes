using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.GameLogicObjects;
using TransportPipes.Backend.GameLogicObjects.Resources;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;

namespace TransportPipes.Backend.Calculations
{
    public class RequestInfo
    {
        public BackendGameObject Requester { get; private set; }
        public List<BackendResourceInfo> ResourcesRequested { get; private set; }
        public List<BackendGameObject> OtherProviders { get; private set; }

        public RequestInfo(BackendGameObject Requester, List<BackendResourceInfo> resourcesRequested, List<BackendGameObject> otherProviders)
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