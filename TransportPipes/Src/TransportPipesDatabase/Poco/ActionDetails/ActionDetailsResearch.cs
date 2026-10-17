using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesDatabase.Poco.ActionDetails
{
    public class ActionDetailsResearch : ActionDetailsBase
    {
        public int ResearchLevel { get; set; }
        public DateTime NogWat { get; set; }

        public override string ToString()
        {
            return string.Format("ActionDetailsResearch: ResearchLevel: {0} NogWat: {1}", ResearchLevel, NogWat);
        }
    }
}
