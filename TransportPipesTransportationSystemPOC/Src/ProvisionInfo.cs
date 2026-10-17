using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesTransportationSystem
{
    public  class ProvisionInfo
    {
        public List<ResourceInfo> ResourcesProvided { get; private set; }
        public GameObject Provisioner { get; private set; }
        public ProvisionInfo(List<ResourceInfo> resourcesProvided, GameObject provisioner)
        {
            this.ResourcesProvided = resourcesProvided;
            this.Provisioner = provisioner;
        }

        public override string ToString()
        {
            return String.Join(",", ResourcesProvided.Select(t => " " + t.ResourceType + ": " + t.Value));
        }
    }
}
