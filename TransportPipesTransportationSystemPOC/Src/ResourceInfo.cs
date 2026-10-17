using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesTransportationSystem
{
    public class ResourceInfo
    {
        public ResourceType ResourceType { get; private set; }
        public double Value { get; set; }

        public ResourceInfo(ResourceType resourceType, double value)
        {
            this.ResourceType = resourceType;
            this.Value = value;
        }
    }
}
