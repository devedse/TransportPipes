using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesTransportationSystem
{
    public class CalculationForGameObject
    {
        //All fields needed for the calculation
        public Boolean IHaveSendedOutMyRequests { get; set; }
        public int DestinationsHandled { get; set; } //Can be replaced by Requests.Count()
        public int SourcesHandled { get; set; }

        public List<RequestInfo> Requests = new List<RequestInfo>();
        public List<ProvisionInfo> Provisions = new List<ProvisionInfo>();

        public List<ResourceInfo> Storage = new List<ResourceInfo>();

        public CalculationForGameObject()
        {

        }
    }
}
