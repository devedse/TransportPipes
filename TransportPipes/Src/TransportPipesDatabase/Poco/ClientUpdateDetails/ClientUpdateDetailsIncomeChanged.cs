using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesDatabase.Poco.ClientUpdateDetails
{
    public class ClientUpdateDetailsIncomeChanged : ClientUpdateDetailsBase
    {
        public List<long> DbUserIds { get; set; } = new List<long>();
    }
}
