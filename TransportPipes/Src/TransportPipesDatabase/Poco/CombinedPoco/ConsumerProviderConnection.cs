using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesDatabase.Poco.CombinedPoco
{
    public class ConsumerProviderConnection
    {
        public int ConnectionId { get; set; }
        public int SourceId { get; set; }
        public int DestinationId { get; set; }
        public int GlobalConnectionId { get; set; }

        public override string ToString()
        {
            return $"{ConnectionId}: ({SourceId} -> {DestinationId}) {{{GlobalConnectionId}}}";
        }
    }
}
