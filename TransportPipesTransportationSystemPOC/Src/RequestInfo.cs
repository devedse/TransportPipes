using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesTransportationSystem
{
    public class RequestInfo
    {
        public GameObject Requester { get; private set; }
        public List<ResourceInfo> ResourcesRequested { get; private set; }
        public List<GameObject> OtherProviders { get; private set; }

        public RequestInfo(GameObject Requester, List<ResourceInfo> resourcesRequested, List<GameObject> otherProviders)
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
