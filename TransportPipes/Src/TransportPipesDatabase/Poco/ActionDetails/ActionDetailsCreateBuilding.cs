using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon.Enums;

namespace TransportPipesDatabase.Poco.ActionDetails
{
    public class ActionDetailsCreateBuilding : ActionDetailsBase
    {
        public GameObjectType GameObject { get; set; }
        public int PriceOfBuilding { get; set; }

        public override string ToString()
        {
            return string.Format("ActionDetailsCreateBuilding: GameObject: {0}", GameObject);
        }
    }
}
